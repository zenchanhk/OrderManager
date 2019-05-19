using AmiBroker.OrderManager;
using Sdl.MultiSelectComboBox.API;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Krs.Ats.IBNet;

namespace AmiBroker.Controllers
{
    class FTContract
    {
        public int ConId { get; set; }
        public string Symbol { get; set; }
        public string Exchange { get; set; }
        public string Currency { get; set; }
        public string LocalSymbol { get; set; }
    }
    class FTController : IController, INotifyPropertyChanged
    {

        private bool _pDummy;
        public bool Dummy
        {
            get { return _pDummy; }
            set
            {
                if (_pDummy != value)
                {
                    _pDummy = value;
                    OnPropertyChanged("Dummy");
                }
            }
        }

        public Type Type { get { return this.GetType(); } }
        public event PropertyChangedEventHandler PropertyChanged;
        public ObservableCollection<AccountInfo> Accounts { get; } = new ObservableCollection<AccountInfo>();
        public string Vendor { get; } = "FT";
        public string VendorFullName { get; } = "FuTu NiuNiu";
        public static Dictionary<string, FTContract> Contracts { get; } = new Dictionary<string, FTContract>();

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
            private set
            {
                if (_pConnectionStatus != value)
                {
                    _pConnectionStatus = value;
                    OnPropertyChanged("ConnectionStatus");
                }
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

        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }

        public BitmapImage Image { get; }
        public Size ImageSize { get; }
        public IItemGroup Group { get; set; }
        public bool IsEnabled { get; set; }

        private MainViewModel mainVM;
        public FTController(MainViewModel vm)
        {
            mainVM = vm;
            Accounts = new ObservableCollection<AccountInfo>();
            Uri uri = new Uri("pack://application:,,,/OrderManager;component/Controllers/images/ft.png");
            Image = new BitmapImage(uri);
            ImageSize = new Size(16, 16);
            Group = DefaultGroupService.GetItemGroup("FT");
        }
        public void Connect() { IsConnected = true; ConnectionStatus = "Connected"; }
        
        public Task ConnectAsync() { return new Task(() => { }); }
        public void Disconnect() { IsConnected = false; ConnectionStatus = "Disconnected"; }
        public void DisconnectByManual() { Disconnect(); }
        public async Task<List<OrderLog>> PlaceOrder(AccountInfo accountInfo, Strategy strategy, BaseOrderType orderType, 
            OrderAction orderAction, int batchNo, OrderInfo oi0, double? posSize = null, Contract security = null, 
            bool errorSuppressed = false, bool addToInfoList = true)
        {
            return null;
        }

        public void CancelOrder(int orderId)
        {

        }
        public async Task<bool> CancelOrderAsync(int orderId)
        {
            return false;
        }
    }
}
