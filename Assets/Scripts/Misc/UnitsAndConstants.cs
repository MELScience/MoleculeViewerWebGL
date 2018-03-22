using System;
using UnityEngine;

// https://en.wikipedia.org/wiki/Conversion_of_units
namespace Units
{
    public static class GasConstants
    {
        public static readonly float R = 8.31f;
    }

    public struct Celsius
    {
        const string suffix = "C";
        public readonly float value;

        public Celsius(float _value)
        {
            value = _value;
        }

        public static implicit operator Celsius(Kelvin kelvin)
        {
            return new Celsius(kelvin.value - 273.15f);
        }

        public static explicit operator Celsius(float degrees)
        {
            return new Celsius(degrees);
        }
    }

    public struct Kelvin
    {
        const string suffix = "K";
        public readonly float value;

        public Kelvin(float _value)
        {
            value = _value;
        }

        public static implicit operator Kelvin(Celsius celsius)
        {
            return new Kelvin(celsius.value + 273.15f);
        }

        public static explicit operator Kelvin(float degrees)
        {
            return new Kelvin(degrees);
        }
    }
    
    public struct Pascal
    {
        const string suffix = "Pa";
        public readonly float value;

        public Pascal(float _value)
        {
            value = _value;
        }

        public static implicit operator Pascal(Atmosphere atmosphere)
        {
            return new Pascal(atmosphere.value * 101325f);
        }

        public static explicit operator Pascal(Bar bar)
        {
            return new Pascal(bar.value * 100000f);
        }

        public static explicit operator Pascal(Torr torr)
        {
            return new Pascal(torr.value * 101325f / 760f);
        }
    }

    public struct Atmosphere
    {
        const string suffix = "atm";
        public readonly float value;

        public Atmosphere(float _value)
        {
            value = _value;
        }

        public static implicit operator Atmosphere(Pascal pascal)
        {
            return new Atmosphere(pascal.value / 101325f);
        }

        public static explicit operator Atmosphere(Bar bar)
        {
            return new Atmosphere(bar.value / 1.01325f);
        }

        public static explicit operator Atmosphere(Torr torr)
        {
            return new Atmosphere(torr.value / 760f);
        }
    }

    public struct Bar
    {
        const string suffix = "bar";
        public readonly float value;

        public Bar(float _value)
        {
            value = _value;
        }

        public static implicit operator Bar(Pascal pascal)
        {
            return new Bar(pascal.value / 100000f);
        }

        public static explicit operator Bar(Atmosphere atmosphere)
        {
            return new Bar(atmosphere.value * 1.01325f);
        }

        public static explicit operator Bar(Torr torr)
        {
            return new Bar(torr.value * 1.01325f / 760f);
        }
    }

    public struct Torr
    {
        const string suffix = "torr";
        public readonly float value;

        public Torr(float _value)
        {
            value = _value;
        }

        public static implicit operator Torr(Pascal pascal)
        {
            return new Torr(pascal.value * 760f / 101325f);
        }

        public static explicit operator Torr(Atmosphere atmosphere)
        {
            return new Torr(atmosphere.value * 760f);
        }

        public static explicit operator Torr(Bar bar)
        {
            return new Torr(bar.value * 760f / 1.01325f);
        }
    }

    public struct Meter
    {
        const string suffix = "m";
        public readonly float value;
    }

    public struct Angstrom
    {
        const string suffix = "A";
        public readonly float value;
    }

    public struct CubicMeter
    {
        const string suffix = "m^3";
        public readonly float value;
    }

    public struct CubicAngstrom
    {
        const string suffix = "A^3";
        public readonly float value;
    }

    public struct Mole
    {
        const string suffix = "mol";
        public readonly float value;
    }
}


public struct UnitPrefix
{
    public string text;
    public string symbol;
    public int value;

    public UnitPrefix(string text, string symbol, int value)
    {
        this.text = text;
        this.symbol = symbol;
        this.value = value;
    }
}

public static class UnitsAndConstants
{
    public static readonly string[] ScaleSubscripts = {
        "1 m",
        "10 cm",  "1 cm",  "1 mm",
        "100 µm", "10 µm", "1 µm",
        "100 nm", "10 nm", "1 nm",
        "1 Å",    "10 pm", "1 pm",
        "100 fm", "10 fm", "1 fm"
    };

    public static readonly UnitPrefix[] siUnitPrefixes = {
//             new UnitPrefix("yocto",     "y",    -24),
//             new UnitPrefix("zepto",     "z",    -21),
//             new UnitPrefix("atto",      "a",    -18),
//             new UnitPrefix("femto",     "f",    -15),
        new UnitPrefix("pico",      "p",    -12),
        new UnitPrefix("nano",      "n",    -9),
        new UnitPrefix("micro",     "μ",    -6),
        new UnitPrefix("milli",     "m",    -3),
        new UnitPrefix("",          "",     0),
        new UnitPrefix("kilo",      "k",    3),
        new UnitPrefix("mega",      "M",    6),
        new UnitPrefix("giga",      "G",    9),
        new UnitPrefix("tera",      "T",    12),
//             new UnitPrefix("peta",      "P",    15),
//             new UnitPrefix("exa",       "E",    18),
//             new UnitPrefix("zetta",     "Z",    21),
//             new UnitPrefix("yotta",     "Y",    24)
    };

    public static readonly UnitPrefix[] numberalPrefixes = {
        new UnitPrefix("",              "",    0),
        new UnitPrefix("thousand ",      "",    3),
        new UnitPrefix("million ",       "",    6),
        new UnitPrefix("billion ",       "",    9),
//            new UnitPrefix("trillion",      "",    12),
//            new UnitPrefix("quadrillion",   "",    15),
    };

    public static readonly UnitPrefix[] timePrefixes =
    {
        new UnitPrefix("second", "s", 1),
        new UnitPrefix("minute", "min", 60),
        new UnitPrefix("hour", "h", 60*60),
        new UnitPrefix("day", "d", 60*60*24),
        // In astronomy, the Julian year is a unit of time; it is defined as 365.25 days
        // of exactly 86400 seconds (SI base unit), totalling exactly 31557600 seconds 
        // in the Julian astronomical year.
        //new UnitPrefix("year", "y", 31557600),
        // But, Nuclear Physicists are thinking that year is 365.24219878 days or
        // 31556925.97f seconds, that could be round to 31556926 seconds
        new UnitPrefix("year", "y", Mathf.RoundToInt(60*60*24*365.24219878f)),
    };

    public static UnitPrefix GetUnitPowerPrefix(float number, UnitPrefix[] prefixes)
    {
        var num = Mathf.Abs(number);
        var pow = Mathf.Log10(num);
        var prefixIndex = prefixes.Length - 1;
        while (prefixIndex > 0 && pow < prefixes[prefixIndex].value)
        {
            prefixIndex--;
        }
        return prefixes[prefixIndex]; 
    }

    public static UnitPrefix GetUnitPrefix(float number, UnitPrefix[] prefixes)
    {
        int idx = prefixes.Length - 1;
        while (idx>0 && prefixes[idx].value > number)
        {
            idx--;
        }
        return prefixes[idx];
    }

    public static string GetTimePrefixed(float seconds)
    {
        var resultString = "";

        // format fractions of seconds
        if (seconds < 1)
        {
            // get best multiplier prefix
            var powerPrefix = GetUnitPowerPrefix(seconds, siUnitPrefixes);
            var mulPref = Mathf.Pow(10, powerPrefix.value);
            var multiplied = seconds / mulPref;
            // calculate remaining exponent after scaling
            var remainingLog = Mathf.Log10(multiplied);

            // there is a meaningful value
            if (remainingLog >= -3)
            {
                resultString = String.Format("{0} {1}seconds", multiplied.ToString("0.####"), powerPrefix.text);
            }
            else
            {
                var muchLower = remainingLog < -6;
                resultString = String.Format("{0} 0.001 {1}seconds", muchLower ? "<<" : "<" , powerPrefix.text);
            }
        }
        // format numbers bigger than 1 second
        else
        {
            // get scaling prefix (seconds, days ... years)
            var unitPrefix = GetUnitPrefix(seconds, timePrefixes);
            var units = seconds / unitPrefix.value;

            // get power prefix same as in seconds
            var numeralPrefix = GetUnitPowerPrefix(units, numberalPrefixes);
            var mulPref = Mathf.Pow(10, numeralPrefix.value);
            var multiplied = units / mulPref;
            // calculate remaining exponent after scaling
            var remainingLog = Mathf.Log10(multiplied);

            if (remainingLog <= 3)
            {
                resultString = String.Format("{0} {1}{2}s", multiplied.ToString("0.###"), numeralPrefix.text, unitPrefix.text);
            }
            else
            {
                var muchHigher = remainingLog > 6;

                resultString = String.Format("{0} 1000 {1}{2}s", muchHigher ? ">>" : ">", numeralPrefix.text, unitPrefix.text);
            }
        }
        return resultString;
    }

    // nuclear radius constant, as per https://en.wikipedia.org/wiki/Atomic_nucleus#Nuclear_models
    public const float EstimatedNucluesSizeConstant = 1.25e-15f;

    public static float GetNucleusRadii(int atomMass)
    {
        return EstimatedNucluesSizeConstant * (float)Math.Pow(atomMass, 1.0f / 3);
    }
}
