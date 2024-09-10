@echo off
rmdir /s /q TestProject.v2.AlmostMigratedTo.v3
mkdir TestProject.v2.AlmostMigratedTo.v3
xcopy Support\TestProject.v2.AlmostMigratedTo.v3 TestProject.v2.AlmostMigratedTo.v3 /s