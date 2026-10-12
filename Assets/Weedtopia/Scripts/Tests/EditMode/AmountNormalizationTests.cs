using System;
using NUnit.Framework;
using Weedtopia.Domain;

namespace Weedtopia.Tests
{
    public sealed class AmountNormalizationTests
    {
        [TestCase(1.0, 250, "1e250")]
        [TestCase(9.0, -250, "9e-250")]
        [TestCase(-1.0, -250, "-1e-250")]
        [TestCase(-9.0, 250, "-9e250")]
        [TestCase(1.234567890123, 17, "1.23456789012e17")]
        [TestCase(-1.234567890123, 17, "-1.23456789012e17")]
        [TestCase(8.999999999994, 17, "8.99999999999e17")]
        [TestCase(1.000000000001, 17, "1e17")]
        public void UnitDecimalIntervalStillUsesTwelveSignificantDigits(double value, int power, string expected)
        {
            Assert.That(new Amount(value, power).Serialize(), Is.EqualTo(expected));
        }

        [Test]
        public void ValuesImmediatelyAroundIntervalAndCarryBoundaryNormalizeCorrectly()
        {
            double belowOne = BitConverter.Int64BitsToDouble(BitConverter.DoubleToInt64Bits(1) - 1);
            double aboveNine = BitConverter.Int64BitsToDouble(BitConverter.DoubleToInt64Bits(9) + 1);
            double belowTen = BitConverter.Int64BitsToDouble(BitConverter.DoubleToInt64Bits(10) - 1);
            Assert.That(new Amount(belowOne, 25).Serialize(), Is.EqualTo("1e25"));
            Assert.That(new Amount(aboveNine, 25).Serialize(), Is.EqualTo("9e25"));
            Assert.That(new Amount(belowTen, 25).Serialize(), Is.EqualTo("1e26"));
            Assert.That(new Amount(10, 25).Serialize(), Is.EqualTo("1e26"));
            Assert.That(new Amount(-belowTen, 25).Serialize(), Is.EqualTo("-1e26"));
        }

        [Test]
        public void UnitIntervalRetainsExponentLimitsAndRejectsNonFiniteInputs()
        {
            Assert.That(new Amount(1, 1000000).IsValid, Is.True);
            Assert.That(new Amount(9, -1000000).IsValid, Is.True);
            Assert.Throws<ArgumentOutOfRangeException>(() => new Amount(1, 1000001));
            Assert.Throws<ArgumentOutOfRangeException>(() => new Amount(-9, -1000001));
            Assert.Throws<ArgumentOutOfRangeException>(() => new Amount(double.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => new Amount(double.PositiveInfinity));
            Assert.Throws<ArgumentOutOfRangeException>(() => new Amount(double.NegativeInfinity));
            Assert.That(new Amount(0, int.MaxValue).Serialize(), Is.EqualTo("0e0"));
            Assert.That(new Amount(double.Epsilon).IsValid, Is.True);
        }
    }
}
