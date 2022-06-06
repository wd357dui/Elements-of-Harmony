# Elements of Harmony
A localization mod for My Little Pony: A Maretime Bay Adventure using Harmony API

## Applying the mod to the game

1. build the DLL (remember to restore NuGet package first)
2. put `ElementsOfHarmony.dll` and `0Harmony.dll` and `0Harmony.xml` into `(game folder)\MLP_Data\Managed`
3. add `"ElementsOfHarmony.dll"` into `"names"` in `\MLP_Data\ScriptingAssemblies.json`
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
*to add a language that the game didn't originally support, follow the following steps*
1. create a folder named `Elements of Harmony` in the game folder (where MLP.exe is)
2. to add text translations, in the `Elements of Harmony` folder, create a sub folder named `Translations` and put translation files inside
3. the translation file name should be `(language ISO code).txt` (case sensitive), its content should be tab-separated values (TSV), where first column is the **term** (case sensitive) and second column is the translated text (use \n for line break during text); other columns will be ignored
4. please also add your language code as **term** and your language name as translated text so that your language name can show up in the game menu correctly
5. to add localized audio files, in the `Elements of Harmony` folder, create a sub folder named `AudioClip` and put the audio files inside (you can create sub folders in `AudioClip` as well, the mod will recursively search for all audio files in all sub folders)
6. the name of the audio files should match the **terms** (case sensitive); look for the field `OurSupportedAudioFormats` in [ElementsOfHarmony.cs](ElementsOfHarmony/ElementsOfHarmony.cs) for a list of supported audio formats

## Disable the block on the Russian language (obsolete)
*the mod itself is in charge of doing this now, you don't need to change `Assembly-CSharp.dll` anymore, but I'm leaving the content here for archieve purposes*
1. open `(game folder)\MLP_Data\Managed\Assembly-CSharp.dll` with a binary file editor of your liking (I'm using HxD)
2. search for one of the following strings in ***UTF-16 Little Endian*** encoding
`ru` `fr-BE` `fr-CA` `fr-LU` `fr-MC` `fr-CH` `es-AR` `es-BO` `es-ES` `es-CL` `es-CO` `es-CR` `es-DO` `es-EC` `es-SV` `es-GT` `es-HN` `es-US` `es-MX` `es-NI` `es-PA` `es-PY` `es-PE` `es-PR` `es-UY` `es-VE` `es-LA`
3. the strings you found should be together in this order but with `00 0B` or `01 0B` in between them, if not, repeat step 2
4. replace `ru` to any other 2 characters but do not change the length of the string
5. save the file and you're good to go (HxD will automatically make a backup .bak file in the same folder, I trust that other binary file editor will do it too, but you can always verify integrity of game files if anything goes wrong)

## Bugs? what do?
<sub>during the course of the making of this mod, a lot of things can go wrong (and they did), but I knew it's to be expected for unorthodox projects like this, so I came prepared.</sub>

The mod attachs/hooks to every kind of error events/exceptions I can think of, `AppDomain.CurrentDomain.UnhandledException` event, `UnityEngine.Debug.LogError` method, `System.Exception` constructor, and so on; and then it logs those errors (stack trace + error message) into `Elements of Harmony/Elements of Harmony.log`

so if you think you've encountered a bug, look into `Elements of Harmony/Elements of Harmony.log`, there is about 70% chance that the clues we needed are recorded in there
