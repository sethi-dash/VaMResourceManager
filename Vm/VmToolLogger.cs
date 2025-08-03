using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Vrm.Util;

namespace Vrm.Vm
{
    public class VmToolLogger : VmBase, ILogger
    {
        public ObservableCollection<string> LogMessages { get; } = new ObservableCollection<string>();

        private int _errCount;
        public int ErrCount
        {
            get => _errCount;
            set
            {
                if (SetField(ref _errCount, value))
                {
                    Name = $"log {ErrCount}|{MsgCount}";
                }
            }
        }

        private int _msgCount;
        public int MsgCount
        {
            get => _msgCount;
            set
            {
                if (SetField(ref _msgCount, value))
                {
                    Name = $"log {ErrCount}|{MsgCount}";
                }
            }
        }

        public VmToolLogger()
        {
            Name = "log";
            CmdClear = new RelayCommand(_ => Clear(), _ => LogMessages.Any());
            CmdSaveLogToFile = new RelayCommand(x =>
            {
                var path = FileHelper.PathCombine(Settings.ExePath, "log.txt");
                FileHelper.WriteAllLines(path, LogMessages.ToArray());
                FileHelper.Run(path);
            });
        }

        public override IEnumerable<VmCmdBtn> GetCmds()
        {
            yield return new VmCmdBtn(CmdClear, "Clear Log");
            yield return new VmCmdBtn(CmdSaveLogToFile, "Save to file");
        }

        public override void OnUpdateTools(ShowTools tools)
        {
            base.OnUpdateTools(tools);
            tools.UpdateAll(false);
            tools.Invoke();
        }

        public void Clear()
        {
            LogMessages.Clear();
            ErrCount = 0;
            MsgCount = 0;
        }

        public void LogEx(Exception ex)
        {
            LogErr(ex.GetBaseException().Message);
        }

        public void LogErr(string text)
        {
            UiHelper.InvokeAsync(() =>
            {
                LogMessages.Add($"{DateTime.Now:yyyy-MM-ddTHH:mm:ss.fff} [err]: {text}");
                ErrCount += 1;
            });
        }

        public void LogMsg(string text)
        {
            UiHelper.InvokeAsync(() =>
            {
                LogMessages.Add($"{DateTime.Now:yyyy-MM-ddTHH:mm:ss.fff} [msg]: {text}");
                MsgCount += 1;
            });

        }

        public ICommand CmdClear { get; }
        public ICommand CmdSaveLogToFile { get; }
    }
}
