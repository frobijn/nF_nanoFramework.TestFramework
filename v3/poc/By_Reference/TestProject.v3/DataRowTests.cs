// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using nanoFramework.TestFramework;

namespace NFUnitTest
{
    [TestClass]
    [TestCategory("Attributes")]
    public class TestOfDataRow
    {
        [TestMethod]
        [DataRow(1, 2, 3)]
        [DataRow(5, 6, 11)]
        public void TestAddition(int number1, int number2, int result)
        {
            int additionResult = number1 + number2;

            Assert.AreEqual(additionResult, result);
        }

        [TestMethod]
        [DataRow("TestString")]
        public void TestString(string testData)
        {
            Assert.AreEqual(testData, "TestString");
        }

        [TestMethod]
        [DataRow("adsdasdasasddassaadsdasdasasddassaadsdasdasasddassaadsdasdasasddassaadsdasdasasddassaadsdasdasasddassa")]
        public void TestLongString(string testData)
        {
            Assert.AreEqual(testData, "adsdasdasasddassaadsdasdasasddassaadsdasdasasddassaadsdasdasasddassaadsdasdasasddassaadsdasdasasddassa");
        }

        [TestMethod]
        [DataRow("Right align in 10 chars: {0,10:N2}: and then more", 1234.5641, "Right align in 10 chars:   1,234.56: and then more")]
        public void TestStringWithComma(string formatString, double value, string outcomeMessage)
        {
            // Test alignment operator which is the "," and a number. Negative is right aligned, positive left aligned
            Assert.AreEqual(string.Format(formatString, value), outcomeMessage);
        }
    }
}
