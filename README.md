# OptiTrack Motive file converter

**(For the tl;dr types, download the executable [here](https://github.com/ha5dzs/optitrack-motive-file-converter/releases/tag/5.0).)**

Sometimes in my experiments I don't just need to stream OptiTrack data [using the NatNet SDK](https://optitrack.com/software/natnet-sdk/), but also I need access to the entire trial, which I recorded as *take* files.

Unfortunately these files contain binary data, and I have not been able to find a file format description online. I need them in an easily accessible file format, so I can do statistics on the motion tracker data later on.

## Motive 3 differences

With Motive 3, it seems that the rigid body data is not included by default. In research, we almost exclusively use rigid bodies, as we need to rely on virtual markers. In Motive terminology, we set the 'pivot' (or as of Motive 3.1 or so, a 'Bone') to a marker that doesn't exist on the rigid body during normal use. Presumably (as, in, this is all speculation), in order to increase performance, the rigid body solver is a quick and less accurate one for when handling the data. When there are multiple rigid bodies that are similar to each other, this algorithm gets easily confused. When the files are reconstructed, auto-labelled, and then solved, this problem disappears.

The only problem with this are that this process is really slow, Motive Batch Processor requires a license, and it seems that is single-core execution only.

So, this new version of code has been brought up-to-date: It now requires .NET framework 9.0, and does all the additional steps (Reconstruct, Auto-label, Solve) before exporting to CSV.

Additionally, it seems that a lot of binaries have been updated, and `NMotive.dll` didn't load due to some undocumented dependencies. So I ended up senselessly copying everything in the project until it worked.

## Why

Motive allows you to manually export these files as .csv, but the tool is either part in Motive, or you can use the [batch processor](https://v23.wiki.optitrack.com/index.php?title=Motive_Batch_Processor) to get a bunch of files processed.

Neither of these solutions allowed me to do the conversion programmatically, or allowed me to use a separate computer that does not run Motive to process my data.

Luckily, the source code is included for the batch processor, and mere mortals like me are 'welcome to use as a starting point' for a custom application.

So I did exactly that.

## How do I use the converter?

This code is written to be called programmatically, and I made it as simple as possible:
```
converter.exe <path_to_take_file> <path_to_csv_file> <OPTIONAL: rotation_format>
```

This code creates the .csv file at `path_to_csv_file`, by reading `path_to_take_file`. The default output format is quaternions. You can change this by editing `CSVExporterSettings.motive`. If you want Euler angles and with different rotation orders for each file you process, you can simply add a number as an extra input argument. This number is interpreted as:

rotation_format  Format and rotation order
 * 0: Quaternion, `w-x-y-z` (`w-i-j-k`)
 * 1: Euler, `X-Y-Z`
 * 2: Euler, `X-Z-Y`
 * 3: Euler, `Y-X-Z`
 * 4: Euler, `Y-Z-X`
 * 5: Euler, `Z-X-Y`
 * 6: Euler, `Z-Y-X`

The paths should be absolute. You can run this tool on your computer, and you don't need a licensed copy of Motive to do this.
The exported CSV file will have 6 or 7 columns for each rigid body, with the number of lines corresponding to the number of frames recorded.
The last 3 columns are the translation coordinates. The rest is the rotation. You can tell, because the rotations are in radians, but the coordinates are in millimetres.

## What's in the code?

First of all, *there are very few sanity checks and error management built in*, so beware of unhandled exceptions and other goodies when not exactly giving the correct input arguments. This is a tool made for programmers to be used with their code, and there are many more user-friendly tools are out there than this one.

I added a return value as well, which helps when calling this external executable from my environment.

Interestingly, instead of it being shared in the Wiki, the [NMotive API](https://v22.wiki.optitrack.com/index.php?title=Motive_Batch_Processor#Class_Reference)'s documentation is bundled with Motive as a .chm file, and it claims to support C#, Visual Basic, C++, and F#. The Wiki I linked above uses C# and IronPython. Based on this, the common denominator is C#, so I wrote the code in this language.

While adding external libraries to a C# project is relatively easy, it seems that the NMotive API needs the entire Qt framework as well: more than 20 DLL files were required just to run this tiny piece of code. I guess the developers wanted people to develop shiny GUI applications that dazzles users.

The code itself does the following things:

* Check and validate config files
* Basic sanity checks on input arguments and prints usage
* Load the take file
* Process: Reconstruct and auto-label with default settings
* Save
* Solve
* Save (again)
* Initialise an instance of `CSVExporter` to export rigid body data, and converts units to millimeters, as opposed to Motive's default meter units.
* Executes the export operation to the .csv file.

I tried to add some error messages, so hopefully I will have a bit of a clue on why something failed in the future.

### Compiling

I used [Visual Studio Code](https://code.visualstudio.com/Download), downloaded a recent .NET framework, and ran the code with

```
dotnet run <path_to_take_file> <path_to_csv_file>
```

### The config files

There are now two config files. Both of them are placed in the same directory where the executable is. The code finds the absolute paths itself, so you don't have to specify anything.

The first one is `ReconstructionSettings.motive` in XML syntax, and this file is the same as the one bundled with the batch processor. Without knowing the internal workings of how Motive processes the camera data, these don't really mean much to the end user.

The other config file is `CSVExporterSettings.motive`, which also in XML syntax, and is created by serialising and exporting the CSVExporter object. The code reads this file and updates `csv_exporter` accordingly. If you delete the file, the code will create a default one. If you add something crazy or just make a typo, the code will fail to load. There will be an error message, but since it's coming from the exception management directly, it may be cryptic. Then, just delete the file, and start over.

Note that since this is going into the C# code directly, all variable types MUST have the same name as if you wrote C# code. For example, the `<Units>` tag is not just 'millimetres', but `Units_Millimeters`, because this is how it was defined in `NMotive.LengthUnits`. As this is an enumeration, you could use a number instead.

The options `true` or `false` may look straightforward, but they are case sensitive.

In case if you don't have access to `NMotiveAPI.chm`, here is a short summary of what options are available.

#### `<RotationType>`

| Mnemonic (i.e. what is in the tags) | Numerical value |
|---------|-------------|
| QuaternionFormat | 0 |
| XYZ | 1 |
| XZY | 2 |
| YXZ | 3 |
| YZX | 4 |
| ZXY | 5 |
| ZYX | 6 |

#### `<Units>`

| Mnemonic (i.e. what is in the tags) | Numerical value |
|---------|-------------|
| Units_Meters | 0 |
| Units_Centimeters | 1 |
| Units_Millimeters | 2 |

## How do I use this externally?

Super simple: you assemble the string you want to execute first, and you use your own environment's method to call it.
For instance in Matlab, will be something like this:

```
system(Y:/converter/converter.exe "D:/my_data/take05.tak" "D:/my_data/take05.csv");
```

If you want Euler angles instead of quaternions, then:

```
system(Y:/converter/converter.exe "D:/my_data/take05.tak" "D:/my_data/take05.csv 1");
```

Yes, Matlab is OK with forward slashes in the path, instead of backslashes. Also, note that the input arguments are in quotation marks, to cope with spaces and special characters in the file path.

## Enhancing the ~~brutality~~ performance

There is an included Matlab file. You just need to specify where the executable is, which directory the `.tak` files are in, and which directory the `.csv` files should go. Then it starts a parallel pool, and executes the binary.

Processing 180 trials in Motive takes about 3-4 hours. Doing the same processing steps with this code takes about 5-6 minutes on the decent workstation in the lab, depending on how you set the config files.
