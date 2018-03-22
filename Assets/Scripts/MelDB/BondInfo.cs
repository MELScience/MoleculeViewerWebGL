using UnityEngine;
using System.Collections;

public class BondInfo
{
    public enum BondType : sbyte
    {
        SINGLE = 0,
        DOUBLE,
        TRIPLE,
        QUADRUPLE,
        DASHED,
        SINGLEANDDASHED,
        DASHEDANDSINGLE,
        UNKNOWN = -1
    }

    public short atom1 = -1;
    public short atom2 = -1;
    public BondType bondType = BondType.UNKNOWN;

    //Need that for building compounds
    public BondInfo ShallowCopy()
    {
        return (BondInfo)this.MemberwiseClone();
    }
}
