Remove-Item ../publish/Standalone -Recurse

mkdir ../publish
mkdir ../publish/Standalone
mkdir ../publish/Standalone/MLP_Data
mkdir ../publish/Standalone/MLP_Data/Managed
mkdir ../publish/Standalone/MyLittlePonyZephyrHeights_Data
mkdir ../publish/Standalone/MyLittlePonyZephyrHeights_Data/Managed

copy -Recurse "../Assets" "../publish/Standalone/Elements of Harmony/Assets"

copy "../x64/Release/DirectXHook.dll" "../publish/Standalone/DirectXHook.dll"

copy "../Assembly Load Patch/AMBA/RuntimeInitializeOnLoads.json" "../publish/Standalone/MLP_Data/RuntimeInitializeOnLoads.json"
copy "../Assembly Load Patch/AMBA/ScriptingAssemblies.json" "../publish/Standalone/MLP_Data/ScriptingAssemblies.json"

copy "../Assembly Load Patch/AZHM/RuntimeInitializeOnLoads.json" "../publish/Standalone/MyLittlePonyZephyrHeights_Data/RuntimeInitializeOnLoads.json"
copy "../Assembly Load Patch/AZHM/ScriptingAssemblies.json" "../publish/Standalone/MyLittlePonyZephyrHeights_Data/ScriptingAssemblies.json"

copy "C:/Users/wd357/.nuget/packages/lib.harmony/2.3.3/lib/net472/0Harmony.dll" "../publish/Standalone/MLP_Data/Managed/0Harmony.dll"
copy "C:/Users/wd357/.nuget/packages/lib.harmony/2.3.3/lib/net472/0Harmony.dll" "../publish/Standalone/MyLittlePonyZephyrHeights_Data/Managed/0Harmony.dll"

copy "../x64/Release/netstandard2.1/ElementsOfHarmony.dll" "../publish/Standalone/MLP_Data/Managed/ElementsOfHarmony.dll"
copy "../x64/Release/netstandard2.1/ElementsOfHarmony.AMBA.dll" "../publish/Standalone/MLP_Data/Managed/ElementsOfHarmony.AMBA.dll"
copy "../x64/Release/netstandard2.1/ElementsOfHarmony.KinectControl.dll" "../publish/Standalone/MLP_Data/Managed/ElementsOfHarmony.KinectControl.dll"
copy "../x64/Release/netstandard2.1/ElementsOfHarmony.KinectControl.AMBA.dll" "../publish/Standalone/MLP_Data/Managed/ElementsOfHarmony.KinectControl.AMBA.dll"

copy "../x64/Release/netstandard2.1/ElementsOfHarmony.dll" "../publish/Standalone/MyLittlePonyZephyrHeights_Data/Managed/ElementsOfHarmony.dll"
copy "../x64/Release/netstandard2.1/ElementsOfHarmony.AZHM.dll" "../publish/Standalone/MyLittlePonyZephyrHeights_Data/Managed/ElementsOfHarmony.AZHM.dll"
copy "../x64/Release/netstandard2.1/ElementsOfHarmony.KinectControl.dll" "../publish/Standalone/MyLittlePonyZephyrHeights_Data/Managed/ElementsOfHarmony.KinectControl.dll"
copy "../x64/Release/netstandard2.1/ElementsOfHarmony.KinectControl.AZHM.dll" "../publish/Standalone/MyLittlePonyZephyrHeights_Data/Managed/ElementsOfHarmony.KinectControl.AZHM.dll"