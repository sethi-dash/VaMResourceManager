using Vrm.Vam;

namespace Vrm.Vm
{
    public class VmTextElement : VmElementBase
    {
        private string _value;
        public string Value
        {
            get => _value;
            set => SetField(ref _value, value);
        }

        public VmTextElement(VarFile var, string value, UserItem userItem, ElementInfo ei) : base(ei, var, userItem)
        {
            Value = value;
        }
    }
}