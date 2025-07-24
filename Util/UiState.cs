using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vrm.Util
{
    public class UIState : INotifyPropertyChanged
    {
        public static UIState Instance { get; } = new UIState();

        static UIState()
        {
        }


        private double _imgSize= Settings.Config.ImageSize;
        public double ImgSize
        {
            get => _imgSize;
            set
            {
                if (Math.Abs(_imgSize - value) > double.Epsilon)
                {
                    _imgSize = value;
                    Settings.Config.ImageSize = value;
                    OnPropertyChanged(nameof(ImgSize));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
