using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using System;
using System.Linq;

[Serializable]
public class MelDB
{
    private static MelDB _instance = null;
    
	public List<ParticleInfo> particles = new List<ParticleInfo>();
    public ushort[] __sortedIds;
    public ushort[] __sortedAtomsHashes;

    [XmlIgnore]
    public static MelDB Instance { get { Init(); return _instance; } }
    
    [XmlIgnore]
	public static List<ParticleInfo> Particles
	{ get { return Instance.particles; } }
    
    public static ParticleInfo FindParticleById(ulong id)
    {
        // use binary search; assume only one result
        var particles = Instance.particles;
        var sorted = Instance.__sortedIds;
        int min = 0;
        int max = particles.Count;
        int center = 0;
        while (max != min)
        {
            center = (min + max) / 2;
            var currId = particles[sorted[center]].id;
            if (id < currId)
            {
                max = center;
                continue;
            }
            if (id > currId)
            {
                min = center + 1;
                continue;
            }
            return particles[sorted[center]];
        }
        return null;
    }

    public static ParticleInfo FindParticleByIdOrName(string idOrName)
    {
        ulong id;
        if (ulong.TryParse(idOrName, out id))
            return FindParticleById(id);
        return FindParticle(idOrName);
    }

    public static ParticleInfo FindParticle(string name)
    {
        name = ParticleInfo.UnifiedName(name);
        // use binary search; if there are equal names - return first particle
        var particles = Instance.particles;
        int min = 0;
        int max = particles.Count;
        int center = 0;
        while (max != min)
        {
            center = (min + max) / 2;
            var comparison = string.CompareOrdinal(name, particles[center].name);
            if (comparison < 0)
            {
                max = center;
                continue;
            }
            if (comparison > 0)
            {
                min = center + 1;
                continue;
            }
            break;
        }
        if (min == max)
            return null;
        // exact match was found
        int start = center - 1;
        while (start >= min && string.CompareOrdinal(name, particles[start].name) == 0)
            start--;
        return particles[start + 1]; // we return first particle with this name
    }

    /// <summary>
    /// Find first particle from the MELDB which has the same structure as provided
    /// </summary>
    /// <param name="atoms">atom elements list</param>
    /// <param name="exact">pass true if you want compare bond types also</param>
    /// <param name="reordering">if you pass it, the correspondance atom indexes will be written in it: if reordering[11] == 22 then atom 11 from provided list is the same as atom 22 in resulting particle</param>
    /// <returns>first found particle; null if not found</returns>
    public static ParticleInfo FindParticle(List<Element> atoms, List<BondInfo> bonds, bool exact = true, ParticleComparer.AtomsReordering reordering = null, ParticleInfo.ParticleFlags flags = ParticleInfo.ParticleFlags.None)
    {
        var aHash = ParticleInfo.GetAtomsHash(atoms);
        
        int startIndx, endIndx;
        InnerSearch(aHash, out startIndx, out endIndx);
        if (startIndx < 0)
            return null;
        var particles = Instance.particles;
        var sorting = Instance.__sortedAtomsHashes;
        uint hash = 0;
        for (int i = startIndx; i <= endIndx; i++)
        {
            var p = particles[sorting[i]];
            if ((p.flags & flags) != flags)
                continue;
            if (hash == 0)  // we do want to avoid this operation as long as possible, so it should be here
                hash = ParticleComparer.GetStructureHash(atoms, bonds, exact);
            var pHash = exact ? p.structureHashExact : p.structureHash;
            if (pHash != hash)
                continue;
            if (ParticleComparer.AreEqual(atoms, bonds, p, exact, reordering, false))
                return p;
        }
        return null;
    }
    /// <summary>
    /// Find all particles from the MELDB which has the same structure as provided
    /// </summary>
    /// <param name="atoms">atom elements list</param>
    /// <param name="exact">pass true if you want compare bond types also</param>
    /// <returns>first found particle; null if not found</returns>
    public static IEnumerator FindParticles(List<ParticleInfo> results, List<Element> atoms, List<BondInfo> bonds, bool exact = true, ParticleInfo.ParticleFlags flags = ParticleInfo.ParticleFlags.None)
    {
        var aHash = ParticleInfo.GetAtomsHash(atoms);
        results.Clear();

        int startIndx, endIndx;
        InnerSearch(aHash, out startIndx, out endIndx);
        if (startIndx < 0)
            yield break;
        yield return null;
        var particles = Instance.particles;
        var sorting = Instance.__sortedAtomsHashes;
        uint hash = 0;
        for (int i = startIndx; i <= endIndx; i++)
        {
            var p = particles[sorting[i]];
            if ((p.flags & flags) != flags)
                continue;
            if (hash == 0)  // we do want to avoid this operation as long as possible, so it should be here
            {
                hash = ParticleComparer.GetStructureHash(atoms, bonds, exact);
                yield return null;
            }
            var pHash = exact ? p.structureHashExact : p.structureHash;
            if (pHash != hash)
                continue;
            if (ParticleComparer.AreEqual(atoms, bonds, p, exact, (ParticleComparer.AtomsReordering)null, false))
                results.Add(p);
            yield return null;
        }
    }

    private static void InnerSearch(uint aHash, out int startIndx, out int endIndx)
    {
        startIndx = -1;
        endIndx = -1;
        var particles = Instance.particles;
        var sorted = Instance.__sortedAtomsHashes;

        int min = 0;
        int max = particles.Count;
        int center = 0;
        while (max != min)
        {
            center = (min + max) / 2;
            var currAHash = particles[sorted[center]].atomsHash;
            if (aHash < currAHash)
            {
                max = center;
                continue;
            }
            if (aHash > currAHash)
            {
                min = center + 1;
                continue;
            }
            break;
        }
        if (min == max)
            return;
        // exact match was found
        // TODO: maybe use binary search to find start/end? especially if a lot of particles have the same atomHashes
        startIndx = center - 1;
        while (startIndx >= min && aHash == particles[sorted[startIndx]].atomsHash)
            startIndx--;
        startIndx++;
        endIndx = center + 1;
        while (endIndx < max && aHash == particles[sorted[endIndx]].atomsHash)
            endIndx++;
        endIndx--;
        // now all particles inside start/end range are fulfill atomHash query
    }

	public static void Init()
	{
        if (_instance != null)
            return;
        _instance = MelDBSerializer.Instance.LoadIndex();
        Debug.Log("MelDB index loaded: " + _instance.particles.Count + " particles");
	}

    public static IEnumerator InitAsync(MonoBehaviour coroutineHolder)
    {
        if (_instance != null)
            yield break;
        MelDBSerializer.coroutineHolder = coroutineHolder;
        var request = MelDBSerializer.Instance.LoadIndexAsync();
        yield return request;

        if (_instance == null)
        {
            _instance = request.result;
            Debug.Log("MelDB index async-loaded: " + _instance.particles.Count + " particles");
        }
    }

    public enum DataType : byte
    {
        Compound = 0,
        Molecule = 1,
        Cation = 2,
        Anion = 3
    }

    private static System.Random _random = new System.Random();
    private static byte[] _uintBuffer = new byte[8];
    public static ulong GetRandomId()
    {
        _random.NextBytes(_uintBuffer);
        return BitConverter.ToUInt64(_uintBuffer, 0);
    }

    private static System.Text.StringBuilder _sbName = new System.Text.StringBuilder(50);

    public static uint ParseCASNumber(string s)
    {
        s = s.Replace("-", "");
        string ns = s.Substring(0, s.Length - 1);
        uint cas;
        if (!uint.TryParse(ns, out cas))
            return 0;
        uint sHash;
        if (!uint.TryParse(s.Substring(s.Length - 1), out sHash))
            return 0;
        uint hash = 0;
        uint check = cas;
        uint digit = 1;
        while (check > 0) {
            hash += (check % 10) * digit;
            check /= 10;
            digit++;
        }
        hash = hash % 10;
        if (sHash != hash)
            return 0;
        return cas;
    }

    public static string CASToString(uint cas, bool addDashes = true)
    {
        _sbName.Remove(0, _sbName.Length);
        _sbName.Append(cas / 100);
        if (addDashes)
            _sbName.Append('-');
        _sbName.Append((cas % 100).ToString("00"));
        if (addDashes)
            _sbName.Append('-');
        uint hash = 0;
        uint digit = 1;
        while (cas > 0) {
            hash += (cas % 10) * digit;
            digit++;
            cas = cas / 10;
        }
        hash = hash % 10;
        _sbName.Append(hash);

        return _sbName.ToString();
    }

#if UNITY_EDITOR

    public static void Clear()
    {
        _instance = null;
    }

    public void Sort()
    {
        var parts = particles.ToArray();
        int count = parts.Length;
        string[] keys = new string[count];
        for (int i = count - 1; i >= 0; i--)
            keys[i] = parts[i].name;
        Array.Sort(keys, parts, StringComparer.Ordinal);
        for (int i = count - 1; i >= 0; i--)
            particles[i] = parts[i];

        Debug.Assert(count < ushort.MaxValue);

        // ids
        var sorted = new ushort[count];
        for (int i = 0; i < count; i++)
            sorted[i] = (ushort)i;
        var ids = new ulong[count];
        for (int i = 0; i < count; i++)
            ids[i] = parts[i].id;

        Array.Sort(ids, sorted);
        __sortedIds = sorted;

        // atoms hashes
        sorted = new ushort[count];
        for (int i = 0; i < count; i++)
            sorted[i] = (ushort)i;
        var hashes = new uint[count];
        for (int i = 0; i < count; i++)
            hashes[i] = parts[i].atomsHash;

        Array.Sort(hashes, sorted);
        __sortedAtomsHashes = sorted;

        //// structure hashes
        //sorted = new ushort[count];
        //for (int i = 0; i < count; i++)
        //    sorted[i] = (ushort)i;
        //for (int i = 0; i < count; i++)
        //    hashes[i] = parts[i].structureHash;

        //Array.Sort(hashes, sorted);
        //__sortedStructureHashes = sorted;

        //// structure hashes exact
        //sorted = new ushort[count];
        //for (int i = 0; i < count; i++)
        //    sorted[i] = (ushort)i;
        //for (int i = 0; i < count; i++)
        //    hashes[i] = parts[i].structureHashExact;

        //Array.Sort(hashes, sorted);
        //__sortedStructureHashesExact = sorted;
    }

    public static void FilterOut(Predicate<ParticleInfo> predicate)
    {
        Instance.particles.RemoveAll(predicate);
        Instance.Sort();
    }

    public static void InitAll()
    {
        foreach (var p in Instance.particles)
            p.Init();
    }

    public static void RemoveParticle(ParticleInfo p)
    {
        if (Instance.particles.Remove(p))
            MelDBSerializer.xmlInstance.RemoveParticleFile(p);
    }
    
#endif
}