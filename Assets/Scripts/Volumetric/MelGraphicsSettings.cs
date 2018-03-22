using UnityEngine;
using System.Linq;

[CreateAssetMenu()]
public class MelGraphicsSettings : ScriptableObject
{
    private static readonly string PATH = "MELGraphicsSettings";

    private static MelGraphicsSettings _instance;
    public static MelGraphicsSettings instance
    {
        get
        {
            if (_instance == null)
                _instance = Resources.Load<MelGraphicsSettings>(PATH);
            return _instance;
        }
    }
    private static GraphicsPreset _preset;
    public static GraphicsPreset preset { get { if (_preset == null) instance.Init(); return _preset; } }
    private static int verticesPerObjectId = Shader.PropertyToID("_BatchSettingsNum");
    private static int indexingSignId = Shader.PropertyToID("_BatchSettingsSign");
    private static int indexingBiasId = Shader.PropertyToID("_BatchSettingsBias");
    
    [System.Serializable]
    public class GraphicsPreset
    {
        public string name;
        public string[] gpuModels;
        public bool reducedShaderArrays = false;
        [Header("CrystalLattice")]
        public int crystalAtomsBudget;
        public int crystalBondsBudget;
        public float crystalSurfaceAtomsBudget;
        public bool allowAutoFog;
        public int maxAtomsInCrystalBatch { get { return reducedShaderArrays ? 310 : 600; } }
        public int maxBondsInCrystalBatch { get { return reducedShaderArrays ? 150 : 310; } }
        [Header("ParticleRenderer")]
        public MeshStorage.MeshType particleMeshSf;
        public MeshStorage.MeshType particleMeshFlat;
        public bool accurateDepthInParticles = true;
        // technical limitations: for 512 uniform registers - 90, for 1024 uniform registers - 180; but not required now
        public int maxAtomsInParticle { get { return reducedShaderArrays ? 80 : 80; } }
        public ShaderVariantCollection warmUpShaders;
    }

    [SerializeField] private int editorPresetIndex;
    [SerializeField] private int defaultHighPresetIndex;
    [SerializeField] private int defaultLowPresetIndex;
    public GraphicsPreset[] presets = new GraphicsPreset[0];
    [SerializeField] private Shader particleShader;

    public static void SetIndexingForBatch(Material mat, MeshStorage.MeshType type, int objects, bool reverse = false)
    {
        mat.SetInt(verticesPerObjectId, MeshStorage.GetVerticesPerObject(type));
        mat.SetInt(indexingSignId, reverse ? -1 : 1);
        mat.SetInt(indexingBiasId, reverse ? (objects - 1) : 0);
    }

    private void Init()
    {
        //Debug.LogWarningFormat(
        //    "SystemInfo:\ngraphicsDeviceName: '{0}'\ngraphicsDeviceType: '{2}'\ngraphicsDeviceVendor: '{3}'\ngraphicsDeviceVersion: '{5}'\ngraphicsMemorySize: '{6}'\ngraphicsMultiThreaded: '{7}'\ngraphicsShaderLevel: '{8}'\nmaxTextureSize: '{9}'\n",
        //    SystemInfo.graphicsDeviceName,
        //    SystemInfo.graphicsDeviceID,
        //    SystemInfo.graphicsDeviceType,
        //    SystemInfo.graphicsDeviceVendor,
        //    SystemInfo.graphicsDeviceVendorID,
        //    SystemInfo.graphicsDeviceVersion,
        //    SystemInfo.graphicsMemorySize,
        //    SystemInfo.graphicsMultiThreaded,
        //    SystemInfo.graphicsShaderLevel,
        //    SystemInfo.maxTextureSize);
        if (Graphics.activeTier == UnityEngine.Rendering.GraphicsTier.Tier1)
            Debug.LogErrorFormat("Open GL ES 3 is required for the application to run, current device {0} does not support it", SystemInfo.deviceModel);

        string device = SystemInfo.graphicsDeviceName;
        var p = presets.FirstOrDefault(x => x.gpuModels.Contains(device));
        if (p == null)
        {
            p = presets[
                SystemInfo.maxTextureSize > 16000 // we hope if 16k textures are available then 1k uniform vectors are too
                ? defaultHighPresetIndex
                : defaultLowPresetIndex];
#if UNITY_EDITOR
            p = presets[editorPresetIndex];
#endif
        }
        if (p.reducedShaderArrays)
            Shader.EnableKeyword("REDUCED_SHADER_ARRAYS");
        else
            Shader.DisableKeyword("REDUCED_SHADER_ARRAYS");
        if (p.accurateDepthInParticles)
            particleShader.maximumLOD = 400;
        else
            particleShader.maximumLOD = 250;
        _preset = p;

        if (p.warmUpShaders != null)
            p.warmUpShaders.WarmUp();

        Debug.LogFormat("MelGraphicsSettings preset {0} selected for device '{1}', shader arrays: {2}", _preset.name, device, p.reducedShaderArrays ? "REDUCED" : "NORMAL");
    }
}
