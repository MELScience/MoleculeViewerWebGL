using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Access individual balls via array of BallInfos
/// Internally has list of BallsBuckets with corresponding BallsBucketDatas
/// BallInfos push data to BallsBucketDatas
/// BallsBucket applies it's data to material or list of GameObjects or whatever
/// </summary>
public class BallsRenderer : MonoBehaviour
{
    public enum Mode {
        BATCH = 0,
        SINGLE
    }

    private const int NEIGHBORS_NUM = 6;
    private const float INDEX_OFFSET = 0.2f;

    private static Vector4 CLR_Pos = new Vector4(1e4f, 1e4f, 1e4f, 0f);

    private const float billboardScaleFactor = 1.082393f; // for octagon billboard mesh

    #region Shader properties

    private static int positionsArrayId = Shader.PropertyToID("_Positions");
    private static int positionId = Shader.PropertyToID("_Position");
    private static int appearanceId = Shader.PropertyToID("_Appearance");
    private static int neighborsArrayId1 = Shader.PropertyToID("_Neighbors1");
    private static int neighborsArrayId2 = Shader.PropertyToID("_Neighbors2");

    private static int neighborId1 = Shader.PropertyToID("_Neighbors1");
    private static int neighborId2 = Shader.PropertyToID("_Neighbors2");
    private static int neighborId3 = Shader.PropertyToID("_Neighbors3");
    private static int neighborId4 = Shader.PropertyToID("_Neighbors4");
    private static int neighborId5 = Shader.PropertyToID("_Neighbors5");
    private static int neighborId6 = Shader.PropertyToID("_Neighbors6");
    
    private static int highlightColorsArrayId = Shader.PropertyToID("_Colors");
    private static int localLightId = Shader.PropertyToID("_LocalLightDirection");
    private static int localCameraPosId = Shader.PropertyToID("_LocalCameraPosition");
    private static int localCameraForwardId = Shader.PropertyToID("_LocalCameraForward");
    private static int aoSettingsId = Shader.PropertyToID("_AOParams");
    private static int alphaSettingsId = Shader.PropertyToID("_AtomAlphas");
    private static int settingsId = Shader.PropertyToID("_Settings");
    private static int hoverPropertyId = Shader.PropertyToID("_HoverColor");
    private static int objectToWorldMatrixId = Shader.PropertyToID("My_ObjectToWorld");
    private static int worldToObjectMatrixId = Shader.PropertyToID("My_WorldToObject");

    private static int billboardExtrudeFactorId = Shader.PropertyToID("_BillboardExtrudeFactor");

    private static readonly string disableAoKeyword = "DISABLE_AO";

    #endregion

    #region Internal classes

    // Container class for data arrays.
    public class BallsData
	{
        public int size = 0;
        public float FREE_INDX = 0f;
        public Vector4 CLR_Indexes = Vector4.zero;
        public Vector4 CLR_Indexes_2 = Vector4.zero;

        // xyz - position; w - radius
        public Vector4[] positions = null;
		public bool positionsDirty = false;

		// xyz - rgb ball colors, w - packed label atlas uv coords
		public Vector4[] ballsAppearance = null;
		public bool ballsAppearanceDirty = false;

		// xyz - ball highlighting color, w - intensity of highlighting
		public Vector4[] highlightColors = null;
		public bool highlightColorsDirty = false;

        // x, y, z, w - indexes of atom neighbors 1-4, always sorted in decreased distance
        public Vector4[] _atomsNeighbors1 = null;

        // x, y - indexes of atom neighbors 5-6, always sorted in decreased distance
        // z - highlighting (hover) intensity
        // w - atom index in sorted order for back-to-front rendering with AA
        public Vector4[] _atomsNeighbors2 = null;
        
        // Temporary values only
        public float[,] _neighborDistances = null;
        public int[,] _neighborIndexes = null;
        public bool _neighborsDirty = false;


        public BallsData(int _size)
		{
            size = _size;
            FREE_INDX = (size - 1) + INDEX_OFFSET;
            CLR_Indexes = FREE_INDX * Vector4.one;
            CLR_Indexes_2 = new Vector4(FREE_INDX, FREE_INDX, 0f, FREE_INDX);

            positions = MELUtils.CreateArray<Vector4>(CLR_Pos, size);
			ballsAppearance = MELUtils.CreateArray<Vector4>(CLR_Pos, size);
            highlightColors = MELUtils.CreateArray<Vector4>(CLR_Pos, size);

            _atomsNeighbors1 = MELUtils.CreateArray<Vector4>(CLR_Indexes, size);
            _atomsNeighbors2 = MELUtils.CreateArray<Vector4>(CLR_Indexes_2, size);

            _neighborDistances = new float[size, NEIGHBORS_NUM];
            for (int x = 0; x < size; x++)
                for (int y = 0; y < NEIGHBORS_NUM; y++)
                    _neighborDistances[x, y] = float.MaxValue;

            _neighborIndexes = new int[size, NEIGHBORS_NUM];
            for (int x = 0; x < size; x++)
                for (int y = 0; y < NEIGHBORS_NUM; y++)
                    _neighborIndexes[x, y] = size - 1;
        }

        public bool SubsortAtoms(Vector3 localCameraPosition)
        {
            UnityEngine.Profiling.Profiler.BeginSample("Subsort Particle Atoms");

            int unorderedIndx1 = -1;
            int unorderedIndx2 = -1;
            float lastDistance = float.NegativeInfinity;

            for (int i = size - 1; i >= 0; i--) {
                Vector3 pos = positions[(int)_atomsNeighbors2[i].w];
                if (pos.x == CLR_Pos.x)
                    continue;
                var distance = (pos - (Vector3)localCameraPosition).sqrMagnitude;
                if (distance < lastDistance) {
                    unorderedIndx1 = i;
                    break;
                }
                unorderedIndx2 = i;
                lastDistance = distance;
            }
            if (unorderedIndx1 < 0) // everything is perfectly sorted
            {
                UnityEngine.Profiling.Profiler.EndSample();
                return true;
            }
            // swap unorderedIndx and unorderedIndx2
            var n1 = _atomsNeighbors2[unorderedIndx1].w;
            var n2 = _atomsNeighbors2[unorderedIndx2].w;
            _atomsNeighbors2[unorderedIndx1].w = n2;
            _atomsNeighbors2[unorderedIndx2].w = n1;

            UnityEngine.Profiling.Profiler.EndSample();

            return false;
        }
    }
 
	// Proxy acessor class
	public class BallInfo {
		public int index;
		private BallsData data;

		public BallInfo(BallsData _data, int _index)
		{
            data = _data;
            index = _index;
		}
       
        public Vector3 localPosition {
			 get { return data.positions[index]; }
			 set { 
				 data.positions[index].x = value.x;
				 data.positions[index].y = value.y;
				 data.positions[index].z = value.z;
                 data.positionsDirty = true;
            }
		}

        public float radius {
            get { return data.positions[index].w; }
            set { data.positions[index].w = value; data.positionsDirty = true; }
        }

        public Vector3 worldPosition {
			get { throw new System.NotImplementedException(); }
			set { throw new System.NotImplementedException(); }
		}

        public Color color {
			get { return new Color(data.ballsAppearance[index].x, data.ballsAppearance[index].y, data.ballsAppearance[index].z, 1f); }
			set { 
				data.ballsAppearance[index].x = value.r;
				data.ballsAppearance[index].y = value.g;
				data.ballsAppearance[index].z = value.b;
                data.ballsAppearanceDirty = true;
            }
		}

        public Vector2 labelUV {
			get {
				return new Vector2(Mathf.Floor(data.ballsAppearance[index].z * 16f) / 16f,
									data.ballsAppearance[index].z * 16f - Mathf.Floor(data.ballsAppearance[index].z * 16f));
			}
			set {
				data.ballsAppearance[index].w = (value.x + value.y / 16f);
                data.ballsAppearanceDirty = true;
            }
		}

        public Color highlightColor {
			get {
                Vector4 c = data.highlightColors[index];
                return new Color(c.x, c.y, c.z, 1f);
            }
            set {
                data.highlightColors[index].x = value.r;
                data.highlightColors[index].y = value.g;
                data.highlightColors[index].z = value.b;
                data.highlightColorsDirty = true;
            }
		}

        public float highlightIntensity {
            get { return data.highlightColors[index].w; }
            set { data.highlightColors[index].w = value; data.highlightColorsDirty = true; }
        }

        public float hoverIntensity {
            get { return data._atomsNeighbors2[index].z; }
            set { data._atomsNeighbors2[index].z = value; data._neighborsDirty = true; }
        }

        public bool enabled {
            get { return (data.positions[index].x != CLR_Pos.x); }
            set { if (!value) {
                    data.positions[index] = CLR_Pos;

                    data._atomsNeighbors1[index] = data.CLR_Indexes;
                    data._atomsNeighbors2[index].x = data.FREE_INDX;
                    data._atomsNeighbors2[index].w = data.FREE_INDX;

                    for (int i = 0; i < NEIGHBORS_NUM; i++) {
                        data._neighborDistances[index, i] = float.MaxValue;
                        data._neighborIndexes[index, i] = data.size - 1;
                    }
                    
                    // remove this atom from sorted rendering order array entry
                    for (int i = 0; i < data.size; i++)
                        if ((int)data._atomsNeighbors2[i].w == index) {
                            data._atomsNeighbors2[i].w = data.FREE_INDX;
                            break;
                        }

                    data.positionsDirty = true;
                }
            }
        }
	}

    private abstract class BallsBucketBase
    {
        public GameObject holderObject = null;

        // Cached variables
        protected Transform thisTransform = null;
        protected Transform cameraTransform = null;
        protected Transform lightTransform = null;
        // TODO: move to protected once Subsort internalized
        public Vector4 localCameraPosition = Vector4.zero;
        protected Vector4 localCameraForward = Vector4.zero;
        protected Vector4 localLightDirection = Vector4.zero;

        public BallsBucketBase(BallsRendererSettings settings, Transform parent)
        {
            holderObject = GameObject.Instantiate(settings.bucketPrototype, parent);

            thisTransform = holderObject.transform;
            cameraTransform = Camera.main.transform;
            lightTransform = (LightController.Instance == null || !LightController.Instance.mainLight.isActiveAndEnabled)
                    ? FindObjectOfType<Light>().transform
                    : LightController.Instance.mainLight.transform;
        }

        public abstract void ApplyData(BallsData data, bool force = false);

        public abstract void Update();

        public void UpdateCachedVariables()
        {
            // world matrices
            var l2w = thisTransform.localToWorldMatrix;
            var w2l = thisTransform.worldToLocalMatrix;
            var scale = thisTransform.lossyScale.x;

            // camera position
            Vector4 camPos = cameraTransform.position; camPos.w = 1.0f;

            this.localCameraPosition = w2l * camPos;
            this.localCameraPosition.w = scale;

            // camera forward
            Vector4 camFwd = cameraTransform.forward; camFwd.w = 0.0f;
            this.localCameraForward = (w2l * camFwd).normalized;

            // light direction
            Vector4 lightDir = -lightTransform.forward; lightDir.w = 0.0f;
            this.localLightDirection = (w2l * lightDir).normalized;
        }

        public void ApplyCachedVariables(Material material)
        {
            material.SetMatrix(objectToWorldMatrixId, thisTransform.localToWorldMatrix);
            material.SetMatrix(worldToObjectMatrixId, thisTransform.worldToLocalMatrix);

            material.SetVector(localCameraForwardId, this.localCameraForward);
            material.SetVector(localCameraPosId, this.localCameraPosition);
            material.SetVector(localLightId, this.localLightDirection);
        }

        public abstract void SetBillboardExtrudeFactor(float billboardScaleFactor);
        public abstract void SetHoverColor(Color hoverColor);
        public abstract void SetAOParams(Vector4 aoParams);
        public abstract void SetShaderParams(Vector4 shaderParams);
        public abstract void SetAlphaParams(Vector4 alphaParams);
        public abstract void SetIndexingParams(MeshStorage.MeshType type, int objects, bool reverse = false);
        public abstract void EnableKeyword(string keyword, bool enable);
        public abstract void UpdateEditorCamera(Vector4 cameraLocalPos, Vector4 cameraForward);
        public abstract void UpdateMesh(Mesh newMesh);
    }

	private class BallsBatchBucket : BallsBucketBase {

        public MeshFilter meshFilter = null;
        private Material material = null;

        public BallsBatchBucket(BallsRendererSettings settings, Transform parent)
            : base(settings, parent)
		{
			var meshRenderer = this.holderObject.GetComponent<MeshRenderer>();
            meshFilter = this.holderObject.GetComponent<MeshFilter>();

            meshRenderer.material = Instantiate(settings.material);
			material = meshRenderer.material;

            material.SetFloat(billboardExtrudeFactorId, billboardScaleFactor);
        }

        public override void ApplyData(BallsData data, bool force = false)
        {
            ApplyBatchData(data, force);
        }

        public override void Update()
        {
            UpdateCachedVariables();
        }

        private void ApplyBatchData(BallsData data, bool force = false)
        {
            if (data.positionsDirty || force) {
                material.SetVectorArray(positionsArrayId, data.positions);
                data.positionsDirty = false;
            }

            if (data.ballsAppearanceDirty || force) {
                material.SetVectorArray(appearanceId, data.ballsAppearance);
                data.ballsAppearanceDirty = false;
            }

            if (data._neighborsDirty || force) {
                for (int i = 0; i < data.size; i++) {
                    data._atomsNeighbors1[i] = new Vector4(data._neighborIndexes[i, 0], data._neighborIndexes[i, 1], data._neighborIndexes[i, 2], data._neighborIndexes[i, 3]) + INDEX_OFFSET * Vector4.one;
                    data._atomsNeighbors2[i].x = data._neighborIndexes[i, 4] + INDEX_OFFSET;
                    data._atomsNeighbors2[i].y = data._neighborIndexes[i, 5] + INDEX_OFFSET;
                }

                material.SetVectorArray(neighborsArrayId1, data._atomsNeighbors1);
                material.SetVectorArray(neighborsArrayId2, data._atomsNeighbors2);

                data._neighborsDirty = false;
            }

            if (data.highlightColorsDirty || force) {
                material.SetVectorArray(highlightColorsArrayId, data.highlightColors);
                data.highlightColorsDirty = false;
            }

            ApplyCachedVariables(material);
        }


        #region Properties setters

        public override void EnableKeyword(string keyword, bool enable)
        {
            if (enable)
                material.EnableKeyword(keyword);
            else
                material.DisableKeyword(keyword);
        }

        public override void SetAlphaParams(Vector4 alphaParams)
        {
            material.SetVector(alphaSettingsId, alphaParams);
        }

        public override void SetAOParams(Vector4 aoParams)
        {
            material.SetVector(aoSettingsId, aoParams);
        }

        public override void SetBillboardExtrudeFactor(float billboardScaleFactor)
        {
            material.SetFloat(billboardExtrudeFactorId, billboardScaleFactor);
        }

        public override void SetHoverColor(Color hoverColor)
        {
            material.SetColor(hoverPropertyId, hoverColor);
        }

        public override void SetIndexingParams(MeshStorage.MeshType type, int objects, bool reverse = false)
        {
            MelGraphicsSettings.SetIndexingForBatch(material, type, objects, reverse);
        }

        public override void SetShaderParams(Vector4 shaderParams)
        {
            material.SetVector(settingsId, shaderParams);
        }

        public override void UpdateEditorCamera(Vector4 cameraLocalPos, Vector4 cameraForward)
        {
            material.SetVector(localCameraPosId, cameraLocalPos);
            material.SetVector(localCameraForwardId, cameraForward.normalized);
        }

        public override void UpdateMesh(Mesh newMesh)
        {
            meshFilter.mesh = newMesh;
        }

        #endregion
    }

    private class BallsListBucket : BallsBucketBase
    {
        private int size = 0;
        private Transform[] transforms = null;
        private Material[] materials = null;
        private MeshFilter[] meshes = null;

        // TODO: Async init
        public BallsListBucket(BallsRendererSettings settings, int _size, Transform parent)
            : base(settings, parent)
        {
            size = _size;
            transforms = new Transform[size];
            materials = new Material[size];
            meshes = new MeshFilter[size];

            Mesh atomMesh = MeshStorage.GetMesh(MeshStorage.MeshType.TrueSphere, 1);

            for (int i = 0; i < size; i++) {
                GameObject go = GameObject.Instantiate(settings.ballObjectPrototype, thisTransform);
                go.name = "ball_" + i;
                go.GetComponent<MeshFilter>().sharedMesh = atomMesh;

                transforms[i] = go.transform;
                materials[i] = go.GetComponent<MeshRenderer>().material;
                meshes[i] = go.GetComponent<MeshFilter>();

                materials[i].SetFloat(billboardExtrudeFactorId, billboardScaleFactor);
            }
        }

        public override void Update()
        {
            UpdateCachedVariables();
        }

        public override void ApplyData(BallsData data, bool force = false)
        {   
            for (int i = 0; i < size; i++) {
                transforms[i].localPosition = data.positions[i];
                materials[i].SetVector(positionId, data.positions[i]);
            }

            for (int i = 0; i < size; i++) {
                materials[i].SetVector(appearanceId, data.ballsAppearance[i]);
            }

            for (int i = 0; i < size; i++) {
                Vector4 thisPosition = data.positions[i];
                thisPosition.w = 0f;

                Vector4 n1 = data.positions[data._neighborIndexes[i, 0]] - thisPosition;
                Vector4 n2 = data.positions[data._neighborIndexes[i, 1]] - thisPosition;
                Vector4 n3 = data.positions[data._neighborIndexes[i, 2]] - thisPosition;
                Vector4 n4 = data.positions[data._neighborIndexes[i, 3]] - thisPosition;
                Vector4 n5 = data.positions[data._neighborIndexes[i, 4]] - thisPosition;
                Vector4 n6 = data.positions[data._neighborIndexes[i, 5]] - thisPosition;

                if (i == 0)
                    Debug.LogFormat("{0} {1} {2}", (Vector3)n1, ((Vector3)n1).magnitude, n1.w);

                materials[i].SetVector(neighborId1, n1);
                materials[i].SetVector(neighborId2, n2);
                materials[i].SetVector(neighborId3, n3);
                materials[i].SetVector(neighborId4, n4);
                materials[i].SetVector(neighborId5, n5);
                materials[i].SetVector(neighborId6, n6);
            }

            for (int i = 0; i < size; i++) {
                ApplyCachedVariables(materials[i]);
            }

            /*
           if (data.highlightColorsDirty || force) {
               material.SetVectorArray(highlightColorsArrayId, data.highlightColors);
               data.highlightColorsDirty = false;
           }

           */
        }

        public override void EnableKeyword(string keyword, bool enable)
        {
            return;
        }

        public override void UpdateMesh(Mesh newMesh)
        {
            return;
        }

        public override void SetAlphaParams(Vector4 alphaParams)
        {
            for (int i = 0; i < size; i++)
                materials[i].SetVector(alphaSettingsId, alphaParams);
        }

        public override void SetAOParams(Vector4 aoParams)
        {
            for (int i = 0; i < size; i++)
                materials[i].SetVector(aoSettingsId, aoParams);
        }

        public override void SetBillboardExtrudeFactor(float billboardScaleFactor)
        {
            for (int i = 0; i < size; i++)
                materials[i].SetFloat(billboardExtrudeFactorId, billboardScaleFactor);
        }

        public override void SetHoverColor(Color hoverColor)
        {
            for (int i = 0; i < size; i++)
                materials[i].SetColor(hoverPropertyId, hoverColor);
        }

        public override void SetIndexingParams(MeshStorage.MeshType type, int objects, bool reverse = false)
        {
            for (int i = 0; i < size; i++)
                MelGraphicsSettings.SetIndexingForBatch(materials[i], type, objects, reverse);
        }

        public override void SetShaderParams(Vector4 shaderParams)
        {
            for (int i = 0; i < size; i++)
                materials[i].SetVector(settingsId, shaderParams);
        }

        public override void UpdateEditorCamera(Vector4 cameraLocalPos, Vector4 cameraForward)
        {
            for (int i = 0; i < size; i++) {
                materials[i].SetVector(localCameraPosId, cameraLocalPos);
                materials[i].SetVector(localCameraForwardId, cameraForward.normalized);
            }
        }
    }

    #endregion

    [HideInInspector()]
    public BallInfo[] balls = null;

    private BallsRendererSettings settings = null;
    
	// TODO: Replace with list of buckets and datas.
	private BallsBucketBase bucket = null;
    private BallsData bucketData = null;

    // TODO: Legacy accessor for to-be-removed methods.
    //private BallsBatchBucket batchBucket = null;

    private bool isInit = false;
	
    // TODO: Delete
    public void CopyPositionsArrayTo(System.Array destination)
    {
        System.Array.Copy(bucketData.positions, destination, bucketData.positions.Length);
    }

    public void SetActive(bool active)
    {
        this.gameObject.SetActive(active);
    }

    public void UpdateMesh(Mesh newMesh)
    {
        bucket.UpdateMesh(newMesh);
    }

#region Initialization methods

    public void Init(int size, BallsRendererSettings _settings = null)
	{
		settings = (_settings == null) ? BallsRendererSettings.Batch : _settings;

		bucketData = new BallsData(size);
        balls = new BallInfo[size];
        for (int i = 0; i < size; i++)
            balls[i] = new BallInfo(bucketData, i);

        // Switch-case settings?
        if (settings.mode == Mode.BATCH) {
            bucket = new BallsBatchBucket(settings, this.transform);
        } else if (settings.mode == Mode.SINGLE) {
            bucket = new BallsListBucket(settings, size, this.transform);
        }

        isInit = true;
	}

    public static BallsRenderer Create(int maxSize, Transform parent = null, BallsRenderer.Mode mode = BallsRenderer.Mode.BATCH)
    {
        BallsRendererSettings settings = BallsRendererSettings.Get(mode);

        return Create(maxSize, parent, settings);
    }

    private static BallsRenderer Create(int maxSize, Transform parent = null, BallsRendererSettings _settings = null)
	{
		_settings = _settings == null ? BallsRendererSettings.Batch : _settings;

		GameObject go = Instantiate(_settings.ballsRendererPrototype, parent);
		BallsRenderer result = go.GetComponent<BallsRenderer>();

        result.Init(maxSize, _settings);

        return result;
	}

    #endregion

    #region Neighbors methods

    private bool CheckNeighbors(int host, int neighbor, float distance)
    {
        // Early exit
        if (distance >= bucketData._neighborDistances[host, NEIGHBORS_NUM - 1])
            return false;

        for (int i = 0; i < NEIGHBORS_NUM; i++)
            if (bucketData._neighborIndexes[host, i] == neighbor)
                return false;

        // Find neighbor place
        int replaceIndex = NEIGHBORS_NUM - 1;
        while ((replaceIndex > 0) && (bucketData._neighborDistances[host, replaceIndex - 1] > distance))
            replaceIndex--;

        // Shift array
        for (int r = NEIGHBORS_NUM - 1; r > replaceIndex; r--) {
            bucketData._neighborDistances[host, r] = bucketData._neighborDistances[host, r - 1];
            bucketData._neighborIndexes[host, r] = bucketData._neighborIndexes[host, r - 1];
        }

        // Overwrite
        bucketData._neighborDistances[host, replaceIndex] = distance;
        bucketData._neighborIndexes[host, replaceIndex] = neighbor;
        bucketData._neighborsDirty = true;

        return true;
    }

    // TODO: Move to internal handling.
    public bool UpdateNeighbors(int atomIndex)
    {
        if (!balls[atomIndex].enabled)
            return false;

        UnityEngine.Profiling.Profiler.BeginSample("Particle Neighbors Update");

        for (int i = 0; i < NEIGHBORS_NUM; i++) {
            bucketData._neighborDistances[atomIndex, i] = float.PositiveInfinity;
            bucketData._neighborIndexes[atomIndex, i] = bucketData.size - 1;
        }

        for (int i = 0; i < bucketData.size; i++) {
            if (i == atomIndex || !balls[i].enabled)
                continue;

            float distance = (balls[i].localPosition - balls[atomIndex].localPosition).sqrMagnitude;

            CheckNeighbors(atomIndex, i, distance);
            CheckNeighbors(i, atomIndex, distance);
        }

        UnityEngine.Profiling.Profiler.EndSample();

        bucketData._neighborsDirty = true;

        return true;
    }

#endregion

#region Sorting methods

    public void AssignFreeSortingIndex(int atomIndex)
    {
        int i = bucketData.size - 1;
        while (bucketData._atomsNeighbors2[i].w != bucketData.FREE_INDX) i--;

        bucketData._atomsNeighbors2[i].w = atomIndex + INDEX_OFFSET;
    }

    // TODO: Internalize
    public bool SubsortAtoms()
    {
        return bucketData.SubsortAtoms(bucket.localCameraPosition);
    }

    public float GetLargestBallCoverage()
    {
        float maxCoverage = 0f;

        for (int i = 0; i < bucketData.size; i++) {
            if (!balls[i].enabled)
                continue;

            Vector3 position = balls[i].localPosition;
            float radius = balls[i].radius;

            var distance = (position - (Vector3)bucket.localCameraPosition).sqrMagnitude;
            var coverage = radius * radius / distance;

            maxCoverage = Mathf.Max(maxCoverage, coverage);
        }

        return maxCoverage;
    }

#endregion

#region Unity callbacks

    protected void OnApplicationFocus(bool focus)
    {
        if (!focus)
            return;

        bucket.SetBillboardExtrudeFactor(billboardScaleFactor);

        bucket.Update();
        bucket.ApplyData(bucketData, true);
    }

    public void LateUpdate()
	{
		if (!isInit)
			return;
        
        bucket.Update();
        bucket.ApplyData(bucketData, false);
	}

    #endregion

#region Material properties

    private Color hoverColor = Color.white;
    public Color HoverColor {
        get { return hoverColor; }
        set {
            if (hoverColor != value) {
                hoverColor = value;
                bucket.SetHoverColor(hoverColor);
            }
        }
    }
    
    private Vector4 aoParams = Vector4.zero;
    public void SetAOParams(Vector4 newSettings)
    {
        if (aoParams != newSettings) {
            aoParams = newSettings;
            bucket.SetAOParams(aoParams);
        }
    }

    private Vector4 shaderParams = Vector4.zero;
    public void SetShaderParams(float animatedHighlight, float animatedHover, float labelScale, float atomsScale)
    {
        Vector4 newSettings = new Vector4(animatedHighlight, animatedHover, 1f / labelScale, atomsScale);

        if (shaderParams != newSettings) {
            shaderParams = newSettings;
            bucket.SetShaderParams(shaderParams);
        }
    }

    private Vector4 alphaParams = Vector4.zero;
    public void SetAlphaParams(Vector4 newSettings)
    {
        if (alphaParams != newSettings) {
            alphaParams = newSettings;
            bucket.SetAlphaParams(alphaParams);
        }
    }

    public void SetIndexingForBatch(MeshStorage.MeshType type, int objectsNum, bool reverse = false)
    {
        bucket.SetIndexingParams(type, objectsNum, reverse);
    }

    private bool _aoEnabled = true;
    public void SetAOEnabled(bool enable)
    {
        if (enable == _aoEnabled)
            return;

        bucket.EnableKeyword(disableAoKeyword, !enable);

        _aoEnabled = enable;
    }

    private bool _volumetricNormalmapEnabled = true;
    public void SetVolumetricNormalmapEnabled(bool enable)
    {
        if (enable != _volumetricNormalmapEnabled)
            return;

        bucket.EnableKeyword(VolumetricMaterialConsts.VolumetricNormalmap, enable);

        _volumetricNormalmapEnabled = enable;
    }

    #endregion

    #region Editor helpers

#if UNITY_EDITOR

    public void UpdateEditorCamera(Vector4 cameraLocalPos, Vector4 cameraForward)
    {
        bucket.UpdateEditorCamera(cameraLocalPos, cameraForward);
    }

#endif

#endregion

}
