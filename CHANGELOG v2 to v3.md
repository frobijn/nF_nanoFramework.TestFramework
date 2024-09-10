# Changes from v2 to v3 

## Goal of v3

The initial goal of the v3 version of the TestFramework was to address some limitations of v2. The v2 unit test framework is already very capable, and its strengths should be preserved. But there is room to make the framework even more awesome. At the start of the v3 development some must-have requirements have been formulated:

- Ability to run selected tests only

    In v2 it was possible to *specify* that a single test should be executed, but that is not how it was *implemented*. All tests are run and only the result of the single test is published. This is problematic if there are special tests, e.g., tests that require a particular setup or performance tests, as they may take up a lot of time or even damage hardware.

- Test categories

    Test categories help organise the tests and allow a developer to easily select, execute and assess related tests. It is also one of the mechanism to prevent special tests from running in an automated build environment, where a filter can be specified to exclude test based on their test categories and a few other criteria. Test categories are not present in v2.

- Run tests on as many devices as required/possible

    In v2 tests can be run on either the virtual device (= `nanoclr.exe`) or on a single hardware device, via options in the nano.runsettings configuration. That is a viable mode of operation for tests in an automated build environment where tests are always executed in the same way. But for a developer working on the code this is cumbersome. If a selection of tests have to be run after a code change, the tests should be run on all devices the tests are supposed to be executed on (and that are available). This should not require a configuration change, as that would modify a file under source control.

- Run tests in parallel

    If tests from various projects are selected, in v2 the projects are run one by one on a device. There are good reasons *not* to run the tests within one project in parallel, but there is no excuse why the various projects cannot be executed in parallel. That is: multiple virtual devices if needed, and simultaneously on a virtual device and (multiple) hardware devices. There is a feature in Visual Studio/VSTest (Run Tests in Parallel) that works like this. That would work for tests that run on the virtual device, but not for tests that run on real hardware, as there would be multiple VS test processes that try to access the hardware. Parallelization has to be implemented in the test framework. This is in line with the approach taken by other test frameworks.

- Express device criteria in the test's source code

    At the time of coding a unit test, there usually is a clear understanding why the test is created. And on what device the test should be executed. Some tests only use core features from the nanoCLR and it is sufficient to execute the tests on the virtual device. Others are intended to be run on real hardware. And maybe even a particular type of device, as other devices may not have the hardware/CLR support required. The device criteria should not be limited to the device's platform or target, but also "make and model" criteria: some tests may require a lot of RAM or require that extra hardware (e.g., IoT devices) is present. It should be possible to express that.

    These criteria only make sense if they are taken into account when executing tests. The test engine (test adapter) should use the criteria to determine which of the available devices are suitable to run the tests on. If a test is designed to be run on various types of devices to verify the code works in different contexts, the test adapter should execute the test on multiple devices if they are considered a different "make and model" by the author of the test.

- Make "make and model"-related data available to the tests

    Some tests are designed to test code that is written for specific hardware (IoT device) connected to the MCU. Sometimes it even does not matter what MCU is used. But the hardware configuration (which pins are connected) may depend on the MCU and may not be predictable by the author of the test. It should be possible to pass this aspect of the "make and model"-description of the hardware to the unit tests. Cf. deployment configuration for "normal" applications that is never made part of the application, but the application has a generic way of reading the configuration.

- Instantiation of test classes

    In v2 test classes are never instantiated, and test methods are called as if they are static methods, even if they are not. The decision whether or not to instantiate a test class should not be made by the test framework but by the developer. If instantiation is not required or not desired, the developer should be able to use static test classes. If the developer wants to use fields in the test class, a non-static class should be used and that should be instantiated by the test framework.

- Make it easy to debug tests

    In v2 it is not possible to debug tests via the test explorer or otherwise. There are good (technical) reasons why that is the case. As unit tests are often the first users of software under development, it is not uncommon that a new test fails immediately because of imperfections in the source code. It is essential that the developer can easily debug a single test. The selection of the test should be designed in a way that it does not have to be stored in git/version control, as it is a local/per user feature.

- Support development with a frozen/old version of the nanoFramework.

    It is important that a developer can freeze the nanoFramework version to avoid interference of new nanoFramework features or bugs with the project that uses the framework. Or even go back to old versions, to fix problems in products released some time ago. This is especially relevant in CrowdStrike scenarios, if new features implement breaking changes, or if a fix for an old version cannot be based on a new version of the software/firmware. In v2 that is not possible as it always uses the global `nanoclr.exe` tool and even auto-updates that version - the developer cannot stop that. In v3 the developer can control the auto-update and can also use a local version of the tool.

## Additional improvements/changes

While working on the implementation of the must-have requirements, it turned out that there were other features that could easily be realised. These are incorporated in the solution:

- Improved presentation of the unit tests and reporting of the outcome

    In v2 the setup/cleanup methods are presented separately in the test explorer and if a setup/cleanup method fails, the errors are reported with that method (including method name). If tests are not ordered by class, it is not obvious how the methods are related to the actual tests. The general overview of loaded assemblies is not reported in the test explorer while it may have clues why a test failed. In v3 all output of the unit tests is presented with the tests, even if that requires duplication. The setup/cleanup methods are no longer presented as tests, but via the presentation of the result of a test it is made clear whether the test or a setup/cleanup method failed. A developer can also use that as a selection criterion in the test explorer.

    For unit tests that use data rows the parameters are visible in the test explorer. The source code link points to the data row attribute rather than to the start of the test method.

- Debugging via the test adapter is no longer supported

    Actually this is also unsupported in v2 but if a developer selected to debug a unit test, the test was executed without debugger without notification. This will surprise developers who are new to the nanoFramework and/or developers that do not use the nanoFramework often. In v3 the tests are not run if debugging is selected. Instead an error message is displayed to inform the user to use the new debug-unit-tests feature.

- No need to have a fixed name for the unit tests assembly

    In v2 the assembly name of a unit test has to be NFUnitTest.dll. In v3 there is no need for that. If a developer wants to debug tests from multiple assemblies via the same debug project (a special project type introduced in v3), the assembly names should *not* be the same for all unit test projects.s

- Default constructor and `IDisposable` as alternative for setup/cleanup methods; setup/cleanup per test method.

    Using a default constructor instead of setup and `IDisposable` for cleanup is a common pattern in test frameworks (e.g, NUnit, xUnit). It is also possible to specify whether an instance should be created per test method or setup/cleanup should be executed for each test method (as some test frameworks do), so that every test method is started with the same context even if other test methods result in a modified context.

- Extensible test framework

    The interaction between the attributes describing the tests and the test framework is no longer based on the names of attributes but on the interfaces the attributes implement. That makes it easy for a developer to create custom attributes. One use case is to define attributes with the same type as a .NET framework (MSTest, NUnit, xUnit) so that not only the source code but also unit tests can be shared between nanoFramework and .NET projects. Extending the test framework is required to define additional attributes that test for "make and model" information.

- Test framework exceptions are easily discernible from other exceptions

    All framework exceptions now have the same base class. This is useful in order to make a distinction in asserts between an exceptions that originate from asserts and exceptions from the source code under test. Assert methods that test for the latter should let the former pass unmodified.

- Library for building custom test tooling

    All functionality required for the test adapter and for debugging the unit tests is placed in a separate class library (`TestFramework.Tooling`), with accompanying unit tests. If a developer has a requirement for a custom tool to work with test projects, the library can provide classes that do most of the hard work. The custom tooling (that runs on .NET Framework) can get the full description of test cases in a (.NET nanoFramework) test assembly and even introduce custom attributes (in .NET nanoFramework) that are visible to the custom tooling.

- Test adapter should not be limited by its test host

    To resolve assembly version conflicts, the test framework uses a separate host for discovering and directing the execution of the tests. As a result, the TestFramework.Tooling can use updates for NuGet packages that previously prevented running the test adapter (underlying [VSTest issue](https://github.com/microsoft/vstest/issues/4775)).

- Protection against running tests simultaneously from multiple Visual Studio/VSTest instances

    Running tests in parallel from multiple Visual Studio/VSTest instances may cause problems for tests that should be run on real hardware, as multiple instances may try to access the same device. Similarly, running tests on a Virtual Device may use the same `nanoclr.exe` file that can be updated by other processes at the same time. Exclusive access control is implemented for obtaining access to a device and running tests on a device. It is intended as a protection mechanism; no attempt has been optimize running the tests in an optimal way. As the other nanoFramework applications do not use this protection mechanism, it only works as a protection in the test framework.

- The result of a single test is made available to Visual Studio/VSTest immediately after the test is completed, rather than after completion of all tests in a test assembly.

## Backward compatibility 
The v3 version is backward compatible with v2. A v2 test project does not have to be changed to work with the v3 test adapter and test framework; updating the NuGet packages should be sufficient to profit from most of the improvements. To benefit from all improvement, the project has to be modified but that can be done in stages if needed. The documentation includes a migration guide for v2 projects to v3.

There are four possible exceptions to the backward compatibility claim:

- Use of non-public test classes

    All test frameworks require that test classes are public, because they have to be accessible by code outside the test assembly and a fundamental premise of .NET is that only public classes are/should be externally visible. That is not enforced in the v2 test platform, and as it uses only reflection it can get away with that. The v3 test platform has to honour the fundamental.NET premises as it uses more of the .NET platform. This is a good thing, so no attempt has been made to reproduce the v2 behaviour of accessing non-public test classes.

- Use of a generated unit test launcher

    The generated unit test launcher is slightly larger than than the v2 unit test launcher. This may be an issue if the combined size of unit test assembly, test framework and nanoCLR .pe assemblies is already close to the capabilities of the hardware device. If this is a pressing concern, there are ways to slim down the generated code if knowledge could be made available on how a .NET assembly is transformed into a .pe assembly. It is not expected to be an issue for unit tests that are executed on a virtual device.

- Instantiation of test classes

    This could be an issue if the current unit tests are already close to the capabilities of the hardware device and instantiation of (many) test classes would exceed the capabilities.

- Use in automated build environments/vstest.console.exe

    The backward compatibility claim holds if the test projects are configured to run on the virtual device and are using nano.runsettings in the project's directory as propagated by the nanoFramework documentation/project templates. It is believed that the v3 test adapter will always be compatible with the v2 version, but it is impossible to conceive of all possible ways to configure VSTest and to verify that the v3 test adapter is working in the same way as the v2 adapter.

As the v3 test framework behaves quite differently from the v2 version, a nanoFramework user should not be surprised with an unexpected change from v2 to v3. Migrating from v2 to v3 is a choice that has to be made explicitly. The general nanoFramework documentation describes how to do that.

## Hands-on demo
A hands-on demo for all features are available in this repository:

- Go to the `v3\poc\By_Reference` directory
- See the [README.md](v3/poc/By_Reference/README.md) for further instructions.

The use of the v3 test framework is documented in the [general nanoFramework documentation](https://docs.nanoframework.net/content/unit-test/framework-v3/index.html).

## Development, testing and debugging

All v3 code and tests are collected in `nanoFramework.TestFramework.sln`. There are five nanoFramework applications/libraries in `source`:

- `nanoFramework.TestFramework` is the library to be used for nanoFramework-based unit tests.
- `nanoFramework.TestFramework.TestAdapter` is the test adapter that acts as a bridge between VSTest and `nanoFramework.TestFramework.TestHost`.
- `nanoFramework.TestFramework.TestHost` is the host for the discovery and execution of unit tests.
- `nanoFramework.TestFramework.DebugProjectBuildTool` is a build task/tool that generates code and is used by the new unit test debug project type.
- `nanoFramework.TestFramework.Tooling` is the implementation of most of the heavy lifting required for the discovery and execution of unit tests.

There are several test projects:

- `TestFramework.Tooling.Tests` contains the unit tests for `nanoFramework.TestFramework.Tooling` and `nanoFramework.TestFramework.Tooling.Shared`.
- `TestFramework.Tooling.BuildTools.Tests` contains the unit tests for `nanoFramework.TestFramework.DebugProjectBuildTool` and `nanoFramework.TestFramework.TestProjectBuildTool`.
- `TestFramework.TestAdapter.Tests` contains the unit tests for `nanoFramework.TestFramework.TestAdapter`.
- `TestFramework.Tooling.Tests.Original.v2`, `TestFramework.Tooling.Tests.Discovery.v2`, `TestFramework.Tooling.Tests.Discovery.v3`, `TestFramework.Tooling.Execution.Execution.v3` and `TestFramework.Tooling.Execution.Hardware_esp32.v3` are modified nanoFramework unit tests projects. The assemblies and project files are used by the unit tests in `TestFramework.Tooling.Tests` and `TestFramework.TestAdapter.Tests`.

Some of the unit tests require a connected real hardware device. If no device is available, the test will be skipped. The tests have *@Hardware nanoDevice* as trait, same as for nanoFramework tests that require real hardware. To exclude the tests from the Visual Studio Test Explorer, filter by *-Trait:@Hardware nanoDevice*.

Some of the tests require a computer with multiple (logical) processors to run the test on. The tests have *@Multiple logical processors* as trait.

The unit tests are using the MS Test platform and are configured to run in parallel. This works well if a subset of tests are selected from the Visual Studio Test Explorer to be run. If all tests are selected and run, some tests may fail. The root cause seems to be that the number of asynchronous tasks is so high that some of them start to suffer from hardcoded timeouts. And/or the start of new tasks is delayed so much that the sequence of (parallel) steps in a test is different from the sequence that occurs if the test is run in isolation. However, as some test take quite a while to finish, running all tests one after the other in Visual Studio makes development/maintenance of the code very slow. It is much quicker for a developer to run all tests in parallel, and rerun the few test again that incorrectly failed. For automated tests, MS Test should be configured to have `<Parallelize><Workers>1</Workers></Parallelize>` in the [MSTest runsettings](https://learn.microsoft.com/en-us/visualstudio/test/configure-unit-tests-by-using-a-dot-runsettings-file).

Most of the code can be debugged via the unit tests. There are a few exceptions:
- The code generated for the unit test launcher. Most of the code can be developed and debugged using the auxiliary project `TestFramework.Tooling.UnitTestLauncher.Tests`. It is a nanoFramework application that includes the same source code for the unit test launcher that is used for the generation of the unit test launcher. This is not a unit test debug project; no code generation is done in the project. Use this project to code and debug the unit test launcher code. The unit tests for the code generation, including running the unit test launcher on a virtual device, are part of `TestFramework.Tooling.Tests`.
- The `nanoFramework.TestFramework.TestHost` can be debugged:
    - Open the `nanoFramework.TestFramework` solution in Visual Studio.
    - Compile the test host with the `LAUNCHDEBUGGER` symbol defined
    - Open the `nanoFramework.TestFramework` solution in another Visual Studio instances.
    - Use one Visual Studio instance to run/debug one of the tests with trait *@Test host*. If the test host starts, a dialog opens; select the other Visual Studio instance (typically the top one) to debug the test host.
- The `nanoFramework.TestFramework.TestAdapter` can be debugged:
    - Open the `nanoFramework.TestFramework` solution in Visual Studio.
    - Compile the test host with the `LAUNCHDEBUGGER` symbol defined
    - If you want to debug the adapter as used from Visual Studio, open the `poc\By Reference\Demo By Reference` solution in another Visual Studio instance. Every time Visual Studio uses the test adapter, a dialog opens; select the Visual Studio instance with the `nanoFramework.TestFramework` solution to debug the adapter.
    - If you want to debug the adapter as used from the VSTest command line, use one of the `poc\By Reference\VSTest.*` scripts. If VSTest uses the test adapter, a dialog opens; select the Visual Studio instance with the `nanoFramework.TestFramework` solution to debug the adapter.
- The `nanoFramework.TestFramework.DebugProjectBuildTool` and `nanoFramework.TestFramework.TestProjectBuildTool` can be debugged:
    - Open the `nanoFramework.TestFramework` solution in Visual Studio.
    - Compile the test host with the `LAUNCHDEBUGGER` symbol defined
    - Open the `poc\By Reference\Demo By Reference` solution in another Visual Studio instance and rebuild `DebugProject` / `TestProject.v3`.
    - If MSBuild uses the custom task a dialog opens; select the Visual Studio instance with the `nanoFramework.TestFramework` solution to debug the custom task.

## Changes to the software architecture

The basic architecture of v2 still applies: VS test hosts call the test adapter for various functions, test adapter loads the unit test assembly to discover the test and orchestrate execution of the tests. The must-have requirements of v3 mainly concern the interaction between the test adapter and the unit test assembly in the discovery phase and the orchestration of running unit tests. Functionally this works the same in v2 and v3, apart from the new features, but technically it is implemented differently to overcome some technical challenges. In practical terms the code for the interaction of the test adapter with the devices and the installation/auto-updating of `nanoclr.exe` is largely unchanged, all other code had to be modified.

A major architectural change is that the discovery and orchestration of unit tests is no longer performed in the process that hosts the test adapter, but in a `nanoFramework.TestFramework.TestHost`. The test adapter implementation must be based on .NET Framework in order to work with the nanoFramework-based assemblies, but the VSTest host for the .NET Framework limits the use of newer versions of some NuGet packages. This is an open issue (for the VSTest project) that is not expected to be resolved soon, and is the reason that the v2 test adapter cannot be upgraded to use the latest CliWrap package. Resolution of this issue is required for v3 as it also inhibits the use of the Roslyn NuGet packages. Other test frameworks (e.g., xUnit) experienced or anticipated similar assembly version conflicts and solved that by hosting the discovery and orchestration software in a separate host process that is fully controlled by the test framework. The test adapter implements all required interfaces to communicate with VSTest and starts an instance of `nanoFramework.TestFramework.TestHost` to perform the actual work. The test adapter and `nanoFramework.TestFramework.TestHost` communicate via standard input/output. The test adapter has no dependencies on non-VSTest packages anymore.

Easy debugging of unit tests is implemented via a new type of project. This is essentially the same project as a nanoFramework application, with predefined source files and a MSBuild task (`nanoFramework.TestFramework.DebugProjectBuildTool`). The MSBuild task uses the same discovery mechanism as the test adapter to known which unit tests exist and obtain knowledge how to execute the tests, and adds the source code of the unit test launcher to the project to execute the selected test(s).

Other architectural changes/implementation aspects are:

- The v2 test framework code uses shared code. In v3 that is no longer the case.

    Code sharing is still used to make sure that projects targeted at different platforms (.NET, nanoCLR) have a common view of the type definitions. But this is needed only for the projects in the test framework. Other projects that want to use the test framework by reference (e.g., the CoreLibrary) should not know about that. Code sharing is now accomplished by adding references in the project files. Visual Studio 2022 fully supports this: it recognises this way of code sharing, Roslyn can analyse the code, in the editor it is possible to switch to the various projects the code is used in.

- Almost all 'business logic' is moved into a class library TestFramework.Tooling.

    The main reason it to make it easy to (unit) test the various features in isolation. If this library is made available via a NuGet package, developers that require custom test adapters/runners/analysers could rely on the library to do most of the heavy lifting.

- Source code analysis is done via Roslyn

    In v2 the source code was parsed using basic text analysis that made some assumptions about how the source code was formatted. Roslyn offers a more robust way. The implementation in v3 is still limited: only the source code of the unit test assemblies is analysed. If a test class in one assembly is derived from an test class in another assembly and inherits test/setup/cleanup methods, the source code for the inherited methods is not analysed.

- The unit test launcher is generated at runtime, separate project/application removed.

    To implement the requirement to run selective unit tests without running other tests in the assembly, a method had to be found to pass the selection of tests to the unit test launcher. As the "production" software should not be contaminated with features required only in the development phase, adding unit test extensions to the wire protocol for communication with the devices is undesirable. Instead, the unit test launcher application is generated for each selection of tests, compiled and converted to a .pe assembly using `nanoFramework.Tools.MetadataProcessor.CLI`.

- The `nanoFramework.TestFramework.DebugProjectBuildTool` is implemented as a console application rather than as a build task

    The build task loads unit test assemblies and their dependencies, but does not unload them. It is technically complex to unload assemblies, this would require loading assemblies in a different AppDomain from the discovery software. For performance reasons Visual Studio keeps MSBuild processes alive even after the build. If `nanoFramework.TestFramework.DebugProjectBuildTool` was implemented as a build task, the unit test assemblies and dependencies are locked and cannot be overwritten by subsequent builds. The easiest way to solve that is to make the build task an application that ends (and unloads the assemblies) after its work is done. A second reason is potential assembly/platform version conflicts, the same issue the test adapter suffered from and that was solved by introducing a custom test host.

- A full suite of unit tests covers the functionality in the various code libraries.

    The motivation to work on v3 was that the contributor would like to improve the nanoFramework because unit testing is so important. The contributor would not be taken seriously if the v3 code was not sufficiently covered by unit tests. As most heavy lifting is done by `TestFramework.Tooling`, that class library is the main target of the tests. Including running the generated code in a virtual device, parsing and asserting the output as part of the execution of the unit tests. The unit tests for the test adapter assert the communication between the test adapter and the host that runs the tooling, and tests the test host. There are no automated tests to test the interaction between Visual Studio/VSTest and the test adapter, but those interactions can be debugged.

- A hierarchy of .runsettings configuration files that are separate from Visual Studio's .runsettings files.

    Visual Studio's .runsettings configuration system is ill suited for the nanoFramework test framework. The main reasons are:

    - Some generic settings in *RunConfiguration* must have a fixed value for the nanoFramework test framework to work. Such as *MaxCpuCount* = 1, otherwise Visual Studio will start multiple test hosts in parallel if the user selects the *Run Tests in Parallel* option and several test hosts may try to access the same real hardware. Although the test framework is protected against that, it will slow down the overall execution of the tests. 

    - Visual Studio can run unit tests for several platforms (.NET framework, .NET, ...) mainly because it can figure out for each unit test project what platform to select for the test host. VS cannot do that for the nanoFramework and the *TargetFrameworkVersion* must be set to *net48*. Similarly the *TestAdaptersPaths* must be set. But this will then also apply to all other (regular .NET) unit tests projects.

    - Visual Studio groups test projects per configuration and asks the test adapter to execute one such group. If two projects have a slightly different configuration, they end up in different groups. Because of the *MaxCpuCount* = 1 requirement, Visual Studio runs each group in a separate test host one after the other and the parallelization features of the nanoFramework test framework cannot be used.

    - Some settings, such as the serial ports to use, are user/computer specific, should not be stored in version control/git and should not be part of the main configuration that is stored in version control/git. The .runsettings mechanism is not designed for that.

    Instead the test framework uses a hierarchy of configuration files, with nano.runsettings for version controlled configurations, nano.runsettings.user for user-specific configurations, and nano.vs.runsettings (controlled by the test framework) for the Visual Studio/VSTest test host configuration. The regular .runsettings files are then available for the non-nanoFramework test adapters.

    A new MSBuild tool `nanoFramework.TestFramework.TestProjectBuildTool` is introduced to ensure nano.vs.runsettings exists and is as required, and assist in the migration of a project from v2 to v3.

## Other code changes / implementation considerations

This section describes a selection of changes in the code that are not merely a refactoring of the v2 code or new code for implementation of the v3 features, and some technical considerations behind the v3 implementation.

- Traits only have a *name* and are not *name=value*, nanoDevice trait starts with @

    The Visual Studio/VSTest object model allows for traits that have the form *name = value*, e.g., nanoDevice = Hardware. If a trait has a non-empty value, is is shown in the Test Explorer as *name=value*, if it has an empty value, just *name* is shown. Other test frameworks have different approaches. MSTest has TestCategory and that is just the *name*, while xUnit allows for *name=value*. The *name=value* option has a problem if *value* is empty, as it is not possible to select or omit the test in a test case filter for VSTest because of an issue in VSTest. A trait that has *name=value* where *name* is fixed by the framework (e.g., *nanoDevice*) clutters the list of traits in the Test Explorer. Not surprisingly, VSTest/Visual Studio seems to work best with the MSTest use of traits (just *name*), so that is also chosen for the nanoFramework test framework. The only special type of trait is the type of device a test should run on. It is prefixed with a "@" to mark its special role. Another advantage is that the traits used in the test framework can be reproduced in all other test frameworks. As an example, the *@Hardware nanoDevice* and *@Virtual nanoDevice* categories are also used in the MSTest unit tests for the test framework to indicate that a nanoDevice is required for the tests.

- AssertException is now in the nanoFramework.TestFramework namespace

    In v2 it was in TestFrameworkShared. The change is backward compatible. The v3 framework throws nanoFramework.TestFramework.AssertException and translates TestFrameworkShared.AssertException to nanoFramework.TestFramework.AssertException. As nanoFramework.TestFramework.AssertException is based on TestFrameworkShared.AssertException, existing code that catches TestFrameworkShared.AssertException will still be triggered. TestFrameworkShared.AssertException is marked as obsolete.

- In Assert methods are added to throw the correct exception

    The supported exceptions (that share a common base class) are AssertException and SkipTestException (from v2), CleanupFailedException and SetupFailedException (new in v3).

- Test classes are instantiated if they are not static

    This is also true for migrated v2 test projects that are run using the v3 test adapter/framework. The backward compatibility of v2 projects that are upgraded to the v3 test adapter/test framework is obtained by a smart choice of the defaults for the new test attributes. It is not possible to detect whether the test project should be run in a "backward compatible way".

- Attributes for the whole assembly must be applied to a special global type rather than to the assembly.

    Initially it was considered to use assembly attributes for annotations that are valid for an entire test project. This is in line with other test projects. Candidates for assembly attributes are test categories (assembly attributes in MSTest, xUnit as well) and the attributes that determine on what device the unit tests should be executed.

    Unfortunately that turned out to be impossible. The discovery of test framework attributes is possible only because the test framework uses only classes from nanoFramework's mscorlib. When the unit test assembly is loaded, the .NET Framework implementation (in which the discovery process is running) maps all types from nanoFramework's mscorlib to .NET Framework 4's mscorlib. This works amazingly well for the required types (attribute base type, string operations, etc.) But there is one attribute type in nanoFramework's mscorlib that is not present in .NET Framework 4's mscorlib and that is always present in nanoFramework assemblies: `System.Reflection.AssemblyNativeVersionAttribute`. As a consequence, the discovery process cannot retrieve the assembly attributes from the test assembly - every attempt to do that results in an exception. It would be quite complex to work around this to get to the assembly attributes. (A work around is applied to get assembly metadata for each naoFramework assembly, but that is a much less complex use case.)

    The easiest workaround is to define a special class that should be defined in the test assembly and apply the attributes to that class. As all attributes are then applied to classes or methods that are specifically designed for test purposes, there is minimal risk that attribute retrieval in the discovery process suffers from the issue of missing types or type conflicts.

- Test framework interfaces are implemented explicitly

    The test adapter uses the interfaces defined in the test framework rather than the names of the attributes. For each interface the test framework has one or more attributes that implement the interface. As an attribute's properties are not supposed to be used directly by custom code, the interfaces as implemented explicitly. It hides the implementation of an attribute: in a future version of the test framework the implementation of the attribute can be changed without breaking any code of nanoFramework users. An exception is made for the `DataRow` attribute, as that already exposed a property in v2 and hiding it would be a breaking change.

- The `DeploymentConfigurationAttribute` has a weird constructor

    Its argument is `params object[]` instead of `params string[]` because the latter results in a invalid typecast exception in `GetCustomAttributes`.

- The unit test launcher uses the type of test classes and the name of methods

    The code for the unit test launcher is generated based on the results of the discovery process. It does not have to re-analyze the classes and methods for test attributes, as the discovery process already provides all required information. Some aspects (device information, but also whether a test class is static = abstract and sealed) cannot be determined by the unit test launcher. It is sufficient to generate code that states: for this class, do/do not instantiate it, run this setup/cleanup method, run these test methods, apply these data row attributes. A first idea was to identify the classes, methods and data row attributes by their index in the enumeration of types in the assembly, methods in the class, attribute in the list of attributes. This is apparently not robust as classes (and methods?) may be removed from the assembly in the conversion to .pe assembly. Instead the `typeof (testclass)` is used and `nameof(method)`, and the index of the data row attribute among the IDataRow-implementing attributes. This seems to be the most robust (compiler/Roslyn will detect type/name conflicts), has most selection-related data in ROM rather than RAM, and eliminates the need to use the name of the test assembly.

- The unit test launcher uses static data for deployment configuration ("make and model")

    If a test class setup requires deployment configuration data, it is added as `const string` / `static readonly byte[]` to the generated code. Not sure if that is the best way, at least the data is stored in ROM/metadata and not in RAM. The alternative (store as resource) cannot be implemented because the tools to create both the resource manager code and the resources is only part of the Visual Studio extension and cannot be used as a stand-alone tool, which is required to generate the unit test launcher at runtime.

- The relation between the deployment configuration ("make and model") and a device must be assigned by hand

    The initial goal was to use metadata that identifies an individual device to retrieve the deployment configuration. However, all metadata that is available via the COM port communication is created by the CLR - this in effect duplicates the use of the target name as it is not identifying the individual device. Manufacturer's hats prevent addition of metadata that can be assigned from a .NET application layer, or the availability of other data that can be read from the device. Although other identifying features are available (e.g., MAC address) there is no way for nanoFramework tooling to relate those to the COM port the device is connected to.

- The test adapter is changed from `nanoFramework.TestAdapter` to `nanoFramework.TestFramework.TestAdapter`, to make sure the v3 version will never overwrite a v2 version and to have a name consistent with the other test framework tools. Most code has moved from the test adapter to `nanoFramework.TestFramework.Tooling`. The URI to identify the test adapter / executor has been changed as well, so Visual Studio / VSTest won't be confused.

- A new .NET Standard 2 class library `nanoFramework.TestFramework.Tooling.Shared` contains some functionality that is required in tools that run in environments that cannot host the `nanoFramework.TestFramework.Tooling` library, e.g., .NET (non-Framework) build tools and the test adapter.

- New NuGet packages are introduced:

    - `nanoFramework.TestFramework.DebugTestProject` with the support for the new debug-unit-tests project
    
    - `nanoFramework.TestFramework.Tooling` for the .NET Framework library of the same name, as this is required to unit test / debug more complex test framework extensions. It will also help people who want to create new/custom tools that work with unit tests.
    
    - `nanoFramework.TestFramework.Tooling.Shared` for the .NET Framework library of the same name, as this is a companion to `nanoFramework.TestFramework.Tooling` for custom tool hosts that can be on a target platform/process environment that does not support by `nanoFramework.TestFramework.Tooling` (and custom tools that want to work with the test framework configuration).

- The `nanoFramework.TestFramework` package is discontinued and replaced by `nanoFramework.TestFramework.Core` and `nanoFramework.TestFramework.TestProject`.

    The current `nanoFramework.TestFramework` contains the test framework library, test adapter and unit test launcher. In v3 the test framework library without adapter/unit test launcher is required for class libraries with test framework extensions. If the v3 NuGet package with only the test framework library would be called `nanoFramework.TestFramework`, people may upgrade to the new version of the package and find that the unit test projects no longer work. Better to use `nanoFramework.TestFramework.Core` as the package that only contains the class library.

    Similar arguments apply to the name of the package that succeeds the `nanoFramework.TestFramework` as the package required to support the unit tests project. Proposed name is `nanoFramework.TestFramework.TestProject`, with `nanoFramework.TestFramework.Core` as dependency. The package contains the test adapter `nanoFramework.TestFramework.TestAdapter` (including test host `nanoFramework.TestFramework.TestHost`) and `nanoFramework.TestFramework.TestProjectBuildTool`.

- The code has workaround(s) to compensate for nf-debugger design choices

    The code checks the serial ports in parallel to see whether a real hardware device is connected (of course, as that may take some time and there is no reason to wait for that). The way to do that (`PortBase.CreateInstanceForSerial(false).AddDevice(serialPort)`) suggests that this is a thread-safe process, but it is not. Instead of returning the discovered device or add it to a collection of the instance of `PortBase`, it is added to a static list of `NanoFrameworkDevices`. There is no way to gain exclusive access to that collection, as the exclusive access is implemented by separate locks in `PortSerialManager` and `PortTcpIpManager`. Unfortunately, the `NanoFrameworkDevices` must be accessed to find out whether a device has been discovered, as the `AddDevice` does not provide information whether is has discovered a device and which device it is. The workaround is that if there's an exception accessing `NanoFrameworkDevices`, it is attempted again and again until no exception occurs.

- In the solution `nanoFramework.TestFramework` extra project dependencies have been defined.

    Some of the test projects and the TestAdapter project require the binaries from other projects as input/external tool. A direct project reference cannot be used as the build system then assumes all projects should have a common framework etc. The dependencies have been entered using the *Project dependencies* for the solution - this ensures the projects are built before the test projects.

- The v2 test adapter knows how to recognize v3 test projects

    If both a v2 and a v3 test project are present in the solution, Visual Studio will try to use the v2 test adapter for the v3 assemblies. A small change in the v2 test adapter prevents that. The v3 test adapter will never detect any tests in a v2 assembly, as the v2 test framework lacks the interfaces used to recognize test classes.

- Test classes must be public
