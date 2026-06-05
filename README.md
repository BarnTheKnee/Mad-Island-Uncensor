# Mad Island Uncensor
Uncensor Mad Island with a single click.  
<img width="573" height="290" alt="WindowsTerminal_krzpOAAAbW" src="https://github.com/user-attachments/assets/5d4479c0-edda-4168-a876-6480d97c208f" />


## How to Use
1. Download and extract the latest [release](https://github.com/BarnTheKnee/Mad-Island-Uncensor/releases).
2. Move the executable to the main game folder.
3. Run the executable.
4. You can delete the executable once it's finished.

## What the patch does
* Creates an empty file in "Mad Island_Data\StreamingAssets\XML\none.bat".
* Loads the file "Mad Island_data\data.unity3d" in memory.
* Looks for a shader that contains the text "MosaicField".
* Looks for the entries with "colMask" and "val = 15" and it replaces them with "val = 0".
* Saves and compresses the file.

## FAQ
* ### Can I run the tool without compressing back the file?
    Yes, you can run the executable from a command line with the flag -c
    ```text
    Mad_Island_Uncensor.exe -c
    ```
* ### Why create the empty none.bat file?
    A combination of patching the data.unity3d and creating an empty none.bat file is necessary to uncensor everything in the game.

      
[🛡️ View the live VirusTotal Scan Report here](https://www.virustotal.com/gui/url/0c8a2dc7ac6e72aa7db92362627cdbccc9cbd1414152afa9a29dc176258f3547)
