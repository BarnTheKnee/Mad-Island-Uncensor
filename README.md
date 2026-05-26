# Mad-Island-Uncensor
Uncensor Mad Island with a single click.

## How to Use
1. Download and extract the latest [release](https://github.com/BarnTheKnee/Mad-Island-Uncensor/releases).
2. Move the executable to the main game folder.
3. Run the executable.
4. You can delete the executable once it's finished.

## What the patch does
* It loads the file "Mad Island_data\data.unity3d" in memory.
* It looks for a shader that contains the text "MosaicField".
* It looks for the entries with "colMask" and "val = 15" and it replaces them with "val = 0".
* Then saves and compresses the file.
* Finally, creates and empty file in "Mad Island_Data\StreamingAssets\XML\none.bat".

## FAQ
* ### Why does it take a couple of minutes to patch the game?
    Because it compresses the file back to its original size. The uncompressed file is around 3.5 GB while compressed is around 1GB.
* ### Why create the empty none.bat file?
    Creating the empty file uncensors the majority of the game, but not all.  
    After replacing the "val = 15" to "val = 0" (which should remove all mosaics), having the empty none.bat file is probably unnecessary, but it won't hurt.
