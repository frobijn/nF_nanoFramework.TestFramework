// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using nanoFramework.TestFramework.Tooling;
using TestFramework.Tooling.Tests.Helpers;


/*
    There seems to be something off with the dependency relations between some Microsoft assemblies.
    When running this code from a unit test, an exception may be thrown in ProjectSourceAnalyzer that has to do
    with the dependency of System.Memory on System.Runtime.CompilerServices.Unsafe. This can be solved by adding
    to the file:

C:\Program Files\Microsoft Visual Studio\...year...\...version...\Common7\IDE\Extensions\TestPlatform\testhost.net48.exe.Config

    the binding redirect:

     <dependentAssembly>
        <assemblyIdentity name="System.Runtime.CompilerServices.Unsafe" publicKeyToken="b03f5f7f11d50a3a" culture="neutral" />
        <bindingRedirect oldVersion="1.0.0.0-6.0.0.0" newVersion="6.0.0.0" />
     </dependentAssembly>

    The System.Runtime.CompilerServices.Unsafe assembly in the TestFramework.Tooling bin directory originates
    from the Microsoft.CodeAnalysis.CSharp nuget package dependencies.
 */


namespace TestFramework.Tooling.Tests
{
    [TestClass]
    public sealed class ProjectSourceInventoryTest
    {

        #region Test for parsing source code
        [TestMethod]
        [TestCategory("Source code")]
        public void Parse_SingleTestClass()
        {
            #region Source code
            string sourceCode1 = @"using nanoFramework.TestFramework;

namespace ProjectSourceTest.TestFramework.Tooling.Tests
{
    [TestClass]
    public static class SomeTest
    {
        [TestMethod]
        public void Method2 ()
        {
        }

        [DataRow]
        [DataRow]
        public void Method2 ()
        {
        }
    }

    public partial class SomeOtherTest
    {
        [TestMethod]
        [Trait(""trait 1"")]
        [Trait(""trait 2"")]
        public void Method2 ()
        {
        }
    }
}
";
            string sourceCode2 = @"using nanoFramework.TestFramework;

namespace ProjectSourceTest.TestFramework.Tooling.Tests
{
    [My.Namespace.SpecialTestClass]
    public partial class SomeOtherTest
    {
        [TestMethod]
        // Some comments for good measure
        [TestForVirtualDevice, myNamespace.Special]
        [RunInParallel]
        public void Method2 ()
        {
        }
    }
}
";
            #endregion

            var logger = new LogMessengerMock();
            var actual = new ProjectSourceInventory(new (string, string)[]
            {
                ("SomeTestClasses.cs", sourceCode1),
                ("SomeOtherTest.cs", sourceCode2)
            }, logger);

            Assert.AreEqual(0, logger.Messages.Count);

            // All classes
            Assert.AreEqual(
                "ProjectSourceTest.TestFramework.Tooling.Tests.SomeTest in SomeTestClasses.cs(4,4)" + "\n" +
                "ProjectSourceTest.TestFramework.Tooling.Tests.SomeOtherTest in SomeTestClasses.cs(19,4)",
                string.Join("\n",
                    from c in actual.ClassDeclarations
                    select $"{c.Name} in {c.SourceFilePath}({c.LineNumber},{c.Position})"
                )
            );

            // SomeTest : attributes
            Assert.AreEqual(
                "TestClass in SomeTestClasses.cs(4,5)",
                string.Join("\n",
                    from a in actual.TryGet("ProjectSourceTest.TestFramework.Tooling.Tests.SomeTest").Attributes
                    select $"{a.Name} in {a.SourceFilePath}({a.LineNumber},{a.Position})"
                )
            );

            // SomeTest : methods
            Assert.AreEqual(
                "Method2 in SomeTestClasses.cs(7,8)" + "\n" +
                "Method2 in SomeTestClasses.cs(12,8)",
                string.Join("\n",
                    from m in actual.TryGet("ProjectSourceTest.TestFramework.Tooling.Tests.SomeTest").Methods
                    select $"{m.Name} in {m.SourceFilePath}({m.LineNumber},{m.Position})"
                )
            );

            // SomeTest, Method2: attributes
            Assert.AreEqual(
                "TestMethod in SomeTestClasses.cs(7,9)",
                string.Join("\n",
                    from a in
                        (from m in actual.TryGet("ProjectSourceTest.TestFramework.Tooling.Tests.SomeTest").Methods
                         where m.Name == "Method2"
                         select m).First().Attributes
                    select $"{a.Name} in {a.SourceFilePath}({a.LineNumber},{a.Position})"
                )
            );

            // SomeTest, Method2: attributes
            Assert.AreEqual(
                "DataRow in SomeTestClasses.cs(12,9)" + "\n" +
                "DataRow in SomeTestClasses.cs(13,9)",
                string.Join("\n",
                    from a in
                        (from m in actual.TryGet("ProjectSourceTest.TestFramework.Tooling.Tests.SomeTest").Methods
                         where m.Name == "Method2"
                         select m).First().Attributes
                    select $"{a.Name} in {a.SourceFilePath}({a.LineNumber},{a.Position})"
                )
            );

            // SomeOtherTest : attributes
            Assert.AreEqual(
                "SpecialTestClass in SomeOtherTest.cs(4,5)",
                string.Join("\n",
                    from a in actual.TryGet("ProjectSourceTest.TestFramework.Tooling.Tests.SomeOtherTest").Attributes
                    select $"{a.Name} in {a.SourceFilePath}({a.LineNumber},{a.Position})"
                )
            );

            // SomeOtherTest : methods
            Assert.AreEqual(
                "Method2 in SomeTestClasses.cs(21,8)" + "\n" +
                "Method2 in SomeOtherTest.cs(7,8)",
                string.Join("\n",
                    from m in actual.TryGet("ProjectSourceTest.TestFramework.Tooling.Tests.SomeOtherTest").Methods
                    select $"{m.Name} in {m.SourceFilePath}({m.LineNumber},{m.Position})"
                 )
            );

            // SomeOtherTest, Method2: attributes
            Assert.AreEqual(
                "TestMethod in SomeTestClasses.cs(21,9)" + "\n" +
                "Trait in SomeTestClasses.cs(22,9)" + "\n" +
                "Trait in SomeTestClasses.cs(23,9)",
                string.Join("\n",
                    from a in
                        (from m in actual.TryGet("ProjectSourceTest.TestFramework.Tooling.Tests.SomeOtherTest").Methods
                         where m.Name == "Method2"
                         select m).First().Attributes
                    select $"{a.Name} in {a.SourceFilePath}({a.LineNumber},{a.Position})"
                )
            );



            // SomeOtherTest, Method2: attributes
            Assert.AreEqual(
                "TestMethod in SomeOtherTest.cs(7,9)" + "\n" +
                "TestForVirtualDevice in SomeOtherTest.cs(9,9)" + "\n" +
                "Special in SomeOtherTest.cs(9,31)" + "\n" +
                "RunInParallel in SomeOtherTest.cs(10,9)",
                string.Join("\n",
                    from a in
                        (from m in actual.TryGet("ProjectSourceTest.TestFramework.Tooling.Tests.SomeOtherTest").Methods
                         where m.Name == "Method2"
                         select m).First().Attributes
                    select $"{a.Name} in {a.SourceFilePath}({a.LineNumber},{a.Position})"
                )
            );
        }
        #endregion

        #region Test for reading project and sanity check
        [TestMethod]
        [TestCategory("Source code")]
        public void Parse_TestFramework_Tooling_Tests_NFUnitTest()
        {
            (ProjectSourceInventory actual, string pathPrefix) = TestProjectSourceAnalyzer.FindAndCreateProjectSource("TestFramework.Tooling.Tests.NFUnitTest", true);

            Assert.AreEqual(
                $"TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes in {pathPrefix}TestAllCurrentAttributes.cs(8,4)" + "\n" +
                $"TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods in {pathPrefix}TestWithMethods.cs(4,4)",
                string.Join("\n",
                    from c in actual.ClassDeclarations
                    select $"{c.Name} in {c.SourceFilePath}({c.LineNumber},{c.Position})"
                )
            );
        }

        [TestMethod]
        [TestCategory("Source code")]
        public void Parse_TestFramework_Tooling_Tests_NFUnitTest_New()
        {
            (ProjectSourceInventory actual, string pathPrefix) = TestProjectSourceAnalyzer.FindAndCreateProjectSource("TestFramework.Tooling.Tests.NFUnitTest.New", true);

            Assert.AreEqual(
                $"TestFramework.Tooling.Tests.NFUnitTest.StaticTestClassRunInParallel in {pathPrefix}TestClassVariants.cs(18,4)" + "\n" +
                $"TestFramework.Tooling.Tests.NFUnitTest.StaticTestClassRunOneByOne in {pathPrefix}TestClassVariants.cs(7,4)" + "\n" +
                $"TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes in {pathPrefix}TestAllCurrentAttributes.cs(8,4)" + "\n" +
                $"TestFramework.Tooling.Tests.NFUnitTest.TestClassInstantiateOnceForAllMethodsRunOneByOne in {pathPrefix}TestClassVariants.cs(29,4)" + "\n" +
                $"TestFramework.Tooling.Tests.NFUnitTest.TestClassInstantiatePerMethodRunInParallel in {pathPrefix}TestClassVariants.cs(65,4)" + "\n" +
                $"TestFramework.Tooling.Tests.NFUnitTest.TestClassInstantiatePerMethodRunOneByOne in {pathPrefix}TestClassVariants.cs(47,4)" + "\n" +
                $"TestFramework.Tooling.Tests.NFUnitTest.TestFrameworkExtensions.BrokenAfterRefactoringAttribute in {pathPrefix}TestFrameworkExtensions\\BrokenAfterRefactoringAttribute.cs(8,4)" + "\n" +
                $"TestFramework.Tooling.Tests.NFUnitTest.TestFrameworkExtensions.TestOnDoublePrecisionDeviceAttribute in {pathPrefix}TestFrameworkExtensions\\TestOnDoublePrecisionDeviceAttribute.cs(9,4)" + "\n" +
                $"TestFramework.Tooling.Tests.NFUnitTest.TestWithFrameworkExtensions in {pathPrefix}TestWithFrameworkExtensions.cs(8,4)" + "\n" +
                $"TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods in {pathPrefix}TestWithMethods.cs(8,4)" + "\n" +
                $"TestFramework.Tooling.Tests.NFUnitTest.TestWithNewTestMethodsAttributes in {pathPrefix}TestWithNewTestMethodsAttributes.cs(7,4)",
            string.Join("\n",
            from c in actual.ClassDeclarations
            orderby c.Name
            select $"{c.Name} in {c.SourceFilePath}({c.LineNumber},{c.Position})"
                )
            );
        }
        #endregion
    }
}
