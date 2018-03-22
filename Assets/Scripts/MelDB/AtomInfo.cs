using UnityEngine;
using System.Xml.Serialization;

public class AtomInfo
{
    public Vector3 position = Vector3.zero;
    public Vector2 flatPosition = Vector2.zero;
    public Element element = Element.H;
    public sbyte atomCharge = 0;
    public sbyte radical = 0;

    [XmlIgnore]
    public string atomType { get { return element.ToString("G"); } }

    // TODO: Replace with integer mass of stable isotope..?
    public int GetAtomicMass() { return (int)ElementInfo.Get(element).MolarMass; }

    //[XmlIgnore]
    //public float electroNegativity { get { return ElementsTable.elements[element].electronegativity; } }

    //[XmlIgnore]
    //public int[] possibleOxidations { get { return ElementsTable.elements[element].possibleOxidations; } }

}
