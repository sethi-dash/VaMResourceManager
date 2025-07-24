using Vrm.Vam;

namespace Vrm.Vm
{
    public class VmImageElement : VmElementBase
    {
        public string ImagePath { get; set; } //local cached path

        public VmImageElement(VarFile var, string imagePath, UserItem userItem, ElementInfo ei) : base(ei, var, userItem)
        {
            ImagePath = imagePath;
        }
    }
}