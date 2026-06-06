# Mad Island Uncensor
Uncensor Mad Island with a single click.  
<img width="573" height="290" alt="WindowsTerminal_krzpOAAAbW" src="https://github.com/user-attachments/assets/5d4479c0-edda-4168-a876-6480d97c208f" />

## How to Use
1. Download and extract the latest [release](https://github.com/BarnTheKnee/Mad-Island-Uncensor/releases) in the game's main folder.
2. Run the executable.

## What the patch does
* Creates an empty file in "Mad Island_Data\StreamingAssets\XML\none.bat".
* Loads the file "Mad Island_data\data.unity3d" in memory.
* Looks for a shader that contains the text "MosaicField".
* Looks for the entries with "colMask" and "val = 15" and it replaces them with "val = 0".
* Saves and compresses the file.

## Flags
-c (No Compression)  
Saves the data.unity3d file without re-compressing it.
```text
Mad_Island_Uncensor.exe -c
```
-d (Unpack to Disk)  
Unpacks the file to the disk before processing.  
Note: This option does not significantly reduce RAM usage, as the highest memory consumption occurs during the final save operation. Furthermore, this method is slower and requires extra disk space.  
Why it exists: This flag is included for future-proofing. Should future game updates change the structure of the data.unity3d file, this method may become necessary to ensure the patch remains compatible.
```text
Mad_Island_Uncensor.exe -d
```
  
[🛡️ View the live VirusTotal Scan Report here](https://www.virustotal.com/gui/url/0c8a2dc7ac6e72aa7db92362627cdbccc9cbd1414152afa9a29dc176258f3547)
