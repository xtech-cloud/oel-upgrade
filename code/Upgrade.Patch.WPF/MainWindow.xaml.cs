using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using XTC.oelUpgrade;

namespace Upgrade.WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Patch patcher { get; set; }
        private Dictionary<string, string> patchArgs = new Dictionary<string, string>();

        private string programPath = "";
        private string programArgs = "";
        private string programWorkDir = "";
        public MainWindow()
        {
            patcher = new Patch();
            patcher.onStatus = (_progress, _tip) =>
            {
                progressbar.Value = _progress;
                tbStatus.Text = _tip;
            };
            patcher.onSuccess = () =>
            {
                tbStatus.Text = "";
                tbTip.Text = "";
                runApp();
            };
            InitializeComponent();
            loadImage();
        }

        public void RunWithArgs(string[] _args)
        {
            foreach (var arg in Environment.GetCommandLineArgs())
            {
                if (arg.StartsWith("--patch-repository="))
                {
                    patchArgs["repository"] = arg.Remove(0, "--patch-repository=".Length);
                }
                else if (arg.StartsWith("--patch-version="))
                {
                    patchArgs["version"] = arg.Remove(0, "--patch-version=".Length);
                }
                else if (arg.StartsWith("--patch-target="))
                {
                    patchArgs["target"] = arg.Remove(0, "--patch-target=".Length);
                }
                else if (arg.StartsWith("--program-path="))
                {
                    programPath = arg.Remove(0, "--program-path=".Length);
                }
                else if (arg.StartsWith("--program-workdir="))
                {
                    programWorkDir = arg.Remove(0, "--program-workdir=".Length);
                }
                else if (arg.StartsWith("--program-args="))
                {
                    programArgs = arg.Remove(0, "--program-args=".Length);
                }
            }
            patch(patchArgs);
        }

        public void RunWithConfig()
        {
            patchArgs["repository"] = "http://localhost/test.json";
            patchArgs["version"] = "1.0.0";
            patchArgs["target"] = "D:/tmp";
            programPath = "d:/mytest/OGM.exe";
            programWorkDir = "d:/mytest";
            patch(patchArgs);
        }

        private void loadImage()
        {
            Image image = new System.Windows.Controls.Image();
            image.Width = 640;
            image.Height = 320;

            try
            {
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(string.Format("pack://siteoforigin:,,,/{0}", "images/bg.jpg"), UriKind.Absolute);
                bitmap.EndInit();
                imgBg.Source = bitmap;
            }
            catch (System.Exception ex)
            {

            }
        }

        private void patch(Dictionary<string, string> _patchArgs)
        {
            Patch.Args args = new Patch.Args();
            args.repository = _patchArgs["repository"];
            args.targetDir = _patchArgs["target"];
            tbTip.Text = "正在查询";
            patcher.PullRpository(args, (_repository) =>
            {
                if (patcher.CompareVersion(_patchArgs["version"], _repository.version) < 0)
                {
                    if (Patch.Strategy.Auto.ToString() == _repository.strategy)
                    {
                        // 自动开始更新
                        tbTip.Text = "正在下载";
                        patcher.Upgrade(_repository);
                    }
                    else if (Patch.Strategy.Auto.ToString() == _repository.strategy)
                    {
                        // 显示更新提示
                    }
                    else
                    {
                        //启动应用程序
                        runApp();
                    }
                }
            }, (_error) =>
            {

            });
        }

        private async void runApp()
        {
            if (string.IsNullOrEmpty(programPath))
                return;

            await Task.Run(() =>
            {
                Thread.Sleep(1000);
            });
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = programPath;
            if (!string.IsNullOrEmpty(programArgs))
                psi.Arguments = programArgs;
            if (!string.IsNullOrEmpty(programWorkDir))
                psi.WorkingDirectory = programWorkDir;
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;
            Process.Start(psi);

            Application.Current.MainWindow.Close();
        }

    }
}
