using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.Windows.Data;
using System.Windows.Controls;
using AmiBroker.OrderManager;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Collections.Specialized;
using System.Windows.Threading;
using System.Windows;

namespace AmiBroker.Controllers
{
    public class TimeZone
    {
        public string Id { get; set; }
        public TimeSpan UtcOffset { get; set; }
        public string Description { get; set; }
        public override string ToString()
        {
            float offset = UtcOffset.Hours + UtcOffset.Minutes / 60;
            return "UTC" + (offset != 0 ? offset.ToString("+00;-00") : "") + "/" + Description;
        }
    }
    public class FilteredAccount
    {
        public string Id { get; set; }  // usually should be symbol + script's name
        public SymbolInAction Symbol { get; set; }

        private List<AccountInfo> _accounts;
        public List<AccountInfo> Accounts { get => _accounts ?? (_accounts = new List<AccountInfo>()); }
    }
    public class MainViewModel : NotifyPropertyChangedBase
    {

        private string _pStatusMsg;
        public string StatusMsg
        {
            get { return _pStatusMsg; }
            set { _UpdateField(ref _pStatusMsg, value); }
        }
        // Icons
        public Image ImageSaveLayout { get; private set; } = Util.MaterialIconToImage(MaterialIcons.ContentSaveAll, Util.Color.Indigo);
        public Image ImageRestoreLayout { get; private set; } = Util.MaterialIconToImage(MaterialIcons.WindowRestore, Util.Color.Indigo);
        public Image ImagePowerPlug { get; private set; } = Util.MaterialIconToImage(MaterialIcons.PowerPlug, Util.Color.Green);
        public Image ImagePowerPlugOff { get; private set; } = Util.MaterialIconToImage(MaterialIcons.PowerPlugOff, Util.Color.Red);
        public Image ImageSettings { get; private set; } = Util.MaterialIconToImage(MaterialIcons.Settings);
        public Image ImageHelp { get; private set; } = Util.MaterialIconToImage(MaterialIcons.BookOpenPageVariant, Util.Color.Purple);
        public Image ImageAbout { get; private set; } = Util.MaterialIconToImage(MaterialIcons.HelpCircle);
        public Image ImageRefresh { get; private set; } = Util.MaterialIconToImage(MaterialIcons.Refresh, Util.Color.Green);        
        //public Image ImageOrderCancel { get; private set; } = new Image() { Source = new BitmapImage(new Uri("pack://application:,,,/OrderManager;component/Controllers/images/order-cancel.png")) };        
        // commands
        public Commands Commands { get; set; } = new Commands();
        public List<TimeZone> TimeZones { get; private set; } = new List<TimeZone>();
        // All OrderTypes 
        public List<BaseOrderType> AllIBOrderTypes { get; set; } = new List<BaseOrderType>();
        public List<BaseOrderType> AllFTOrderTypes { get; set; } = new List<BaseOrderType>();
        public List<VendorOrderType> VendorOrderTypes { get; set; } = new List<VendorOrderType>();
        public UserPreference UserPreference { get; private set; }
        public ObservableCollection<IController> Controllers { get; set; }
        public Dictionary<int, OrderInfo> OrderInfoList { get; set; } = new Dictionary<int, OrderInfo>();
        // Lists for displaying
        public ObservableCollection<Message> MessageList { set; get; }
        public ObservableCollection<Log> LogList { set; get; }
        public ObservableCollection<Log> MinorLogList { set; private get; } = new ObservableCollection<Log>();
        public ObservableCollectionEx<DisplayedOrder> Orders { set; get; }
        public ObservableCollection<SymbolInMkt> Portfolio { set; get; } = new ObservableCollection<SymbolInMkt>();
        public SymbolInMkt SelectedPortfolio { get; set; }
        public ObservableCollection<SymbolInAction> SymbolInActions { get; set; }
        public ICollectionView PendingOrdersView { get; set; }
        public DisplayedOrder SelectedPendingOrder { get; set; }
        public ICollectionView ExecutionView { get; set; }
        // collectionViewSources for views
        private CollectionViewSource poViewSource;
        private CollectionViewSource execViewSource;
        
        // for script treeview use -- selected treeview item
        private object _pSelectedItem;
        public object SelectedItem
        {
            get { return _pSelectedItem; }
            set { _UpdateField(ref _pSelectedItem, value); }
        }

        // for binding(displaying) controller for each symbol
        private bool _pDummy;
        public bool Dummy
        {
            get { return _pDummy; }
            set { _UpdateField(ref _pDummy, value); }
        }

        // for selecting accounts - multiselect-combox
        public CustomFilterService FilterService { get; } = new CustomFilterService();

        // singelton pattern
        public static MainViewModel Instance { get { return instance; } }
        private static readonly MainViewModel instance = new MainViewModel();
        // Explicit static constructor to tell C# compiler
        // not to mark type as beforefieldinit
        static MainViewModel()
        {
        }
        private MainViewModel()
        {
            Controllers = new ObservableCollection<IController>();
            MessageList = new ObservableCollection<Message>();
            LogList = new ObservableCollection<Log>();

            Orders = new ObservableCollectionEx<DisplayedOrder>();
            Orders.CollectionChanged += Orders_CollectionChanged;
            ((INotifyPropertyChanged)Orders).PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == "Status")
                {
                    poViewSource.View.Refresh();
                    execViewSource.View.Refresh();
                }
            };            

            SymbolInActions = new ObservableCollection<SymbolInAction>();

            poViewSource = new CollectionViewSource();
            poViewSource.Source = Orders;
            poViewSource.Filter += PendingOrders_Filter;
            PendingOrdersView = poViewSource.View;

            execViewSource = new CollectionViewSource();
            execViewSource.Source = Orders;
            execViewSource.Filter += Execution_Filter;
            ExecutionView = execViewSource.View;
                        
            // retrieving the settings
            ReadSettings();

            // reading all order types
            var types = typeof(IBOrderType).Assembly.GetTypes().Where(x => x.IsSubclassOf(typeof(IBOrderType)));
            foreach (var t in types)
            {
                AllIBOrderTypes.Add((IBOrderType)Activator.CreateInstance(t));
            }
            types = typeof(FTOrderType).Assembly.GetTypes().Where(x => x.IsSubclassOf(typeof(FTOrderType)));
            foreach (var t in types)
            {
                AllFTOrderTypes.Add((FTOrderType)Activator.CreateInstance(t));
            }
            //VendorOrderTypes.Add("Order Types for IB", AllIBOrderTypes);
            //VendorOrderTypes.Add("Order Types for FT", AllFTOrderTypes);
            VendorOrderTypes.Add(new VendorOrderType { Name = "Order Types for Interative Broker", OrderTypes = AllIBOrderTypes });
            VendorOrderTypes.Add(new VendorOrderType { Name = "Order Types for FuTu NiuNiu", OrderTypes = AllFTOrderTypes });

            TimeZones.Add(new TimeZone { Id = "CST", UtcOffset = new TimeSpan(8,0,0), Description = "China Standard Time" });
            TimeZones.Add(new TimeZone { Id = "EST", UtcOffset = new TimeSpan(-5, 0, 0), Description = "Eastern Standard Time (North America)" });

        }
        public void test()
        {
            LogList.Add(new Log() { Source = "test1", Text = "text" });
            SymbolInAction symbol = new SymbolInAction("HSI", 5);
            SymbolInActions.Add(symbol);
            Script script = new Script("Basic", symbol);            
            script.AllowMultiLong = true;
            script.MaxOpenPosition = 5;
            script.MaxEntriesPerDay = 3;
            symbol.Scripts.Add(script);
            Script script1 = new Script("Basic1", symbol);
            script1.AllowMultiLong = true;
            script1.MaxOpenPosition = 8;
            script1.MaxEntriesPerDay = 4;
            symbol.Scripts.Add(script1);
            Strategy s2 = new Strategy("strategy2", script);
            s2.MaxEntriesPerDay = 3;
            s2.MaxOpenPosition = 5;
            s2.ActionType = ActionType.LongAndShort;
            s2.Prices = new List<string>() { "bp2a", "bp2b" };
            script.Strategies.Add(s2);
            Strategy s4 = new Strategy("strategy4", script);
            s4.MaxEntriesPerDay = 3;
            s4.MaxOpenPosition = 5;
            s4.ActionType = ActionType.Long;
            s4.Prices = new List<string>() { "bp4a", "bp4b" };
            script.Strategies.Add(s4);
            Strategy s1 = new Strategy("strategy1", script);
            s1.MaxEntriesPerDay = 2;
            s1.MaxOpenPosition = 4;
            s1.ActionType = ActionType.LongAndShort;
            s1.Prices = new List<string>() { "bp1a", "bp1b" };
            script.Strategies.Add(s1);            
            Strategy s3 = new Strategy("strategy3", script);
            s3.MaxEntriesPerDay = 2;
            s3.MaxOpenPosition = 6;
            s3.ActionType = ActionType.Short;
            s3.Prices = new List<string>() { "bp3a", "bp3b" };
            script.Strategies.Add(s3);

            Strategy s8 = new Strategy("strategy8", script);
            s8.MaxEntriesPerDay = 2;
            s8.MaxOpenPosition = 6;
            s8.ActionType = ActionType.Short;
            s8.Prices = new List<string>() { "bp8a", "bp8b" };
            script1.Strategies.Add(s8);
            Strategy s5 = new Strategy("strategy5", script);
            s5.MaxEntriesPerDay = 3;
            s5.MaxOpenPosition = 5;
            s5.ActionType = ActionType.LongAndShort;
            s5.Prices = new List<string>() { "bp5a", "bp5b" };
            script1.Strategies.Add(s5);
            Strategy s6 = new Strategy("strategy6", script);
            s6.MaxEntriesPerDay = 6;
            s6.MaxOpenPosition = 6;
            s6.ActionType = ActionType.Long;
            s6.Prices = new List<string>() { "bp6a", "bp6b" };
            script1.Strategies.Add(s6);
            Strategy s7 = new Strategy("strategy7", script);
            s7.MaxEntriesPerDay = 2;
            s7.MaxOpenPosition = 4;
            s7.ActionType = ActionType.LongAndShort;
            s7.Prices = new List<string>() { "bp7a", "bp7b" };
            script1.Strategies.Add(s7);
            
            SymbolInAction symbol1 = new SymbolInAction("QQQ", 5);
            SymbolInActions.Add(symbol1);
            Script script2 = new Script("Basic2", symbol1);
            script2.AllowMultiLong = true;
            script2.MaxOpenPosition = 8;
            script2.MaxEntriesPerDay = 5;
            symbol1.Scripts.Add(script2);
            Script script3 = new Script("Basic3", symbol1);
            script3.AllowMultiLong = true;
            script3.MaxOpenPosition = 8;
            script3.MaxEntriesPerDay = 6;
            symbol1.Scripts.Add(script3);
            Strategy s9 = new Strategy("strategy9", script2);
            s9.MaxEntriesPerDay = 9;
            s9.MaxOpenPosition = 9;
            s9.ActionType = ActionType.Short;
            s9.Prices = new List<string>() { "bp9a", "bp9b" };
            script2.Strategies.Add(s9);
            Strategy s10 = new Strategy("strategy10", script2);
            s10.MaxEntriesPerDay = 10;
            s10.MaxOpenPosition = 10;
            s10.ActionType = ActionType.LongAndShort;
            s10.Prices = new List<string>() { "bp10a", "bp10b" };
            script2.Strategies.Add(s10);
            Strategy s11 = new Strategy("strategy11", script3);
            s11.MaxEntriesPerDay = 11;
            s11.MaxOpenPosition = 11;
            s11.ActionType = ActionType.Long;
            s11.Prices = new List<string>() { "bp11a", "bp11b" };
            script3.Strategies.Add(s11);
            Strategy s12 = new Strategy("strategy12", script3);
            s12.MaxEntriesPerDay = 12;
            s12.MaxOpenPosition = 12;
            s12.ActionType = ActionType.LongAndShort;
            s12.Prices = new List<string>() { "bp12a", "bp12b" };
            script3.Strategies.Add(s12);
        }
        public bool AddSymbol(string name, float timeframe, out SymbolInAction symbol)
        {
            symbol = SymbolInActions.FirstOrDefault(x => x.Name == name && x.TimeFrame == timeframe);
            if (symbol == null || (symbol != null && symbol.IsDirty))
            {
                if (symbol != null)
                {
                    symbol.RefreshScripts();
                    symbol.IsDirty = false;
                }
                else
                {
                    symbol = new SymbolInAction(name, timeframe);
                    SymbolInActions.Add(symbol);
                }                
                return true;
            }
            return false;
        }
        public void Log(Log log)
        {
            Dispatcher.FromThread(OrderManager.UIThread).Invoke(() =>
            {
                LogList.Insert(0, log);
            });
        }
        public void MinorLog(Log log)
        {
            if (!OrderManager.MainWin.MinorLogPause)
                Dispatcher.FromThread(OrderManager.UIThread).Invoke(() =>
                {
                    MinorLogList.Insert(0, log);
                });
        }
        private void Orders_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            PendingOrdersView.Refresh();
            ExecutionView.Refresh();
        }

        // filters for CollectionView
        private List<string> pendingStatus = new List<string>() { "Inactive", "PreSumitted", "Submitted", "Pending" };
        private void PendingOrders_Filter(object sender, FilterEventArgs e)
        {
            var order = e.Item as DisplayedOrder;
            e.Accepted = pendingStatus.Any(s => order.Status.Contains(s));
        }

        private void Execution_Filter(object sender, FilterEventArgs e)
        {
            var order = e.Item as DisplayedOrder;
            e.Accepted = !pendingStatus.Any(s => order.Status.Contains(s));
        }

        public void ReadSettings()
        {
            UserPreference = JsonConvert.DeserializeObject<UserPreference>(Properties.Settings.Default["preference"].ToString());
            List<IController> ctrls = new List<IController>();
            if (UserPreference != null)
            {
                Type t = typeof(Helper);
                string ns = t.Namespace;
                foreach (string vendor in UserPreference.Vendors)
                {
                    AccountOption accOpt = (dynamic)UserPreference.GetType().GetProperty(vendor + "Account").GetValue(UserPreference);
                    foreach (var acc in accOpt.Accounts)
                    {
                        if (acc.IsActivate)
                        {
                            string clsName = ns + "." + vendor + "Controller";
                            Type type = Type.GetType(clsName);
                            IController ctrl = Activator.CreateInstance(type, this) as IController;
                            ctrl.ConnParam = acc;
                            // if some connection is connected, then remain unchanged
                            IController ic = Controllers.FirstOrDefault(x => x.DisplayName == ctrl.DisplayName);
                            if (ic != null && ic.IsConnected)
                                ctrls.Add(ic);
                            else
                                ctrls.Add(ctrl);
                        }
                    }
                    Controllers.Clear();
                    foreach (var item in ctrls)
                    {
                        Controllers.Add(item);
                    }
                }
            }

        }
    }
}
