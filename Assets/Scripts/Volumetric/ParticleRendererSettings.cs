using UnityEngine;

[CreateAssetMenu(fileName = "ParticleRenderingSettings", menuName = "MEL/Particle Rendering Settings")]
public class ParticleRendererSettings : ScriptableObject
{
    #region singleton

    private static readonly string settingsPath = "ParticleRenderingSettings";

    private static ParticleRendererSettings _instance;
    public static ParticleRendererSettings instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Resources.Load<ParticleRendererSettings>(settingsPath);
                if (_instance == null)
                    Debug.LogErrorFormat("Particle rendering settings was not found at '{0}'", settingsPath);
            }
            return _instance;
        }
    }

    #endregion

    [System.Serializable]
    public struct AppearanceSettings
    {
        [Range(0f, 1.5f)] public float atomsScale;
        [Range(0f, 2.5f)] public float bondScale;
        [Range(0f, 3f)] public float bondDistance;
        [Range(0f, 2.5f)] public float aoIntensity;
        [Tooltip("x - 0 for regular atom, 1 for formula\ny - sphere alpha\nz - label alpha\nw - highlighted label alpha")]
        public Vector4 alphaSettings;
        [Range(0f, 2f)] public float labelScale;
        [Range(0f, 1f)] public float alwaysShowBonds; // if 0 - bonds will be hidden if one of the atom's scale is very small

        public static AppearanceSettings Lerp(AppearanceSettings a1, AppearanceSettings a2, float factor)
        {
            var f2 = 1f - factor;
            return new AppearanceSettings
            {
                atomsScale = a1.atomsScale * f2 + factor * a2.atomsScale,
                bondScale = a1.bondScale * f2 + factor * a2.bondScale,
                bondDistance = a1.bondDistance * f2 + factor * a2.bondDistance,
                aoIntensity = a1.aoIntensity * f2 + factor * a2.aoIntensity,
                alphaSettings = a1.alphaSettings * f2 + factor * a2.alphaSettings,
                labelScale = a1.labelScale * f2 + factor * a2.labelScale,
                alwaysShowBonds = a1.alwaysShowBonds * f2 + factor * a2.alwaysShowBonds
            };
        }
        public void SetNaN()
        {
            atomsScale = float.NaN;
            bondScale = float.NaN;
            bondDistance = float.NaN;
            atomsScale = float.NaN;
            aoIntensity = float.NaN;
            alphaSettings = new Vector4(float.NaN, float.NaN, float.NaN, float.NaN);
            labelScale = float.NaN;
            alwaysShowBonds = float.NaN;
        }
    }

    public Material material;
    public ParticleBond[] bondPrefabs;
    public ParticleInteractiveAtom interactiveAtomPrefab;
    public GameObject prototype;
    [Header("Interaction")]
    public Color defaultHoverColor = new Color(1f, 1f, 1f, 0.5f);
    public Color defaultHighlightColor = new Color(0.8f, 1f, 1f, 0.8f);
    public float hoverHighlightingAnimationTime = 0.3f;
    public AnimationCurve blinkCurve;
    public float defaultBlinkDuration = 1.5f;
    [Header("Rendering modes appearances")]
    public float normalMapsThreshold = 1f;
    public AnimationCurve transitionCurve;
    public AppearanceSettings spaceFillingAppearance;
    public AppearanceSettings ballAndStickAppearance;
    public AppearanceSettings ballAndStickFlatAppearance;
    public AppearanceSettings formulaAppearance;
    public AppearanceSettings inCrystalAppearance;
    public AppearanceSettings constructorAppearance;
    public AppearanceSettings covalentNoBondsAppearance;
    [Header("Ambient Occlusion")]
    [Range(0f, 1f)]  public float aoStrengthCurvature = 0f;
    [Range(1f, 3f)]  public float aoMaxDistanceFactor = 2f; // max distance devided by atom radius (it's different for atoms with different radiuses
    
    public static int maxAtoms { get { return MelGraphicsSettings.preset.maxAtomsInParticle; } }
    public MeshStorage.MeshType sfMeshType { get; private set; }
    public Mesh sfMesh { get; private set; }
    public MeshStorage.MeshType flatMeshType { get; private set; }
    public Mesh flatMesh { get; private set; }
    public bool accurateDepthInParticles { get; private set; }

    protected void OnEnable()
    {
        sfMeshType = MelGraphicsSettings.preset.particleMeshSf;
        sfMesh = MeshStorage.GetMesh(sfMeshType, MelGraphicsSettings.preset.maxAtomsInParticle);
        flatMeshType = MelGraphicsSettings.preset.particleMeshFlat;
        flatMesh = MeshStorage.GetMesh(MelGraphicsSettings.preset.particleMeshFlat, MelGraphicsSettings.preset.maxAtomsInParticle);
        accurateDepthInParticles = MelGraphicsSettings.preset.accurateDepthInParticles;
    }
}
