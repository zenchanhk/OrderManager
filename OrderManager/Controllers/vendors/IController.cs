using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Sdl.MultiSelectComboBox.API;
using System.Windows.Media.Imaging;
using System.Drawing;
using System.Collections.Specialized;
using System.Runtime.Serialization;
using AmiBroker.OrderManager;
using Newtonsoft.Json;
using Krs.Ats.IBNet;

namespace AmiBroker.Controllers
{
    public enum AccountStatus
    {
        None=1,
        BuyPending=2,
        Long=4,
        ShortPending=8,
        Short=16,
        SellPending=32,
        CoverPending=64
    }
    public class AccountStatusOp
    {
        //private readonly static List<string> PendingStatus = ["PreSubmitted"];
        public static void RevertActionStatus(ref BaseStat strategyStat, OrderAction orderAction, bool cancelled = false)
        {
            if (orderAction == OrderAction.Buy)
            {
                strategyStat.AccountStatus &= ~AccountStatus.BuyPending;
            }
            else if (orderAction == OrderAction.Short)
            {
                strategyStat.AccountStatus &= ~AccountStatus.ShortPending;
            }
            else if (orderAction == OrderAction.Sell)
            {
                if (strategyStat.LongPosition == 0)
                    strategyStat.AccountStatus &= ~AccountStatus.SellPending;
                else
                {
                    foreach (OrderInfo orderInfo in strategyStat.OrderInfos[orderAction])
                    {
                        // 1. remaining > 0
                        // 2. no error
                        // 3. not cancelled
                        if (orderInfo.PosSize > orderInfo.Filled && string.IsNullOrEmpty(orderInfo.Error) && !cancelled)
                            return;
                    }
                    strategyStat.AccountStatus &= ~AccountStatus.SellPending;
                }
            }
            else if (orderAction == OrderAction.Cover)
            {
                if (strategyStat.ShortPosition == 0)
                    strategyStat.AccountStatus &= ~AccountStatus.CoverPending;
                else
                {
                    foreach (OrderInfo orderInfo in strategyStat.OrderInfos[orderAction])
                    {
                        // 1. remaining > 0
                        // 2. no error
                        // 3. not cancelled
                        if (orderInfo.PosSize > orderInfo.Filled && string.IsNullOrEmpty(orderInfo.Error) && !cancelled)
                            return;
                    }
                    strategyStat.AccountStatus &= ~AccountStatus.CoverPending;
                }
            }
        }

        public static void SetActionStatus(ref BaseStat strategyStat, OrderAction orderAction)
        {
            if (orderAction == OrderAction.Buy)
            {
                strategyStat.AccountStatus |= AccountStatus.BuyPending;
            }
            else if (orderAction == OrderAction.Short)
            {
                strategyStat.AccountStatus |= AccountStatus.ShortPending;
            }
            else if (orderAction == OrderAction.Sell)
            {
                strategyStat.AccountStatus |= AccountStatus.SellPending;
            }
            else if (orderAction == OrderAction.Cover)
            {
                strategyStat.AccountStatus |= AccountStatus.CoverPending;
            }
        }
        public static void SetPositionStatus(ref BaseStat strategyStat, OrderAction orderAction)
        {
            if (orderAction == OrderAction.Buy)
            {
                strategyStat.AccountStatus |= AccountStatus.Long;
            }
            else if (orderAction == OrderAction.Short)
            {
                strategyStat.AccountStatus |= AccountStatus.Short;
            }
            else if (orderAction == OrderAction.Sell && strategyStat.LongPosition == 0)
            {
                strategyStat.AccountStatus &= ~AccountStatus.Long;
            }
            else if (orderAction == OrderAction.Cover && strategyStat.ShortPosition == 0)
            {
                strategyStat.AccountStatus &= ~AccountStatus.Short;
            }
        }
        public static void SetAttemps(ref BaseStat strategyStat, OrderAction orderAction)
        {
            if (orderAction == OrderAction.Buy)
            {
                strategyStat.LongAttemps++;
            }
            if (orderAction == OrderAction.Short)
            {
                strategyStat.ShortAttemps++;
            }
        }
    }
    public class AccountTag : INotifyPropertyChanged
    {
        public Type Type { get; }
           
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }

        public string Tag { get; set; }
        public string Currency { get; set; }
        
        private string _pValue;
        public string Value
        {
            get { return _pValue; }
            set
            {
                if (_pValue != value)
                {
                    _pValue = value;
                    OnPropertyChanged("Value");
                }
            }
        }

    }
    
    public class AccountInfo : IItemGroupAware, IItemEnabledAware, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }        
        public string Name { get; private set; }        
        public IController Controller { get; private set; }
        [JsonIgnore]
        public ObservableCollection<AccountTag> Properties { get; set; } = new ObservableCollection<AccountTag>();
        // properties for use in Multi-select combox
        public IItemGroup Group { get; set; }
        public BitmapImage Image { get; private set; }
        public Size ImageSize { get; private set; }
        public bool IsEnabled { get; set; } = true;

        private AccountTag _pTotalCashValue;
        public AccountTag TotalCashValue
        {
            get { return _pTotalCashValue; }
            set
            {
                if (_pTotalCashValue != value)
                {
                    _pTotalCashValue = value;
                    OnPropertyChanged("TotalCashValue");
                }
            }
        }

        //
        public AccountInfo(string name, IController controller)
        {
            Name = name;
            Controller = controller;
            string vendor = controller.Vendor;
            Uri uri = new Uri("pack://application:,,,/OrderManager;component/Controllers/images/"+vendor+".png");
            Image = new BitmapImage(uri);
            ImageSize = new Size(16, 16);
            Group = DefaultGroupService.GetItemGroup(vendor);
            Properties.CollectionChanged += Properties_CollectionChanged;
        }

        private void Properties_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (AccountTag tag in e.NewItems)
                {
                    if (tag.Tag.ToLower() == "totalcashvalue")
                    {
                        TotalCashValue = tag;
                    }
                }
            }
        }

        public override string ToString()
        {
            return Name;
        }
    }
    public interface IController : IItemGroupAware, IItemEnabledAware
    {
        // IB can have more than one linked account (Finacial Advisor Account and sub accounts)
        ObservableCollection<AccountInfo> Accounts { get; }
        AccountInfo SelectedAccount { get; set; }
        // should be unique
        string DisplayName { get; }
        string Vendor { get; }  //short name
        string VendorFullName { get; }
        ConnectionParam ConnParam { get; set; }
        bool IsConnected { get; }
        string ConnectionStatus { get; }
        void Connect();
        void Disconnect();

        // properties for use in Multi-select combox
        BitmapImage Image { get; }
        Size ImageSize { get; }
        bool Dummy { get; set; }    // used in listview in account selecting section
        Task<List<OrderLog>> PlaceOrder(AccountInfo accountInfo, Strategy strategy, BaseOrderType orderType, OrderAction orderAction, int barIndex, int batchNo, double? posSize = null, Contract security = null, bool errorSuppress = false, bool addToInfoList = true);
        void CancelOrder(int orderId);
        Task<bool> CancelOrderAsync(int orderId);
    }
}
