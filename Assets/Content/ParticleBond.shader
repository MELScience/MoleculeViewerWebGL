Shader "MEL/Molecule Bond"
{
	Properties
	{
		_Color ("Color", Color) = (1.0, 1.0, 1.0, 1.0)
		//_HoverColor("Highlight Color", Color) = (1, 1, 1, 0)
		_SpecAmount("Specular Amount", Range(0, 1)) = 0.66
		_HoverIntensity("Highlight Intensity", Range(0, 1)) = 0
		_AOParams("AO settings", Vector) = (0.7, 0.3, -0.10309, 1)
		[HideInInspector] _AtomAlphas("Formula-to-atom view factor", Vector) = (0, 0, 0, 0)
	}
	SubShader
	{
		LOD 100
		Tags {
			"RenderType"="Opaque"
			"Queue" = "Geometry"
			"IgnoreProjector" = "True"
		}

		Pass
		{
			Tags { "LightMode" = "ForwardBase" }

			Cull Off
			ZTest LEqual
			ZWrite On
			Blend One Zero

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"
			#include "Lighting.cginc"

			uniform half _SpecAmount;
			uniform fixed3 _Color;
			uniform fixed4 _HoverColor;
			uniform half _HoverIntensity;
			uniform half4 _AOParams;
			uniform fixed4 _AtomAlphas;

			struct appdata
			{
				half4 vertex : POSITION;
				half4 uv : TEXCOORD0;
				half3 normal : NORMAL;
			};
			struct v2f
			{
				half4 	pos : SV_POSITION;
				half4 	uv : TEXCOORD0;
				half3 	normal : NORMAL;
				fixed3 	color : COLOR0;		// rgb - diffuse color (highlighting applied)
				fixed3 	specColor : COLOR1;	// rgb - specular color (highlighting applied, specular power applied)
				half3	view : TEXCOORD1;
			};
			
			v2f vert (appdata v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				o.normal = UnityObjectToWorldNormal(v.normal);
				half highlightingIntensity = _HoverIntensity * _HoverColor.a;
				o.color.rgb = lerp(_Color.rgb * _LightColor0.rgb, _HoverColor.rgb, highlightingIntensity);
				o.specColor.rgb = lerp(0.5 * (_LightColor0.rgb + fixed3(1.0, 1.0, 1.0)) * _SpecAmount, _HoverColor.rgb, highlightingIntensity);
				o.view = mul(unity_ObjectToWorld, v.vertex).xyz - _WorldSpaceCameraPos.xyz;
				return o;
			}
			
			half4 frag (v2f i) : SV_Target
			{
				half3 N = normalize(i.normal);
				half3 L = _WorldSpaceLightPos0.xyz;
				half3 V = normalize(i.view);
				// lighting (double-Lambert) from directional source
				half3 diffuseLighting = lerp(unity_FogColor * 0.1, i.color.rgb, 0.5 * (dot(N, L) + 1.0));
				half3 R = reflect(V, N);
				half RdotL = dot(L, R);
				half RdotLcap = saturate(RdotL);
				half specStrength = RdotLcap * RdotLcap * RdotLcap; // hardcoded power = 3
				// AO
				half edgeDistance = abs(i.uv.x);
				edgeDistance *= edgeDistance;
				half aoLight = 1.0 - saturate(_AOParams.w * edgeDistance);
				aoLight *= 1.0 - _AOParams.w * i.uv.y;

				half3 sumColor = (step(abs(i.uv.w), 0.999) * specStrength) * i.specColor.rgb + diffuseLighting;
				sumColor *= aoLight;
				sumColor = lerp(sumColor, _Color.rgb, _AtomAlphas.x);

				return half4(sumColor, 1.0);
			}
			ENDCG
		}
	}
}
