@echo off
REM Remove the directory if it exists
IF EXIST "..\publish\MelonLoader" (
    rmdir /S /Q "..\publish\MelonLoader"
)

REM Create directories
mkdir "..\publish"
mkdir "..\publish\MelonLoader"
mkdir "..\publish\MelonLoader\Mods"
mkdir "..\publish\MelonLoader\Elements of Harmony"
mkdir "..\publish\MelonLoader\Elements of Harmony\Managed"
mkdir "..\publish\MelonLoader\MyLittlePonyZephyrHeights_Data"

REM Copy directories and files
xcopy /E /I "..\Assets" "..\publish\MelonLoader\Elements of Harmony\Assets"

copy "..\Patch\AZHM\globalgamemanagers.assets" "..\publish\MelonLoader\MyLittlePonyZephyrHeights_Data\globalgamemanagers.assets"

copy "..\x64\Release\DirectXHook.dll" "..\publish\MelonLoader\DirectXHook.dll"

copy "..\x64\Release\netstandard2.1\ElementsOfHarmony.MelonLoaderReference.dll" "..\publish\MelonLoader\Mods\ElementsOfHarmony.MelonLoaderReference.dll"

copy "..\x64\Release\netstandard2.1\ElementsOfHarmony.dll" "..\publish\MelonLoader\Elements of Harmony\Managed\ElementsOfHarmony.dll"
copy "..\x64\Release\netstandard2.1\ElementsOfHarmony.AMBA.dll" "..\publish\MelonLoader\Elements of Harmony\Managed\ElementsOfHarmony.AMBA.dll"
copy "..\x64\Release\netstandard2.1\ElementsOfHarmony.AZHM.dll" "..\publish\MelonLoader\Elements of Harmony\Managed\ElementsOfHarmony.AZHM.dll"
copy "..\x64\Release\netstandard2.1\ElementsOfHarmony.KinectControl.dll" "..\publish\MelonLoader\Elements of Harmony\Managed\ElementsOfHarmony.KinectControl.dll"
copy "..\x64\Release\netstandard2.1\ElementsOfHarmony.KinectControl.AMBA.dll" "..\publish\MelonLoader\Elements of Harmony\Managed\ElementsOfHarmony.KinectControl.AMBA.dll"
copy "..\x64\Release\netstandard2.1\ElementsOfHarmony.KinectControl.AZHM.dll" "..\publish\MelonLoader\Elements of Harmony\Managed\ElementsOfHarmony.KinectControl.AZHM.dll"
