using System;
using System.Globalization;

namespace Weedtopia.Domain
{
    /// <summary>Signed base-ten value with 12 significant digits, independent of Unity and locale.</summary>
    [Serializable]
    public struct Amount : IComparable<Amount>, IEquatable<Amount>
    {
        public double mantissa;
        public int exponent;
        public static Amount Zero => default;
        public bool IsValid => !double.IsNaN(mantissa) && !double.IsInfinity(mantissa) &&
            Math.Abs(exponent) <= 1000000 && (mantissa == 0 ? exponent == 0 : Math.Abs(mantissa) >= 1 && Math.Abs(mantissa) < 10);
        public Amount(double value, int power = 0)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) throw new ArgumentOutOfRangeException(nameof(value));
            mantissa = value; exponent = power;
            if (value == 0) { exponent = 0; return; }
            int shift = (int)Math.Floor(Math.Log10(Math.Abs(value)));
            // Split subnormal exponents to avoid Pow underflow.
            if (shift < -300) { value *= 1e300; power -= 300; shift += 300; }
            mantissa = Math.Round(value / Math.Pow(10, shift), 11, MidpointRounding.AwayFromZero);
            exponent = checked(power + shift);
            if (Math.Abs(mantissa) >= 10) { mantissa /= 10; exponent++; }
            if (!IsValid) throw new ArgumentOutOfRangeException(nameof(power));
        }
        public static Amount FromLog10(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || Math.Abs(value) > 999999) throw new ArgumentOutOfRangeException(nameof(value));
            int power = (int)Math.Floor(value);
            return new Amount(Math.Pow(10, value - power), power);
        }
        public static implicit operator Amount(int n) => new Amount(n);
        public static implicit operator Amount(double n) => new Amount(n);
        public static Amount operator +(Amount a, Amount b)
        {
            if (a.mantissa == 0) return b;
            if (b.mantissa == 0) return a;
            int power = Math.Max(a.exponent, b.exponent);
            return new Amount(a.mantissa * Math.Pow(10, Math.Max(-320, a.exponent - power)) +
                b.mantissa * Math.Pow(10, Math.Max(-320, b.exponent - power)), power);
        }
        public static Amount operator -(Amount a, Amount b) => a + new Amount(-b.mantissa, b.exponent);
        public static Amount operator *(Amount a, Amount b) => new Amount(a.mantissa * b.mantissa, checked(a.exponent + b.exponent));
        public static Amount operator /(Amount a, Amount b) => b.mantissa == 0 ? throw new DivideByZeroException() :
            new Amount(a.mantissa / b.mantissa, checked(a.exponent - b.exponent));
        public static bool operator <(Amount a, Amount b) => a.CompareTo(b) < 0;
        public static bool operator >(Amount a, Amount b) => a.CompareTo(b) > 0;
        public static bool operator <=(Amount a, Amount b) => a.CompareTo(b) <= 0;
        public static bool operator >=(Amount a, Amount b) => a.CompareTo(b) >= 0;
        public static bool operator ==(Amount a, Amount b) => a.Equals(b);
        public static bool operator !=(Amount a, Amount b) => !a.Equals(b);
        public int CompareTo(Amount other)
        {
            int sign = Math.Sign(mantissa).CompareTo(Math.Sign(other.mantissa));
            if (sign != 0) return sign;
            if (mantissa == 0) return 0;
            int e = exponent.CompareTo(other.exponent);
            return e == 0 ? mantissa.CompareTo(other.mantissa) : e * Math.Sign(mantissa);
        }
        public bool Equals(Amount other) => mantissa.Equals(other.mantissa) && exponent == other.exponent;
        public override bool Equals(object obj) => obj is Amount a && Equals(a);
        public override int GetHashCode() => mantissa.GetHashCode() ^ exponent;
        public static Amount Min(Amount a, Amount b) => a < b ? a : b;
        public static Amount Max(Amount a, Amount b) => a > b ? a : b;
        public Amount Floor() => exponent >= 11 ? this : exponent < 0 ? new Amount(Math.Floor(ToDouble())) : new Amount(Math.Floor(ToDouble()));
        public double ToDouble() => mantissa * Math.Pow(10, exponent);
        public string Serialize() => mantissa.ToString("R", CultureInfo.InvariantCulture) + "e" + exponent.ToString(CultureInfo.InvariantCulture);
        public static Amount Parse(string text)
        {
            int at = text.LastIndexOf('e');
            return at < 0 ? new Amount(double.Parse(text, CultureInfo.InvariantCulture)) :
                new Amount(double.Parse(text.Substring(0, at), CultureInfo.InvariantCulture), int.Parse(text.Substring(at + 1), CultureInfo.InvariantCulture));
        }
        public override string ToString()
        {
            if (mantissa == 0) return "0";
            if (exponent < 3 && exponent > -3) return ToDouble().ToString("0.##", CultureInfo.InvariantCulture);
            string[] suffix = { "", "K", "M", "B", "T", "Qa", "Qi" };
            int group = exponent / 3;
            return group > 0 && group < suffix.Length
                ? (mantissa * Math.Pow(10, exponent % 3)).ToString("0.##", CultureInfo.InvariantCulture) + suffix[group]
                : mantissa.ToString("0.##", CultureInfo.InvariantCulture) + "e" + exponent;
        }
    }
}
