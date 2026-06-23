using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using AssetsTools.NET;
using AssetsTools.NET.Extra;

namespace MIUncensor
{
    public class Uncensor
    {
        public enum LogType { Default, Ok, Warning, Skip, Error }
        public static event Action<string, int, LogType> OnLog;
        /// <summary>
        /// Logs a formatted message to the console and broadcasts it to any event subscribers.
        /// </summary>
        public static void Log(string message, int indentLevel = 0, LogType type = LogType.Default)
        {
            OnLog?.Invoke(message, indentLevel, type);
            string tag = "";
            ConsoleColor? color = null;
            switch (type)
            {
                case LogType.Ok:        tag = "[OK] ";    color = ConsoleColor.Green; break;
                case LogType.Warning:   tag = "[WARN] ";  color = ConsoleColor.Yellow; break;
                case LogType.Skip:      tag = "[SKIP] ";  color = ConsoleColor.DarkYellow; break;
                case LogType.Error:     tag = "[ERROR] "; color = ConsoleColor.Red; break;
            }
            if (color.HasValue) Console.ForegroundColor = color.Value;
            string indent = new string(' ', indentLevel * 3);
            Console.WriteLine($"{indent}{tag}{message}");
            Console.ResetColor();
        }
        /// <summary>
        /// Logs a horizontal separator line to the console.
        /// </summary>
        public static void Log(int length = 67)
        {
            Log(new string('-', length));
        }
        private static void Main(string[] args)
        {
            Stopwatch sw = new();
            sw.Start();
            //string[] keywords = ["mos", "moz", "masi", "maz", "pixel", "censor", "ピクセル", "モザイク"];
            string[] keywords = ["MosaicField"];
            string noneBatPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Mad Island_Data", "StreamingAssets", "XML", "none.bat");
            string unityPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Mad Island_Data", "data.unity3d");
            string dlcPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Mad Island_Data", "StreamingAssets", "DLC", "dlc_00");
            // Default arguments
            bool backup = false;
            bool createNoneBat = true;
            bool patchDLC = false;
            bool unPack = false;
            AssetBundleCompressionType compType = AssetBundleCompressionType.LZ4Fast;
            // Parse command-line arguments
            if (args.Contains("-b") || args.Contains("--backup")) backup = !backup;
            if (args.Contains("-d") || args.Contains("--dlc")) patchDLC = !patchDLC;
            if (args.Contains("-e") || args.Contains("--empty")) createNoneBat = !createNoneBat;
            if (args.Contains("-h") || args.Contains("--help"))
            {
                ShowHelp();
                return;
            }
            if (args.Contains("-u") || args.Contains("--unpack")) unPack = !unPack;
            int cIndex = Array.IndexOf(args, "-c");            
            if (cIndex != -1)
            {
                // Check if -c has a value after it AND that value doesn't start with '-' (which would be another flag like -d)
                if (cIndex + 1 < args.Length && !args[cIndex + 1].StartsWith("-"))
                {
                    string typeStr = args[cIndex + 1].ToLower();
                    switch (typeStr)
                    {
                        case "lzma":
                            compType = AssetBundleCompressionType.LZMA;
                            break;
                        case "lz4":
                            compType = AssetBundleCompressionType.LZ4;
                            break;
                        case "none":
                            compType = AssetBundleCompressionType.None;
                            break;
                        case "lz4fast":
                        default:
                            compType = AssetBundleCompressionType.LZ4Fast;
                            break;
                    }
                }
                else
                {
                    // -c without [type] will toggle between compression and not compression
                    if (compType != AssetBundleCompressionType.None)
                        compType = AssetBundleCompressionType.None;
                    else 
                        compType = AssetBundleCompressionType.LZ4Fast;
                }
            }
            Log();
            Log("Mad Island Uncensor v1.1.2");
            Log();
            if (createNoneBat)
            {
                // Create an empty none.bat file
                Log($"Creating 'Mad Island_Data\\StreamingAssets\\XML\\none.bat'");
                CreateEmptyNoneBat(noneBatPath);
            }
            Log();
            Log($"Modifying game assets");
            // Execute the main patching workflow on the asset bundle
            if (PatchUnityBundle(unityPath, keywords, compType, unPack, backup))
                Log($"{Path.GetFileName(unityPath)} successfully patched", 0, LogType.Ok);
            if (patchDLC)
            {
                if (File.Exists(dlcPath))
                {
                    File.Move(dlcPath, dlcPath + ".zip", true);
                    dlcPath += ".zip";
                }
                else if (File.Exists(dlcPath + ".zip"))
                    dlcPath += ".zip";
                Log();
                if (!File.Exists(dlcPath))
                    Log ("[dlc_00 or dlc_00.zip] don't exist", 0, LogType.Error);
                else if (PatchUnityBundle(dlcPath, keywords, compType, true, backup))
                    Log($"{Path.GetFileName(dlcPath)} successfully patched", 0, LogType.Ok);
            }
            //Log();
            sw.Stop();
            //Console.WriteLine($"Time elapsed: {sw.Elapsed:ss\\.fff}s");
            Log();
            Log("Press ENTER to exit");
            Log();
            Console.ReadLine();
        }
        /// <summary>
        /// Prints the available arguments
        /// </summary>
        private static void ShowHelp()
        {
            Log();
            Log("Usage: MIUncensor.exe [options]");
            Log("Options:");
            Log("  -b, --backup     Creates a backup of the asset bundles.");
            Log("  -c [type]        Sets the compression type for output bundles.");
            Log("                   Available types: lz4fast (default), lz4, lzma.");
            Log("                   Using -c without a type disables compression.");
            Log("  -d, --dlc        Processes the DLC bundle too.");
            Log("  -e, --empty      Skips creating the empty 'none.bat' file.");
            Log("  -u, --unpack     Forces the unpacking of bundles to disk.");
            Log("  -h, --help       Displays this help message.");
            Log();
            Log("Press ENTER to exit");
            Log();
            Console.ReadLine();
        }
        /// <summary>
        /// Creates an empty none.bat if it doesn't already exist.
        /// </summary>
        public static bool CreateEmptyNoneBat(string noneBat)
        {
            string directory = Path.GetDirectoryName(noneBat);
            if (File.Exists(noneBat))
            {
                Log($"{Path.GetFileName(noneBat)} already exists", 0, LogType.Skip);
                return true;
            }
            try
            {
                if (!Directory.Exists(directory))
                {
                    Log($"\'Mad Island_Data\\StreamingAssets\\XML\' doesn't exist. Make sure this tool is in the game's root directory", 0, LogType.Error);
                    return false;
                }
                Directory.CreateDirectory(directory);
                File.WriteAllBytes(noneBat, []);
                Log($"Empty {Path.GetFileName(noneBat)} successfully created.", 0, LogType.Ok);
                return true;
            }
            catch (Exception ex)
            {
                Log($"Failed to create {Path.GetFileName(noneBat)}: {ex.Message}", 0, LogType.Error);
                return false;
            }
        }
        /// <summary>
        /// Loads, targets, modifies and saves the specific asset bundle file.
        /// </summary>
        private static bool PatchUnityBundle(string fileName, string[] searchText, AssetBundleCompressionType compType = AssetBundleCompressionType.LZ4Fast, bool unPack = false, bool backup = false)
        {
            BundleFileInstance bundle = null;
            var am = new AssetsManager();
            // Load Unity bundle
            if (!LoadFile(unPack, fileName, ref bundle, am)) return false;
            // Search for the Shaders with specific text
            var foundShaders = SearchShaders(searchText, bundle, am);
            // Edit the Shaders found
            if (foundShaders.Count != 0 && !EditShaders(foundShaders, bundle, am))
            {
                CloseBundle(ref bundle, am);
                Log("No modifications applied", 0, LogType.Warning);
                return false;
            }
            // Save changes
            return SaveUnityBundle(fileName, bundle, am, compType, unPack, backup);
        }
        /// <summary>
        /// Loads the bundle into memory.
        /// </summary>
        private static bool LoadFile(bool unPack, string originalPath, ref BundleFileInstance bundle, AssetsManager am, bool logBundleName = true)
        {
            if (!File.Exists(originalPath))
            {
                string relativePath = Path.GetRelativePath(AppDomain.CurrentDomain.BaseDirectory, originalPath);
                Log($"'{relativePath}' doesn't exist. Make sure this tool is in the game's root directory", 0, LogType.Error);
                return false;
            }
            GetTypeTree(ref am);
            if (logBundleName) Log($"[{Path.GetFileName(originalPath)}]");
            string loadPath = originalPath;
            if (unPack)
            {
                string unpackedPath = originalPath + ".pack";
                if (UnpackBundle(am, originalPath, ref unpackedPath))
                    loadPath = unpackedPath;
                else
                    return false;
            }
            Log($"Loading bundle...", 1);
            bundle = am.LoadBundleFile(loadPath, false);
            if (bundle == null)
            {
                Log("Failed to load bundle file (returned null).", 0, LogType.Error);
                return false;
            }
            if (!GetConfSchema(bundle, am))
            {
                if (unPack) return false;
                // If this failed it could be because the bundle needs to be unpacked first
                Log("Will retry unpacking the bundle first", 1);
                CloseBundle(ref bundle, am);
                return LoadFile(true, originalPath, ref bundle, am, false);
            }
            return true;
        }
        /// <summary>
        /// Scans the bundle for shader assets matching the search text and returns their Path IDs grouped by directory block name.
        /// </summary>
        public static Dictionary<string, Dictionary<long, string>> SearchShaders(string[] searchText, BundleFileInstance bundle, AssetsManager am, string blockName = "")
        {
            var matchedAssets = new Dictionary<string, Dictionary<long, string>>();
            foreach (var dir in bundle.file.BlockAndDirInfo.DirectoryInfos)
            {
                if(!string.IsNullOrEmpty(blockName) && blockName != dir.Name)
                    continue;
                var inst = am.LoadAssetsFileFromBundle(bundle, dir.Name, false);
                if (inst?.file?.AssetInfos == null)
                    continue;
                foreach (var info in inst.file.AssetInfos)
                {
                    if ((AssetClassID)info.TypeId != AssetClassID.Shader)
                        continue;
                    try
                    {
                        var baseField = am.GetBaseField(inst, info);
                        string matchedShaderName = null;
                        foreach (var target in searchText)
                        {
                            matchedShaderName = ContainsText(baseField, target);
                            if (matchedShaderName != null) 
                            {
                                Log($"\"{target}\" found in 'Path_ID = {info.PathId}'", 2);
                                // Ensure the list exists for this specific asset directory block
                                if (!matchedAssets.TryGetValue(dir.Name, out Dictionary<long, string> value))
                                {
                                    value = [];
                                    matchedAssets[dir.Name] = value;
                                }
                                value[info.PathId] = matchedShaderName;
                                break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Skip to next asset
                        Log($"Failed to scan asset with 'Path_ID = {info.PathId}': {ex.Message}", 2, LogType.Error);
                    }
                }
            }
            // Convert the flat list into a lookup grouped by Directory Name
            return matchedAssets;
        }
        /// <summary>
        /// Overload to directly accept the output of SearchShaders as input
        /// </summary>
        public static bool EditShaders(Dictionary<string, Dictionary<long, string>> targets, BundleFileInstance bundle, AssetsManager am)
        {
            if (targets == null || targets.Count == 0) return false;
            // Convert the nested dictionary
            var convertedTargets = new Dictionary<string, List<long>>();
            foreach (var dirKvp in targets)
            {
                string dirName = dirKvp.Key;
                Dictionary<long, string> pathIdMap = dirKvp.Value;
                // Extract just the Path_ID keys from the inner dictionary into a List
                convertedTargets[dirName] = [.. pathIdMap.Keys];
            }
            return EditShaders(convertedTargets, bundle, am);
        }
        /// <summary>
        /// Patches targeted shader assets within the bundle using pre-discovered directory block names and Path IDs.
        /// </summary>
        public static bool EditShaders(Dictionary<string, List<long>> targets, BundleFileInstance bundle, AssetsManager am)
        {
            if (targets == null || !targets.Any()) return false;
            bool changesMade = false;
            foreach (var dir in bundle.file.BlockAndDirInfo.DirectoryInfos)
            {
                // Snipe specific directory
                if (!targets.ContainsKey(dir.Name)) continue;
                var inst = am.LoadAssetsFileFromBundle(bundle, dir.Name, false);
                if (inst?.file == null) continue;
                bool fileModified = false;                
                // Retrieve only the specific Path IDs saved for this directory block
                foreach (long pathId in targets[dir.Name])
                {
                    //Log("Path_id=" + pathId, 2);
                    // Direct asset lookup from the loaded file structure
                    var info = inst.file.GetAssetInfo(pathId);
                    if (info == null) continue;
                    try
                    {
                        var baseField = am.GetBaseField(inst, info);
                        int changed = PatchColMask(baseField);
                        if (changed > 0)
                        {
                            info.SetNewData(baseField);
                            fileModified = true;
                            Log($"{changed} colMasks replaced in {pathId}", 2);
                        }
                        else
                        {
                            Log($"No 'val > 0' found in {pathId}, already patched?", 2, LogType.Skip);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log($"Failed to patch asset with 'Path_ID = {pathId}': {ex.Message}", 2, LogType.Error);
                    }
                }
                // Save modifications back to this directory block
                if (fileModified)
                {
                    try
                    {
                        Log($"Applying modifications...", 2);
                        using var ms = new MemoryStream();
                        using (var assetsWriter = new AssetsFileWriter(ms))
                        {
                            inst.file.Write(assetsWriter);
                        }
                        dir.SetNewData(ms.ToArray());
                        changesMade = true;
                    }
                    catch (Exception ex)
                    {
                        Log($"Failed to save changes to block {dir.Name}: {ex.Message}", 2, LogType.Error);
                        return false;
                    }
                }
            }
            return changesMade;
        }
        /// <summary>
        /// Saves, compresses and backs up the modifications done to a Unity Bundle, then cleans up temporary files.
        /// </summary>
        public static bool SaveUnityBundle(string fileName, BundleFileInstance bundle, AssetsManager am, AssetBundleCompressionType compType = AssetBundleCompressionType.LZ4Fast, bool unPack = false, bool backup = false)
        {
            string unpackedPath = fileName + ".pack";
            string patchedPath = fileName + ".patch";
            string compressedPath = fileName + ".comp";
            try
            {
                // Save changes
                Log("Saving bundle...", 1);
                using (var writer = new AssetsFileWriter(File.Create(patchedPath)))
                {
                    bundle!.file.Write(writer);
                }
                CloseBundle(ref bundle, am);
                // Compress
                if (compType != AssetBundleCompressionType.None && !Compress(compType, patchedPath, compressedPath))
                    compressedPath = "";
                // Backups and/or replaces the original file with the patched or compressed one
                if (FinalizePatch(fileName, patchedPath, compressedPath, backup))
                    return true;
                return false;
            }
            catch (Exception ex)
            {
                Log($"Patching failed: {ex.Message}", 0, LogType.Error);
                return false;
            }
            finally
            {
                CleanUpTempFiles(unpackedPath, patchedPath, compressedPath);
            }
        }
        /// <summary>
        /// Unpacks the embedded type database resource required by AssetsTools to serialize and deserialize assets without engine runtimes.
        /// </summary>
        private static void GetTypeTree(ref AssetsManager am)
        {
            if (am.ClassPackage != null) return;
            var assembly = typeof(Uncensor).Assembly;
            string assemblyName = assembly.GetName().Name;
            string resourceName = $"{assemblyName}.classdata.tpk";
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                foreach (var name in assembly.GetManifestResourceNames()) Log("Available: " + name);
                throw new Exception("Could not find embedded 'classdata.tpk'!");
            }
            am.LoadClassPackage(stream);
        }
        /// <summary>
        /// Gather structural configuration schemas against the first indexed directory file's layout
        /// </summary>
        private static bool GetConfSchema(BundleFileInstance bundle, AssetsManager am)
        {
            try
            {
                if (bundle?.file?.BlockAndDirInfo?.DirectoryInfos == null || bundle.file.BlockAndDirInfo.DirectoryInfos.Count == 0)
                {
                    Log("Bundle does not contain any directory blocks.", 1, LogType.Error);
                    return false;
                }
                var firstDir = bundle.file.BlockAndDirInfo.DirectoryInfos[0];
                var assetInst = am.LoadAssetsFileFromBundle(bundle, firstDir.Name, false);
                am.LoadClassDatabaseFromPackage(assetInst.file.Metadata.UnityVersion);
                return true;
            }
            catch (Exception ex)
            {
                Log($"Failed to initialize shader search: {ex.Message}", 1, LogType.Error);
                return false;
            }            
        }
        /// <summary>
        /// Unpacks compressed archives to a temporary file.
        /// </summary>
        private static bool UnpackBundle(AssetsManager am, string originalPath, ref string unpackedPath)
        {
            BundleFileInstance bundleInst = null;
            try
            {
                bundleInst = am.LoadBundleFile(originalPath, false);
                if (bundleInst.file.GetCompressionType() == AssetBundleCompressionType.None)
                {
                    Log($"{Path.GetFileName(originalPath)} is already uncompressed. Skipping extraction", 1, LogType.Skip);
                    unpackedPath = originalPath;
                    return true;
                }
                Log($"Unpacking...", 1);
                using var writer = new AssetsFileWriter(File.Create(unpackedPath));
                    bundleInst.file.Unpack(writer);
                return true;
            }
            catch (Exception ex)
            {
                Log($"Failed to unpack bundle: {ex.GetType().Name} - {ex.Message}", 0, LogType.Warning);
                return false;
            }
            finally
            {
                CloseBundle(ref bundleInst, am);
            }            
        }
        /// <summary>
        /// Targets specific naming fields to check for search keywords.
        /// </summary>
        private static string ContainsText(AssetTypeValueField field, string target)
        {
            if (field == null) return null;
            //Get the root m_Name
            var nameField = field["m_Name"];
            string shaderName = nameField != null && !nameField.IsDummy ? nameField.AsString : string.Empty;
            // If root m_Name is empty, fall back to m_ParsedForm -> m_Name
            if (string.IsNullOrWhiteSpace(shaderName))
            {
                var parsedForm = field["m_ParsedForm"];
                if (parsedForm != null && !parsedForm.IsDummy)
                {
                    var internalNameField = parsedForm["m_Name"];
                    if (internalNameField != null && !internalNameField.IsDummy)
                    {
                        shaderName = internalNameField.AsString;
                    }
                }
            }
            // Perform the keyword check on the found name
            if (!string.IsNullOrEmpty(shaderName) && shaderName.Contains(target, StringComparison.OrdinalIgnoreCase))
            {
                return shaderName;
            }
            return null;
        }
        /// <summary>
        /// Navigates the shader's structural layout to target and reset color mask parameters.
        /// </summary>
        private static int PatchColMask(AssetTypeValueField field)
        {
            if (field == null) return 0;
            int changed = 0;
            var parsedForm = field["m_ParsedForm"];
            if (parsedForm == null || parsedForm.IsDummy) return 0;
            // Go to m_SubShaders array wrapper
            var subShadersArray = parsedForm["m_SubShaders"]["Array"];
            if (subShadersArray == null || subShadersArray.IsDummy) return 0;
            // Loop through SubShaders
            foreach (var subShader in subShadersArray.Children)
            {
                // Go to m_Passes array wrapper
                var passesArray = subShader["m_Passes"]["Array"];
                if (passesArray == null || passesArray.IsDummy) continue;
                // Loop through Passes
                foreach (var pass in passesArray.Children)
                {
                    var state = pass["m_State"];
                    if (state == null || state.IsDummy) continue;
                    // Loop through fields inside m_State (rtBlend0 to rtBlend7)
                    foreach (var rtBlend in state.Children)
                    {
                        var colMask = rtBlend["colMask"];
                        if (colMask == null || colMask.IsDummy) continue;
                        var valField = colMask["val"];
                        if (valField != null && !valField.IsDummy)
                        {
                            //if (valField.AsFloat == 15f) 
                            if (valField.AsFloat > 0f)
                            {
                                valField.AsFloat = 0f;
                                changed++;
                            }
                        }
                    }
                }
            }
            return changed;
        }
        /// <summary>
        /// Reads back uncompressed patch outputs and recompresses using selected bundle encoders (LZ4/LZ4fast/LZMA).
        /// </summary>
        private static bool Compress(AssetBundleCompressionType compType, string patchedPath, string compressedPath)
        {
            Log($"Compressing bundle using {compType}...", 1);
            AssetsManager tempAm = null;
            BundleFileInstance tempBundle = null;
            try
            {
                tempAm = new AssetsManager();
                tempBundle = tempAm.LoadBundleFile(patchedPath, false);
                using (var compStream = File.Create(compressedPath)) 
                using (var compWriter = new AssetsFileWriter(compStream))
                {
                    tempBundle.file.Pack(compWriter, compType);
                }
                CloseBundle(ref tempBundle, tempAm);
                return true;
            }
            catch (Exception)
            {
                Log($"Couldn't compress to the bundle", 0, LogType.Warning);
                CloseBundle(ref tempBundle, tempAm);
                if (File.Exists(compressedPath)) try {File.Delete(compressedPath);} catch {}
                return false;
            }
        }
        /// <summary>
        /// Finalizes the patching process by overwriting the original file with either the patched or compressed file.
        /// </summary>
        private static bool FinalizePatch(string originalPath, string patchedPath, string compressedPath = "", bool backup = false)
        {
            bool useCompressed = !string.IsNullOrEmpty(compressedPath) && File.Exists(compressedPath);
            string sourcePath = useCompressed ? compressedPath : patchedPath;
            if (backup && !CreateBackup(originalPath)) return false;
            try
            {
                File.Move(sourcePath, originalPath, true);
                return true;
            }
            catch (Exception ex)
            {
                Log($"Process Failed! Couldn't overwrite {Path.GetFileName(originalPath)}\nMake sure the game is closed!\n{ex.Message}", 0, LogType.Error);
                return false;
            }            
        }
        /// <summary>
        /// Creates a backup of the file
        /// </summary>
        private static bool CreateBackup(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) 
            {
                Log($"Incorrect original filename provided:{fileName}", 0, LogType.Error);
                return false;
            }
            string backupFile = fileName + ".orig";
            if (File.Exists(backupFile))
                Log($"Backup already exists in {Path.GetFileName(backupFile)}. Overwriting...", 1);
            else
                Log($"Creating a backup: {Path.GetFileName(backupFile)}...", 1);
            try
            {
                File.Move(fileName, backupFile, true);
                return true;
            }
            catch (Exception ex)
            {
                Log($"Process Failed! Couldn't move {Path.GetFileName(fileName)} or overwrite {Path.GetFileName(backupFile)}\nMake sure the game is closed!\n{ex.Message}", 0, LogType.Error);
                return false;                
            }
        }
        /// <summary>
        /// Safely releases file handles and resources associated with the bundle and its manager.
        /// </summary>
        private static void CloseBundle(ref BundleFileInstance bundle, AssetsManager am)
        {
            bundle?.file?.Close();
            bundle?.file?.Reader?.Close();
            am.UnloadAll();
            bundle = null;
            GC.Collect();
        }
        /// <summary>
        /// Restores a backed up file
        /// </summary>
        private static bool RestoreBackup(string fileName)
        {
            try
            {
                string backupFile = fileName + ".back";
                if (!string.IsNullOrEmpty(fileName) && File.Exists(backupFile))
                    File.Move(backupFile, fileName, true);
                return true;
            }
            catch (Exception ex)
            {
                Log($"Failed to restore the backup. {ex.Message}", 0, LogType.Error);
                return false;
            }
        }
        /// <summary>
        /// Cleans up leftover temporary files.
        /// </summary>
        private static bool CleanUpTempFiles(string unpackedPath, string patchedPath, string compressedPath)
        {
            try
            {
                string[] tempFiles = [unpackedPath,patchedPath,compressedPath];
                if (tempFiles.Any(File.Exists))
                {
                    Log($"Cleaning up temporary files...", 1);
                    foreach (var file in tempFiles)
                        if (!string.IsNullOrWhiteSpace(file) && File.Exists(file))
                            File.Delete(file);
                }
                return true;
            } 
            catch (Exception ex) 
            {
                Log($"Couldn't delete temporary files: {ex.Message}", 1, LogType.Warning);
                return false;
            }
        }
    }
}