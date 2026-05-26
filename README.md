# Mad-Island-Uncensor
Uncensor Mad Island with a single click.  
<img width="667" height="450" alt="image" src="https://github.com/user-attachments/assets/ca8e7f11-9cd6-4f99-b03f-0359bc9be12b" />




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
* It does the same process for the DLC file.
* Finally, creates and empty file in "Mad Island_Data\StreamingAssets\XML\none.bat".

## FAQ
* ### Why does it take a couple of minutes to patch the game?
    Because it compresses the file back to its original size. The uncompressed file is around 3.5 GB while compressed is around 1GB.
* ### Why patch the DLC file too?
    Apparently if only the data.unity3d file is patched and there's no empty none.bat file, some DLC content may be still censored. It seems that both patching the DLC file or creating the empty none.bat fixes this problem, so the script does both.
* ### Why create the empty none.bat file?
    Creating the empty file uncensors the majority of the game, but not all. And apparently patching the data.unity3d file doesn't uncensor some DLC content either. So the combination of patching the data.unity3d and creating the emtpy file is necessary to uncensor everything in the game.  
  
[🛡️ View the live VirusTotal Scan Report here](https://www.virustotal.com/gui/url/4d08d91833b327ba212b252564c85e422946f31b8d1591abb3cb04d3a6ffa5d6)
