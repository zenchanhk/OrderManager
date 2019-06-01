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
        CoverPending=64,
        APSLongActivated=128, // adaptive profit stop activated
        APSShortActivated = 256, // adaptive profit stop activated
        StoplossLongActivated = 512,
        StoplossShortActivated = 1024,
        PreForceExitLongActivated = 2048,
        PreForceExitShortActivated = 4096,
        FinalForceExitLongActivated = 8192,
        FinalForceExitShortActivated = 16384,
    }
    public class AccountStatusOp
    {
        //private readonly static List<string> PendingStatus = ["PreSubmitted"];
        private readonly static OrderAction[] ShortExitAction = { OrderAction.APSShort, OrderAction.FinalForceExitShort,
            OrderAction.Cover, OrderAction.PreForceExitShort, OrderAction.StoplossShort };
        private readonly static OrderAction[] LongExitAction = { OrderAction.APSLong, OrderAction.FinalForceExitLong,
            OrderAction.Sell, OrderAction.PreForceExitLong, OrderAction.StoplossLong };

        public static void RevertActionStatus(ref BaseStat strategyStat, ref BaseStat scriptStat, string strategyName, OrderAction orderAction, bool cancelled = false)
        {
            if (orderAction == OrderAction.Buy)
            {
                strategyStat.AccountStatus &= ~AccountStatus.BuyPending;
                scriptStat.LongPendingStrategies.Remove(strategyName);
            }
            else if (orderAction == OrderAction.Short)
            {
                strategyStat.AccountStatus &= ~AccountStatus.ShortPending;
                scriptStat.ShortPendingStrategies.Remove(strategyName);
            }
            else if (orderAction == OrderAction.Sell)
            {
                if (strategyStat.LongPosition == 0)
                {
                    strategyStat.AccountStatus &= ~AccountStatus.SellPending;
                }
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
            else if (orderAction == OrderAction.APSLong)
            {
                strategyStat.AccountStatus &= ~AccountStatus.APSLongActivated;
            }
            else if (orderAction == OrderAction.APSShort)
            {
                strategyStat.AccountStatus &= ~AccountStatus.APSShortActivated;
            }
            else if (orderAction == OrderAction.StoplossLong)
            {
                strategyStat.AccountStatus &= ~AccountStatus.StoplossLongActivated;
            }
            else if (orderAction == OrderAction.StoplossShort)
            {
                strategyStat.AccountStatus &= ~AccountStatus.StoplossShortActivated;
            }
            else if (orderAction == OrderAction.PreForceExitLong)
            {
                strategyStat.AccountStatus &= ~AccountStatus.PreForceExitLongActivated;
            }
            else if (orderAction == OrderAction.PreForceExitShort)
            {
                strategyStat.AccountStatus &= ~AccountStatus.PreForceExitShortActivated;
            }
            else if (orderAction == OrderAction.FinalForceExitLong)
            {
                strategyStat.AccountStatus &= ~AccountStatus.FinalForceExitLongActivated;
            }
            else if (orderAction == OrderAction.FinalForceExitShort)
            {
                strategyStat.AccountStatus &= ~AccountStatus.FinalForceExitShortActivated;
            }
        }

        // set initial status of OrderAction
        public static void SetActionStatus(ref BaseStat strategyStat, ref BaseStat scriptStat, string strategyName, OrderAction orderAction)
        {
            if (orderAction == OrderAction.Buy)
            {
                strategyStat.AccountStatus |= AccountStatus.BuyPending;
                scriptStat.LongPendingStrategies.Add(strategyName);
            }
            else if (orderAction == OrderAction.Short)
            {
                strategyStat.AccountStatus |= AccountStatus.ShortPending;
                scriptStat.ShortPendingStrategies.Add(strategyName);
            }
            else if (orderAction == OrderAction.Sell)
            {
                strategyStat.AccountStatus |= AccountStatus.SellPending;
            }
            else if (orderAction == OrderAction.Cover)
            {
                strategyStat.AccountStatus |= AccountStatus.CoverPending;
            }
            else if (orderAction == OrderAction.APSLong)
            {
                strategyStat.AccountStatus |= AccountStatus.APSLongActivated;
            }
            else if (orderAction == OrderAction.APSShort)
            {
                strategyStat.AccountStatus |= AccountStatus.APSShortActivated;
            }
            else if (orderAction == OrderAction.StoplossLong)
            {
                strategyStat.AccountStatus |= AccountStatus.StoplossLongActivated;
            }
            else if (orderAction == OrderAction.StoplossShort)
            {
                strategyStat.AccountStatus |= AccountStatus.StoplossShortActivated;
            }
            else if (orderAction == OrderAction.PreForceExitLong)
            {
                strategyStat.AccountStatus |= AccountStatus.PreForceExitLongActivated;
            }
            else if (orderAction == OrderAction.PreForceExitShort)
            {
                strategyStat.AccountStatus |= AccountStatus.PreForceExitShortActivated;
            }
            else if (orderAction == OrderAction.FinalForceExitLong)
            {
                strategyStat.AccountStatus |= AccountStatus.FinalForceExitLongActivated;
            }
            else if (orderAction == OrderAction.FinalForceExitShort)
            {
                strategyStat.AccountStatus |= AccountStatus.FinalForceExitShortActivated;
            }
        }
        public static void SetPositionStatus(ref BaseStat strategyStat, ref BaseStat scriptStat, ref Strategy strategy, OrderAction orderAction)
        {
            if (orderAction == OrderAction.Buy)
            {
                strategyStat.AccountStatus |= AccountStatus.Long;
                scriptStat.LongStrategies.Add(strategy.Name);
            }
            else if (orderAction == OrderAction.Short)
            {
                strategyStat.AccountStatus |= AccountStatus.Short;
                scriptStat.ShortStrategies.Add(strategy.Name);
            }
            else if (orderAction == OrderAction.Sell && strategyStat.LongPosition == 0)
            {
                strategyStat.AccountStatus &= ~AccountStatus.Long;                
            }
            else if (orderAction == OrderAction.Cover && strategyStat.ShortPosition == 0)
            {
                strategyStat.AccountStatus &= ~AccountStatus.Short;                
            }
            else if (orderAction == OrderAction.APSLong && strategyStat.LongPosition == 0)
            {
                strategyStat.AccountStatus &= ~AccountStatus.APSLongActivated;
            }
            else if (orderAction == OrderAction.APSShort && strategyStat.ShortPosition == 0)
            {
                strategyStat.AccountStatus &= ~AccountStatus.APSShortActivated;
            }
            else if (orderAction == OrderAction.StoplossLong && strategyStat.LongPosition == 0)
            {
                strategyStat.AccountStatus &= ~AccountStatus.StoplossLongActivated;
            }
            else if (orderAction == OrderAction.StoplossShort && strategyStat.ShortPosition == 0)
            {
                strategyStat.AccountStatus &= ~AccountStatus.StoplossShortActivated;
            }
            else if (orderAction == OrderAction.PreForceExitLong && strategyStat.LongPosition == 0)
            {
                strategyStat.AccountStatus &= ~AccountStatus.PreForceExitLongActivated;
            }
            else if (orderAction == OrderAction.PreForceExitShort && strategyStat.ShortPosition == 0)
            {
                strategyStat.AccountStatus &= ~AccountStatus.PreForceExitShortActivated;
            }
            else if (orderAction == OrderAction.FinalForceExitLong && strategyStat.LongPosition == 0)
            {
                strategyStat.AccountStatus &= ~AccountStatus.FinalForceExitLongActivated;
            }
            else if (orderAction == OrderAction.FinalForceExitShort && strategyStat.ShortPosition == 0)
            {
                strategyStat.AccountStatus &= ~AccountStatus.FinalForceExitShortActivated;                
            }

            string msg = string.Empty;
            if (strategyStat.LongPosition == 0 && LongExitAction.Contains(orderAction))
            {
                /*
                strategyStat.OrderInfos[OrderAction.APSLong].Clear();
                strategyStat.OrderInfos[OrderAction.StoplossLong].Clear();
                strategyStat.OrderInfos[OrderAction.Buy].Clear();
                strategyStat.OrderInfos[OrderAction.Sell].Clear();
                strategyStat.OrderInfos[OrderAction.PreForceExitLong].Clear();
                strategyStat.OrderInfos[OrderAction.FinalForceExitLong].Clear();*/
                scriptStat.LongStrategies.Remove(strategy.Name);

                strategyStat.AccountStatus &= ~AccountStatus.Long;
                strategy.ForceExitOrderForLong.Reset();
                strategy.AdaptiveProfitStopforLong.Reset();

                msg = "Long cleared, OrderAction:" + orderAction.ToString();
            }

            if (strategyStat.ShortPosition == 0 && ShortExitAction.Contains(orderAction))
            {
                /*
                strategyStat.OrderInfos[OrderAction.APSShort].Clear();
                strategyStat.OrderInfos[OrderAction.StoplossShort].Clear();
                strategyStat.OrderInfos[OrderAction.Short].Clear();
                strategyStat.OrderInfos[OrderAction.Cover].Clear();
                strategyStat.OrderInfos[OrderAction.PreForceExitShort].Clear();
                strategyStat.OrderInfos[OrderAction.FinalForceExitShort].Clear();*/
                scriptStat.ShortStrategies.Remove(strategy.Name);

                strategyStat.AccountStatus &= ~AccountStatus.Short;
                strategy.ForceExitOrderForShort.Reset();
                strategy.AdaptiveProfitStopforShort.Reset();

                msg = "Short cleared, OrderAction:" + orderAction.ToString();
            }

            if (!string.IsNullOrEmpty(msg))
            {
                MainViewModel.Instance.Log(new Log
                {
                    Text = msg,
                    Time = DateTime.Now,
                    Source = strategy.Symbol.Name + "." + strategy.Name
                });
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

    /*
    public class OrderIdCounter
    {
        public static object LockObject { get; } = new object();

        private static int _orderId = 0;
        public static int OrderId {
            get { return _orderId++; }
            set { _orderId = value; }
        }
    }*/
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
        bool ModifyOrder(AccountInfo accountInfo, Strategy strategy, OrderAction orderAction, BaseOrderType orderType);
        Task<List<OrderLog>> PlaceOrder(AccountInfo accountInfo, Strategy strategy, BaseOrderType orderType, OrderAction orderAction, int batchNo, OrderInfo oi = null, double? posSize = null, Contract security = null, bool errorSuppress = false, bool addToInfoList = true);
        void CancelOrder(int orderId);
        Task<bool> CancelOrderAsync(int orderId);
    }
}
