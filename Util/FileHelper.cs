using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Management;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Newtonsoft.Json;
using Vrm.Cfg;
using Vrm.Json;
using Vrm.Vam;

namespace Vrm.Util
{
    public static class FileHelper
    {
        public static string GetFileName_Meta_Var(string fullName, bool isArchive)
        {
            var relPath = FileHelper.NormalizePath(FileHelper.GetRelativePath(Settings.Config, fullName, isArchive));
            var path = PathCombine(Settings.Config.CachePath, ChangeExt(relPath, "meta"));
            return path;
        }

        public static VarFile GetOrAddVar(FileInfo fi, bool isArchive, ConcurrentBag<string> messages)
        {
            var pathMeta = GetFileName_Meta_Var(fi.FullName, isArchive);
            if (File.Exists(pathMeta))
            {
                var dto = JsonModifiedParser.LoadIfModifiedEquals<VarFileDto>(pathMeta, nameof(VarFileDto.Modified), fi.LastWriteTime);
                if (dto == null)
                {
                    messages?.Add($"{fi.Name}: Cache not found or is outdated. A new cache will be created.");
                    return ParseVarAndWriteMeta(fi, isArchive, pathMeta);
                }
                else
                {
                    return VarFile.From(dto, fi, isArchive);
                }
            }
            else
            {
                messages?.Add($"{fi.Name}: Cache not found or is outdated. A new cache will be created.");
                return ParseVarAndWriteMeta(fi, isArchive, pathMeta);
            }
        }

        public static void SetVarPrefs_Slow(VarFile v)
        {
            var p1fn = FileHelper.GetPrefsFileName_NotesPreloadMorphs(v.Name);
            //io
            var p1Json = FileHelper.FileExists(p1fn) ? FileHelper.FileReadAllText(p1fn) : "";

            var p2fn = FileHelper.GetPrefsFileName_EnableDisableIgnore(v.Name);
            //io
            var p2Json = FileHelper.FileExists(p2fn) ? FileHelper.FileReadAllText(p2fn) : "";

            v.IsPreloadMorhpsDisabledByPrefs = FileHelper.GetPreloadMorphs(p1Json) == false;
            v.Info.UserPrefs = p2Json + Environment.NewLine + p1Json;

            foreach (var cat in new[] { FolderType.Scene, FolderType.SubScene, FolderType.Clothing, FolderType.Hair })
                if (v.ElementsDict.TryGetValue(cat, out var entries))
                    FileHelper.SetUserPrefsAndFavHide_InsideVar_Slow(v.Name, entries);
        }

        public static void SetUserPrefsAndFavHide_InsideVar_Slow(VarName name, List<ElementInfo> entries)
        {
            var elGroups = entries.GroupBy(x1 => FileHelper.ChangeExt(x1.FullName, null));
            foreach (var g in elGroups)
            {
                string userPrefs = "";
                string userTags = "";
                var pf = FileHelper.GetPrefsFileName_InsideVar(g.First().FullName);

                //io
                if (FileHelper.FileExists(pf))
                {
                    userPrefs = FileHelper.FileReadAllText(pf);
                    userTags = FileHelper.FindUserTags(userPrefs);
                }

                bool isHide = false;
                bool isFav = false;
                foreach (var item in g)
                {
                    var ext = FileHelper.GetExtension(item.FullName);
                    if (Ext.IsScene(ext))
                    {
                        //io
                        isHide = FileHelper.IsHideOrFav_Scene_InsideVar(item.FullName, name, true);
                        //io
                        isFav = FileHelper.IsHideOrFav_Scene_InsideVar(item.FullName, name, false);
                        break;
                    }
                    else if (Ext.IsHairOrClothing(ext))
                    {
                        //io
                        isHide = FileHelper.IsHide_ClothingOrHair_InsideVar(item.FullName, name);
                        break;
                    }
                }

                foreach (var item in g)
                {
                    item.UserPrefs = userPrefs;
                    item.UserTags = userTags;
                    item.IsHide = isHide;
                    item.IsFav = isFav;
                }
            }
        }

        public static void SetVarPrefs_Fast(VarFile v, HashSet<string> filePaths, Dictionary<string, string> prefsContents)
        {
            var p1fn = FileHelper.GetPrefsFileName_NotesPreloadMorphs(v.Name);
            prefsContents.TryGetValue(p1fn, out var p1Json);

            var p2fn = FileHelper.GetPrefsFileName_EnableDisableIgnore(v.Name);
            prefsContents.TryGetValue(p2fn, out var p2Json);

            v.IsPreloadMorhpsDisabledByPrefs = FileHelper.GetPreloadMorphs(p1Json) == false;
            v.Info.UserPrefs = p2Json + Environment.NewLine + p1Json;

            foreach (var cat in new[] { FolderType.Scene, FolderType.SubScene, FolderType.Clothing, FolderType.Hair })
                if (v.ElementsDict.TryGetValue(cat, out var entries))
                    FileHelper.SetUserPrefsAndFavHide_InsideVar_Fast(v.Name, entries, filePaths, prefsContents);
        }

        public static void SetUserPrefsAndFavHide_InsideVar_Fast(VarName name, List<ElementInfo> entries, HashSet<string> filePaths, Dictionary<string, string> prefsContents)
        {
            var elGroups = entries.GroupBy(x1 => FileHelper.ChangeExt(x1.FullName, null));
            foreach (var g in elGroups)
            {
                string userPrefs = "";
                string userTags = "";

                var pf = FileHelper.GetPrefsFileName_InsideVar(g.First().FullName);
                if(prefsContents.TryGetValue(pf, out var content))
                {
                    userPrefs = content;
                    userTags = FileHelper.FindUserTags(userPrefs);
                }

                bool isHide = false;
                bool isFav = false;
                foreach (var item in g)
                {
                    var ext = FileHelper.GetExtension(item.FullName);
                    if (Ext.IsScene(ext))
                    {
                        var hidePath = GetFileName_HideOrFav_Scene_InsideVar(item.FullName, name, true);
                        hidePath = FileHelper.NormalizePath(hidePath, false);
                        isHide = filePaths.Contains(hidePath);

                        var favPath = GetFileName_HideOrFav_Scene_InsideVar(item.FullName, name, false);
                        favPath = FileHelper.NormalizePath(favPath, false);
                        isFav = filePaths.Contains(favPath);

                        break;
                    }
                    else if (Ext.IsHairOrClothing(ext))
                    {
                        var hidePath = GetFileName_Hide_ClothingOrHair_InsideVar(item.FullName, name);
                        hidePath = FileHelper.NormalizePath(hidePath, false);
                        isHide = filePaths.Contains(hidePath);

                        break;
                    }
                }

                foreach (var item in g)
                {
                    item.UserPrefs = userPrefs;
                    item.UserTags = userTags;
                    item.IsHide = isHide;
                    item.IsFav = isFav;
                }
            }
        }

        public static (HashSet<string> filePaths, Dictionary<string, string> prefsContents) ScanFolders(IEnumerable<string> folders)
        {
            var extensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".hide", ".fav", ".prefs" };
            var allFiles = new ConcurrentBag<string>();

            Parallel.ForEach(folders, folder =>
            {
                try
                {
                    var files = Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories)
                        .Where(path => extensions.Contains(Path.GetExtension(path)));
                    foreach (var file in files)
                        allFiles.Add(file);
                }
                catch
                {
                    /**/
                }
            });

            var filePaths = new ConcurrentDictionary<string, byte>(); // thread-safe HashSet substitute
            var prefsFiles = new ConcurrentDictionary<string, string>();

            Parallel.ForEach(
                Partitioner.Create(allFiles.ToList(), true),
                new ParallelOptions { MaxDegreeOfParallelism = 6 },
                path =>
                {
                    try
                    {
                        if (path.EndsWith(".prefs", StringComparison.OrdinalIgnoreCase))
                        {
                            prefsFiles[path] = FileReadAllText(path);
                        }
                        else
                        {
                            filePaths.TryAdd(path, 0);
                        }
                    }
                    catch
                    {
                        /**/
                    }
                });

            return (new HashSet<string>(filePaths.Keys), new Dictionary<string, string>(prefsFiles));
        }

        public static (HashSet<string> filePaths, Dictionary<string, string> prefsContents) ScanFoldersSimplePartitioner(IEnumerable<string> folders)
        {
            var extensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".hide", ".fav", ".prefs" };
            var allFiles = new ConcurrentBag<string>();

            //1. prepare files
            Parallel.ForEach(folders, folder =>
            {
                try
                {
                    var files = Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories)
                        .Where(path => extensions.Contains(Path.GetExtension(path)));
                    foreach (var file in files)
                        allFiles.Add(file);
                }
                catch
                {
                    /**/
                }
            });

            var filePaths = new ConcurrentDictionary<string, byte>(); // thread-safe HashSet substitute
            var prefsFiles = new ConcurrentDictionary<string, string>();

            // 2. balancing
            var fileList = allFiles.ToArray();
            int processorCount = 6;
            int chunkSize = (fileList.Length + processorCount - 1) / processorCount;

            //3. calc parallel
            Parallel.For(0, processorCount, i =>
            {
                int start = i * chunkSize;
                int end = Math.Min(start + chunkSize, fileList.Length);

                for (int j = start; j < end; j++)
                {
                    var path = fileList[j];
                    try
                    {
                        if (path.EndsWith(".prefs", StringComparison.OrdinalIgnoreCase))
                        {
                            prefsFiles[path] = FileReadAllText(path);
                        }
                        else
                        {
                            filePaths.TryAdd(path, 0);
                        }
                    }
                    catch
                    {
                        /**/
                    }
                }
            });

            return (new HashSet<string>(filePaths.Keys), new Dictionary<string, string>(prefsFiles));
        }

        private static VarFile ParseVarAndWriteMeta(FileInfo fi, bool isArchive, string metaPath)
        {
            EnsureDirectoryStructureExists(metaPath);
            var obj = ParseVar(fi, isArchive);
            var dto = VarFileDto.From(obj);
            File.WriteAllText(metaPath, JsonConvert.SerializeObject(dto, Formatting.Indented));
            return obj;
        }

        public static string GetFileName_Meta_UserItem(string relativePath)
        {
            var path = PathCombine(Settings.Config.CachePath, ChangeExt(relativePath, "meta"));
            return path;
        }

        public static UserItem GetOrAddUserItem(ElementInfo ei, bool isArchive, List<FileInfo> files, ConcurrentBag<string> messages)
        {
            var pathMeta = GetFileName_Meta_UserItem(ei.RelativePath);
            var (main, canContainDependencies) = UserItem.SelectMain(files);
            Func<UserItem> f = ()=> new UserItem(ei, isArchive, files, main, canContainDependencies);

            if (!canContainDependencies)
                return f();

            if (File.Exists(pathMeta))
            {
                var dto = JsonModifiedParser.LoadIfModifiedEquals<UserItemDto>(pathMeta, nameof(UserItemDto.Modified), ei.LastWriteTime);
                if (dto == null)
                {
                    messages?.Add($"{ei.Name}: Cache not found or is outdated. A new cache will be created.");
                    return f(); //write later, after load all dictionaries
                }
                else
                {
                    return new UserItem(dto, ei, isArchive, files, main, canContainDependencies) { IsDependenciesResolved = true };
                }
            }
            else
            {
                messages?.Add($"{ei.Name}: Cache not found or is outdated. A new cache will be created.");
                return f(); //write later, after load all dictionaries
            }
        }

        #region images

        public static string GetOrAddImage(VarFile v, ZipArchiveEntry e)
        {
            var path = Path.Combine(Settings.Config.CachePath, v.Name.RawName, e.FullName);
            if (!File.Exists(path))
            {
                EnsureDirectoryStructureExists(path);
                SaveZipEntryToFile(e, path);
            }
            return path;
        }

        public static (int width, int height)? GetJpegDimensionsFromEntry(ZipArchiveEntry entry)
        {
            using (Stream zipStream = entry.Open())
            {
                using (BinaryReader reader = new BinaryReader(zipStream))
                {
                    if (reader.ReadByte() != 0xFF || reader.ReadByte() != 0xD8)
                        return null; // Not a JPEG

                    while (true)
                    {
                        // Find marker
                        byte prefix;
                        do
                        {
                            int next = reader.ReadByte();
                            if (next == -1)
                                return null;
                            prefix = (byte)next;
                        } while (prefix != 0xFF);

                        byte marker = reader.ReadByte();
                        if (marker == 0xD9 || marker == 0xDA)
                            break;

                        ushort segmentLength = ReadBigEndianUInt16(reader);
                        if (segmentLength < 2)
                            return null;

                        if (marker == 0xC0 || marker == 0xC2)
                        {
                            reader.ReadByte(); // sample precision
                            ushort height = ReadBigEndianUInt16(reader);
                            ushort width = ReadBigEndianUInt16(reader);
                            return (width, height);
                        }

                        // Skipping the remaining bytes in the segment
                        int toSkip = segmentLength - 2;
                        if (reader.BaseStream.Read(new byte[toSkip], 0, toSkip) != toSkip)
                            return null;
                    }
                }
            }

            return null;
        }

        private static ushort ReadBigEndianUInt16(BinaryReader reader)
        {
            byte hi = reader.ReadByte();
            byte lo = reader.ReadByte();
            return (ushort)((hi << 8) + lo);
        }

        private static MemoryStream CopyStreamToMemory(Stream input)
        {
            var mem = new MemoryStream();
            input.CopyTo(mem);
            mem.Position = 0;
            return mem;
        }

        private static BitmapImage CreateImage(Stream entryStream)
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = CopyStreamToMemory(entryStream); // should be copied in .net 4.8
            bitmap.EndInit();
            bitmap.Freeze(); // enable UI-thread
            return bitmap;
        }

        public static BitmapImage CreateImage(byte[] data, int decodePixelWidth)
        {
            var bitmap = new BitmapImage();
            using (var ms = new MemoryStream(data))
            {
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.StreamSource = ms;
                bitmap.DecodePixelWidth = decodePixelWidth;
                bitmap.EndInit();
                bitmap.Freeze();
            }
            return bitmap;
        }

        public static BitmapImage CreateImage(string embeddedResourcePath)
        {
            var assembly = Assembly.GetExecutingAssembly();

            string resourceName = Array.Find(
                assembly.GetManifestResourceNames(),
                name => name.EndsWith(embeddedResourcePath, StringComparison.OrdinalIgnoreCase)
            );

            var image = new BitmapImage();
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
            {
                image.BeginInit();
                image.StreamSource = stream;
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.EndInit();
                image.Freeze();
            }
            return image;
        }

        #endregion

        #region process

        public static string CreateTempFile(IEnumerable<string> lines)
        {
            string tempFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            tempFile = ChangeExt(tempFile, "txt");
            File.WriteAllText(tempFile, string.Join(Environment.NewLine, lines));
            return tempFile;
        }

        public static void ShowInExplorer(string path)
        {
            if (System.IO.File.Exists(path))
            {
                Process.Start("explorer.exe", $"/select,\"{path}\"");
            }
            else if (Directory.Exists(path))
            {
                Process.Start("explorer.exe", $"\"{path}\"");
            }
        }

        public static Process Run(string path)
        {
            if (File.Exists(path))
            {
                return Process.Start(path);
            }

            return null;
        }

        public static Process FindProcess(string basePath, string processName = "vam")
        {
            Process[] processes = Process.GetProcessesByName(processName);
            foreach (var p in processes)
            {
                var path = GetExecutablePath(p.Id);
                if(path != null && path.Contains(basePath))
                    return p;
            }

            return null;
        }

        public static void CloseOrKill(Process p, int timeoutMs = 3000)
        {
            try
            {
                if (p == null)
                    return;
                if (p.MainWindowHandle != IntPtr.Zero)
                {
                    p.CloseMainWindow();
                    if (!p.WaitForExit(timeoutMs))
                    {
                        p.Kill();
                    }
                }
                else
                {
                    p.Kill();
                }
            }
            catch
            {
                /**/
            }
            finally
            {
            }
        }

        public static string GetExecutablePath(int processId)
        {
            try
            {
                string query = $"SELECT ExecutablePath FROM Win32_Process WHERE ProcessId = {processId}";
                using (var searcher = new ManagementObjectSearcher(query))
                {
                    foreach (var obj in searcher.Get())
                    {
                        return obj["ExecutablePath"]?.ToString();
                    }
                }
            }
            catch { }
            return null;
        }

        #endregion

        #region prefs & fav & hide

        private static readonly Regex _userTagsRegex = new Regex(@"""userTags""\s*:\s*""([^""]*)""", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static string FindUserTags(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return null;
            var match = _userTagsRegex.Match(input);
            if (match.Success)
            {
                string value = match.Groups[1].Value;
                return value;
            }

            return null;
        }

        public static string GetPrefsFileName_EnableDisableIgnore(VarName v)
        {
            return Path.Combine(Settings.Config.VamPath, @"AddonPackagesUserPrefs\", v.FullName + Ext.Prefs);
        }

        public static string GetPrefsFileName_NotesPreloadMorphs(VarName v)
        {
            return Path.Combine(Settings.Config.VamPath, @"AddonPackagesUserPrefs\", v.CreatorAndName + Ext.Prefs);
        }

        public static string GetPrefsFileName_UserResource(string fullPath)
        {
            var path = ChangeExt(fullPath, Ext.Prefs);
            return Path.Combine(Settings.Config.VamPath, path);
        }

        public static string GetPrefsFileName_InsideVar(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
                return "";
            var path = ChangeExt(relativePath, Ext.Prefs);
            return Path.Combine(Settings.Config.VamPath, path);
        }

        private static string GetFileName_HideOrFav_UserResource(string fullPath, bool hideOrFav)
        {
            var ext = hideOrFav ? Ext.Hide : Ext.Favorite;
            var path = fullPath + ext;
            return path;
        }
        public static bool IsHideOrFav_UserResource(string fullPath, bool hideOrFav)
        {
            var path = GetFileName_HideOrFav_UserResource(fullPath, hideOrFav);
            return FileExists(path);
        }
        public static void SetHideOrFav_UserResource(string fullPath, bool hideOrFav, bool setValue)
        {
            var fp = GetFileName_HideOrFav_UserResource(fullPath, hideOrFav);
            if(setValue)
                File.Create(fp).Dispose();
            else
                File.Delete(fp);
        }

        private static string GetFileName_Hide_ClothingOrHair_InsideVar(string resourceRelativePath, VarName n)
        {
            var path = ChangeExt(resourceRelativePath, null) + Ext.DotVamDotHide;
            var fp = Path.Combine(Settings.Config.VamPath, @"AddonPackagesFilePrefs\", n.RawName, path);
            return fp;
        }
        public static bool IsHide_ClothingOrHair_InsideVar(string resourceRelativePath, VarName n)
        {
            var fp = GetFileName_Hide_ClothingOrHair_InsideVar(resourceRelativePath, n);
            return FileExists(fp);
        }
        public static void SetHide_ClothingOrHair_InsideVar(string resourceRelativePath, VarName n, bool setValue)
        {
            var fp = GetFileName_Hide_ClothingOrHair_InsideVar(resourceRelativePath, n);
            if (setValue)
            {
                EnsureDirectoryStructureExists(fp);
                File.Create(fp).Dispose();
            }
            else
                File.Delete(fp);
        }

        private static string GetFileName_HideOrFav_Scene_InsideVar(string relPath, VarName n, bool hideOrFav)
        {
            var path = relPath + (hideOrFav ? Ext.Hide : Ext.Favorite);
            var fp = Path.Combine(Settings.Config.VamPath, @"AddonPackagesFilePrefs\", n.RawName, path);
            return fp;
        }
        public static bool IsHideOrFav_Scene_InsideVar(string relPath, VarName n, bool hideOrFav)
        {
            var fp = GetFileName_HideOrFav_Scene_InsideVar(relPath, n, hideOrFav);
            return FileExists(fp);
        }
        public static void SetHideOrFav_Scene_InsideVar(string relPath, VarName n, bool hideOrFav, bool setValue)
        {
            var fp = GetFileName_HideOrFav_Scene_InsideVar(relPath, n, hideOrFav);
            if (setValue)
            {
                EnsureDirectoryStructureExists(fp);
                File.Create(fp).Dispose();
            }
            else
                File.Delete(fp);
        }

        public static void SetTag(string fn1, string tag)
        {
            var json1 = FileHelper.FileExists(fn1) ? FileHelper.FileReadAllText(fn1) : "";
            var editor = new JsonPrefsEditor(json1);
            editor.SetString(JsonPrefsEditor.str_userTags, tag);
            string updatedJson1 = editor.GetEditedJson();
            EnsureDirectoryStructureExists(fn1);
            FileHelper.WriteAllText(fn1, updatedJson1);
        }

        public static void SetFavHide(string fullName, ElementInfo ei)
        {
            ei.IsHide = FileHelper.IsHideOrFav_UserResource(fullName, true);
            ei.IsFav = FileHelper.IsHideOrFav_UserResource(fullName, false);
        }

        public static void UpdateTagInElementInfo(string fullPath, ElementInfo ei)
        {
            var fp = GetPrefsFileName_UserResource(fullPath);
            if (FileExists(fp))
            {
                var prefs = FileHelper.FileReadAllText(fp);
                var tag = FindUserTags(prefs);
                ei.UserPrefs = prefs;
                ei.UserTags = tag;
            }
            else
            {
                ei.UserPrefs = null;
                ei.UserTags = null;
            }
        }

        #endregion

        #region File & Directory & Path

        public static void MoveToOldAddonPackages(string filePath, bool isArchive)
        {
            string addonPackagesRoot = Path.Combine(isArchive ? Settings.Config.VamArchivePath : Settings.Config.VamPath  , "AddonPackages");
            string oldAddonPackagesRoot = Path.Combine(Settings.Config.VamPath, "Old.AddonPackages");

            string fullAddonRoot = Path.GetFullPath(addonPackagesRoot);
            string fullFilePath = Path.GetFullPath(filePath);

            if (!fullFilePath.StartsWith(fullAddonRoot, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("File not found");

            string relativePath = fullFilePath.Substring(fullAddonRoot.Length).TrimStart(Path.DirectorySeparatorChar);
            string newFullPath = Path.Combine(oldAddonPackagesRoot, relativePath);
            string newDir = Path.GetDirectoryName(newFullPath);
            if (!Directory.Exists(newDir))
                Directory.CreateDirectory(newDir);

            if(File.Exists(newFullPath))
                FileDelete(fullFilePath);
            else
                File.Move(fullFilePath, newFullPath);
        }

        public static void EnsureDirectoryStructureExists(string fullFilePath)
        {
            if (string.IsNullOrWhiteSpace(fullFilePath))
                throw new ArgumentNullException(nameof(fullFilePath));

            string directoryPath = Path.GetDirectoryName(Path.GetFullPath(fullFilePath));

            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
        }

        public static bool IsNoImagePath(string path)
        {
            return Settings.NoImagePath.IndexOf(path, StringComparison.OrdinalIgnoreCase) == 0;
        }

        public static List<string> FindOtherExts(string filePath)
        {
            var res = new List<string>();
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException(nameof(filePath));

            if (!File.Exists(filePath))
                return res;

            string directory = Path.GetDirectoryName(filePath);
            string nameWithoutExt = Path.GetFileNameWithoutExtension(filePath);

            foreach (var path in Directory.GetFiles(directory))
            {
                if (path.Equals(filePath, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (Path.GetFileNameWithoutExtension(path).Equals(nameWithoutExt, StringComparison.OrdinalIgnoreCase))
                {
                    res.Add(Path.GetExtension(path));
                }
            }
            return res;
        }

        private static string AppendDirectorySeparatorChar(string path)
        {
            if (!path.EndsWith(Path.DirectorySeparatorChar.ToString()))
                return path + Path.DirectorySeparatorChar;
            return path;
        }

        private static string RemoveDirectorySeparatorCharFromBegin(string path)
        {
            return path.TrimStart(Path.DirectorySeparatorChar);
        }

        public static string RemoveFirstFolder(string path, string folderToRemove)
        {
            if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(folderToRemove))
                return path;

            path = NormalizePath(path);
            folderToRemove = folderToRemove.TrimEnd(Path.DirectorySeparatorChar);
            string prefix = folderToRemove + Path.DirectorySeparatorChar;
            if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return path.Substring(prefix.Length);

            return path;
        }

        public static string ChangeExt(string path, string newExtension)
        {
            return Path.ChangeExtension(path, newExtension);
        }

        public static string NormalizePath(string path, bool removeExt = false)
        {
            if (string.IsNullOrWhiteSpace(path))
                return string.Empty;

            string normalized = path.Replace('/', '\\');
            normalized = normalized.TrimEnd('\\');

            if (removeExt)
                normalized = Path.ChangeExtension(path, null);

            return normalized;
        }

        public static IEnumerable<string> NormalizePaths(IEnumerable<string> items)
        {
            foreach (var item in items)
                yield return NormalizePath(item);
        }

        public static bool ArePathsEqual(string path1, string path2, bool ignoreExtensions = false)
        {
            string p1 = NormalizePath(path1);
            string p2 = NormalizePath(path2);

            if (ignoreExtensions)
            {
                p1 = ChangeExt(p1, null);
                p2 = ChangeExt(p2, null);
            }

            return string.Equals(p1, p2, StringComparison.OrdinalIgnoreCase);
        }

        public static bool ClearDirectory(string path, HashSet<string> excludes = null, bool setNormal = false)
        {
            if (!Directory.Exists(path))
                return false;

            foreach (string file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
            {
                if (excludes != null && excludes.Contains(file))
                    continue;

                if(setNormal)
                    File.SetAttributes(file, FileAttributes.Normal);

                File.Delete(file);
            }

            foreach (string dir in Directory.GetDirectories(path, "*", SearchOption.AllDirectories))
            {
                bool hasFiles = Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories).Any();
                if (!hasFiles)
                {
                    Directory.Delete(dir, true);
                }
            }

            return true;
        }

        public static bool DeleteDirectory(string path)
        {
            if (!Directory.Exists(path))
                return false;

            
            Directory.Delete(path, recursive: true);
            return true;
        }

        public static bool DirectoryExists(string path)
        {
            return Directory.Exists(path);
        }

        public static string PathCombine(params string[] paths)
        {
            return Path.Combine(paths);
        }

        public static string GetExtension(string path)
        {
            return Path.GetExtension(path);
        }

        public static string GetDir(string path)
        {
            return Path.GetDirectoryName(path);
        }

        public static string GetOnlyFileName(string path, bool woExt)
        {
            if(woExt)
                return Path.GetFileNameWithoutExtension(path);
            else
                return Path.GetFileName(path);
        }

        public static bool FileExists(string path)
        {
            return File.Exists(path);
        }

        public static string FileReadAllText(string path)
        {
            return File.ReadAllText(path);
        }

        public static void FileDelete(string path)
        {
            File.Delete(path);
        }

        public static string PathChangeExtension(string path, string extension)
        {
            return Path.ChangeExtension(path, extension);
        }

        public static bool PathHasExtension(string path)
        {
            return Path.HasExtension(path);
        }

        public static string GetOnlyFileNameWithoutExtension(string path)
        {
            return Path.GetFileNameWithoutExtension(path);
        }

        //fast
        public static IEnumerable<string> FileReadLines2(string path)
        {
            return File.ReadAllLines(path);
        }

        public static async Task<byte[]> ReadAllBytesAsync(string path)
        {
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true))
            {
                var data = new byte[fs.Length];
                int bytesRead = 0;
                while (bytesRead < data.Length)
                {
                    int read = await fs.ReadAsync(data, bytesRead, data.Length - bytesRead).ConfigureAwait(false);
                    if (read == 0)
                        break; //End of file (just in case)
                    bytesRead += read;
                }
                return data;
            }
        }

        public static byte[] ReadAllBytes(string path)
        {
            return File.ReadAllBytes(path);
        }

        public static void WriteAllText(string path, string contents)
        {
            File.WriteAllText(path, contents);
        }

        public static void WriteAllLines(string path, string[] lines)
        {
            File.WriteAllLines(path, lines);
        }

        public static void CreateDirectoryInNotExists(string dir)
        {
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }

        public static void CreateDirectoryOfFileInNotExists(string file)
        {
            var dir = Path.GetDirectoryName(file);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }

        public static bool ContainsAnyFolder(string fullPath, IEnumerable<string> folderNames)
        {
            var directoryPath = Path.GetDirectoryName(fullPath);
            if (directoryPath == null)
                return false;

            var folders = directoryPath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return folders.Any(f => folderNames.Any(name => string.Equals(f, name, StringComparison.OrdinalIgnoreCase)));
        }

        #endregion

        #region utility

        public static void SaveZipEntryToFile(ZipArchiveEntry entry, string outputFilePath)
        {
            using (var entryStream = entry.Open())
            {
                using (var fileStream = new FileStream(outputFilePath, FileMode.Create))
                {
                    entryStream.CopyTo(fileStream);
                }
            }
        }

        public static bool TryMoveFile(string sourcePath, string destPath, out string errorMessage)
        {
            errorMessage = null;

            try
            {
                // Check for null or empty paths
                if (string.IsNullOrWhiteSpace(sourcePath))
                {
                    errorMessage = "Source path is not specified.";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(destPath))
                {
                    errorMessage = "Destination path is not specified.";
                    return false;
                }

                // Check if the source file exists
                if (!File.Exists(sourcePath))
                {
                    errorMessage = $"Source file not found: {sourcePath}";
                    return false;
                }

                // If the destination file already exists, just delete the source file
                if (File.Exists(destPath))
                {
                    File.Delete(sourcePath);
                    return true;
                }

                var targetDir = Path.GetDirectoryName(destPath);
                if (!Directory.Exists(targetDir))
                    Directory.CreateDirectory(targetDir);

                // Move the file
                File.Move(sourcePath, destPath);
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = $"Error while moving {sourcePath}->{destPath}: {ex.Message}";
                return false;
            }
        }

        public static void ProcessParallel(string dir, string fileExt, CancellationToken ct, Action<FileInfo> processor, ConcurrentBag<string> errors, int? processorCount = null)
        {
            if (processorCount == null)
                processorCount = Environment.ProcessorCount;
            var options = new ParallelOptions { MaxDegreeOfParallelism = processorCount.Value }; //1
            Parallel.ForEach(new DirectoryInfo(dir).GetFiles(fileExt, SearchOption.AllDirectories),
                options, x =>
                {
                    try
                    {
                        ct.ThrowIfCancellationRequested();
                        processor(x);
                        ct.ThrowIfCancellationRequested();
                    }
                    catch (OperationCanceledException) { /**/ }
                    catch (Exception ex)
                    {
                        errors.Add($@"Error on {x}: {ex.Message}");
                    }
                });
        }

        public static List<T> LoadItems<T>(string dir, string searchPattern, Predicate<string> predicate)
        {
            var res = new List<T>();
            if (Directory.Exists(dir))
            {
                foreach (var item in Directory.GetFiles(dir, searchPattern, SearchOption.AllDirectories))
                {
                    if (predicate != null && !predicate(item))
                        continue;
                    try
                    {
                        var r = JsonConvert.DeserializeObject<T>(File.ReadAllText(item));
                        res.Add(r);
                    }
                    catch (Exception ex)
                    {
                        Settings.Logger.LogErr($"{item} : {ex.Message}");
                    }
                }
            }
            return res;
        }

        #endregion

        #region vam

        //"urlValue" : "DJSoapyKnuckles.flogger-whip.1:/Custom/Assets/bull_whip/flogger-whip.assetbundle"
        //"name" : "A_assetUrl:DJSoapyKnuckles.flogger-whip.1:/Custom/Assets/bull_whip/flogger-whip.assetbundle", 
        //TODO add case? "shaderPropertiesFile" : "AddonPackages\\Regguise.Shader_Stocking.3.var:\\Custom\\Assets\\Guise/Stocking.json" 
        private static readonly Regex _pattern = new Regex(@"""\s*[^""]+""\s*:\s*""(?<creator>[^.]+)\.(?<name>[^.]+)\.(?<version>[a-zA-Z0-9]+):/.*", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public static bool TryFindVarName(string line, out VarName name)
        {
            name = null;
            if (line.IndexOf(':') == -1)
                return false;
            if (line.IndexOf('.') == -1)
                return false;
            var match = _pattern.Match(line);
            if (match.Success)
            {
                var creator = match.Groups["creator"].Value;
                int index = creator.IndexOf(":", StringComparison.Ordinal);
                if (index > 0)
                {
                    creator = creator.Substring(index + 1);
                }

                if (VarName.ParseVersion(match.Groups["version"].Value, out var isLatest, out var version))
                {
                    name = new VarName
                    {
                        Creator = creator,
                        Name = match.Groups["name"].Value,
                        IsLatest = isLatest,
                        Version = version
                    };
                    name.RawName = name.FullName;
                    return true;
                }
                else
                {
                    Settings.Logger.LogErr("Can not parse version: " + line);
                }
            }
            return false;
        }

        public static IEnumerable<VarName> FindVarNames(string filePath)
        {
            using (var reader = new StreamReader(filePath))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (TryFindVarName(line, out var name))
                        yield return name;
                }
            }
        }

        //"id" : "Custom/Clothing/Female/_created/YameteOuji_Under_ThighHighB/Under_ThighHighB_m2.vam",   //user created clothing
        //"url" : "Saves/music/M.mp3"
        //private static readonly Regex _pattern = new Regex(@"""id""\s*:\s*""(?<path>Custom/[^""]+)""", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly Regex _patternRes = new Regex(@"""(id|url)""\s*:\s*""(?<path>(Custom|Saves)/[^""]+)""", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary>
        /// Looks for custom items that cannot be found in a var
        /// </summary>
        /// <returns>resource path</returns>
        public static IEnumerable<string> FindUserResources(string filePath)
        {
            using (var reader = new StreamReader(filePath))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.IndexOf(':') < 0)
                        continue;
                    if (line.IndexOf("id", StringComparison.OrdinalIgnoreCase) < 0 &&
                        line.IndexOf("url", StringComparison.OrdinalIgnoreCase) < 0)
                        continue;

                    var match = _patternRes.Match(line);
                    if (match.Success)
                    {
                        var path = match.Groups["path"].Value;
                        yield return path.Trim();
                    }
                }
            }
        }

        public static ILookup<string, string> GroupFiles(IEnumerable<(string, SearchOpt)> directoryPaths)
        {
            if (directoryPaths == null)
                throw new ArgumentNullException(nameof(directoryPaths));

            var groups = new ConcurrentDictionary<string, ConcurrentBag<string>>();

            Parallel.ForEach(directoryPaths, x =>
            {
                var directoryPath = x.Item1;
                var searchOpt = x.Item2;
                if (searchOpt == SearchOpt.Unset || searchOpt == SearchOpt.Ignore)
                    return;

                SearchOption? searchOption = null;
                if (searchOpt == SearchOpt.All)
                    searchOption = SearchOption.AllDirectories;
                else if(searchOpt == SearchOpt.FirstLevel)
                    searchOption = SearchOption.TopDirectoryOnly;
                if (searchOption == null)
                    return;

                if (!Directory.Exists(directoryPath))
                    return;

                Parallel.ForEach(Directory.EnumerateFiles(directoryPath, "*.*", searchOption.Value),
                    filePath =>
                    {
                        var path = filePath;
                        if(filePath.EndsWith(Ext.Hide, StringComparison.OrdinalIgnoreCase))
                            path = filePath.Substring(0, filePath.Length - Ext.Hide.Length);
                        else if(filePath.EndsWith(Ext.Favorite, StringComparison.OrdinalIgnoreCase))
                            path = filePath.Substring(0, filePath.Length - Ext.Favorite.Length);
                        path = PathChangeExtension(path, null);
                        groups.GetOrAdd(path, _ => new ConcurrentBag<string>()).Add(filePath);
                    });
            });

            return groups
                .SelectMany(kv => kv.Value.Select(file => new { Key = kv.Key, Value = file }))
                .ToLookup(x => x.Key, x => x.Value);
        }


        public static HashSet<VarName> GetArray(List<string> lines)
        {
            var vars = new HashSet<VarName>(VarName.Eq);
            foreach (var str in lines)
            {
                foreach (var line in str.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (TryFindVarName(line, out var name))
                        vars.Add(name);
                }
            }

            return vars;
        }

        #region ops

        public static Task<int> GetImageCount()
        {
            return Task.Run(() => Directory.GetFiles(Settings.Config.CachePath, "*.jpg", SearchOption.AllDirectories).Length);
        }

        public static string GetRelativePath(Config cfg, string fullPath, bool isInArchive)
        {
            var basePath = isInArchive ? cfg.VamArchivePath : cfg.VamPath;
            return GetRelativePath(basePath, fullPath);
        }

        public static string GetRelativePath(string basePath, string fullPath)
        {
            basePath = Path.GetFullPath(AppendDirectorySeparatorChar(basePath));
            fullPath = Path.GetFullPath(fullPath);

            if (!fullPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
                return fullPath; //Outside the base directory — return the absolute path

            return fullPath.Substring(basePath.Length);
        }

        public static string GetFullPath(string relativePath, bool isInArchive, Config cfg, bool removeFirstFolderSeparator = false)
        {
            if (removeFirstFolderSeparator)
                relativePath = RemoveDirectorySeparatorCharFromBegin(relativePath);
            if (isInArchive)
                return Path.Combine(cfg.VamArchivePath, relativePath);
            else
                return Path.Combine(cfg.VamPath, relativePath);
        }

        #endregion

        #endregion

        public static void ProcessZip(string filename, Func<List<string>, IGrouping<string, ZipArchiveEntry>, bool> entryAction)
        {
            using (FileStream zipStream = File.OpenRead(filename))
            {
                using (ZipArchive archive = new ZipArchive(zipStream, ZipArchiveMode.Read))
                {
                    var allEntries = archive.Entries.Select(x => x.FullName).ToList();
                    var elGroupsDict = archive.Entries.GroupBy(x1 => FileHelper.ChangeExt(x1.FullName, null))
                        .ToDictionary(x => x.Key);
                    foreach (var item in elGroupsDict.Values)
                    {
                        if(!entryAction(allEntries, item))
                            break;
                    }
                }
            }
        }

        public static string ProcessVaj(string vajContent, bool isPreset, VarName name, string newCreator, string dir, string vabName, string presetName)
        {
            var json = vajContent;
            if(isPreset)
                json = json.Replace(@"SELF:", name.FullName + ":");

            string pattern = "\"id\"\\s*:\\s*\"[^\"]+\"";
            string patternDot = @"^(\s*""customTexture_[^""]*""\s*:\s*"")\./";
            string replacementPrefix = $"$1{name.FullName}:/{dir}/";

            var lines = json.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            for (int i = 0; i < lines.Length; i++)
            {
                if (Regex.IsMatch(lines[i], pattern))
                {
                    lines[i] = lines[i].Replace(name.Creator, newCreator);
                    if (isPreset)
                    {
                        //lines[i] = lines[i].Replace(vabName, presetName);
                        lines[i] = ReplaceFirst(lines[i], vabName, presetName);
                    }
                }

                if(!isPreset && Regex.IsMatch(lines[i], patternDot))
                {
                    lines[i] = Regex.Replace(lines[i], patternDot, replacementPrefix).Replace('\\', '/');;
                }
            }

            json = string.Join(Environment.NewLine, lines);
            return json;
        }

        private static string ReplaceFirst(string source, string oldValue, string newValue)
        {
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(oldValue))
                return source;

            int index = source.IndexOf(oldValue, StringComparison.Ordinal);
            if (index < 0)
                return source;

            return source.Substring(0, index) + newValue + source.Substring(index + oldValue.Length);
        }


        #region var

        public static ElementInfo CreateVarInfo(FileInfo x, bool isArchive, FolderType type)
        {
            return new ElementInfo(x.FullName, null, x.Name,
                FileHelper.NormalizePath(FileHelper.GetRelativePath(Settings.Config, x.FullName, isArchive)),
                x.LastWriteTime, x.CreationTime, x.Length, type);
        }

        public static VarFile ParseVar(FileInfo fi, bool isArchive)
        {
            var var = new VarFile();
            var.IsInArchive = isArchive;
            var.Info = CreateVarInfo(fi, isArchive, FolderType.Unset);
            var.Name = VarName.Parse(Path.GetFileNameWithoutExtension(fi.Name));

            using (FileStream zipStream = File.OpenRead(fi.FullName))
            {
                using (ZipArchive archive = new ZipArchive(zipStream, ZipArchiveMode.Read))
                {
                    var elGroupsDict = archive.Entries.GroupBy(x1 => FileHelper.ChangeExt(x1.FullName, null)).ToDictionary(x=>x.Key);
                    foreach (var e in archive.Entries)
                    {
                        var.Entries.Add(e.FullName);
                        var type = Folders.Parse(e.FullName);
                        var.Info.Type |= type;

                        if (type == FolderType.AddonPackages)
                        {
                            continue;//Inside the var, there are items in the AddonPackages folder — we ignore them
                        }

                        ElementInfo ei = null;
                        if (e.Name == "meta.json")
                        {
                            using (Stream entryStream = e.Open())
                            {
                                using (StreamReader reader = new StreamReader(entryStream))
                                {
                                    string json = reader.ReadToEnd();
                                    var.MetaJson = json;
                                    
                                    var meta = ParseMeta(json);
                                    if (meta == null)
                                    {
                                        var.CorruptedMetaJson = true;
                                        json = SafeJsonLoader.FixSimpleJson(json);
                                        meta = ParseMeta(json);
                                    }
                                    if (meta != null)
                                    {
                                        var.Meta = meta;
                                        var.IsPreloadMorphs = GetPreloadMorphs(meta);
                                        var.Dependencies = EnumDependencies(meta).Select(VarName.Parse).Distinct(VarName.Eq).ToList();
                                        var.KeyDependencies = var.Dependencies.Select(x => new KeyCreatorAndName(x)).ToHashSet();
                                        var.IsLoaded = true;
                                    }
                                    else
                                    {
                                        var.Meta = null;
                                        var preloadMorphs = GetPreloadMorphs(json);
                                        var deps = ExtractDependencies(json);
                                        if(preloadMorphs != null && deps != null)
                                        {
                                            var.IsPreloadMorphs = preloadMorphs.Value;
                                            var.Dependencies = deps.Select(VarName.Parse).Distinct(VarName.Eq).ToList();
                                            var.KeyDependencies = var.Dependencies.Select(x => new KeyCreatorAndName(x)).ToHashSet();
                                            var.IsLoaded = true;
                                            //Expecting first-level dependencies, but in the corrupted meta we get dependencies that belong to the second level or deeper.
                                            //    We ignore this and treat all dependencies as first-level.
                                        }
                                        else
                                        {
                                            Settings.Logger.LogMsg("Failed to read morphs and dependencies from the corrupted meta");
                                        }
                                    }
                                }
                            }
                        }
                        else if (Ext.IsFileExtMatch(e.FullName, Ext.Jpg))
                        {
                            if (elGroupsDict.TryGetValue(FileHelper.ChangeExt(e.FullName, null), out var entries))
                            {
                                //Do not process single images
                                if (entries.Count() < 2)
                                    continue;
                            }
                            if (Folders.HasPreviewImage(type))
                            {
                                var size = GetJpegDimensionsFromEntry(e);
                                if (size != null && size.Value.height <= 512 && size.Value.width <= 512)
                                {
                                    var imgPath = GetOrAddImage(var, e);
                                    ei = new ElementInfo(e.FullName, imgPath, e.Name, e.FullName, e.LastWriteTime.DateTime, fi.CreationTime, e.Length, type);
                                    ei.Exts = entries?.Select(x => FileHelper.GetExtension(x.FullName)).ToList();
                                    var.Add(type, ei);
                                }
                            }
                        }
                        else
                        {
                            if (!string.IsNullOrWhiteSpace(e.Name))
                            {
                                ei = new ElementInfo(e.FullName, null, e.Name, e.FullName, e.LastWriteTime.DateTime, fi.CreationTime, e.Length, type);
                                var.Add(type, ei);
                            }

                            if (ContainsMorph(e.FullName))
                                var.MorphCount += 1;
                        }
                    }
                }
            }
            return var;
        }

        private static VarMeta ParseMeta(string json)
        {
            try
            {
                return JsonConvert.DeserializeObject<VarMeta>(json);
            }
            catch
            {
                return null;
            }
        }

        #region deps

        public static List<string> ExtractDependencies(string json)
        {
            int startIndex = json.IndexOf("dependencies", StringComparison.OrdinalIgnoreCase);
            if (startIndex == -1)
                return null;

            string searchRegion = json.Substring(startIndex);

            string pattern = @"""[^{}\[\]()""\r\n]*?\.[^{}\[\]()""\r\n]*?\.[^{}\[\]()""\r\n]*?""";

            var results = new List<string>();
            foreach (Match match in Regex.Matches(searchRegion, pattern))
            {
                string value = match.Value;

                int dotCount = value.Split('.').Length - 1;
                if (dotCount == 2)
                {
                    value = value.Trim('"', '/', '\\', '\'');
                    results.Add(value);
                }
            }

            return results;
        }

        public static IEnumerable<string> EnumDependencies(VarMeta meta, bool onlyFirstLevel = true)
        {
            if (meta.Dependencies == null)
                yield break;
            foreach (var item in meta.Dependencies)
            {
                yield return item.Key;

                if (!onlyFirstLevel)
                    foreach(var item2 in GetAllDependencyKeys(item.Value))
                        yield return item2;
            }
        }

        private static IEnumerable<string> GetAllDependencyKeys(DependencyNode node)
        {
            if (node?.Dependencies == null)
                yield break;

            foreach (var kvp in node.Dependencies)
            {
                yield return kvp.Key;

                foreach (var childKey in GetAllDependencyKeys(kvp.Value))
                    yield return childKey;
            }
        }

        #endregion

        #endregion

        #region morphs

        public static bool ContainsMorph(string path)
        {
            if(Folders.TryParse(path, out var type))
                return type == FolderType.PresetMorphs && Ext.IsFileExtMatch(path, Ext.Vmi); // path.EndsWith("vmi", StringComparison.InvariantCultureIgnoreCase);
            return false;
        }

        private static bool GetPreloadMorphs(VarMeta item)
        {
            
            if (item.CustomOptions != null && item.CustomOptions.ContainsKey(JsonPrefsEditor.str_preloadMorphs))
            {
                var value = item.CustomOptions[JsonPrefsEditor.str_preloadMorphs];
                if (value.Trim().ToLowerInvariant() == "true")
                    return true;
            }
            return false;
        }

        public static bool? GetPreloadMorphs(string potentiallyCorruptedJson)
        {
            if (string.IsNullOrEmpty(potentiallyCorruptedJson))
                return null;

            var match = Regex.Match(potentiallyCorruptedJson, @"[""']?preloadMorphs[""']?\s*:\s*[""']?(true|false)[""']?", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                string value = match.Groups[1].Value;
                if(bool.TryParse(value, out var res))
                   return res;
            }

            return null;
        }

        #endregion

        #region user item

        public static ElementInfo CreateUserItemEi(List<FileInfo> sortedFiles, bool isInArchive)
        {
            bool containsResource = false;
            foreach (var f in sortedFiles)
            {
                var ext = FileHelper.GetExtension(f.Name);
                var passed = Ext.CanContainResources(ext);
                if (passed)
                {
                    containsResource = true;
                    break;
                }
            }

            if (!containsResource)
                return null;

            var fullName = sortedFiles.Select(x=>x.FullName).FirstOrDefault();
            if (Folders.TryParse(fullName, out var type))
            {
                var relPath = NormalizePath(GetRelativePath(Settings.Config, fullName, isInArchive));
                var name = Path.GetFileName(fullName);
                string imagePath = null;
                if (Folders.HasPreviewImage(type)) //Textures will not pass the check
                {
                    var jpg = Settings.NoImagePath;
                    var preview = sortedFiles.FirstOrDefault(x => Ext.IsFileExtMatch(x.FullName, Ext.Jpg));
                    if (preview != null)
                        jpg = preview.FullName;
                    imagePath = jpg;
                }

                return new ElementInfo(fullName, imagePath, name, relPath, sortedFiles[0].LastWriteTime,
                    sortedFiles[0].CreationTime, sortedFiles.Sum(x => x.Length), type);
            }

            throw new Exception($"Cannot parse {fullName}");
        }

        #endregion

        #region app resources

        public static string GetErFileContent(string fileName)
        {
            var assembly = Assembly.GetExecutingAssembly();

            string resourceName = Array.Find(
                assembly.GetManifestResourceNames(),
                name => name.EndsWith(fileName, StringComparison.OrdinalIgnoreCase)
            );

            if (resourceName == null)
                throw new InvalidOperationException($"{fileName} not found");

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream != null)
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }

            return "";
        }

        #endregion
    }
}