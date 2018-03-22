using Units;

public class CompoundPhysicalProperties
{
    public float MolarMass { get; private set; }

    public float Density { get; private set; }

    public Kelvin MeltingT { get; private set; }

    public Kelvin BoilingT { get; private set; }

    public CompoundPhysicalProperties(float molarMass, float density, Kelvin meltingT, Kelvin boilingT)
    {
        MolarMass = molarMass;
        Density = density;
        MeltingT = meltingT;
        BoilingT = boilingT;
    }
}
