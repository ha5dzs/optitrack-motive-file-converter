# OptiTrack Motive file converter

### (For the tl;dr types, download the executable [here](https://github.com/ha5dzs/optitrack-motive-file-converter/releases/tag/1.0).)

Sometimes in my experiments I don't just need to stream OptiTrack data [using the NatNet SDK](https://optitrack.com/software/natnet-sdk/), but also I need access to the entire trial, which I recorded as *take* files.

Unfortunately these files contain binary data, and I have not been able to find a file format description online. I need them in an easily accessible file format, so I can do statistics on the motion tracker data later on.

Motive allows you to manually export these files as .csv, but the tool is either part in Motive, or you can use the [batch processor](https://v23.wiki.optitrack.com/index.php?title=Motive_Batch_Processor) to get a bunch of files processed.

None of these solutions allowed me to do the conversion programmatically.

Luckily, the source code is included for the batch processor, and mere mortals like me are 'welcome to use as a starting point' for a custom application.

So I did exactly that.

# How do I use the converter?

This code is written to be called programmatically, and I made it as simple as possible:
```
converter.exe <path_to_take_file> <path_to_csv_file>
```

This code creates the .csv file at `path_to_csv_file`, by reading `path_to_take_file`.


The paths should be absolute. You can run this tool on your computer, and you don't need a licensed copy of Motive to do this.

# What's in the code?

First of all, there are very few sanity checks and error management built in, so beware of unhandled exceptions and other goodies when not exactly giving the correct input arguments.

I added a return value as well, which helps when calling this external executable from my environment.

Interestingly, instead of it being shared in the Wiki, the [NMotive API](https://v22.wiki.optitrack.com/index.php?title=Motive_Batch_Processor#Class_Reference)'s documentation is bundled with Motive as a .chm file, and it claims to support C#, Visual Basic, C++, and F#. The Wiki I linked above uses C# and IronPython. Based on this, the common denominator is C#, so I wrote the code in this language.

While adding external libraries to a C# project is relatively easy, it seems that the NMotive API needs the entire Qt framework as well: more than 20 DLL files were required just to run this tiny piece of code. I guess the developers wanted people to develop shiny GUI applications that dazzles users.

The code itself does the following things:

* Basic sanity checks on input arguments and prints usage
* Load the take file
* Initialise an instance `CSVExporter` to export rigid body data, and converts units to millimeters, as opposed to Move's default meter units.
* Executes the export operation to the .csv file
I tried to add some error messages so hopefully I will have a bit of a clue on why something failed in the future.

## Compiling

I used [Visual Studio Code](https://code.visualstudio.com/Download), downloaded a recent .NET framework, and ran the code with
```
dotnet run <path_to_take_file> <path_to_csv_file>
```

This created my binary (with all the debug symbols in it, but hey, this is a quick and dirty project), which you can download here too.

# How do I use this externally?

Super simple: you assemble the string you want to execute first, and you use your own environment's method to call it. For instance in Matlab, will will be something like this:
```
system(Y:/converter/converter.exe "D:/my_data/take05.tak" "D:/my_data/take05.csv");
```
Yes, Matlab is OK with forward slashes in the path, instead of backslashes. Also, note that the input arguments are in quotation marks, to cope with spaces and special characters in the file path.