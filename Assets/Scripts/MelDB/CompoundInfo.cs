using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.IO;
using System;

[Serializable]
public class CompoundInfo
{
    public ulong id;

    public string name = "unknown";

    [Serializable]
    public class CompoundInfoData
    {
        public string chemicalFormula = "";
        public List<string> tags = new List<string>();

        public List<ulong> particleIds = new List<ulong>();

        //List for manual layout
        public List<Vector2> particlesLayout = new List<Vector2>();
    }
    
    public CompoundInfo() { id = MelDB.GetRandomId(); }

    [XmlIgnore]
    [NonSerialized]
    public CompoundInfoData data = null;

    [XmlIgnore]
    public string ChemicalFormula { get { Init(); return data.chemicalFormula; } }
    [XmlIgnore]
    public List<string> Tags { get { Init(); return data.tags; } }
    [XmlIgnore]
    public List<Vector2> ParticlesLayout { get { Init(); return data.particlesLayout; } }
    [XmlIgnore]
    [NonSerialized]
    private List<ParticleInfo> particles = null;
    [XmlIgnore]
    public List<ParticleInfo> Particles
    {
        get
        {
            if (particles == null) {
                particles = new List<ParticleInfo>();
                data.particleIds.ForEach(id => particles.Add(MelDB.Particles.FirstOrDefault(pi => (pi.id == id))));
            }

            return particles;
        }
    }
    
    public void Init()
    {
        if (data != null)
            return;
        // TODO
    }
}
