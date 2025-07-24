using Vrm.Vam;

namespace Vrm.Vm
{
    public class VmTexts : VmElements
    {
        public override bool OnAdd(FolderType type, ElementInfo el, VarFile var)
        {
            if (type == Type && el.IsText)
            {
                Items.Add(new VmTextElement(var, el.Name, null, el));
                return true;
            }
            return false;
        }

        public override void OnAdd(UserItem item)
        {
            var type = item.Type;
            var el = item.Info;
            if (type == Type && el.IsText)
            {
                Items.Add(new VmTextElement(null, el.Name, item, el));
            }
        }

        public override void OnAdd(VarFile var)
        {
            var str = var.Name.ToString();
            var el = new VmTextElement(var, str, null, var.Info);
            Items.Add(el);
        }
    }
}