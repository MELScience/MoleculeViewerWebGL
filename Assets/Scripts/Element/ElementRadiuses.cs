using System.Collections.Generic;

public partial class ElementInfo
{
    public const float RadiusMultiplier = 10e-10f;

    public static readonly ElementRadiuses MaxRadiuses = new ElementRadiuses(2.98f, 2.6f, 2.25f, 2.75f, 2.06f);
    public static readonly ElementRadiuses MinRadiuses = new ElementRadiuses(0.31f, 0.25f, 0.32f, 1.2f, 0.1f);
    public static readonly ElementRadiuses LogMaxMin = new ElementRadiuses(2.263f, 2.342f, 1.95f, 0.829f, 3.025f);

    // NOTE! Some of elements have no defined radiuses, in this case value is -1.0f

    public float AtomicRadius { get { return GetRadiuses(this.Element).AtomicRadius; } }

    public float IonicRadius { get { return GetRadiuses(this.Element).IonicRadius; } }

    public float CovalentRadius { get { return GetRadiuses(this.Element).CovalentRadius; } }

    public float VanDerWaalsRadius { get { return GetRadiuses(this.Element).VanDerWaalsRadius; } }

    public float CrystalRadius { get { return GetRadiuses(this.Element).CrystalRadius; } }

    public ElementRadiuses AllRadiuses { get { return GetRadiuses(this.Element); } }

    public class ElementRadiuses
    {
        public float AtomicRadius { get; private set; }
        public float IonicRadius { get; private set; }
        public float CovalentRadius { get; private set; }
        public float VanDerWaalsRadius { get; private set; }
        public float CrystalRadius { get; private set; }

        public ElementRadiuses(float atomicRadius, float ionicRadius, float covalentRadius, float vanDerWaalsRadius, float crystalRadius)
        {
            AtomicRadius = atomicRadius;
            IonicRadius = ionicRadius;
            CovalentRadius = covalentRadius;
            VanDerWaalsRadius = vanDerWaalsRadius;
            CrystalRadius = crystalRadius;
        }
    }

    private ElementRadiuses GetRadiuses(Element el)
    {
        ElementRadiuses rad;
        return s_atomicRadiuses.TryGetValue(el, out rad) ? rad : emptyRadius;
    }


    #region Elements radiuses data

    //
    // from http://crystalmaker.com/support/tutorials/crystalmaker/atomic-radii/index.html

    private static readonly ElementRadiuses emptyRadius = new ElementRadiuses(-1f,-1f,-1f,-1f,-1f);

    private static readonly Dictionary<Element, ElementRadiuses> s_atomicRadiuses = new Dictionary<Element, ElementRadiuses>
    {
        { Element.H,        new ElementRadiuses(0.53f,      0.25f,      0.37f,      1.2f,       0.1f) },
        { Element.He,       new ElementRadiuses(0.31f,      0.31f,      0.32f,      1.4f,       -1.0f) },
        { Element.Li,       new ElementRadiuses(1.67f,      1.45f,      1.34f,      1.82f,      0.9f) },
        { Element.Be,       new ElementRadiuses(1.12f,      1.05f,      0.9f,       -1.0f,      0.41f) },
        { Element.B,        new ElementRadiuses(0.87f,      0.85f,      0.82f,      -1.0f,      0.25f) },
        { Element.C,        new ElementRadiuses(0.67f,      0.7f,       0.77f,      1.7f,       0.29f) },
        { Element.N,        new ElementRadiuses(0.56f,      0.65f,      0.75f,      1.55f,      0.3f) },
        { Element.O,        new ElementRadiuses(0.48f,      0.6f,       0.73f,      1.52f,      1.21f) },
        { Element.F,        new ElementRadiuses(0.42f,      0.5f,       0.71f,      1.47f,      1.19f) },
        { Element.Ne,       new ElementRadiuses(0.38f,      0.38f,      0.69f,      1.54f,      -1.0f) },
        { Element.Na,       new ElementRadiuses(1.9f,       1.8f,       1.54f,      2.27f,      1.16f) },
        { Element.Mg,       new ElementRadiuses(1.45f,      1.5f,       1.3f,       1.73f,      0.86f) },
        { Element.Al,       new ElementRadiuses(1.18f,      1.25f,      1.18f,      -1.0f,      0.53f) },
        { Element.Si,       new ElementRadiuses(1.11f,      1.1f,       1.11f,      2.1f,       0.4f) },
        { Element.P,        new ElementRadiuses(0.98f,      1.0f,       1.06f,      1.8f,       0.31f) },
        { Element.S,        new ElementRadiuses(0.88f,      1.0f,       1.02f,      1.8f,       0.43f) },
        { Element.Cl,       new ElementRadiuses(0.79f,      1.0f,       0.99f,      1.75f,      1.67f) },
        { Element.Ar,       new ElementRadiuses(0.71f,      0.71f,      0.97f,      1.88f,      -1.0f) },
        { Element.K,        new ElementRadiuses(2.43f,      2.2f,       1.96f,      2.75f,      1.52f) },
        { Element.Ca,       new ElementRadiuses(1.94f,      1.8f,       1.74f,      -1.0f,      1.14f) },
        { Element.Sc,       new ElementRadiuses(1.84f,      1.6f,       1.44f,      -1.0f,      0.89f) },
        { Element.Ti,       new ElementRadiuses(1.76f,      1.4f,       1.36f,      -1.0f,      0.75f) },
        { Element.V,        new ElementRadiuses(1.71f,      1.35f,      1.25f,      -1.0f,      0.68f) },
        { Element.Cr,       new ElementRadiuses(1.66f,      1.4f,       1.27f,      -1.0f,      0.76f) },
        { Element.Mn,       new ElementRadiuses(1.61f,      1.4f,       1.39f,      -1.0f,      0.81f) },
        { Element.Fe,       new ElementRadiuses(1.56f,      1.4f,       1.25f,      -1.0f,      0.69f) },
        { Element.Co,       new ElementRadiuses(1.52f,      1.35f,      1.26f,      -1.0f,      0.54f) },
        { Element.Ni,       new ElementRadiuses(1.49f,      1.35f,      1.21f,      1.63f,      0.7f) },
        { Element.Cu,       new ElementRadiuses(1.45f,      1.35f,      1.38f,      1.4f,       0.71f) },
        { Element.Zn,       new ElementRadiuses(1.42f,      1.35f,      1.31f,      1.39f,      0.74f) },
        { Element.Ga,       new ElementRadiuses(1.36f,      1.3f,       1.26f,      1.87f,      0.76f) },
        { Element.Ge,       new ElementRadiuses(1.25f,      1.25f,      1.22f,      -1.0f,      0.53f) },
        { Element.As,       new ElementRadiuses(1.14f,      1.15f,      1.19f,      1.85f,      0.72f) },
        { Element.Se,       new ElementRadiuses(1.03f,      1.15f,      1.16f,      1.9f,       0.56f) },
        { Element.Br,       new ElementRadiuses(0.94f,      1.15f,      1.14f,      1.85f,      1.82f) },
        { Element.Kr,       new ElementRadiuses(0.88f,      0.88f,      1.1f,       2.02f,      -1.0f) },
        { Element.Rb,       new ElementRadiuses(2.65f,      2.35f,      2.11f,      -1.0f,      1.66f) },
        { Element.Sr,       new ElementRadiuses(2.19f,      2.0f,       1.92f,      -1.0f,      1.32f) },
        { Element.Y,        new ElementRadiuses(2.12f,      1.85f,      1.62f,      -1.0f,      1.04f) },
        { Element.Zr,       new ElementRadiuses(2.06f,      1.55f,      1.48f,      -1.0f,      0.86f) },
        { Element.Nb,       new ElementRadiuses(1.98f,      1.45f,      1.37f,      -1.0f,      0.78f) },
        { Element.Mo,       new ElementRadiuses(1.9f,       1.45f,      1.45f,      -1.0f,      0.79f) },
        { Element.Tc,       new ElementRadiuses(1.83f,      1.35f,      1.56f,      -1.0f,      0.79f) },
        { Element.Ru,       new ElementRadiuses(1.78f,      1.3f,       1.26f,      -1.0f,      0.82f) },
        { Element.Rh,       new ElementRadiuses(1.73f,      1.35f,      1.35f,      -1.0f,      0.81f) },
        { Element.Pd,       new ElementRadiuses(1.69f,      1.4f,       1.31f,      1.63f,      0.78f) },
        { Element.Ag,       new ElementRadiuses(1.65f,      1.6f,       1.53f,      1.72f,      1.29f) },
        { Element.Cd,       new ElementRadiuses(1.61f,      1.55f,      1.48f,      1.58f,      0.92f) },
        { Element.In,       new ElementRadiuses(1.56f,      1.55f,      1.44f,      1.93f,      0.94f) },
        { Element.Sn,       new ElementRadiuses(1.45f,      1.45f,      1.41f,      2.17f,      0.69f) },
        { Element.Sb,       new ElementRadiuses(1.33f,      1.45f,      1.38f,      -1.0f,      0.9f) },
        { Element.Te,       new ElementRadiuses(1.23f,      1.4f,       1.35f,      2.06f,      1.11f) },
        { Element.I,        new ElementRadiuses(1.15f,      1.4f,       1.33f,      1.98f,      2.06f) },
        { Element.Xe,       new ElementRadiuses(1.08f,      1.08f,      1.3f,       2.16f,      0.62f) },
        { Element.Cs,       new ElementRadiuses(2.98f,      2.6f,       2.25f,      -1.0f,      1.81f) },
        { Element.Ba,       new ElementRadiuses(2.53f,      2.15f,      1.98f,      -1.0f,      1.49f) },
        { Element.La,       new ElementRadiuses(1.95f,      1.95f,      1.69f,      -1.0f,      1.36f) },
        { Element.Ce,       new ElementRadiuses(1.85f,      1.85f,      -1.0f,      -1.0f,      1.15f) },
        { Element.Pr,       new ElementRadiuses(2.47f,      1.85f,      -1.0f,      -1.0f,      1.32f) },
        { Element.Nd,       new ElementRadiuses(2.06f,      1.85f,      -1.0f,      -1.0f,      1.3f) },
        { Element.Pm,       new ElementRadiuses(2.05f,      1.85f,      -1.0f,      -1.0f,      1.28f) },
        { Element.Sm,       new ElementRadiuses(2.38f,      1.85f,      -1.0f,      -1.0f,      1.1f) },
        { Element.Eu,       new ElementRadiuses(2.31f,      1.85f,      -1.0f,      -1.0f,      1.31f) },
        { Element.Gd,       new ElementRadiuses(2.33f,      1.8f,       -1.0f,      -1.0f,      1.08f) },
        { Element.Tb,       new ElementRadiuses(2.25f,      1.75f,      -1.0f,      -1.0f,      1.18f) },
        { Element.Dy,       new ElementRadiuses(2.28f,      1.75f,      -1.0f,      -1.0f,      1.05f) },
        { Element.Ho,       new ElementRadiuses(2.26f,      1.75f,      -1.0f,      -1.0f,      1.04f) },
        { Element.Er,       new ElementRadiuses(2.26f,      1.75f,      -1.0f,      -1.0f,      1.03f) },
        { Element.Tm,       new ElementRadiuses(2.22f,      1.75f,      -1.0f,      -1.0f,      1.02f) },
        { Element.Yb,       new ElementRadiuses(2.22f,      1.75f,      -1.0f,      -1.0f,      1.13f) },
        { Element.Lu,       new ElementRadiuses(2.17f,      1.75f,      1.6f,       -1.0f,      1.0f) },
        { Element.Hf,       new ElementRadiuses(2.08f,      1.55f,      1.5f,       -1.0f,      0.85f) },
        { Element.Ta,       new ElementRadiuses(2.0f,       1.45f,      1.38f,      -1.0f,      0.78f) },
        { Element.W,        new ElementRadiuses(1.93f,      1.35f,      1.46f,      -1.0f,      0.74f) },
        { Element.Re,       new ElementRadiuses(1.88f,      1.35f,      1.59f,      -1.0f,      0.77f) },
        { Element.Os,       new ElementRadiuses(1.85f,      1.3f,       1.28f,      -1.0f,      0.77f) },
        { Element.Ir,       new ElementRadiuses(1.8f,       1.35f,      1.37f,      -1.0f,      0.77f) },
        { Element.Pt,       new ElementRadiuses(1.77f,      1.35f,      1.28f,      1.75f,      0.74f) },
        { Element.Au,       new ElementRadiuses(1.74f,      1.35f,      1.44f,      1.66f,      1.51f) },
        { Element.Hg,       new ElementRadiuses(1.71f,      1.5f,       1.49f,      1.55f,      0.83f) },
        { Element.Tl,       new ElementRadiuses(1.56f,      1.9f,       1.48f,      1.96f,      1.03f) },
        { Element.Pb,       new ElementRadiuses(1.54f,      1.8f,       1.47f,      2.02f,      1.49f) },
        { Element.Bi,       new ElementRadiuses(1.43f,      1.6f,       1.46f,      -1.0f,      1.17f) },
        { Element.Po,       new ElementRadiuses(1.35f,      1.9f,       -1.0f,      -1.0f,      1.08f) },
        { Element.At,       new ElementRadiuses(1.27f,      1.27f,      -1.0f,      -1.0f,      0.76f) },
        { Element.Rn,       new ElementRadiuses(1.2f,       1.2f,       1.45f,      -1.0f,      -1.0f) },
        { Element.Fr,       new ElementRadiuses(-1.0f,      -1.0f,      -1.0f,      -1.0f,      1.94f) },
        { Element.Ra,       new ElementRadiuses(-1.0f,      2.15f,      -1.0f,      -1.0f,      1.62f) },
        { Element.Ac,       new ElementRadiuses(1.95f,      1.95f,      -1.0f,      -1.0f,      1.26f) },
        { Element.Th,       new ElementRadiuses(1.8f,       1.8f,       -1.0f,      -1.0f,      1.19f) },
        { Element.Pa,       new ElementRadiuses(1.8f,       1.8f,       -1.0f,      -1.0f,      1.09f) },
        { Element.U,        new ElementRadiuses(1.75f,      1.75f,      -1.0f,      1.86f,      0.87f) },
        { Element.Np,       new ElementRadiuses(1.75f,      1.75f,      -1.0f,      -1.0f,      -1.0f) },
        { Element.Pu,       new ElementRadiuses(1.75f,      1.75f,      -1.0f,      -1.0f,      1.0f) },
        { Element.Am,       new ElementRadiuses(1.75f,      1.75f,      -1.0f,      -1.0f,      1.12f) },
    };

    #endregion
}