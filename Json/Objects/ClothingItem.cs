using System.Linq;
using Vrm.Vam;

namespace Vrm.Json.Objects
{
    public class ClothingItem
    {
        public string id { get; set; }
        public string enabled { get; set; }

        public VarName Var
        {
            get
            {
                if (id.Split(':').Count() == 2)
                    return VarName.Parse(id.Split(':')[0]);
                else
                    return VarName.Null;
            }
        }
    }

//    { 
//    "id" : "Shorts", 
//    "enabled" : "false"
//}, 
//{ 
//    "id" : "YameteOuji.Under_BraPanty_SetA01_P.2:/Custom/Clothing/Female/YameteOuji/YameteOuji_Under_PantyA/Under_PantyA.vam", 
//    "internalId" : "YameteOuji:Under_PantyA", 
//    "enabled" : "true"
//}, 
}
