using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//[RequireComponent(typeof(MeshRenderer), typeof(MeshFilter))]
public class ParticleRenderer : MonoBehaviour
{
    #region enums and static data/helpers
    public enum Mode : int
    {
        SpaceFilling = 0,
        BallAndStick,
        InCrystal,
        BallAndStickFlat,
        StructuralFormula,
        FormulaWithoutH,
        SkeletalFormula,
        Hidden,
        InConstructor,
        CovalentNoBonds,
        HiddenFaded
    }
    public enum AnimationMode
    {
        PlayAllStages,
        SkipHydrogenDisappearStage,
        SkipFormulaStages,
        SkipAllStages
    }

    private static int MAX_ATOMS = 0;
    private static int MAX_INDX; // max atoms - 1
    private const float EQUAL_RADIUS = 1f;
    
    private static int formulaToAtomViewId = Shader.PropertyToID("_AtomAlphas");
    private static int hoverPropertyId = Shader.PropertyToID("_HoverColor");

    private static int[] bondsMapping; // buffer to avoid allocations
    private static byte[] formulaSizes; // buffer to avoid allocations
    public static MaterialPropertyBlock bondsMaterialPropertyBlock;
    
    private static Vector4 CLR_Pos = new Vector4(1e4f, 1e4f, 1e4f, 0f);

    // todo: find better mb
    private static MonoBehaviour coroutineHolder { get { return FindObjectOfType<VolumetricCameraHelper>(); } }
    
    private struct MyBondInfo
    {
        public int atom1;
        public int atom2;
        public BondInfo.BondType type;
    }
    private enum OrientationMode
    {
        Invalid,
        Identity,
        FaceCamera,
        Constructor
    }
    #endregion

    #region static API for instantiation

    private static ParticleRenderer InstantiateInner(ParticleInfo particleInfo = null, Transform parent = null, Mode mode = Mode.BallAndStick)
    {
        var settings = ParticleRendererSettings.instance;

        var go = GameObject.Instantiate(settings.prototype, parent);
        go.name = "Particle" + (particleInfo == null ? "" : " " + particleInfo.name);

        var p = go.GetComponent<ParticleRenderer>();
        p.settings = settings;
        p._mode = mode;

        return p;
    }

    public static ParticleRenderer Instantiate(ParticleInfo particleInfo = null, Transform parent = null, Mode mode = Mode.BallAndStick)
    {
        var p = InstantiateInner(particleInfo, parent, mode);
        p.Init();

        if (particleInfo != null) {
            particleInfo.Init();
            p.AddParticle(particleInfo);
        }
        
        p.enabled = true;
        return p;
    }
    
    // TODO: Think and get rid of it.
    public static WaitForAsyncCreate<ParticleRenderer> InstantiateAsync(ParticleInfo pInfo, Transform parent = null, Mode mode = Mode.BallAndStick)
    {
        var operation = new WaitForAsyncCreate<ParticleRenderer>();
        coroutineHolder.StartCoroutineInBackground(InstantiationCoroutine(operation, pInfo, parent, mode), 0.016f, 0.005f);
        return operation;
    }

    private static IEnumerator InstantiationCoroutine(WaitForAsyncCreate<ParticleRenderer> operation, ParticleInfo particleInfo, Transform parent, Mode mode)
    {
        if (particleInfo != null && !particleInfo.initialized)
            yield return particleInfo.InitAsync();

        var p = InstantiateInner(particleInfo, parent, mode);

        yield return null;
        p.Init();
        yield return null;

        if (particleInfo != null)
            yield return p.AddParticleAsyncInner(particleInfo);

        p.enabled = true;
        operation.Finish(p, null);
    }
    
    #endregion

    #region public API (fields, properties, events)

    public event Action<ParticleInteractiveAtom> AtomWasClicked;
    public event Action<ParticleRenderer> ParticleWasClicked;
    public delegate void AtomWasHoveredDelegate(ParticleInteractiveAtom hoveredAtom, ParticleInteractiveAtom previousHoveredAtom);
    public event AtomWasHoveredDelegate AtomWasHovered;
    public event Action<ParticleBond> BondWasClicked;
    public delegate void BondWasHoveredDelegate(ParticleBond hoveredAtom, ParticleBond previousHoveredAtom);
    public event BondWasHoveredDelegate BondWasHovered;
    public event Action<Mode> ModeChanged;

    public int atomsCount { get; private set; }
    [SerializeField] private Mode _mode;
    public Mode mode {
        get { return _mode; }
        set { SetMode(value, defaultModeTransitionTime); }
    }
    public AnimationMode animationMode;
    public float defaultModeTransitionTime = 1.3f;
    public float maxBondLength = 2.5f; // bond will be hidden if it's longer
    public ParticleRendererSettings settings;
    [Range(0, 10f)] public int neighborsUpdatesPerFrame = 5;
    [Range(0f, 2f)] public float subsortsPerFrame = 1.0f;
    public bool isAnimating { get; private set; }
    [NonSerialized] public Transform particleTransform;
    public ParticleRendererSettings.AppearanceSettings currentAppearance; // use it if you want manually animate appearance
    [NonSerialized] public Transform lightTransform; // modify it if you change light source after particle Awake();
    public IEnumerator WaitForEndOfAnimation { get { while (isActiveAndEnabled & isAnimating) yield return null; } }
    public bool bondsAreInteractable;
    public bool atomsAreInteractable;
    public bool highlightParticleIfAtomHovered = false;
    [NonSerialized] public float hoverHighlightingTime;
    public ParticleInteractiveAtom hoveredAtom { get; private set; }
    public ParticleBond hoveredBond { get; private set; }
    public float localScale { get { return particleTransform.localScale.x; } set { particleTransform.localScale = new Vector3(value, value, value); } }
    public float highlightIntensity;
    [NonSerialized] public Color defaultHightlightColor;
    private Color _hoverColor;
    public Color hoverColor
    {
        get { return _hoverColor; }
        set
        {
            _hoverColor = value;
            for (int i = 0; i < _bondsMaterials.Length; i++)
                _bondsMaterials[i].SetColor(hoverPropertyId, _hoverColor);
        }
    }
    public bool hasSkeletalFormula { get { return animatedFormulaStage1 > 0 | animatedFormulaStage2 > 0; } }
    public ParticleInfo.ParticleFlags lastParticleFlags { get; private set; }
    private enum InteractivityRestriction
    {
        None,
        NoInteractiveAtoms,
        NoInteractiveAtomsAndBondColliders,
        NoInteractiveAtomsAndBonds,
        NoBonds
    }
    [SerializeField] private InteractivityRestriction _interactivityRestriction = InteractivityRestriction.None;

    #endregion

    #region private fields and properties
    /// <summary>
    /// !!! For internal usage only, its values will be automatically overwritten - use particleTransform instead
    /// </summary>
    [NonSerialized] public  Transform atomsHolderTransform;

    [NonSerialized] public BallsRenderer ballsRenderer = null;
    
    private ParticleInteractiveAtom[] _interactiveAtoms;
    private Vector3[] atomsPositionsModel;
    private Vector3[] atomsPositionsFormula;
    private Vector3[] atomsPositionsFormulaNoH;
    private Vector3[] atomsPositionsLattice;
    private Vector3[] atomsPositionsConstructor;

    private float[] atomsRadiusesCovalent;
    private float[] atomsRadiusesRendering;
    private float[] atomsRadiusesFormulaNoH;
    private float[] atomsRadiusesFormulaNoCH;

    private int[] inParticleIndexes;
    [NonSerialized] private List<ParticleBond> bondObjects;
    [NonSerialized] private List<MyBondInfo> bondInfos = new List<MyBondInfo>();
    private bool _atomsDirty;
    private bool _shouldRenderAtoms;
    private Transform _camTransform; // cache
    private int lockedAtomsCount;
    private Material[] _bondsMaterials; // assigned to all bonds to make it easier to animate bonds colors in formula transition
    private int animatedFormulaStage1; // how many hydrogens will be hidden in skeletal formula
    private int animatedFormulaStage2; // how many carbons will be hidden in skeletal formula
    private ParticleRendererSettings.AppearanceSettings lastAppearance;
    public void CountLockedAtom(bool locked) { lockedAtomsCount += locked ? 1 : -1; }
    private float animatedHoverIntensity = 0f;
    private float animatedHighlightIntensity = 0f;
    private Quaternion constructorQuaternion = Quaternion.identity;
    private Quaternion latticeQuaternion = Quaternion.identity;
    private float latticeAtomsScale; // store separatly to not modify ScriptableObject with settings
    private CullingGroup _cullingGroup;
    private BoundingSphere[] _cullingSphere = new BoundingSphere[1];
    [NonSerialized] public Vector3 inLatticePos;

    #endregion

    #region Unity callbacks
    protected void Awake()
    {
        if (MAX_ATOMS == 0)
        {
            MAX_ATOMS = ParticleRendererSettings.maxAtoms;
            MAX_INDX = MAX_ATOMS - 1;
            bondsMapping = new int[MAX_ATOMS]; // buffer to avoid allocations
            formulaSizes = new byte[MAX_ATOMS]; // buffer to avoid allocations
        }
        if (settings != null)
            Init();
        else
            enabled = false;
    }

    private void Init()
    {
        Debug.Assert(_targetPositions == null);
        var mainCamera = Camera.main;
        _camTransform = mainCamera.transform;
        lightTransform = LightController.Instance == null || !LightController.Instance.mainLight.isActiveAndEnabled
            ? FindObjectOfType<Light>().transform // todo: get rid of Find
            : LightController.Instance.mainLight.transform;

        particleTransform = transform;
        GameObject holderGO = new GameObject("Atoms Holder");
        atomsHolderTransform = holderGO.transform;
        atomsHolderTransform.SetParent(particleTransform, false);

        // Init BallsRenderer
        ballsRenderer = BallsRenderer.Create(MAX_ATOMS, this.transform, BallsRenderer.Mode.SINGLE);

        ShouldRenderAtoms(_mode != Mode.Hidden & _mode != Mode.HiddenFaded);
        
        if (bondsMaterialPropertyBlock == null)
            bondsMaterialPropertyBlock = new MaterialPropertyBlock();
        lastAppearance.SetNaN();

        _bondsMaterials = new Material[settings.bondPrefabs.Length];
        var materialsDict = new Dictionary<Material, Material>(_bondsMaterials.Length);
        for (int i = 0; i < _bondsMaterials.Length; i++)
        {
            var sharedM = settings.bondPrefabs[i].GetComponent<Renderer>().sharedMaterial;
            Material instancedM;
            if (!materialsDict.TryGetValue(sharedM, out instancedM))
            {
                instancedM = Instantiate(sharedM);
                instancedM.SetColor(hoverPropertyId, settings.defaultHoverColor);
                materialsDict.Add(sharedM, instancedM);
            }
            _bondsMaterials[i] = instancedM;
        }

        hoverHighlightingTime = settings.hoverHighlightingAnimationTime;
        defaultHightlightColor = settings.defaultHighlightColor;
        hoverColor = settings.defaultHoverColor;
        isAnimating = false;
        
        // init arrays
        _targetPositions = MELUtils.CreateArray<Vector4>(CLR_Pos, MAX_ATOMS);
        _startAnimationPositions = MELUtils.CreateArray<Vector4>(CLR_Pos, MAX_ATOMS);

        var hc = defaultHightlightColor; hc.a = 0f;
        switch (_interactivityRestriction)
        {
            case InteractivityRestriction.None:
                _interactiveAtoms = new ParticleInteractiveAtom[MAX_ATOMS];
                bondObjects = new List<ParticleBond>();
                break;
            case InteractivityRestriction.NoBonds:
                _interactiveAtoms = new ParticleInteractiveAtom[MAX_ATOMS];
                break;
            case InteractivityRestriction.NoInteractiveAtoms:
                bondObjects = new List<ParticleBond>();
                break;
            case InteractivityRestriction.NoInteractiveAtomsAndBonds:
                break;
            case InteractivityRestriction.NoInteractiveAtomsAndBondColliders:
                bondObjects = new List<ParticleBond>();
                break;
        }

        atomsPositionsModel = MELUtils.CreateArray<Vector3>(CLR_Pos, MAX_ATOMS);
        atomsPositionsFormula = MELUtils.CreateArray<Vector3>(CLR_Pos, MAX_ATOMS);
        atomsPositionsFormulaNoH = MELUtils.CreateArray<Vector3>(CLR_Pos, MAX_ATOMS);
        atomsPositionsLattice = MELUtils.CreateArray<Vector3>(CLR_Pos, MAX_ATOMS);
        atomsPositionsConstructor = MELUtils.CreateArray<Vector3>(CLR_Pos, MAX_ATOMS);

        atomsRadiusesCovalent = MELUtils.CreateArray<float>(0f, MAX_ATOMS);
        atomsRadiusesRendering = MELUtils.CreateArray<float>(0f, MAX_ATOMS);
        atomsRadiusesFormulaNoH = MELUtils.CreateArray<float>(0f, MAX_ATOMS);
        atomsRadiusesFormulaNoCH = MELUtils.CreateArray<float>(0f, MAX_ATOMS);

        _cullingGroup = new CullingGroup();
        _cullingGroup.targetCamera = mainCamera;
        _cullingGroup.SetBoundingSpheres(_cullingSphere);
        _cullingGroup.SetBoundingSphereCount(1);
        _cullingGroup.onStateChanged = StateChangedMethod;

        _targetMode = _mode;
        ClearAtoms();
        SetMode(_mode, true);
    }

    protected void LateUpdate()
    {
        UpdateAndAnimateAppearanceSettings(false);
        if (atomsCount > 0)
        {
            AnimateOrientation();
            UpdateCullingSphere();
        }
        UpdateInteractiveAtomsUpdateHighlightingUpdateBoundingSphereAnimatePositions();
        if (atomsCount > 0 && bondObjects != null)
            UpdateBondPositions();
    }

    private Coroutine _backgroundWork;
    protected void OnEnable()
    {
#if UNITY_EDITOR
        var mainCamera = Camera.main;
        if (mainCamera != null && mainCamera.GetComponent<VolumetricCameraHelper>() == null)
        {
            Debug.LogErrorFormat("{0} requires {1} on the rendering camera to work correctly: some shaders in {0} use values that updated by {1} based on camera parameters", GetType().Name, typeof(VolumetricCameraHelper).Name);
        }
#endif
        _backgroundWork = StartCoroutine(BackgroundWork());
        ShouldRenderAtoms(_mode != Mode.Hidden & _mode != Mode.HiddenFaded);
        _bondsWereHidden = true;
        if (_interactiveAtoms != null & atomsAreInteractable)
            for (int i = 0; i < MAX_INDX; i++)
                if (ballsRenderer.balls[i].enabled)
                    _interactiveAtoms[i].interactable = true;
    }

    protected void OnDisable()
    {
        if (_targetPositions == null)
            return;
        StopAnimation();
        StopCoroutine(_backgroundWork);
        _backgroundWork = null;
        // disable all colliders and renderers
        ShouldRenderAtoms(false);
        if (bondObjects != null)
            for (int i = bondObjects.Count - 1; i >= 0; i--)
                bondObjects[i].visible = false;
        if (_interactiveAtoms != null)
            for (int i = 0; i < MAX_INDX; i++)
                if (ballsRenderer.balls[i].enabled)
                    _interactiveAtoms[i].interactable = false;
    }

    protected void OnApplicationFocus(bool focus)
    {
        if (!focus)
            return;
#if !UNITY_EDITOR
        if (!enabled)
            return;
#endif

        UpdateAndAnimateAppearanceSettings(true);
        UpdateCullingSphere();
    }

    private bool _isVisible;
    private void StateChangedMethod(CullingGroupEvent evt)
    {
        if (evt.hasBecomeVisible)
            _isVisible = true;
        if (evt.hasBecomeInvisible)
            _isVisible = false;
#if UNITY_EDITOR
        _isVisible = true; // just to render particle in scene view
#endif

        ballsRenderer.SetActive(_shouldRenderAtoms & _isVisible);
    }

    protected void OnDestroy()
    {
        _cullingGroup.Dispose();
        _cullingGroup = null;
    }

#if UNITY_EDITOR
    protected void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(_cullingSphere[0].position, _cullingSphere[0].radius);
    }

    protected void OnWillRenderObject() // for scene camera
    {
        if (_targetPositions == null) return;
        var w2l = atomsHolderTransform.worldToLocalMatrix;
        var cameraTransform = Camera.current.transform;

        // camera position
        Vector4 cameraLocalPos = cameraTransform.position; cameraLocalPos.w = 1.0f;
        cameraLocalPos = w2l * cameraLocalPos;
        cameraLocalPos.w = particleTransform.lossyScale.x;

        // camera forward
        Vector4 cameraForward = cameraTransform.forward; cameraForward.w = 0.0f;
        cameraForward = w2l * cameraForward;

        ballsRenderer.UpdateEditorCamera(cameraLocalPos, cameraForward);
    }
#endif
    #endregion

    #region interactivity: hovering, clicking

    public void HandleAtomClick(ParticleInteractiveAtom atom)
    {
        if (!atomsAreInteractable)
            return;
        if (AtomWasClicked != null)
            AtomWasClicked(atom);
        if (ParticleWasClicked != null)
            ParticleWasClicked(this);
    }

    public void HandleBondClick(ParticleBond bond)
    {
        if (!bondsAreInteractable)
            return;
        if (BondWasClicked != null)
            BondWasClicked(bond);
        if (ParticleWasClicked != null)
            ParticleWasClicked(this);
    }

    /// <summary>
    /// Do not use this method. Use ParticleInteractiveAtom.SetHighlightingColor for more control
    /// </summary>
    public void SetAtomHighlightingColor(ParticleInteractiveAtom ia, Color color)
    {   
        ballsRenderer.balls[ia.index].highlightColor = color;
    }
    
    #endregion

    #region public API functions

    public IEnumerable<ParticleInteractiveAtom> atoms
    {
        get {
            if (_interactiveAtoms == null)
                yield break;
            for (int i = 0; i < MAX_INDX; i++)
            {
                if (!ballsRenderer.balls[i].enabled)
                    continue;
                yield return _interactiveAtoms[i];
            }
        }
    }
    public IEnumerable<ParticleBond> bonds
    {
        get {
            if (bondObjects == null)
                yield break;
            for (int i = 0; i < bondObjects.Count; i++)
                yield return bondObjects[i];
        }
    }

    /// <summary>
    /// Add all atoms and bonds from specified particle
    /// </summary>
    /// <param name="particle">particle from MelDB</param>
    public void AddParticle(ParticleInfo particle)
    {
        var en = AddParticleAsyncInner(particle);
        while (en.MoveNext()) { };
    }

    /// <summary>
    /// Add all atoms and bonds from specified particle
    /// </summary>
    /// <param name="particle">particle from MelDB</param>
    public BackgroundCoroutine AddParticleAsync(ParticleInfo particle)
    { return coroutineHolder.StartCoroutineInBackground(AddParticleAsyncInner(particle), 0.016f, 0.005f); }

    private IEnumerator AddParticleAsyncInner(ParticleInfo particle)
    {
        lastParticleFlags = particle == null ? ParticleInfo.ParticleFlags.None : particle.flags;
        var patoms = particle.atoms;
        var pbonds = particle.bonds;
        int imax = patoms.Count;

        // we will hide C and H in particle only if there are three conected C atoms presented: C-C-C
        bool hideCarbonAndHydrogenInFormula = false;
        int carbons = 0;
        for (int i = patoms.Count - 1; i >= 0; i--)
        {
            formulaSizes[i] = 0;
            var e = patoms[i].element;
            if (e == Element.C)
                carbons++;
            else if (e != Element.H)
                hideCarbonAndHydrogenInFormula = true;
        }
        hideCarbonAndHydrogenInFormula &= carbons > 0;
        hideCarbonAndHydrogenInFormula |= carbons > 1;
        for (int i = pbonds.Count - 1; i >= 0; i--)
        {
            var bond = pbonds[i];
            if (bond.bondType == BondInfo.BondType.DASHED)
                continue;
            byte btype = bond.bondType == BondInfo.BondType.DOUBLE ? (byte)30 : (byte)1;
            var i1 = bond.atom1;
            var i2 = bond.atom2;
            formulaSizes[i1] += btype;
            formulaSizes[i2] += btype;
        }
        yield return null;

        // prepare data about disappearing C and H in formula view
        // 255 - non processed or non-C atom, won't be changed in skeletal formula
        // 254 - C atom with two double bonds - won't be changed in skeletal formula
        // 253 - C atom, will be hidden in skeletal formula
        // lower - H atom, value stores index of corresponding C atom, H will be shrinked towards that C atom
        for (int i = patoms.Count - 1; i >= 0; i--)
            formulaSizes[i] = formulaSizes[i] == 60 ? (byte)254 : (byte)255;
        if (hideCarbonAndHydrogenInFormula)
        {
            for (int i = pbonds.Count - 1; i >= 0; i--)
            {
                var bond = pbonds[i];
                var i1 = bond.atom1;
                var i2 = bond.atom2;
                var a1 = patoms[i1].element;
                var a2 = patoms[i2].element;
                if (a1 == Element.H & a2 == Element.C)
                    formulaSizes[i1] = (byte)i2;
                else if (a1 == Element.C & a2 == Element.H)
                    formulaSizes[i2] = (byte)i1;
                if (a1 == Element.C && formulaSizes[i1] == 255)
                    formulaSizes[i1] = 253;
                if (a2 == Element.C && formulaSizes[i2] == 255)
                    formulaSizes[i2] = 253;
            }
        }
        yield return null;

        // add all atoms
        var hasFlatView = (particle.flags & ParticleInfo.ParticleFlags.Has2D) > 0;
        inParticleIndexes = MELUtils.CreateArray<int>(-1, MAX_ATOMS);
        for (int i = 0; i < imax; i++)
        {
            var atom = patoms[i];
            var formulaSize = formulaSizes[i];
            var atomFlatPos = atom.flatPosition;
            Vector3 flatPos = (hasFlatView ? new Vector3(atomFlatPos.x, atomFlatPos.y) : atom.position);
            Vector4 formula1Pos = flatPos; formula1Pos.w = 1f;
            Vector4 formula2Pos = formula1Pos;
            if (formulaSize < 253)
            {
                // it's H atom - will be hidden and moved behind connected C atom
                int neighborIndx = formulaSize;
                var neighborC = patoms[neighborIndx];
                var neighFlatPos = neighborC.flatPosition;
                var hiddenFlatPos = (hasFlatView ? new Vector3(neighFlatPos.x, neighFlatPos.y) : neighborC.position);
                formula1Pos.x = hiddenFlatPos.x;
                formula1Pos.y = hiddenFlatPos.y;
                formula1Pos.z = hiddenFlatPos.z;
                formula1Pos.w = 0f;
                formula2Pos = formula1Pos;
            }
            else if (formulaSize == 253)
            {
                formula2Pos.w = 0f; // it's C atom, will be hidden
            }
            bondsMapping[i] = AddAtom(atom.element, atom.position, flatPos, formula1Pos, formula2Pos, false);
            inParticleIndexes[bondsMapping[i]] = i;
            yield return null;
        }
        // only now update all neighbors for new atoms
        for (int i = 0; i < imax; i++)
        {   
            ballsRenderer.UpdateNeighbors(bondsMapping[i]);
            yield return null;
        }

        // add bonds
        if (bondObjects != null)
        {
            var newBondsCount = bondInfos.Count + pbonds.Count;
            if (bondInfos.Capacity < newBondsCount)
                bondInfos.Capacity = newBondsCount;
            if (bondObjects.Count < newBondsCount)
                bondObjects.Capacity = newBondsCount;
            for (int i = 0; i < pbonds.Count; i++)
            {
                var bond = pbonds[i];
                int atom1 = bondsMapping[bond.atom1];
                int atom2 = bondsMapping[bond.atom2];
                AddBond(atom1, atom2, bond.bondType);
                yield return null;
            }
        }
    }

    private static Quaternion GetClosestRotation(Vector3[] constructorPositions, Vector3[] atomPositions, int[] particleIndexes)
    {
        const int STEPS = 10;
        var result = Quaternion.identity;
        float bestScore = float.PositiveInfinity;
        for (int attempts = STEPS; attempts > 0; attempts--)
        {
            float angle = 360f * attempts / STEPS - 180f;
            Quaternion current = Quaternion.Euler(0f, 0f, angle);
            float currentScore = 0f;
            for (int ii = 0; ii < MAX_ATOMS; ii++)
            {
                int i = particleIndexes[ii];
                if (i < 0)
                    continue;
                var pos = current * constructorPositions[i];
                currentScore += Vector3.Angle(pos, atomPositions[ii]); // TODO: optimize
            }
            if (currentScore < bestScore)
            {
                bestScore = currentScore;
                result = current;
            }
        }

        return result;
    }

    public bool ApplyConstructorParticlePositions(Vector3[] inConstructorPositions)
    {
        if (inParticleIndexes == null){
            Debug.LogErrorFormat("Trying to read constructor positions for {0} after particle atoms were modified (removed)", GetType().Name);
            return false;
        }
        // first find the best quaternion for the constructed atoms match the 2d structure
        constructorQuaternion = GetClosestRotation(inConstructorPositions, atomsPositionsFormula, inParticleIndexes);

        // now read coordinates taking quaternion into account
        Vector3[] coords;
        float[] radiuses;
        ModeToCoordsAndRadiuses(mode, out coords, out radiuses);
        for (int ii = 0; ii < MAX_ATOMS; ii++)
        {
            int i = inParticleIndexes[ii];
            if (i < 0)
                continue;
            var pos = constructorQuaternion * inConstructorPositions[i];
            atomsPositionsConstructor[ii] = pos;
            if (mode != Mode.InConstructor)
                continue;

            ballsRenderer.balls[ii].localPosition = pos;
            ballsRenderer.balls[ii].radius = (radiuses == null ? EQUAL_RADIUS : radiuses[ii]);
        }

        constructorQuaternion = Quaternion.Inverse(constructorQuaternion);
        if (mode == Mode.InConstructor)
            atomsHolderTransform.localRotation = constructorQuaternion; // immediately rotate to constructor position
        return true;
    }

    public bool ApplyLatticeParticlePositions(Vector3[] inCrystalPositions, Quaternion partRotation, float atomsScale)
    {
        if (inParticleIndexes == null)
        {
            Debug.LogErrorFormat("Trying to read constructor positions for {0} after particle atoms were modified (removed)", GetType().Name);
            return false;
        }
        // now read coordinates taking quaternion into account
        Vector3[] coords;
        float[] radiuses;
        ModeToCoordsAndRadiuses(mode, out coords, out radiuses);
        for (int ii = 0; ii < MAX_ATOMS; ii++)
        {
            int i = inParticleIndexes[ii];
            if (i < 0)
                continue;
            var pos = inCrystalPositions[i];
            atomsPositionsLattice[ii] = pos;
            if (mode != Mode.InCrystal)
                continue;

            ballsRenderer.balls[ii].localPosition = pos;
            ballsRenderer.balls[ii].radius = (radiuses == null ? EQUAL_RADIUS : radiuses[ii]);
        }
        latticeQuaternion = partRotation;
        latticeAtomsScale = atomsScale;
        if (mode == Mode.InCrystal)
        {
            atomsHolderTransform.rotation = partRotation;
            currentAppearance.atomsScale = latticeAtomsScale;
        }
        return true;
    }

    public ParticleInteractiveAtom AddAtom(Element element, Vector3 position3d)
    {
        Vector4 v = position3d; v.w = 1f;
        var indx = AddAtom(element, position3d, position3d, v, v, true);
        return _interactiveAtoms == null ? null : _interactiveAtoms[indx];
    }
    public ParticleInteractiveAtom AddAtom(Element element, Vector3 position3d, Vector3 positionFlat, bool hideInFormula = false)
    {
        Vector4 posHydr = positionFlat;
        posHydr.w = 1f;
        var posFormula = posHydr;
        posFormula.w = hideInFormula ? 0f : 1f;
        var indx = AddAtom(element, position3d, positionFlat, posHydr, posFormula, true);
        return _interactiveAtoms == null ? null : _interactiveAtoms[indx];
    }
    public ParticleInteractiveAtom AddAtom(Element element, Vector3 position3d, Vector3 positionFlat, Vector3 positionHideHydrogen)
    {
        Vector4 posHydr = positionHideHydrogen;
        posHydr.w = 0f;
        var indx = AddAtom(element, position3d, positionFlat, posHydr, posHydr, true);
        return _interactiveAtoms == null ? null : _interactiveAtoms[indx];
    }
    
    public void RemoveAtom(ref ParticleInteractiveAtom atom) { if (atom == null) return; RemoveAtom(atom.index); atom = null; }
    public void RemoveAtom(int index)
    {
        if (index < 0)
        {
            Debug.LogWarning("Trying to remove already removed atom");
            return;
        }

        if (inParticleIndexes != null && inParticleIndexes[index] >= 0)
            inParticleIndexes = null;
        atomsCount--;
        ShouldRenderAtoms(_mode != Mode.Hidden & _mode != Mode.HiddenFaded);

        // clear the atom record in arrays
        ballsRenderer.balls[index].enabled = false;
        
        if (_interactiveAtoms != null)
            _interactiveAtoms[index].Reset(defaultHightlightColor.a);
        
        // remove all bonds with this atom
        RemoveAllBonds(index);
    }

    public void GetBondAtoms(ParticleBond bond, out ParticleInteractiveAtom atom1, out ParticleInteractiveAtom atom2)
    {
        if (_interactiveAtoms == null)
        {
            atom1 = null;
            atom2 = null;
            return;
        }
        atom1 = _interactiveAtoms[bond.atomIndex1];
        atom2 = _interactiveAtoms[bond.atomIndex2];
        if (!atom1.exist)
            atom1 = null;
        if (!atom2.exist)
            atom2 = null;
    }

    public ParticleBond AddBond(ParticleInteractiveAtom atom1, ParticleInteractiveAtom atom2, BondInfo.BondType type = BondInfo.BondType.SINGLE)
    { return AddBond(atom1.index, atom2.index, type); }
    public ParticleBond AddBond(int indx1, int indx2, BondInfo.BondType type = BondInfo.BondType.SINGLE)
    {
        if (indx1 < 0 | indx2 < 0)
        {
            Debug.LogWarning("Trying to add bond with already removed atom(s)");
            return null;
        }

        bondInfos.Add(new MyBondInfo() { atom1 = indx1, atom2 = indx2, type = type });
        int i = bondInfos.Count - 1;
        var bond = bondObjects.Count <= i ? null : bondObjects[i];
        if (bond != null && bond.type != type)
        {
            Destroy(bond.gameObject);
            bond = null;
        }
        if (bond != null)
            bond.interactable = bondsAreInteractable;
        else
        {
            int bondIndx = (int)type;
            bond = Instantiate(settings.bondPrefabs[bondIndx], atomsHolderTransform);
            if (_interactivityRestriction == InteractivityRestriction.NoInteractiveAtomsAndBondColliders)
                bond.DestroyCollider();
            Debug.Assert(bond.type == type, "Invalid bond prefab type: expected " + type + ", get " + bond.type + ". in prefab " + settings.bondPrefabs[bondIndx].name);
            if (!bondsAreInteractable)
                bond.interactable = false;
            bond.SetMaterial(_bondsMaterials[bondIndx]);
            if (bondObjects.Count <= i)
                bondObjects.Add(bond);
            else
                bondObjects[i] = bond;
        }
        bond.Init(indx1, indx2, this, !_bondsWereHidden && enabled);
        return bond;
    }

    public void RemoveBond(ParticleInteractiveAtom atom1, ParticleInteractiveAtom atom2) { RemoveBond(atom1.index, atom2.index); }
    public void RemoveBond(int indx1, int indx2)
    {
        if (indx1 < 0 | indx2 < 0)
        {
            Debug.LogWarning("Trying to remove bond with already removed atom(s)");
            return;
        }
        
        int bondIndx = 0;
        for (; bondIndx < bondInfos.Count; bondIndx++)
        {
            var bond = bondInfos[bondIndx];
            if ((bond.atom1 == indx1 & bond.atom2 == indx2)
               || (bond.atom1 == indx2 & bond.atom2 == indx1))
                break;
        }
        if (bondIndx < bondInfos.Count)
        {
            for (int healthyBond = bondIndx + 1; healthyBond < bondInfos.Count; healthyBond++)
            {
                var bond = bondInfos[healthyBond];
                if ((bond.atom1 == indx1 & bond.atom2 == indx2)
                   || (bond.atom1 == indx2 & bond.atom2 == indx1))
                    continue;
                var oldBond = bondObjects[bondIndx];
                bondObjects[bondIndx] = bondObjects[healthyBond];
                bondObjects[healthyBond] = oldBond;
                bondInfos[bondIndx++] = bond;
            }
            for (int i = bondIndx; i < bondInfos.Count; i++)
                bondObjects[i].Reset();
            bondInfos.RemoveRange(bondIndx, bondInfos.Count - bondIndx);
        }
    }
    public void RemoveBond(ParticleBond bond)
    {
        if (!bond.exist)
        {
            Debug.LogWarning("Trying to remove a bond which already was removed");
            return;
        }
        int bondsCount = bondInfos.Count;
        for (int i = 0; i < bondsCount; i++)
        {
            if (bondObjects[i] != bond)
                continue;
            bond.Reset();
            for (int ii = i + 1; ii < bondsCount; ii++)
            {
                bondInfos[ii - 1] = bondInfos[ii];
                bondObjects[ii - 1] = bondObjects[ii];
            }
            bondInfos.RemoveAt(bondsCount - 1);
            bondObjects[bondsCount - 1] = bond;
            break;
        }
    }
    public void RemoveAllBonds(ParticleInteractiveAtom atom) { RemoveAllBonds(atom.index); }
    public void RemoveAllBonds(int index)
    {
        if (index < 0)
        {
            Debug.LogWarning("Trying to remove all bonds for already removed atom");
            return;
        }
        int bondIndx = 0;
        for (; bondIndx < bondInfos.Count; bondIndx++)
        {
            var bond = bondInfos[bondIndx];
            if (bond.atom1 == index | bond.atom2 == index)
                break;
        }
        if (bondIndx < bondInfos.Count)
        {
            for (int healthyBond = bondIndx + 1; healthyBond < bondInfos.Count; healthyBond++)
            {
                var bond = bondInfos[healthyBond];
                if (bond.atom1 == index | bond.atom2 == index)
                    continue;
                var oldBond = bondObjects[bondIndx];
                bondObjects[bondIndx] = bondObjects[healthyBond];
                bondObjects[healthyBond] = oldBond;
                bondInfos[bondIndx++] = bond;
            }
            for (int i = bondIndx; i < bondInfos.Count; i++)
                bondObjects[i].Reset();
            bondInfos.RemoveRange(bondIndx, bondInfos.Count - bondIndx);
        }
    }

    /// <summary>
    /// Change rendering mode in specified time with current animation settings
    /// </summary>
    /// <param name="newMode">rendering mode to switch to</param>
    /// <param name="duration">duration of one animation stage, skip it for instant change</param>
    /// <param name="forced">if true - start animation/update atom positions even if the current mode is the same as the new one</param>
    public void SetMode(Mode newMode, float duration = -1f, bool forced = false)
    {
        if (_mode == newMode & !forced)
            return;
        if (duration > 0f)
        {
            StopAnimation(false);
            _targetMode = newMode;
            var oldMode = _mode;
            _mode = NextTransitionStage(_mode, newMode);
            _transitionAnimation = StartCoroutine(SmoothModeTransition(duration, oldMode));
        }
        else
        {
            if (_transitionAnimation != null)
            {
                StopCoroutine(_transitionAnimation);
                _transitionAnimation = null;
            }
            _targetMode = newMode;
            SetTargetPositionsAndAppearanceForMode(newMode, _mode);
            _mode = newMode;
            if (_interactiveAtoms == null)
            {
                for (int i = 0; i < MAX_INDX; i++)
                    if (ballsRenderer.balls[i].enabled) {
                        ballsRenderer.balls[i].localPosition = _targetPositions[i];
                        ballsRenderer.balls[i].radius = _targetPositions[i].w;
                    }
            }
            else
            {
                for (int i = 0; i < MAX_INDX; i++)
                    if (ballsRenderer.balls[i].enabled && !_interactiveAtoms[i].locked) {
                        ballsRenderer.balls[i].localPosition = _targetPositions[i];
                        ballsRenderer.balls[i].radius = _targetPositions[i].w;
                    }
            }
            animationProgressSmooth = 1f;
            isAnimating = true;
            AnimateOrientation();
            isAnimating = false;
            currentAppearance = _targetAppearance;
            _hasTargetAppearance = false;
            UpdateMesh();
            ShouldRenderAtoms(_mode != Mode.Hidden & _mode != Mode.HiddenFaded);
        }
        if (ModeChanged != null) ModeChanged(_mode);
    }
    /// <summary>
    /// Instantly change rendering mode
    /// </summary>
    /// <param name="newMode">rendering mode to switch to</param>
    /// <param name="forced">if true - update atom positions even if the current mode is the same as the new one</param>
    public void SetMode(Mode newMode, bool forced) { SetMode(newMode, -1f, forced); }
    /// <summary>
    /// Change rendering mode with default animation speed
    /// </summary>
    /// <param name="newMode">rendering mode to switch to</param>
    /// <param name="forced">if true - start animation even if the current mode is the same as the new one</param>
    public void SetModeAnimated(Mode newMode, bool forced = false) { SetMode(newMode, defaultModeTransitionTime, forced); }

    private bool _aoDisabled = false;
    private MeshStorage.MeshType _atomsMeshType = MeshStorage.MeshType.None;
    private void UpdateMesh(Mode oldMode = Mode.Hidden)
    {
        bool shouldHaveSfMesh = oldMode == Mode.SpaceFilling | _mode == Mode.SpaceFilling | oldMode == Mode.InCrystal | _mode == Mode.InCrystal;

        var newMeshType = shouldHaveSfMesh ? settings.sfMeshType : settings.flatMeshType;
        if (newMeshType != _atomsMeshType)
        {
            _atomsMeshType = newMeshType;
            ballsRenderer.UpdateMesh(shouldHaveSfMesh ? settings.sfMesh : settings.flatMesh);

            ballsRenderer.SetIndexingForBatch(_atomsMeshType, MAX_ATOMS, shouldHaveSfMesh & !settings.accurateDepthInParticles);
        }
    }

#if UNITY_EDITOR
    private readonly float screenAreaInv = 200f / Screen.width * Screen.height;
#else
    private readonly float screenAreaInv = 100f / Screen.width * Screen.height;
#endif
    
    private void UpdateShaderKeywords()
    {
        float _maxAtomCoverage = 0f;
        bool shouldHaveNormals = false;
        if (_mode != Mode.SkeletalFormula & _mode != Mode.StructuralFormula & _mode != Mode.FormulaWithoutH)
        {
            _maxAtomCoverage = ballsRenderer.GetLargestBallCoverage();

            var s = localScale * currentAppearance.atomsScale;
            _maxAtomCoverage *= s * s * screenAreaInv;
            shouldHaveNormals = _maxAtomCoverage > settings.normalMapsThreshold;
        }
        
        ballsRenderer.SetVolumetricNormalmapEnabled(shouldHaveNormals);
        ballsRenderer.SetAOEnabled(currentAppearance.aoIntensity >= 0.05f);
    }

    /// <summary>
    /// Stop rendering mode switch animation
    /// </summary>
    /// <param name="setTargetModeImmediate">if true - immediately change atoms according to target rendering mode</param>
    public void StopAnimation(bool setTargetModeImmediate = true)
    {
        if (isAnimating)
        {
            isAnimating = false;
            _hasTargetAppearance = false;
            StopCoroutine(_transitionAnimation);
        }
        if (setTargetModeImmediate)
            SetMode(_targetMode, true);
    }

    public void ClearAtoms()
    {
        var hc = defaultHightlightColor; hc.a = 0f;
        for (int i = 0; i < MAX_INDX; i++)
        {
            if (!ballsRenderer.balls[i].enabled)
                continue;

            ballsRenderer.balls[i].enabled = false;
            if (_interactiveAtoms != null)
                _interactiveAtoms[i].Reset(defaultHightlightColor.a);
        }
        atomsCount = 0;
        animatedFormulaStage2 = 0;
        animatedFormulaStage1 = 0;
        inParticleIndexes = null;
        lastParticleFlags = ParticleInfo.ParticleFlags.None;
        inLatticePos = new Vector3();
        if (bondObjects != null)
            for (int i = 0; i < bondInfos.Count; i++)
                bondObjects[i].Reset();
        bondInfos.Clear();
        ShouldRenderAtoms(false);
    }
    #endregion

    #region private state machine helper functions
    private void SetTargetPositionsAndAppearanceForMode(Mode mode, Mode oldMode)
    {
        Vector3[] coordsMode;
        float[] radiusMode;
        if (mode == Mode.Hidden)
        {
            // if this is a transition to hidden - then place atoms destination positions the same as they are now
            // only atom size and appearance will be animated
            for (int i = 0; i < MAX_ATOMS; i++)
            {
                var coord = ballsRenderer.balls[i].localPosition;
                _targetPositions[i] = new Vector4(coord.x, coord.y, coord.z, 0f);
            }
        }
        else if (mode == Mode.HiddenFaded)
        {
            for (int i = 0; i < MAX_ATOMS; i++) {
                _targetPositions[i] = ballsRenderer.balls[i].localPosition;
                _targetPositions[i].w = ballsRenderer.balls[i].radius;
            }
        }
        else
        {
            ModeToCoordsAndRadiuses(mode, out coordsMode, out radiusMode);
            PrepareTargetAtoms(coordsMode, radiusMode);
        }
        if (oldMode == Mode.Hidden)
        {
            // if this is a transition from hidden to anything - then place all atoms in the final position
            // only atom size and appearance will be animated
            for (int i = 0; i < MAX_ATOMS; i++)
            {
                var coord = _targetPositions[i];
                if (ballsRenderer.balls[i].enabled) {
                    ballsRenderer.balls[i].localPosition = coord;
                    ballsRenderer.balls[i].radius = 0f;
                }
            }
        }
        else if (oldMode == Mode.HiddenFaded)
        {
            for (int i = 0; i < MAX_ATOMS; i++)
                if (ballsRenderer.balls[i].enabled) {
                    ballsRenderer.balls[i].localPosition = _targetPositions[i];
                    ballsRenderer.balls[i].radius = _targetPositions[i].w;
                }
        }

        switch (mode)
        {
            case Mode.SpaceFilling:
                _targetAppearance = settings.spaceFillingAppearance;
                _targetLocalRotation = Quaternion.identity;
                _targetRotationFaceCamera = false;
                break;
            case Mode.BallAndStick:
                _targetAppearance = settings.ballAndStickAppearance;
                _targetLocalRotation = Quaternion.identity;
                _targetRotationFaceCamera = false;
                break;
            case Mode.BallAndStickFlat:
                _targetAppearance = settings.ballAndStickFlatAppearance;
                _targetRotationFaceCamera = true;
                break;
            case Mode.StructuralFormula:
            case Mode.FormulaWithoutH:
            case Mode.SkeletalFormula:
                _targetAppearance = settings.formulaAppearance;
                _targetRotationFaceCamera = true;
                break;
            case Mode.InCrystal:
                _targetAppearance = settings.inCrystalAppearance;
                _targetAppearance.atomsScale = latticeAtomsScale;
                _targetLocalRotation = Quaternion.Inverse(particleTransform.rotation) * latticeQuaternion;
                _targetRotationFaceCamera = false;
                break;
            case Mode.Hidden:
                _targetAppearance.atomsScale = 0f;
                _targetAppearance.alphaSettings = _startAnimationAppearance.alphaSettings;
                _targetAppearance.aoIntensity = 0f;
                _targetAppearance.labelScale = _startAnimationAppearance.labelScale;
                _targetAppearance.bondDistance = _startAnimationAppearance.bondDistance;
                _targetAppearance.bondScale = 0f;
                break;
            case Mode.HiddenFaded:
                _targetAppearance.atomsScale = _startAnimationAppearance.atomsScale;
                _targetAppearance.alphaSettings =
                    new Vector4(_startAnimationAppearance.alphaSettings.x, 0f, 0f, 0f);
                _targetAppearance.aoIntensity = 0f;
                _targetAppearance.labelScale = _startAnimationAppearance.labelScale;
                _targetAppearance.bondDistance = _startAnimationAppearance.bondDistance;
                _targetAppearance.bondScale = 0f;
                break;
            case Mode.InConstructor:
                _targetAppearance = settings.constructorAppearance;
                _targetLocalRotation = constructorQuaternion;
                _targetRotationFaceCamera = false;
                break;
            case Mode.CovalentNoBonds:
                _targetAppearance = settings.covalentNoBondsAppearance;
                _targetLocalRotation = Quaternion.identity;
                _targetRotationFaceCamera = false;
                break;
            default:
                throw new NotImplementedException("No target appearance logic for mode " + mode);
        }
        if (oldMode == Mode.Hidden)
        {
            _startAnimationAppearance.atomsScale = _targetAppearance.atomsScale;
            _startAnimationAppearance.alphaSettings = _targetAppearance.alphaSettings;
            _startAnimationAppearance.labelScale = _targetAppearance.labelScale;
            _startAnimationAppearance.bondDistance = _targetAppearance.bondDistance;
        }
        else if (oldMode == Mode.HiddenFaded)
        {
            _startAnimationAppearance.atomsScale = _targetAppearance.atomsScale;
            _startAnimationAppearance.alphaSettings =
                    new Vector4(_targetAppearance.alphaSettings.x, 0f, 0f, 0f);
            _startAnimationAppearance.labelScale = _targetAppearance.labelScale;
            _startAnimationAppearance.bondDistance = _targetAppearance.bondDistance;
        }
        _hasTargetAppearance = true;
    }

    private void ModeToCoordsAndRadiuses(Mode mode, out Vector3[] coords, out float[] radius)
    {
        switch (mode)
        {
            case Mode.SpaceFilling:
            case Mode.CovalentNoBonds:
                coords = atomsPositionsModel;
                radius = atomsRadiusesCovalent;
                break;
            case Mode.BallAndStick:
                coords = atomsPositionsModel;
                radius = atomsRadiusesRendering;
                break;
            case Mode.BallAndStickFlat:
                coords = atomsPositionsFormula;
                radius = atomsRadiusesRendering;
                break;
            case Mode.StructuralFormula:
                coords = atomsPositionsFormula;
                radius = null;
                break;
            case Mode.FormulaWithoutH:
                coords = atomsPositionsFormulaNoH;
                radius = atomsRadiusesFormulaNoH;
                break;
            case Mode.SkeletalFormula:
                coords = atomsPositionsFormulaNoH;
                radius = atomsRadiusesFormulaNoCH;
                break;
            case Mode.InCrystal:
                coords = atomsPositionsLattice;
                radius = atomsRadiusesRendering;
                break;
            case Mode.InConstructor:
                coords = atomsPositionsConstructor;
                radius = atomsRadiusesCovalent;
                break;
            default:
                throw new NotImplementedException("No coords and radiuses array selection for mode " + mode);
        }
    }
    
    private void PrepareTargetAtoms(Vector3[] coords, float[] radiuses)
    {
        if (radiuses == null)
        {
            for (int i = 0; i < MAX_ATOMS; i++)
            {
                var coord = coords[i];
                _targetPositions[i] = new Vector4(coord.x, coord.y, coord.z, EQUAL_RADIUS);
            }
        }
        else
        {
            for (int i = 0; i < MAX_ATOMS; i++)
            {
                var coord = coords[i];
                _targetPositions[i] = new Vector4(coord.x, coord.y, coord.z, radiuses[i]);
            }
        }
    }

    private Mode NextTransitionStage(Mode from, Mode to)
    {
        if (from == to)
            return to;
        if (animationMode == AnimationMode.SkipAllStages)
            return to;
        var result = NextValidModeTowardsTargetInner(from, to);
        if (result == to)
            return result;
        if (result == Mode.Hidden || from == Mode.Hidden || result == Mode.HiddenFaded || from == Mode.HiddenFaded)
            return result;
        // here are some transition stages may be skipped depending on the actual existance of C and H atoms
        if (animatedFormulaStage1 == 0)
        {
            if ((from == Mode.FormulaWithoutH & result == Mode.StructuralFormula)
                || (result == Mode.FormulaWithoutH & from == Mode.StructuralFormula))
                return NextTransitionStage(result, to);
            if ((to == Mode.FormulaWithoutH & result == Mode.StructuralFormula)
                || (result == Mode.FormulaWithoutH & to == Mode.StructuralFormula))
                return to;
        }
        if (animatedFormulaStage2 == 0)
        {
            if ((from == Mode.FormulaWithoutH & result == Mode.SkeletalFormula)
                || (result == Mode.FormulaWithoutH & from == Mode.SkeletalFormula))
                return NextTransitionStage(result, to);
            if ((to == Mode.FormulaWithoutH & result == Mode.SkeletalFormula)
                || (result == Mode.FormulaWithoutH & to == Mode.SkeletalFormula))
                return to;
        }
        if (animatedFormulaStage1 == 0 & animatedFormulaStage2 == 0)
        {
            if ((from == Mode.StructuralFormula & result == Mode.SkeletalFormula)
                || (result == Mode.StructuralFormula & from == Mode.SkeletalFormula))
                return NextTransitionStage(result, to);
            if ((to == Mode.StructuralFormula & result == Mode.SkeletalFormula)
                || (result == Mode.StructuralFormula & to == Mode.SkeletalFormula))
                return to;
        }
        return result;
    }

    // this is full transition graph, some states may be skipped depending on animation mode settings
    private Mode NextValidModeTowardsTargetInner(Mode from, Mode to)
    {
        if (to == Mode.Hidden || from == Mode.Hidden || to == Mode.HiddenFaded || from == Mode.HiddenFaded)
            return to;
        switch (from)
        {
            case Mode.SpaceFilling:
                if (to == Mode.InCrystal | to == Mode.InConstructor | to == Mode.CovalentNoBonds)
                    return to;
                return Mode.BallAndStick;
            case Mode.BallAndStick:
                if (to == Mode.SkeletalFormula | to == Mode.StructuralFormula | to == Mode.FormulaWithoutH)
                    return Mode.BallAndStickFlat;
                return to;
            case Mode.InCrystal:
                if (to == Mode.SkeletalFormula | to == Mode.StructuralFormula | to == Mode.FormulaWithoutH)
                    return Mode.BallAndStickFlat;
                // instant transition to
                // InConstructor
                // BallAndStick
                // BallAndStickFlat
                // SpaceFilling
                // CovalentNoBonds
                return to;
            case Mode.BallAndStickFlat:
                if (to == Mode.SpaceFilling | to == Mode.InCrystal | to == Mode.InConstructor | to == Mode.CovalentNoBonds)
                    return Mode.BallAndStick;
                if (animationMode == AnimationMode.SkipFormulaStages)
                    return to;
                if (to == Mode.SkeletalFormula | to == Mode.FormulaWithoutH)
                    return Mode.StructuralFormula;
                return to;
            case Mode.StructuralFormula:
                if (to == Mode.SkeletalFormula)
                    return animationMode == AnimationMode.SkipHydrogenDisappearStage ? to : Mode.FormulaWithoutH;
                if (to == Mode.FormulaWithoutH)
                    return to;
                return Mode.BallAndStickFlat;
            case Mode.FormulaWithoutH:
                if (to == Mode.StructuralFormula | to == Mode.SkeletalFormula)
                    return to;
                if (animationMode == AnimationMode.SkipFormulaStages)
                    return Mode.BallAndStickFlat;
                return Mode.StructuralFormula;
            case Mode.SkeletalFormula:
                if (animationMode == AnimationMode.SkipFormulaStages)
                {
                    if (to == Mode.StructuralFormula | to == Mode.FormulaWithoutH)
                        return to;
                    return Mode.BallAndStickFlat;
                }
                if (animationMode == AnimationMode.SkipHydrogenDisappearStage)
                {
                    if (to == Mode.FormulaWithoutH)
                        return to;
                    return Mode.StructuralFormula;
                }
                return Mode.FormulaWithoutH;
            case Mode.InConstructor:
                return Mode.BallAndStick;
        }
        throw new Exception(string.Format("Not handled transition case from {0} to {1} states of {2}", from, to, GetType().Name));
    }

    private void ShouldRenderAtoms(bool value)
    {
        var newShouldRenderAtoms = value & (atomsCount > 0);
        if (_shouldRenderAtoms == newShouldRenderAtoms)
            return;
        _shouldRenderAtoms = newShouldRenderAtoms;
        ballsRenderer.SetActive(newShouldRenderAtoms & _isVisible);
    }
    #endregion

    #region private rendering loop helper functions

    private void UpdateCullingSphere()
    {
        // prepare bounding sphere
        _cullingSphere[0].position = atomsHolderTransform.localToWorldMatrix.GetColumn(3);
        _cullingSphere[0].radius = particleTransform.lossyScale.x; // here we store particle scale, which later in render loop will be multiplied by maxParticleExtent
    }

    private void UpdateAndAnimateAppearanceSettings(bool force)
    {
        UnityEngine.Profiling.Profiler.BeginSample("Particle Update and Animate Appearance");

        if (_hasTargetAppearance) // animate
            currentAppearance = ParticleRendererSettings.AppearanceSettings.Lerp(_startAnimationAppearance, _targetAppearance, animationProgressSmooth);

        if (force | lastAppearance.aoIntensity != currentAppearance.aoIntensity)
        {
            Vector4 aoSettings = new Vector4();
            aoSettings.x = settings.aoStrengthCurvature;
            aoSettings.y = 1f - settings.aoStrengthCurvature;
            aoSettings.z = -aoSettings.y / (settings.aoMaxDistanceFactor * settings.aoMaxDistanceFactor - settings.aoStrengthCurvature);
            aoSettings.w = currentAppearance.aoIntensity;

            ballsRenderer.SetAOParams(aoSettings);
        }
        
        if (force | lastAppearance.alphaSettings != currentAppearance.alphaSettings)
        {
            ballsRenderer.SetAlphaParams(currentAppearance.alphaSettings);

            for (int i = _bondsMaterials.Length - 1; i >= 0; i--)
                _bondsMaterials[i].SetVector(formulaToAtomViewId, currentAppearance.alphaSettings);
        }

        lastAppearance = currentAppearance;
        UnityEngine.Profiling.Profiler.EndSample();
    }

    private bool _atomsWereInteractable;
    private void UpdateInteractiveAtomsUpdateHighlightingUpdateBoundingSphereAnimatePositions()
    {
        if (!_atomsDirty && lockedAtomsCount == 0
            && (_atomsWereInteractable == atomsAreInteractable))
            return;
        UnityEngine.Profiling.Profiler.BeginSample("Particle Update Interactive Atoms and Animate");
        ParticleInteractiveAtom newHoveredAtom = null;
        float dt = Time.deltaTime;
        Vector4 updSettings = new Vector4(currentAppearance.atomsScale, 1f / Mathf.Max(currentAppearance.atomsScale, 0.0001f), dt / hoverHighlightingTime, isAnimating ? 1f : -1f);
        float maxParticleExtent = 0f;
        for (int i = 0; i < MAX_INDX; i++)
        {
            Vector4 a = ballsRenderer.balls[i].localPosition;
            if (!ballsRenderer.balls[i].enabled)
                continue;
            // animate atoms
            if (isAnimating) {
                ballsRenderer.balls[i].localPosition = Vector4.Lerp(_startAnimationPositions[i], _targetPositions[i], animationProgressSmooth);
                ballsRenderer.balls[i].radius = Mathf.Lerp(_startAnimationPositions[i].w, _targetPositions[i].w, animationProgressSmooth);
            }

            float d = a.x * a.x + a.y * a.y + a.z * a.z;
            if (d > maxParticleExtent)
                maxParticleExtent = d;
            if (_interactiveAtoms != null)
            {
                // update positions for interactive atoms colliders
                var ia = _interactiveAtoms[i];
                ia.UpdateState(ballsRenderer.balls[i], ref updSettings);
                
                if (ia.pointerHovered)
                    newHoveredAtom = ia;
            }
        }
        maxParticleExtent = Mathf.Sqrt(maxParticleExtent);
        _cullingSphere[0].radius *= (maxParticleExtent + currentAppearance.atomsScale);
        // switch interactivity if settings were changed
        if (_interactiveAtoms != null)
            if (atomsAreInteractable != _atomsWereInteractable)
            {
                for (int i = 0; i < MAX_INDX; i++)
                    if (ballsRenderer.balls[i].enabled)
                        _interactiveAtoms[i].interactable = atomsAreInteractable;
                _atomsWereInteractable = atomsAreInteractable;
            }

        // handle atoms' global hover-highlighting
        if (atomsAreInteractable)
        {
            animatedHoverIntensity += ((newHoveredAtom != null || hoveredBond != null)
                & highlightParticleIfAtomHovered ? 1 : -1) * updSettings.z;
            animatedHoverIntensity = Mathf.Clamp01(animatedHoverIntensity);
        }
        // handle atoms' global highlighting
        if (highlightIntensity != animatedHighlightIntensity)
        {
            var speed = updSettings.z;
            if (Mathf.Abs(highlightIntensity - animatedHighlightIntensity) < speed)
                animatedHighlightIntensity = highlightIntensity;
            else
                animatedHighlightIntensity += speed * ((highlightIntensity > animatedHighlightIntensity) ? 1f : -1f);
        }

        // trigger hovered atom change event
        if (newHoveredAtom != hoveredAtom)
        {
            var oldHoveredAtom = hoveredAtom;
            hoveredAtom = newHoveredAtom;
            if (AtomWasHovered != null)
                AtomWasHovered(hoveredAtom, oldHoveredAtom);
        }

        ballsRenderer.SetShaderParams(animatedHoverIntensity, animatedHighlightIntensity, currentAppearance.labelScale, currentAppearance.atomsScale);

        UnityEngine.Profiling.Profiler.EndSample();
    }
    
    private bool _bondsWereHidden = true;
    private bool _bondsWereInteractable;
    private void UpdateBondPositions()
    {
        UnityEngine.Profiling.Profiler.BeginSample("Particle Bonds Update");
        if (currentAppearance.bondDistance > 2f | currentAppearance.bondScale <= 0f)
        {
            if (_bondsWereHidden)
                return;
            // hide all bonds
            for (int i = bondInfos.Count - 1; i >= 0; i--)
                bondObjects[i].visible = false;
            _bondsWereHidden = true;
            if (hoveredBond != null)
            {
                var oldHovered = hoveredBond;
                hoveredBond = null;
                if (BondWasHovered != null)
                    BondWasHovered(hoveredBond, oldHovered);
            }
            return;
        }
        if (_bondsWereHidden)
        {
            // show bonds again
            for (int i = bondInfos.Count - 1; i >= 0; i--)
                bondObjects[i].visible = true;
            _bondsWereHidden = false;
        }

        float bondScale = Mathf.Min(currentAppearance.atomsScale * 10f, currentAppearance.bondScale);
        Vector3 scale = new Vector3(bondScale, bondScale);
        Vector3 localView = atomsHolderTransform.InverseTransformVector(_camTransform.forward);
        ParticleBond newHoveredBond = null;
        for (int i = bondInfos.Count - 1; i >= 0; i--)
        {
            var bond = bondObjects[i];
            if (bondsAreInteractable != _bondsWereInteractable)
                bond.interactable = bondsAreInteractable;
            if (bond.pointerHovered)
                newHoveredBond = bond;
            var info = bondInfos[i];
            Vector4 p1 = ballsRenderer.balls[info.atom1].localPosition;
            p1.w = ballsRenderer.balls[info.atom1].radius;
            Vector4 p2 = ballsRenderer.balls[info.atom2].localPosition;
            p2.w = ballsRenderer.balls[info.atom2].radius;
            Vector3 d = p2 - p1;
            float length = d.magnitude;
            d *= currentAppearance.atomsScale / length; // it's d.normalized * atomsScale
            length -= currentAppearance.atomsScale * (p1.w + p2.w + currentAppearance.bondDistance * 2f);
            if (length <= 0.01f | bondScale < 0.01f)
                bond.localScale = new Vector3();
            else
            {
                bond.SetLocalPositionAndRotation(
                    (Vector3)p1 + d * (p1.w + currentAppearance.bondDistance),
                    Quaternion.LookRotation(d, localView));
                var s = scale * Mathf.Min(1f, 4f * Mathf.Min(p1.w, p2.w) + currentAppearance.alwaysShowBonds);
                if (length > maxBondLength)
                    s *= Mathf.Max(0f, 2f * (maxBondLength + 0.5f - length));
                s.z = length;
                bond.localScale = s;
            }
        }
        // update hovered bond + events
        if (newHoveredBond != hoveredBond)
        {
            var oldHovered = hoveredBond;
            hoveredBond = newHoveredBond;
            if (BondWasHovered != null)
                BondWasHovered(hoveredBond, oldHovered);
        }
        _bondsWereInteractable = bondsAreInteractable;
        UnityEngine.Profiling.Profiler.EndSample();
    }
    
    #endregion

    #region private coroutines (mode animation, background work)
    private Coroutine _transitionAnimation;
    private bool _hasTargetAppearance;
    private ParticleRendererSettings.AppearanceSettings _startAnimationAppearance;
    private ParticleRendererSettings.AppearanceSettings _targetAppearance;
    private Mode _targetMode;
    private Vector4[] _targetPositions;
    private Vector4[] _startAnimationPositions;
    private Quaternion _targetLocalRotation = Quaternion.identity;
    private Quaternion _startRotation = Quaternion.identity;
    private bool _targetRotationFaceCamera;
    [NonSerialized] public float animationProgress;
    public float animationProgressSmooth { get; private set; }
    private IEnumerator SmoothModeTransition(float duration, Mode oldMode)
    {
        UpdateMesh(oldMode);
        isAnimating = true;
        _startAnimationAppearance = currentAppearance;
        SetTargetPositionsAndAppearanceForMode(_mode, oldMode);
        ShouldRenderAtoms((_mode != Mode.Hidden & _mode != Mode.HiddenFaded) || (oldMode != Mode.Hidden & oldMode != Mode.HiddenFaded));
        _startRotation = _targetRotationFaceCamera ? atomsHolderTransform.rotation : atomsHolderTransform.localRotation;
        //Array.Copy(_atomsPositions, _startAnimationPositions, MAX_ATOMS);
        ballsRenderer.CopyPositionsArrayTo(_startAnimationPositions);
        animationProgress = 0;
        while (animationProgress < 1f)
        {
            animationProgressSmooth = settings.transitionCurve.Evaluate(animationProgress);
            _atomsDirty = true;
            yield return null;
            animationProgress += Time.deltaTime / duration;
        }
        animationProgress = 1f;
        animationProgressSmooth = 1f;
        _atomsDirty = true;
        yield return null;
        _hasTargetAppearance = false;
        _transitionAnimation = null;
        if (_mode == _targetMode)
        {
            isAnimating = false;
            UpdateMesh();
            ShouldRenderAtoms(_mode != Mode.Hidden & _mode != Mode.HiddenFaded);
        }
        else
        {
            oldMode = _mode;
            _mode = NextTransitionStage(_mode, _targetMode);
            _transitionAnimation = StartCoroutine(SmoothModeTransition(duration, oldMode));
            if (ModeChanged != null) ModeChanged(_mode);
        }
    }

    private readonly List<int> _lockedIndexes = new List<int>();
    private const int FRAMES_TO_KEYWORDS_UPDATE = 7;
    private IEnumerator BackgroundWork()
    {
        var waiter = new WaitForEndOfFrame();
        int neighUpdIndx = 0;
        int toKeywordsUpdate = 0;
        for (;;)
        {
            _lockedIndexes.Clear();
            if (lockedAtomsCount > 0)
            {
                _atomsDirty = true;
                for (int i = 0; i < MAX_INDX; i++)
                    if (_interactiveAtoms[i] != null && _interactiveAtoms[i].locked)
                        _lockedIndexes.Add(i);
            }

            if (--toKeywordsUpdate < 0)
            {
                toKeywordsUpdate = FRAMES_TO_KEYWORDS_UPDATE;
                UpdateShaderKeywords();
            }

            for (int i = 1 + (int)(atomsCount * subsortsPerFrame); i > 0; i--)
                if (ballsRenderer.SubsortAtoms()) break;

            if (atomsCount <= neighborsUpdatesPerFrame)
            {
                // update neighbors for all atoms
                for (int ii = 0; ii < MAX_INDX; ii++)
                    ballsRenderer.UpdateNeighbors(ii);
            }
            else
            {
                // update neighbors only for neighborsUpdatesPerFrame atoms
                for (int ii = neighborsUpdatesPerFrame; ii > 0;)
                {
                    if (++neighUpdIndx >= MAX_INDX)
                        neighUpdIndx = 0;
                    if (ballsRenderer.UpdateNeighbors(neighUpdIndx))
                    {
                        ii--;
                        _lockedIndexes.Remove(neighUpdIndx);
                    }
                }
                for (int i = _lockedIndexes.Count - 1; i >= 0; i--)
                    ballsRenderer.UpdateNeighbors(_lockedIndexes[i]);
            }
            yield return waiter;
        }
    }
    
    private void AnimateOrientation()
    {
        if (_targetRotationFaceCamera)
        {
            var direction = atomsHolderTransform.position - _camTransform.position;
            var yAngle = -Vector2.SignedAngle(new Vector2(0f, 1f), new Vector2(direction.x, direction.z));
            var xAngle = -Vector2.SignedAngle(new Vector2(1f, 0f), new Vector2(direction.z, direction.y));
            var targetRotation = Quaternion.Euler(xAngle, yAngle, 0f);
            
            atomsHolderTransform.rotation = isAnimating
                ? Quaternion.Lerp(_startRotation, targetRotation, animationProgressSmooth)
                : targetRotation;

            ballsRenderer.transform.rotation = atomsHolderTransform.rotation;
        }
        else if (isAnimating)
        {
            atomsHolderTransform.localRotation = Quaternion.Lerp(_startRotation, _targetLocalRotation, animationProgressSmooth);
            ballsRenderer.transform.localRotation = atomsHolderTransform.localRotation;
        }
    }
    #endregion

    private int AddAtom(Element element,
        Vector3 position, Vector3 flatPosition, Vector4 flatWoHydrogenPosition, Vector4 formulaPosition,
        bool updateNeighbors)
    {
        if (atomsCount >= MAX_INDX)
            throw new Exception(string.Format("Too many atoms in {0}, allowed {1} only", GetType().Name, MAX_ATOMS - 1));
        _atomsDirty = true;
        // find index of free array entry to put atom here
        int addAtomIndex = 0;
        while (ballsRenderer.balls[addAtomIndex].enabled)
            if (++addAtomIndex >= MAX_INDX)
                addAtomIndex = 0;
        // add atom into arrays and to target positions (for rendering modes) arrays
        atomsPositionsModel[addAtomIndex] = position;
        atomsPositionsFormula[addAtomIndex] = flatPosition;
        atomsPositionsFormulaNoH[addAtomIndex] = flatWoHydrogenPosition;
        atomsPositionsConstructor[addAtomIndex] = flatPosition * 2;
        atomsPositionsLattice[addAtomIndex] = new Vector3();

        var info = ElementInfo.Get(element);
        atomsRadiusesCovalent[addAtomIndex] = info.CovalentRadius;
        atomsRadiusesRendering[addAtomIndex] = info.RenderingRadius;
        atomsRadiusesFormulaNoH[addAtomIndex] = flatWoHydrogenPosition.w;
        atomsRadiusesFormulaNoCH[addAtomIndex] = formulaPosition.w;

        float[] radiuses = null;
        // select current position based on current mode
        if (mode == Mode.Hidden) {
            ballsRenderer.balls[addAtomIndex].localPosition = position;
            ballsRenderer.balls[addAtomIndex].radius = 0f;
        } else {
            Vector3[] coords;
            ModeToCoordsAndRadiuses(mode, out coords, out radiuses);
            var currentPos = coords[addAtomIndex];
            ballsRenderer.balls[addAtomIndex].localPosition = currentPos;
            ballsRenderer.balls[addAtomIndex].radius = (radiuses == null ? EQUAL_RADIUS : radiuses[addAtomIndex]);
        }

        // update formula animations counters
        if (flatWoHydrogenPosition.w != 1f)
            animatedFormulaStage1++;
        else if (formulaPosition.w != flatWoHydrogenPosition.w)
            animatedFormulaStage2++;

        // encode atom appearance (color and label UVs)
        int labelU = info.AtomicNumber / 16;
        int labelV = info.AtomicNumber % 16;

        ballsRenderer.balls[addAtomIndex].color = info.DefaultColor.ToColor();
        ballsRenderer.balls[addAtomIndex].labelUV = new Vector2(labelU / 16f, labelV / 16f);

        if (_interactiveAtoms != null)
        {
            // update interactive atom
            var ia = _interactiveAtoms[addAtomIndex];
            var radius = radiuses == null ? 0f : radiuses[addAtomIndex];
            if (ia == null)
            {
                ia = Instantiate(settings.interactiveAtomPrefab, atomsHolderTransform);
                ia.Init(this, position, radius, currentAppearance.atomsScale, addAtomIndex, element, atomsAreInteractable, defaultHightlightColor.a);
                _interactiveAtoms[addAtomIndex] = ia;
            }
            else
                ia.Init(this, position, radius, currentAppearance.atomsScale, addAtomIndex, element, atomsAreInteractable, defaultHightlightColor.a);
        }
        // add rendering sorting index to the last available free slot (during sorting all occupied indexes are moved to the beginning of the array)
        ballsRenderer.AssignFreeSortingIndex(addAtomIndex);

        atomsCount++;
        int indx = addAtomIndex;
        if (updateNeighbors)
            ballsRenderer.UpdateNeighbors(addAtomIndex);
        if (++addAtomIndex >= MAX_INDX)
            addAtomIndex = 0;
        ShouldRenderAtoms(_mode != Mode.Hidden & _mode != Mode.HiddenFaded);
        return indx;
    }
}

public static class ParticleRendererModeExtensions
{
    public static bool isFormulaLike(this ParticleRenderer.Mode mode)
    {
        return mode == ParticleRenderer.Mode.FormulaWithoutH
             | mode == ParticleRenderer.Mode.SkeletalFormula
             | mode == ParticleRenderer.Mode.StructuralFormula;
    }

    public static bool isHidden(this ParticleRenderer.Mode mode)
    {
        return mode == ParticleRenderer.Mode.Hidden
             | mode == ParticleRenderer.Mode.HiddenFaded;
    }

    public static bool isFlat(this ParticleRenderer.Mode mode)
    {
        return mode.isFormulaLike()
            | mode == ParticleRenderer.Mode.BallAndStickFlat;
    }
}