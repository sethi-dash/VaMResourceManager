using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vrm.Util
{
    public static class TaskHelper
    {
        public static void NoWarning(this Task task)
        {
            task.ContinueWith(t =>
            {
                var _ = t.Exception;
            }, TaskContinuationOptions.OnlyOnFaulted);
        }
    }
}
