@echo off
REM Remove the directory if it exists
IF EXIST "..\publish\MelonLoader" (
    rmdir /S /Q "..\publish\MelonLoader"
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
mkdir "..\publish\MelonLoader"
mkdir "..\publish\MelonLoader\Mods"
mkdir "..\publish\MelonLoader\Elements of Harmony"
mkdir "..\publish\MelonLoader\Elements of Harmony\Managed"
mkdir "..\publish\MelonLoader\MyLittlePonyZephyrHeights_Data"

REM Copy directories and files
xcopy /E /I "..\Assets" "..\publish\MelonLoader\Elements of Harmony\Assets"

copy "..\Patch\AZHM\globalgamemanagers.assets" "..\publish\MelonLoader\MyLittlePonyZephyrHeights_Data\globalgamemanagers.assets"

IF "%BuildType%"=="Debug" (
copy "..\x64\%BuildType%\D3D11.dll" "..\publish\MelonLoader\D3D11.dll"
)
copy "..\x64\%BuildType%\DirectXHook.dll" "..\publish\MelonLoader\DirectXHook.dll"
copy "..\x64\%BuildType%\ElementsOfHarmony.Native.dll" "..\publish\MelonLoader\ElementsOfHarmony.Native.dll"

copy "..\x64\%BuildType%\netstandard2.1\ElementsOfHarmony.MelonLoaderReference.dll" "..\publish\MelonLoader\Mods\ElementsOfHarmony.MelonLoaderReference.dll"

copy "..\x64\%BuildType%\netstandard2.1\ElementsOfHarmony.dll" "..\publish\MelonLoader\Elements of Harmony\Managed\ElementsOfHarmony.dll"
copy "..\x64\%BuildType%\netstandard2.1\ElementsOfHarmony.AMBA.dll" "..\publish\MelonLoader\Elements of Harmony\Managed\ElementsOfHarmony.AMBA.dll"
copy "..\x64\%BuildType%\netstandard2.1\ElementsOfHarmony.AZHM.dll" "..\publish\MelonLoader\Elements of Harmony\Managed\ElementsOfHarmony.AZHM.dll"
copy "..\x64\%BuildType%\netstandard2.1\ElementsOfHarmony.KinectControl.dll" "..\publish\MelonLoader\Elements of Harmony\Managed\ElementsOfHarmony.KinectControl.dll"
copy "..\x64\%BuildType%\netstandard2.1\ElementsOfHarmony.KinectControl.AMBA.dll" "..\publish\MelonLoader\Elements of Harmony\Managed\ElementsOfHarmony.KinectControl.AMBA.dll"
copy "..\x64\%BuildType%\netstandard2.1\ElementsOfHarmony.KinectControl.AZHM.dll" "..\publish\MelonLoader\Elements of Harmony\Managed\ElementsOfHarmony.KinectControl.AZHM.dll"

IF NOT DEFINED Pausing (
    pause
)