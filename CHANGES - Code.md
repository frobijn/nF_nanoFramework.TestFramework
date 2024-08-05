# Code changes
An overview of changes made to the code.

## nanoFramework.NFUnitTestAnalyzer
All code that examined the test assembly and test project, and all code to select and run tests is moved to a new class library. This may be a good candidate for a new nuget package, as the community can use it to build custom tools. The library is targeted for .NET 4.8 in line with the TestAdapter. The library is used by the TestAdapter and by the new NFDebugUnitTest tool.

The reasons to do so: the same business logic is used in multiple tools, it is not straightforward to do correctly, and it is easier to test a class library than a TestAdapter.

There are unit tests for this library in the `tests` directory.

## TestAdapter

Changes to implement the new features:

- The code for analysis of the unit tests is moved to a new NFUnitTestAnalyzer, because it is easier to test. The code in the TestAdapter is now more focused on the interaction with Visual Studio / vstest.

- The TestAdapter now generates the UnitTestLauncher application that is executed on a device. This seemed to be the easiest way to pass the user's selection of tests (in the Test Explorer) to the devices.

- Code generation is done via Roslyn and added to NFUnitTestAnalyzer.

- The programming logic before the tests are deployed has been changed. The current logic is that one set of test assemblies is deployed to either the first available real hardware, or to a virtual device (nanoCLR process). The new logic first finds the devices (if that is necessary), then determines what test should be run on which device (both are part of NFUnitTestAnalyzer), and then deploys the resulting tests to one or more devices (still coded in TestAdapter). The interaction between TestAdapter and devices is not changed.

- The interpretation of the output of a test is updated to include the new exception types.

- One of the proposals is to be able to use files stored in the firmware of a device to decide whether a test will be executed on the device. This is partly implemented; the part that is still missing is a call from the TestAdapter to the device to actually get the file, as that is not yet supported by the nanoCLR / nf-debugger. This call would be made part of NFUnitTestAnalyzer.

Changes made to solve other issues:

- Code analysis of the test source code is done via Roslyn (in NFUnitTestAnalyzer). The current source code parser made some assumptions about the coding style. Although that could be corrected by switching to regular expressions, Roslyn is more robust.

## nanoframework.TestFramework / TestFrameworkShared

Changes to implement the new features:

- New attributes are added to the framework: `Traits`, `TestOnXXX`, `TestForXXX`, `RunParallel`, updated `TestClass`.

- Users can extend the framework by defining attributes that implement one or all of the new interfaces: `ITestClass`, `ITestMethod`, `ITraits`, `ITestForDevice`, `ITestOnDevice`, `IRunParallel`. The interface-style extensibility is copied from xUnit. Attributes that are not extensible are now sealed.

- The new attributes receive information on a device that is available for test purposes via `ITestDevice`. A user should not create code that implements the interface on the nanoCLR platform, it is used as a bridge to tooling running on the .NET platform. The same is true for the content of the `Tools` directory.

- TestFrameworkShared (shared code) has been removed, but code is shared with the new NFUnitTestAnalyzer.

	The source code of the TestFramework was part of a shared code project. The reason for this is that code in
	tools like the TestAdapter (.NET-based assemblies) cannot access the TestFramework (nanoCLR-based assembly)
	types directly as to the .NET type system the attributes with the same name are different types as they are 
	defined in different assemblies. This makes using the nanoCLR types in .NET tools quite tricky, as it requires matching-by-name
	and reflection. The logic to do that is now part of NFUnitTestAnalyzer and shielded from "regular" users. There still is code
	sharing between TestFramework and NFUnitTestAnalyzer (and NFUnitTestAnalyzerTests).

	There was a reference from a solution in the CoreLibrary repository to the TestFrameworkShared; this has been removed; the reference to the TestFramework class library is sufficient. No references to the shared code were found in the other repositories.

- Most of the UnitTestLauncher is now part of nanoFramework.TestFramework: `TestRunner` in `Tools`. It contains all logic to select and execute the unit tests. The TestAdapter/nanoFramework.NFUnitTestAnalyzer contains code to generate a UnitTestLauncher application that calls the `TestRunner`. 

- TestClassAttribute has extra options via the `ITestClass` interface, TestMethodAttribute can be extended/replaced by implementing `ITestMethod`

	The interface is introduced for the new features. A virtual method GetTestMethodAttribute existed. Not clear how this method would work, it was not used by any code in the framework. The same functionality can now be achieved via the `ITestMethod` extensibility. The method is removed.

- Changed a few things for exceptions:
	- AssertFailedException was in the TestFrameworkShared namespace instead of the nanoFramework.TestFramework. Introduced the nanoFramework.TestFramework.AssertFailedException, made TestFrameworkShared.AssertFailedException obsolete. It is done in a backward compatible fashion:
		- The test framework throws nanoFramework.TestFramework.AssertFailedException
		- TestFrameworkShared.AssertFailedException is a base class of nanoFramework.TestFramework.AssertFailedException. if any user's code still catches TestFrameworkShared.AssertFailedException, it will catch the exceptions thrown by the framework.
		- If a user's code throws TestFrameworkShared.AssertFailedException, the unit test launcher (that is now part of the TestFramework) catches that exception and turns it into a nanoFramework.TestFramework.AssertFailedException exception.
	
	- Introduced InconclusiveException, CleanupFailedException
	
	- All framework exceptions derive from TestFrameworkException, and the replacement of null chars is now in TestFrameworkException. You may need this in special situations where the test code monitors for exceptions. Like in the framework:
	
	- Assert.ThrowsException modified. If an exception occurs that is a framework exception and the expected exception is not a framework exception, the framework exception is re-thrown. This is possible in code that checks if an action leads to an non-framework exception, and within the action the user wants to verify some values via Assert, and one of those values are incorrect.	But if the user is testing for a framework exception (e.g., when testing a framework extension), occurence of a different framework extension is a test failure.
	
## Unit test runner

- Instantiation of classes
