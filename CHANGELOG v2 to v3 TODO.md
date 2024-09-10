# What has not yet been implemented

Most of the v3 features are already implemented and can be tested via the hands-on demo. Work remaining:

- All work relating to the Visual Studio extension:

    - Update the *Unit Test Project* to include the new test platform
    - Add a new *Unit Test Debug Project* is added.
    - Virtual device: option to start the nanoClr that is specified in nano.runsettings for the current solution.

- Creation of NuGet packages:

    - Creation of the nanoFramework.TestFramework.Core package that contains only the nanoFramework.TestFramework library
    - Creation of the nanoFramework.TestFramework.TestProject package that includes the test adapter and MSBuild tool to support the *Unit Test Project*; this has the nanoFramework.TestFramework.Core package as dependency.
    - Creation of the nanoFramework.TestFramework.DebugTestProject package, to support the *Unit Test Debug Project*
    - Creation of the nanoFramework.TestFramework.Tooling package, required to test more complex test framework extensions.
    - Creation of the nanoFramework.TestFramework.Tooling.Shared package, companion to nanoFramework.TestFramework.Tooling.


- Update of the nf_CoreLibrary

    The nanoFramework.TestFramework is used as submodule in the nf_CoreLibrary repository. The unit tests in that repository reference the v2 test framework directly. If the unit tests should keep using the v2 test framework, the path of the references should be updated. If the unit tests start using v3:
 
    - A new project should be added that links the source of nanoFramework.TestFramework (similar to nanoFramework.TestFramework.Tooling / nanoFramework.TestFramework.Tooling.Tests). The nanoFramework.TestFramework project cannot be used as it may be based on an old version of the core library. 
    - The v3 test adapter should be referenced directly (similar to v3\poc\ByReference)
    - The submodule reference should be updated (should be done after accepting the PR)

No other (nanoFramework) repositories references nanoFramework.TestFramework directly.
