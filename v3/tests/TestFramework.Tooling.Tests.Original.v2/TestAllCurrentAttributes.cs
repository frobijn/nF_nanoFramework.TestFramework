using nanoFramework.TestFramework;

namespace TestFramework.Tooling.Tests.NFUnitTest
{
    /// <summary>
    /// This class is analysed in backward compatibility mode as the project
    /// only uses the current TestFramework attributes
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
