# What has not yet been implemented

Most of the v3 features are already implemented and can be tested via the hands-on demo. Work remaining:

- All work relating to the Visual Studio extension:

    - Update the *Unit Test Project* to include the new test platform
    - Add a new *Unit Test Debug Project* is added.
    - Virtual device: option to start the nanoClr that is specified in nano.runsettings for the current solution.

- Creation of NuGet packages:

    - Creation of the nanoFramework.TestFramework.Core package that contains only the nanoFramework.TestFramework library
    - Creation of the nanoFramework.TestFramework.UnitTestsProject package that includes the test adapter and MSBuild tool to support the *Unit Test Project*; this has the nanoFramework.TestFramework.Core package as dependency.
    - Creation of the nanoFramework.TestFramework.DebugTestsProject package, to support the *Unit Test Debug Project*
    - Creation of the nanoFramework.TestFramework.Tooling package, required to test more complex test framework extensions.
    - Creation of the nanoFramework.TestFramework.Tooling.Shared package, companion to nanoFramework.TestFramework.Tooling.


- Documentation on using VSTest
