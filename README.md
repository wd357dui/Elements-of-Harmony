# Elements of Harmony
This project started as a localization mod for My Little Pony: A Maretime Bay Adventure using [Harmony API](https://github.com/pardeike/Harmony).
Now it's gradually becoming a multi-purpose mod that adds features (and fixes bugs?).

# New Goal 2024
A new MLP game from the same publisher (not the same developer, but they *are from* España, like the previous one) is confirmed, it's called **A Zephyr Heights Mystery**.

That motivated me to remaster this project to use it to mod both games.
I'm aiming to use this same modding DLL for both games, if possible. (gotta remind myself to investigate the possibility of it later)

- **The goals of this remaster (before moving on to the next stage of development)**

- [x] Refactor the codebase to meet my newest standards
- [x] Fix bugs in the AMBA base game (did you hear that Melbot? I'm fixing bugs for you so you don't have to :wink:)
- [x] Additional small improvements & small features
- [ ] Refactor and merge the `Loyalty` branch (which is a Kinect V2 motion control mod)

## Bug fixes / Improvements / Additional features

Here I will list the bugs/improvements that I have fixed/made and will (try to) fix/make in the future:

- [x] **(for AMBA)** In the controls menu, two text fields were not localized, and one text field has a missing character... [Fixed](https://github.com/wd357dui/Elements-of-Harmony/blob/2a5923cbc1d3fa3228c5fb73f5897327704d5832/ElementsOfHarmony/Localization.cs#L523-L563) on March 17
- [x] **(for AZHM, can be used for both)** Did they [turn off ACES tone mapping for AZHM](https://twitter.com/_DJTHED/status/1767448844374294534)? No problem, let me just invent a way to turn it back on... There, [fixed](https://github.com/wd357dui/Elements-of-Harmony/blob/1229ea2797f30bb2ab82959192c87078cb805fe8/ElementsOfHarmony/DirectXHook.cs#L351-L515)
- [x] **(for AMBA)** I saw a line that says "Logo language" in the log, and that's when I knew that I could implement a **logo localization** feature, too. [Implemented](https://github.com/wd357dui/Elements-of-Harmony/blob/4102f64e94fe77489219f200350835ac8305359a/ElementsOfHarmony/Localization.cs#L557-L611) on March 19, recovered the [Russian game logo](https://github.com/wd357dui/Elements-of-Harmony/blob/master/Assets/Localization/AMBA/ru/RUS_MLP_logo.png), made a [Chinese game logo](https://github.com/wd357dui/Elements-of-Harmony/blob/master/Assets/Localization/AMBA/zh-CN/CHI_MLP_logo.png).
- [x] **(for AMBA)** After some investigation in the logs and some scouting on the Internet, I found the reason why all the text on the main menu appears blurry, it's because the main menu [is using FXAA](https://forum.unity.com/threads/tmpro-same-text-looks-fine-on-ui-but-blurry-in-3d.1077041/#post-6950597)! [Fixed](https://github.com/wd357dui/Elements-of-Harmony/blob/25f33e4213a21f1dab3988d33bbbbf199339e286/ElementsOfHarmony/Localization.cs#L652-L665) on March 19 (when encounters FXAA settings, change it to SMAA)

- [x] All this talk of tone mapping made me sort of want to find ways to enable HDR output for the game. Codes of interest I found are: `Camera.allowHDR`; [implemented](https://github.com/wd357dui/Elements-of-Harmony/blob/2f8c7b6a488d60cb6b9a1ee5718e91e750bad1ca/ElementsOfHarmony/DirectXHook.cs#L178-L184) on March 21 but didn't test it out yet, I don't have an HDR display right now, I'm attending college in another province and my HDR TV is back at home...
- [ ] With all these gradually increasing amounts of settings, I feel like I need to bring the property editor module from my DirectX game engine project (not on GitHub) into this mod, to support real-time adjustments of settings. However, I need to consider whether to use a separate window (already implemented) or re-render controls with Direct2D in-game (not implemented yet), the latter will take time, and may not make it before the game comes out. *(If anyone asks why I don't just use [ImGUI](https://github.com/ocornut/imgui) or something, it's because I don't like using any third-party libraries)*

> [!NOTE]
> It's been some time since I updated this (in favor of development efficiency), so I couldn't keep track and post the code links anymore, so for these below, I'm just going to post only the descriptions. (April, 16)

- [x] **(for both)** `DirectXHook` - fixes compatibility with the Steam overlay hook by temporarily un-patching Steam's hook when [Present](https://learn.microsoft.com/en-us/windows/win32/api/dxgi/nf-dxgi-idxgiswapchain-present) is being called more than one time for a single frame (being called more than one time but has returned 0 times)
- [x] **(for both)** Implement display settings in `Settings.txt`, resolution, fullscreen, refresh rate, anti-aliasing, v-sync, target framerate
- [x] **(for AMBA)** Background music was too quiet, I adjusted it so that setting 100% is now +20db instead of -0db
- [x] **(for AMBA)** Move all minigame tutorials to appear before the "3, 2, 1, go" countdown
- [x] **(for AMBA)** Recover audio instructions from many of the minigame tutorials (yes they all used to have audio, but some are disabled in release)
- [x] **(for AMBA)** Fix **Zipp's Flight Academy** minigame's background music volume (was too quiet and didn't comply with the menu's audio settings)
- [x] **(for AMBA)** Recover the hit evaluation text ("good", "cool", "perfect") in the **Pipp Pipp Dance Parade** minigame (yes, this existed in the game but was disabled in release)
- [x] **(for AMBA)** In the **Sprout's Roller Blading Chase** minigame, shorten the distance between Sprout and the player as the game progresses while in singleplayer mode
- [x] **(for AMBA)** In the **Sprout's Roller Blading Chase** minigame, play some of the unused audio clips in story mode ("story mode" means entering the minigame through the story instead of from the main menu)
- [x] **(for AMBA)** Remove the green filter in the swamp area (in Town Park where the herding crabs minigame is) and in the herding crabs minigame
- [ ] **(for AMBA, can be used for both)** Implement custom font?... I spent 4 days trying out different ideas, but after finally understanding the situation, I've concluded that this is not possible until I implement the `Generosity` module mentioned in the [Additional Overall Optional goals](#additional-overall-optional-goals) section

## Additional Overall Optional goals

(*most of these are stuff I wanted to do at the time, some are just for the meme, and the others are there because I wanted to "unlock the true potential" of this mod, but I didn't, fearing that no one would be interested and I would have just wasted lots of time and effort for nothing...* :cry:)

- [x] Magic - Hook the DirectX API to acquire the game's swap chain, so that we can render our graphics on top of the original game using Direct2D - *already implemented in 2022, will improve in the future*
- [x] Loyalty - Kinect motion control - *already implemented in 2022, will refactor in the future*
- [ ] Generosity - Enable the game to load custom models - *[AssetBundle.LoadAsset](https://docs.unity3d.com/ScriptReference/AssetBundle.LoadAsset.html) seems like a promising lead*
- [ ] Laughter - Add online multiplayer functionality, you should be able to see other players as any pony they choose, there will be text chat and voice chat, and you should be able to compete with other players in a minigame - *this is the one I'm most interested in, the only dilemma is how to host a game; P2P or dedicated server? Is there any funding for a server?*
- [ ] Battle of the Bands - Add the ability to add custom songs to the music minigame - *I think it's possible, and fun (probably)*
- [ ] Friendship Games - Add new minigames - *my current idea is to build new projects in Unity and then abuse [AssetBundle.LoadAsset](https://docs.unity3d.com/ScriptReference/AssetBundle.LoadAsset.html) to implement this*
- [ ] Loyalty (Extended) - implement VR support

------

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
