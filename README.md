# Elements of Harmony

[中文简介](README_zh.md)

This project started as a localization mod for My Little Pony: A Maretime Bay Adventure using [Harmony API](https://github.com/pardeike/Harmony) aiming to enable fan translation of the game into Chinese.

The codebase I ended up writing does not limit it to translate to only Chinese, which is why in collaboration with Ponywka, we re-enabled Russian localizations as well.

However, since anything is possible with [Harmony API](https://github.com/pardeike/Harmony), I plan to add tons of more features, unlocking the true potential of this mod, this game, and the Unity engine.

One of those featured is already proven back in 2022 - **Loyalty**: the Motion Control module.

## Goals

- [x] Magic - Hook the DirectX API to acquire the game's swap chain, so that we can render our graphics on top of the original game using Direct2D - *already implemented in 2022, will improve in the future*
- [x] Loyalty - Kinect motion control - *already implemented in 2022, will refactor in the future*
- [ ] Generosity - Enable the game to load custom models - *[AssetBundle.LoadAsset](https://docs.unity3d.com/ScriptReference/AssetBundle.LoadAsset.html) seems like a promising lead*
- [ ] Laughter - Add online multiplayer functionality, you should be able to see other players as any pony they choose, there will be text chat and voice chat, and you should be able to compete with other players in a minigame - *should I use P2P or dedicated server?*
- [x] Battle of the Bands - Add the ability to add custom songs to the music minigame - *implemented in June 12 2024 in [Dance.cs](https://github.com/wd357dui/Elements-of-Harmony/blob/4b30960b6de5e19e246f9e79612390e1625ff82f/ElementsOfHarmony/Dance.cs)*
> [!NOTE]
> check out the tutorial on how you can add custom songs in [CustomizeSongsTutorial.md](CustomizeSongsTutorial.md)
- [ ] Friendship Games - Add new minigames - *my current idea is to create new projects in Unity, make the gameplay there, build it, and then abuse [AssetBundle.LoadAsset](https://docs.unity3d.com/ScriptReference/AssetBundle.LoadAsset.html) and [Assembly.Load](https://learn.microsoft.com/dotnet/api/system.reflection.assembly.load) to load that onto this game*
- [ ] Loyalty (Extended) - implement VR support

## Bug fixes & improvements for the base game + small additional features

- [x] **(for AMBA)** In the controls menu, two text fields were not localized, and one text field has a missing character... [Fixed](https://github.com/wd357dui/Elements-of-Harmony/blob/2a5923cbc1d3fa3228c5fb73f5897327704d5832/ElementsOfHarmony/Localization.cs#L523-L563) on March 17
- [x] **(for AZHM, can be used for both games)** Did they [turn off ACES tone mapping for AZHM](https://twitter.com/_DJTHED/status/1767448844374294534)? No problem, let me just invent a way to turn it back on... There, [fixed](https://github.com/wd357dui/Elements-of-Harmony/blob/1229ea2797f30bb2ab82959192c87078cb805fe8/ElementsOfHarmony/DirectXHook.cs#L351-L515)
- [x] **(for AMBA)** I saw a line that says "Logo language" in the log, and that's when I knew that I could implement a **logo localization** feature, too. [Implemented](https://github.com/wd357dui/Elements-of-Harmony/blob/4102f64e94fe77489219f200350835ac8305359a/ElementsOfHarmony/Localization.cs#L557-L611) on March 19, recovered the [Russian game logo](https://github.com/wd357dui/Elements-of-Harmony/blob/master/Assets/Localization/AMBA/ru/RUS_MLP_logo.png), made a [Chinese game logo](https://github.com/wd357dui/Elements-of-Harmony/blob/master/Assets/Localization/AMBA/zh-CN/CHI_MLP_logo.png).
- [x] **(for AMBA)** After some investigation in the logs and some scouting on the Internet, I found the reason why all the text on the main menu appears blurry, it's because the main menu [is using FXAA](https://forum.unity.com/threads/tmpro-same-text-looks-fine-on-ui-but-blurry-in-3d.1077041/#post-6950597)! [Fixed](https://github.com/wd357dui/Elements-of-Harmony/blob/25f33e4213a21f1dab3988d33bbbbf199339e286/ElementsOfHarmony/Localization.cs#L652-L665) on March 19 (when encounters FXAA settings, change it to SMAA)

- [x] All this talk of tone mapping made me sort of want to find ways to enable HDR output for the game. ~~Codes of interest I found are: `Camera.allowHDR`; [implemented](https://github.com/wd357dui/Elements-of-Harmony/blob/2f8c7b6a488d60cb6b9a1ee5718e91e750bad1ca/ElementsOfHarmony/DirectXHook.cs#L178-L184) on March 21 but didn't test it out yet, I don't have an HDR display right now, I'm attending college in another province and my HDR TV is back at home...~~ Implemented in [v0.3.2](https://github.com/wd357dui/Elements-of-Harmony/releases/tag/v0.3.2)
- [ ] With all these gradually increasing amounts of settings, I feel like I need to bring the property editor module from my DirectX game engine project (not on GitHub) into this mod, to support real-time adjustments of settings. However, I need to consider whether to use a separate window (already implemented) or re-render controls with Direct2D in-game (not implemented yet), the latter will take time, and may not make it before the game comes out. *(If anyone asks why I don't just use [ImGUI](https://github.com/ocornut/imgui) or something, it's because I don't like using any third-party libraries)*

> [!NOTE]
> It's been some time since I updated this list, I couldn't keep track and post the code links anymore, so from now on I'm just going to post only the descriptions. (April 16 2024)

- [x] **(for both games)** `DirectXHook` - fixes compatibility with the Steam overlay hook by temporarily un-patching Steam's hook when [Present](https://learn.microsoft.com/en-us/windows/win32/api/dxgi/nf-dxgi-idxgiswapchain-present) is being called more than one time for a single frame (being called more than one time but has returned 0 times)
- [x] **(for both games)** Implement display settings in `Settings.txt`, resolution, fullscreen, refresh rate, anti-aliasing, v-sync, target framerate
- [x] **(for AMBA)** Background music was too quiet, I adjusted it so that setting 100% is now +20db instead of -0db
- [x] **(for AMBA)** Move all minigame tutorials to appear before the "3, 2, 1, go" countdown
- [x] **(for AMBA)** Recover audio instructions from many of the minigame tutorials (yes they all used to have audio, but some are disabled in release)
- [x] **(for AMBA)** Fix **Zipp's Flight Academy** minigame's background music volume (was too quiet and didn't comply with the menu's audio settings)
- [x] **(for AMBA)** Recover the hit evaluation text ("good", "cool", "perfect") in the **Pipp Pipp Dance Parade** minigame (yes, this existed in the game but was disabled in release)
- [x] **(for AMBA)** In the **Sprout's Roller Blading Chase** minigame, shorten the distance between Sprout and the player as the game progresses while in singleplayer mode
- [x] **(for AMBA)** In the **Sprout's Roller Blading Chase** minigame, play some of the unused audio clips in story mode ("story mode" means entering the minigame through the story instead of from the main menu)
- [x] **(for AMBA)** Remove the green filter in the swamp area (in Town Park where the herding crabs minigame is) and in the herding crabs minigame
- [ ] **(for AMBA, can be used for both games)** Implement custom font?... I spent 4 days trying out different ideas, but after finally understanding the situation, I've concluded that this is not possible until I implement the `Generosity` module

> [!NOTE]
> First update following AZHM's release, it's a shame that I'm 2 days late due to classes (whose idea was it to put classes on Saturday?) May 19 2024

- [x] **(for both games)** added MelonLoader support, you can now apply this mod using [MelonLoader](https://github.com/LavaGang/MelonLoader). in [MelonLoaderReference](https://github.com/wd357dui/Elements-of-Harmony/blob/f45d8e19e6b18232251bb7cec12259814fe0df92/ElementsOfHarmony.MelonLoaderReference/ElementsOfHarmony.cs)
- [x] **(for AZHM)** fixed a bug where the Chinese language cannot be displayed due to the base game's incorrect language mappings in [Localization.cs](https://github.com/wd357dui/Elements-of-Harmony/blob/f45d8e19e6b18232251bb7cec12259814fe0df92/ElementsOfHarmony.AZHM/Localization.cs#L54-L73)
- [x] **(for both games)** added BepInEx support in [BepInExReference](https://github.com/wd357dui/Elements-of-Harmony/blob/51dcd45d01d2aea6960f581f1256f44dd1179bfb/ElementsOfHarmony.BepInExReference/ElementsOfHarmony.cs)
- [x] **(for AZHM)** The URP shaders in AZHM were stripped, I tried to bring them back and succeeded by patching [globalgamemanagers.assets](https://github.com/wd357dui/Elements-of-Harmony/blob/e7a6b9d0b757e52d9b29a6d5f4586d71da9b4eeb/Patch/AZHM/globalgamemanagers.assets) after fixing AssetRipper/AssetRipper#1365
- [x] **(for AMBA)** fix combo bar's stutter issue in the dancing game minigame by depending on `Time.time` instead of `AudioSource.time` for checking time progression. My reason and supporting evidence for [Fixing](https://github.com/wd357dui/Elements-of-Harmony/blob/e7a6b9d0b757e52d9b29a6d5f4586d71da9b4eeb/ElementsOfHarmony.AMBA/Dance.cs#L457-L464) the issue in this way are: 1.[Timeline AudioTrack stuttering and artifacts](https://forum.unity.com/threads/timeline-audiotrack-stuttering-and-artifacts.488543/); 2.[Audiosource.time innaccurate in Android?](https://forum.unity.com/threads/audiosource-time-innaccurate-in-android.471872/)
- [x] **(for AZHM)** fix stutter every 1 second by stopping it from re-setting the same resolution every 1 second in [DirectXHook.cs](https://github.com/wd357dui/Elements-of-Harmony/blob/70806c98696f6af7b82c3a90f382d25f50a00c66/ElementsOfHarmony.AZHM/DirectXHook.cs#L37-L44)

------

> [!WARNING]
> Everything beyond this line is legacy documentation from 2022, only for archive purposes

------

## ~~Applying the mod to the game~~ (obsolete)

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
2. to add text translations, in the `Elements of Harmony` folder, create ~~a sub folder named `Translations` and put translation files inside~~ (since version 0.3.0, this location is changed to `Elements of Harmony/Assets/Localization/[GameTitleInitials]/[YourLanguageISOCode]/[YourLanguageISOCode].txt`)
3. the translation file name should be `(language ISO code).txt` (case sensitive), its content should be tab-separated values (TSV), where first column is the **term** (case sensitive) and second column is the translated text (use \n for line break during text); other columns will be ignored
4. please also add your language code as **term** and your language name as translated text so that your language name can show up in the game menu correctly
5. to add localized audio files, in the `Elements of Harmony` folder, ~~create a sub folder named `AudioClip` and put the audio files inside (you can create sub folders in `AudioClip` as well, the mod will recursively search for all audio files in all sub folders)~~ (since version 0.3.0, this location is changed to `Elements of Harmony/Assets/Localization/[GameTitleInitials]/[YourLanguageISOCode]/AudioClip/`)
6. the name of the audio files should match the **translated terms** (case sensitive); in nerds' language, it's a two-stage-mapping method.
look for the field `OurSupportedAudioFormats` in [ElementsOfHarmony.cs](ElementsOfHarmony/ElementsOfHarmony.cs) for a list of supported audio formats; 
    >example for audio clip:<br>
    >term `Audio_BeachCove/INTRO/EV_01/BC_INTRO_EV_01_01_CS_ZP_01`<br>
    >translated (en-US) `BC_INTRO_EV_01_01_CS_ZP_01_en-US`<br>
    >translated (ru) `BC_INTRO_EV_01_01_CS_ZP_01_ru`<br>
    >audio file name (en-US) `BC_INTRO_EV_01_01_CS_ZP_01_en-US.ogg`<br>
    >audio file name (ru) `BC_INTRO_EV_01_01_CS_ZP_01_ru.ogg`<br>

    Look here for a list of **[terms](https://docs.google.com/spreadsheets/d/1-Qh_ZdBCHs9MmK423SHe68L2yfnijATV_edYO-vNyek/edit?usp=sharing)**

## ~~Disable the block on the Russian language~~ (obsolete)
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
