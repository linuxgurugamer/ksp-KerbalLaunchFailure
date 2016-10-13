@echo off
set DEFHOMEDRIVE=d:
set DEFHOMEDIR=%DEFHOMEDRIVE%%HOMEPATH%
set HOMEDIR=
set HOMEDRIVE=%CD:~0,2%

set RELEASEDIR=d:\Users\jbb\release
set ZIP="c:\Program Files\7-zip\7z.exe"
echo Default homedir: %DEFHOMEDIR%

rem set /p HOMEDIR= "Enter Home directory, or <CR> for default: "

if "%HOMEDIR%" == "" (
set HOMEDIR=%DEFHOMEDIR%
)
echo %HOMEDIR%

SET _test=%HOMEDIR:~1,1%
if "%_test%" == ":" (
set HOMEDRIVE=%HOMEDIR:~0,2%
)


type KerbalLaunchFailure.version
set /p VERSION= "Enter version: "

rd /s %HOMEDIR%\install\GameData\KerbalLaunchFailure
mkdir %HOMEDIR%\install\GameData\KerbalLaunchFailure
mkdir %HOMEDIR%\install\GameData\KerbalLaunchFailure\Plugins
mkdir %HOMEDIR%\install\GameData\KerbalLaunchFailure\PluginData

copy bin\Release\KerbalLaunchFailure.dll ..\GameData\KerbalLaunchFailure\Plugins

xcopy /y /s "..\GameData\KerbalLaunchFailure" %HOMEDIR%\install\GameData\KerbalLaunchFailure


%HOMEDRIVE%
cd %HOMEDIR%\install

set FILE="%RELEASEDIR%\KerbalLaunchFailure-%VERSION%.zip"
IF EXIST %FILE% del /F %FILE%
%ZIP% a -tzip %FILE% Gamedata\KerbalLaunchFailure