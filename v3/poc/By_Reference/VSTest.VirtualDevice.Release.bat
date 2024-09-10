@echo off

where /q vstest.console.exe
if errorlevel 1 goto Not_Found
goto Start

:Not_Found
echo.
echo Cannot find vstest.console.exe
echo Run %~n0 from the Visual Studio Developer Command Prompt
echo See the menu: Tools - Command Line
echo.
pause
goto End

:Start
vstest.console.exe "/settings:%~dp0Configuration\vstest.runsettings" "/logger:trx;LogFileName=%~dp0%~n0.trx" "/TestCaseFilter:TestCategory!=@Hardware nanoDevice" "%~dp0TestProject.v2.Migrated\bin\Release\TestProject.v2.Migrated.dll" "%~dp0TestProject.v3\bin\Release\TestProject.v3.dll"

:End
