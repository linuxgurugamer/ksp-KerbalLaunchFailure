

set H=R:\KSP_1.2.2_dev
echo %H%

set d=%H%
if exist %d% goto one
mkdir %d%
:one
set d=%H%\Gamedata
if exist %d% goto two
mkdir %d%
:two
set d=%H%\Gamedata\KerbalLaunchFailure
if exist %d% goto three
mkdir %d%
:three
set d=%H%\Gamedata\KerbalLaunchFailure\Plugins
if exist %d% goto four
mkdir %d%
:four
set d=%H%\Gamedata\KerbalLaunchFailure\PluginData
mkdir %d%
:five

copy bin\Debug\KerbalLaunchFailure.dll ..\GameData\KerbalLaunchFailure\Plugins


xcopy /y /s "..\GameData\KerbalLaunchFailure" "%H%\GameData\KerbalLaunchFailure"
