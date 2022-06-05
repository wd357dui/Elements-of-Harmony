# Elements of Harmony
A localization mod for My Little Pony: A Maretime Bay Adventure using Harmony API

## Applying the mod to the game

1. build the DLL

2. put `ElementsOfHarmony.dll` and `0Harmony.dll` and `0Harmony.xml` into `(game folder)\MLP_Data\Managed`

3. add "ElementsOfHarmony.dll" into `"names"` in `\MLP_Data\ScriptingAssemblies.json`
and corresponding value `16` into `"types"`

4. add the following element into `"root"` in `\MLP_Data\RuntimeInitializeOnLoads.json`
```
{
  "assemblyName": "ElementsOfHarmony.dll",
  "nameSpace": "ElementsOfHarmony",
  "className": "ElementsOfHarmony",
  "methodName": "Exist",
  "loadTypes": 2,
  "isUnityClass": false
}
```

## Adding our custom translations
(this section is under construction)
(if you don't want to wait just look for the comments in `ElementsOfHarmony.cs`)

## Disable the block on the Russian language
1. open `(game folder)\MLP_Data\Managed\Assembly-CSharp.dll` with a binary file editor of your liking (I'm using HxD)
2. search for one of the following strings in ***UTF-16 Little Endian*** encoding
`ru` `fr-BE` `fr-CA` `fr-LU` `fr-MC` `fr-CH` `es-AR` `es-BO` `es-ES` `es-CL` `es-CO` `es-CR` `es-DO` `es-EC` `es-SV` `es-GT` `es-HN` `es-US` `es-MX` `es-NI` `es-PA` `es-PY` `es-PE` `es-PR` `es-UY` `es-VE` `es-LA`
3. the strings you found should be together in this order but with `00 0B` or `01 0B` in between them, if not, repeat step 2
4. replace `ru` to any other 2 characters but do not change the length of the string
5. save the file and you're good to go (HxD will automatically make a backup .bak file in the same folder, I trust that other binary file editor will do it too, but you can always verify integrity of game files if anything goes wrong)
