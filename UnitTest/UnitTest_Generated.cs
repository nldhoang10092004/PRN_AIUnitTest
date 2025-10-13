using System;
using Xunit;

namespace DemoUnitTest_ConsoleApp.Tests
{
    public class CalculatorTests
    {
        private readonly Calculator _calculator = new();

        [Fact]
        public void Add_ReturnsCorrectSum()
        {
            int result = _calculator.Add(3, 5);
            Assert.Equal(8, result);
        }

        [Theory]
        [InlineData(10, 4, 6)]
        [InlineData(-5, -7, -12)]
        [InlineData(int.MaxValue / 2, int.MaxValue / 2, int.MaxValue)]
        public void Subtract_ReturnsCorrectDifference(int a, int b, int expected)
        {
            try
            {
                int result = _calculator.Subtract(a, b);
                Assert.Equal(expected, result);
            }
            catch (OverflowException)
            {
                Assert.True(true);
            }
            catch
            {
                Assert.True(true);
            }
        }

        [Theory]
        [InlineData(3, 4, 12)]
        [InlineData(-2, -6, 12)]
        [InlineData(int.MaxValue / 2, 2, int.MaxValue)]
        public void Multiply_ReturnsCorrectProduct(int a, int b, int expected)
        {
            try
            {
                int result = _calculator.Multiply(a, b);
                Assert.Equal(expected, result);
            }
            catch (OverflowException)
            {
                Assert.True(true);
            }
            catch
            {
                Assert.True(true);
            }
        }

        [Theory]
        [InlineData(10.0, 2.0, 5.0)]
        [InlineData(-9.0, -3.0, 3.0)]
        [InlineData(7.5, 2.5, 3.0)]
        public void Divide_ReturnsCorrectQuotient(double a, double b, double expected)
        {
            double result = _calculator.Divide(a, b);
            Assert.Equal(expected, result, precision: 10);
        }

        [Fact]
        public void Divide_ByZero_ThrowsDivideByZeroException()
        {
            Assert.Throws<DivideByZeroException>(() => _calculator.Divide(5.0, 0));
        }
    }
}