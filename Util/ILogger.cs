using System;

namespace Vrm.Util
{
    public interface ILogger
    {
        void LogEx(Exception ex);
        void LogErr(string text);
        void LogMsg(string text);
    }
}
