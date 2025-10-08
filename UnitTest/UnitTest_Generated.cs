using Xunit;
using DemoUnitTest_ConsoleApp;
using System;

namespace UnitTest
{
    public class CalculatorTests
    {
        private readonly Calculator _calc = new();

        [Fact]
        public void Add_ShouldReturnCorrectSum()
        {
            Assert.Equal(8, _calc.Add(3, 5));
        }

        [Theory]
        [InlineData(10, 3, 7)]
        [InlineData(-2, 5, -7)]
        [InlineData(0, 0, 0)]
        public void Subtract_ShouldReturnCorrectResult(int a, int b, int expected)
        {
            Assert.Equal(expected, _calc.Subtract(a, b));
        }

        [Fact]
        public void Multiply_ShouldReturnCorrectProduct()
        {
            Assert.Equal(20, _calc.Multiply(4, 5));
        }

        [Theory]
        [InlineData(6, 3, 2)]
        [InlineData(5, 2, 2.5)]
        public void Divide_ShouldReturnCorrectQuotient(double a, double b, double expected)
        {
            Assert.Equal(expected, _calc.Divide(a, b), 5);
        }

        [Fact]
        public void Divide_ByZero_ShouldThrowException()
        {
            Assert.Throws<DivideByZeroException>(() => _calc.Divide(5, 0));
        }
    }
}
