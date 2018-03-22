using System;

#region Elements enum

public enum Element : short
{
    Invalid = -1,
    H = 1,
    He,
    Li,
    Be,
    B,
    C,
    N,
    O,
    F,
    Ne,
    Na,
    Mg,
    Al,
    Si,
    P,
    S,
    Cl,
    Ar,
    K,
    Ca,
    Sc,
    Ti,
    V,
    Cr,
    Mn,
    Fe,
    Co,
    Ni,
    Cu,
    Zn,
    Ga,
    Ge,
    As,
    Se,
    Br,
    Kr,
    Rb,
    Sr,
    Y,
    Zr,
    Nb,
    Mo,
    Tc,
    Ru,
    Rh,
    Pd,
    Ag,
    Cd,
    In,
    Sn,
    Sb,
    Te,
    I,
    Xe,
    Cs,
    Ba,
    La,
    Ce,
    Pr,
    Nd,
    Pm,
    Sm,
    Eu,
    Gd,
    Tb,
    Dy,
    Ho,
    Er,
    Tm,
    Yb,
    Lu,
    Hf,
    Ta,
    W,
    Re,
    Os,
    Ir,
    Pt,
    Au,
    Hg,
    Tl,
    Pb,
    Bi,
    Po,
    At,
    Rn,
    Fr,
    Ra,
    Ac,
    Th,
    Pa,
    U,
    Np,
    Pu,
    Am,
    Cm,
    Bk,
    Cf,
    Es,
    Fm,
    Md,
    No,
    Lr,
    Rf,
    Db,
    Sg,
    Bh,
    Hs,
    Mt,
    Ds,
    Rg,
    Cn,
    Nh,
    Fl,
    Mc,
    Lv,
    Ts,
    Og,
    MAX,
    // known but unused
#if EXTENDED_ATOM_INFO
    ,

    // unknown or not synthesized
    Uue,
    Ubn,
    Ubu,
    Ubb,
    Ubt,
    Ubq,
    Ubp,
    Ubh,
    Ubs,
    Ubo,
    Ube,
    Utn,
    Utu,
    Utb,
    Utt,
    Utq,
    Utp,
    Uth,
    Uts,
    Uto,
    Ute,
    Uqn,
    Uqu,
    Uqb,
    Uqt,
    Uqq,
    Uqp,
    Uqh,
    Uqs,
    Uqo,
    Uqe,
    Upn,
    Upu,
    Upb,
    Upt,
    Upq,
    Upp,
    Uph,
    Ups,
    Upo,
    Upe,
    Uhn,
    Uhu,
    Uhb,
    Uht,
    Uhq,
    Uhp,
    Uhh,
    Uhs,
    Uho,
    Uhe,
    Usn,
    Usu,
    Usb,
    // last possible element
    Ust,
#endif
}

public enum ElementsInternalConstants
{
    AtomConstructorLimit = Element.Cm
}

public enum ElOccurrence
{
    Primordial = 1,
    Transient = 2,
    Synthetic = -1,
}

public enum ElKind
{
    Unknown = -1,
    AlkaliMetal,
    AlkalineEarthMetal,
    TransitionMetal,
    Metal,
    Metalloid,
    Nonmetal,
    Halogen,
    NobleGas,
    Lanthanide,
    Actinide,
    MAX,
}
#endregion

public enum OrbitalNumber
{
    Any = -255,
    Invalid = -1,
    S = 0,
    P = 1,
    D = 2,
    F = 3,
    G = 4,
    H = 5,
    I = 6,
    Max = 7
};

public partial class ElementInfo
{
    // Element enumeration value.
    public Element Element { get; private set; }

    // Z, Atomic number - the number of protons found in the nucleus of an atom of that element
    public int AtomicNumber { get { return (int)Element; } }

    // A symbol is a code for a chemical element. Chemical symbols are one to three letters long, 
    // and are written with only the first letter capitalized.
    public string Symbol
    {
        get { return Element.ToString("G"); }
    }

    // A period is one of the horizontal rows in the periodic table, all of whose elements
    // have the same number of electron shells. Going across a period, each element has
    // one more proton and is less metallic than its predecessor.
    public int Period { get; private set; }

    // Group (also known as a family) is a column of elements in the periodic table 
    // of the chemical elements. There are 18 numbered groups in the periodic table, 
    // but the f-block columns (between groups 2 and 3) are not numbered.
    public int Group { get; private set; }

    // English element name (localization somewhat later).
    public string Name { get; private set; }

    public OrbitalNumber Block { get; private set; }

    public ElOccurrence Occurrence { get; private set; }

    public ElKind Kind { get; private set; }

    // Default display color (https://en.wikipedia.org/wiki/CPK_coloring)
    // Int32 to avoid Color dependency
    public Int32 DefaultColor { get; private set; }

    // Display radius, not really corellated to "real" element radius
    public float RenderingRadius { get; private set; }
    
    private ElementInfo(Element element, int period, int group, OrbitalNumber block, ElOccurrence occurrence, ElKind kind, string name, Int32 color, float rendRadius)
    {
        Element = element;
        Period = period;
        Group = group;
        Block = block;
        Occurrence = occurrence;
        Kind = kind;
        Name = name;
        DefaultColor = color;
        RenderingRadius = rendRadius;
    }

    public static ElementInfo Get(Element element)
    {
        return elements[(int)element];
    }
    public static ElementInfo Get(int element)
    {
        return elements[element];
    }

#region Elements static array

    private static ElementInfo[] elements = new ElementInfo[] {
        null,
        new ElementInfo(Element.H,      1,  1,      OrbitalNumber.S,    ElOccurrence.Primordial,  ElKind.Nonmetal,            "Hydrogen",         0xffffff,   0.50f),
        new ElementInfo(Element.He,     1,  18,     OrbitalNumber.S,    ElOccurrence.Primordial,  ElKind.NobleGas,            "Helium",           0xd9ffff,   1.00f),
        new ElementInfo(Element.Li,     2,  1,      OrbitalNumber.S,    ElOccurrence.Primordial,  ElKind.AlkaliMetal,         "Lithium",          0xcc80ff,   1.00f),
        new ElementInfo(Element.Be,     2,  2,      OrbitalNumber.S,    ElOccurrence.Primordial,  ElKind.AlkalineEarthMetal,  "Beryllium",        0xc2ff00,   1.00f),
        new ElementInfo(Element.B,      2,  13,     OrbitalNumber.P,    ElOccurrence.Primordial,  ElKind.Metalloid,           "Boron",            0xffb5b5,   1.00f),
        new ElementInfo(Element.C,      2,  14,     OrbitalNumber.P,    ElOccurrence.Primordial,  ElKind.Nonmetal,            "Carbon",           0x323232,   1.00f),
        new ElementInfo(Element.N,      2,  15,     OrbitalNumber.P,    ElOccurrence.Primordial,  ElKind.Nonmetal,            "Nitrogen",         0x3050f8,   1.00f),
        new ElementInfo(Element.O,      2,  16,     OrbitalNumber.P,    ElOccurrence.Primordial,  ElKind.Nonmetal,            "Oxygen",           0xff0d0d,   1.00f),
        new ElementInfo(Element.F,      2,  17,     OrbitalNumber.P,    ElOccurrence.Primordial,  ElKind.Halogen,             "Fluorine",         0x90e050,   1.00f),
        new ElementInfo(Element.Ne,     2,  18,     OrbitalNumber.P,    ElOccurrence.Primordial,  ElKind.NobleGas,            "Neon",             0xb3e3f5,   1.00f),
        new ElementInfo(Element.Na,     3,  1,      OrbitalNumber.S,    ElOccurrence.Primordial,  ElKind.AlkaliMetal,         "Sodium",           0xab5cf2,   1.00f),
        new ElementInfo(Element.Mg,     3,  2,      OrbitalNumber.S,    ElOccurrence.Primordial,  ElKind.AlkalineEarthMetal,  "Magnesium",        0x8aff00,   1.00f),
        new ElementInfo(Element.Al,     3,  13,     OrbitalNumber.P,    ElOccurrence.Primordial,  ElKind.Metal,               "Aluminium",        0xbfa6a6,   1.00f),
        new ElementInfo(Element.Si,     3,  14,     OrbitalNumber.P,    ElOccurrence.Primordial,  ElKind.Metalloid,           "Silicon",          0xf0c8a0,   1.00f),
        new ElementInfo(Element.P,      3,  15,     OrbitalNumber.P,    ElOccurrence.Primordial,  ElKind.Nonmetal,            "Phosphorus",       0xff8000,   1.00f),
        new ElementInfo(Element.S,      3,  16,     OrbitalNumber.P,    ElOccurrence.Primordial,  ElKind.Nonmetal,            "Sulfur",           0xffff30,   1.00f),
        new ElementInfo(Element.Cl,     3,  17,     OrbitalNumber.P,    ElOccurrence.Primordial,  ElKind.Halogen,             "Chlorine",         0x1ff01f,   1.00f),
        new ElementInfo(Element.Ar,     3,  18,     OrbitalNumber.P,    ElOccurrence.Primordial,  ElKind.NobleGas,            "Argon",            0x80d1e3,   1.00f),
        new ElementInfo(Element.K,      4,  1,      OrbitalNumber.S,    ElOccurrence.Primordial,  ElKind.AlkaliMetal,         "Potassium",        0x8f40d4,   1.00f),
        new ElementInfo(Element.Ca,     4,  2,      OrbitalNumber.S,    ElOccurrence.Primordial,  ElKind.AlkalineEarthMetal,  "Calcium",          0x3dff00,   1.00f),
        new ElementInfo(Element.Sc,     4,  3,      OrbitalNumber.D,    ElOccurrence.Primordial,  ElKind.TransitionMetal,     "Scandium",         0xe6e6e6,   1.00f),
        new ElementInfo(Element.Ti,     4,  4,      OrbitalNumber.D,    ElOccurrence.Primordial,  ElKind.TransitionMetal,     "Titanium",         0xbfc2c7,   1.00f),
        new ElementInfo(Element.V,      4,  5,      OrbitalNumber.D,    ElOccurrence.Primordial,  ElKind.TransitionMetal,     "Vanadium",         0xa6a6ab,   1.00f),
        new ElementInfo(Element.Cr,     4,  6,      OrbitalNumber.D,    ElOccurrence.Primordial,  ElKind.TransitionMetal,     "Chromium",         0x8a99c7,   1.00f),
        new ElementInfo(Element.Mn,     4,  7,      OrbitalNumber.D,    ElOccurrence.Primordial,  ElKind.TransitionMetal,     "Manganese",        0x9c7ac7,   1.00f),
        new ElementInfo(Element.Fe,     4,  8,      OrbitalNumber.D,    ElOccurrence.Primordial,  ElKind.TransitionMetal,     "Iron",             0xe06633,   1.00f),
        new ElementInfo(Element.Co,     4,  9,      OrbitalNumber.D,    ElOccurrence.Primordial,  ElKind.TransitionMetal,     "Cobalt",           0xf090a0,   1.00f),
        new ElementInfo(Element.Ni,     4,  10,     OrbitalNumber.D,    ElOccurrence.Primordial,  ElKind.TransitionMetal,     "Nickel",           0x50d050,   1.00f),
        new ElementInfo(Element.Cu,     4,  11,     OrbitalNumber.D,    ElOccurrence.Primordial,  ElKind.TransitionMetal,     "Copper",           0xc88033,   1.00f),
        new ElementInfo(Element.Zn,     4,  12,     OrbitalNumber.D,    ElOccurrence.Primordial,  ElKind.TransitionMetal,     "Zinc",             0x7d80b0,   1.00f),
        new ElementInfo(Element.Ga,     4,  13,     OrbitalNumber.P,    ElOccurrence.Primordial,  ElKind.Metal,               "Gallium",          0xc28f8f,   1.00f),
        new ElementInfo(Element.Ge,     4,  14,     OrbitalNumber.P,    ElOccurrence.Primordial,  ElKind.Metalloid,           "Germanium",        0x668f8f,   1.00f),
        new ElementInfo(Element.As,     4,  15,     OrbitalNumber.P,    ElOccurrence.Primordial,  ElKind.Metalloid,           "Arsenic",          0xbd80e3,   1.00f),
        new ElementInfo(Element.Se,     4,  16,     OrbitalNumber.P,    ElOccurrence.Primordial,  ElKind.Nonmetal,            "Selenium",         0xffa100,   1.00f),
        new ElementInfo(Element.Br,     4,  17,     OrbitalNumber.P,    ElOccurrence.Primordial,  ElKind.Halogen,             "Bromine",          0xa62929,   1.00f),
        new ElementInfo(Element.Kr,     4,  18,     OrbitalNumber.P,    ElOccurrence.Primordial,  ElKind.NobleGas,            "Krypton",          0x5cb8d1,   1.00f),
        new ElementInfo(Element.Rb,     5,  1,      OrbitalNumber.S,    ElOccurrence.Primordial,  ElKind.AlkaliMetal,         "Rubidium",         0x702eb0,   1.00f),
        new ElementInfo(Element.Sr,     5,  2,      OrbitalNumber.S,    ElOccurrence.Primordial,  ElKind.AlkalineEarthMetal,  "Strontium",        0x00ff00,   1.00f),
        new ElementInfo(Element.Y,      5,  3,      OrbitalNumber.D,    ElOccurrence.Primordial,  ElKind.TransitionMetal,     "Yttrium",          0x94ffff,   1.00f),
        new ElementInfo(Element.Zr,     5,  4,      OrbitalNumber.D,    ElOccurrence.Primordial,  ElKind.TransitionMetal,     "Zirconium",        0x94e0e0,   1.00f),
        new ElementInfo(Element.Nb,     5,  5,      OrbitalNumber.D,    ElOccurrence.Primordial,  ElKind.TransitionMetal,     "Niobium",          0x73c2c9,   1.00f),
        new ElementInfo(Element.Mo,     5,  6,      OrbitalNumber.D,    ElOccurrence.Primordial,  ElKind.TransitionMetal,     "Molybdenum",       0x54b5b5,   1.00f),
        new ElementInfo(Element.Tc,     5,  7,      OrbitalNumber.D,    ElOccurrence.Transient,   ElKind.TransitionMetal,     "Technetium",       0x3b9e9e,   1.00f),
        new ElementInfo(Element.Ru,     5,  8,      OrbitalNumber.D,    ElOccurrence.Primordial,  ElKind.TransitionMetal,     "Ruthenium",        0x248f8f,   1.00f),
        new ElementInfo(Element.Rh,     5,  9,      OrbitalNumber.D,    ElOccurrence.Primordial,  ElKind.TransitionMetal,     "Rhodium",          0x0a7d8c,   1.00f),
        new ElementInfo(Element.Pd,     5,  10,     OrbitalNumber.D,    ElOccurrence.Primordial,  ElKind.TransitionMetal,     "Palladium",        0x006985,   1.00f),
        new ElementInfo(Element.Ag,     5,  11,     OrbitalNumber.D,    ElOccurrence.Primordial,  ElKind.TransitionMetal,     "Silver",           0xc0c0c0,   1.00f),
        new ElementInfo(Element.Cd,     5,  12,     OrbitalNumber.D,    ElOccurrence.Primordial,  ElKind.TransitionMetal,     "Cadmium",          0xffd98f,   1.00f),
        new ElementInfo(Element.In,     5,  13,     OrbitalNumber.P,    ElOccurrence.Primordial,  ElKind.Metal,               "Indium",           0xa67573,   1.00f),
        new ElementInfo(Element.Sn,     5,  14,     OrbitalNumber.P,    ElOccurrence.Primordial,  ElKind.Metal,               "Tin",              0x668080,   1.00f),
        new ElementInfo(Element.Sb,     5,  15,     OrbitalNumber.P,    ElOccurrence.Primordial,  ElKind.Metalloid,           "Antimony",         0x9e63b5,   1.00f),
        new ElementInfo(Element.Te,     5,  16,     OrbitalNumber.P,    ElOccurrence.Primordial,  ElKind.Metalloid,           "Tellurium",        0xd47a00,   1.00f),
        new ElementInfo(Element.I,      5,  17,     OrbitalNumber.P,    ElOccurrence.Primordial,  ElKind.Halogen,             "Iodine",           0x940094,   1.00f),
        new ElementInfo(Element.Xe,     5,  18,     OrbitalNumber.P,    ElOccurrence.Primordial,  ElKind.NobleGas,            "Xenon",            0x429eb0,   1.00f),
        new ElementInfo(Element.Cs,     6,  1,      OrbitalNumber.S,    ElOccurrence.Primordial,  ElKind.AlkaliMetal,         "Caesium",          0x57178f,   1.00f),
        new ElementInfo(Element.Ba,     6,  2,      OrbitalNumber.S,    ElOccurrence.Primordial,  ElKind.AlkalineEarthMetal,  "Barium",           0x00c900,   1.00f),
        new ElementInfo(Element.La,     6,  -1,     OrbitalNumber.F,    ElOccurrence.Primordial,  ElKind.Lanthanide,          "Lanthanum",        0x70d4ff,   1.00f),
        new ElementInfo(Element.Ce,     6,  -1,     OrbitalNumber.F,    ElOccurrence.Primordial,  ElKind.Lanthanide,          "Cerium",           0xffffc7,   1.00f),
        new ElementInfo(Element.Pr,     6,  -1,     OrbitalNumber.F,    ElOccurrence.Primordial,  ElKind.Lanthanide,          "Praseodymium",     0xd9ffc7,   1.00f),
        new ElementInfo(Element.Nd,     6,  -1,     OrbitalNumber.F,    ElOccurrence.Primordial,  ElKind.Lanthanide,          "Neodymium",        0xc7ffc7,   1.00f),
        new ElementInfo(Element.Pm,     6,  -1,     OrbitalNumber.F,    ElOccurrence.Transient,   ElKind.Lanthanide,          "Promethium",       0xa3ffc7,   1.00f),
        new ElementInfo(Element.Sm,     6,  -1,     OrbitalNumber.F,    ElOccurrence.Primordial,  ElKind.Lanthanide,          "Samarium",         0x8fffc7,   1.00f),
        new ElementInfo(Element.Eu,     6,  -1,     OrbitalNumber.F,    ElOccurrence.Primordial,  ElKind.Lanthanide,          "Europium",         0x61ffc7,   1.00f),
        new ElementInfo(Element.Gd,     6,  -1,     OrbitalNumber.F,    ElOccurrence.Primordial,  ElKind.Lanthanide,          "Gadolinium",       0x45ffc7,   1.00f),
        new ElementInfo(Element.Tb,     6,  -1,     OrbitalNumber.F,    ElOccurrence.Primordial,  ElKind.Lanthanide,          "Terbium",          0x30ffc7,   1.00f),
        new ElementInfo(Element.Dy,     6,  -1,     OrbitalNumber.F,    ElOccurrence.Primordial,  ElKind.Lanthanide,          "Dysprosium",       0x1fffc7,   1.00f),
        new ElementInfo(Element.Ho,     6,  -1,     OrbitalNumber.F,    ElOccurrence.Primordial,  ElKind.Lanthanide,          "Holmium",          0x00ff9c,   1.00f),
        new ElementInfo(Element.Er,     6,  -1,     OrbitalNumber.F,    ElOccurrence.Primordial,  ElKind.Lanthanide,          "Erbium",           0x00e675,   1.00f),
        new ElementInfo(Element.Tm,     6,  -1,     OrbitalNumber.F,    ElOccurrence.Primordial,  ElKind.Lanthanide,          "Thulium",          0x00d452,   1.00f),
        new ElementInfo(Element.Yb,     6,  -1,     OrbitalNumber.F,    ElOccurrence.Primordial,  ElKind.Lanthanide,          "Ytterbium",        0x00bf38,   1.00f),
        new ElementInfo(Element.Lu,     6,  3,    OrbitalNumber.F/*D*/, ElOccurrence.Primordial,  ElKind.Lanthanide,          "Lutetium",         0x00ab24,   1.00f),
        new ElementInfo(Element.Hf,     6,  4,      OrbitalNumber.D,    ElOccurrence.Primordial,  ElKind.TransitionMetal,     "Hafnium",          0x4dc2ff,   1.00f),
        new ElementInfo(Element.Ta,     6,  5,      OrbitalNumber.D,    ElOccurrence.Primordial,  ElKind.TransitionMetal,     "Tantalum",         0x4da6ff,   1.00f),
        new ElementInfo(Element.W,      6,  6,      OrbitalNumber.D,    ElOccurrence.Primordial,  ElKind.TransitionMetal,     "Tungsten",         0x2194d6,   1.00f),
        new ElementInfo(Element.Re,     6,  7,      OrbitalNumber.D,    ElOccurrence.Primordial,  ElKind.TransitionMetal,     "Rhenium",          0x267dab,   1.00f),
        new ElementInfo(Element.Os,     6,  8,      OrbitalNumber.D,    ElOccurrence.Primordial,  ElKind.TransitionMetal,     "Osmium",           0x266696,   1.00f),
        new ElementInfo(Element.Ir,     6,  9,      OrbitalNumber.D,    ElOccurrence.Primordial,  ElKind.TransitionMetal,     "Iridium",          0x175487,   1.00f),
        new ElementInfo(Element.Pt,     6,  10,     OrbitalNumber.D,    ElOccurrence.Primordial,  ElKind.TransitionMetal,     "Platinum",         0xd0d0e0,   1.00f),
        new ElementInfo(Element.Au,     6,  11,     OrbitalNumber.D,    ElOccurrence.Primordial,  ElKind.TransitionMetal,     "Gold",             0xffd123,   1.00f),
        new ElementInfo(Element.Hg,     6,  12,     OrbitalNumber.D,    ElOccurrence.Primordial,  ElKind.TransitionMetal,     "Mercury",          0xb8b8d0,   1.00f),
        new ElementInfo(Element.Tl,     6,  13,     OrbitalNumber.P,    ElOccurrence.Primordial,  ElKind.Metal,               "Thallium",         0xa6544d,   1.00f),
        new ElementInfo(Element.Pb,     6,  14,     OrbitalNumber.P,    ElOccurrence.Primordial,  ElKind.Metal,               "Lead",             0x575961,   1.00f),
        new ElementInfo(Element.Bi,     6,  15,     OrbitalNumber.P,    ElOccurrence.Primordial,  ElKind.Metal,               "Bismuth",          0x9e4fb5,   1.00f),
        new ElementInfo(Element.Po,     6,  16,     OrbitalNumber.P,    ElOccurrence.Transient,   ElKind.Metal,               "Polonium",         0xab5c00,   1.00f),
        new ElementInfo(Element.At,     6,  17,     OrbitalNumber.P,    ElOccurrence.Transient,   ElKind.Halogen,             "Astatine",         0x754f45,   1.00f),
        new ElementInfo(Element.Rn,     6,  18,     OrbitalNumber.P,    ElOccurrence.Transient,   ElKind.NobleGas,            "Radon",            0x428296,   1.00f),
        new ElementInfo(Element.Fr,     7,  1,      OrbitalNumber.S,    ElOccurrence.Transient,   ElKind.AlkaliMetal,         "Francium",         0x420066,   1.00f),
        new ElementInfo(Element.Ra,     7,  2,      OrbitalNumber.S,    ElOccurrence.Transient,   ElKind.AlkalineEarthMetal,  "Radium",           0x007d00,   1.00f),
        new ElementInfo(Element.Ac,     7,  -1,     OrbitalNumber.F,    ElOccurrence.Transient,   ElKind.Actinide,            "Actinium",         0x70abfa,   1.00f),
        new ElementInfo(Element.Th,     7,  -1,     OrbitalNumber.F,    ElOccurrence.Primordial,  ElKind.Actinide,            "Thorium",          0x00baff,   1.00f),
        new ElementInfo(Element.Pa,     7,  -1,     OrbitalNumber.F,    ElOccurrence.Transient,   ElKind.Actinide,            "Protactinium",     0x00a1ff,   1.00f),
        new ElementInfo(Element.U,      7,  -1,     OrbitalNumber.F,    ElOccurrence.Primordial,  ElKind.Actinide,            "Uranium",          0x008fff,   1.00f),
        new ElementInfo(Element.Np,     7,  -1,     OrbitalNumber.F,    ElOccurrence.Transient,   ElKind.Actinide,            "Neptunium",        0x0080ff,   1.00f),
        new ElementInfo(Element.Pu,     7,  -1,     OrbitalNumber.F,    ElOccurrence.Primordial,  ElKind.Actinide,            "Plutonium",        0x006bff,   1.00f),
        new ElementInfo(Element.Am,     7,  -1,     OrbitalNumber.F,    ElOccurrence.Transient,   ElKind.Actinide,            "Americium",        0x545cf2,   1.00f),
        new ElementInfo(Element.Cm,     7,  -1,     OrbitalNumber.F,    ElOccurrence.Transient,   ElKind.Actinide,            "Curium",           0x785ce3,   1.00f),
        new ElementInfo(Element.Bk,     7,  -1,     OrbitalNumber.F,    ElOccurrence.Transient,   ElKind.Actinide,            "Berkelium",        0x8a4fe3,   1.00f),
        new ElementInfo(Element.Cf,     7,  -1,     OrbitalNumber.F,    ElOccurrence.Transient,   ElKind.Actinide,            "Californium",      0xa136d4,   1.00f),
        new ElementInfo(Element.Es,     7,  -1,     OrbitalNumber.F,    ElOccurrence.Synthetic,   ElKind.Actinide,            "Einsteinium",      0xb31fd4,   1.00f),
        new ElementInfo(Element.Fm,     7,  -1,     OrbitalNumber.F,    ElOccurrence.Synthetic,   ElKind.Actinide,            "Fermium",          0xb31fba,   1.00f),
        new ElementInfo(Element.Md,     7,  -1,     OrbitalNumber.F,    ElOccurrence.Synthetic,   ElKind.Actinide,            "Mendelevium",      0xb30da6,   1.00f),
        new ElementInfo(Element.No,     7,  -1,     OrbitalNumber.F,    ElOccurrence.Synthetic,   ElKind.Actinide,            "Nobelium",         0xbd0d87,   1.00f),
        new ElementInfo(Element.Lr,     7,  3,    OrbitalNumber.F/*D*/, ElOccurrence.Synthetic,   ElKind.Actinide,            "Lawrencium",       0xc70066,   1.00f),
        new ElementInfo(Element.Rf,     7,  4,      OrbitalNumber.D,    ElOccurrence.Synthetic,   ElKind.TransitionMetal,     "Rutherfordium",    0xcc0059,   1.00f),
        new ElementInfo(Element.Db,     7,  5,      OrbitalNumber.D,    ElOccurrence.Synthetic,   ElKind.TransitionMetal,     "Dubnium",          0xd1004f,   1.00f),
        new ElementInfo(Element.Sg,     7,  6,      OrbitalNumber.D,    ElOccurrence.Synthetic,   ElKind.TransitionMetal,     "Seaborgium",       0xd90045,   1.00f),
        new ElementInfo(Element.Bh,     7,  7,      OrbitalNumber.D,    ElOccurrence.Synthetic,   ElKind.TransitionMetal,     "Bohrium",          0xe00038,   1.00f),
        new ElementInfo(Element.Hs,     7,  8,      OrbitalNumber.D,    ElOccurrence.Synthetic,   ElKind.TransitionMetal,     "Hassium",          0xe6002e,   1.00f),
        new ElementInfo(Element.Mt,     7,  9,      OrbitalNumber.D,    ElOccurrence.Synthetic,   ElKind.Unknown,             "Meitnerium",       0xeb0026,   1.00f),
        new ElementInfo(Element.Ds,     7,  10,     OrbitalNumber.D,    ElOccurrence.Synthetic,   ElKind.Unknown,             "Darmstadtium",     0xffffff,   1.00f),
        new ElementInfo(Element.Rg,     7,  11,     OrbitalNumber.D,    ElOccurrence.Synthetic,   ElKind.Unknown,             "Roentgenium",      0xffffff,   1.00f),
        new ElementInfo(Element.Cn,     7,  12,     OrbitalNumber.D,    ElOccurrence.Synthetic,   ElKind.TransitionMetal,     "Copernicium",      0xffffff,   1.00f),
        new ElementInfo(Element.Nh,     7,  13,     OrbitalNumber.P,    ElOccurrence.Synthetic,   ElKind.Unknown,             "Nihonium",         0xffffff,   1.00f),
        new ElementInfo(Element.Fl,     7,  14,     OrbitalNumber.P,    ElOccurrence.Synthetic,   ElKind.Unknown,             "Flerovium",        0xffffff,   1.00f),
        new ElementInfo(Element.Mc,     7,  15,     OrbitalNumber.P,    ElOccurrence.Synthetic,   ElKind.Unknown,             "Moscovium",        0xffffff,   1.00f),
        new ElementInfo(Element.Lv,     7,  16,     OrbitalNumber.P,    ElOccurrence.Synthetic,   ElKind.Unknown,             "Livermorium",      0xffffff,   1.00f),
        new ElementInfo(Element.Ts,     7,  17,     OrbitalNumber.P,    ElOccurrence.Synthetic,   ElKind.Unknown,             "Tennessine",       0xffffff,   1.00f),
        new ElementInfo(Element.Og,     7,  18,     OrbitalNumber.P,    ElOccurrence.Synthetic,   ElKind.Unknown,             "Oganesson",        0xffffff,   1.00f),
#if EXTENDED_ATOM_INFO
        ,
        // extended

        new ElementInfo(Element.Uue,    8,  1,      "Ununennium",       0xffffff,   1.00f),
        new ElementInfo(Element.Ubn,    8,  2,      "Unbinilium",       0xffffff,   1.00f),
        new ElementInfo(Element.Ubu,    8,  -1,     "Unbiunium",        0xffffff,   1.00f),
        new ElementInfo(Element.Ubb,    8,  -1,     "Unbibium",         0xffffff,   1.00f),
        new ElementInfo(Element.Ubt,    8,  -1,     "Unbitrium",        0xffffff,   1.00f),
        new ElementInfo(Element.Ubq,    8,  -1,     "Unbiquadium",      0xffffff,   1.00f),
        new ElementInfo(Element.Ubp,    8,  -1,     "Unbipentium",      0xffffff,   1.00f),
        new ElementInfo(Element.Ubh,    8,  -1,     "Unbihexium",       0xffffff,   1.00f),
        new ElementInfo(Element.Ubs,    8,  -1,     "Unbiseptium",      0xffffff,   1.00f),
        new ElementInfo(Element.Ubo,    8,  -1,     "Unbioctium",       0xffffff,   1.00f),
        new ElementInfo(Element.Ube,    8,  -1,     "Unbiennium",       0xffffff,   1.00f),
        new ElementInfo(Element.Utn,    8,  -1,     "Untrinilium",      0xffffff,   1.00f),
        new ElementInfo(Element.Utu,    8,  -1,     "Untriunium",       0xffffff,   1.00f),
        new ElementInfo(Element.Utb,    8,  -1,     "Untribium",        0xffffff,   1.00f),
        new ElementInfo(Element.Utt,    8,  -1,     "Untritrium",       0xffffff,   1.00f),
        new ElementInfo(Element.Utq,    8,  -1,     "Untriquadium",     0xffffff,   1.00f),
        new ElementInfo(Element.Utp,    8,  -1,     "Untripentium",     0xffffff,   1.00f),
        new ElementInfo(Element.Uth,    8,  -1,     "Untrihexium",      0xffffff,   1.00f),
        new ElementInfo(Element.Uts,    8,  -1,     "Untriseptium",     0xffffff,   1.00f),
        new ElementInfo(Element.Uto,    8,  -1,     "Untrioctium",      0xffffff,   1.00f),
        new ElementInfo(Element.Ute,    8,  13,     "Untriennium",      0xffffff,   1.00f),
        new ElementInfo(Element.Uqn,    8,  14,     "Unquadnilium",     0xffffff,   1.00f),
        new ElementInfo(Element.Uqu,    8,  -1,     "Unquadunium",      0xffffff,   1.00f),
        new ElementInfo(Element.Uqb,    8,  -1,     "Unquadbium",       0xffffff,   1.00f),
        new ElementInfo(Element.Uqt,    8,  -1,     "Unquadtrium",      0xffffff,   1.00f),
        new ElementInfo(Element.Uqq,    8,  -1,     "Unquadquadium",    0xffffff,   1.00f),
        new ElementInfo(Element.Uqp,    8,  -1,     "Unquadpentium",    0xffffff,   1.00f),
        new ElementInfo(Element.Uqh,    8,  -1,     "Unquadhexium",     0xffffff,   1.00f),
        new ElementInfo(Element.Uqs,    8,  -1,     "Unquadseptium",    0xffffff,   1.00f),
        new ElementInfo(Element.Uqo,    8,  -1,     "Unquadoctium",     0xffffff,   1.00f),
        new ElementInfo(Element.Uqe,    8,  -1,     "Unquadennium",     0xffffff,   1.00f),
        new ElementInfo(Element.Upn,    8,  -1,     "Unpentnilium",     0xffffff,   1.00f),
        new ElementInfo(Element.Upu,    8,  -1,     "Unpentunium",      0xffffff,   1.00f),
        new ElementInfo(Element.Upb,    8,  -1,     "Unpentbium",       0xffffff,   1.00f),
        new ElementInfo(Element.Upt,    8,  -1,     "Unpenttrium",      0xffffff,   1.00f),
        new ElementInfo(Element.Upq,    8,  -1,     "Unpentquadium",    0xffffff,   1.00f),
        new ElementInfo(Element.Upp,    8,  3,      "Unpentpentium",    0xffffff,   1.00f),
        new ElementInfo(Element.Uph,    8,  4,      "Unpenthexium",     0xffffff,   1.00f),
        new ElementInfo(Element.Ups,    8,  5,      "Unpentseptium",    0xffffff,   1.00f),
        new ElementInfo(Element.Upo,    8,  6,      "Unpentoctium",     0xffffff,   1.00f),
        new ElementInfo(Element.Upe,    8,  7,      "Unpentennium",     0xffffff,   1.00f),
        new ElementInfo(Element.Uhn,    8,  8,      "Unhexnilium",      0xffffff,   1.00f),
        new ElementInfo(Element.Uhu,    8,  9,      "Unhexunium",       0xffffff,   1.00f),
        new ElementInfo(Element.Uhb,    8,  10,     "Unhexbium",        0xffffff,   1.00f),
        new ElementInfo(Element.Uht,    8,  11,     "Unhextrium",       0xffffff,   1.00f),
        new ElementInfo(Element.Uhq,    8,  12,     "Unhexquadium",     0xffffff,   1.00f),
        new ElementInfo(Element.Uhp,    8,  -1,     "Unhexpentium",     0xffffff,   1.00f),
        new ElementInfo(Element.Uhh,    8,  -1,     "Unhexhexium",      0xffffff,   1.00f),
        new ElementInfo(Element.Uhs,    8,  13,     "Unhexseptium",     0xffffff,   1.00f),
        new ElementInfo(Element.Uho,    8,  14,     "Unhexoctium",      0xffffff,   1.00f),
        new ElementInfo(Element.Uhe,    8,  15,     "Unhexennium",      0xffffff,   1.00f),
        new ElementInfo(Element.Usn,    8,  16,     "Unseptnilium",     0xffffff,   1.00f),
        new ElementInfo(Element.Usu,    8,  17,     "Unseptunium",      0xffffff,   1.00f),
        new ElementInfo(Element.Usb,    8,  18,     "Unseptbium",       0xffffff,   1.00f),
        new ElementInfo(Element.Ust,    8,  -1,     "Unsepttrium",      0xffffff,   1.00f),
#endif
    };

#endregion

#region ElementInfo static accessors

    public static ElementInfo H { get { return elements[(int)Element.H]; } }
    public static ElementInfo He { get { return elements[(int)Element.He]; } }
    public static ElementInfo Li { get { return elements[(int)Element.Li]; } }
    public static ElementInfo Be { get { return elements[(int)Element.Be]; } }
    public static ElementInfo B { get { return elements[(int)Element.B]; } }
    public static ElementInfo C { get { return elements[(int)Element.C]; } }
    public static ElementInfo N { get { return elements[(int)Element.N]; } }
    public static ElementInfo O { get { return elements[(int)Element.O]; } }
    public static ElementInfo F { get { return elements[(int)Element.F]; } }
    public static ElementInfo Ne { get { return elements[(int)Element.Ne]; } }
    public static ElementInfo Na { get { return elements[(int)Element.Na]; } }
    public static ElementInfo Mg { get { return elements[(int)Element.Mg]; } }
    public static ElementInfo Al { get { return elements[(int)Element.Al]; } }
    public static ElementInfo Si { get { return elements[(int)Element.Si]; } }
    public static ElementInfo P { get { return elements[(int)Element.P]; } }
    public static ElementInfo S { get { return elements[(int)Element.S]; } }
    public static ElementInfo Cl { get { return elements[(int)Element.Cl]; } }
    public static ElementInfo Ar { get { return elements[(int)Element.Ar]; } }
    public static ElementInfo K { get { return elements[(int)Element.K]; } }
    public static ElementInfo Ca { get { return elements[(int)Element.Ca]; } }
    public static ElementInfo Sc { get { return elements[(int)Element.Sc]; } }
    public static ElementInfo Ti { get { return elements[(int)Element.Ti]; } }
    public static ElementInfo V { get { return elements[(int)Element.V]; } }
    public static ElementInfo Cr { get { return elements[(int)Element.Cr]; } }
    public static ElementInfo Mn { get { return elements[(int)Element.Mn]; } }
    public static ElementInfo Fe { get { return elements[(int)Element.Fe]; } }
    public static ElementInfo Co { get { return elements[(int)Element.Co]; } }
    public static ElementInfo Ni { get { return elements[(int)Element.Ni]; } }
    public static ElementInfo Cu { get { return elements[(int)Element.Cu]; } }
    public static ElementInfo Zn { get { return elements[(int)Element.Zn]; } }
    public static ElementInfo Ga { get { return elements[(int)Element.Ga]; } }
    public static ElementInfo Ge { get { return elements[(int)Element.Ge]; } }
    public static ElementInfo As { get { return elements[(int)Element.As]; } }
    public static ElementInfo Se { get { return elements[(int)Element.Se]; } }
    public static ElementInfo Br { get { return elements[(int)Element.Br]; } }
    public static ElementInfo Kr { get { return elements[(int)Element.Kr]; } }
    public static ElementInfo Rb { get { return elements[(int)Element.Rb]; } }
    public static ElementInfo Sr { get { return elements[(int)Element.Sr]; } }
    public static ElementInfo Y { get { return elements[(int)Element.Y]; } }
    public static ElementInfo Zr { get { return elements[(int)Element.Zr]; } }
    public static ElementInfo Nb { get { return elements[(int)Element.Nb]; } }
    public static ElementInfo Mo { get { return elements[(int)Element.Mo]; } }
    public static ElementInfo Tc { get { return elements[(int)Element.Tc]; } }
    public static ElementInfo Ru { get { return elements[(int)Element.Ru]; } }
    public static ElementInfo Rh { get { return elements[(int)Element.Rh]; } }
    public static ElementInfo Pd { get { return elements[(int)Element.Pd]; } }
    public static ElementInfo Ag { get { return elements[(int)Element.Ag]; } }
    public static ElementInfo Cd { get { return elements[(int)Element.Cd]; } }
    public static ElementInfo In { get { return elements[(int)Element.In]; } }
    public static ElementInfo Sn { get { return elements[(int)Element.Sn]; } }
    public static ElementInfo Sb { get { return elements[(int)Element.Sb]; } }
    public static ElementInfo Te { get { return elements[(int)Element.Te]; } }
    public static ElementInfo I { get { return elements[(int)Element.I]; } }
    public static ElementInfo Xe { get { return elements[(int)Element.Xe]; } }
    public static ElementInfo Cs { get { return elements[(int)Element.Cs]; } }
    public static ElementInfo Ba { get { return elements[(int)Element.Ba]; } }
    public static ElementInfo La { get { return elements[(int)Element.La]; } }
    public static ElementInfo Ce { get { return elements[(int)Element.Ce]; } }
    public static ElementInfo Pr { get { return elements[(int)Element.Pr]; } }
    public static ElementInfo Nd { get { return elements[(int)Element.Nd]; } }
    public static ElementInfo Pm { get { return elements[(int)Element.Pm]; } }
    public static ElementInfo Sm { get { return elements[(int)Element.Sm]; } }
    public static ElementInfo Eu { get { return elements[(int)Element.Eu]; } }
    public static ElementInfo Gd { get { return elements[(int)Element.Gd]; } }
    public static ElementInfo Tb { get { return elements[(int)Element.Tb]; } }
    public static ElementInfo Dy { get { return elements[(int)Element.Dy]; } }
    public static ElementInfo Ho { get { return elements[(int)Element.Ho]; } }
    public static ElementInfo Er { get { return elements[(int)Element.Er]; } }
    public static ElementInfo Tm { get { return elements[(int)Element.Tm]; } }
    public static ElementInfo Yb { get { return elements[(int)Element.Yb]; } }
    public static ElementInfo Lu { get { return elements[(int)Element.Lu]; } }
    public static ElementInfo Hf { get { return elements[(int)Element.Hf]; } }
    public static ElementInfo Ta { get { return elements[(int)Element.Ta]; } }
    public static ElementInfo W { get { return elements[(int)Element.W]; } }
    public static ElementInfo Re { get { return elements[(int)Element.Re]; } }
    public static ElementInfo Os { get { return elements[(int)Element.Os]; } }
    public static ElementInfo Ir { get { return elements[(int)Element.Ir]; } }
    public static ElementInfo Pt { get { return elements[(int)Element.Pt]; } }
    public static ElementInfo Au { get { return elements[(int)Element.Au]; } }
    public static ElementInfo Hg { get { return elements[(int)Element.Hg]; } }
    public static ElementInfo Tl { get { return elements[(int)Element.Tl]; } }
    public static ElementInfo Pb { get { return elements[(int)Element.Pb]; } }
    public static ElementInfo Bi { get { return elements[(int)Element.Bi]; } }
    public static ElementInfo Po { get { return elements[(int)Element.Po]; } }
    public static ElementInfo At { get { return elements[(int)Element.At]; } }
    public static ElementInfo Rn { get { return elements[(int)Element.Rn]; } }
    public static ElementInfo Fr { get { return elements[(int)Element.Fr]; } }
    public static ElementInfo Ra { get { return elements[(int)Element.Ra]; } }
    public static ElementInfo Ac { get { return elements[(int)Element.Ac]; } }
    public static ElementInfo Th { get { return elements[(int)Element.Th]; } }
    public static ElementInfo Pa { get { return elements[(int)Element.Pa]; } }
    public static ElementInfo U { get { return elements[(int)Element.U]; } }
    public static ElementInfo Np { get { return elements[(int)Element.Np]; } }
    public static ElementInfo Pu { get { return elements[(int)Element.Pu]; } }
    public static ElementInfo Am { get { return elements[(int)Element.Am]; } }

#endregion
}