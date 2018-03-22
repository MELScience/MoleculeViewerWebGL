using System;
using System.Collections;
using UnityEngine;

public class AnimatedProperty<T>
{
    public event Action<T> transitionFinished;

    private T _current;
    public T value
    {
        get { return _current; }
        set { Set(value); }
    }
    public T previous { get; private set; }

    private MonoBehaviour _mb;
    private AnimationCurve curve;
    private Func<T, T, float, T> lerp;
    private Action<T, float> update;
    private Func<T, T, bool> areEqual;
    private Coroutine transition;
    private bool dirty;
    public float progress { get; private set; }

    public AnimatedProperty(MonoBehaviour mb, T value, AnimationCurve curve, Func<T, T, float, T> lerp, Action<T, float> update, Func<T, T, bool> areEqual)
    {
        _mb = mb;
        _current = value;
        this.lerp = lerp;
        this.update = update;
        this.curve = curve;
        this.areEqual = areEqual;
        progress = 1f;
    }

    public void Set(T value, float duration = 0f)
    {
        if (areEqual(_current, value))
            return;
        if (transition != null)
        {
            _mb.StopCoroutine(transition);
            transition = null;
        }
        previous = _current;
        if (duration <= 0f)
        {
            progress = 1f;
            _current = lerp(value, value, 1f);
            dirty = true;
            if (transitionFinished != null)
                transitionFinished(_current);
        }
        else
            transition = _mb.StartCoroutine(SmoothTransition(duration, value));
    }

    private IEnumerator SmoothTransition(float duration, T target)
    {
        progress = 0f;
        while (true)
        {
            yield return null;
            progress += Time.deltaTime / duration;
            if (progress >= 1f)
                progress = 1f;
            dirty = true;
            float lerpFactor = curve.Evaluate(progress);
            _current = lerp(previous, target, lerpFactor);
            if (progress == 1f)
                break;
        }
        if (transitionFinished != null)
            transitionFinished(_current);
        transition = null;
    }

    public void Update()
    {
        if (!dirty)
            return;
        update(_current, progress);
        dirty = false;
    }
}

[Serializable]
public class LightingSettings
{
    public Light light;

    public bool overrideSkybox = true;
    public Cubemap skyboxTexture = null;
    [ColorUsage(false)]
    public Color skyColor = Color.black;

    public bool overrideFog = true;
    [ColorUsage(false)]
    public Color fogColor = Color.black;
    public float fogDensity = 0f;
}

public class LightController : MonoBehaviour
{
    public static LightController Instance;

    [SerializeField] private Transform lightTransform;
    [SerializeField] private Light directionalLight;
    public Light mainLight { get { return directionalLight; } }

    [Range(0.8f, 0.999f)]
    [SerializeField] private float fogIntensityOnFarPlane = 0.99f;
    [SerializeField] private float maxCameraFarPlane = 70f;

    [SerializeField] private AnimationCurve smoothCurve;

    [SerializeField] private Material tintedSkyboxMaterial;
    [SerializeField] private Material mixedSkyboxMaterial;

    private float _minVisibilityLog;
    private bool _fog;
    private Material _activeMaterial;
    private int _textureId;
    private int _texture2Id;
    private int _tintAmountId;
    private int _tintColorId;
    private Camera mainCamera;
    
    public AnimatedProperty<Quaternion> lightRotation;
    public AnimatedProperty<Color> lightColor;
    public AnimatedProperty<float> lightIntensity;
    
    public float fogDistance
    {
        get
        {
            return _minVisibilityLog / fogDensity.value;
        }
        set
        {
            if (value < 0f || value == float.PositiveInfinity)
                fogDensity.value = 0f;
            else
                fogDensity.value = _minVisibilityLog / value;
        }
    }
    public AnimatedProperty<float> fogDensity;
    public AnimatedProperty<Color> fogColor;

    public AnimatedProperty<Texture> skybox;
    public AnimatedProperty<Color> skyColor;

    protected void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Debug.LogErrorFormat("Duplicated {0} found in object {1}", GetType().Name, name);
            Destroy(this);
            return;
        }

        _tintAmountId = Shader.PropertyToID("_TintAmount");
        _textureId = Shader.PropertyToID("_MainTex");
        _texture2Id = Shader.PropertyToID("_TintTex");
        _tintColorId = Shader.PropertyToID("_TintColor");

        tintedSkyboxMaterial = Instantiate(tintedSkyboxMaterial); // to avoid material modifications in editor
        mixedSkyboxMaterial = Instantiate(mixedSkyboxMaterial); // to avoid material modifications in editor
        
        lightRotation = new AnimatedProperty<Quaternion>(this, lightTransform.localRotation, smoothCurve, Quaternion.Lerp, (q, p) => lightTransform.localRotation = q, (a, b) => a == b);
        lightColor = new AnimatedProperty<Color>(this, directionalLight.color, smoothCurve, Color.Lerp, (c, p) => directionalLight.color = c, (a, b) => a == b);
        lightIntensity = new AnimatedProperty<float>(this, directionalLight.intensity, smoothCurve, Mathf.Lerp, (i, p) => directionalLight.intensity = i, (a, b) => Mathf.Abs(a - b) < Mathf.Epsilon);
        fogDensity = new AnimatedProperty<float>(this, 0f, smoothCurve, Mathf.Lerp, (f, p) => UpdateFogAndCamera(f), (a, b) => Mathf.Abs(a - b) < Mathf.Epsilon);
        fogColor = new AnimatedProperty<Color>(this, Color.black, smoothCurve, Color.Lerp, (c, p) => RenderSettings.fogColor = c, (a, b) => a == b);
        skybox = new AnimatedProperty<Texture>(this, null, smoothCurve, LerpSkyboxes, UpdateSkybox,
            (a, b) => a == null ? b == null : a.Equals(b));
        mainCamera = Camera.main;
        skyColor = new AnimatedProperty<Color>(this, Color.black, smoothCurve, Color.Lerp, (c, p) => {
            mainCamera.backgroundColor = c;
            tintedSkyboxMaterial.SetColor(_tintColorId, c);
        }, (a, b) => a == b);

        _minVisibilityLog = Mathf.Sqrt(-Mathf.Log(1f - fogIntensityOnFarPlane));
        RenderSettings.fog = false;
        _fog = false;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
    }

    public void ApplyLightOnly(Light light, float duration = 0f)
    {
        if (light != null)
        {
            light.enabled = false;
            lightRotation.Set(light.transform.localRotation, duration);
            lightColor.Set(light.color, duration);
            lightIntensity.Set(light.intensity, duration);
        }
    }
    public void ApplyLightingSettings(LightingSettings settings, float duration = 0f)
    {
        if (settings.overrideFog)
        {
            fogColor.Set(settings.fogColor, duration);
            fogDensity.Set(settings.fogDensity, duration);
        }
        ApplyLightOnly(settings.light, duration);
        if (settings.overrideSkybox)
        {
            skybox.Set(settings.skyboxTexture, duration);
            skyColor.Set(settings.skyColor, duration);
        }
    }

    public void SetFogDistance(float value, float duration = -1)
    {
        if (duration <= 0f)
            fogDensity.value = value;
        else
        {
            if (value < 0f || value == float.PositiveInfinity)
                fogDensity.Set(0f, duration);
            else
                fogDensity.Set(_minVisibilityLog / value, duration);
        }
    }

    protected void LateUpdate()
    {
        lightRotation.Update();
        lightColor.Update();
        lightIntensity.Update();
        fogDensity.Update();
        fogColor.Update();
        skybox.Update();
        skyColor.Update();
    }

    private void UpdateFogAndCamera(float density)
    {
        // enable/disable fog if required
        {
            bool shouldFog = density > 0.00001f;
            if (shouldFog != _fog)
            {
                _fog = shouldFog;
                RenderSettings.fog = shouldFog;
            }
        }
        if (!_fog)
            return;
        // update density
        RenderSettings.fogDensity = density;
        float distance = _minVisibilityLog / density;

        //RenderSettings.fogDensity = Mathf.Sqrt(-Mathf.Log(distantFogFactor)) / maxVisibleDistance;

        mainCamera.farClipPlane = Mathf.Max(distance + 0.1f, maxCameraFarPlane);
    }

    private bool _disableSkyboxRendering = false;
    public bool disableSkyboxRendering
    {
        get { return _disableSkyboxRendering; }
        set
        {
            if (_disableSkyboxRendering == value)
                return;
            if (value)
            {
                mainCamera.clearFlags = CameraClearFlags.Color;
                RenderSettings.skybox = null;
            }
            else
            {
                mainCamera.clearFlags = _activeMaterial == null ? CameraClearFlags.Color : CameraClearFlags.Skybox;
                RenderSettings.skybox = _activeMaterial;
            }
            _disableSkyboxRendering = value;
        }
    }

    private int _lastSkyboxMode = -1;
    private Texture LerpSkyboxes(Texture a, Texture b, float l)
    {
        int requiredMode = b == null
            ? a == null ? 0 : 1
            : a == null ? 2 : 3;
        if (requiredMode != _lastSkyboxMode)
        {
            switch (requiredMode)
            {
                case 0:
                    // no skybox, use ambient color transition
                    _activeMaterial = null;
                    mainCamera.clearFlags = CameraClearFlags.Color;
                    break;
                case 1:
                    // transition from skybox to color
                    _activeMaterial.SetTexture(_textureId, a);
                    mainCamera.clearFlags = CameraClearFlags.Skybox;
                    break;
                case 2:
                    // transition from color to skybox
                    _activeMaterial = tintedSkyboxMaterial;
                    _activeMaterial.SetTexture(_textureId, b);
                    mainCamera.clearFlags = CameraClearFlags.Skybox;
                    break;
                case 3:
                    // transition from one skybox to another
                    _activeMaterial = mixedSkyboxMaterial;
                    _activeMaterial.SetTexture(_textureId, a);
                    _activeMaterial.SetTexture(_texture2Id, b);
                    mainCamera.clearFlags = CameraClearFlags.Skybox;
                    break;
            }
            if (disableSkyboxRendering)
            {
                mainCamera.clearFlags = CameraClearFlags.Color;
                RenderSettings.skybox = null;
            }
            else
            {
                mainCamera.clearFlags = _activeMaterial == null ? CameraClearFlags.Color : CameraClearFlags.Skybox;
                RenderSettings.skybox = _activeMaterial;
            }
            _lastSkyboxMode = requiredMode;
        }
        return l > 0.5f ? b : a; // never used actually, maybe in future
    }
    private void UpdateSkybox(Texture c, float progress)
    {
        if (progress >= 1f)
        {
            if (c == null)
                // disable skybox
                mainCamera.clearFlags = CameraClearFlags.Color;
            else
            {
                // switch material to less performance-heavy one
                RenderSettings.skybox = tintedSkyboxMaterial;
                _activeMaterial = tintedSkyboxMaterial;
                _activeMaterial.SetTexture(_textureId, c);
                _activeMaterial.SetFloat(_tintAmountId, 0f);
            }
            return;
        }
        if (_activeMaterial != null)
            _activeMaterial.SetFloat(_tintAmountId, _lastSkyboxMode != 2 ? progress : 1 - progress);
    }
}
