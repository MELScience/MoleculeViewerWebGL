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

        StartCoroutine(CameraFov_Coroutine());

        JSImports.LoadComplete_Callback();
    }

    // Caches find request.
    private ParticleInfo FindParticleCAS(string cas)
    {
        Debug.Log("FindParticleCAS " + cas);

        if (string.IsNullOrEmpty(cas))
            return null;

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

    
    private IEnumerator CameraFov_Coroutine()
    {
        Camera camera = Camera.main;
        float initialFov = camera.fieldOfView;

        int screenWidth = Screen.width;
        int screenHeight = Screen.height;

        while (true) {
            if ((screenWidth != Screen.width) || (screenHeight != Screen.height)) {
                screenWidth = Screen.width;
                screenHeight = Screen.height;

                float coeff = Mathf.Max(1f, screenHeight / (float)screenWidth);
                float newFov = 2 * Mathf.Rad2Deg * Mathf.Atan(coeff * Mathf.Tan(0.5f * Mathf.Deg2Rad * initialFov));

                Debug.Log(coeff);
                Debug.Log(Mathf.PI * initialFov / 360f);
                Debug.Log(newFov);

                camera.fieldOfView = newFov;
            }

            yield return null;
        }

        yield break;
    }
    

    public void Update()
    {
        Quaternion rot = Quaternion.AngleAxis(30f * Time.deltaTime, Vector3.up);

        if (particleRenderer != null) {
            particleRenderer.transform.localRotation *= rot;
        }
    }
}
