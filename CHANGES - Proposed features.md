# Possible TestFramework improvements 

## Proposed features in short

- Custom traits / test categories, to make it easier to filter tests in the Test Explorer.
- Make a selection of tests in the Test Explorer and run only those tests.
- Fine-grained control over the device a test runs on:
	- Run tests only on specific devices (by platform/target/make-and-model-description).
	- Do not run tests on devices that cannot execute the test (by platform/target/make-and-model-description).
- Run a test on all available devices (as far as that makes sense for the test).
- Run tests in parallel (if that is possible)
- Disable individual tests
- Improved reporting of the outcome of tests
- Template for a companion project for each unit test project (and maybe multiple) to debug one of more tests on real hardware.

## Hands-on demo
All features except for the make-and-model-description dependency are available in forks of the nanoFramework repositories:

- Clone and switch to the branch TestFrameworkImprovements of the fork at https://github.com/frobijn/nF_nanoFramework.TestFramework/tree/TestFrameworkImprovements
- Build the `nanoFramework.TestFramework` solution
- Build the `nanoFramework.TestAdapter` solution
- Open the `NFUnit Test DemoByReference`. The solution contains a test project that shows off the new features and the updated TestAdapter, and a project to debug the unit tests.

If you want to test with real-world test projects:

- Clone and switch to the branch TestFrameworkImprovements_Demo_BackwardCompatible of the fork at https://github.com/frobijn/nF_CoreLibrary/tree/TestFrameworkImprovements_Demo_BackwardCompatible
- Clone and switch to the branch TestFrameworkImprovements of the fork at https://github.com/frobijn/nF_nanoFramework.TestFramework/tree/TestFrameworkImprovements in the `nanoFramework.TestFramework` subdirectory
- Open the `nanoFramework.CoreLibrary` solution. There are many tests in the solution.
- Because the proposed changes are backward compatible, the list of tests in the Test Explorer is unchanged. The tests are run via the updated TestAdapter.
- Play around to run the tests. Everything is running as with the current version of the framework, but it is now possible to run a selection of tests (without running all tests in the assembly).
- At the top of the solution is a demo project that demonstrates debugging of unit tests.

- Close the `nanoFramework.CoreLibrary` solution. Change the branch in the CoreLibrary clone to TestFrameworkImprovements_Demo_New. Open the `nanoFramework.CoreLibrary` solution.
- For demonstration purposes, all test projects have been modified: [TestForRealHardware] / [TestForVirtualDevice] have been added at assembly level, and the name of the resulting assemblies is equal to the project name (instead of NFUnitTest). The tests will always be executed on the virtual device and on all connected real hardware, unless those tests are not selected in the Test Explorer. 
- Play around to run selective tests, all tests, tests running in parallel on the Virtual Device, tests running simultaneously on multiple connected hardware devices and the Virtual Device, etc.
- At the top of the solution is a demo project for debugging of unit tests. There is one project to debug all tests, to show that it is possible to do.

## Selective execution of a subset of tests from the Test Explorer

It works, just try it. A user does not have to change the test project to get this, other than to update the TestFramework
package (after this change is accepted and has made it into the nanoFramework.TestFramework package).

## Improved reporting of the outcome of tests

Assert.Inconclusive/InclusiveException is added to enable to express more precisely why a test fails. If the user selects a test to be run, it is expected that the outcome is either success or failure. But it is possible that there are other possible outcomes:

- The device is not capable of running the test, e.g., because it lacks support for some features. Use Assert.SkipTest for this.
- The setup or initialisation of the test context fails before the test proper has been started. Use Assert.Inconclusive for this.
- The test succeeds but the cleanup/teardown fails. Use Assert.CleanupFailed for this

Each of the possible outcomes are displayed in a different way in the Test Explorer, to make it easy to see why a test did not succeed. For existing test projects, Assert.Inconclusive and Assert.CleanupFailed are now used for failure of the Setup / Cleanup methods.

Assert.Fail is added for complex asserts and test framework extensibility.

## Proposed attributes and extensibility

### Example

```
	[TestClass]
	public class TestSensorAndPostProcessing
	{
		[TestMethod]
		[Trait ("Sensors")]
		[TestOnRealHardware]
		public void TestSensor ()
		{
			...
		}

		[TestMethod]
		[Trait ("Sensors")]
		[TestForTarget ("ESP_S3", onEveryDevice: false)]
		[TestForTarget ("ESP_C6", onEveryDevice: false)]
		public void TestSensorOptionThatOnlyExistsOnEsp32 ()
		{
			...
		}
		
		[DataRow (0, "0..9")]
		[DataRow (42, "40..49")]
		[Trait ("PostProcessing")]
		[TestOnRealHardware]
		[TestOnVirtualDevice (runParallel: true)]
		public void TestPostProcessing (int actual, string expected)
		{
			...
		}

		[TestMethod]
		[Trait ("PostProcessing")]
		[TestForVirtualDevice (runParallel: true)]
		public void TestPostProcessedDataCompliance ()
		{
			...
		}
	}
```

Let's say that there are three devices connected to the computer, one with ESP32_C6 firmware and two with ESP32_S3. On all devices the sensor being tested is connected in a way compatible with the TestSensorAndPostProcessing.

All tests are selected in the Test Explorer and the nano.runconfig has `<RealHardware>true</RealHardware>`. The user starts the unit tests. TestSensor and TestPostProcessing is run on the first available device (one of the ESP32), TestSensorOptionThatOnlyExistsOnEsp32 on the ESP32_C6 and also on one of the ESP32_S3. 

Suppose all tests succeed except the one on ESP32_C6. Oops, the sensor was not connected correctly, but now it is. The user selects the TestSensorOptionThatOnlyExistsOnEsp32 for ESP32_C6 in the Test Explorer and reruns only the TestSensorOptionThatOnlyExistsOnEsp32 test on the ESP32_C6.

Later the devices are disconnected. If the user starts all unit tests, only TestPostProcessing is executed on the virtual device. The two DataRow cases are run in parallel on the virtual device.

Partially implemented in this repository (= not working) but would be nice to have: suppose a sensor is connected to ESP32_C6 and one of the ESP32_S3 but not the other. A make-and-model-description of the hardware and connected sensors is stored in the firmware of the devices. The first test method is now:
```
		[TestMethod]
		[Trait ("Sensors")]
		[TestOnMakeAndModelThatHas ("TheSensorType")]
		public void TestSensor ()
		{
			...
		}
```
The test method would only be executed on one of the devices with a connected sensor, not on the third one. Both the format of the make-and-model-description and the TestOnMakeAndModelThatHas attribute would have to be implemented by the user, the nanoFramework takes care of the integration. 

### Attributes and their effect

- What is a test class?

	- A test class is a class with a TestClass attribute (same as current framework)
	
	- A test class can be a static class. If that is the case, the test class will not be instantiated in a test and all test methods are static methods.
	
	- A test class can be a regular non-static class. The test class will be instantiated in a test and all test methods should be non-static methods.
	
	- For non-static classes: in the `TestClass` attribute a user can specify whether the instantiation and the Setup/Cleanup methods (if defined) should be done once per test method, or once for all test methods. The default is once for all test methods.
	
- What is a test method?

	- A test method is a method of a TestClass and is recognised by a TestMethod attribute. The attribute is optional if any of the other test attributes (except Setup/Cleanup) are present (same as current framework).

	- Setup and Cleanup are not considered test methods as they cannot be selected to be executed as stand-alone test.
	
	- If a test fails, the output of the test indicates whether that happened in the Setup, TestMethod or Cleanup phase.
	
	- The output will include a link to the source of the Setup / CleanUp method.

- Custom traits / test categories

	- There is a new attribute, `Trait`, that can be applied to a test method to assign a trait/category to the test. That shows up as trait in the Test Explorer in Visual Studio. Multiple `Trait` attributes can be applied to the same test method.

	- A user can extend the test framework by implementing a new attribute. If that attribute wants to add a trait, it should implement the `ITrait` interface.

- Do not run tests on devices that cannot execute the test (or don't need to execute the test).

	- There are `TestOnXXX` attributes that inform the test framework on what devices the test are supposed to run. `XXX` = `VirtualDevice` (= nanoCLR), `RealHardware` (any real device), `Platform`, `Target`. These attributes also add a `On: ...` trait to the test method.
	
	- The attributes can be applied to a test method, test class (= all methods in the class) and/or assembly (= all test methods in the project). The attributes are additive: the set attributes for a test methods = attributes for the method + for its test class + for the assembly.
	
	- A user can extend the test framework by implementing a new `TestOnXXX`-type attribute that implements the `ITestOnDevice` interface.

	- The test never runs on devices that do not match any of the `TestOnXXX`/`ITestOnDevice` attributes. The test will be run on a device that matches one of the attributes. It is up to the test framework (and maybe other attributes) to pick a device.
	
	- If there are no `TestOnXXX`/`ITestOnDevice` attributes for a test method, it is assumed that the test can run on any device. This is equivalent to the combination of `TestOnVirtualDevice` and `TestOnRealHardware`.

- Tests that are designed to be run on a specific device.

	- There are `TestForXXX` attributes that inform the test framework what devices the test is designed for. `XXX` = `VirtualDevice` (= nanoCLR), `RealHardware` (any real device), `Platform`, `Target`. These attributes also add a `For: ...` to the test method in the Test Explorer. The `TestForXXX` is for devices what `DataRow` is for test data. 
	
	- The attributes can be applied to a test method, test class (= all methods in the class) and/or assembly (= all test methods in the project). The attributes are additive: the set attributes for a test methods = attributes for the method + for its test class + for the assembly.
	
	- A user can extend the test framework by implementing a new `TestForXXX`-type attribute that implements the `ITestForDevice` interface.

	- If a test has one or more `TestForXXX`/`ITestForDevice` attributes will be started on at least one device that matches an attribute, provided that device is available.
	
	- The user can specify whether an attribute means that a test should be started on each matching device that is available. If that is not the case, the test is started on at most one device that matches that attribute.
	
	- A test that has one `TestForXXX`/`ITestForDevice` attributes 

	- If there are no `TestForXXX`/`ITestForDevice` attributes for a test method, it is assumed that the test is not designed for a specific device and that it only has to be run on a single device.

- Parallel execution

	- A test can be run simultaneously on different devices, depending on the `TestOnXXX`/`TestForXXX`-style attributes
	
	- The user can request parallel execution of multiple tests via a button in the Test Explorer. The Virtual Device is the only device that (in principle) can honor this request.

	- The framework tries to be smart about running in parallel:
	
		- Test methods from different test classes can run in parallel
		- Test methods from static test classes that run Setup/Cleanup for each test method can run in parallel
		- Test methods from non-static test classes that are instantiated per test method can run in parallel
		- For other test classes, test methods from the same class cannot be run in parallel

	- A user can change the default behavior via the `RunParallel` attribute for classes, to indicate that the test methods from the same class can (or cannot) be run in parallel
	
	- A user can change the default behavior via the `RunInIsolation` attribute for a method, to indicate that the test method cannot be run in parallel with any other test.

	- A user can extend the test framework by implementing a new `RunParallel`/`RunInIsolation`-type attribute that implements the `IRunParallel` interface.

- Disable individual tests

	- A user can indicate that a test is temporary out of order via the `SkipTest` attribute
	
	- The test is visible in the Test Explorer but it will never be executed and its other attributes will not be evaluated

- nano.runconfig

	- The nano.runconfig should by default have `<RealHardware>true</RealHardware>` to enable all hardware-related attributes.
	- The setting `<RealHardware>false</RealHardware>` can be used to disable all hardware-related tests, e.g., if it is known that they will fail or to prevent test being run on connected hardware. 
	- If a test project only contains tests that are designed to run on the virtual machine, add the assembly-wide `TestForVirtualDevice` to the project. This will force the execution of the tests on the virtual machine, regardless of the .runconfig setting or whether or not devices are connected.
	
	- Parallel execution can be prohibited via the nano.runsettings; by default it should enabled. If the user does not want to run tests in parallel, the corresponding option in the test explorer should not be selected. The nano.runsettings should be used, e.g.:
	
		- If running tests in parallel does not work as expected, but there is no time to solve the issue.
		
		- To start using new attributes in an existing test project (which turns off backward compatibility) without the need to figure out whether parallel running of tests leads to new issues.

### Backward compatibility mode

The current framework functions in a way that is not always compatible with the proposed rules. Most of the above rules are designed in such a way that if the test project does not use any of the new attributes, the tests are presented/run in the same way as the current framework does. There are a few exceptions:

- The current framework creates default traits based on the type of attributes
- The Setup and Cleanup methods are displayed in the Test Explorer
- The current framework does not instantiate non-static test classes
- Tests are never run in parallel

The code in this repository has implemented a backward compatibility mode: if a test project does not use any of the new attributes anywhere in the project, the "classical" behaviour is maintained.

## Debugging

The Test Explorer lets a user choose to *run* or *debug* a set of tests. The *debug* for nanoFramework test projects is the same as *run*, and that is the same in the version in this repository. But the nanoFramework does have excellent debug support for applications.

Solution: a project template that creates an application to run the unit tests. This repository has one such project, in the `NFUnit Test DemoByReference` solution. The project consists of two code files:

- One code file with a `main` method that calls the `TestRunner` (new feature) from the test framework.
- One code file that is not added to git and that configures the `TestRunner` what unit test(s) to debug.

(PS is it possible to allow any assembly name for the unit test assembly instead of NFUnitTest? The debug application could then be used for multiple test projects.)