Remove-Item ../publish/MelonLoader -Recurse

mkdir ../publish
mkdir ../publish/MelonLoader
mkdir ../publish/MelonLoader/Mods
mkdir ../publish/MelonLoader/UserLibs

copy -Recurse "../Assets" "../publish/MelonLoader/Elements of Harmony/Assets"

copy "../x64/Release/DirectXHook.dll" "../publish/MelonLoader/DirectXHook.dll"

copy "../x64/Release/netstandard2.1/ElementsOfHarmony.MelonLoaderReference.dll" "../publish/MelonLoader/Mods/ElementsOfHarmony.MelonLoaderReference.dll"

copy "../x64/Release/netstandard2.1/ElementsOfHarmony.dll" "../publish/MelonLoader/UserLibs/ElementsOfHarmony.dll"
copy "../x64/Release/netstandard2.1/ElementsOfHarmony.AMBA.dll" "../publish/MelonLoader/UserLibs/ElementsOfHarmony.AMBA.dll"
copy "../x64/Release/netstandard2.1/ElementsOfHarmony.AZHM.dll" "../publish/MelonLoader/UserLibs/ElementsOfHarmony.AZHM.dll"
copy "../x64/Release/netstandard2.1/ElementsOfHarmony.KinectControl.dll" "../publish/MelonLoader/UserLibs/ElementsOfHarmony.KinectControl.dll"
copy "../x64/Release/netstandard2.1/ElementsOfHarmony.KinectControl.AMBA.dll" "../publish/MelonLoader/UserLibs/ElementsOfHarmony.KinectControl.AMBA.dll"
copy "../x64/Release/netstandard2.1/ElementsOfHarmony.KinectControl.AZHM.dll" "../publish/MelonLoader/UserLibs/ElementsOfHarmony.KinectControl.AZHM.dll"