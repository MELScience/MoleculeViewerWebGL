using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using UnityEngine;

public class MelDBLoadRequest : CustomYieldInstruction
{
    public bool isDone;
    public override bool keepWaiting { get { return !isDone; } }
    public MelDB result;
}

public abstract class MelDBSerializer
{
    public static MonoBehaviour coroutineHolder;

    public static MelDBSerializer binaryInstance = new MelDBSerializerBinary();
    private static MelDBSerializer instance;
    public static MelDBSerializer Instance { get
        {
            if (instance == null)
            {
                instance = binaryInstance;
                /*
#if UNITY_EDITOR
                var xmlIndexPath = MelDBSerializerXML.GetIndexPath();
                if (Directory.Exists(Directory.GetParent(xmlIndexPath).FullName)
                    && File.Exists(xmlIndexPath))
                {
                    Debug.LogWarning("XML version of MelDB is in use\nKeep in mind it contains more particles then binary db, which is used in actual build");
                    instance = xmlInstance;
                }
                else
                    Debug.Log("BINARY version of MelDB is in use");
#endif
*/
            }
            return instance;
        } }
    protected class ParticleDataRequest
    {
        public bool isDone;
        public ParticleInfo.ParticleInfoData data;
    }

    protected static object _initLock = new object();

    protected static ParticleInfo.ParticleFlags _include = ParticleInfo.ParticleFlags.ForceIncludeInBuild | ParticleInfo.ParticleFlags.ShowInConstructor | ParticleInfo.ParticleFlags.ShowInExplorer;
    protected bool IncludeInBuild(ParticleInfo p)
    {
        var flags = p.flags;
        if ((flags & _include) > 0)
            return true;
        return false;
    }

    public abstract MelDB LoadIndex();
    public abstract MelDBLoadRequest LoadIndexAsync();
    public abstract void LoadParticleData(ParticleInfo p);
    public abstract IEnumerator LoadParticleDataAsync(ParticleInfo p);

#if UNITY_EDITOR
    public static MelDBSerializer xmlInstance = new MelDBSerializerXML();
    public abstract void RemoveParticleFile(ParticleInfo p);
    public abstract void Serialize(MelDB db, bool indexOnly, bool removeUnusedFiles = true,
        Func<string, int, int, bool> progress = null);
#endif
}

#if UNITY_EDITOR
public class MelDBSerializerXML : MelDBSerializer
{
    public static readonly string XmlFolder = "/Assets/MELDB_xml/";
    private static readonly string IndexFile = "MELDB_Index";

    public static string GetIndexPath() { return Directory.GetParent(Application.dataPath).FullName + XmlFolder + IndexFile + ".xml"; }
    protected string GetParticlePath(ParticleInfo p)
    { return Directory.GetParent(Application.dataPath).FullName + XmlFolder
            + (p.charge == 0 ? "Molecules/" : p.charge < 0 ? "Anions/" : "Cations/")
            + p.id + ".xml"; }

    public override MelDB LoadIndex()
    {
        lock (_initLock)
        {
            var indexPath = GetIndexPath();

            if (!File.Exists(indexPath))
                throw new Exception("file was not found: " + indexPath);
            
            XmlSerializer serializer = new XmlSerializer(typeof(MelDB));
            var reader = new FileStream(indexPath, FileMode.Open);
            var result = serializer.Deserialize(reader) as MelDB;
            reader.Close();
            return result;
        }
    }
    
    public override MelDBLoadRequest LoadIndexAsync()
    {
        lock (_initLock)
        {
            var indexPath = GetIndexPath();
            
            if (!File.Exists(indexPath))
                throw new Exception("file was not found: " + indexPath);

            var request = new MelDBLoadRequest();
            var thread = new System.Threading.Thread(() => {
                XmlSerializer serializer = new XmlSerializer(typeof(MelDB));
                var reader = new FileStream(indexPath, FileMode.Open);
                var db = serializer.Deserialize(reader) as MelDB;
                reader.Dispose();
                request.result = db;
                request.isDone = true;
            });
            thread.IsBackground = true;
            thread.Start();
            return request;
        }
    }

    private static XmlSerializer _pDataSerializer = new XmlSerializer(typeof(ParticleInfo.ParticleInfoData));
    public override void LoadParticleData(ParticleInfo p)
    {
        string path = GetParticlePath(p);
        
        if (!File.Exists(path))
            throw new Exception("file was not found: " + path);

        var reader = new FileStream(path, FileMode.Open);
        p.data = _pDataSerializer.Deserialize(reader) as ParticleInfo.ParticleInfoData;
        reader.Dispose();
    }

    public override IEnumerator LoadParticleDataAsync(ParticleInfo p)
    {
        string path = GetParticlePath(p);

        if (!File.Exists(path))
            throw new Exception("file was not found: " + path);

        var request = new ParticleDataRequest();
        
        var thread = new System.Threading.Thread(() => {
            var reader = new FileStream(path, FileMode.Open);
            request.data = _pDataSerializer.Deserialize(reader) as ParticleInfo.ParticleInfoData;
            reader.Dispose();
            request.isDone = true;
        });
        thread.IsBackground = true;
        thread.Start();

        while (!request.isDone)
            yield return null;

        p.data = request.data;
    }

    public void RemoveUnusedFiles()
    {
        string path = Directory.GetParent(GetIndexPath()).FullName;
        var files = Directory.GetFiles(path, "*.xml", SearchOption.AllDirectories);
        MelDB.Instance.Sort();
        for (int i = 0; i < files.Length; i++)
        {
            var name = Path.GetFileNameWithoutExtension(files[i]);
            ulong id;
            if (ulong.TryParse(name, out id) && MelDB.FindParticleById(id) != null)
                continue;
            if (name.Equals(IndexFile, StringComparison.OrdinalIgnoreCase))
                continue;
            try
            {
                File.Delete(files[i]);
            }
            catch (Exception e)
            {
                Debug.LogErrorFormat("Failed to remove file: {0}\n{1}", e.Message, files[i]);
            }
        }
    }

    public override void Serialize(MelDB db, bool indexOnly, bool removeUnusedFiles = true,
        Func<string, int, int, bool> progress = null)
    {
        if (progress != null && progress("XML serialization: 1/3 sorting DB", 0, 1))
            return;
        db.Sort();
        XmlSerializer melDBSerializer = new XmlSerializer(typeof(MelDB));

        if (progress != null && progress("XML serialization: 2/3 serializing index", 0, 1))
            return;
        using (FileStream fDB = File.Create(GetIndexPath()))
        {
            melDBSerializer.Serialize(fDB, db);
        }

        if (!indexOnly)
        {
            XmlSerializer particleDataSerializer = new XmlSerializer(typeof(ParticleInfo.ParticleInfoData));
            int partsCount = db.particles.Count;

            for (int i = 0; i < partsCount; i++)
            {
                var p = db.particles[i];
                if (p.data == null)
                    continue;

                if (progress != null && progress("XML serialization: 3/3 serializing particles", i, partsCount))
                    return;

                string fullPath = GetParticlePath(p);
                p.data.metaInfo = p;

                // create directory
                string dirPath = Path.GetDirectoryName(fullPath);
                if (!Directory.Exists(dirPath))
                    Directory.CreateDirectory(dirPath);

                using (FileStream f = File.Create(fullPath))
                {
                    particleDataSerializer.Serialize(f, p.data);
                }
                p.data.metaInfo = null;
            }
        }

        if (removeUnusedFiles)
            RemoveUnusedFiles();
    }

    public override void RemoveParticleFile(ParticleInfo p)
    {
        var path = GetParticlePath(p);
        File.Delete(path);
    }
}
#endif

public class MelDBSerializerBinary : MelDBSerializer
{
    public const uint BUCKETS = 256;
    public static readonly string DBPath = "/Resources/";
    public static readonly string IndexFilename = "MELDB_binary/MELDB_Index";
    public static readonly string BucketFilename = "MELDB_binary/Bucket_";
    private static readonly System.Text.Encoding encoding = new System.Text.ASCIIEncoding();

    protected int GetParticleBuckedIndex(ParticleInfo p) { return (int)(p.id % BUCKETS); }

    public override MelDB LoadIndex()
    {
        lock (_initLock)
        {
            var dataAsset = Resources.Load<TextAsset>(IndexFilename);
            if (dataAsset == null)
                throw new Exception("Failed to load from Resources: " + IndexFilename);
            return LoadIndexFromBytes(dataAsset.bytes);
        }
    }

    public override MelDBLoadRequest LoadIndexAsync()
    {
        var request = new MelDBLoadRequest();
        coroutineHolder.StartCoroutine(LoadIndexInner(request));
        return request;
    }

    private IEnumerator LoadIndexInner(MelDBLoadRequest request)
    {
        var assetRequest = Resources.LoadAsync<TextAsset>(IndexFilename);
        while (!assetRequest.isDone)
            yield return null;
        var dataAsset = assetRequest.asset as TextAsset;
        if (dataAsset == null)
            throw new Exception("Failed to load from Resources: " + IndexFilename);
        var bytes = dataAsset.bytes;
        lock (_initLock)
        {
            var thread = new System.Threading.Thread(() => {
                request.result = LoadIndexFromBytes(bytes);
                request.isDone = request.result != null;
            });
            thread.IsBackground = true;
            thread.Start();
        }
    }

    private MelDB LoadIndexFromBytes(byte[] bytes)
    {
        var stream = new MemoryStream(bytes);
        BinaryReader br = new BinaryReader(stream, encoding);
        var result = new MelDB();
        int particlesCount = br.ReadInt32();
        var particles = new List<ParticleInfo>(particlesCount);
        result.particles = particles;

        for (int i = particlesCount - 1; i >= 0; i--)
        {
            var p = new ParticleInfo();
            particles.Add(p);

            p.atomsHash = br.ReadUInt32();
            p.structureHash = br.ReadUInt32();
            p.structureHashExact = br.ReadUInt32();
            p.binaryDataLocationInBucket = br.ReadUInt16();
            p.id = br.ReadUInt64();
            p.name = br.ReadString();
            var flags = (ParticleInfo.ParticleFlags)br.ReadUInt16();
            p.flags = flags;
            if ((flags & ParticleInfo.ParticleFlags.HasChemicalFormula) > 0)
                p.chemicalFormula = br.ReadString();
            if ((flags & ParticleInfo.ParticleFlags.HasParticleCharge) > 0)
                p.charge = br.ReadSByte();
            int casesCount = br.ReadByte();
            p.CASes = new List<uint>(casesCount);
            for (int cas = 0; cas < casesCount; cas++)
                p.CASes.Add(br.ReadUInt32());
        }

        result.__sortedIds = new ushort[particlesCount];
        for (int i = 0; i < particlesCount; i++)
            result.__sortedIds[i] = br.ReadUInt16();
        //result.__sortedStructureHashes = new ushort[particlesCount];
        //for (int i = 0; i < particlesCount; i++)
        //    result.__sortedStructureHashes[i] = br.ReadUInt16();
        //result.__sortedStructureHashesExact = new ushort[particlesCount];
        //for (int i = 0; i < particlesCount; i++)
        //    result.__sortedStructureHashesExact[i] = br.ReadUInt16();
        result.__sortedAtomsHashes = new ushort[particlesCount];
        for (int i = 0; i < particlesCount; i++)
            result.__sortedAtomsHashes[i] = br.ReadUInt16();

        br.Close();
        stream.Dispose();
        return result;
    }

    public override void LoadParticleData(ParticleInfo p)
    {
        var path = BucketFilename + GetParticleBuckedIndex(p).ToString("000");
        var dataAsset = Resources.Load<TextAsset>(path);
        if (dataAsset == null)
            throw new Exception("Failed to load from Resources: " + path);
        LoadParticleFromBytes(dataAsset.bytes, p);
    }

    public override IEnumerator LoadParticleDataAsync(ParticleInfo p)
    {
        var path = BucketFilename + GetParticleBuckedIndex(p).ToString("000");
        var assetRequest = Resources.LoadAsync<TextAsset>(path);
        while (!assetRequest.isDone)
            yield return null;
        var dataAsset = assetRequest.asset as TextAsset;
        if (dataAsset == null)
            throw new Exception("Failed to load from Resources: " + path);
        var bytes = dataAsset.bytes;
        yield return null;
        LoadParticleFromBytes(bytes, p); // it's fast, no need to start separate thread
    }

    private void LoadParticleFromBytes(byte[] bytes, ParticleInfo p)
    {
        var stream = new MemoryStream(bytes);
        stream.Seek(p.binaryDataLocationInBucket, SeekOrigin.Begin);
        BinaryReader br = new BinaryReader(stream, encoding);
        var d = new ParticleInfo.ParticleInfoData();

        int atomsCount = br.ReadByte();
        // atoms type
        var atoms = new List<AtomInfo>(atomsCount);
        d.atoms = atoms;
        for (int i = 0; i < atomsCount; i++)
        {
            var a = new AtomInfo();
            a.element = (Element)br.ReadByte();
            atoms.Add(a);
        }

        // atoms 3D position
        if (atomsCount > 1 & (p.flags & ParticleInfo.ParticleFlags.Has3D) > 0)
        {
            if (atomsCount > 2)
            {
                Vector3 max = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                for (int i = 0; i < atomsCount; i++)
                    atoms[i].position = new Vector3(br.ReadInt16() * max.x, br.ReadInt16() * max.y, br.ReadInt16() * max.z);
            }
            else
            {
                for (int i = 0; i < atomsCount; i++)
                    atoms[i].position = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
            }
        }

        // atoms 2D position
        if (atomsCount > 1 & (p.flags & ParticleInfo.ParticleFlags.Has2D) > 0)
        {
            if (atomsCount > 2)
            {
                Vector2 max = new Vector3(br.ReadSingle(), br.ReadSingle());
                for (int i = 0; i < atomsCount; i++)
                    atoms[i].flatPosition = new Vector2(br.ReadInt16() * max.x, br.ReadInt16() * max.y);
            }
            else
            {
                for (int i = 0; i < atomsCount; i++)
                    atoms[i].flatPosition = new Vector2(br.ReadSingle(), br.ReadSingle());
            }
        }

        // atoms charges
        if ((p.flags & ParticleInfo.ParticleFlags.HasAtomCharges) > 0)
        {
            for (int i = 0; i < atomsCount; i++)
                atoms[i].atomCharge = br.ReadSByte();
        }

        // atoms radicals
        if ((p.flags & ParticleInfo.ParticleFlags.HasRadicalAtoms) > 0)
        {
            for (int i = 0; i < atomsCount; i++)
                atoms[i].radical = br.ReadSByte();
        }

        // bonds
        if (atomsCount > 1)
        {
            int bondsCount = br.ReadByte();
            var bonds = new List<BondInfo>(bondsCount);
            d.bonds = bonds;
            for (int i = 0; i < bondsCount; i++)
            {
                bonds.Add(new BondInfo() { atom1 = br.ReadByte(), atom2 = br.ReadByte(), bondType = (BondInfo.BondType)br.ReadByte() });
            }
        }
        else
            d.bonds = new List<BondInfo>(0);

        p.data = d;
        br.Close();
        stream.Dispose();
    }

#if UNITY_EDITOR
    protected string GetIndexPath() { return Application.dataPath + DBPath + IndexFilename + ".bytes"; }

    public override void RemoveParticleFile(ParticleInfo p)
    { throw new Exception("Trying to remove file from binary-serialized DB"); }

    public override void Serialize(MelDB db, bool indexOnly, bool removeUnusedFiles = true,
        Func<string, int, int, bool> progress = null)
    {
        if (indexOnly)
            throw new Exception("Trying to binary serialize DB with index only");

        var bucketPrefix = Application.dataPath + DBPath + BucketFilename;
        var dir = Path.GetDirectoryName(bucketPrefix);
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        if (progress != null && progress("Binary Serialization: 1/4 preparing buckets", 0, 1))
            return;
        var streams = new BinaryWriter[BUCKETS];
        for (int i = 0; i < BUCKETS; i++)
        {
            var s = File.Create(bucketPrefix + i.ToString("000") + ".bytes");
            streams[i] = new BinaryWriter(s, encoding);
        }
        var particles = db.particles;
        int particlesCount = particles.Count;

        // first serialize all particles into buckets to get buckets location indexes
        for (int i = particlesCount - 1; i >= 0; i--)
        {
            if (progress != null && progress("Binary Serialization: 2/4 cleaning and serializing particles", particlesCount - i, particlesCount))
                return;
            var p = particles[i];
            p.Init();
            if (p.data == null | !IncludeInBuild(p))
            {
                particles.RemoveAt(i);
                continue;
            }

            var buck = GetParticleBuckedIndex(p);
            var writer = streams[buck];
            WriteParticleData(writer, p);
        }
        for (int i = 0; i < BUCKETS; i++)
            streams[i].Close();
        particlesCount = particles.Count;
        if (progress != null && progress("Binary Serialization: 3/4 sorting DB", 0, 1))
            return;
        db.Sort();
        
        dir = Path.GetDirectoryName(GetIndexPath());
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        // now we can serialize index with bucked indexes
        using (FileStream fDB = File.Create(GetIndexPath()))
        {
            var bw = new BinaryWriter(fDB, encoding);
            bw.Write(particlesCount);
            for(int i = 0; i < particlesCount; i++)
            {
                if (progress != null && progress("Binary Serialization: 4/4 serializing index", i, particlesCount))
                    return;
                var p = particles[i];
                var flags = p.flags;
                bw.Write(p.atomsHash);
                bw.Write(p.structureHash);
                bw.Write(p.structureHashExact);
                bw.Write((ushort)p.binaryDataLocationInBucket);
                bw.Write(p.id);
                bw.Write(p.name);
                bw.Write((ushort)p.flags);
                if ((flags & ParticleInfo.ParticleFlags.HasChemicalFormula) > 0)
                    bw.Write(p.chemicalFormula);
                if ((flags & ParticleInfo.ParticleFlags.HasParticleCharge) > 0)
                    bw.Write((sbyte)p.charge);
                bw.Write((byte)p.CASes.Count);
                for (int cas = 0; cas < p.CASes.Count; cas++)
                    bw.Write(p.CASes[cas]);
            }

            for (int i = 0; i < particlesCount; i++)
                bw.Write(db.__sortedIds[i]);
            //for (int i = 0; i < particlesCount; i++)
            //    bw.Write(db.__sortedStructureHashes[i]);
            //for (int i = 0; i < particlesCount; i++)
            //    bw.Write(db.__sortedStructureHashesExact[i]);
            for (int i = 0; i < particlesCount; i++)
                bw.Write(db.__sortedAtomsHashes[i]);
            bw.Close();
        }

        Debug.LogFormat("MelDB serialized with {0} particles into {1} buckets\nFlags: {2}",
            particlesCount, BUCKETS, _include);
    }

    protected void WriteParticleData(BinaryWriter bw, ParticleInfo p)
    {
        var binPos = bw.BaseStream.Position;
        Debug.Assert(binPos < ushort.MaxValue);
        p.binaryDataLocationInBucket = (ushort)binPos;
        var d = p.data;

        var atoms = d.atoms;
        bw.Write((byte)atoms.Count);

        // atoms type
        for (int i = 0; i < atoms.Count; i++)
            bw.Write((byte)atoms[i].element);

        // atoms 3D position
        if (atoms.Count > 1 & (p.flags & ParticleInfo.ParticleFlags.Has3D) > 0)
        {
            if (atoms.Count > 2)
            {
                Vector3 max = new Vector3();
                for (int i = 0; i < atoms.Count; i++)
                {
                    var pos = atoms[i].position;
                    max.x = Mathf.Max(max.x, Mathf.Abs(pos.x));
                    max.y = Mathf.Max(max.y, Mathf.Abs(pos.y));
                    max.z = Mathf.Max(max.z, Mathf.Abs(pos.z));
                }
                max.x = (max.x + 0.1f) / short.MaxValue;
                max.y = (max.y + 0.1f) / short.MaxValue;
                max.z = (max.z + 0.1f) / short.MaxValue;
                bw.Write(max.x);
                bw.Write(max.y);
                bw.Write(max.z);
                for (int i = 0; i < atoms.Count; i++)
                {
                    var pos = atoms[i].position;
                    bw.Write((short)(pos.x / max.x));
                    bw.Write((short)(pos.y / max.y));
                    bw.Write((short)(pos.z / max.z));
                }
            }
            else
            {
                for (int i = 0; i < atoms.Count; i++)
                {
                    var pos = atoms[i].position;
                    bw.Write(pos.x);
                    bw.Write(pos.y);
                    bw.Write(pos.z);
                }
            }
        }

        // atoms 2D position
        if (atoms.Count > 1 & (p.flags & ParticleInfo.ParticleFlags.Has2D) > 0)
        {
            if (atoms.Count > 2)
            {
                Vector2 max = new Vector2();
                for (int i = 0; i < atoms.Count; i++)
                {
                    var pos = atoms[i].flatPosition;
                    max.x = Mathf.Max(max.x, Mathf.Abs(pos.x));
                    max.y = Mathf.Max(max.y, Mathf.Abs(pos.y));
                }
                max.x = (max.x + 0.1f) / short.MaxValue;
                max.y = (max.y + 0.1f) / short.MaxValue;
                bw.Write(max.x);
                bw.Write(max.y);
                for (int i = 0; i < atoms.Count; i++)
                {
                    var pos = atoms[i].flatPosition;
                    bw.Write((short)(pos.x / max.x));
                    bw.Write((short)(pos.y / max.y));
                }
            }
            else
            {
                for (int i = 0; i < atoms.Count; i++)
                {
                    var pos = atoms[i].flatPosition;
                    bw.Write(pos.x);
                    bw.Write(pos.y);
                }
            }
        }

        // atoms charges
        if ((p.flags & ParticleInfo.ParticleFlags.HasAtomCharges) > 0)
        {
            for (int i = 0; i < atoms.Count; i++)
                bw.Write(atoms[i].atomCharge);
        }

        // atoms radicals
        if ((p.flags & ParticleInfo.ParticleFlags.HasRadicalAtoms) > 0)
        {
            for (int i = 0; i < atoms.Count; i++)
                bw.Write(atoms[i].radical);
        }

        // bonds
        if (atoms.Count > 1)
        {
            var bonds = p.bonds;
            bw.Write((byte)bonds.Count);
            for (int i = 0; i < bonds.Count; i++)
            {
                var b = bonds[i];
                bw.Write((byte)b.atom1);
                bw.Write((byte)b.atom2);
                bw.Write((byte)b.bondType);
            }
        }
    }
#endif
}