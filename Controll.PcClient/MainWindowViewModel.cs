using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Controll.PcClient
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private ZombieViewModel selectedZombie;
        public ObservableCollection<ZombieViewModel> Zombies { get; set; }
        public ZombieViewModel SelectedZombie
        {
            get { return selectedZombie; }
            set
            {
                selectedZombie = value;
                OnPropertyChanged("SelectedZombie");
                OnPropertyChanged("SelectedZombiesAcitivites");
            }
        }
        public IEnumerable<Activity> SelectedZombiesAcitivites { get { return SelectedZombie.Activities; } } 
        public Activity SelectedActivity { get; set; }

        public string FilterText { get; set; }

        public string LoggedInUserName { get; set; }

        public MainWindowViewModel()
        {
            LoggedInUserName = "thecopy";
            Zombies = new ObservableCollection<ZombieViewModel>
                {
                    new ZombieViewModel(new ObservableCollection<Activity>()
                        {
                            new Activity()
                                {
                                    Name = "Mock Activity"
                                }
                        }, "MockedZombie", true)
                };
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
