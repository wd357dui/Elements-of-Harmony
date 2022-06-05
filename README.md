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
(this section is under construction)
