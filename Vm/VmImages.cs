using Vrm.Vam;

namespace Vrm.Vm
{
    public class VmImages : VmElements
    {
        public override bool OnAdd(FolderType type, ElementInfo el, VarFile var)
        {
            if (type == Type && el.IsImage)
            {
                if (ShowWithoutPresets && el.Exts.Contains(Ext.Preset)) //.vap
                    return false;

                Items.Add(new VmImageElement(var, el.ImgLocalPath, null, el));
                return true;
            }
            return false;
        }

        public override void OnAdd(UserItem item)
        {
            var type = item.Type;
            var el = item.Info;

            if (type == Type && el.IsImage)
            {
                Items.Add(new VmImageElement(null, el.ImgLocalPath, item, el));
            }
        }
    }
}