using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Vrm.Util
{
    public static class Texts
    {
        public static string DependenciesText
        {
            get
            {
                return FileHelper.GetErFileContent("Dependencies.txt");
            }
        }

        public static string GuideText
        {
            get
            {
                return FileHelper.GetErFileContent("Guide.txt");
            }
        }
    }
}
