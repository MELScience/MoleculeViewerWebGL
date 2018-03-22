using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoleculeTestScript : MonoBehaviour
{

#if UNITY_EDITOR

    public IEnumerator Start()
    {
        MoleculeViewer viewer = GameObject.FindObjectOfType<MoleculeViewer>();

        yield return null;

        while (true) {
            ParticleInfo randomParticle = MelDB.Particles[Random.Range(0, MelDB.Particles.Count)];

            viewer.ShowParticle(randomParticle);

            yield return new WaitForSeconds(5f);
        }
        
        yield break;
    }

#endif

}
