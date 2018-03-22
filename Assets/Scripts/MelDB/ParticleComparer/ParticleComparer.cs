//#define COMPARE_BONDS

using System;
using ParticleComparerInternal;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using System.Linq;
using System.Text;
#endif

public static class ParticleComparer
{
    public const int MAX_ATOMS = 256;
    public const uint NOT_CONNECTED_HASH = uint.MaxValue - 1;
    private static uint[] rHashes;

    public class AtomsReordering
    {
        public AtomsReordering(int capacity) { reordering = new List<int>(capacity); }
        public AtomsReordering(bool allocateList = true) { if (allocateList) reordering = new List<int>(); }
        public List<int> reordering;
        public int totalReorderings; // OUTPUT. in some symmetrical molecules there are multiple possible correct atom reorderings. here there will be written the total number of possible reorderings for passed molecule. If this number is more than 1, you can use this number to enumerate through all possible reorderings by repeating your queries with different selectedReordering values
        public int selectedReordering; // INPUT. set it to some number in range [0, reorderingVariants) before passing to AreEqual or GetHash functions to get reordering corresponding to this variant
    }

    // static data to avoid allocations in frequent queries
    private static List<Element> _atomsList = new List<Element>();
    private static AtomsReordering _reordering2 = new AtomsReordering(false);
    private static HangedGraph _hangedGraph = new HangedGraph();
    private static List<int> _reorderingList = new List<int>();
    private static List<int> _reorderingList2 = new List<int>();
    private static List<int> _temp = new List<int>();
    private static AtomsReordering _reordering1 = new AtomsReordering(false);

    static ParticleComparer()
    {
        int seed = 2016932201;
        rHashes = new uint[MAX_ATOMS];

        var r = new System.Random(seed);
        byte[] buf = new byte[4];
        for (int i = 0; i < MAX_ATOMS; i++)
        {
            r.NextBytes(buf);
            uint rand = BitConverter.ToUInt32(buf, 0);
            rHashes[i] = rand;
        }
    }

    #region comparison API

    /// <summary>
    /// Compares two particles on topological equality
    /// </summary>
    /// <param name="exact">pass true if you want the bonds types to be taken into account</param>
    /// <param name="reordering">if you pass it, the correspondance atom indexes will be written in it: if reordering[11] == 22 then atom 11 from particle 1 is the same as atom 22 in particle 2</param>
    /// <returns>true if the particles are equal</returns>
    public static bool AreEqual(ParticleInfo part1, ParticleInfo part2, bool exact, List<int> reordering)
    {
        _reordering1.selectedReordering = 0;
        _reordering1.reordering = reordering;
        var result = AreEqual(part1, part2, exact, _reordering1);
        _reordering1.reordering = null;
        return result;
    }
    public static bool AreEqual(ParticleInfo part1, ParticleInfo part2, bool exact, AtomsReordering reordering = null)
    {
        // compare by hashes
        if (exact)
        {
            if (part1.structureHashExact != 0 & part2.structureHashExact != 0) // only if hashes are already calculated
                if (part1.structureHashExact != part2.structureHashExact)
                    return false;
        }
        else
        {
            if (part1.structureHash != 0 & part2.structureHash != 0) // only if hashes are already calculated
                if (part1.structureHash != part2.structureHash)
                    return false;
        }
        // init particles and compare the rest
        var atoms1 = part1.atoms;
        var atoms2 = part2.atoms;
        if (atoms1.Count != atoms2.Count)
            return false;
        if (part1.bonds.Count != part2.bonds.Count)
            return false;
        // compare by molecular formula
        var a1hash = ParticleInfo.GetAtomsHash(atoms1);
        var a2hash = ParticleInfo.GetAtomsHash(atoms2);
        if (a1hash != a2hash)
            return false;
        // compare by topology
        _atomsList.Clear();
        for (int i = 0; i < part1.atoms.Count; i++)
            _atomsList.Add(part1.atoms[i].element);
        return AreEqual(_atomsList, part1.bonds, part2, exact, reordering, false);
    }
    
    /// <summary>
    /// Compares two particles on topological equality
    /// </summary>
    /// <param name="exact">pass true if you want the bonds to be thoroughly compared</param>
    /// <param name="reordering">if you pass it, the correspondance atom indexes will be written in it: if reordering[11] == 22 then atom 11 from particle 1 is the same as atom 22 in particle 2</param>
    /// <returns></returns>
    public static bool AreEqual(List<Element> atms, List<BondInfo> bonds, ParticleInfo part2, bool exact, List<int> reordering, bool checkMolecularFormula = true)
    {
        _reordering1.selectedReordering = 0;
        _reordering1.reordering = reordering;
        var result = AreEqual(atms, bonds, part2, exact, _reordering1, checkMolecularFormula);
        _reordering1.reordering = null;
        return result;
    }
    public static bool AreEqual(List<Element> atms, List<BondInfo> bonds, ParticleInfo part2, bool exact, AtomsReordering reordering = null, bool checkMolecularFormula = true)
    {
        var atoms2 = part2.atoms;
        if (atms.Count != atoms2.Count)
            return false;
        if (bonds.Count != part2.bonds.Count)
            return false;
        if (checkMolecularFormula)
        {
            // compare by molecular formula
            var a1hash = ParticleInfo.GetAtomsHash(atms);
            var a2hash = ParticleInfo.GetAtomsHash(atoms2);
            if (a1hash != a2hash)
                return false;
        }
        // compare by hashes
        var p1hash = GetStructureHash(atms, bonds, exact, _reorderingList);
        if (exact && p1hash != part2.structureHashExact & part2.structureHashExact != 0)
            return false;
        if (!exact && p1hash != part2.structureHash & part2.structureHash != 0)
            return false;

        if (reordering == null)
            reordering = _reordering1;
        if (reordering.reordering == null)
            reordering.reordering = _reorderingList2;
        reordering.reordering.Clear();
        var p2hash = GetStructureHash(part2, exact, reordering);
        if (p1hash != p2hash)
            return false;

        var rr = reordering.reordering;
        CombineReorderings(rr, _reorderingList);
        // compare each atom and bond, to be exactly sure the particles are equal
        for (int i = atms.Count - 1; i >= 0; i--)
        {
            if (atms[i] != atoms2[rr[i]].element)
                return false;
        }
#if COMPARE_BONDS
        // bonds comparison is not necessary - hash almost guarantees correct bonds
        var pbonds = part2.bonds;
        int maxBonds = bonds.Count - 1;
        for (int i = maxBonds; i >= 0;)
        {
            var b = bonds[i];
            int indx1 = rr[b.atom1];
            int indx2 = rr[b.atom2];
            for (int j = maxBonds; j >= 0; j--)
            {
                var b2 = pbonds[j];
                if (b2.atom1 == indx1 & b2.atom2 == indx2)
                    goto bond_found;
                if (b2.atom1 == indx2 & b2.atom2 == indx1)
                    goto bond_found;
            }
            return false;
bond_found:
            i--;
        }
#endif
        return true;
    }

    #endregion

    #region get hash API

    public static uint GetStructureHash(ParticleInfo particle, bool exact, List<int> reordering)
    {
        _reordering2.selectedReordering = 0;
        _reordering2.reordering = reordering;
        var hash = GetStructureHash(particle, exact, _reordering2);
        _reordering2.reordering = null;
        return hash;
    }
    public static uint GetStructureHash(ParticleInfo particle, bool exact, AtomsReordering reordering = null)
    {
        if (particle.atoms.Count == 0)
            return 0;
        var gr = new ParticleGraphProto(particle, exact, rHashes);
        int variant = reordering == null ? 0 : reordering.selectedReordering;
        int total;
        var hash = GetHashInner(gr, variant, out total);
        if (reordering != null)
        {
            reordering.totalReorderings = total;
            BuildReorderingArray(gr, reordering.reordering);
        }
        return hash;
    }

    public static uint GetStructureHash(List<Element> atoms, List<BondInfo> bonds, bool exact, List<int> reordering)
    {
        _reordering2.selectedReordering = 0;
        _reordering2.reordering = reordering;
        var hash = GetStructureHash(atoms, bonds, exact, _reordering2);
        _reordering2.reordering = null;
        return hash;
    }
    public static uint GetStructureHash(List<Element> atoms, List<BondInfo> bonds, bool exact, AtomsReordering reordering = null)
    {
        if (atoms.Count == 0)
            return 0;
        var gr = new ParticleGraphProto(atoms, bonds, exact, rHashes);
        int variant = reordering == null ? 0 : reordering.selectedReordering;
        int total;
        var hash = GetHashInner(gr, variant, out total);
        if (reordering != null)
        {
            reordering.totalReorderings = total;
            BuildReorderingArray(gr, reordering.reordering);
        }
        return hash;
    }

    #endregion

    // result is written in the first argument
    private static void CombineReorderings(List<int> r2, List<int> r1)
    {
        int len = r1.Count;
        _temp.Clear();
        if (_temp.Capacity < len)
            _temp.Capacity = len;
        for (int i = 0; i < len; i++)
            _temp.Add(0);
        for (int i = 0; i < len; i++)
            _temp[r1[i]] = r2[i];
        for (int i = 0; i < len; i++)
            r2[i] = _temp[i];
    }

    private static uint GetHashInner(ParticleGraphProto gr, int variant, out int totalVariants)
    {
        uint hash;
        totalVariants = 1;
        gr.PrepareSimpleHashes();
        gr.TrySortAndUpdateHash(out hash);
        gr.PrepareSimpleHashes();
        gr.TrySortAndUpdateHash(out hash);
        _hangedGraph.AttachTo(gr);
        if (!_hangedGraph.AllNodesConnected(gr.atoms[0]))
            return NOT_CONNECTED_HASH;
        // try to get unique hash based only on hanged graphs on each atom
        var newHashes = new uint[gr.atoms.Count];
        for (int i = 0; i < gr.atoms.Count; i++)
        {
            IterateOnHashes(gr, _hangedGraph, newHashes);
            if (gr.TrySortAndUpdateHash(out hash))
                return hash;
            // there are symmetrical atoms. lets introduce some assymetry by changing hash in one of them
            uint atomHash;
            int variants = gr.CountSimilarVariants(out atomHash);
            totalVariants *= variants;
            int v = variant % variants;
            variant /= variants;
            gr.ModifySimilarHash(atomHash, v);
            gr.TrySortAndUpdateHash(out hash);
        }
        Debug.LogError("Last\n\t" + hash);
        return hash;
    }

    private static void BuildReorderingArray(ParticleGraphProto gr, List<int> reordering)
    {
        if (reordering == null)
            return;
        reordering.Clear();
        var atoms = gr.atoms;
        int atomsCount = atoms.Count;
        if (reordering.Capacity < atomsCount)
            reordering.Capacity = atomsCount;
        for (int i = 0; i < atomsCount; i++)
            reordering.Add(atoms[i].originalIndx);
    }

    private static void IterateOnHashes(ParticleGraphProto gr, HangedGraph hg, uint[] newHashes)
    {
        for (int i = gr.atoms.Count - 1; i >= 0; i--)
        {
            var atom = gr.atoms[i];
            hg.Hang(atom);
            newHashes[i] = hg.GetHash(rHashes);
        }
        for (int i = gr.atoms.Count - 1; i >= 0; i--)
            gr.atoms[i].protoHash = newHashes[i];
    }

#if UNITY_EDITOR
    private static void Print(ParticleGraphProto gr, string prefix = "")
    {
        var sb = new StringBuilder(prefix);
        if (!string.IsNullOrEmpty(prefix))
            sb.AppendLine();
        for (int i = 0; i < gr.atoms.Count; i++)
        {
            var atom = gr.atoms[i];
            sb.AppendFormat("\tatom {0} @{2} [ {1:X} ]\tbonds: ", atom.type, atom.protoHash, atom.originalIndx);
            for (int j = 0; j < atom.bonds.Count; j++)
            {
                var bond = atom.bonds[j];
                sb.AppendFormat("\t<b>{1}</b>@{2}@{3}->{0:X}      ", bond.Other(atom).protoHash, bond.type - 127, bond.node1.originalIndx, bond.node2.originalIndx);
            }
            sb.AppendLine();
        }
        sb.AppendLine();
        Debug.Log(sb.ToString());
    }


    private const int TESTS = 5;
    //[MenuItem("MELDB/Test", priority = 2000)]
    private static void Test()
    {
        var p = MelDB.FindParticle("1h-phosphirene 1-oxide");
        //var p = MelDB.GetParticle("Benzene");
        Test(p);
    }
    public static bool Test(ParticleInfo p)
    {
        var c = p.CreateCopy();

        var reord = new List<int>(p.atoms.Count);

        for (int test = 0; test < TESTS; test++)
        {
            c.Shuffle();

            var atoms = c.atoms.Select(x => x.element).ToList();
            var bonds = c.bonds;

            //Debug.LogWarning("1. SHUFFLED:\n" + string.Join(", ", atoms.Select(x => x.ToString()).ToArray()) + "\n");
            //Debug.LogWarning("2. ORIGINAL:\n" + string.Join(", ", p.atoms.Select(x => x.element.ToString()).ToArray()) + "\n");
            //Debug.LogWarning(string.Join(", ", bonds.Select(x => x.atom1 + "-" + x.atom2).ToArray()) + "\n");
            //Debug.LogWarning(string.Join(", ", p.bonds.Select(x => x.atom1 + "-" + x.atom2).ToArray()) + "\n");

            foreach (var b in c.bonds)
                b.bondType = BondInfo.BondType.SINGLE;
            if (ParticleComparer.AreEqual(atoms, bonds, p, false, reord))
            {
                //Debug.Log("YES\n");
                //var rdr = new List<Element>(atoms.Count);
                //Debug.LogWarning(string.Join(", ", reord.Select(x => x.ToString()).ToArray()) + "\nREORDERING");
                //for (int i = 0; i < p.atoms.Count; i++)
                //rdr.Add(p.atoms[reord[i]].element);
                //Debug.LogWarning("REORDERED:\n" + string.Join(", ", rdr.Select(x => x.ToString()).ToArray()) + "\n");
            }
            else
            {
                Debug.LogError("NO!!!\n" + p.name);
                return false;
            }
        }
        return true;
    }
#endif
}
