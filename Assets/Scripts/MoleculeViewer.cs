using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoleculeViewer : MonoBehaviour {

    private ParticleInfo _cachedParticle = null;
    private ParticleRenderer particleRenderer = null;

    // Use this for initialization
    void Start()
    {
        MelDB.Init();

        JSImports.LoadComplete_Callback();
    }

    // Caches find request.
    private ParticleInfo FindParticleCAS(string cas)
    {
        Debug.Log("FindParticleCAS " + cas);

        uint casNumber = MelDB.ParseCASNumber(cas);

        if (casNumber == 0)
            return null;

        if ((_cachedParticle != null) && (_cachedParticle.CASes.Contains(casNumber)))
            return _cachedParticle;

        _cachedParticle = MelDB.Particles.Find(p => p.CASes.Contains(casNumber));

        return _cachedParticle;
    }

    public void CanShowCAS(string cas)
    {
        Debug.Log("CanShowCAS " + cas);

        bool canShow = (FindParticleCAS(cas) != null);

        JSImports.CanShowCAS_Callback(cas, canShow);
    }
    
    public void ShowParticle(ParticleInfo particleInfo)
    {
        Debug.Log("ShowParticle");

        if (particleRenderer != null)
            Destroy(particleRenderer.gameObject);

        if (particleInfo != null) {
            particleRenderer = ParticleRenderer.Instantiate(particleInfo, null, ParticleRenderer.Mode.SpaceFilling);
            particleRenderer.transform.position = Vector3.zero;
        }
    }

    public void ShowCAS(string cas)
    {
        Debug.Log("ShowCAS " + cas);

        ParticleInfo particleInfo = FindParticleCAS(cas);

        ShowParticle(particleInfo);

        JSImports.ShowCAS_Callback(cas, particleInfo != null);
    }

    public void Update()
    {
        Quaternion rot = Quaternion.AngleAxis(30f * Time.deltaTime, Vector3.up);

        if (particleRenderer != null) {
            particleRenderer.transform.localRotation *= rot;
        }
    }
}
