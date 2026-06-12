using System;
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
            //string noneBatPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Mad Island_Data\StreamingAssets\XML", "none.bat");
            string noneBatPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Mad Island_Data", "StreamingAssets", "XML", "none.bat");
            string unityPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Mad Island_Data", "data.unity3d");
            string dlcPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Mad Island_Data", "StreamingAssets", "DLC", "dlc_00");
            //string dlcPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Mad Island_Data\StreamingAssets\DLC", "dlc_00");
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
            if (createNoneBat)
            {
                Log();
                // Create an empty none.bat file
                Log($"Creating 'Mad Island_Data\\StreamingAssets\\XML\\none.bat'");
                CreateEmptyNoneBat(noneBatPath);
            }
            Log();
            Log($"Modifying game assets");
            // Execute the main patching workflow on the asset bundle
            PatchUnityBundle(unityPath, compType, unPack, backup);
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
                else
                    PatchUnityBundle(dlcPath, compType, true, backup);
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
        /// Handles loading, targeting, modifying, saving, and re-compressing the specific asset bundle file.
        /// </summary>
        public static bool PatchUnityBundle(string filePath, AssetBundleCompressionType compType = AssetBundleCompressionType.LZ4Fast, bool unPack = false, bool backup = false, string mosaicText = "MosaicField")
        {
            string unpackedPath = filePath + ".pack";
            string patchedPath = filePath + ".patch";
            string compressedPath = filePath + ".comp";
            BundleFileInstance bundle = null;
            var am = new AssetsManager();

            try
            {
                // Load the target Unity bundle
                if (!LoadFile(unPack, filePath, unpackedPath, ref bundle, am))
                    return false;
                // Perform patching
                if (!EditMosaicShader(mosaicText, bundle!, am))
                {
                    CloseBundle(bundle, am);
                    CleanUpTempFiles(unpackedPath, patchedPath, compressedPath);
                    Log("No modifications applied", 0, LogType.Warning);
                    return false;
                }
                // Save changes
                Log("Saving bundle...", 1);
                using (var writer = new AssetsFileWriter(File.Create(patchedPath)))
                {
                    bundle!.file.Write(writer);
                }
                CloseBundle(bundle, am);
                // Compress
                if (compType != AssetBundleCompressionType.None && !Compress(compType, patchedPath, compressedPath))
                    compressedPath = "";
                // Finalize
                if (FinalizePatch(filePath, patchedPath, compressedPath, backup) && CleanUpTempFiles(unpackedPath, patchedPath, compressedPath))
                {
                    Log($"{Path.GetFileName(filePath)} successfully patched", 0, LogType.Ok);
                    return true;
                }
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
        /// Loads the bundle into memory.
        /// </summary>
        private static bool LoadFile(bool unPack, string originalPath, string unpackedPath, ref BundleFileInstance bundle, AssetsManager am, bool logBundleName = true)
        {
            if (!File.Exists(originalPath))
            {
                string relativePath = Path.GetRelativePath(AppDomain.CurrentDomain.BaseDirectory, originalPath);
                Log($"'{relativePath}' doesn't exist. Make sure this tool is in the game's root directory", 1, LogType.Error);
                return false;
            }
            GetTypeTree(am);
            if (logBundleName) Log($"[{Path.GetFileName(originalPath)}]");
            string loadPath = originalPath;
            if (unPack)
            {
                if (UnpackBundle(am, originalPath, unpackedPath))
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
            if (!GetConfSchema(bundle, ref am))
            {
                if (unPack) return false;
                // If this failed it could be because the bundle needs to be unpacked first
                Log("Will retry unpacking the bundle first", 1);
                return LoadFile(true, originalPath,unpackedPath, ref bundle, am, false);
            }
            return true;
        }
        /// <summary>
        /// Unpacks compressed archives to a temporary file.
        /// </summary>
        private static bool UnpackBundle(AssetsManager am, string originalPath, string unpackedPath)
        {
            BundleFileInstance bundleInst = null;
            try
            {
                bundleInst = am.LoadBundleFile(originalPath, false);
                if (bundleInst.file.GetCompressionType() == AssetBundleCompressionType.None)
                {
                    Log($"{Path.GetFileName(originalPath)} is already uncompressed. Skipping extraction", 0, LogType.Skip);
                    return false;
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
                CloseBundle(bundleInst, am);
            }            
        }
        /// <summary>
        /// Unpacks the embedded type database resource required by AssetsTools to serialize and deserialize assets without engine runtimes.
        /// </summary>
        private static void GetTypeTree(AssetsManager am)
        {
            var assembly = typeof(Uncensor).Assembly;
            using var stream = assembly.GetManifestResourceStream("MIUncensor.classdata.tpk");
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
        private static bool GetConfSchema(BundleFileInstance bundle, ref AssetsManager am)
        {
            try
            {
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
        /// Iterates through inner directory info files mapping, verifying, and altering valid shader data trees.
        /// </summary>
        private static bool EditMosaicShader(string searchText, BundleFileInstance bundle, AssetsManager am)
        {
            bool changesMade = false;
            // Outer loop: Iterate through distinct serialized data files inside the bundle
            foreach (var dir in bundle.file.BlockAndDirInfo.DirectoryInfos)
            {
                var inst = am.LoadAssetsFileFromBundle(bundle, dir.Name, false);
                if (inst?.file?.AssetInfos == null)
                    continue;
                //Log($"[{dir.Name}]",1);
                bool fileModified = false;
                // Inner loop: Iterate through individual asset records stored inside the directory file
                foreach (var info in inst.file.AssetInfos)
                {
                    // Filter down explicitly to Shader Assets
                    if ((AssetClassID)info.TypeId != AssetClassID.Shader)
                        continue;
                    try
                    {
                        var baseField = am.GetBaseField(inst, info);
                        // Filter fields containing target search terminology strings
                        if (!ContainsText(baseField, searchText))
                            continue;
                        Log($"\"{searchText}\" found. 'Path_ID = {info.PathId}'", 2);
                        // Modify rendering properties inside type fields
                        int changed = PatchColMask(baseField);
                        if (changed > 0)
                        {
                            info.SetNewData(baseField); 
                            fileModified = true; 
                            Log($"'val = 15' replaced with 'val = 0' in {changed} rows", 2);
                        }
                        else
                        {
                            Log("No 'val = 15' entries found. The file might be patched", 2, LogType.Skip);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Skips to the next asset
                        Log($"Failed to process asset with 'Path_ID = {info.PathId}': {ex.Message}", 2, LogType.Error);
                    }
                }                
                // Write updates to directory block once all mutations on the file scope are fully aggregated
                if (fileModified)
                {
                    try
                    {
                        Log($"Applying modifications to block...", 2);
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
        /// Recursively navigates deserialized layout trees to match string values safely.
        /// </summary>
        private static bool ContainsText(AssetTypeValueField field, string target)
        {
            if (field == null) return false;
            if (field.Value?.AsString != null)
            {
                var str = field.AsString;
                if (!string.IsNullOrEmpty(str) && str.IndexOf(target, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }
            if (field.Children != null)
                foreach (var child in field.Children)
                    if (ContainsText(child, target))
                        return true;
            return false;
        }
        /// <summary>
        /// Recursively discovers target values matching parameters inside structural field instances and updates value parameters.
        /// </summary>
        private static int PatchColMask(AssetTypeValueField field)
        {
            int changed = 0;
            if (field == null) return 0;
            // Target color masks/values explicitly named 'val' evaluating to float values of 15
            if (field.FieldName == "val" && field.Value != null)
                if (field.AsFloat == 15f)
                {
                    field.AsFloat = 0f; // Reset to 0f to strip/disable alpha or mosaic parameters
                    return 1;
                }
            if (field.Children != null)
                foreach (var child in field.Children)
                    changed += PatchColMask(child);
            return changed;
        }
        /// <summary>
        /// Safely releases file handles and resources associated with the bundle and its manager.
        /// </summary>
        private static void CloseBundle(BundleFileInstance bundle, AssetsManager am)
        {
            bundle?.file?.Close();
            bundle?.file?.Reader?.Close();
            am.UnloadAll();
            GC.Collect();
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
                CloseBundle(tempBundle, tempAm);
                return true;
            }
            catch (Exception)
            {
                Log($"Couldn't compress to the bundle", 0, LogType.Warning);
                CloseBundle(tempBundle, tempAm);
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
                Log($"Process Failed! Couldn't overwrite {Path.GetFileName(originalPath)}: {ex.Message}", 0, LogType.Error);
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
                File.Copy(fileName, backupFile, true);
                return true;
            }
            catch (Exception ex)
            {
                Log($"Process Failed! Couldn't create/overwrite {Path.GetFileName(backupFile)}: {ex.Message}", 0, LogType.Error);
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