//#define BINARY_DESERIALIZATION // TODO: Vector2, Vector3, Quaternion are not serializable by BinaryFormatter

using UnityEngine;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Xml.Serialization;
using System;
using System.Collections;

[Serializable]
public class ParticleInfo
{

#if UNITY_EDITOR
    // META information, only stored in XML version of DB
    /// <summary>
    /// DO NOT USE, for convenient XML serialization only
    /// </summary>
    public uint CID; // CID from PubChem web-site
    
    /// <summary>
    /// DO NOT USE, for convenient XML serialization only
    /// </summary>
    public string primaryName;
    /// <summary>
    /// DO NOT USE, for convenient XML serialization only
    /// </summary>
    public List<string> iupacs = new List<string>();
#endif
    public uint atomsHash;  // only atom types and their numbers affects this hash
    public uint structureHash; // only topology and atom types affects this hash atom positions are ignored
    public uint structureHashExact; // also bond types are accounted here
    [XmlIgnore]
    public uint binaryDataLocationInBucket;
    public ulong id;
    public string name = "";
    public string chemicalFormula = "";  // C2 H5 O H, non-formatted
    public short charge = 0;
    public ParticleFlags flags = ParticleFlags.None;

    public List<uint> CASes = new List<uint>(1); // CAS numbers without check-sum and dashes

    [Flags]
    public enum ParticleFlags : ushort
    {
        None                = 0,
        Has3D               = 1 << 0,
        Has2D               = 1 << 1,
        HasTopology         = 1 << 2,
        PrimarilyFlat       = 1 << 3,
        HasSkeletalFormula  = 1 << 4,
        HasChemicalFormula  = 1 << 5,
        ShowInExplorer      = 1 << 6,
        ShowInConstructor   = 1 << 7,
        HasParticleCharge   = 1 << 8,
        HasAtomCharges      = 1 << 9,
        ForceIncludeInBuild = 1 << 10,
        HasRadicalAtoms     = 1 << 11,
        NameIsIUPAC         = 1 << 12,
        MolecularFormula_1  = 1 << 13,  // these two flags are used to encode one of the four possible rules for molecular formula construction
        MolecularFormula_2  = 1 << 14,  // 00 - C_H_x, 01 - H_x_O, 10 - electroneg, 11 - reversed
        IncorrectValence    = 1 << 15   // right now only C atom validated
    }

    [Serializable]
    public class ParticleInfoData
    {
        public List<AtomInfo> atoms = new List<AtomInfo>();
        public List<BondInfo> bonds = new List<BondInfo>();
#if UNITY_EDITOR
        /// <summary>
        /// DO NOT USE, for convenient XML serialization only
        /// </summary>
        public ParticleInfo metaInfo;
#endif
        public void Clear()
        {
            atoms.Clear();
            bonds.Clear();
        }
    }
    
    [NonSerialized]
    [XmlIgnore]
    public ParticleInfoData data = null;

    [XmlIgnore]
    private string _molecularFormula;
    /// <summary>
    /// Formatted molecular formula
    /// </summary>
    [XmlIgnore]
    public string molecularFormula {
        get {
            if (string.IsNullOrEmpty(_molecularFormula))
                _molecularFormula = GetMolecularFormula(this);
            return _molecularFormula;
        }
    }
    /// <summary>
    /// Formatted for TMP_Text chemical formula (if exist, otherwise - molecular one)
    /// </summary>
    [XmlIgnore]
    public string chemicalOrMolecularFormula {
        get {
            if (string.IsNullOrEmpty(chemicalFormula))
                return molecularFormula;
            return AddFormattingToFormula(chemicalFormula);
        }
    }

    [XmlIgnore]
    public bool initialized { get { return data != null; } }

    [XmlIgnore]
    public List<AtomInfo> atoms {
        get { Init(); return data.atoms; }
    }      

    [XmlIgnore]
    public List<BondInfo> bonds {
        get { Init(); return data.bonds; }
    }
        
    public float GetLinearSize()
    {
        Init();
        float maxValue = 0f;
        var atoms = data.atoms;
        for (int i = atoms.Count - 1; i >= 0; i--)
        {
            var atom = atoms[i];
            var r2 = atom.position.sqrMagnitude;
            if (r2 > maxValue)
                maxValue = r2;
        }
        return Mathf.Sqrt(maxValue) + 1f;
    }

    public float GetMolarMass()
    {
        Init();
        return data.atoms.Sum(ai => ai.GetAtomicMass());
    }

    private struct NamePrefix
    {
        public string lowerPrefix;
        public string prefix;
        public bool nextLetterIsCapital;
        public NamePrefix(string prefix, bool nextIsCapital)
        {
            this.prefix = prefix;
            lowerPrefix = prefix.ToLowerInvariant();
            nextLetterIsCapital = nextIsCapital;
        }
    }
    private static NamePrefix[] prefixes = new NamePrefix[]
    {
        new NamePrefix("D-",        true),
        new NamePrefix("L-",        true),
        new NamePrefix("S-",        true),
        new NamePrefix("DL-",       true),
        new NamePrefix("cis-",      true),
        new NamePrefix("trans-",    true),
        new NamePrefix("m-",        true),
        new NamePrefix("meta-",     true),
        new NamePrefix("p-",        true),
        new NamePrefix("para-",     true),
        new NamePrefix("o-",        true),
        new NamePrefix("ortho-",    true),
        new NamePrefix("aza-",      true),
        new NamePrefix("alpha-",    true),
        new NamePrefix("beta-",     true),
        new NamePrefix("gamma-",    true),
        new NamePrefix("delta-",    true),
        new NamePrefix("theta-",    true)
    };
    public static string UnifiedName(string name)
    {
        int i = 0;
        while (char.IsWhiteSpace(name[i])) { i++; }
        bool nextIsCapital = true;

        bool hasPrefix = true;
        while (hasPrefix)
        {
            hasPrefix = false;
            for (int p = 0; p < prefixes.Length; p++)
            {
                var prefix = prefixes[p].lowerPrefix;
                var max = i + prefix.Length;
                if (max >= name.Length)
                    continue;
                bool match = true;
                for (int j = i, jj = 0; j < max; j++, jj++)
                {
                    if (char.ToLowerInvariant(name[j]) != prefix[jj])
                    {
                        match = false;
                        break;
                    }
                }
                if (match)
                {
                    hasPrefix = true;
                    _sb.Append(prefixes[p].prefix);
                    nextIsCapital = prefixes[p].nextLetterIsCapital;
                    i += prefix.Length;
                    break;
                }
            }
        }

        if (nextIsCapital)
            _sb.Append(char.ToUpperInvariant(name[i++]));
        else
            _sb.Append(name[i++]);

        int lastNonWhite = i;
        for (; i < name.Length; i++)
        {
            char c = name[i];
            if (c == '$')
            {
                // skip $l^ prefix in radical notations
                if (name[i + 1] == 'l'
                    & name[i + 2] == '^')
                {
                    i += 2;
                    continue;
                }
                // replace &apos; by '
                if (char.ToLowerInvariant(name[i + 1]) == 'a'
                    & char.ToLowerInvariant(name[i + 2]) == 'p'
                    & char.ToLowerInvariant(name[i + 3]) == 'o'
                    & char.ToLowerInvariant(name[i + 4]) == 's'
                    & char.ToLowerInvariant(name[i + 5]) == ';')
                {
                    i += 5;
                    _sb.Append('\'');
                    lastNonWhite = _sb.Length;
                    continue;
                }
                continue;
            }
            if (c == '(')
            {
                int end = i + 1;
                while(end < name.Length && name[end] != ')')
                {
                    var cc = Char.ToLowerInvariant(name[end]);
                    if (cc != 'i' && c != 'v')
                    {
                        end = name.Length;
                        break;
                    }
                    end++;
                }
                if (end < name.Length)
                {
                    // it was sequence '(number)'
                    for (; i <= end; i++)
                        _sb.Append(char.ToUpperInvariant(name[i]));
                    if (i < name.Length && name[i] != ' ')
                    {
                        var nextC = name[i];
                        if (nextC != ']' & nextC != ')' & nextC != '-')
                            _sb.Append(' ');
                    }
                    i--;
                    lastNonWhite = _sb.Length;
                    continue;
                }
            }
            _sb.Append(char.ToLowerInvariant(c));
            if (!char.IsWhiteSpace(c))
                lastNonWhite = _sb.Length;
        }
        _sb.Remove(lastNonWhite, _sb.Length - lastNonWhite);
        var result = _sb.ToString();
        _sb.Remove(0, _sb.Length);
        return result;
    }

    #region molecular formula strings and hashes

    public static string GetMolecularFormula(ParticleInfo particle, bool formatted = true)
    {
        CountDifferentAtoms(particle.atoms);
        var ordering = GetMolecularFormulaOrderingFor(particle.flags);
        return GetSimpleMolecularFormulaInner(ordering, formatted);
    }

    private static string SUB_START = "<sub>";
    private static string SUB_END = "</sub>";
    private static System.Text.StringBuilder _sb = new System.Text.StringBuilder();
    private static ushort[] _atomCounter = new ushort[(int)Element.MAX];
    public static string GetSimpleMolecularFormula(List<AtomInfo> atoms, bool formatted = false)
    {
        CountDifferentAtoms(atoms);
        return GetSimpleMolecularFormulaInner(ordering_C_H_alphabetical, formatted);
    }
    public static string GetSimpleMolecularFormula(List<Element> atoms, bool formatted = false)
    {
        CountDifferentAtoms(atoms);
        return GetSimpleMolecularFormulaInner(ordering_C_H_alphabetical, formatted);
    }
    private static string GetSimpleMolecularFormulaInner(Element[] ordering, bool formatted)
    {
        int elements = ordering.Length;
        for (int i = 0; i < elements; i++)
        {
            var element = ordering[i];
            var index = (int)element;
            int count = _atomCounter[index];
            if (count == 0)
                continue;
            _atomCounter[index] = 0;
            _sb.Append(element);
            if (count > 1)
            {
                if (formatted) _sb.Append(SUB_START);
                _sb.Append(count);
                if (formatted) _sb.Append(SUB_END);
            }
        }
        var result = _sb.ToString();
        _sb.Remove(0, _sb.Length);
        return result;
    }
    public static uint GetAtomsHash(List<AtomInfo> atoms)
    {
        CountDifferentAtoms(atoms);
        return GetAtomsHashInner();
    }
    public static uint GetAtomsHash(List<Element> atoms)
    {
        CountDifferentAtoms(atoms);
        return GetAtomsHashInner();
    }
    public static void GetUniqueAtoms(List<AtomInfo> atoms, List<Element> result)
    {
        result.Clear();
        CountDifferentAtoms(atoms);
        var ordering = ordering_electronegativity;
        int elements = ordering.Length;
        for (int i = 0; i < elements; i++)
        {
            var element = ordering[i];
            var index = (int)element;
            int count = _atomCounter[index];
            if (count == 0)
                continue;
            _atomCounter[index] = 0;
            result.Add(element);
        }
    }
    private static void CountDifferentAtoms(List<Element> atoms)
    {
        for (int i = atoms.Count - 1; i >= 0; i--)
            _atomCounter[(int)atoms[i]]++;
    }
    private static void CountDifferentAtoms(List<AtomInfo> atoms)
    {
        for (int i = atoms.Count - 1; i >= 0; i--)
            _atomCounter[(int)atoms[i].element]++;
    }
    private static uint GetAtomsHashInner()
    {
        uint hash = 105943;
        for (uint i = 0; i < _atomCounter.Length; i++)
        {
            uint count = _atomCounter[i];
            _atomCounter[i] = 0;
            hash = unchecked(15486173 * hash + i);
            hash = unchecked(15489079 * hash + count);
        }
        return hash;
    }

    public static string AddFormattingToFormula(string formula, char specialSymbol = '#')
    {
        bool digitMode = false;
        int lastIndx = 0;
        for (int i = 0; i < formula.Length; i++)
        {
            bool isDigit = Char.IsDigit(formula[i]);
            if (!isDigit) isDigit = formula[i] == specialSymbol;
            if (digitMode == isDigit)
                continue;
            _sb.Append(formula, lastIndx, i - lastIndx);
            lastIndx = i;
            _sb.Append(digitMode ? SUB_END : SUB_START);
            digitMode = isDigit;
        }
        _sb.Append(formula, lastIndx, formula.Length - lastIndx);
        if (digitMode)
            _sb.Append(SUB_END);
        var result = _sb.ToString();
        _sb.Remove(0, _sb.Length);
        return result;
    }
    public static string ReplaceFormulaNumbersBySymbolAndAddFormatting(string formula, char replacementSymbol, List<int> visibleIndexes = null, List<int>rawIndexes = null, List<int> numberValues = null)
    {
        if (visibleIndexes != null) visibleIndexes.Clear();
        if (rawIndexes != null) rawIndexes.Clear();
        if (numberValues != null) numberValues.Clear();
        bool digitMode = false;
        int lastIndx = 0;
        int visibleIndx = 0;
        for (int i = 0; i < formula.Length; i++)
        {
            var c = formula[i];
            bool isDigit = Char.IsDigit(c);
            bool isCapital = Char.IsUpper(c);
            if (digitMode == isDigit & !isCapital)
                continue;
            if (digitMode)
            {
                _sb.Append(SUB_START);
                if (visibleIndexes != null) visibleIndexes.Add(visibleIndx++);
                if (rawIndexes != null) rawIndexes.Add(_sb.Length);
                if (numberValues != null) numberValues.Add(Int32.Parse(formula.Substring(lastIndx, i - lastIndx)));
                _sb.Append(replacementSymbol);
                _sb.Append(SUB_END);
            }
            else
            {
                _sb.Append(formula, lastIndx, i - lastIndx);
                visibleIndx += i - lastIndx;
                if (isCapital & i != 0)
                {
                    _sb.Append(SUB_START);
                    if (visibleIndexes != null) visibleIndexes.Add(visibleIndx++);
                    if (rawIndexes != null) rawIndexes.Add(_sb.Length);
                    if (numberValues != null) numberValues.Add(1);
                    _sb.Append(replacementSymbol);
                    _sb.Append(SUB_END);
                }
            }
            lastIndx = i;
            digitMode = isDigit;
        }
        if (digitMode)
        {
            if (numberValues != null) numberValues.Add(Int32.Parse(formula.Substring(lastIndx, formula.Length - lastIndx)));
        }
        else
        {
            _sb.Append(formula, lastIndx, formula.Length - lastIndx);
            visibleIndx += formula.Length - lastIndx;
            if (numberValues != null) numberValues.Add(1);
        }
        _sb.Append(SUB_START);
        if (visibleIndexes != null) visibleIndexes.Add(visibleIndx++);
        if (rawIndexes != null) rawIndexes.Add(_sb.Length);
        _sb.Append(replacementSymbol);
        _sb.Append(SUB_END);
        var result = _sb.ToString();
        _sb.Remove(0, _sb.Length);
        return result;
    }

    #endregion

    public MelDB.DataType particleType { get {
            if (charge < 0) return MelDB.DataType.Anion;
            else if (charge > 0) return MelDB.DataType.Cation;
            return MelDB.DataType.Molecule;
        } }

    public bool ContainsBondedAtoms(Element a1, Element a2)
    {
        var bnds = bonds;
        var atms = atoms;
        for(int i = 0; i < bnds.Count; i++)
        {
            var b = bnds[i];
            var e1 = atms[b.atom1].element;
            var e2 = atms[b.atom2].element;
            if (e1 == a1 & e2 == a2)
                return true;
            if (e1 == a2 & e2 == a1)
                return true;
        }
        return false;
    }

    public void Init()
    {
        if (data != null)
            return;
        MelDBSerializer.Instance.LoadParticleData(this);
    }
    public IEnumerator InitAsync()
    {
        if (data != null)
            yield break;
        yield return MelDBSerializer.Instance.LoadParticleDataAsync(this);
    }

#if UNITY_EDITOR

    /// <summary>
    /// Do not use! for deserialization only
    /// </summary>
    public ParticleInfo() { }
    public ParticleInfo(bool initData) { id = MelDB.GetRandomId(); if (initData) data = new ParticleInfoData(); }

    [XmlIgnore]
    public Element maxElement
    {
        get
        {
            Init();
            Element max = Element.H;
            for (int i = 0; i < atoms.Count; i++)
                if (atoms[i].element > max)
                    max = atoms[i].element;
            return max;
        }
    }
    
    public bool HashesEqual(ParticleInfo other)
    {
        if (other.structureHashExact != 0 & structureHashExact != 0)
        {
            if (other.structureHashExact != structureHashExact)
                return false;
        }
        else return false;
        if (other.structureHash != 0 & structureHash != 0)
        {
            if (other.structureHash != structureHash)
                return false;
        }
        else return false;
        if (other.atomsHash != 0 & atomsHash != 0)
        {
            if (atomsHash != other.atomsHash)
                return false;
        }
        else return false;
        return true;
    }

    public bool NamesEqual(ParticleInfo other)
    {
        if (!string.IsNullOrEmpty(name) & !string.IsNullOrEmpty(other.name))
            if (name.Equals(other.name, StringComparison.Ordinal))
                return true;

        if (!string.IsNullOrEmpty(primaryName) & !string.IsNullOrEmpty(other.primaryName))
            if (primaryName.Equals(other.primaryName, StringComparison.Ordinal))
                return true;

        if (!string.IsNullOrEmpty(primaryName))
            for (int i = 0; i < other.iupacs.Count; i++)
                if (primaryName.Equals(other.iupacs[i], StringComparison.Ordinal))
                    return true;
        if (!string.IsNullOrEmpty(other.primaryName))
            for (int i = 0; i < iupacs.Count; i++)
                if (other.primaryName.Equals(iupacs[i], StringComparison.Ordinal))
                    return true;
        
        for (int i = 0; i < other.iupacs.Count; i++)
        {
            var otherName = other.iupacs[i];
            for (int j = 0; j < iupacs.Count; j++)
            {
                if (otherName.Equals(iupacs[j], StringComparison.Ordinal))
                    return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Take all new data from other particle. Id, charge and hashes are not taken
    /// </summary>
    /// <param name="source">particle to take data from</param>
    /// <param name="reorderings">temporary buffer</param>
    /// <returns>false if topology mismatch</returns>
    public bool TakeAbsentDataFrom(ParticleInfo source, List<int> reorderings, bool allowAutofix = false)
    {
        Init();
        
        // data copied only if not present in destination
        // id stays the same
        // hashes are not updated, should be recalculated by the caller

        if (string.IsNullOrEmpty(name))
            name = source.name;
        if (CID == 0)
            CID = source.CID;
        for (int i = 0; i < source.CASes.Count; i++)
            if (!CASes.Contains(source.CASes[i]))
                CASes.Add(source.CASes[i]);
        if ((flags & ParticleFlags.HasChemicalFormula) == 0)
        {
            chemicalFormula = source.chemicalFormula;
            flags |= (source.flags & ParticleFlags.HasChemicalFormula);
        }

        flags |= (source.flags & ParticleFlags.HasSkeletalFormula);
        flags |= (source.flags & ParticleFlags.ShowInExplorer);
        flags |= (source.flags & ParticleFlags.ShowInConstructor);
        flags |= (source.flags & ParticleFlags.ForceIncludeInBuild);

        bool success = true;
        var structureFlags = ParticleFlags.Has3D | ParticleFlags.Has2D;

        if ((flags & structureFlags) != structureFlags  // there is something missing
            && (source.flags & structureFlags) > 0)     // there is something to copy
        {
            var sAtoms = source.data.atoms;
            if ((flags & structureFlags) == 0)          // everything is missing
            {
                // just copy the whole data
                data.atoms = DeepCopy(sAtoms);
                data.bonds = DeepCopy(source.bonds);
                // copy some flags from source
                var flagsToCopy = ParticleFlags.HasParticleCharge | ParticleFlags.HasAtomCharges
                    | ParticleFlags.Has2D | ParticleFlags.Has3D | ParticleFlags.HasTopology
                    | ParticleFlags.HasRadicalAtoms;
                flags &= ~flagsToCopy;
                flags |= (source.flags & flagsToCopy);
            }
            else
            {
                var atoms = data.atoms;
                success = ParticleComparer.AreEqual(this, source, true, reorderings);
                if (!success & allowAutofix)
                    success = ParticleComparer.AreEqual(this, source, false, reorderings);
                if (success)
                {
                    if ((flags & ParticleFlags.Has2D) == 0
                        & (source.flags & ParticleFlags.Has2D) > 0)
                    {
                        // we should take 2d from source, but keep our 3d
                        for (int i = atoms.Count - 1; i >= 0; i--)
                            atoms[i].flatPosition = sAtoms[reorderings[i]].flatPosition;
                        flags |= ParticleFlags.Has2D | ParticleFlags.HasTopology;
                    }
                    else if ((flags & ParticleFlags.Has3D) == 0
                        & (source.flags & ParticleFlags.Has3D) > 0)
                    {
                        // we should take 3d from source, but keep our 2d
                        for (int i = atoms.Count - 1; i >= 0; i--)
                            atoms[i].position = sAtoms[reorderings[i]].position;
                        flags |= ParticleFlags.Has3D | ParticleFlags.HasTopology;
                    }
                    // take charges
                    if ((source.flags & ParticleFlags.HasAtomCharges) > 0)
                    {
                        flags |= (source.flags & (ParticleFlags.HasParticleCharge | ParticleFlags.HasAtomCharges));
                        for (int i = atoms.Count - 1; i >= 0; i--)
                            if (atoms[i].atomCharge == 0)
                                atoms[i].atomCharge = sAtoms[reorderings[i]].atomCharge;
                    }
                    // take radicals
                    if ((source.flags & ParticleFlags.HasRadicalAtoms) > 0)
                    {
                        flags |= (source.flags & ParticleFlags.HasRadicalAtoms);
                        for (int i = atoms.Count - 1; i >= 0; i--)
                            if (atoms[i].radical == 0)
                                atoms[i].radical = sAtoms[reorderings[i]].radical;
                    }
                }
            }
        }
        
        if (string.IsNullOrEmpty(primaryName))
            primaryName = source.primaryName;

        var sSynonyms = source.iupacs;
        for (int i = 0; i < sSynonyms.Count; i++)
        {
            var syn = sSynonyms[i];
            if (syn.Equals(primaryName, StringComparison.Ordinal))
                continue;
            if (iupacs.Contains(syn))
                continue;
            iupacs.Add(syn);
        }

        return success;
    }
    public ParticleInfo CreateCopy()
    {
        Init();
        var p = (ParticleInfo)MemberwiseClone();
        p.CASes = new List<uint>(CASes);
        var d = new ParticleInfoData();
        p.data = d;
        d.atoms = DeepCopy(data.atoms);
        d.bonds = DeepCopy(data.bonds);
        p.iupacs = DeepCopy(iupacs);
        return p;
    }
    private List<T> DeepCopy<T>(List<T> list) where T : class
    {
        System.Reflection.MethodInfo dynMethod = this.GetType().GetMethod("MemberwiseClone",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var result = new List<T>(list.Count);
        for (int i = 0; i < list.Count; i++)
            result.Add((T)dynMethod.Invoke(list[i], null));
        return result;
    }

    /// <summary>
    /// For test purposes only! do not use in client code
    /// </summary>
    public void Shuffle()
    {
        var newIndxs = new int[data.atoms.Count];
        var keys = new float[newIndxs.Length];
        for (int i = 0; i < keys.Length; i++)
        {
            newIndxs[i] = i;
            keys[i] = UnityEngine.Random.value;
        }
        Array.Sort(keys, newIndxs);

        var newAtoms = new List<AtomInfo>(keys.Length);
        var newIndxs2 = new short[data.atoms.Count];
        for (short i = 0; i < keys.Length; i++)
        {
            newAtoms.Add(data.atoms[newIndxs[i]]);
            newIndxs2[newIndxs[i]] = i;
        }
        data.atoms = newAtoms;

        for (int i = 0; i < data.bonds.Count; i++)
        {
            var bond = data.bonds[i];
            bond.atom1 = newIndxs2[bond.atom1];
            bond.atom2 = newIndxs2[bond.atom2];
        }
    }
#endif

    #region molecular formula orderings
    
    public static Element[] GetMolecularFormulaOrderingFor(ParticleFlags flags)
    {
        bool flag1 = (flags & ParticleFlags.MolecularFormula_1) > 0;
        bool flag2 = (flags & ParticleFlags.MolecularFormula_2) > 0;
        if (!flag1 & !flag2)
            return ordering_C_H_alphabetical;
        if (!flag1 & flag2)
            return ordering_H_alphabetical_O;
        if (!flag2)
            return ordering_electronegativity;
        return ordering_electronegativity_reversed;
    }

    private static Element[] ordering_C_H_alphabetical = new[]
    {
        Element.C,
        Element.H,
        Element.Ac,
        Element.Ag,
        Element.Al,
        Element.Am,
        Element.Ar,
        Element.As,
        Element.At,
        Element.Au,
        Element.B,
        Element.Ba,
        Element.Be,
        Element.Bh,
        Element.Bi,
        Element.Bk,
        Element.Br,
        Element.Ca,
        Element.Cd,
        Element.Ce,
        Element.Cf,
        Element.Cl,
        Element.Cm,
        Element.Cn,
        Element.Co,
        Element.Cr,
        Element.Cs,
        Element.Cu,
        Element.Db,
        Element.Ds,
        Element.Dy,
        Element.Er,
        Element.Es,
        Element.Eu,
        Element.F,
        Element.Fe,
        Element.Fl,
        Element.Fm,
        Element.Fr,
        Element.Ga,
        Element.Gd,
        Element.Ge,
        Element.He,
        Element.Hf,
        Element.Hg,
        Element.Ho,
        Element.Hs,
        Element.I,
        Element.In,
        Element.Ir,
        Element.K,
        Element.Kr,
        Element.La,
        Element.Li,
        Element.Lr,
        Element.Lu,
        Element.Lv,
        Element.Mc,
        Element.Md,
        Element.Mg,
        Element.Mn,
        Element.Mo,
        Element.Mt,
        Element.N,
        Element.Na,
        Element.Nb,
        Element.Nd,
        Element.Ne,
        Element.Nh,
        Element.Ni,
        Element.No,
        Element.Np,
        Element.O,
        Element.Og,
        Element.Os,
        Element.P,
        Element.Pa,
        Element.Pb,
        Element.Pd,
        Element.Pm,
        Element.Po,
        Element.Pr,
        Element.Pt,
        Element.Pu,
        Element.Ra,
        Element.Rb,
        Element.Re,
        Element.Rf,
        Element.Rg,
        Element.Rh,
        Element.Rn,
        Element.Ru,
        Element.S,
        Element.Sb,
        Element.Sc,
        Element.Se,
        Element.Sg,
        Element.Si,
        Element.Sm,
        Element.Sn,
        Element.Sr,
        Element.Ta,
        Element.Tb,
        Element.Tc,
        Element.Te,
        Element.Th,
        Element.Ti,
        Element.Tl,
        Element.Tm,
        Element.Ts,
        Element.U,
        Element.V,
        Element.W,
        Element.Xe,
        Element.Y,
        Element.Yb,
        Element.Zn,
        Element.Zr,
    };
    private static Element[] ordering_H_alphabetical_O = new[]
    {
        Element.H,
        Element.Ac,
        Element.Ag,
        Element.Al,
        Element.Am,
        Element.Ar,
        Element.As,
        Element.At,
        Element.Au,
        Element.B,
        Element.Ba,
        Element.Be,
        Element.Bh,
        Element.Bi,
        Element.Bk,
        Element.Br,
        Element.C,
        Element.Ca,
        Element.Cd,
        Element.Ce,
        Element.Cf,
        Element.Cl,
        Element.Cm,
        Element.Cn,
        Element.Co,
        Element.Cr,
        Element.Cs,
        Element.Cu,
        Element.Db,
        Element.Ds,
        Element.Dy,
        Element.Er,
        Element.Es,
        Element.Eu,
        Element.F,
        Element.Fe,
        Element.Fl,
        Element.Fm,
        Element.Fr,
        Element.Ga,
        Element.Gd,
        Element.Ge,
        Element.He,
        Element.Hf,
        Element.Hg,
        Element.Ho,
        Element.Hs,
        Element.I,
        Element.In,
        Element.Ir,
        Element.K,
        Element.Kr,
        Element.La,
        Element.Li,
        Element.Lr,
        Element.Lu,
        Element.Lv,
        Element.Mc,
        Element.Md,
        Element.Mg,
        Element.Mn,
        Element.Mo,
        Element.Mt,
        Element.N,
        Element.Na,
        Element.Nb,
        Element.Nd,
        Element.Ne,
        Element.Nh,
        Element.Ni,
        Element.No,
        Element.Np,
        Element.Og,
        Element.Os,
        Element.P,
        Element.Pa,
        Element.Pb,
        Element.Pd,
        Element.Pm,
        Element.Po,
        Element.Pr,
        Element.Pt,
        Element.Pu,
        Element.Ra,
        Element.Rb,
        Element.Re,
        Element.Rf,
        Element.Rg,
        Element.Rh,
        Element.Rn,
        Element.Ru,
        Element.S,
        Element.Sb,
        Element.Sc,
        Element.Se,
        Element.Sg,
        Element.Si,
        Element.Sm,
        Element.Sn,
        Element.Sr,
        Element.Ta,
        Element.Tb,
        Element.Tc,
        Element.Te,
        Element.Th,
        Element.Ti,
        Element.Tl,
        Element.Tm,
        Element.Ts,
        Element.U,
        Element.V,
        Element.W,
        Element.Xe,
        Element.Y,
        Element.Yb,
        Element.Zn,
        Element.Zr,
        Element.O,
    };
    private static Element[] ordering_electronegativity = new[]
    {
        // source: https://en.wikipedia.org/wiki/Electronegativity
        Element.Fr,  	// 0.70
        Element.Cs,  	// 0.79
        Element.K,  	// 0.82
        Element.Rb,  	// 0.82
        Element.Ba,  	// 0.89
        Element.Ra,  	// 0.90
        Element.Na,  	// 0.93
        Element.Sr,  	// 0.95
        Element.Li,  	// 0.98
        Element.Ca,  	// 1.00
        Element.Ac,  	// 1.10
        Element.La,  	// 1.10
        Element.Tb,  	// 1.10
        Element.Yb,  	// 1.10
        Element.Ce,  	// 1.12
        Element.Am,  	// 1.13
        Element.Pm,  	// 1.13
        Element.Pr,  	// 1.13
        Element.Nd,  	// 1.14
        Element.Sm,  	// 1.17
        Element.Eu,  	// 1.20
        Element.Gd,  	// 1.20
        Element.Dy,  	// 1.22
        Element.Y,  	// 1.22
        Element.Ho,  	// 1.23
        Element.Er,  	// 1.24
        Element.Tm,  	// 1.25
        Element.Lu,  	// 1.27
        Element.Cm,  	// 1.28
        Element.Pu,  	// 1.28
        Element.Bk,  	// 1.30
        Element.Cf,  	// 1.30
        Element.Es,  	// 1.30
        Element.Fm,  	// 1.30
        Element.Hf,  	// 1.30
        Element.Rf,  	// unknown
        Element.Lr,  	// 1.30
        Element.Md,  	// 1.30
        Element.No,  	// 1.30
        Element.Th,  	// 1.30
        Element.Mg,  	// 1.31
        Element.Zr,  	// 1.33
        Element.Np,  	// 1.36
        Element.Sc,  	// 1.36
        Element.U,  	// 1.38
        Element.Pa,  	// 1.50
        Element.Ta,  	// 1.50
        Element.Db,  	// unknown
        Element.Ti,  	// 1.54
        Element.Mn,  	// 1.55
        Element.Be,  	// 1.57
        Element.Nb,  	// 1.60
        Element.Al,  	// 1.61
        Element.Tl,  	// 1.62
        Element.Nh,  	// unknown
        Element.V,  	// 1.63
        Element.Zn,  	// 1.65
        Element.Cr,  	// 1.66
        Element.Cd,  	// 1.69
        Element.In,  	// 1.78
        Element.Ga,  	// 1.81
        Element.Fe,  	// 1.83
        Element.Pb,  	// 1.87
        Element.Fl,  	// unknown
        Element.Co,  	// 1.88
        Element.Cu,  	// 1.90
        Element.Re,  	// 1.90
        Element.Bh,  	// unknown
        Element.Si,  	// 1.90
        Element.Tc,  	// 1.90
        Element.Ni,  	// 1.91
        Element.Ag,  	// 1.93
        Element.Sn,  	// 1.96
        Element.Hg,  	// 2.00
        Element.Cn,  	// unknown
        Element.Po,  	// 2.00
        Element.Lv,  	// unknown
        Element.Ge,  	// 2.01
        Element.Bi,  	// 2.02
        Element.Mc,  	// unknown
        Element.B,  	// 2.04
        Element.Sb,  	// 2.05
        Element.Te,  	// 2.10
        Element.Mo,  	// 2.16
        Element.As,  	// 2.18
        Element.P,  	// 2.19
        Element.At,  	// 2.20
        Element.Ts,  	// unknown
        Element.H,  	// 2.20
        Element.Ir,  	// 2.20
        Element.Mt,  	// unknown
        Element.Os,  	// 2.20
        Element.Og,  	// unknown
        Element.Hs,  	// unknown
        Element.Pd,  	// 2.20
        Element.Rn,  	// 2.20
        Element.Ru,  	// 2.20
        Element.Pt,  	// 2.28
        Element.Ds,  	// unknown
        Element.Rh,  	// 2.28
        Element.W,  	// 2.36
        Element.Sg,  	// unknown
        Element.Au,  	// 2.54
        Element.Rg,  	// unknown
        Element.C,  	// 2.55
        Element.Se,  	// 2.55
        Element.S,  	// 2.58
        Element.Xe,  	// 2.60
        Element.I,  	// 2.66
        Element.Br,  	// 2.96
        Element.Kr,  	// 3.00
        Element.N,  	// 3.04
        Element.Cl,  	// 3.16
        Element.O,  	// 3.44
        Element.F,  	// 3.98
        Element.Ar,  	// unknown
        Element.Ne,  	// unknown
        Element.He,  	// unknown
    };
    private static Element[] ordering_electronegativity_reversed = new[]
    {
        // source: https://en.wikipedia.org/wiki/Electronegativity
        Element.He,     // unknown
        Element.Ne,     // unknown
        Element.Ar,     // unknown
        Element.F,      // 3.98
        Element.O,      // 3.44
        Element.Cl,     // 3.16
        Element.N,      // 3.04
        Element.Kr,     // 3.00
        Element.Br,     // 2.96
        Element.I,      // 2.66
        Element.Xe,     // 2.60
        Element.S,      // 2.58
        Element.Se,     // 2.55
        Element.C,      // 2.55
        Element.Rg,     // unknown
        Element.Au,     // 2.54
        Element.Sg,     // unknown
        Element.W,      // 2.36
        Element.Rh,     // 2.28
        Element.Ds,     // unknown
        Element.Pt,     // 2.28
        Element.Ru,     // 2.20
        Element.Rn,     // 2.20
        Element.Pd,     // 2.20
        Element.Hs,     // unknown
        Element.Og,     // unknown
        Element.Os,     // 2.20
        Element.Mt,     // unknown
        Element.Ir,     // 2.20
        Element.H,      // 2.20
        Element.Ts,     // unknown
        Element.At,     // 2.20
        Element.P,      // 2.19
        Element.As,     // 2.18
        Element.Mo,     // 2.16
        Element.Te,     // 2.10
        Element.Sb,     // 2.05
        Element.B,      // 2.04
        Element.Mc,     // unknown
        Element.Bi,     // 2.02
        Element.Ge,     // 2.01
        Element.Lv,     // unknown
        Element.Po,     // 2.00
        Element.Cn,     // unknown
        Element.Hg,     // 2.00
        Element.Sn,     // 1.96
        Element.Ag,     // 1.93
        Element.Ni,     // 1.91
        Element.Tc,     // 1.90
        Element.Si,     // 1.90
        Element.Bh,     // unknown
        Element.Re,     // 1.90
        Element.Cu,     // 1.90
        Element.Co,     // 1.88
        Element.Fl,     // unknown
        Element.Pb,     // 1.87
        Element.Fe,     // 1.83
        Element.Ga,     // 1.81
        Element.In,     // 1.78
        Element.Cd,     // 1.69
        Element.Cr,     // 1.66
        Element.Zn,     // 1.65
        Element.V,      // 1.63
        Element.Nh,     // unknown
        Element.Tl,     // 1.62
        Element.Al,     // 1.61
        Element.Nb,     // 1.60
        Element.Be,     // 1.57
        Element.Mn,     // 1.55
        Element.Ti,     // 1.54
        Element.Db,     // unknown
        Element.Ta,     // 1.50
        Element.Pa,     // 1.50
        Element.U,      // 1.38
        Element.Sc,     // 1.36
        Element.Np,     // 1.36
        Element.Zr,     // 1.33
        Element.Mg,     // 1.31
        Element.Th,     // 1.30
        Element.No,     // 1.30
        Element.Md,     // 1.30
        Element.Lr,     // 1.30
        Element.Rf,     // unknown
        Element.Hf,     // 1.30
        Element.Fm,     // 1.30
        Element.Es,     // 1.30
        Element.Cf,     // 1.30
        Element.Bk,     // 1.30
        Element.Pu,     // 1.28
        Element.Cm,     // 1.28
        Element.Lu,     // 1.27
        Element.Tm,     // 1.25
        Element.Er,     // 1.24
        Element.Ho,     // 1.23
        Element.Y,      // 1.22
        Element.Dy,     // 1.22
        Element.Gd,     // 1.20
        Element.Eu,     // 1.20
        Element.Sm,     // 1.17
        Element.Nd,     // 1.14
        Element.Pr,     // 1.13
        Element.Pm,     // 1.13
        Element.Am,     // 1.13
        Element.Ce,     // 1.12
        Element.Yb,     // 1.10
        Element.Tb,     // 1.10
        Element.La,     // 1.10
        Element.Ac,     // 1.10
        Element.Ca,     // 1.00
        Element.Li,     // 0.98
        Element.Sr,     // 0.95
        Element.Na,     // 0.93
        Element.Ra,     // 0.90
        Element.Ba,     // 0.89
        Element.Rb,     // 0.82
        Element.K,      // 0.82
        Element.Cs,     // 0.79
        Element.Fr,     // 0.70
    };
    #endregion
}