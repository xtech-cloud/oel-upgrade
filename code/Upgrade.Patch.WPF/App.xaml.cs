using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Upgrade.WPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            MainWindow mainWindow = new MainWindow();
            if (Environment.GetCommandLineArgs().Length > 1)
            {
                mainWindow.RunWithArgs(Environment.GetCommandLineArgs());
            }
            else
            {
                mainWindow.RunWithConfig();
            }

            mainWindow.Show();
        }
    }
}
