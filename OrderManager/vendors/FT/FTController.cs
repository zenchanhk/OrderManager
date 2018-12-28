using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AmiBroker.Controllers
{
    class FTController : IController, INotifyPropertyChanged
    {
        public Type Type { get { return this.GetType(); } }
        public event PropertyChangedEventHandler PropertyChanged;
        public ObservableCollection<AccountInfo> Accounts { get; }

        private ConnectionParam _pConnParam;
        public ConnectionParam ConnParam
        {
            get { return _pConnParam; }
            set
            {
                if (_pConnParam != value)
                {
                    _pConnParam = value;
                    DisplayName = "FT(" + value.AccName + ")";
                    OnPropertyChanged("ConnParam");
                }
            }
        }

        public bool IsConnected { get; private set; } = false;

        private string _pName;
        public string DisplayName
        {
            get { return _pName; }
            set
            {
                if (_pName != value)
                {
                    _pName = value;
                    OnPropertyChanged("DisplayName");
                }
            }
        }

        private string _pConnectionStatus = "Disconnected";
        public string ConnectionStatus
        {
            get { return _pConnectionStatus; }
            set
            {
                if (_pConnectionStatus != value)
                {
                    _pConnectionStatus = value;
                    OnPropertyChanged("ConnectionStatus");
                }
            }
        }
        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }

        private AccountInfo _pSelectedAccount;
        public AccountInfo SelectedAccount
        {
            get { return _pSelectedAccount; }
            set
            {
                if (_pSelectedAccount != value)
                {
                    _pSelectedAccount = value;
                    OnPropertyChanged("SelectedAccount");
                }
            }
        }

        private MainWindow mainWin;
        public FTController(MainWindow mw)
        {
            mainWin = mw;
        }
        public void Connect() { IsConnected = true; }
        public Task ConnectAsync() { return new Task(() => { }); }
        public void Disconnect() { IsConnected = false; }

    }
}
