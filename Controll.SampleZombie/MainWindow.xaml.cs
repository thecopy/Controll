using System;
using System.Collections.Generic;
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

namespace Controll.SampleZombie
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainWindowViewModel vm { get { return (MainWindowViewModel) this.DataContext; } }
        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = new MainWindowViewModel();
        }

        private void Connect(object sender, RoutedEventArgs e)
        {
            vm.Connect("Erik", "zombieTest");
        }

        private void LogOn(object sender, RoutedEventArgs e)
        {
            vm.LogOn("password");
        }

        private void GetAllActivities(object sender, RoutedEventArgs e)
        {
            vm.GetAllActivities();
        }
        
        private void DownloadSpotifyRemote(object sender, RoutedEventArgs e)
        {
            vm.DownloadActivity(Guid.Parse("27611FAD-17CD-463B-A179-796F3E3B1120"));
        }

        private void ListAllInstalledActivities(object sender, RoutedEventArgs e)
        {
            vm.ListAllInstalledActivities();
        }

        private void PrintStatus(object sender, RoutedEventArgs e)
        {
            vm.PrintStatus();
        }
    }
}
