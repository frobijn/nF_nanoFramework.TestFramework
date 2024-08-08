using nanoFramework.TestFramework;

namespace TestFramework.Tooling.Tests.NFUnitTest
{
    /// <summary>
    /// This class uses only the current TestFramework attributes but is
    /// analysed as a  new-style test as this project uses new test attributes.
    /// </summary>
    [TestClass]
    public class TestAllCurrentAttributes
    {
        [TestMethod]
        public void TestMethod()
        {
        }

        [DataRow(1, "1")]
        [DataRow(2, "2")]
        public void TestMethod1(int actual, string expected)
        {
        }

        [Setup]
        public void Setup()
        {
        }

        [Cleanup]
        public void Cleanup()
        {
        }
    }
}
