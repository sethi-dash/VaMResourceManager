using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using Vrm.Control;
using Vrm.Json;
using Vrm.Refs;
using Vrm.Util;
using Vrm.Vam;
using Vrm.Window;
using MessageBox = System.Windows.Forms.MessageBox;

namespace Vrm.Vm
{
    public class VmToolVam : VmBase
    {
        private string NameCmdRunVam = "Run VaM";
        private string NameCmdStopVam = "Close VaM";
        private string NameCmdStartScan = "Scan";
        private string NameCmdStopScan = "Stop scan";

        private CancellationTokenSource _ctsScan;
        private bool _started;

        private string _vamPath = Settings.Config.VamPath;
        public string VamPath
        {
            get => _vamPath;
            set
            {
                if (SetField(ref _vamPath, value))
                {
                    Settings.Config.VamPath = value;
                    Settings.RefreshPath();
                }
            }
        }

        private string _vamArchivePath = Settings.Config.VamArchivePath;
        public string VamArchivePath
        {
            get => _vamArchivePath;
            set
            {
                if (SetField(ref _vamArchivePath, value))
                {
                    Settings.Config.VamArchivePath = value;
                    Settings.RefreshPath();
                }
            }
        }

        private string _referencesPath = Settings.Config.ReferenceFolder;
        public string ReferencesPath
        {
            get => _referencesPath;
            set
            {
                if (SetField(ref _referencesPath, value))
                {
                    Settings.Config.ReferenceFolder = value;
                    Settings.RefreshPath();
                }
            }
        }

        private string _imageCachePath = Settings.Config.CachePath;
        public string ImageCachePath
        {
            get => _imageCachePath;
            set
            {
                if (SetField(ref _imageCachePath, value))
                {
                    Settings.Config.CachePath = value;
                }
            }
        }

        private bool _autoScan = Settings.Config.AutoScan;
        public bool AutoScan
        {
            get => _autoScan;
            set
            {
                if (SetField(ref _autoScan, value))
                {
                    Settings.Config.AutoScan = value;
                }
            }
        }

        private bool _enableFilterHideFavTagFeature;
        public bool EnableFilterHideFavTagFeature
        {
            get => _enableFilterHideFavTagFeature;
            set
            {
                if (SetField(ref _enableFilterHideFavTagFeature, value))
                {
                    Settings.Config.EnableHideFavTagFeature = value;
                }
            }
        }


        private bool _useMaxAvailableVarVersion = Settings.Config.UseMaxAvailableVarVersion;
        public bool UseMaxAvailableVarVersion
        {
            get => _useMaxAvailableVarVersion;
            set
            {
                if (SetField(ref _useMaxAvailableVarVersion, value))
                {
                    Settings.Config.UseMaxAvailableVarVersion = value;
                }
            }
        }

        private string _shortcutVam = Settings.Config.ShortcutVam;
        public string ShortcutVam
        {
            get => _shortcutVam;
            set
            {
                if (SetField(ref _shortcutVam, value))
                {
                    Settings.Config.ShortcutVam = value;
                    RunVamViaShortcutIsEnabled = CmdRunVamShortcut.CanExecute(null);
                }
            }
        }


        private bool _runVamViaShortcut = Settings.Config.RunVamViaShortcut;
        public bool RunVamViaShortcut
        {
            get => _runVamViaShortcut;
            set
            {
                if (SetField(ref _runVamViaShortcut, value))
                {
                    Settings.Config.RunVamViaShortcut = value;
                }
            }
        }

        private bool _runVamViaShortcutIsEnabled;
        public bool RunVamViaShortcutIsEnabled
        {
            get => _runVamViaShortcutIsEnabled;
            set => SetField(ref _runVamViaShortcutIsEnabled, value);
        }

        public VmStatTable StatTable { get; }

        private int _imagesInCache;
        public int ImagesInCache
        {
            get => _imagesInCache;
            set
            {
                if (SetField(ref _imagesInCache, value))
                {
                    ((AsyncRelayCommand)CmdClearCache).RaiseCanExecuteChanged();
                }
            }
        }

        private int _varCount;
        public int VarCount
        {
            get => _varCount;
            set
            {
                if (SetField(ref _varCount, value))
                {
                    ((AsyncRelayCommand)CmdClear).RaiseCanExecuteChanged();
                }
            }
        }

        private Process _vamProcess;
        public Process VamProcess
        {
            get => _vamProcess;
            set
            {
                if (SetField(ref _vamProcess, value))
                {
                    if (value != null)
                    {
                        CmdStartStopVam.Caption = NameCmdStopVam;
                        CmdStartStopVam.Command = KillVamCmd;
                    }
                    else
                    {
                        CmdStartStopVam.Caption = NameCmdRunVam;
                        CmdStartStopVam.Command = RunVamCmd;
                    }
                }
            }
        }

        private bool _isWindowTopmost;
        public bool IsWindowTopmost
        {
            get => _isWindowTopmost;
            set
            {
                if (SetField(ref _isWindowTopmost, value))
                {
                    Settings.Config.IsWindowTopmost = value;
                    UiHelper.MainWindow.Topmost = value;
                }
            }
        }

        public VmUserFolders UserFolders { get; } = new VmUserFolders { Text = string.Join(Environment.NewLine, Settings.Config.UserFolders) };

        public ICommand CmdSetVamFolder {get;}
        public ICommand CmdSetVamArchiveFolder {get;}
        public ICommand CmdSetReferenceFolder {get;}
        public ICommand CmdImageCachePath {get;}
        public AsyncRelayCommand CmdScan {get;}
        public ICommand CmdScanCancel {get;}
        public ICommand CmdClear {get;}
        public ICommand CmdClearCache {get;}
        public ICommand DtLoadedCmd {get;}
        public ICommand RunVamCmd {get;}
        public ICommand KillVamCmd {get;}
        public VmCmdBtn CmdStartStopVam {get;}
        public VmCmdBtn CmdStartStopScan {get;}
        public ICommand CmdChangeUserPrefs {get;}
        public ICommand CmdOpenRefsFolder {get;}
        public ICommand CmdOpenVamFolder {get;}
        public ICommand CmdOpenVamArchiveFolder {get;}
        public ICommand CmdOpenReferenceFolder {get;}
        public ICommand CmdOpenImageCachePath {get;}
        public ICommand CmdSetVamShortcut {get;}
        public ICommand CmdRunVamShortcut {get;}
        public ICommand CmdRemoveOldVarVersions {get;}

        private VmMain VmMain => (VmMain)ParentTab;
        private VmToolVar VarExplorer => VmMain.ToolVar;
        private VmToolUserData UserDataExplorer => VmMain.ToolUserData;

        #region init

        public VmToolVam()
        {
            Name = "vam";

            Task.Run(CheckVamProcess);

            DtLoadedCmd = new RelayCommand(_ =>
            {
                if (!_started)
                {
                    _started = true;

                    if(VarCount < 1 && Settings.Config.AutoScan && !VmMain.Progress.IsBusy)
                        CmdScan.Execute(null);

                    VmMain.ApplySortFromConfig();
                }

                //ImagesInCache = await FileHelper.GetImageCount();
                RunVamViaShortcutIsEnabled = CmdRunVamShortcut.CanExecute(null);
                if (!RunVamViaShortcutIsEnabled)
                    RunVamViaShortcut = false;
                IsWindowTopmost = Settings.Config.IsWindowTopmost;
            });

            CmdSetVamFolder = new RelayCommand(_ =>
            {
                using (var dialog = new FolderBrowserDialog())
                {
                    dialog.Description = @"Select VaM folder";
                    dialog.ShowNewFolderButton = false;
                    dialog.SelectedPath = VamPath;

                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        VamPath = dialog.SelectedPath;
                    }
                }
            });
            
            CmdSetVamArchiveFolder = new RelayCommand(_ =>
            {
                using (var dialog = new FolderBrowserDialog())
                {
                    dialog.Description = @"Select VaM archive folder";
                    dialog.ShowNewFolderButton = false;
                    dialog.SelectedPath = VamArchivePath;

                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        VamArchivePath = dialog.SelectedPath;
                    }
                }
            });

            CmdSetReferenceFolder = new RelayCommand(_ =>
            {
                using (var dialog = new FolderBrowserDialog())
                {
                    dialog.Description = @"Select Reference folder";
                    dialog.ShowNewFolderButton = false;
                    var path = ReferencesPath == "references" ? FileHelper.PathCombine(AppDomain.CurrentDomain.BaseDirectory, ReferencesPath) : ReferencesPath;
                    dialog.SelectedPath = path;

                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        ReferencesPath = dialog.SelectedPath;
                    }
                }
            });

            CmdImageCachePath = new RelayCommand(_ =>
            {
                using (var dialog = new FolderBrowserDialog())
                {
                    dialog.Description = @"Select image cache folder";
                    dialog.ShowNewFolderButton = true;
                    var imgPath = ImageCachePath == "images" ? FileHelper.PathCombine(AppDomain.CurrentDomain.BaseDirectory, ImageCachePath) : ImageCachePath;
                    dialog.SelectedPath = imgPath;

                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        ImageCachePath = dialog.SelectedPath;
                    }
                }
            });

            CmdClearCache = new AsyncRelayCommand(async () =>
            {
                var dr = MessageBox.Show(@"Are you sure?", @"Cache Deletion", MessageBoxButtons.YesNo);
                if (dr != DialogResult.Yes)
                    return;
                _ctsScan = new CancellationTokenSource();
                var w = TextBoxDialog.ShowProgress("Clearing cache");
                try
                {
                    VmMain.Progress.IsBusy = true;
                    Clear(); //free files
                    await Task.Run(ClearCache);
                    ImagesInCache = 0;
                }
                catch
                {
                    /**/
                }
                finally
                {
                    VmMain.Progress.IsBusy = false;
                    w.Close();
                    try { _ctsScan?.Dispose(); }catch{/**/}
                    _ctsScan = null;
                }
            }, ()=> ImagesInCache > 0);

            CmdClear = new AsyncRelayCommand(() =>
            {
                _ctsScan = new CancellationTokenSource();
                try
                {
                    VmMain.Progress.IsBusy = true;
                    Clear();
                }
                catch
                {
                    /**/
                }
                finally
                {
                    VmMain.Progress.IsBusy = false;
                    try { _ctsScan?.Dispose(); }catch{/**/}
                    _ctsScan = null;
                }
                return Task.CompletedTask;
            }, ()=> VarCount > 0);

            CmdScan = new AsyncRelayCommand(async () =>
            {
                var w = TextBoxDialog.ShowProgress("Scanning. The first Scan may take a long time.");
                _ctsScan = new CancellationTokenSource();
                CmdStartStopScan.Caption = NameCmdStopScan;
                CmdStartStopScan.Command = CmdScanCancel;
                var stopwatch = new Stopwatch();
                try
                {
                    VmMain.Progress.IsBusy = true;
                    Clear();
                    var rdArchive = new ResourceDict();
                    var rdLoaded = new ResourceDict();
                    stopwatch.Start();
                    var varBag = await ReadVars(_ctsScan.Token);
                    var duplicates = FindDuplicatesOutsideAddonPackages(varBag).ToList();
                    if (duplicates.Any())
                    {
                        w.Close();
                        var @fixed = ResolveDuplicates(duplicates);
                        if (@fixed)
                            w = TextBoxDialog.ShowProgress("Scanning. The first Scan may take a long time.");
                        else
                            return;
                    }
                    await ScanVar(varBag, stopwatch, rdArchive, rdLoaded, _ctsScan.Token, Settings.Logger, StatTable);
                    await ScanUserData(rdArchive, rdLoaded, _ctsScan.Token, StatTable);
                    Settings.ArchiveRd = rdArchive;
                    Settings.LoadedRd = rdLoaded;
                    StatTable.IsVisible = true;

                    var markBeforeResolveDeps = stopwatch.ElapsedMilliseconds;
                    var items = rdArchive.UserResources.Concat(rdLoaded.UserResources);
                    await Task.Run(()=>Parallel.ForEach(items, x => x.ResolveDependencies()));
                    await Task.Run(() =>
                    {
                        foreach (var item in items)
                            item.WriteAsDto();
                    });
                    Settings.Logger.LogMsg($"Dependencies resolved for: {stopwatch.ElapsedMilliseconds - markBeforeResolveDeps} ms");

                    VmMain.DateStartModified = rdArchive.StartModified < rdLoaded.StartModified ? rdArchive.StartModified : rdLoaded.StartModified;
                    VmMain.DateEndModified = rdArchive.EndModified > rdLoaded.EndModified ? rdArchive.EndModified : rdLoaded.EndModified;
                    VmMain.DateStartCreated = rdArchive.StartCreated < rdLoaded.StartCreated ? rdArchive.StartCreated : rdLoaded.StartCreated;
                    VmMain.DateEndCreated = rdArchive.EndCreated > rdLoaded.EndCreated ? rdArchive.EndCreated : rdLoaded.EndCreated;
                    if (VmMain.DateStartModified == DateTime.MaxValue)
                        VmMain.DateStartModified = null;
                    if (VmMain.DateEndModified == DateTime.MinValue)
                        VmMain.DateEndModified = null;
                    if (VmMain.DateStartCreated == DateTime.MaxValue)
                        VmMain.DateStartCreated = null;
                    if (VmMain.DateEndCreated == DateTime.MinValue)
                        VmMain.DateEndCreated = null;
                    VmMain.ApplyFilter();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
                finally
                {
                    w.Close();
                    stopwatch.Stop();
                    try
                    {
                        VmMain.Progress.IsBusy =false;
                        try { _ctsScan?.Dispose(); }catch{/**/}
                        _ctsScan = null;
                    }
                    catch{/**/}
                    CmdStartStopScan.Caption = NameCmdStartScan;
                    CmdStartStopScan.Command = CmdScan;
                }
            }, ()=>!VmMain.Progress.IsBusy);
            
            CmdScanCancel = new RelayCommand(_ =>
            {
                try
                {
                    _ctsScan.Cancel();
                }
                catch
                {
                    /**/
                }
                finally
                {
                    CmdStartStopScan.Caption = NameCmdStartScan;
                    CmdStartStopScan.Command = CmdScan;
                }
            }, _=>VmMain.Progress.IsBusy);

            RunVamCmd = new RelayCommand(_ =>
            {
                var fn = RunVamViaShortcut ? Settings.Config.ShortcutVam : "VaM (Desktop Mode).bat";
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = FileHelper.PathCombine(Settings.Config.VamPath, fn),
                        WorkingDirectory = Settings.Config.VamPath,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                    },
                };
                process.Start();
            }, x =>
            {
                var fn = RunVamViaShortcut ? Settings.Config.ShortcutVam : "VaM (Desktop Mode).bat";
                fn = FileHelper.PathCombine(Settings.Config.VamPath, fn);
                return FileHelper.FileExists(fn) && VamProcess == null;
            });

            KillVamCmd = new RelayCommand(_ =>
            {
                try
                {
                    var p = VamProcess;
                    if (p != null)
                        FileHelper.CloseOrKill(p);
                }
                catch
                {
                    /**/
                }
                finally
                {
                    CommandManager.InvalidateRequerySuggested();
                }
            }, x => VamProcess != null);

            CmdStartStopVam = new VmCmdBtn(NameCmdRunVam, RunVamCmd) {Tooltip = "Start or stop the VaM application"};
            CmdStartStopScan = new VmCmdBtn(NameCmdStartScan, CmdScan) {Tooltip = "Find all resources in the Loaded and Archived directories."};

            CmdChangeUserPrefs = new RelayCommand(x =>
            {
                int processed = 0;
                var changedNames = new ConcurrentBag<string>();
                var path = FileHelper.PathCombine(Settings.Config.VamPath, Settings.Loaded[FolderType.AddonPackages]);
                var errors = new ConcurrentBag<string>();
                var wnd = new System.Windows.Window
                {
                    Title = ".UserPrefs Editor",
                    Width = 600,
                    Height = 400,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen
                };
                var vm = new UserPrefsEditor()
                {
                    Header = $"All Var files currently located in '{path}' will receive the following settings.",
                    Footer = "If you have changed the morphs settings, you need to run a Scan to update the statistics.",
                    OnOk = async userPrefs =>
                    {
                        wnd.Close();
                        var w = TextBoxDialog.ShowProgress(".UserPrefs Editor - Processing files...");
                        try
                        {
                            await Task.Run(() =>
                            {
                                var vars = new List<VarFile>();
                                foreach (var item in Settings.LoadedRd.Vars)
                                {
                                    if (userPrefs.ViewModel.ProcessOnlyLatestVars)
                                        vars.Add(item.Value.GetMaxVersion());
                                    else
                                    {
                                        foreach (var item2 in item.Value.Vars)
                                            vars.Add(item2.Value);
                                    }
                                }

                                foreach (var v in vars)
                                {
                                    processed++;
                                    try
                                    {
                                        var fn1 = FileHelper.GetPrefsFileName_EnableDisableIgnore(v.Name);
                                        var json1 = FileHelper.FileExists(fn1) ? FileHelper.FileReadAllText(fn1) : "";
                                        var editor = new JsonPrefsEditor(json1);
                                        editor.SetFlag(JsonPrefsEditor.str_pluginsAlwaysEnabled, userPrefs.ViewModel.PluginsAlwaysEnabled);
                                        editor.SetFlag(JsonPrefsEditor.str_pluginsAlwaysDisabled, userPrefs.ViewModel.PluginsAlwaysDisabled);
                                        editor.SetFlag(JsonPrefsEditor.str_ignoreMissingDependencyErrors, userPrefs.ViewModel.IgnoreMissingDependencyErrors);
                                        string updatedJson1 = editor.GetEditedJson();
                                        FileHelper.WriteAllText(fn1, updatedJson1);

                                        var fn2 = FileHelper.GetPrefsFileName_NotesPreloadMorphs(v.Name);
                                        var json2 = FileHelper.FileExists(fn2) ? FileHelper.FileReadAllText(fn2) : "";
                                        editor = new JsonPrefsEditor(json2);
                                        editor.SetCustomOption(JsonPrefsEditor.str_preloadMorphs,
                                            userPrefs.ViewModel.PreloadMorphs);
                                        string updatedJson2 = editor.GetEditedJson();
                                        FileHelper.WriteAllText(fn2, updatedJson2);

                                        if (json1 != updatedJson1 || json2 != updatedJson2)
                                            changedNames.Add(v.Name.FullName);
                                    }
                                    catch (Exception ex)
                                    {
                                        errors.Add(ex.Message);
                                    }
                                }
                            });
                        }
                        finally
                        {
                            w.Close();
                        }

                        if (errors.Any())
                        {
                            TextBoxDialog.ShowDialog("Errors:", errors);
                        }

                        if (changedNames.Any())
                        {
                            TextBoxDialog.ShowDialog($"{changedNames.Count}/{processed} file(s) was changed:", changedNames);
                        }
                        else
                        {
                            TextBoxDialog.ShowDialog($"Total files: {processed}", $"No files changed");
                        }

                    }
                };
                wnd.Content = vm;
                wnd.ShowDialog();
            });

            CmdOpenRefsFolder = new RelayCommand(x =>
            {
                FileHelper.ShowInExplorer(Settings.Config.ReferenceFolder);
            });

            CmdOpenVamFolder = new RelayCommand(x =>
            {
                FileHelper.ShowInExplorer(Settings.Config.VamPath);
            });

            CmdOpenVamArchiveFolder = new RelayCommand(x =>
            {
                FileHelper.ShowInExplorer(Settings.Config.VamArchivePath);
            });

            CmdOpenReferenceFolder = new RelayCommand(x =>
            {
                FileHelper.ShowInExplorer(Settings.Config.ReferenceFolder);
            });

            CmdOpenImageCachePath = new RelayCommand(x =>
            {
                FileHelper.ShowInExplorer(Settings.Config.CachePath);
            });

            CmdSetVamShortcut = new RelayCommand(x =>
            {
                using (var dialog = new OpenFileDialog())
                {
                    dialog.Title = @"Select Shortcut";
                    dialog.Filter = @"Batch Files (*.bat)|*.bat|All Files (*.*)|*.*";
                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        ShortcutVam = dialog.FileName;
                    }
                }
            });
            CmdRunVamShortcut = new RelayCommand(x =>
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = ShortcutVam,
                        WorkingDirectory = Settings.Config.VamPath,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                    },
                };
                process.Start();
            }, _ => FileHelper.FileExists(ShortcutVam) && FileHelper.GetExtension(ShortcutVam).ToLower() == ".bat");

            CmdRemoveOldVarVersions = new RelayCommand(async x =>
            {
                var toRemove = new List<VarFile>();
                foreach (var item in Settings.LoadedRd.Vars.Concat(Settings.ArchiveRd.Vars))
                {
                    foreach (var item2 in item.Value.GetOldVersions())
                    {
                        toRemove.Add(item2);
                    }
                }

                var toRemoveStr = toRemove.Select(x1=>x1.Info.FullName).ToHashSet<string>(StringComparer.OrdinalIgnoreCase);
                bool isEmpty = !toRemoveStr.Any();
                if (isEmpty)
                    toRemoveStr.Add("No items");
                var res = TextBoxDialog.ShowDialog($"Move old versions to {Settings.Config.VamPath}\\Old.AddonPackages\\?", toRemoveStr);
                if (res.HasValue && res.Value && !isEmpty)
                {
                    var w = TextBoxDialog.ShowProgress("Processing.");
                    try
                    {
                        foreach (var item in toRemoveStr)
                            FileHelper.MoveToOldAddonPackages(item, true);

                        FindVmMain().Delete(toRemove);
                        FindVmMain().ToolVam.CmdScan.Execute(null);
                        await FindVmMain().ToolVam.CmdScan.ExecutionTask;
                    }
                    catch { /**/ }
                    finally
                    {
                        w.Close();
                    }
                    TextBoxDialog.ShowDialog($"Cleanup result", $"{toRemove.Count} file(s) were moved");
                }
            });

            StatTable = new VmStatTable() {IsVisible = false};
        }

        #endregion

        private async Task CheckVamProcess()
        {
            while (true)
            {
                try
                {
                    var p = FileHelper.FindProcess(Settings.Config.VamPath);
                    await UiHelper.InvokeAsync(() =>
                    {
                        VamProcess = p;
                        CommandManager.InvalidateRequerySuggested();
                    });
                }
                catch{/**/}

                await Task.Delay(3000);
            }
        }

        public override IEnumerable<VmCmdBtn> GetCmds()
        {
            yield return CmdStartStopVam;
            yield return CmdStartStopScan;
            yield return new VmCmdBtn(CmdChangeUserPrefs, "Change UserPrefs");
            yield return new VmCmdBtn(CmdRemoveOldVarVersions, "Remove old vars"){Tooltip = "Move old versions of vars to the Old.AddonPackages folder"};
            yield return new VmCmdBtn(CmdClearCache, "Clear cache");
        }

        public override void OnUpdateTools(ShowTools tools)
        {
            base.OnUpdateTools(tools);
            tools.UpdateAll(false);
            tools.Invoke();
        }

        public override void OnHide()
        {
            base.OnHide();
            try
            {
            }
            catch{/**/}
        }

        #region vam

        private void Clear()
        {
            Settings.ArchiveRd?.Clear();
            Settings.LoadedRd?.Clear();
            VmMain.DateStartModified = null;
            VmMain.DateEndModified = null;
            VmMain.DateStartCreated = null;
            VmMain.DateEndCreated = null;
            VarCount = 0;
            VarExplorer.OnReset();
            UserDataExplorer.OnReset();
            StatTable.Items.Clear();
            StatTable.IsVisible = false;
        }

        private void ClearCache()
        {
            if(FileHelper.DeleteDirectory(Settings.Config.CachePath))
                Settings.Logger.LogMsg("Cache cleared");
            FileHelper.CreateDirectoryInNotExists(Settings.Config.CachePath);
        }

        private bool ResolveDuplicates(List<VarFile> duplicates)
        {
            bool dupsFixed = false;
            var res = TextBoxDialog.ShowDialog($"{duplicates.Count} VAR file duplicates were found.", "VaM Resource Manager currently does not support managing duplicate VAR files. To proceed, all duplicates not located in AddonPackages must be relocated to Old.AddonPackages. This will improve scanning and processing performance.\nDuplicates will not be deleted and can be restored later if needed.\n\nWould you like to remove duplicates and continue?");
            if (res.HasValue && res.Value)
            {
                var res2 = TextBoxDialog.ShowDialog(
                    $"Move duplicates to {Settings.Config.VamPath}\\Old.AddonPackages\\?",
                    duplicates.Select(x=>x.Info.FullName));
                if (res2.HasValue && res2.Value)
                {
                    var w = TextBoxDialog.ShowProgress("Moving duplicates.");
                    try
                    {
                        foreach (var item in duplicates)
                            FileHelper.MoveToOldAddonPackages(item.Info.FullName, item.IsInArchive);
                        dupsFixed = true;
                    }
                    catch { /**/ }
                    finally
                    {
                        w.Close();
                    }

                }
                TextBoxDialog.ShowDialog($"Cleanup result", $"{duplicates.Count} file(s) were moved");
            }
            if(!dupsFixed)
                TextBoxDialog.ShowDialog($"Scanning was canceled.", "A scan must be performed first for the program to work.");
            return dupsFixed;
        }

        #endregion

        #region var

        private IEnumerable<VarFile> FindDuplicatesOutsideAddonPackages(IEnumerable<VarFile> vars)
        {
            var dGroups = vars.GroupBy(x => x.Name.FullName).Where(x=>x.Count() > 1);
            foreach (var g in dGroups)
            {
                var sortedByLen = g.OrderBy(x => x.Info.RelativePath.Length);
                foreach (var v in sortedByLen.Skip(1))
                {
                    yield return v;
                }
            }
        }

        private async Task<ConcurrentBag<VarFile>> ReadVars(CancellationToken ct)
        {
            var bag = new ConcurrentBag<VarFile>();
            var errors = new ConcurrentBag<string>();
            var messages = new ConcurrentBag<string>();
            await Task.Run(() => { ReadVarsParallel(false, bag, ct, errors, messages); }, ct);
            await Task.Run(() => { ReadVarsParallel(true, bag, ct, errors, messages); }, ct);
            foreach (var e in errors)
                Settings.Logger.LogErr(e);
            foreach (var m in messages)
                Settings.Logger.LogMsg(m);
            return bag;
        }

        private async Task ScanVar(ConcurrentBag<VarFile> bag, Stopwatch stopwatch, ResourceDict rdArchive, ResourceDict rdLoaded, CancellationToken ct, ILogger log, VmStatTable statTable)
        {
            List<VarFile> sorted = null;
            await Task.Run(() =>
            {
                sorted = bag.ToList();
                sorted.Sort(VarFile.Comparer);
            }, ct);

            if (Settings.Config.EnableHideFavTagFeature)
            {
                await Task.Run(() =>
                {
                    var folders = new List<string>()
                    {
                        FileHelper.PathCombine(Settings.Config.VamPath, "AddonPackagesUserPrefs"), // UserPrefs of vars 
                        FileHelper.PathCombine(Settings.Config.VamPath, "AddonPackagesFilePrefs"), // .hide/.fav of stuff inside var
                        FileHelper.PathCombine(Settings.Config.VamPath, "Custom"), // UserPrefs of stuff inside var, UserPrefs of user resources
                        FileHelper.PathCombine(Settings.Config.VamArchivePath, "Custom"), // UserPrefs of stuff inside var, UserPrefs of user resources
                        FileHelper.PathCombine(Settings.Config.VamPath, "Saves"), // .hide/.fav of user resources
                        FileHelper.PathCombine(Settings.Config.VamArchivePath, "Saves") // .hide/.fav of user resources
                    };
                    var (paths, prefs) = FileHelper.ScanFolders(folders);
                    foreach (var v in sorted)
                        FileHelper.SetVarPrefs_Fast(v, paths, prefs);
                }, ct);
            }

            var t1 = stopwatch.ElapsedMilliseconds;
            Settings.Logger.LogMsg($"VAR scan time: {t1} ms");
            if (sorted == null)
                return;

            int countImage_Loaded = 0;
            int countImage_InArchive = 0;

            int countVar_Loaded = 0;
            int countVar_InArchive = 0;

            int countMorphs_Loaded = 0;
            int countMorphs_InArchive = 0;

            int countPreloadedMorphs = 0;
            int countPreloadedMorphsDisabledByMeta = 0;
            int countPreloadedMorphsDisabledByUserPrefs = 0;

            foreach (var v in sorted)
            {
                if (!v.IsLoaded)
                {
                    log.LogMsg($"Skipped corrupted: {v.Name.RawName}");
                    continue;
                }

                if (v.CorruptedMetaJson)
                    log.LogMsg($"Invalid meta.json: {v.Name}");
                if (v.CorruptedMetaJson && v.IsLoaded)
                    log.LogMsg($"Data extracted successfully: {v.Name}");
                #if DEBUG
                if (v.Type == FolderType.Unset)
                    log.LogMsg($"No type detected: {v.Name}");
                #endif

                if (v.IsInArchive)
                {
                    countVar_InArchive++;
                    countMorphs_InArchive += v.MorphCount;
                }
                else
                {
                    countVar_Loaded++;
                    countMorphs_Loaded += v.MorphCount;

                    if (v.IsPreloadMorphs)
                    {
                        if (v.IsPreloadMorhpsDisabledByPrefs)
                            countPreloadedMorphsDisabledByUserPrefs += v.MorphCount;
                        else
                            countPreloadedMorphs += v.MorphCount;
                    }
                    else
                        countPreloadedMorphsDisabledByMeta += v.MorphCount;
                }

                VarExplorer.OnAdd(v);
                foreach (var kvp in v.ElementsDict)
                {
                    foreach (var el in kvp.Value)
                    {
                        if (el.IsImage)
                        {
                            if (v.IsInArchive)
                                countImage_InArchive++;
                            else
                                countImage_Loaded++;
                        }

                        VarExplorer.OnAdd(kvp.Key, el, v);
                    }
                }

                if (v.IsInArchive)
                    rdArchive.AddVar(v);
                else
                    rdLoaded.AddVar(v);
            }

            VarExplorer.OnAddComplete();
            ImagesInCache = countImage_Loaded + countImage_InArchive;
            //if (ImagesInCache != await FileHelper.GetImageCount()) { }
            statTable.Add(new StatRow("vars", countVar_Loaded, countVar_InArchive, countVar_Loaded + countVar_InArchive));
            statTable.Add(new StatRow("cached images", countImage_Loaded, countImage_InArchive, countImage_Loaded + countImage_InArchive));
            statTable.Add(new StatRow("morphs", countMorphs_Loaded, countMorphs_InArchive, countMorphs_Loaded + countMorphs_InArchive));
            statTable.Add(new StatRow("preload morphs", countPreloadedMorphs, 0, countPreloadedMorphs));
            statTable.Add(new StatRow("preload morphs disabled by UserPrefs", countPreloadedMorphsDisabledByUserPrefs, 0, countPreloadedMorphsDisabledByUserPrefs));
            statTable.Add(new StatRow("preload morphs disabled by Meta", countPreloadedMorphsDisabledByMeta, 0, countPreloadedMorphsDisabledByMeta));

            Settings.Logger.LogMsg($"VAR rendering time: {stopwatch.ElapsedMilliseconds - t1} ms");
            stopwatch.Stop();
        }

        private static void ReadVarsParallel(bool isArchive, ConcurrentBag<VarFile> vars, CancellationToken ct, ConcurrentBag<string> errors, ConcurrentBag<string> messages)
        {
            var dir = isArchive ? Settings.Archive[FolderType.AddonPackages] : Settings.Loaded[FolderType.AddonPackages];
            if (!FileHelper.DirectoryExists(dir))
                return;
            FileHelper.ProcessParallel(dir, "*.var", ct, x =>
            {
                var v = FileHelper.GetOrAddVar(x, isArchive, messages);
                vars.Add(v);
            }, errors);
        }

        #endregion

        #region user data

        private async Task ScanUserData(ResourceDict rdArchive, ResourceDict rdLoaded, CancellationToken ct, VmStatTable statTable)
        {
            var stopwatch = Stopwatch.StartNew();

            var bag = new ConcurrentBag<UserItem>();
            var errors = new ConcurrentBag<string>();
            var messages = new ConcurrentBag<string>();
            await Task.Run(() =>
            {
                ReadParallel(false, bag, ct, errors, messages);
            }, ct);
            await Task.Run(() =>
            {
                ReadParallel(true, bag, ct, errors, messages);
            }, ct);
            foreach (var e in errors)
                Settings.Logger.LogErr(e);
            foreach (var m in messages)
                Settings.Logger.LogMsg(m);

            List<UserItem> sorted = null;
            await Task.Run(() =>
            {
                sorted = bag.ToList();
                sorted.Sort(UserItem.Comparer);
            }, ct);
            if (sorted == null)
                return;

            int countLoaded = 0;
            int countArchived = 0;
            foreach (var item in sorted)
            {
                UserDataExplorer.OnAdd(item);

                if (item.IsInArchive)
                {
                    countArchived++;
                    rdArchive.AddRes(item);
                    foreach(var r in item.Files)
                        rdArchive.AddRes(FileHelper.GetRelativePath(Settings.Config, r.FullName, true)); //item.Info.RelativePath
                }
                else
                {
                    countLoaded++;
                    rdLoaded.AddRes(item);
                    foreach(var r in item.Files)
                        rdLoaded.AddRes(FileHelper.GetRelativePath(Settings.Config, r.FullName, false)); //item.Info.RelativePath
                }
            }

            UserDataExplorer.OnAddComplete();
            UserDataExplorer.Count = countLoaded + countArchived;
            StatTable.Add(new StatRow("user items", countLoaded, countArchived, countLoaded + countArchived));

            stopwatch.Stop();
            Settings.Logger.LogMsg($"User items scan time: {stopwatch.ElapsedMilliseconds} ms");
        }

        private void ReadParallel(bool isArchive, ConcurrentBag<UserItem> bag, CancellationToken ct, ConcurrentBag<string> errors, ConcurrentBag<string> messages)
        {
            var dirs = isArchive ? Settings.Archive : Settings.Loaded;
            var dirs2 = dirs.Where(x=>x.Key != FolderType.AddonPackages).Select(x => (x.Value, Folders.GetSearchOption(x.Key))).ToList();
            foreach(var uf in Settings.Config.UserFolders)
                dirs2.Add((FileHelper.GetFullPath(uf, isArchive, Settings.Config), SearchOpt.All));
            var fileGroups = FileHelper.GroupFiles(dirs2);

            var options = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }; //1  
            Parallel.ForEach(fileGroups, options, fg =>
            {
                try
                {
                    ct.ThrowIfCancellationRequested();
                    var files = fg.ToList();
                    files.Sort();
                    var fileInfos = files.Select(x => new FileInfo(x)).ToList();
                    var ei = FileHelper.CreateUserItemEi(fileInfos, isArchive);
                    if (ei != null)
                    {
                        var item = FileHelper.GetOrAddUserItem(ei, isArchive, fileInfos, messages);
                        if (item != null)
                        {
                            if (Settings.Config.EnableHideFavTagFeature)
                            {
                                FileHelper.SetFavHide(item.Item.FullName, ei);
                                FileHelper.UpdateTagInElementInfo(item.Item.FullName, ei);
                            }

                            bag.Add(item);
                        }
                    }
                    else
                    {
                        //Debug.WriteLine($"Pref/Hide/Fav file: {fileInfos[0].FullName}");
                    }
                }
                catch(OperationCanceledException){/**/}
                catch (Exception ex)
                {
                    errors.Add(ex.Message);
                }
            });
        }

        #endregion
    }
}