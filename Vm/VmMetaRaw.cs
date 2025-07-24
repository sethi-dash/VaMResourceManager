using System;
using Vrm.Vam;

namespace Vrm.Vm
{
    public class VmMetaRaw : VmBase
    {
        private string _metaJson;
        public string MetaJson
        {
            get => _metaJson;
            set
            {
                if (SetField(ref _metaJson, value))
                {
                }
            }
        }

        private void Update()
        {
            if (SelectedItem != null && SelectedItem.IsVar)
                MetaJson = SelectedItem.Var.Info.FullName + Environment.NewLine + "-------------------------" + Environment.NewLine + SelectedItem.Var.MetaJson;
            else
            {
                MetaJson = "No .var selected";
            }
        }

        public override void OnShow()
        {
            base.OnShow();

            Update();
        }

        public override void OnHide()
        {
            base.OnHide();
        }

        protected override void OnSelectedItemChanged()
        {
            Update();
        }

        public VmMetaRaw()
        {
            Name = "Meta.json";
        }
    }
}
