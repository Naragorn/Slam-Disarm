using SlamDisarm.Logic;
using Xunit;

namespace SlamDisarm.Tests
{
    public class ChanceTests
    {
        [Theory]
        [InlineData(1f, 1f, true)]
        [InlineData(1f, 0.99f, true)]
        [InlineData(0f, 0f, false)]
        [InlineData(0.5f, 0.49f, true)]
        [InlineData(0.5f, 0.5f, false)]
        public void Hits(float chance, float roll, bool expected)
        {
            Assert.Equal(expected, Chance.Hits(chance, roll));
        }
    }
}
