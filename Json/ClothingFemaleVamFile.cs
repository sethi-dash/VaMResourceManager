using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vrm.Json
{
    internal class ClothingFemaleVamFile
    {
        public string itemType { get; set; } = "ClothingFemale";
        public string uid { get; set; }
        public string displayName { get; set; } = "dispName";
        public string creatorName { get; set; }
        public string tags { get; set; } = "";
        public string isRealItem { get; set; } = "true";


        public ClothingFemaleVamFile(string uid, string creatorName, string displayName = "")
        {
            this.uid = uid;
            this.creatorName = creatorName;
            this.displayName = displayName;
        }
    }
}
