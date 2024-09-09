# Demo by reference

## Content of the directory
This directory contains projects that reference directly the TestFramework v3 source files rather than using NuGet packages. There are several projects:

- `TestProject.v2` is a TestFramework v2 project. It is included to demonstrate that a the v2 and v3 versions of the TestFramework can co-exist in the same solution.
- `TestProject.v2.Migrated` is the same TestFramework v2 project after migration to v3 and a rebuild (as described in the documentation), but before any other changes other than the removal of the migrated `nano.runsettings`.
- `TestProject.v3` is a TestFramework v3 project that showcases all v3 features. The project does not use any hardware-specific features and can be run on any device.
- `TestProject.Hardware_esp32.v3` is a TestFramework v3 project that simulates testing code that uses an ESP32-hardware-specific class library. It contains tests that should only be run on an ESP32 nanoDevice.
- `DebugTestProject.v3` and `DebugTestProject.Hardware_esp32.v3` are the projects to debug a test from `TestProject.v3` / `TestProject.Hardware_esp32.v3`.

- `VSTest.VirtualDevice.bat` uses VSTest to run the tests in `TestProject.v2.Migrated` and `TestProject.v3` on a Virtual nanoDevice
- `VSTest.esp32.bat` uses VSTest to run the tests in `TestProject.v2.Migrated`, `TestProject.v3` and `DebugTestProject.Hardware_esp32.v3` on one or more ESP32 nanoDevice.
- `TestProject.v2.AlmostMigratedTo.v3.sln` and `Copy TestProject.v2.AlmostMigratedTo.v3.bat` are required to experience the automated migration from v2 to v3.
- `Support` is a directory to make all this work; keep out!

## Suggested actions

If you want to know more about the TestFramework v3, read the [general documentation](https://docs.nanoframework.net/content/unit-test/framework-v3/index.html) and/or the `CHANGELOG v2 to v3.md` in this repository.

Before you begin, open the `v3\nanoFramework.TestFramework.sln` solution and build the TestFramework v3.

Open the `Demo By Reference` solution and build it. In the Test Explorer window the tests from all projects are shown. Experiment with running tests from Visual Studio:

- Try the *Group by* button to order the tests in different ways.
- Add a filter *-Trait:@Hardware nanoDevice* and see that only tests remain that can be run on the Virtual nanoDevice
- Try running one, multiple or all tests that have *@Virtual nanoDevice* as trait. Take a look at the detailed test results.
- Change the `SelectUnitTests.json` file from `DebugTestProject.v3` and experience the intellisense support. Select a single test, set breakpoints in `TestProject.v3`, start the Virtual nanoDevice via the Device Explorer, debug the test on the virtual device.
- Use the *Options | Columns* to see what information is displayed for the tests
- Replace the filter by *-Trait:Type* and see that only TestFramework v3 tests remain.
- Connect a real hardware device and run all tests. Take a look at the detailed test results.
- Connect two real hardware devices (ideally with different target/firmware, at least one ESP32) and run all tests. Take a look at the detailed test results.

Once the `Demo By Reference` solution has been built, you can experiment with VSTest:

- Run `VSTest.VirtualDevice.bat` to run all v3 tests that should be run on a Virtual nanoDevice.
- Connect one or two (ESP32 or other) real hardware devices and run `VSTest.esp32.bat` to run all v3 tests that should be run on a real hardware nanoDevice.
- Delete `vstest.runsettings` and verify that it reappears after `TestProject.v3` is rebuild. Look at `TestProject.v3` to see how it is created.

Other actions:

- Read the documentation on the [test framework configuration](https://docs.nanoframework.net/content/unit-test/framework-v3/controlling-the-test-execution.html) and experiment with `nano.runsettings` and `nano.runsettings.user`.

- Run `Copy TestProject.v2.AlmostMigratedTo.v3` and open the `TestProject.v2.AlmostMigratedTo.v3` solution. The `TestProject.v2.AlmostMigratedTo.v3` project simulates a TestFramework v2 project where the `nanoFramework.TestFramework` package has been replaced by the `nanoFramework.TestFramework.TestProject`/`nanoFramework.TestFramework.Core` packages. Build it to experience the first build of the project that performs some automated migrations. Notice that the assembly name is no longer `NFUnitTest.dll`, and that the `nano.runsettings` is migrated to a `nano.runsettings`/`nano.runsettings.user` pair. Run `Copy TestProject.v2.AlmostMigratedTo.v3` again and change the `nano.runsettings` before the build to see how the migration works.

- Take a look at the framework extensions in `TestProject.v3`, perhaps create your own.

