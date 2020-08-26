call "C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\Common7\Tools\VsDevCmd.bat"
msbuild -p:Configuration=Release
if errorlevel 1 goto :end
VSTest.Console.exe .obj\Tests\Release\Tests.dll /Settings:Tests\Tests.runsettings
:end
