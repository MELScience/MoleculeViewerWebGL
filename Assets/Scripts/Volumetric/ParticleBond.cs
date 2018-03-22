using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class ParticleBond : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    // TODO: update bond shader to make highlighting consistent with particle atoms
    // TODO: add ability to change colliders scale (use capsule colliders?)
    // TODO: separate hovering and highlitghting like in particle atoms

    private static int hoverPropertyId = Shader.PropertyToID("_HoverIntensity");

    public BondInfo.BondType type;
    public int atomIndex1 { get; private set; }
    public int atomIndex2 { get; private set; }
    [SerializeField] private MeshRenderer _renderer;
    [SerializeField] private Collider _collider;
    [SerializeField] private Transform _transform;

    private bool _visible = true;
    public bool visible
    {
        get { return _visible; }
        set { _visible = value; _renderer.enabled = value; if (_hasCollider) _collider.enabled = _interanctable & _visible; }
    }

    private bool _interanctable = true;
    public bool interactable
    {
        get { return _interanctable; }
        set { _interanctable = value; if (_hasCollider) _collider.enabled = _interanctable & _visible; }
    }

    private bool _hasCollider = true;

    private float _highlighting;
    public float highlighting
    {
        get { return _highlighting; }
        set
        {
            _highlighting = value;
            if (_highlighting < 0.01f)
                _renderer.SetPropertyBlock(null);
            else
            {
                ParticleRenderer.bondsMaterialPropertyBlock.SetFloat(hoverPropertyId, _highlighting);
                _renderer.SetPropertyBlock(ParticleRenderer.bondsMaterialPropertyBlock);
            }
        }
    }

    /// <summary>
    /// false if the bond was removed from particle
    /// </summary>
    public bool exist { get { return atomIndex1 >= 0; } }

    public bool pointerHovered { get; private set; }

    public Vector3 localScale
    {
        get { return _transform.localScale; }
        set { _transform.localScale = value; }
    }

    private ParticleRenderer _particle;

    #region interaction, clicking

    public void OnPointerEnter(PointerEventData eventData)
    {
        pointerHovered = true;
        if (_highlightingCoroutine == null)
            _highlightingCoroutine = StartCoroutine(HighlightingCoroutine());
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        pointerHovered = false;
    }

    private Coroutine _highlightingCoroutine;
    private IEnumerator HighlightingCoroutine()
    {
        for (;;)
        {
            float h = highlighting;
            if (pointerHovered)
            {
                h += _particle.hoverHighlightingTime * Time.deltaTime;
                if (h > 1f) h = 1f;
            }
            else
            {
                h -= _particle.hoverHighlightingTime * Time.deltaTime;
                if (h < 0f)
                {
                    h = 0f;
                    highlighting = h;
                    _highlightingCoroutine = null;
                    yield break;
                }
            }
            highlighting = h;
            yield return null;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        _particle.HandleBondClick(this);
    }

    protected void OnDisable()
    {
        _highlightingCoroutine = null;
        highlighting = 0f;
        pointerHovered = false;
    }

    public void DestroyCollider()
    {
        if (!_hasCollider)
            return;
        _hasCollider = false;
        Destroy(_collider);
    }

    #endregion

    #region ParticleRenderer interactions

    /// <summary>
    /// Should be used only by ParticleRenderer!
    /// </summary>
    public void SetMaterial(Material mat) { _renderer.material = mat; }
    /// <summary>
    /// Should be used only by ParticleRenderer!
    /// </summary>
    public void Init(int indx1, int indx2, ParticleRenderer particle, bool visible)
    {
        _particle = particle;
        this.visible = visible;
        atomIndex1 = indx1;
        atomIndex2 = indx2;
    }
    /// <summary>
    /// Should be used only by ParticleRenderer!
    /// </summary>
    public void Reset()
    {
        visible = false;
        atomIndex1 = -1;
        atomIndex2 = -2;
        pointerHovered = false;
        _highlightingCoroutine = null;
    }
    /// <summary>
    /// Should be used only by ParticleRenderer!
    /// </summary>
    public void SetLocalPositionAndRotation(Vector3 position, Quaternion rotation)
    {
        _transform.localPosition = position;
        _transform.localRotation = rotation;
    }
    #endregion
}
