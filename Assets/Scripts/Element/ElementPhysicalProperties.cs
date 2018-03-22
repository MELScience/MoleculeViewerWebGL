using Units;
using System.Collections.Generic;

public partial class ElementInfo
{
    public float MolarMass
    {
        get {
            CompoundPhysicalProperties value;
            return s_elementPhysicalProperties.TryGetValue(this.Element, out value) ? value.MolarMass : -1f;
        }
    }
    public float Density
    {
        get
        {
            CompoundPhysicalProperties value;
            return s_elementPhysicalProperties.TryGetValue(this.Element, out value) ? value.Density : -1f;
        }
    }
    public Kelvin MeltingT
    {
        get
        {
            CompoundPhysicalProperties value;
            return s_elementPhysicalProperties.TryGetValue(this.Element, out value) ? value.MeltingT : new Kelvin(-1f);
        }
    }
    public Kelvin BoilingT
    {
        get
        {
            CompoundPhysicalProperties value;
            return s_elementPhysicalProperties.TryGetValue(this.Element, out value) ? value.BoilingT : new Kelvin(-1f);
        }
    }

    #region Elements physical properties

    private static readonly Dictionary<Element, CompoundPhysicalProperties> s_elementPhysicalProperties = new Dictionary<Element, CompoundPhysicalProperties>
    {
        { Element.H,       new CompoundPhysicalProperties(1.1979f,       0.00008988f,  (Kelvin)14.01f,       (Kelvin)20.28f) },
        { Element.He,      new CompoundPhysicalProperties(4.2026f,       0.0001785f,   (Kelvin)0.956f,       (Kelvin)4.22f) },
        { Element.Li,      new CompoundPhysicalProperties(6.941f,        0.534f,       (Kelvin)453.69f,      (Kelvin)1560) },
        { Element.Be,      new CompoundPhysicalProperties(9.0122f,       1.85f,        (Kelvin)1560,         (Kelvin)2742) },
        { Element.B,       new CompoundPhysicalProperties(10.811f,       2.34f,        (Kelvin)2349,         (Kelvin)4200) },
        { Element.C,       new CompoundPhysicalProperties(12.0107f,      2.267f,       (Kelvin)3800,         (Kelvin)4300) },
        { Element.N,       new CompoundPhysicalProperties(14.0067f,      0.0012506f,   (Kelvin)63.15f,       (Kelvin)77.36f) },
        { Element.O,       new CompoundPhysicalProperties(15.9994f,      0.001429f,    (Kelvin)54.36f,       (Kelvin)90.20f) },
        { Element.F,       new CompoundPhysicalProperties(18.9984f,      0.001696f,    (Kelvin)53.53f,       (Kelvin)85.03f) },
        { Element.Ne,      new CompoundPhysicalProperties(20.1797f,      0.0008999f,   (Kelvin)24.56f,       (Kelvin)27.07f) },
        { Element.Na,      new CompoundPhysicalProperties(22.9897f,      0.971f,       (Kelvin)370.87f,      (Kelvin)1156) },
        { Element.Mg,      new CompoundPhysicalProperties(24.305f,       1.738f,       (Kelvin)923,          (Kelvin)1363) },
        { Element.Al,      new CompoundPhysicalProperties(26.9815f,      2.698f,       (Kelvin)933.47f,      (Kelvin)2792) },
        { Element.Si,      new CompoundPhysicalProperties(28.0855f,      2.3296f,      (Kelvin)1687,         (Kelvin)3538) },
        { Element.P,       new CompoundPhysicalProperties(30.9738f,      1.82f,        (Kelvin)317.30f,      (Kelvin)550) },
        { Element.S,       new CompoundPhysicalProperties(32.065f,       2.067f,       (Kelvin)388.36f,      (Kelvin)717.87f) },
        { Element.Cl,      new CompoundPhysicalProperties(35.453f,       0.003214f,    (Kelvin)171.6f,       (Kelvin)239.11f) },
        { Element.Ar,      new CompoundPhysicalProperties(39.948f,       0.0017837f,   (Kelvin)83.80f,       (Kelvin)87.30f) },
        { Element.K,       new CompoundPhysicalProperties(39.0983f,      0.862f,       (Kelvin)336.53f,      (Kelvin)1032) },
        { Element.Ca,      new CompoundPhysicalProperties(40.078f,       1.54f,        (Kelvin)1115,         (Kelvin)1757) },
        { Element.Sc,      new CompoundPhysicalProperties(44.9559f,      2.989f,       (Kelvin)1814,         (Kelvin)3109) },
        { Element.Ti,      new CompoundPhysicalProperties(47.867f,       4.54f,        (Kelvin)1941,         (Kelvin)3560) },
        { Element.V,       new CompoundPhysicalProperties(50.9415f,      6.11f,        (Kelvin)2183,         (Kelvin)3680) },
        { Element.Cr,      new CompoundPhysicalProperties(51.9961f,      7.15f,        (Kelvin)2180,         (Kelvin)2944) },
        { Element.Mn,      new CompoundPhysicalProperties(54.938f,       7.44f,        (Kelvin)1519,         (Kelvin)2334) },
        { Element.Fe,      new CompoundPhysicalProperties(55.845f,       7.874f,       (Kelvin)1811,         (Kelvin)3134) },
        { Element.Co,      new CompoundPhysicalProperties(58.9332f,      8.86f,        (Kelvin)1768,         (Kelvin)3200) },
        { Element.Ni,      new CompoundPhysicalProperties(58.6934f,      8.912f,       (Kelvin)1728,         (Kelvin)3186) },
        { Element.Cu,      new CompoundPhysicalProperties(63.546f,       8.96f,        (Kelvin)1357.77f,     (Kelvin)2835) },
        { Element.Zn,      new CompoundPhysicalProperties(65.39f,        7.134f,       (Kelvin)692.88f,      (Kelvin)1180) },
        { Element.Ga,      new CompoundPhysicalProperties(69.723f,       5.907f,       (Kelvin)302.9146f,    (Kelvin)2477) },
        { Element.Ge,      new CompoundPhysicalProperties(72.64f,        5.323f,       (Kelvin)1211.40f,     (Kelvin)3106) },
        { Element.As,      new CompoundPhysicalProperties(74.9216f,      5.776f,       (Kelvin)1090,         (Kelvin)887) },
        { Element.Se,      new CompoundPhysicalProperties(78.96f,        4.809f,       (Kelvin)453,          (Kelvin)958) },
        { Element.Br,      new CompoundPhysicalProperties(79.904f,       3.122f,       (Kelvin)265.8f,       (Kelvin)332.0f) },
        { Element.Kr,      new CompoundPhysicalProperties(83.8f,         0.003733f,    (Kelvin)115.79f,      (Kelvin)119.93f) },
        { Element.Rb,      new CompoundPhysicalProperties(85.4678f,      1.532f,       (Kelvin)312.46f,      (Kelvin)961) },
        { Element.Sr,      new CompoundPhysicalProperties(87.62f,        2.64f,        (Kelvin)1050,         (Kelvin)1655) },
        { Element.Y,       new CompoundPhysicalProperties(88.9059f,      4.469f,       (Kelvin)1799,         (Kelvin)3609) },
        { Element.Zr,      new CompoundPhysicalProperties(91.224f,       6.506f,       (Kelvin)2128,         (Kelvin)4682) },
        { Element.Nb,      new CompoundPhysicalProperties(92.9064f,      8.57f,        (Kelvin)2750,         (Kelvin)5017) },
        { Element.Mo,      new CompoundPhysicalProperties(95.94f,        10.22f,       (Kelvin)2896,         (Kelvin)4912) },
        { Element.Tc,      new CompoundPhysicalProperties(98,            11.5f,        (Kelvin)2430,         (Kelvin)4538) },
        { Element.Ru,      new CompoundPhysicalProperties(101.07f,       12.37f,       (Kelvin)2607,         (Kelvin)4423) },
        { Element.Rh,      new CompoundPhysicalProperties(102.9055f,     12.41f,       (Kelvin)2237,         (Kelvin)3968) },
        { Element.Pd,      new CompoundPhysicalProperties(106.42f,       12.02f,       (Kelvin)1828.05f,     (Kelvin)3236) },
        { Element.Ag,      new CompoundPhysicalProperties(107.8682f,     10.501f,      (Kelvin)1234.93f,     (Kelvin)2435) },
        { Element.Cd,      new CompoundPhysicalProperties(112.411f,      8.69f,        (Kelvin)594.22f,      (Kelvin)1040) },
        { Element.In,      new CompoundPhysicalProperties(114.818f,      7.31f,        (Kelvin)429.75f,      (Kelvin)2345) },
        { Element.Sn,      new CompoundPhysicalProperties(118.71f,       7.287f,       (Kelvin)505.08f,      (Kelvin)2875) },
        { Element.Sb,      new CompoundPhysicalProperties(121.76f,       6.685f,       (Kelvin)903.78f,      (Kelvin)1860) },
        { Element.Te,      new CompoundPhysicalProperties(127.6f,        6.232f,       (Kelvin)722.66f,      (Kelvin)1261) },
        { Element.I,       new CompoundPhysicalProperties(126.9045f,     4.93f,        (Kelvin)386.85f,      (Kelvin)457.4f) },
        { Element.Xe,      new CompoundPhysicalProperties(131.293f,      0.005887f,    (Kelvin)161.4f,       (Kelvin)165.03f) },
        { Element.Cs,      new CompoundPhysicalProperties(132.9055f,     1.873f,       (Kelvin)301.59f,      (Kelvin)944) },
        { Element.Ba,      new CompoundPhysicalProperties(137.327f,      3.594f,       (Kelvin)1000,         (Kelvin)2170) },
        { Element.La,      new CompoundPhysicalProperties(138.9055f,     6.145f,       (Kelvin)1193,         (Kelvin)3737) },
        { Element.Ce,      new CompoundPhysicalProperties(140.116f,      6.77f,        (Kelvin)1068,         (Kelvin)3716) },
        { Element.Pr,      new CompoundPhysicalProperties(140.9077f,     6.773f,       (Kelvin)1208,         (Kelvin)3793) },
        { Element.Nd,      new CompoundPhysicalProperties(144.24f,       7.007f,       (Kelvin)1297,         (Kelvin)3347) },
        { Element.Pm,      new CompoundPhysicalProperties(145,           7.26f,        (Kelvin)1315,         (Kelvin)3273) },
        { Element.Sm,      new CompoundPhysicalProperties(150.36f,       7.52f,        (Kelvin)1345,         (Kelvin)2067) },
        { Element.Eu,      new CompoundPhysicalProperties(151.964f,      5.243f,       (Kelvin)1099,         (Kelvin)1802) },
        { Element.Gd,      new CompoundPhysicalProperties(157.25f,       7.895f,       (Kelvin)1585,         (Kelvin)3546) },
        { Element.Tb,      new CompoundPhysicalProperties(158.9253f,     8.229f,       (Kelvin)1629,         (Kelvin)3503) },
        { Element.Dy,      new CompoundPhysicalProperties(162.5f,        8.55f,        (Kelvin)1680,         (Kelvin)2840) },
        { Element.Ho,      new CompoundPhysicalProperties(164.9303f,     8.795f,       (Kelvin)1734,         (Kelvin)2993) },
        { Element.Er,      new CompoundPhysicalProperties(167.259f,      9.066f,       (Kelvin)1802,         (Kelvin)3141) },
        { Element.Tm,      new CompoundPhysicalProperties(168.9342f,     9.321f,       (Kelvin)1818,         (Kelvin)2223) },
        { Element.Yb,      new CompoundPhysicalProperties(173.04f,       6.965f,       (Kelvin)1097,         (Kelvin)1469) },
        { Element.Lu,      new CompoundPhysicalProperties(174.967f,      9.84f,        (Kelvin)1925,         (Kelvin)3675) },
        { Element.Hf,      new CompoundPhysicalProperties(178.49f,       13.31f,       (Kelvin)2506,         (Kelvin)4876) },
        { Element.Ta,      new CompoundPhysicalProperties(180.9479f,     16.654f,      (Kelvin)3290,         (Kelvin)5731) },
        { Element.W,       new CompoundPhysicalProperties(183.84f,       19.25f,       (Kelvin)3695,         (Kelvin)5828) },
        { Element.Re,      new CompoundPhysicalProperties(186.207f,      21.02f,       (Kelvin)3459,         (Kelvin)5869) },
        { Element.Os,      new CompoundPhysicalProperties(190.23f,       22.61f,       (Kelvin)3306,         (Kelvin)5285) },
        { Element.Ir,      new CompoundPhysicalProperties(192.217f,      22.56f,       (Kelvin)2719,         (Kelvin)4701) },
        { Element.Pt,      new CompoundPhysicalProperties(195.078f,      21.46f,       (Kelvin)2041.4f,      (Kelvin)4098) },
        { Element.Au,      new CompoundPhysicalProperties(196.9665f,     19.282f,      (Kelvin)1337.33f,     (Kelvin)3129) },
        { Element.Hg,      new CompoundPhysicalProperties(200.59f,       13.5336f,     (Kelvin)234.43f,      (Kelvin)629.88f) },
        { Element.Tl,      new CompoundPhysicalProperties(204.3833f,     11.85f,       (Kelvin)577,          (Kelvin)1746) },
        { Element.Pb,      new CompoundPhysicalProperties(207.2f,        11.342f,      (Kelvin)600.61f,      (Kelvin)2022) },
        { Element.Bi,      new CompoundPhysicalProperties(208.9804f,     9.807f,       (Kelvin)544.7f,       (Kelvin)1837) },
        { Element.Po,      new CompoundPhysicalProperties(209,           9.32f,        (Kelvin)527,          (Kelvin)1235) },
        { Element.At,      new CompoundPhysicalProperties(210,           7,            (Kelvin)575,          (Kelvin)610) },
        { Element.Rn,      new CompoundPhysicalProperties(222,           0.00973f,     (Kelvin)202,          (Kelvin)211.3f) },
        { Element.Fr,      new CompoundPhysicalProperties(223,           1.87f,        (Kelvin)300,          (Kelvin)950) },
        { Element.Ra,      new CompoundPhysicalProperties(226,           5.5f,         (Kelvin)973,          (Kelvin)2010) },
        { Element.Ac,      new CompoundPhysicalProperties(227,           10.07f,       (Kelvin)1323,         (Kelvin)3471) },
        { Element.Th,      new CompoundPhysicalProperties(232.0381f,     11.72f,       (Kelvin)2115,         (Kelvin)5061) },
        { Element.Pa,      new CompoundPhysicalProperties(231.0359f,     15.37f,       (Kelvin)1841,         (Kelvin)4300) },
        { Element.U,       new CompoundPhysicalProperties(238.0289f,     18.95f,       (Kelvin)1405.3f,      (Kelvin)4404) },
        { Element.Np,      new CompoundPhysicalProperties(237,           20.45f,       (Kelvin)917,          (Kelvin)4273) },
        { Element.Pu,      new CompoundPhysicalProperties(244,           19.84f,       (Kelvin)912.5f,       (Kelvin)3501) },
        { Element.Am,      new CompoundPhysicalProperties(243,           13.69f,       (Kelvin)1449,         (Kelvin)2880) },
    };

    #endregion

}