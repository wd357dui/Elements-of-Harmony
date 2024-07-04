@echo off
REM Remove the directory if it exists
IF EXIST "..\publish\BepInEx" (
    rmdir /S /Q "..\publish\BepInEx"
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
mkdir "..\publish\BepInEx"
mkdir "..\publish\BepInEx\BepInEx"
mkdir "..\publish\BepInEx\BepInEx\plugins"
mkdir "..\publish\BepInEx\Elements of Harmony"
mkdir "..\publish\BepInEx\Elements of Harmony\Managed"
mkdir "..\publish\BepInEx\MyLittlePonyZephyrHeights_Data"

REM Copy directories and files
xcopy /E /I "..\Assets" "..\publish\BepInEx\Elements of Harmony\Assets"

copy "..\Patch\AZHM\globalgamemanagers.assets" "..\publish\BepInEx\MyLittlePonyZephyrHeights_Data\globalgamemanagers.assets"

IF "%BuildType%"=="Debug" (
copy "..\x64\%BuildType%\D3D11.dll" "..\publish\BepInEx\D3D11.dll"
)
copy "..\x64\%BuildType%\DirectXHook.dll" "..\publish\BepInEx\DirectXHook.dll"
copy "..\x64\%BuildType%\ElementsOfHarmony.Native.dll" "..\publish\BepInEx\ElementsOfHarmony.Native.dll"

copy "..\x64\%BuildType%\netstandard2.1\ElementsOfHarmony.BepInExReference.dll" "..\publish\BepInEx\BepInEx\plugins\ElementsOfHarmony.BepInExReference.dll"

copy "..\x64\%BuildType%\netstandard2.1\ElementsOfHarmony.dll" "..\publish\BepInEx\Elements of Harmony\Managed\ElementsOfHarmony.dll"
copy "..\x64\%BuildType%\netstandard2.1\ElementsOfHarmony.AMBA.dll" "..\publish\BepInEx\Elements of Harmony\Managed\ElementsOfHarmony.AMBA.dll"
copy "..\x64\%BuildType%\netstandard2.1\ElementsOfHarmony.AZHM.dll" "..\publish\BepInEx\Elements of Harmony\Managed\ElementsOfHarmony.AZHM.dll"
copy "..\x64\%BuildType%\netstandard2.1\ElementsOfHarmony.KinectControl.dll" "..\publish\BepInEx\Elements of Harmony\Managed\ElementsOfHarmony.KinectControl.dll"
copy "..\x64\%BuildType%\netstandard2.1\ElementsOfHarmony.KinectControl.AMBA.dll" "..\publish\BepInEx\Elements of Harmony\Managed\ElementsOfHarmony.KinectControl.AMBA.dll"
copy "..\x64\%BuildType%\netstandard2.1\ElementsOfHarmony.KinectControl.AZHM.dll" "..\publish\BepInEx\Elements of Harmony\Managed\ElementsOfHarmony.KinectControl.AZHM.dll"

IF NOT DEFINED Pausing (
    pause
)