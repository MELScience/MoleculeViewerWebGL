// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'


Shader "MEL/Volumetric/Balls Single Shader" {
	Properties {
		_DiffuseGain("Diffuse strength", Range(0, 1.5)) = 1.0
		_SpecAmount("Specular amount", Range(0, 1)) = 0.66
		//_HoverColor("Color of hovered atom/molecule", Color) = (1, 1, 1, 0.9)
		//_AtomSizeScale("Atoms scale", Range(0, 3)) = 1
		[HideInInspector] _AtomAlphas("Formula-to-atom view factor", Vector) = (0, 0, 0, 0)
		[Toggle(VOLUMETRIC_NORMALMAP)] _Cubemap("Use cubemap for normal-mapping and diffuse", Int) = 1
		_DiffuseNormal("Diffuse&normal cubemap", Cube) = "_Skybox" {}
		_NormalGain("Normal gain", Range(0, 10)) = 0.23
		_LabelAtlas("Label Atlas", 2D) = "white" {}
		_LabelColor("Label Color", Color) = (1, 1, 1, 0.75)
		//_LabelScaleInv("Label scale inversed", Range(0.3, 2)) = 0.5
		_LabelDilate("Label Font Dilate", Range(-0.3, 0.3)) = 0.08
		//_AOParams("AO settings: xy - bias and scale for soft shadows, zw - scale and bias for strong shadow", Vector) = (0.0, 0.62, 15.4, -15.2)
		[HideInInspector] _Settings("", Vector) = (0, 0, 0, 0)
	}

	CGINCLUDE
			#pragma target 2.0
			#pragma vertex vert
			#pragma fragment frag
			//#pragma vertex vert_deb
			//#pragma fragment frag_deb	 // DEBUG

			#pragma multi_compile __ VOLUMETRIC_NORMALMAP
			#pragma multi_compile __ REDUCED_SHADER_ARRAYS
			//#pragma multi_compile __ DISABLE_AO
		#if defined(REDUCED_SHADER_ARRAYS)
			#define FAKE_INSTANCE_SIZE 90
		#else
			#define FAKE_INSTANCE_SIZE 180
		#endif
			
			#include "UnityCG.cginc"
			#include "Lighting.cginc"

	// global volumetric settings
			#define VOLUMETRIC_TEXT_WORLD_UP
			//#define DISABLE_AO

	// uniform values

		// arrays
			// xyz - object-space positions
			// w - object-space radius
			uniform half4 _Position;
				
			// rgb - atom color
			// a - uv coordinates for label atlas (packed)
			uniform half4 _Appearance;
			
			// xyz - relative position of the neighbor in local space
			// w - biased neighbor radius squared
			uniform half4 _Neighbors1;
			uniform half4 _Neighbors2;
			uniform half4 _Neighbors3;
			uniform half4 _Neighbors4;
			uniform half4 _Neighbors5;
			uniform half4 _Neighbors6;
				
			uniform fixed4 _Colors;
				// rgb - highlight color
				// a - highlight intensity

			uniform half4x4 My_ObjectToWorld;		// instead of unity_ObjectToWorld
			uniform half4x4 My_WorldToObject;		// instead of unity_WorldToObject

			uniform half3 _LocalLightDirection;		// should be passed by app every frame
			uniform half4 _LocalCameraPosition;		// should be passed by app every frame
				// xyz - local camera position
				// w - scale
			uniform half3 _LocalCameraForward;		// should be passed by app every frame
			uniform half3 _MainCameraUpWorldspace;	// should be passed by app every frame
			uniform half3 _MainCameraViewWorldspace;// should be passed by app every frame

	 		uniform half _PixelWorldSpaceFactor;	// doubled tangent of half of FOV angle (vertical) devided by Screen.height, should be passed on application start
			uniform half _SpecAmount;
			uniform half _DiffuseGain;
		#if defined(VOLUMETRIC_NORMALMAP)
			uniform samplerCUBE _DiffuseNormal;
			uniform half _NormalGain;
		#endif
			uniform sampler2D _LabelAtlas;
			uniform fixed4 _LabelColor;
			uniform fixed4 _HoverColor;
			uniform half4 _Settings;
				// x - globalHoverIntensity
				// y - globalHighlightIntensity
				// z - label scale inverted
				// w - atoms size scale
			uniform fixed4 _AtomAlphas;
				// x - atom factor (0 - formula, 1 - regular atom)
				// y - sphere alpha
				// z - label alpha
				// w - highlighted label alpha
			uniform half _LabelDilate;
			uniform half4 _AOParams;

	// VERTEX shader

			struct VertexInput {
				half3 position : POSITION;
			};

			struct VertexOutput {
				// lets take the object space and move it so its origin will be exactly at sphere center. call it "local" space

				half4 	pos : SV_POSITION;
					// xyzw - clip-space position for rasterizer
				fixed4 	color : COLOR0;
					// rgb - diffuse color (light source tint applied)
					// a - label alpha
				fixed4  rim : COLOR1;
					// rgb - color
					// a - intensity
				half4 	fragmentPos : TEXCOORD0;
					// xyz - local-space (origin at sphere center) position of the fragment
					// w - local-space (origin at sphere center) sphere radius (scale already applied)
				half4 	cameraPos : TEXCOORD1;
					// xyz - local-space (origin at sphere center) camera position
					// w - object transform scale
				float4 	labelUVs : TEXCOORD2;
					// xy - relative position within tile of labels atlas
					// zw - label uvs center to limit label tile in fragment shader when label scale is too small
				half4 	aaParams : TEXCOORD3;
					// xy - linear coefficients for fragment-distance-to-alpha convertion to achieve anti-aliased sphere edge
					// zw - linear coefficients for SDF convertion to achieve anti-aliased text
			#if !defined(DISABLE_AO)
				half4	neigh1 : TEXCOORD4;
					// xyz - relative position of the neighbor in local space
					// w - biased neighbor radius squared
				half4	neigh2 : TEXCOORD5;
				half4	neigh3 : TEXCOORD6;
				half4	neigh4 : TEXCOORD7;
				half4	neigh5 : TEXCOORD8;
				half4	neigh6 : TEXCOORD9;
			#endif
			};

			fixed4 frag_deb(VertexOutput i) : COLOR	{ return fixed4(1, 1, 1, 1); }	 // DEBUG

			VertexOutput vert_deb(VertexInput v) {
				VertexOutput o;
				half4 wPos = half4(v.position, 1.0);
				o.pos = UnityObjectToClipPos(wPos);
				return o;
			}

	ENDCG

	SubShader {
		LOD 300
		Tags {
			"RenderType" = "Transparent"
			"Queue" = "Transparent-100"
			"IgnoreProjector" = "True"
			"DisableBatching" = "True"
		}
		Pass {
			Name "FORWARD"
			Tags
			{
				"LightMode" = "ForwardBase"
			}

			Cull Off
			ZTest LEqual
			ZWrite On
			Blend SrcAlpha OneMinusSrcAlpha
			//Blend One One // DEBUG
			
			CGPROGRAM

			VertexOutput vert(VertexInput v) {
				VertexOutput o;
				
				// atom's sphere parameters in local space
				half4 sphereCenter = _Position;
				half4 color = _Appearance;

				// label uv coordinates and SDF parameters for antialiasing
				half2 uv = color.w * half2(1.0, 16.0);
				uv = frac(uv);
				uv.x -= uv.y / 16.0;
				o.labelUVs.zw = uv.xy; // left lower corner of the label tile
				o.labelUVs.xy = 0.5.xx + float2(_Settings.z, _Settings.z) * v.position.xy;

				o.cameraPos.xyz = _LocalCameraPosition.xyz - sphereCenter;

				half transformScale = _LocalCameraPosition.w;// length(My_ObjectToWorld._11_21_31_41);
				o.cameraPos.w = transformScale;
				half radius = max(0.0, sphereCenter.w * _Settings.w);
				o.fragmentPos.w = radius;
				half4 sphereCenterWorld = half4(sphereCenter.xyz, 1.0);
				sphereCenterWorld.xyz = mul(My_ObjectToWorld, sphereCenterWorld).xyz;

				half3 camera = _WorldSpaceCameraPos.xyz;
				half3 view = sphereCenterWorld.xyz - camera;
				half view_len = length(view);

				// ortonormal basis calculations in view-space to be able to extrude billboard vertexes
				// TODO?: optimize: get rid of up/right/viewDir vectors and replace them by matrix calculations
				half3 viewDir = view / view_len;
		#if defined(VOLUMETRIC_TEXT_WORLD_UP)
				half3 up = normalize(half3(0.0, 1.0, 0.0) - viewDir.y * viewDir);
				half3 right = cross(viewDir, up); // no normalization required
		#else
				half3 up = _MainCameraUpWorldspace;
				half3 right = normalize(cross(viewDir, up));
		#endif

				// World-space pixel size for antialiasing
				half vDv = -dot(view, _MainCameraViewWorldspace);
				half worldPixelSize = vDv * _PixelWorldSpaceFactor;
				// atom's sphere edge antialiasing parameters
				half tempAa = 0.5 * transformScale / worldPixelSize;
				o.aaParams.x = tempAa / radius; // slope
				o.aaParams.y = 0.49 + tempAa * radius; // bias

				radius *= transformScale;
				// vertices positioning in world-space
				half3 posDisplacement = (radius + worldPixelSize) * v.position;

				half4 vertexWorldPosition = sphereCenterWorld + half4(posDisplacement, 0);
				o.pos = mul(UNITY_MATRIX_VP, vertexWorldPosition);
				o.fragmentPos.xyz = mul(My_WorldToObject, vertexWorldPosition).xyz - sphereCenter.xyz;

				// color precalculations
				o.color.rgb = color.rgb * _LightColor0.rgb;

				half hoverAlpha = max(0, _Settings.x) * _HoverColor.a;

				fixed4 highlightColor = _Colors;
				half highlightAlpha = max(highlightColor.a, _Settings.y);
				o.rim.a = 1.0 - (1.0 - highlightAlpha) * (1.0 - hoverAlpha);
				o.rim.rgb = highlightColor.rgb * highlightAlpha * (1.0 - hoverAlpha) + _HoverColor.rgb * hoverAlpha;

				//o.color.a = lerp(_AtomAlphas.z, _AtomAlphas.w, nIndx2.z);
				o.color.a = lerp(_AtomAlphas.z, _AtomAlphas.w, 0);

				o.aaParams.z = 0.33 / 2.0 / _Settings.z / worldPixelSize * radius; // slope of the font fadeout edge, 0.33 is a magic number (found emperically), depends on TextMeshPro parameters when lebel atlas generated
				o.aaParams.w = 0.5 - (0.5 - _LabelDilate) * o.aaParams.z; // bias
				
			#if !defined(DISABLE_AO)
				o.neigh1 = _Neighbors1;
				o.neigh2 = _Neighbors2;
				o.neigh3 = _Neighbors3;
				o.neigh4 = _Neighbors4;
				o.neigh5 = _Neighbors5;
				o.neigh6 = _Neighbors6;
			#endif
			
				return o;
			}

	// FRAGMENT shader

			struct FragmentOutput
			{
				fixed4 color : COLOR;
				//half depth : DEPTH;
			};
			FragmentOutput frag(VertexOutput i)
			{
				FragmentOutput output;
				fixed4 outColor;

				// ALL calculations are performed in local space (object-space with origin at sphere center)
				// O - center of the sphere (0, 0, 0)
				// C - camera
				// F - fragment
				// H - projection of O on the CF
				// I - intersection of CF with the sphere

				float R = i.fragmentPos.w; // radius of the sphere
				float3 OF = i.fragmentPos.xyz;
				float3 OC = i.cameraPos.xyz;
				float3 CF = OF - OC;
				float R2 = R * R;

				// computations for pixels outside the sphere: alpha or clip them
				half CF_len = length(CF);
				half3 CF_dir = CF / CF_len;
				float FH_len = dot(CF_dir, OF);
				float OH_len2 = dot(OF, OF) - FH_len * FH_len;
				float IH_len2 = R2 - OH_len2; // R^2 - OH^2 = IH^2

				// sphere's normal calculation
				half FI_len = FH_len + sqrt(max(0.0, IH_len2)); // for AA pixels outside the sphere pretend we are on the sphere edge
				half3 CI = CF - CF_dir * FI_len;
				half3 OI = CI + OC;
				half3 Nu = OI / R; // normal unbent, mathematically equals to normalize(I - O)

				// depth calculations
				half CI_len = CF_len - FI_len;
				clip(CI_len);
				//half depth = CI_len * dot(_LocalCameraForward, CF_dir) * i.cameraPos.w;
				//output.depth = (1.0 / depth -_ZBufferParams.w) / _ZBufferParams.z;
				half opaqueness = i.aaParams.y - OH_len2 * i.aaParams.x;
				outColor.a = saturate(opaqueness);
				clip(opaqueness - 0.01);

				// Label
				float2 labelUV = i.labelUVs.zw + 0.0625 * saturate(i.labelUVs.xy);
				half label = saturate(tex2D(_LabelAtlas, labelUV).a * i.aaParams.z + i.aaParams.w);
				label *= i.color.a;
				outColor.a *= max(_AtomAlphas.y, label);
				clip(outColor.a - 0.01);

		#if defined(VOLUMETRIC_NORMALMAP)
				// read cubemap data
				half3 sampleVector = Nu;
				half4 diffNormalData = texCUBE(_DiffuseNormal, sampleVector);
				half diffuseFactor = lerp(diffNormalData.b, 1.0, _DiffuseGain);
				half2 normalData = _NormalGain * (diffNormalData.rg - half2(0.5, 0.5));
				// normal mapping
				half3 temp = -(1.0 + Nu * Nu);
				half3 tangent = half3(temp.y, Nu.x * Nu.y, Nu.x);
				half3 bitangent = half3(Nu.x * Nu.y, temp.x,  Nu.y);
				half3 Nb = normalize(normalData.r * tangent + normalData.g * bitangent + Nu); // normal bent
		#else
				half diffuseFactor = _DiffuseGain;
				half3 Nb = Nu;
		#endif

				// specular from directional light
				half3 reflectDir = reflect(CF_dir, Nb);
				half RdotL = dot(_LocalLightDirection.xyz, reflectDir);
				half RdotLcap = max(RdotL, 0.0);
				half specStrength = RdotLcap * RdotLcap * RdotLcap; // hardcoded power = 3

				// lighting (double-Lambert) from directional source
				half3 diffuseLighting = lerp(unity_FogColor * 0.1, i.color.rgb * diffuseFactor, 0.5 * (dot(Nb, _LocalLightDirection.xyz) + 1.0));
				// resulting color
				half3 sumColor = lerp(diffuseLighting, _LightColor0.rgb, specStrength * _SpecAmount);

				half lightAO = 1;
			#if !defined(DISABLE_AO)
				// Ambient Occlusion
				half3 OI_dir = OI / R;
				half3 s3; // factor from distance between surfaces
				half3 d3; // factor from angle between surfaces

				half3 NI = OI - i.neigh1.xyz; // N - center of neighbor atom
				s3.x = dot(NI, NI);
				d3.x = dot(OI_dir, NI);
				NI = OI - i.neigh2.xyz;
				s3.y = dot(NI, NI);
				d3.y = dot(OI_dir, NI);
				NI = OI - i.neigh3.xyz;
				s3.z = dot(NI, NI);
				d3.z = dot(OI_dir, NI);

				half3 factor = _Settings.w * half3(i.neigh1.w, i.neigh2.w, i.neigh3.w);
				s3.xyz /= factor * factor;
				
				s3 = _AOParams.yyy / (s3 - _AOParams.xxx) + _AOParams.zzz;
				d3 = 1.0.xxx - 0.5 * d3;
				half3 lighting123 = 1.0.xxx - saturate(_AOParams.w * s3) * d3; // lighting (1-shadow) from neighbors 1, 2 and 3

				NI = OI - i.neigh4.xyz;
				s3.x = dot(NI, NI);
				d3.x = dot(OI_dir, NI);
				NI = OI - i.neigh5.xyz;
				s3.y = dot(NI, NI);
				d3.y = dot(OI_dir, NI);
				NI = OI - i.neigh6.xyz;
				s3.z = dot(NI, NI);
				d3.z = dot(OI_dir, NI);
				factor = _Settings.w * half3(i.neigh4.w, i.neigh5.w, i.neigh6.w);
				s3.xyz /= factor * factor;

				s3 = _AOParams.yyy / (s3 - _AOParams.xxx) + _AOParams.zzz;
				d3 = 1.0.xxx - 0.5 * d3;
				half3 lighting456 = 1.0.xxx - saturate(_AOParams.w * s3) * d3; // lighting (1-shadow) from neighbors 4, 5 and 6
				
				lighting123 *= lighting456;
				lightAO = lighting123.x * lighting123.y * lighting123.z;
				

			#endif

				// add rim highlighting
				half rim = 1.0 + min(1.0, 1.5 - i.rim.a) * dot(CF_dir, Nb); // some hack to make highlighting more noticable for high values of alpha color
				sumColor = sumColor * (1.0 - rim * i.rim.a) + i.rim.rgb * rim;

				// Label blending
				sumColor = lerp(sumColor, _LabelColor.rgb, label);

				sumColor = lightAO * sumColor;
				// output color
				outColor.rgb = sumColor;
				output.color = outColor;
				return output;
			}

			ENDCG
		}
	}
}
