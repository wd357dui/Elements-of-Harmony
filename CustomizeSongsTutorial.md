## 0. Installing the Mod
You need to install the mod first. You can download the latest release from [here](https://github.com/wd357dui/Elements-of-Harmony/releases/latest) or [moddb](https://www.moddb.com/mods/elements-of-harmony)

Inside the publish.7z archive, there are three folders.
Extract the files from the Standalone folder into your game directory and replace the original files.
Alternatively, if you're using a mod manager like MelonLoader or BepInEx, use the corresponding folder.

> [!WARNING]
> If you initially used `Standalone` and later switched to `MelonLoader` or `BepInEx`,
> you first need to delete all the previous DLL files extracted from `Standalone`
> and then use Steam's "Verify Integrity of Game Files" option to restore the original game files.

## 1. Files and Locations
Custom song files should be placed in:
`[Game Directory]\Elements of Harmony\Assets\Minigame\Dance`

For example, if your game is installed on drive D, the folder path should probably look like this:
`D:\SteamLibrary\steamapps\common\My Little Pony\Elements of Harmony\Assets\Minigame\Dance`

A song should have 2 (or 3, with the third being optional) files with the same name, such as:
- Starbound.txt
- Starbound.ogg
- Starbound.lrc

The .txt file contains all the song information (format explained later).
.ogg is the audio file. You can use [ffmpeg](https://www.ffmpeg.org/) to convert your song to this format.
.lrc is a standard LRC lyrics file. If you add a lyrics file, the lyrics will be displayed at the top of the screen in the game.

## 2. File Format
The file consists of several [tags] and their corresponding content. For example:

``` plaintext
[Description]
Starbound
Trey Husk, Nexgen & Koa

[BPM]
128

[BeginAt]
00:01.875

[EndAt]
03:01.875

[Speed]
4,6

[Score]
50%,70%,90%

[PunchOffset]
00:00.000

[Punches]
00:01.875
00:02.343
00:02.812
00:03.515
00:04.453
00:04.921
00:05.390
00:06.562
00:07.265
...
```

- **[Description]**: This is plain text that will be displayed on the left side of the screen in the game. It is recommended to write the song name and artists here.
- **[BPM]**: This is the song's BPM, it's usage will be explained later.
- **[BeginAt]**: Sets the timestamp the audio will reach after the "3, 2, 1, go" countdown ends.
- **[EndAt]**: Sets the timestamp when the progress bar starts to retract.
- **[Speed]**: Sets the scroll speed of the Punch points, one for single-player and one for multiplayer, separated by a comma. The multiplayer speed should be set faster as the speed is halved by the left-right split-screen.
- **[Score]**: Sets the score for 1-star to 3-star ratings, which can be in percentages or absolute values.
- **[PunchOffset]**: Can be used to offset all Punch points.
- **[Punches]**: Defines all Punch points. For a long-press "combo bar", define the *start time* and *end time* on the same line, separated by a comma (you can add a space after the comma for readability). For example:

``` plaintext
[Punches]
00:03.515, 00:04.515
```

Each beat within a combo bar will count as one score. The BPM's purpose comes into play here: if you have defined a BPM tag, the code will generate all beat timestamps based on this BPM value.
You can add a `[BeatOffset]` to offset all beat timestamps (this is similar to `[PunchOffset]`).
If your song's BPM changes midway, you can use the `[Beats]` tag instead to manually define all beat timestamps just like the `[Punches]` tag.
If neither `[BPM]` nor `[Beats]` are defined, the combo bar might not score during the long press, but this hasn't been tested as it's not a normal use case.

> [!NOTE]
> As of version v0.3.2.2, the minutes in the timestamp must be in two digits, not one.
> This issue will be improved in the next version, but for now, use the following format:
> 
> Correct: `00:03.515`
> 
> Incorrect: `0:03.515`

## 3. Design Workflow
Feel free to design a workflow for designing maps yourself, here is my workflow for reference.

First, generate all beat time points for the song based on BPM and import them as markers into Adobe Audition.
Here's the code I use to generate the CSV file for import:

``` csharp
using StreamWriter output = new("D:/beats.csv"); // output to D:/beats.csv

decimal length = // the length of the song
	60m * 3m // 3 minutes
	+ 7.325m; // 7.325 seconds

decimal current = 0;
int count = 0;

output.WriteLine(
	"Name\t" +
	"Start\t" +
	"Duration\t" +
	"Time Format\t" +
	"Type\t" +
	"Description");
while (current < length)
{
	decimal minutes = decimal.Floor(current / 60m);
	decimal seconds = decimal.Floor(current % 60m);
	int milliseconds = (int)((current % 1m) * 1000m);
	output.WriteLine(
		$"{++count:D4}\t" + // Name
		$"{minutes}:{seconds}.{milliseconds:D3}\t" + // Start
		$"0:00.000\t" + // Duration
		$"decimal\t" + // Time Format
		$"Subclip\t" + // Type
		$""); // Description
	current +=
		60m / 128m // 128 BPM
		* 0.5m; // mark a timestamp for every half beat
}
```

In this way, in Adobe Audition, you can jump between beat time points by pressing <kbd>Alt</kbd> + <kbd>←</kbd> / <kbd>→</kbd> keys.

Press the <kbd>M</kbd> key at each beat you want to add a punch to, marking it as a punch timestamp.

In this way, each punch timestamp will be at a beat timestamp, making the points neatly aligned.

Finally, export all your marked punch time points, here is my code for extracting punch timestamps from the exported CSV file:

``` csharp
using StreamReader reader = new("D:/markers.csv");
using StreamWriter writer = new("D:/output.txt");

writer.WriteLine("[Punches]");

while (reader.ReadLine() is string line && !string.IsNullOrEmpty(line))
{
	string[] parts = line.Split('\t');
	if (parts[4] == "Cue") // only get the "Cue" type (white colored) markers
	{
		TimeSpan.TryParseExact(parts[1], @"m\:ss\.fff", null, out TimeSpan time);
		TimeSpan.TryParseExact(parts[2], @"m\:ss\.fff", null, out TimeSpan duration);
		if (duration > TimeSpan.Zero) // large combo bar
		{
			var endAt = time + duration;
			writer.WriteLine(@$"{time:mm\:ss\.fff}, {endAt:mm\:ss\.fff}");
		}
		else // punch point
		{
			writer.WriteLine(@$"{time:mm\:ss\.fff}");
		}
	}
}
```
