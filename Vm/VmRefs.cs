using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Vrm.Cfg;
using Vrm.Refs;
using Vrm.Util;
using Vrm.Window;

namespace Vrm.Vm
{
    public class VmRefs : INotifyPropertyChanged
    {
        private VmCmdBtn _sync = null;
        private VmCmdBtn _syncCancel = null;
        private VmCmdBtn _inspect = null;

        public ObservableCollection<VmRefItem> Items { get; } = new ObservableCollection<VmRefItem>();
        public RefCounterSet RefsRelativePathHash { get; } = new RefCounterSet();
        public HashSet<string> Synced = new HashSet<string>();

        private void AddRefsPathHash(string relPath)
        {
            RefsRelativePathHash.Add(FileHelper.NormalizePath(relPath));
        }

        private bool _isSyncPlanned;
        public bool IsSyncPlanned
        {
            get => _isSyncPlanned;
            set
            {
                if (SetField(ref _isSyncPlanned, value))
                {
                    _sync.IsVisible = _syncCancel.IsVisible = value;
                    UpdateCmdsVisibility();
                }
            }
        }

        private VmRefItem _selectedItem;
        public VmRefItem SelectedItem
        {
            get => _selectedItem;
            set => SetField(ref _selectedItem, value);
        }


        private bool _isEditMode;
        public bool IsEditMode
        {
            get => _isEditMode;
            set
            {
                if (_isEditMode != value)
                {
                    _isEditMode = value;
                    OnPropertyChanged();
                }
            }
        }

        private VmRefItem _editedItem;
        public VmRefItem EditedItem
        {
            get => _editedItem;
            set
            {
                if (Equals(value, _editedItem)) 
                    return;
                _editedItem = value;
                OnPropertyChanged();
                IsEditMode = value != null;
                UpdateCmdsVisibility();
            }
        }

        private VmRefItem _viewedItem;
        public VmRefItem ViewedItem
        {
            get => _viewedItem;
            set
            {
                if (Equals(value, _viewedItem)) 
                    return;
                _viewedItem = value;
                OnPropertyChanged();
                UpdateCmdsVisibility();
            }
        }

        public ICommand CmdAdd {get;}
        public ICommand CmdRemove {get;}
        public ICommand CmdBeginEdit {get;}
        public ICommand CmdBeginView {get;}
        public ICommand CmdRename {get;}
        public ICommand CmdEnd {get;}
        public ICommand CmdApply {get;}
        public ICommand CmdInspect {get;}
        public ICommand CmdApplyCancel {get;}
        public ICommand CmdValidateRefs {get;}
        public ICommand CmdClearArchive {get;}

        public ObservableCollection<VmCmdBtn> Cmds { get; } = new ObservableCollection<VmCmdBtn>();
        public VmMain MainVm {get;set;}

        private RefCfg _editedReferenceCfg;
        private VmRefItem _changedRefItem;

        #region init

        public VmRefs()
        {
            Items.CollectionChanged += Items_CollectionChanged;

            CmdAdd = new RelayCommand(x =>
            {
                int n = 1;
                var last = Items.LastOrDefault();
                if (last != null)
                {
                    var match = Regex.Match(last.Name, @"\d+");
                    if (match.Success)
                    {
                        n = int.Parse(match.Value) + 1;
                    }
                    else
                        n = Items.Count + 1;
                }
                Items.Add(VmRefItem.New($"Reference {n}"));
            }, x => true); 

            CmdRemove = new RelayCommand(x =>
            {
                var item = (VmRefItem)x;
                var file = FileHelper.PathCombine(Settings.Config.ReferenceFolder, item.Name + ".json");
                if (!FileHelper.FileExists(file))
                    file = FileHelper.PathCombine(Settings.ExePath, file);
                if (FileHelper.FileExists(file))
                    FileHelper.FileDelete(file);
                else
                    Settings.Logger.LogErr("Could not delete: " + item.Name);
                Items.Remove(item);
                Synced.Remove(item.Name);
                foreach (var item2 in item.Items)
                {
                    foreach(var item3 in item2.Files)
                        RefsRelativePathHash.Remove(item3);
                }

                if(item.IsEnabled)
                    CmdApply?.Execute(null);
            }, x =>
            {
                foreach (var item in Items)
                {
                    if (item == x)
                        continue;
                    if (item.IsEnabled)
                        return true;
                }
                return false;
            });

            CmdRename = new RelayCommand(x =>
            {
                var @ref = (VmRefItem)x;
                var prevName = @ref.Name;
                var w = new RenameWindow(@ref.Name, Items.Select(x1 => x1.Name)){Owner = UiHelper.MainWindow, WindowStartupLocation = WindowStartupLocation.CenterOwner};
                if (w.ShowDialog() == true)
                {
                    @ref.Name = w.NewName;
                    Synced.Remove(prevName);
                    Synced.Add(@ref.Name);
                    Save();
                    SaveSyncList();
                }
            }, x =>
            {
                return x is VmRefItem;// && !IsSyncPlanned;
            });

            CmdBeginEdit = new RelayCommand(x =>
            {
                ShowMessageNoItems();
                ShowMessageLarFilter((VmRefItem)x);

                EditedItem = (VmRefItem)x;
                EditedItem.Display = EditedItem.Name + " (edit)";
                EditedItem.UpdateItemFromView();
                _editedReferenceCfg = RefCfg.CopyOnlyItems(EditedItem.Item);
                _changedRefItem = EditedItem;

                MainVm.ShowSelection = true;
                MainVm.SetElementsChecked(EditedItem.Items.ToHashSet(RefItemCfg.Eq));//apply filter

                GoToGoodTab();

                foreach (var item in Items)
                {
                    item.Ops = item == EditedItem ? VmRefItemOps.End : VmRefItemOps.None;
                }
            }, x=> true);

            CmdBeginView = new RelayCommand(x =>
            {
                ShowMessageNoItems();
                if(ShowMessageEmptyRef((VmRefItem)x))
                    return;
                ShowMessageLarFilter((VmRefItem)x);

                ViewedItem = (VmRefItem)x;
                ViewedItem.Display = ViewedItem.Name + " (view)";

                MainVm.SetElementsChecked(ViewedItem.Items.ToHashSet(RefItemCfg.Eq));
                MainVm.Selection.SetValue(VmMain.NameConditionShowOnlyCheckedElements, true); //apply filter

                GoToGoodTab();

                foreach (var item in Items)
                {
                    item.Ops = item == ViewedItem ? VmRefItemOps.End : VmRefItemOps.None;
                }
            }, x=> true);

            CmdEnd = new RelayCommand(x =>
            {
                MainVm.IsFilterActive = false;
                MainVm.Selection.SetValue(VmMain.NameConditionShowOnlyCheckedElements, false);

                if (EditedItem != null)
                {
                    RemoveFromHash(EditedItem);
                    var checksElems = MainVm.GetCheckedElements();
                    var checks = new HashSet<RefItemCfg>(RefItemCfg.Eq);
                    foreach (var e in checksElems)
                        checks.Add(e.CreateRef());
                    MainVm.ShowSelection = false;
                    EditedItem.Items = checks.ToList();
                    EditedItem.Display = EditedItem.Name;
                    AddToHash(EditedItem);
                    EditedItem.UpdateItemFromView();
                    EditedItem.IsChanged = !_editedReferenceCfg.GetItemNamesHash().SetEquals(EditedItem.Item.GetItemNamesHash());
                    _changedRefItem = EditedItem.IsChanged ? _changedRefItem : null;
                    EditedItem = null;

                    UpdateIsSyncPlanned();
                }

                if (ViewedItem != null)
                {
                    ViewedItem.Display = ViewedItem.Name;
                    ViewedItem = null;
                }

                foreach (var item in Items)
                {
                    item.Ops = VmRefItemOps.All;
                }

                MainVm.IsFilterActive = true;
                MainVm.ApplyFilter();
            }, x => true);

            CmdApply = new AsyncRelayCommand(async (moveAll2Loaded) =>
            {
                TextBoxDialog w = null;
                try
                {
                    MainVm.Progress.IsBusy = true;
                    w = TextBoxDialog.ShowProgress("Sync in progress");

                    Save();
                    var errors = await Task.Run(()=>ApplyRefs(moveAll2Loaded is bool value && value));
                    foreach (var r in Items)
                    {
                        r.IsChanged = false;
                        if (r.Items.All(x => !x.IsInArchive))
                            r.IsEnabled = true;
                        else
                            r.IsEnabled = false;
                    }
                    Save();
                    Synced = Items.Where(x=>x.IsEnabled).Select(x=>x.Name).ToHashSet();
                    SaveSyncList();

                    IsSyncPlanned = false;
                    MainVm.Progress.IsBusy = false;
                    _editedReferenceCfg = null;
                    _changedRefItem = null;
                    w.Close();
                    w = null;

                    if (errors.Any())
                    {
                        TextBoxDialog.ShowDialog("Sync completed with the following issues", errors);
                    }
                    else
                    {
                        TextBoxDialog.ShowDialog("Sync completed", "All referenced files synced successfully");
                    }
                }
                catch (Exception ex)
                {
                    Settings.Logger.LogEx(ex);
                }
                finally
                {
                    MainVm.Progress.IsBusy = false;
                    if (w != null)
                    {
                        w.Owner.IsEnabled = true;
                        w.Close();
                    }
                }
                MainVm.ToolVam.CmdScan.Execute(null);
                await MainVm.ToolVam.CmdScan.ExecutionTask;
            }, (moveAllArchive)=>
            {
                if (moveAllArchive is bool value && value)
                    return true;
                return Items.Any(x => x.IsEnabled) && IsSyncPlanned;
            });

            CmdApplyCancel = new RelayCommand(x =>
            {
                MainVm.IsFilterActive = false;
                foreach (var r in Items)
                {
                    r.IsEnabled = Synced.Contains(r.Name);
                    r.IsChanged = false;
                    if (_editedReferenceCfg != null && _changedRefItem == r)
                    {
                        r.UpdateViewFromItem(_editedReferenceCfg);
                        _editedReferenceCfg = null;
                    }
                }
                UpdateIsSyncPlanned();
                MainVm.IsFilterActive = true;
                MainVm.ApplyFilter();

            }, _ => IsSyncPlanned);

            CmdInspect = new AsyncRelayCommand(() =>
            {
                MainVm.Progress.IsBusy = true;
                Save();
                try
                {
                    var errors = new ConcurrentQueue<string>();
                    var nodes = DepsHelper.CalcNodes(DepsHelper.BuildMegaRef(UpdateAndGetRefs(), errors));
                    var str = NodeHelper.PrintTreeFromList(nodes);
                    MainVm.Progress.IsBusy = false;
                    TextBoxDialog.ShowDialog("Dependencies for all selected references", str);
                }
                catch (Exception ex)
                {
                    Settings.Logger.LogEx(ex);
                }
                finally
                {
                    MainVm.Progress.IsBusy = false;
                }
                return Task.CompletedTask;
            }, ()=>
            {
                return Items.Any(x => x.IsEnabled);
            });

            CmdValidateRefs = new RelayCommand(x =>
            {
                var invalidRefs = new List<string>();
                foreach (var r in Items)
                {
                    foreach (var item in r.Items)
                    {
                        foreach (var file in item.Files)
                        {
                            if ((item.IsInArchive && !FileHelper.FileExists(FileHelper.PathCombine(Settings.Config.VamArchivePath, file)))||
                                (!item.IsInArchive && !FileHelper.FileExists(FileHelper.PathCombine(Settings.Config.VamPath, file))))
                            {
                                invalidRefs.Add(r.Name);
                                goto ExitLoops;
                            }
                        }
                    }
                    ExitLoops: { }
                }

                var duplicates = Items
                    .GroupBy(x1 => x1.Item, new RefCfgComparer())
                    .Where(g => g.Count() > 1)
                    .Select(g => new { Key = g.Key, Count = g.Count(), Items = g.ToList() })
                    .ToList();

                if(invalidRefs.Any())
                    TextBoxDialog.ShowDialog("References validation result", 
                        new []{$"All items of active links must be located in the 'Loaded' folder, and all items of inactive links must be in the 'Archived' folder. " +
                               $"{Environment.NewLine}{Environment.NewLine}Corrupted references:"}
                            .Concat(invalidRefs));
                else if (duplicates.Any())
                {
                    string str = "";
                    int count = 0;
                    foreach (var dup in duplicates)
                    {
                        str += $"Duplicate number {++count}:";
                        if (dup.Items != null)
                        {
                            foreach (var item in dup.Items)
                            {
                                str += Environment.NewLine + item.Name;
                            }
                        }
                        str += Environment.NewLine;
                    }
                    TextBoxDialog.ShowDialog("Duplicated references:", str);
                }
                else
                    TextBoxDialog.ShowDialog("References validation", "All OK");
            }, x=> Items.Any());

            CmdClearArchive = new RelayCommand(x =>
            {
                //Override all references and disable them. Move all files from the archive to loaded (restore the previous state). Perform Clear and Scan.
                CmdApply.Execute(true);
            }, x=> CmdApply.CanExecute(true));

            Cmds.Add(new VmCmdBtn() { Caption = "Add", Command = CmdAdd, Tooltip = "Add new reference" });
            Cmds.Add(new VmCmdBtn() { Caption = "Check", Command = CmdValidateRefs, Tooltip = "Verify the location of the resources the reference points to" });
            Cmds.Add(_sync = new VmCmdBtn() { Caption = "Sync", Command = CmdApply, Tooltip = "Sync referenced items and its dependencies between Loaded and Archive", IsVisible = false });
            Cmds.Add(_syncCancel = new VmCmdBtn() { Caption = "Cancel Sync", Command = CmdApplyCancel, Tooltip = "Reset selected references to their synchronized state", IsVisible = false });
            Cmds.Add(_inspect = new VmCmdBtn() { Caption = "Inspect", Command = CmdInspect, Tooltip = "View dependencies of all checked references" });
            Cmds.Add(new VmCmdBtn() { Caption = "Unarchive All", Command = CmdClearArchive, Tooltip = "Move all items from Archive to Loaded" });
        }

        #endregion

        private void UpdateCmdsVisibility()
        {
            foreach (var item in Cmds)
                item.IsVisible = true;

            if (IsSyncPlanned)
            {
                foreach (var item in Cmds)
                {
                    if (item == _sync || item == _syncCancel || item == _inspect)
                        continue;
                    item.IsVisible = false;
                }
            }
            else
            {
                foreach (var item in Cmds)
                {
                    if (item == _sync || item == _syncCancel)
                        item.IsVisible = false;
                }
            }

            if (EditedItem != null)
            {
                foreach (var item in Cmds)
                {
                    if (item == _inspect)
                        continue;
                    item.IsVisible = false;
                }
            }

            if (ViewedItem != null)
            {
                foreach (var item in Cmds)
                {
                    if (item == _inspect)
                        continue;
                    item.IsVisible = false;
                }
            }
        }

        private void GoToGoodTab()
        {
            VmBase good = MainVm.SelectedTab;
            if (good.Count > 0 && (good == MainVm.ToolVar || good == MainVm.ToolUserData))
                return;

            good = null;
            if (MainVm.ToolVar.Count > 0)
                good = MainVm.ToolVar;
            if (good == null)
            {
                if (MainVm.ToolUserData.Count > 0)
                    good = MainVm.ToolUserData;
            }

            if (good == null)
                return;
            if (MainVm.SelectedTab != good)
                MainVm.SelectedTab = good;
        }

        private void ShowMessageNoItems()
        {
            if (MainVm.ToolVar.Count == 0 && MainVm.ToolUserData.Count == 0)
            {
                new TextBoxDialog(new[] { "No visible items", "You might want to check the Loaded/Archived/Creator/Name filters" }).ShowDialog();
            }
        }

        private void ShowMessageLarFilter(VmRefItem item)
        {
            if (item.IsEnabled)
            {
                if(MainVm.LaFilterPreds.FirstOrDefault(x=>x.IsActive && x.Name.ToLowerInvariant() == "archived") != null &&
                   MainVm.LaFilterPreds.FirstOrDefault(x=>x.IsActive && x.Name.ToLowerInvariant() == "loaded") == null)
                new TextBoxDialog(new[] { "No visible items", "You're trying to view Loaded items while the Loaded filter is turned off" }).ShowDialog();
            }
            else
            {
                if(MainVm.LaFilterPreds.FirstOrDefault(x=>x.IsActive && x.Name.ToLowerInvariant() == "archived") == null &&
                   MainVm.LaFilterPreds.FirstOrDefault(x=>x.IsActive && x.Name.ToLowerInvariant() == "loaded") != null)
                    new TextBoxDialog(new[] { "No visible items", "You're trying to view Archived items while the Archived filter is turned off" }).ShowDialog();
            }
        }

        private bool ShowMessageEmptyRef(VmRefItem item)
        {
            foreach (var x in item.Items)
                return false;
            new TextBoxDialog(new[] { "No items referenced", "You might want to link resource to reverences via Begin Edit command" }).ShowDialog();
            return true;
        }

        private bool _updateRefHash = true;
        private bool _filterOnHashUpdate = true;
        private void AddToHash(VmRefItem item)
        {
            if (!_updateRefHash)
                return;
            foreach (var f in item.Items)
            {
                foreach (var relPath in f.Files)
                    AddRefsPathHash(relPath);
            }
            if(_filterOnHashUpdate)
                MainVm.ApplyFilter();
        }

        private void RemoveFromHash(VmRefItem item)
        {
            if (!_updateRefHash)
                return;
            foreach (var f in item.Items)
            {
                foreach (var relPath in f.Files)
                    RefsRelativePathHash.Remove(relPath);
            }
            if(_filterOnHashUpdate) //lar filter depends on it
                MainVm.ApplyFilter();
        }

        private void Items_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (VmRefItem item in e.NewItems)
                {
                    AddToHash(item);
                    item.PropertyChanged += Item_PropertyChanged;
                }
            }

            if (e.OldItems != null)
            {
                foreach (VmRefItem item in e.OldItems)
                {
                    item.PropertyChanged -= Item_PropertyChanged;
                    RemoveFromHash(item);
                }
            }
        }

        private void Item_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(VmRefItem.IsEnabled))
            {
                if (sender is VmRefItem item)
                {
                    OnIsEnabledChanged(item);
                }
            }
        }

        private void OnIsEnabledChanged(VmRefItem item)
        {
            if (item.IsEnabled)
                AddToHash(item);
            else
                RemoveFromHash(item);

            UpdateIsSyncPlanned();
        }

        private void UpdateIsSyncPlanned()
        {
            IsSyncPlanned = Items.Any(x => x.IsChanged);
            if(IsSyncPlanned)
                return;

            var @checked = Items.Where(x=>x.IsEnabled).Select(x=>x.Name).ToHashSet();
            IsSyncPlanned = !Synced.SetEquals(@checked);
        }

        string GetVerbMessage(int count, string verb)
        {
            return count == 1
                ? $"1 item is {verb}:"
                : $"{count} items are {verb}:";
        }

        private List<RefCfg> UpdateAndGetRefs()
        {
            var refs = new List<RefCfg>();
            foreach (var item in Items)
            {
                item.UpdateItemFromView();
                if (item.IsEnabled)
                    refs.Add(item.Item);
            }
            return refs;
        }

        private List<string> ApplyRefs(bool moveAll2Loaded = false)
        {
            //During sync:
            //For each reference, create its dependencies in a separate thread.
            //    Merge the dependencies into one mega-dependency, removing duplicates.
            //    Sync goal: All elements from the mega-dependency must be in Loaded, and all other elements from the Loaded resource dictionary must be moved to the archive.
            //    Create two lists of relative file paths: move2loaded, move2archive.
            //    Go through the Loaded resource dictionary:

            //If the element is not in the mega-dependency, add it to move2archive.
            //    If the element is in the mega-dependency, do nothing; mark the element in the mega-dependency as processed.
            //    Go through the Archived resource dictionary:

            //If the element is not in the mega-dependency, do nothing.
            //    If the element is in the mega-dependency, add it to move2loaded; mark the element in the mega-dependency as processed.
            //    Go through the mega-dependency:

            //If the element is marked as unprocessed, add it to the list of missing dependencies.

            //move2loaded base path — archive
            //move2archive base path — loaded

            //Go through the elements in move2loaded:
            //sourcePath = archive base path + relative path
            //    destinationPath = loaded base path + relative path
            //File.Move(sourcePath, destinationPath)
            
            //Go through the elements in move2archive:
            //sourcePath = loaded base path + relative path
            //    destinationPath = archive base path + relative path
            //File.Move(sourcePath, destinationPath)
            
            //Update IsInArchive for all references and save them back to the files.

            #region build mega ref
            var errors = new ConcurrentQueue<string>();
            var megaRefAndTree = moveAll2Loaded ? DepsHelper.BuildMegaRefAllArchive() : DepsHelper.BuildMegaRef(UpdateAndGetRefs(), errors);
            var mega = megaRefAndTree.Item1;
            if(Settings.Config.CleanMega)
                CleanMega(mega);
            #endregion
            #region calculate files
            var move2loaded = new RefItemDepItem(); //rel paths
            var move2archive = new RefItemDepItem(); //rel paths
            var processed = new RefItemDepItem();
            #region loaded->archive
            foreach (var item in Settings.LoadedRd.ResourcesRelPath)
            {
                if (mega.ResourcesRelPath.Contains(item))
                    processed.AddRes(item);
                else
                    move2archive.AddRes(item);
            }
            foreach (var kvp in Settings.LoadedRd.Vars)
            {
                foreach (var kvp2 in kvp.Value.Vars)
                {
                    var varFile = kvp2.Value;
                    //There may be multiple versions of a var in the mega if CleanMega was not performed.
                    if (mega.ContainsVar(varFile.Name))
                    {
                        processed.AddVar(varFile.Name, varFile.RelativePath);
                    }
                    else
                    {
                        move2archive.AddVar(varFile.Name, varFile.RelativePath);
                    }
                }
            }
            #endregion
            #region archive->loaded
            foreach (var item in Settings.ArchiveRd.ResourcesRelPath)
            {
                if (mega.ResourcesRelPath.Contains(item))
                {
                    move2loaded.AddRes(item);
                    processed.AddRes(item);
                }
            }
            foreach (var kvp in Settings.ArchiveRd.Vars)
            {
                foreach (var var in kvp.Value.Vars)
                {
                    if (mega.ContainsVar(var.Value.Name))
                    {
                        move2loaded.AddVar(var.Value.Name, var.Value.RelativePath);
                        processed.AddVar(var.Value.Name, var.Value.RelativePath);
                    }
                }
            }
            #endregion
            #region unprocessed

            var unprocResources = new List<string>();
            var unprocVars = new List<string>();
            var missingVars = new List<string>();
            var otherVersionVars = new List<string>();
            foreach (var item in mega.ResourcesRelPath)
            {
                if (!processed.ResourcesRelPath.Contains(item))
                    unprocResources.Add(item);
            }
            foreach (var item in mega.VarsName)
            {
                var varName = item.Key;
                var relPath = item.Value;
                if (!processed.ContainsVar(varName))
                {
                    if (string.IsNullOrWhiteSpace(relPath))
                    {
                        if (processed.ContainsVarKey(varName))
                        {
                            otherVersionVars.Add(varName.FullName);
                        }
                        else
                            missingVars.Add(varName.FullName + " - Missing");
                    }
                    else
                        unprocVars.Add(varName.FullName + " - " + relPath);
                }
            }

            if (unprocResources.Any() || unprocVars.Any())
            {
                errors.Enqueue("");
                errors.Enqueue(GetVerbMessage(unprocResources.Count + unprocVars.Count, "unprocessed"));
                foreach (var item in unprocResources)
                    errors.Enqueue($"   {item}");
                foreach (var item in unprocVars)
                    errors.Enqueue($"   {item}");
            }

            if (missingVars.Any())
            {
                errors.Enqueue("");
                errors.Enqueue(GetVerbMessage(missingVars.Count, "missing"));
                foreach (var item in missingVars)
                    errors.Enqueue($"   {item}");
            }

            if (otherVersionVars.Any())
            {
                errors.Enqueue("");
                errors.Enqueue(GetVerbMessage(otherVersionVars.Count, "substituted by other version"));
                foreach (var item in otherVersionVars)
                    errors.Enqueue($"   {item}");
            }

            #endregion
            #endregion
            #region move files & update refs
            // move2loaded base path – archive
            // move2archive base path – loaded
            var vamPath = Settings.Config.VamPath;
            var archivePath = Settings.Config.VamArchivePath;
            #region -> loaded

            foreach (var item in move2loaded.ResourcesRelPath)
            {
                var sourcePath = FileHelper.PathCombine(archivePath, item);
                var destPath = FileHelper.PathCombine(vamPath, item);
                if (!FileHelper.TryMoveFile(sourcePath, destPath, out var err))
                    errors.Enqueue(err);

                foreach(var r in FindReferencedResource(item))
                    r.IsInArchive = false;
            }
            foreach (var kvp in move2loaded.VarsName)
            {
                if (kvp.Key.IsLatest)
                {
                    errors.Enqueue($"{kvp.Key} contains var reference name, not var name. Item skipped.");
                    continue;
                }
                var sourcePath = FileHelper.PathCombine(archivePath, kvp.Value);
                var destPath = FileHelper.PathCombine(vamPath, kvp.Value);
                if (!FileHelper.TryMoveFile(sourcePath, destPath, out var err))
                    errors.Enqueue(err);

                foreach(var r in FindReferencedVar(kvp.Value))
                    r.IsInArchive = false;
            }

            #endregion
            #region -> archive

            if (!moveAll2Loaded)
            {
                foreach (var item in move2archive.ResourcesRelPath)
                {
                    var sourcePath = FileHelper.PathCombine(vamPath, item);
                    var destPath = FileHelper.PathCombine(archivePath, item);
                    if (!FileHelper.TryMoveFile(sourcePath, destPath, out var err))
                        errors.Enqueue(err);

                    foreach (var r in FindReferencedResource(item))
                        r.IsInArchive = true;
                }

                foreach (var kvp in move2archive.VarsName)
                {
                    var sourcePath = FileHelper.PathCombine(vamPath, kvp.Value);
                    var destPath = FileHelper.PathCombine(archivePath, kvp.Value);
                    if (!FileHelper.TryMoveFile(sourcePath, destPath, out var err))
                        errors.Enqueue(err);

                    foreach (var r in FindReferencedVar(kvp.Value))
                        r.IsInArchive = true;
                }
            }

            #endregion
            #endregion

            return errors.ToList();
        }

        //pick max version of var
        private void CleanMega(RefItemDepItem mega)
        {
            var groups = mega.VarsName.GroupBy(x => x.Key.CreatorAndName).Where(x=>x.Count()>1);
            foreach (var g in groups)
            {
                var stay = g.OrderByDescending(x => x.Key.Version).First();
                foreach (var item in g)
                {
                    if(stay.Key != item.Key)
                        mega.RemoveVar(item.Key);
                }
            }
        }

        private IEnumerable<RefItemCfg> FindReferencedVar(string relPath)
        {
            foreach (var r in Items)
            {
                foreach (var refItem in r.Items.Where(x=>x.IsVar))
                {
                    var file = refItem.Files.First();
                    if(FileHelper.ArePathsEqual(relPath, file))
                        yield return refItem;
                }
            }
        }

        private IEnumerable<RefItemCfg> FindReferencedResource(string relPath)
        {
            foreach (var r in Items)
            {
                foreach (var refItem in r.Items.Where(x=>!x.IsVar))
                {
                    var file = refItem.Files.First();
                    if(FileHelper.ArePathsEqual(FileHelper.PathChangeExtension(file, null), FileHelper.PathChangeExtension(relPath, null)))
                        yield return refItem;
                }
            }
        }

        #region save\load

        public void Save()
        {
            var refs = new List<RefNamedCfg>();

            int index = 0;
            foreach (var r in Items)
            {
                r.UpdateItemFromView();
                r.Item.Index = index++;
                refs.Add(r.Item);
            }
            ConfigManager.SaveRefs(refs);
        }

        private void SaveSyncList()
        {
            ConfigManager.SaveSyncedRefs(Synced.ToList());
        }

        public void Load()
        {
            _updateRefHash = false;
            try
            {
                Items.Clear();
                var refs = ConfigManager.LoadRefs();
                refs.Sort((a, b) => a.Index.CompareTo(b.Index));
                foreach (var r in refs)
                    Items.Add(new VmRefItem(r));

                Synced = ConfigManager.LoadSyncedRefs().ToHashSet();
                foreach (var r in Items)
                {
                    r.IsEnabled = Synced.Contains(r.Name);
                }
            }
            finally
            {
                _updateRefHash = true;
            }

            _filterOnHashUpdate = false;
            try
            {
                foreach (var r in Items.Where(x => x.IsEnabled))
                    AddToHash(r);
            }
            finally
            {
                _filterOnHashUpdate = true;
            }
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        #endregion
    }
}
