@echo off
REM Remove the directory if it exists
IF EXIST "..\publish\Standalone" (
    rmdir /S /Q "..\publish\Standalone"
)
IF NOT DEFINED BuildType (
    set /p BuildType="Please specify build type (Release/Debug): "
)
IF "%BuildType%"=="" (
    echo No build type entered. Defaulting to Release.
    set BuildType=Release
)

REM Create directories
mkdir "..\publish"
mkdir "..\publish\Standalone"
mkdir "..\publish\Standalone\Elements of Harmony"
mkdir "..\publish\Standalone\Elements of Harmony\Managed"
mkdir "..\publish\Standalone\MLP_Data"
mkdir "..\publish\Standalone\MLP_Data\Managed"
mkdir "..\publish\Standalone\MyLittlePonyZephyrHeights_Data"
mkdir "..\publish\Standalone\MyLittlePonyZephyrHeights_Data\Managed"

REM Copy directories and files
xcopy /E /I "..\Assets" "..\publish\Standalone\Elements of Harmony\Assets"

copy "..\Patch\AMBA\RuntimeInitializeOnLoads.json" "..\publish\Standalone\MLP_Data\RuntimeInitializeOnLoads.json"
copy "..\Patch\AMBA\ScriptingAssemblies.json" "..\publish\Standalone\MLP_Data\ScriptingAssemblies.json"

copy "..\Patch\AZHM\globalgamemanagers.assets" "..\publish\Standalone\MyLittlePonyZephyrHeights_Data\globalgamemanagers.assets"
copy "..\Patch\AZHM\RuntimeInitializeOnLoads.json" "..\publish\Standalone\MyLittlePonyZephyrHeights_Data\RuntimeInitializeOnLoads.json"
copy "..\Patch\AZHM\ScriptingAssemblies.json" "..\publish\Standalone\MyLittlePonyZephyrHeights_Data\ScriptingAssemblies.json"

IF "%BuildType%"=="Debug" (
copy "..\x64\%BuildType%\D3D11.dll" "..\publish\Standalone\D3D11.dll"
)
copy "..\x64\%BuildType%\DirectXHook.dll" "..\publish\Standalone\DirectXHook.dll"
copy "..\x64\%BuildType%\ElementsOfHarmony.Native.dll" "..\publish\Standalone\ElementsOfHarmony.Native.dll"

copy "..\x64\%BuildType%\0Harmony.dll" "..\publish\Standalone\MLP_Data\Managed\0Harmony.dll"
copy "..\x64\%BuildType%\netstandard2.1\ElementsOfHarmony.dll" "..\publish\Standalone\MLP_Data\Managed\ElementsOfHarmony.dll"

copy "..\x64\%BuildType%\0Harmony.dll" "..\publish\Standalone\MyLittlePonyZephyrHeights_Data\Managed\0Harmony.dll"
copy "..\x64\%BuildType%\netstandard2.1\ElementsOfHarmony.dll" "..\publish\Standalone\MyLittlePonyZephyrHeights_Data\Managed\ElementsOfHarmony.dll"

copy "..\x64\%BuildType%\netstandard2.1\ElementsOfHarmony.AMBA.dll" "..\publish\Standalone\Elements of Harmony\Managed\ElementsOfHarmony.AMBA.dll"
copy "..\x64\%BuildType%\netstandard2.1\ElementsOfHarmony.AZHM.dll" "..\publish\Standalone\Elements of Harmony\Managed\ElementsOfHarmony.AZHM.dll"
copy "..\x64\%BuildType%\netstandard2.1\ElementsOfHarmony.KinectControl.dll" "..\publish\Standalone\Elements of Harmony\Managed\ElementsOfHarmony.KinectControl.dll"
copy "..\x64\%BuildType%\netstandard2.1\ElementsOfHarmony.KinectControl.AMBA.dll" "..\publish\Standalone\Elements of Harmony\Managed\ElementsOfHarmony.KinectControl.AMBA.dll"
copy "..\x64\%BuildType%\netstandard2.1\ElementsOfHarmony.KinectControl.AZHM.dll" "..\publish\Standalone\Elements of Harmony\Managed\ElementsOfHarmony.KinectControl.AZHM.dll"

IF NOT DEFINED Pausing (
    pause
)