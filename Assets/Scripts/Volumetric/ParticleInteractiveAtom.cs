using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class ParticleInteractiveAtom : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    // TODO: add ability to change colliders scale

    public Element type { get; private set; }
    private bool _locked;
    /// <summary>
    /// If true - atom will not be animated by the molecule. You can animate it manually
    /// </summary>
    public bool locked
    {
        get { return _locked; }
        set {
            if (_locked == value) return;
            _locked = value;
            _particle.CountLockedAtom(value);
            if (!value)
                interactable = _particle.atomsAreInteractable;
            if (customParentTransform & !_locked)
                _transform.SetParent(_particle.atomsHolderTransform, true);
        }
    }
    public float radius
    {
        get { return _collider.radius; }
        set
        {
            if (_locked)
                _collider.radius = value;
#if UNITY_EDITOR
            else
                Debug.LogWarningFormat("Trying to set radius for non-locked {0}, this does not have any sense", GetType().Name);
#endif
        }
    }
    /// <summary>
    /// world-space position of the atom
    /// </summary>
    public Vector3 position
    {
        get { return _transform.position; }
        set { if (_locked) _transform.position = value; }
    }
    public Vector3 localPosition
    {
        get { return _transform.localPosition; }
        set { if (_locked) _transform.localPosition = value; }
    }
    public bool interactable
    {
        get { return _collider.enabled; }
        set { _collider.enabled = value; }
    }
    public int index { get; private set; }
    /// <summary>
    /// false if the atom was removed from particle
    /// </summary>
    public bool exist { get; private set; }
    public bool pointerHovered { get; private set; }
    /// <summary>
    /// Proper local position within particle. Use it in case the atom is locked and you want to animate it back to non-locked state
    /// </summary>
    public Vector3 positionInParticle { get; private set; }
    public float unscaledRadiusInParticle { get; private set; }
    /// <summary>
    /// Proper radius within particle. Use it in case the atom is locked and you want to animate it back to non-locked state
    /// </summary>
    public float radiusInParticle {
        get { return unscaledRadiusInParticle * _particle.currentAppearance.atomsScale; }
    }
    public float highlightingIntensity = 0f;
    
    public void SetHighlightingColorAndIntensity(Color color, float intensity)
    {
        _particle.SetAtomHighlightingColor(this, color);
        highlightingIntensity = intensity;
        _highlightColorAlpha = color.a;
    }
    /// <summary>
    /// set it to negative if you want automatic mode. set (0, 1) value to control it manually
    /// </summary>
    public float hoverIntensity = -1f;

    private ParticleRenderer _particle;
    [SerializeField] private SphereCollider _collider;
    public Transform _transform;
    private float _lastAtomScale;
    private float _highlightColorAlpha = 1f;
    private bool customParentTransform;

    public void Blink(Color color, float duration = -1f)
    {
        if (duration <= 0f)
            duration = _particle.settings.defaultBlinkDuration;
        _highlightColorAlpha = color.a;
        StartCoroutine(BlinkAnimation(color, duration));
    }
    private IEnumerator BlinkAnimation(Color color, float duration)
    {
        var ballInfo = _particle.ballsRenderer.balls[index];
        ballInfo.highlightColor = color;

        var curve = _particle.settings.blinkCurve;
        float progress = 0f;
        while (progress < 1f)
        {
            progress += Time.deltaTime / duration;

            float intensityValue = _highlightColorAlpha * curve.Evaluate(progress) + 0.001f;
            ballInfo.highlightIntensity = intensityValue;

            yield return null;
        }

        ballInfo.highlightIntensity = 0f;
        highlightingIntensity = 0f;
    }

    public void SetParent(Transform newParent, bool worldPositionStays = true)
    {
        if (_locked)
        {
            _transform.SetParent(newParent, worldPositionStays);
            customParentTransform = newParent != _particle.atomsHolderTransform;
        }
#if UNITY_EDITOR
        else
            Debug.LogWarningFormat("Trying to set parent for non-locked {0}, this does not have any sense", GetType().Name);
#endif
    }

    #region interaction, clicking

    public void OnPointerEnter(PointerEventData eventData){ pointerHovered = true; }
    public void OnPointerExit(PointerEventData eventData) { pointerHovered = false; }
    public void OnPointerClick(PointerEventData eventData) { _particle.HandleAtomClick(this); }

    #endregion

    #region ParticleRenderer interactions
    /// <summary>
    /// Should be used only by ParticleRenderer!
    /// </summary>
    public void Init(ParticleRenderer renderer, Vector3 position, float radius, float scale, int index, Element element, bool interactable, float highlightAlpha)
    {
        this.type = element;
#if UNITY_EDITOR
        name = element.ToString();
#endif
        _particle = renderer;
        this.index = index;
        positionInParticle = position;
        _transform.localPosition = position;
        unscaledRadiusInParticle = radius;
        _collider.radius = radius * scale;
        if (interactable)
            this.interactable = true;
        exist = true;
        _highlightColorAlpha = highlightAlpha;
    }

    /// <summary>
    /// Should be used only by ParticleRenderer!
    /// </summary>
    public void Reset(float highlightAlpha)
    {
#if UNITY_EDITOR
        name = "Removed Atom";
#endif
        exist = false;
        locked = false;
        interactable = false;
        index = -1;
        pointerHovered = false;
        _highlightColorAlpha = highlightAlpha;
        unscaledRadiusInParticle = 0f;
    }

    /// <summary>
    /// Should be used only by ParticleRenderer!
    /// settings.x - scale
    /// settings.y - positive scale inverse
    /// settings.z - dt / highlightingTime
    /// settings.w = isAnimating ? 1f : -1f
    /// </summary>
    public bool UpdateState(ref Vector4 posSize, ref Vector4 highlighting, ref Vector4 color, ref Vector4 settings)
    {
        if (settings.w > 0f | !_locked)
        {
            positionInParticle = posSize;
            unscaledRadiusInParticle = Mathf.Max(0f, posSize.w);
            if (!_locked)
            {
                _transform.localPosition = posSize;
                _collider.radius = unscaledRadiusInParticle * settings.x;
            }
        }
        if (_locked)
        {
            posSize = customParentTransform ? _particle.atomsHolderTransform.InverseTransformPoint(_transform.position) : _transform.localPosition;
            posSize.w = _collider.radius * settings.y;
        }
        // update hovered atom animation
        float hoverTarget = hoverIntensity < 0f ? pointerHovered ? 1f : 0f : hoverIntensity;
        if (Mathf.Abs(hoverTarget - highlighting.z) < settings.z)
            highlighting.z = hoverTarget;
        else
            highlighting.z = Mathf.MoveTowards(highlighting.z, hoverTarget, settings.z);
        // update highlighting
        var target = highlightingIntensity * _highlightColorAlpha;
        if (color.w == target)
            return false;
        var speed = _highlightColorAlpha * settings.z;
        if (color.w < target - speed)
            color.w += speed;
        else if (color.w > target + speed)
            color.w -= speed;
        else
            color.w = target;
        return true;
    }

    /// <summary>
    /// Should be used only by ParticleRenderer!
    /// settings.x - scale
    /// settings.y - positive scale inverse
    /// settings.z - dt / highlightingTime
    /// settings.w = isAnimating ? 1f : -1f
    /// </summary>
    public bool UpdateState(BallsRenderer.BallInfo ballInfo, ref Vector4 settings)
    {
        if (settings.w > 0f | !_locked) {
            positionInParticle = ballInfo.localPosition;
            unscaledRadiusInParticle = Mathf.Max(0f, ballInfo.radius);
            if (!_locked) {
                _transform.localPosition = ballInfo.localPosition;
                _collider.radius = unscaledRadiusInParticle * settings.x;
            }
        }

        if (_locked) {
            ballInfo.localPosition = customParentTransform ? _particle.atomsHolderTransform.InverseTransformPoint(_transform.position) : _transform.localPosition;
            ballInfo.radius = _collider.radius * settings.y;
        }

        // update hovered atom animation
        float hoverTarget = hoverIntensity < 0f ? (pointerHovered ? 1f : 0f) : hoverIntensity;
        ballInfo.hoverIntensity = Mathf.MoveTowards(ballInfo.hoverIntensity, hoverTarget, settings.z);

        // update highlighting
        ballInfo.highlightColor = ballInfo.highlightColor.SetAlpha(Mathf.MoveTowards(ballInfo.highlightColor.a, highlightingIntensity * _highlightColorAlpha, _highlightColorAlpha * settings.z));
        
        return true;
    }
    #endregion
}
