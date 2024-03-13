# Elements of Harmony
A localization mod for My Little Pony: A Maretime Bay Adventure using Harmony API

# New Goal 2024
A new MLP game from the same developer (or publisher?) is confirmed, it's called **A Zephyr Heights Mystery**.

That gives me the drive to begin to remaster this mod,
which counts as practice & preparation to mod the new game when it comes out

- **The goals of this remaster**

- [ ] Refactor the codebase to meet my newest standards
- [ ] Refactor and merge the `Loyalty` branch
- [ ] Make a patch tool using PowerShell script or something (I just don't want to make a separate program for this)

------

- **Optional goals** (*most of these are stuff I wanted to do at the time, some are just for the meme, and the others are there because I wanted to "unlock the true potential" of this mod, but I didn't, fearing that no one would be interested and I would have just wasted lots of time and effort for nothing...* :cry:)

- [x] Magic - Hook the DirectX API to acquire the game's swap chain, so that we can render our graphics on top of the original game using Direct2D - *already implemented in 2022, will improve in the future*
- [x] Loyalty - Kinect motion control - *already implemented in 2022, will refactor in the future*
- [ ] Generosity - Enable the game to load custom models - *didn't figure out how to make Unity load assets at run time yet, maybe it's not possible, at least not legally, I may need to turn the entire Unity engine inside out*
- [ ] Laughter - Add online multiplayer functionality, you should be able to see other players as any pony they choose, there will be text chat and voice chat, and you should be able to compete with other players in a minigame - *this is the one I'm most interested in, the only dilemma is how to host a game; P2P or dedicated server? Is there any funding for a server?*
- [ ] Kindness - Dedicated online server program - *maybe this should be a separate GitHub project on its own, and maybe this functionality didn't completely match the title, so maybe in the future I'll reuse this title for something else*
- [ ] Honesty - Online multiplayer anti-cheat? - *well this one is just for the meme, I didn't come up with anything actually useful that matches the title (I mean who would cheat in this game?)... so maybe in the future I'll reuse this title for something else*
- [ ] Battle of the Bands - Add the ability to add custom songs to the music minigame - *I think it's possible, and fun (probably)*
- [ ] Friendship Games - Add new minigames - *might not be possible before I turn the entire unity engine inside out*

> [!NOTE]
> Everything beyond this line is legacy documentation from 2022

------

## Applying the mod to the game

1. build the DLL (remember to restore NuGet package first) (or just download from [release tag](https://github.com/wd357dui/Elements-of-Harmony/releases))
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
6. the name of the audio files should match the **translated terms** (case sensitive);
look for the field `OurSupportedAudioFormats` in [ElementsOfHarmony.cs](ElementsOfHarmony/ElementsOfHarmony.cs) for a list of supported audio formats; 
    >example for audio clip:<br>
    >term `Audio_BeachCove/INTRO/EV_01/BC_INTRO_EV_01_01_CS_ZP_01`<br>
    >translated (en-US) `BC_INTRO_EV_01_01_CS_ZP_01_en-US`<br>
    >translated (ru) `BC_INTRO_EV_01_01_CS_ZP_01_ru`<br>
    >audio file name (en-US) `BC_INTRO_EV_01_01_CS_ZP_01_en-US.ogg`<br>
    >audio file name (ru) `BC_INTRO_EV_01_01_CS_ZP_01_ru.ogg`<br>

    Look here for a list of **[terms](https://docs.google.com/spreadsheets/d/1-Qh_ZdBCHs9MmK423SHe68L2yfnijATV_edYO-vNyek/edit?usp=sharing)**

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

The mod attachs/hooks to every kind of error events/exceptions I can think of, `AppDomain.CurrentDomain.UnhandledException` event, `UnityEngine.Debug.LogError` method, `System.Exception` constructor, and so on; and then it logs those errors (stack trace + error message) into `Elements of Harmony/Elements of Harmony.log` if it's enabled in the settings

To enable log setting add line `Debug=true` and `Debug.Log.Enabled=true` in `Elements of Harmony/Settings.txt`

***if your game is installed in `C:\Program Files` or `C:\Program Files (x86)` the log probably won't work because writing in those folders requires administrator privileges*** <sub>(so I'd recommend NOT to install your game into those folders...)</sub>

so if you think you've encountered a bug, look into `Elements of Harmony/Elements of Harmony.log`, there is about 70% chance that the clues we needed are recorded in there
