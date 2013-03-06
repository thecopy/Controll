using System;
using System.Windows;
using System.Windows.Controls;

namespace Controll.SampleClient
{
    public partial class MainWindow
    {
        private MainWindowViewModel vm { get { return (MainWindowViewModel) this.DataContext; } }
        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = new MainWindowViewModel();
        }

        private void ConnectButtonClicked(object sender, System.Windows.RoutedEventArgs e)
        {
            Button b = e.OriginalSource as Button;

            if(b != null)
            if(b.Content.ToString().Contains("thecopy.mooo.com"))
                vm.Connect("http://thecopy.mooo.com:10244");
            else if (b.Content.ToString().Contains("localhost"))
                vm.Connect("http://localhost:10244");
            else
            {
                MessageBox.Show("hjehe+");
            }

        }

        private void LogOnButtonClicked(object sender, System.Windows.RoutedEventArgs e)
        {
            vm.LogOn("erik", "password", Guid.NewGuid());
        }

        private void RegisterButtonClicked(object sender, System.Windows.RoutedEventArgs e)
        {
            vm.Register("erik", "password");
        }

        private void GetConnectedClientsClicked(object sender, System.Windows.RoutedEventArgs e)
        {
            vm.GetConnectedClients();
        }

        private readonly Guid SpotifyActivity = Guid.Parse("27611FAD-17CD-463B-A179-796F3E3B1120");
        private readonly string zombieName = "zombieTest";


        private void ActivateDoNothingClicked(object sender, System.Windows.RoutedEventArgs e)
        {
            vm.ActivateZombie(zombieName, SpotifyActivity, null/*vm.Parameter*/);
        }

        private void DownloadActivity(object sender, RoutedEventArgs e)
        {
            vm.DownloadActivityAtZombie("zombieTest", Guid.Parse(vm.KeyToDownload));
        }

        private void GetAvaiableActivities(object sender, RoutedEventArgs e)
        {
            vm.PrintAllAvaiableActivities();
        }

        private void PrintActivitesInstalledOnZombie(object sender, RoutedEventArgs e)
        {
            vm.PrintActivitesInstalledOnZombie("zombieTest");
        }
    }
}
