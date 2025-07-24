using System;
using System.Windows;
using Vrm.Util;
using Vrm.Window;

namespace Vrm
{
    public partial class App : Application
    {
        private void App_Startup(object sender, StartupEventArgs e)
        {
            try
            {
                UiHelper.Initialize();
                Settings.Load();
                FileHelper.CreateDirectoryInNotExists(Settings.Config.CachePath);
                FileHelper.CreateDirectoryInNotExists(Settings.Config.ReferenceFolder);
                new CustomWindow().Show();
            }
            catch (Exception ex)
            {
                Settings.Logger.LogEx(ex);
            }
        }

        private void App_Exit(object sender, ExitEventArgs e)
        {
            Settings.Save();
        }
    }
}
