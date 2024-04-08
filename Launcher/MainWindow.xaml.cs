using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
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


namespace Launcher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    

    public partial class MainWindow : Window
    {
        
    public MainWindow()
        {
            InitializeComponent();
        }
        private LauncherLogic brain = new LauncherLogic();
        public void change_progressbar(int i)
        {
            progress_bar.Value = i;
        }
        private async void updateRoutine()
        {
            Dispatcher.Invoke((Action)(() =>
            {
                
                progress_bar.Maximum = brain.CalculateUpdateSize();
            }));
            
            Task update = Task.Run(() => brain.UpdateGame());
            while (true)
            {
                Dispatcher.Invoke((Action)(() =>
                {
                    change_progressbar(brain.updateDownlodedSize);
                }));
                if (brain.updateDone) break;
            }
            await update;
        }
        public async void button1_click(object sender, RoutedEventArgs a)
        {
            button_1.IsEnabled = false;
            
            if (brain.CheckVersion())
            {
                progress_bar.Value = 0;
                await Task.Run(() => updateRoutine());
                brain.Cleanup();
            }
            button_1.IsEnabled = true;
            try
            {
                Process.Start("Game Data\\Eternal Rifts.exe");
            }
            catch(Win32Exception ex)
            {

            }
            Application.Current.Shutdown();
        }

    }

}
