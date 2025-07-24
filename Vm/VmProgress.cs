namespace Vrm.Vm
{
    public class VmProgress : VmBase
    {
        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set => SetField(ref _isBusy, value);
        }
    }
}
