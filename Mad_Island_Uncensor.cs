using System;
using System.IO;
using System.Linq;
using AssetsTools.NET;
using AssetsTools.NET.Extra;
using System.Diagnostics;

namespace Mad_Island_Uncensor
{
    public class Mad_Island_Uncensor
    {
        private static readonly string NoneBat = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Mad Island_Data\\StreamingAssets\\XML", "none.bat");
        private static readonly string UnityPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Mad Island_Data", "data.unity3d");

        public static void Main(string[] args)
        {
            // Start the stopwatch
            Stopwatch sw = Stopwatch.StartNew();

            // Parse command-line arguments
            bool loadToDisk = args.Contains("-d");      // -d forces unpacking to disk first
            int cIndex = Array.IndexOf(args, "-c");     // -c allows custom compression type configuration
            
            AssetBundleCompressionType compType = AssetBundleCompressionType.LZ4Fast; // Default compression
            
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
                    // If -c is present but has no valid value string following it, default to this
                    compType = AssetBundleCompressionType.None;
                }
            }
            
            // Initialize the main manager for AssetsTools.NET
            var am = new AssetsManager();

            Console.WriteLine("--------------------------------------------------------------");
            // Create an empty none.bat file
            Console.WriteLine($"Creating 'Mad Island_Data\\StreamingAssets\\XML\\none.bat'.");
            if (CreateEmptyNoneBat(NoneBat))
            {            
                Console.WriteLine("--------------------------------------------------------------");
                Console.WriteLine("Starting mosaic removal process.");
                // Execute the main patching workflow on the asset bundle
                PatchUnityBundle("MosaicField", UnityPath, loadToDisk, compType, am);
            }

            // Stop the clock
            sw.Stop();
            Console.WriteLine("--------------------------------------------------------------");
            // Format the output: Total seconds, rounded to two decimal places
            //Console.WriteLine($"Process finished in {sw.Elapsed.TotalSeconds:F2} seconds.");
            //Console.WriteLine("--------------------------------------------------------------");
            Console.WriteLine("Press ENTER to exit.");
            Console.WriteLine("--------------------------------------------------------------");
            Console.ReadLine();
        }

        /// <summary>
        /// Handles loading, targeting, modifying, saving, and re-compressing the specific asset bundle file.
        /// </summary>
        private static void PatchUnityBundle(string mosaicText, string filePath, bool loadToDisk, AssetBundleCompressionType compType, AssetsManager am)
        {
            string unpackedPath = filePath + ".unpacked";
            string patchedPath = filePath + ".patched";
            BundleFileInstance bundle = null;

            // Load the target Unity bundle (either directly to memory or unpacked via temporary disk space)
            if (!LoadFile(loadToDisk, filePath, unpackedPath, ref bundle, am))
                return;
            // Prepare global TypeTree schemas from the embedded package file
            GetTypeTree(am);

            // Scan through assets internally and modify target data fields
            string EditMosaicShaderResponse = EditMosaicShader(mosaicText, bundle, am);
            if (EditMosaicShaderResponse == "ChangesMade")
            {
                // Save Changes
                Console.WriteLine("  Saving changes...");
                using (var stream = File.Create(patchedPath))
                using (var writer = new AssetsFileWriter(stream))
                {
                    bundle.file.Write(writer);
                }
                bundle.file.Close();
                am.UnloadAll();
                
                // Compress
                if (compType != AssetBundleCompressionType.None) Compress(compType, filePath, patchedPath);
                
                // Clean Up
                CleanUpTempFiles(compType, filePath, unpackedPath, patchedPath);                

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"[OK] {Path.GetFileName(filePath)} Successfully patched.");
            } 
            else
            {
                // Clean Up and notifications if the patching wasn't successful
                bundle.file.Close();
                am.UnloadAll();
                if (File.Exists(unpackedPath)) File.Delete(unpackedPath);
                if (EditMosaicShaderResponse == mosaicText)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("[SKIP] No 'val = 15' entries found. The file may already be patched.");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("[ERROR] No \"MosaicField\" found in the file. Mosaic patch not applied.");
                }
            }
            Console.ResetColor();       
        }

        /// <summary>
        /// Unpacks the embedded type database resource required by AssetsTools to serialize and deserialize assets without engine runtimes.
        /// </summary>
        private static void GetTypeTree(AssetsManager am)
        {
            var assembly = typeof(Mad_Island_Uncensor).Assembly;
            using (var stream = assembly.GetManifestResourceStream("Mad_Island_Uncensor.classdata.tpk"))
            {
                if (stream == null)
                {
                    foreach (var name in assembly.GetManifestResourceNames()) Console.WriteLine("Available: " + name);
                    throw new Exception("Could not find embedded 'classdata.tpk'!");
                }
                am.LoadClassPackage(stream);
            }            
        }
        
        /// <summary>
        /// Iterates through inner directory info files mapping, verifying, and altering valid shader data trees.
        /// </summary>
        private static string EditMosaicShader(string searchText, BundleFileInstance bundle, AssetsManager am)
        {
            // Gather structural configuration schemas against the first indexed directory file's layout
            var firstDir = bundle.file.BlockAndDirInfo.DirectoryInfos[0];            
            var assetInst = am.LoadAssetsFileFromBundle(bundle, firstDir.Name, false);
            
            am.LoadClassDatabaseFromPackage(assetInst.file.Metadata.UnityVersion);
            string response = "None";
            
            // Outer loop: Iterate through distinct serialized data files inside the bundle
            foreach (var dir in bundle.file.BlockAndDirInfo.DirectoryInfos)
            {
                var inst = am.LoadAssetsFileFromBundle(bundle, dir.Name, false);
                if (inst?.file?.AssetInfos == null)
                    continue;

                bool fileModified = false;

                // Inner loop: Iterate through individual asset records stored inside the directory file
                foreach (var info in inst.file.AssetInfos)
                {
                    // Filter down explicitly to Shader Assets
                    if ((AssetClassID)info.TypeId != AssetClassID.Shader)
                        continue;

                    var baseField = am.GetBaseField(inst, info);
                    // Filter fields containing target search terminology strings
                    if (!ContainsText(baseField, searchText))
                        continue;
                        
                    Console.WriteLine($"  \"{searchText}\" found in shader with 'Path_ID = {info.PathId}'.");
                    response = searchText;
                    
                    // Modify rendering properties inside type fields
                    int changed = PatchColMask(baseField);
                    if (changed > 0)
                    {
                        info.SetNewData(baseField); 
                        fileModified = true; 
                        Console.WriteLine($"  'val = 15' replaced with 'val = 0' in {changed} rows.");
                        response = "ChangesMade";
                    }
                }
                
                // Write updates to directory block once all mutations on the file scope are fully aggregated
                if (fileModified)
                {                      
                    using var ms = new MemoryStream();
                    using var assetsWriter = new AssetsFileWriter(ms);
                    inst.file.Write(assetsWriter);
                    byte[] patchedBytes = ms.ToArray();
                    dir.SetNewData(patchedBytes);
                }
            }
            return response;
        }

        /// <summary>
        /// Cleans up leftover unpack workspace binaries and updates raw files dynamically based on flags.
        /// </summary>
        private static void CleanUpTempFiles (AssetBundleCompressionType compType, string originalPath, string unpackedPath, string patchedPath)
        {
            Console.WriteLine($"  Cleaning up temporary files...");
            if (File.Exists(unpackedPath)) File.Delete(unpackedPath);
            if (compType != AssetBundleCompressionType.None)
            {
                if (File.Exists(patchedPath)) File.Delete(patchedPath);
            }
            else
                if (File.Exists(patchedPath) && File.Exists(originalPath))
                {
                    File.Delete(originalPath);
                    File.Move(patchedPath, originalPath);
                }
        }

        /// <summary>
        /// Reads back uncompressed patch outputs and recompresses using selected bundle encoders (LZ4/LZ4fast/LZMA).
        /// </summary>
        private static void Compress(AssetBundleCompressionType compType, string originalPath, string patchedPath)
        {
            Console.WriteLine($"  Compressing file using {compType} format... (may take a minute)");
            var tempAm = new AssetsManager();
            var tempBundle = tempAm.LoadBundleFile(patchedPath, false);
            using (var compStream = File.Create(originalPath)) 
            using (var compWriter = new AssetsFileWriter(compStream))
            {
                tempBundle.file.Pack(compWriter, compType);
            }
            tempBundle.file.Close();
            tempAm.UnloadAll();
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
        /// Creates an empty none.bat if it doesn't already exist.
        /// </summary>
        private static bool CreateEmptyNoneBat(string noneBat)
        {
            string directory = Path.GetDirectoryName(noneBat);
            if (!Directory.Exists(directory))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[ERROR] {directory} doesn't exist.\nMake sure this patch file is in the game's root directory.");
                Console.ResetColor();
                return false;
            } 
            if (File.Exists(noneBat))
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[SKIP] {Path.GetFileName(noneBat)} already exists.");
            }
            else
            {
                File.WriteAllBytes(noneBat, Array.Empty<byte>());            
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"[OK] Empty {Path.GetFileName(noneBat)} created.");
            }
            Console.ResetColor();
            return true;
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
        /// Controls standard input execution to feed asset maps through AssetsTools API cleanly.
        /// </summary>
        private static bool LoadFile(bool loadToDisk, string originalPath, string unpackedPath, ref BundleFileInstance bundle, AssetsManager am)
        {
            if (!File.Exists(originalPath))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[ERROR] {Path.GetFileName(originalPath)} doesn't exist. Make sure this patch file is in the game's root directory.");
                Console.ResetColor();
                return false;
            }
            if (loadToDisk) 
            {
                UnpackBundle(am, originalPath, unpackedPath);
                bundle = am.LoadBundleFile(unpackedPath, false);
            }
            else
            {
                Console.WriteLine($"  Loading {Path.GetFileName(originalPath)} into memory...");
                bundle = am.LoadBundleFile(originalPath, false);
            }
            return true;
        }

        /// <summary>
        /// Unpacks compressed archives to a temporary file
        /// </summary>
        private static void UnpackBundle(AssetsManager am, string originalPath, string unpackedPath)
        {
            if (File.Exists(unpackedPath)) 
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"  [SKIP] {unpackedPath} Already exists. Skipping unpacking, using the existing file.");
                Console.ResetColor();
                return;
            }
            var bundleInst = am.LoadBundleFile(originalPath, false);
            if (bundleInst.file.GetCompressionType() == AssetBundleCompressionType.None)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"  [SKIP] {Path.GetFileName(originalPath)} is already uncompressed. Skipping unpacking.");
                Console.ResetColor();
                bundleInst.file.Close();
                File.Copy(originalPath,unpackedPath);
                return;
            }
            Console.WriteLine($"  Unpacking {Path.GetFileName(originalPath)} to disk...");
            using (var writer = new AssetsFileWriter(File.OpenWrite(unpackedPath)))
            {
                bundleInst.file.Unpack(writer);
            }
        }
    }
}