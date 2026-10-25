Set WshShell = CreateObject("WScript.Shell")
appData = WshShell.ExpandEnvironmentStrings("%LOCALAPPDATA%")
dotnetRoot = appData & "\Microsoft\dotnet"
dotnetExe = dotnetRoot & "\dotnet.exe"
scriptDir = CreateObject("Scripting.FileSystemObject").GetParentFolderName(WScript.ScriptFullName)
dllPath = scriptDir & "\src\WinNotch\bin\Release\net8.0-windows10.0.19041.0\WinNotch.dll"

Set oEnv = WshShell.Environment("PROCESS")
oEnv("DOTNET_ROOT") = dotnetRoot

WshShell.Run """" & dotnetExe & """ """ & dllPath & """", 1, False
