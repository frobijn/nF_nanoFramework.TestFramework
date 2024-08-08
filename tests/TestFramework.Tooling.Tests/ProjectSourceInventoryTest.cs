// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
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
        public void Parse_SomeTestClasses()
        {
            #region Source code
            string sourceCode1 = @"using nanoFramework.TestFramework;

namespace ProjectSourceTest.TestFramework.Tooling.Tests
{
    [TestClass]
    public static class SomeTest
    {
        [TestMethod]
        public void Method1 ()
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
        public void Method3 ()
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
        public void Method4 ()
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

            Assert.AreEqual(
$@"
".Replace("\r\n", "\n"),
                string.Join("\n",
                        from m in logger.Messages
                        select $"{m.level}: {m.message}"
                    ) + '\n'
            );

            // All classes
            Assert.AreEqual(
@"ProjectSourceTest.TestFramework.Tooling.Tests.SomeTest in SomeTestClasses.cs(4,4)
ProjectSourceTest.TestFramework.Tooling.Tests.SomeOtherTest in SomeTestClasses.cs(19,4)
".Replace("\r\n", "\n"),
                string.Join("\n",
                    from c in actual.ClassDeclarations
                    select $"{c.Name} in {c.SourceFilePath}({c.LineNumber},{c.Position})"
                ) + '\n'
            );

            // SomeTest : attributes
            Assert.AreEqual(
@"TestClass in SomeTestClasses.cs(4,5)
".Replace("\r\n", "\n"),
                string.Join("\n",
                    from a in actual.TryGet("ProjectSourceTest.TestFramework.Tooling.Tests.SomeTest").Attributes
                    select $"{a.Name} in {a.SourceFilePath}({a.LineNumber},{a.Position})"
                ) + '\n'
            );

            // SomeTest : methods
            Assert.AreEqual(
@"Method1 in SomeTestClasses.cs(8,20)
Method2 in SomeTestClasses.cs(14,20)
".Replace("\r\n", "\n"),
                string.Join("\n",
                    from m in actual.TryGet("ProjectSourceTest.TestFramework.Tooling.Tests.SomeTest").Methods
                    select $"{m.Name} in {m.SourceFilePath}({m.LineNumber},{m.Position})"
                ) + '\n'
            );

            // SomeTest, Method2: attributes
            Assert.AreEqual(
@"TestMethod in SomeTestClasses.cs(7,9)
".Replace("\r\n", "\n"),
                string.Join("\n",
                    from a in
                        (from m in actual.TryGet("ProjectSourceTest.TestFramework.Tooling.Tests.SomeTest").Methods
                         where m.Name == "Method1"
                         select m).First().Attributes
                    select $"{a.Name} in {a.SourceFilePath}({a.LineNumber},{a.Position})"
                ) + '\n'
            );

            // SomeTest, Method2: attributes
            Assert.AreEqual(
@"DataRow in SomeTestClasses.cs(12,9)
DataRow in SomeTestClasses.cs(13,9)
".Replace("\r\n", "\n"),
                string.Join("\n",
                    from a in
                        (from m in actual.TryGet("ProjectSourceTest.TestFramework.Tooling.Tests.SomeTest").Methods
                         where m.Name == "Method2"
                         select m).First().Attributes
                    select $"{a.Name} in {a.SourceFilePath}({a.LineNumber},{a.Position})"
                ) + '\n'
            );

            // SomeOtherTest : attributes
            Assert.AreEqual(
@"SpecialTestClass in SomeOtherTest.cs(4,5)
".Replace("\r\n", "\n"),
                string.Join("\n",
                    from a in actual.TryGet("ProjectSourceTest.TestFramework.Tooling.Tests.SomeOtherTest").Attributes
                    select $"{a.Name} in {a.SourceFilePath}({a.LineNumber},{a.Position})"
                ) + '\n'
            );

            // SomeOtherTest : methods
            Assert.AreEqual(
@"Method3 in SomeTestClasses.cs(24,20)
Method4 in SomeOtherTest.cs(11,20)
".Replace("\r\n", "\n"),
                string.Join("\n",
                    from m in actual.TryGet("ProjectSourceTest.TestFramework.Tooling.Tests.SomeOtherTest").Methods
                    select $"{m.Name} in {m.SourceFilePath}({m.LineNumber},{m.Position})"
                 ) + '\n'
            );

            // SomeOtherTest, Method2: attributes
            Assert.AreEqual(
@"TestMethod in SomeTestClasses.cs(21,9)
Trait in SomeTestClasses.cs(22,9)
Trait in SomeTestClasses.cs(23,9)
".Replace("\r\n", "\n"),
                string.Join("\n",
                    from a in
                        (from m in actual.TryGet("ProjectSourceTest.TestFramework.Tooling.Tests.SomeOtherTest").Methods
                         where m.Name == "Method3"
                         select m).First().Attributes
                    select $"{a.Name} in {a.SourceFilePath}({a.LineNumber},{a.Position})"
                ) + '\n'
            );



            // SomeOtherTest, Method2: attributes
            Assert.AreEqual(
@"TestMethod in SomeOtherTest.cs(7,9)
TestForVirtualDevice in SomeOtherTest.cs(9,9)
Special in SomeOtherTest.cs(9,31)
RunInParallel in SomeOtherTest.cs(10,9)
".Replace("\r\n", "\n"),
                string.Join("\n",
                    from a in
                        (from m in actual.TryGet("ProjectSourceTest.TestFramework.Tooling.Tests.SomeOtherTest").Methods
                         where m.Name == "Method4"
                         select m).First().Attributes
                    select $"{a.Name} in {a.SourceFilePath}({a.LineNumber},{a.Position})"
                ) + '\n'
            );
        }
        #endregion

        #region Test for reading project and sanity check
        [TestMethod]
        [TestCategory("Source code")]
        public void Parse_Discovery_v2()
        {
            (ProjectSourceInventory actual, string pathPrefix) = TestProjectHelper.FindAndCreateProjectSource("TestFramework.Tooling.Tests.Discovery.v2", true);

            Assert.AreEqual(
$@"TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes in {pathPrefix}TestAllCurrentAttributes.cs(8,4)
TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods in {pathPrefix}TestWithMethods.cs(4,4)
".Replace("\r\n", "\n"),
                string.Join("\n",
                    from c in actual.ClassDeclarations
                    select $"{c.Name} in {c.SourceFilePath}({c.LineNumber},{c.Position})"
                ) + '\n'
            );
        }

        [TestMethod]
        [TestCategory("Source code")]
        public void Parse_Discovery_v3()
        {
            (ProjectSourceInventory actual, string pathPrefix) = TestProjectHelper.FindAndCreateProjectSource("TestFramework.Tooling.Tests.Discovery.v3", true);

            Assert.AreEqual(
$@"TestFramework.Tooling.Tests.NFUnitTest.NonStaticTestClass in {pathPrefix}TestClassVariants.cs(27,4)
TestFramework.Tooling.Tests.NFUnitTest.StaticTestClass in {pathPrefix}TestClassVariants.cs(7,4)
TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes in {pathPrefix}TestAllCurrentAttributes.cs(8,4)
TestFramework.Tooling.Tests.NFUnitTest.TestFrameworkExtensions.BrokenAfterRefactoringAttribute in {pathPrefix}TestFrameworkExtensions\BrokenAfterRefactoringAttribute.cs(8,4)
TestFramework.Tooling.Tests.NFUnitTest.TestFrameworkExtensions.TestOnDeviceWithSomeFileAttribute in {pathPrefix}TestFrameworkExtensions\TestOnDeviceWithSomeFileAttribute.cs(8,4)
TestFramework.Tooling.Tests.NFUnitTest.TestWithALotOfErrors in {pathPrefix}TestWithALotOfErrors.cs(8,4)
TestFramework.Tooling.Tests.NFUnitTest.TestWithFrameworkExtensions in {pathPrefix}TestWithFrameworkExtensions.cs(8,4)
TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods in {pathPrefix}TestWithMethods.cs(8,4)
TestFramework.Tooling.Tests.NFUnitTest.TestWithNewTestMethodsAttributes in {pathPrefix}TestWithNewTestMethodsAttributes.cs(7,4)
".Replace("\r\n", "\n"),
            string.Join("\n",
                    from c in actual.ClassDeclarations
                    orderby c.Name
                    select $"{c.Name} in {c.SourceFilePath}({c.LineNumber},{c.Position})"
                ) + '\n'
            );
        }
        #endregion

        #region Helper test
        [TestMethod]
        [TestCategory("Source code")]
        public void FindProjectFilePathTest()
        {
            string projectFilePath = TestProjectHelper.FindProjectFilePath("TestFramework.Tooling.Tests.Discovery.v2");
            var logger = new LogMessengerMock();
            string actual = ProjectSourceInventory.FindProjectFilePath(Path.Combine(Path.GetDirectoryName(projectFilePath), "bin", "Release", "NFUnitTest.dll"), logger);
            Assert.AreEqual(0, logger.Messages.Count);
            Assert.AreEqual(projectFilePath, actual);

            projectFilePath = TestProjectHelper.FindProjectFilePath();
            logger = new LogMessengerMock();
            actual = ProjectSourceInventory.FindProjectFilePath(Path.Combine(Path.GetDirectoryName(projectFilePath), "bin", "Debug", "net48", Path.ChangeExtension(projectFilePath, ".dll")), logger);
            Assert.AreEqual(0, logger.Messages.Count);
            Assert.IsNull(actual);

            // Project directory with invalid project file
            logger = new LogMessengerMock();
            actual = ProjectSourceInventory.FindProjectFilePath(Path.Combine(Path.GetDirectoryName(projectFilePath), GetType().Name, "bin", "NFUnitTest.dll"), logger);
            Assert.AreEqual(1, logger.Messages.Count);
            Assert.IsNull(actual);

            // Same without logger
            actual = ProjectSourceInventory.FindProjectFilePath(Path.Combine(Path.GetDirectoryName(projectFilePath), GetType().Name, "bin", "NFUnitTest.dll"), null);
            Assert.IsNull(actual);
        }
        #endregion
    }
}
