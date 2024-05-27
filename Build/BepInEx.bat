@echo off
REM Remove the directory if it exists
IF EXIST "..\publish\BepInEx" (
    rmdir /S /Q "..\publish\BepInEx"
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

copy "..\x64\Release\DirectXHook.dll" "..\publish\BepInEx\DirectXHook.dll"

copy "..\x64\Release\netstandard2.1\ElementsOfHarmony.BepInExReference.dll" "..\publish\BepInEx\BepInEx\plugins\ElementsOfHarmony.BepInExReference.dll"

copy "..\x64\Release\netstandard2.1\ElementsOfHarmony.dll" "..\publish\BepInEx\Elements of Harmony\Managed\ElementsOfHarmony.dll"
copy "..\x64\Release\netstandard2.1\ElementsOfHarmony.AMBA.dll" "..\publish\BepInEx\Elements of Harmony\Managed\ElementsOfHarmony.AMBA.dll"
copy "..\x64\Release\netstandard2.1\ElementsOfHarmony.AZHM.dll" "..\publish\BepInEx\Elements of Harmony\Managed\ElementsOfHarmony.AZHM.dll"
copy "..\x64\Release\netstandard2.1\ElementsOfHarmony.KinectControl.dll" "..\publish\BepInEx\Elements of Harmony\Managed\ElementsOfHarmony.KinectControl.dll"
copy "..\x64\Release\netstandard2.1\ElementsOfHarmony.KinectControl.AMBA.dll" "..\publish\BepInEx\Elements of Harmony\Managed\ElementsOfHarmony.KinectControl.AMBA.dll"
copy "..\x64\Release\netstandard2.1\ElementsOfHarmony.KinectControl.AZHM.dll" "..\publish\BepInEx\Elements of Harmony\Managed\ElementsOfHarmony.KinectControl.AZHM.dll"
