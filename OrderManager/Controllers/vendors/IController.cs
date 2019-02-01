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
        public static void RevertActionStatus(ref BaseStat stat, OrderAction orderAction)
        {
            if (orderAction == OrderAction.Buy)
            {
                stat.AccoutStatus &= ~AccountStatus.BuyPending;
            }
            else if (orderAction == OrderAction.Short)
            {
                stat.AccoutStatus &= ~AccountStatus.ShortPending;
            }
            else if (orderAction == OrderAction.Sell)
            {
                stat.AccoutStatus &= ~AccountStatus.SellPending;
            }
            else if (orderAction == OrderAction.Cover)
            {
                stat.AccoutStatus &= ~AccountStatus.CoverPending;
            }
        }
        public static void SetActionStatus(ref BaseStat stat, OrderAction orderAction)
        {
            if (orderAction == OrderAction.Buy)
            {
                stat.AccoutStatus |= AccountStatus.BuyPending;
            }
            else if (orderAction == OrderAction.Short)
            {
                stat.AccoutStatus |= AccountStatus.ShortPending;
            }
            else if (orderAction == OrderAction.Sell)
            {
                stat.AccoutStatus |= AccountStatus.SellPending;
            }
            else if (orderAction == OrderAction.Cover)
            {
                stat.AccoutStatus |= AccountStatus.CoverPending;
            }
        }
        public static void SetPositionStatus(ref BaseStat stat, OrderAction orderAction)
        {
            if (orderAction == OrderAction.Buy)
            {
                stat.AccoutStatus |= AccountStatus.Long;
            }
            else if (orderAction == OrderAction.Short)
            {
                stat.AccoutStatus |= AccountStatus.Short;
            }
            else if (orderAction == OrderAction.Sell && stat.LongPosition == 0)
            {
                stat.AccoutStatus &= ~AccountStatus.Long;
            }
            else if (orderAction == OrderAction.Cover && stat.ShortPosition == 0)
            {
                stat.AccoutStatus &= ~AccountStatus.Short;
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
        ConnectionParam ConnParam { get; set; }
        bool IsConnected { get; }
        string ConnectionStatus { get; }
        void Connect();
        void Disconnect();

        // properties for use in Multi-select combox
        BitmapImage Image { get; }
        Size ImageSize { get; }
        bool Dummy { get; set; }    // used in listview in account selecting section
        Task<OrderLog> PlaceOrder(AccountInfo accountInfo, Strategy strategy, string symbol, BaseOrderType orderType, OrderAction orderAction, int barIndex);
    }
}
