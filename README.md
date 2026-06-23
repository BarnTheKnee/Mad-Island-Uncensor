# Mad Island Uncensor
<img width="612" height="354" alt="WindowsTerminal_TvDrWRVGyF" src="https://github.com/user-attachments/assets/3e0446fc-20ab-474b-846f-7fa9b8179fa2" />



## :inbox_tray: Download
Get the latest version in [releases](https://github.com/BarnTheKnee/Mad-Island-Uncensor/releases)
  
## :hammer_and_wrench: Installation and Use
### Windows

1. Download and extract the .zip file into your game's main folder.
2. Run the executable.

### Linux / SteamOS
> [!WARNING]
> If the binary and the folders have the right permissions, the command line installation should work fine.  
> But I don't know if the double-click installation without command line will work in every case.  
> Use at your own risk.

1. Download and extract the .zip file into the game's main folder.
2. Open a terminal in that directory and run the following command:
```text
chmod +x ./MIUncensor && ./MIUncensor
```

If you don't want to use the command line:
1. Switch your SteamOS to Desktop Mode.
2. Download and extract the .zip file into the game's main folder.
3. Select MIUncensor and MIUncensor.desktop, Right-click on them and select Properties (You may need to do this one by one).  
4. Go to the Permissions tab and check "Is Executable". Close the window. <img width="636" height="450" alt="image" src="https://github.com/user-attachments/assets/e356e3aa-60dd-408d-905d-59c3dd9b4686" />
5. Double-click the MIUncensor.desktop file to run it (choose "Execute" if prompted).


## :information_source: What the patch does
* Creates an empty file in 'Mad Island_Data/StreamingAssets/XML/none.bat'.
* Loads the file 'Mad Island_Data/data.unity3d'.
* Looks for a shaders that contain the text "MosaicField".
* Replaces all 'val = 15' entries with 'val = 0' in those shaders.
* Saves and compresses the file.


## Arguments
    Usage: MIUncensor.exe [options]
    Options:
    -b, --backup    Creates a backup of the asset bundles.
    -c [type]       Sets the compression type for output bundles.
                    Using -c without a type disables compression.
                    Available types: lz4fast (default), lz4, lzma.
    -d, --dlc       Processes the DLC bundle as well.
                    Note: Currently not necessary, the empty 'none.bat' already uncensors DLC content.
                          The DLC bundle needs to be unpacked before loading; 
                          the program handles this without needing the -u flag.
    -e, --empty     Skips creating the empty 'none.bat' file.  
    -u, --unpack    Forces unpacking of bundles to disk.
                    Note: This option does not significantly reduce RAM usage
                          The highest memory consumption occurs during the final save phase. 
                          It is also slower and requires extra disk space.  
    -h, --help      Displays the help message.

## :package: Dependencies
[AssetsTools.NET](https://github.com/nesrak1/AssetsTools.NET)  
[classdata.tpk](https://github.com/SeriousCache/UABE/blob/master/classdata.tpk)
