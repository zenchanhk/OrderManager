using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using AmiBroker.Controllers;
using Newtonsoft.Json;
using Easy.MessageHub;
using System.Reflection;
using System.Windows.Threading;
using System.Windows;
using Krs.Ats.IBNet;
using AmiBroker.Controllers;

namespace AmiBroker.OrderManager
{   
    public enum ActionType
    {
        Long = 0,
        Short = 1,
        LongAndShort = 2
    }
    public enum OrderAction
    {
        Buy=0,
        Sell=1,
        Short=2,
        Cover=3,
        APSLong=4,  //adaptive profit stop for long
        APSShort = 5,  //adaptive profit stop for short
        StoplossLong=6,
        StoplossShort=7,
        ForceExit=8
    }
    public enum ClosePositionAction
    {
        CloseAllPositions=0,
        CloseLongPositions=1,
        CloseShortPositions=2
    }
    
    public class OrderInfo
    {
        public int RealOrderId { get; set; }    // order id
        public string OrderId { get; set; }     // controller name + order id
        public int BatchNo { get; set; }
        public double PosSize { get; set; }
        public double Filled { get; set; }
        public Strategy Strategy { get; set; }
        public int Slippage { get; set; }
        public AccountInfo Account { get; set; }
        //public List<OrderStatus> Status { get; set; } = new List<OrderStatus>();
        public OrderAction OrderAction { get; set; }
        public DisplayedOrder OrderStatus { get; set; }
        public string Error { get; set; }
        public DateTime PlacedTime { get; set; }
        public bool IsReversedOrder { get; set; }
        public int NoOfRef { get; set; } = 0;  // the number of referenced by reverse signal exit
        public Contract Contract = null;
        public Order Order = null;
        public BaseOrderType OrderType = null;
    }
    public sealed class Entry : IEquatable<Entry>
    {
        public string OrderId { get; }
        public int BatchNo { get; }

        public Entry(string orderId, int batchNo)
        {
            OrderId = orderId;
            BatchNo = batchNo;
        }

        public override int GetHashCode()
        {
            return OrderId.GetHashCode() + BatchNo; 
        }

        public override bool Equals(object obj)
        {
            return obj is Entry && Equals((Entry)obj);
        }
        public bool Equals(Entry entry)
        {
            return entry.OrderId == OrderId && BatchNo == entry.BatchNo;
        }
    }
    public class BaseStat : NotifyPropertyChangedBase
    {
        public AccountInfo Account { get; set; }
        [JsonIgnore]
        public SSBase SSBase { get; set; }

        private AccountStatus _pAccountStatus = AccountStatus.None;
        public AccountStatus AccountStatus
        {
            get { return _pAccountStatus; }
            set {
                _UpdateField(ref _pAccountStatus, value);
                Status = string.Join(",", Helper.TranslateAccountStatus(_pAccountStatus));
                if (SSBase != null && SSBase.GetType() == typeof(Strategy))
                    ((Strategy)SSBase).AFLStatusCallback(value);
            }
        }

        private string _pStatus;
        public string Status
        {
            get { return _pStatus; }
            set { _UpdateField(ref _pStatus, value); }
        }

        private double _pLongPosition;
        //[JsonIgnore]
        public double LongPosition
        {
            get { return _pLongPosition; }
            set { _UpdateField(ref _pLongPosition, value); }
        }

        private double _pShortPosition;
        public double ShortPosition
        {
            get { return _pShortPosition; }
            set { _UpdateField(ref _pShortPosition, value); }
        }
        
        private HashSet<Entry> _pLongEntry = new HashSet<Entry>();
        //[JsonIgnore]
        public HashSet<Entry> LongEntry
        {
            get { return _pLongEntry; }
            set { _UpdateField(ref _pLongEntry, value); }
        }
        /*
        private float _pLongEntryPrice;
        [JsonIgnore]
        public float LongEntryPrice
        {
            get { return _pLongEntryPrice; }
            set { _UpdateField(ref _pLongEntryPrice, value); }
        }

        private float _pShortEntryPrice;
        [JsonIgnore]
        public float ShortEntryPrice
        {
            get { return _pShortEntryPrice; }
            set { _UpdateField(ref _pShortEntryPrice, value); }
        }*/
        
        private HashSet<Entry> _pShortEntry = new HashSet<Entry>();
        //[JsonIgnore]
        public HashSet<Entry> ShortEntry
        {
            get { return _pShortEntry; }
            set { _UpdateField(ref _pShortEntry, value); }
        }
        public HashSet<string> LongStrategies { get; } = new HashSet<string>();
        public HashSet<string> LongPendingStrategies { get; } = new HashSet<string>();
        public HashSet<string> ShortStrategies { get; } = new HashSet<string>();
        public HashSet<string> ShortPendingStrategies { get; } = new HashSet<string>();

        // only applied to Strategy
        private int _pLongAttemps;
        public int LongAttemps
        {
            get { return _pLongAttemps; }
            set { _UpdateField(ref _pLongAttemps, value); }
        }
        // only applied to Strategy
        private int _pShortAttemps;
        public int ShortAttemps
        {
            get { return _pShortAttemps; }
            set { _UpdateField(ref _pShortAttemps, value); }
        }

        [JsonIgnore]
        public Dictionary<OrderAction, List<OrderInfo>> OrderInfos { get; set; }

        public BaseStat()
        {
            OrderInfos = new Dictionary<OrderAction, List<OrderInfo>>();
            var orderActions = Enum.GetValues(typeof(OrderAction)).Cast<OrderAction>();
            foreach (var oa in orderActions)
            {
                OrderInfos.Add(oa, new List<OrderInfo>());
            }
        }
    }
    
    public class SSBase : NotifyPropertyChangedBase
    {
        public SSBase()
        {            
            LongAccounts.CollectionChanged += Accounts_CollectionChanged;
            ShortAccounts.CollectionChanged += Accounts_CollectionChanged;
        }

        private void Accounts_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (AccountInfo account in e.OldItems)
                {
                    AccountStat.Remove(account.Name);
                }
            }
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (AccountInfo account in e.NewItems)
                {
                    if (!AccountStat.ContainsKey(account.Name))
                        AccountStat.Add(account.Name, new BaseStat() { Account = account, SSBase = this });
                }
                // add account into script too
                if (GetType() == typeof(Strategy))
                {
                    Script script = ((Strategy)this).Script;
                    // duing de/serialization, script will be null
                    if (script != null)
                    {
                        foreach (AccountInfo account in e.NewItems)
                        {
                            if (!script.AccountStat.ContainsKey(account.Name))
                                script.AccountStat.Add(account.Name, new BaseStat() { Account = account, SSBase = this });
                        }
                    }                    
                }
            }
        }

        private bool _pDayTradeMode;
        public bool DayTradeMode
        {
            get { return _pDayTradeMode; }
            set { _UpdateField(ref _pDayTradeMode, value); }
        }

        private SymbolInAction _pSymbol;
        public SymbolInAction Symbol
        {
            get { return _pSymbol; }
            set
            {
                _UpdateField(ref _pSymbol, value);
                _pSymbol.AccountCandidates.CollectionChanged += AccountCandidates_CollectionChanged;
            }
        }
        private void AccountCandidates_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (AccountInfo account in e.OldItems)
                {
                    LongAccounts.Remove(account);
                    ShortAccounts.Remove(account);
                }
            }            
        }

        private DebounceDispatcher debounceTimer = new DebounceDispatcher();
        private bool _pIsDirty;
        [JsonIgnore]
        public bool IsDirty
        {
            get { return _pIsDirty; }
            set {
                if (_pIsDirty != value && value == false)
                {
                    debounceTimer.Debounce(2000, parm =>
                    {
                        ShowSign = false;
                    });
                }
                else if (_pIsDirty != value && value)
                {
                    ShowSign = true;
                }
                _UpdateField(ref _pIsDirty, value);
            }
        }

        private bool _pShowSign;
        [JsonIgnore]
        public bool ShowSign
        {
            get { return _pShowSign; }
            set { _UpdateField(ref _pShowSign, value); }
        }

        public string Name { get; set; }

        private int _pMaxEntriesPerDay = 1;
        public int MaxEntriesPerDay
        {
            get { return _pMaxEntriesPerDay; }
            set { _UpdateField(ref _pMaxEntriesPerDay, value); }
        }

        private int _pMaxOpenPosition = 1;
        public int MaxOpenPosition
        {
            get { return _pMaxOpenPosition; }
            set { _UpdateField(ref _pMaxOpenPosition, value); }
        }

        private int _pMaxLongOpenPosition = 1;
        public int MaxLongOpenPosition
        {
            get { return _pMaxLongOpenPosition; }
            set { _UpdateField(ref _pMaxLongOpenPosition, value); }
        }

        private int _pMaxShortOpenPosition = 1;
        public int MaxShortOpenPosition
        {
            get { return _pMaxShortOpenPosition; }
            set { _UpdateField(ref _pMaxShortOpenPosition, value); }
        }

        private bool _pAllowMultiLong;
        public bool AllowMultiLong
        {
            get { return _pAllowMultiLong; }
            set { _UpdateField(ref _pAllowMultiLong, value); }
        }

        private int _pMaxLongOpen = 1;
        public int MaxLongOpen
        {
            get { return _pMaxLongOpen; }
            set { _UpdateField(ref _pMaxLongOpen, value); }
        }

        private bool _pAllowMultiShort;
        public bool AllowMultiShort
        {
            get { return _pAllowMultiShort; }
            set { _UpdateField(ref _pAllowMultiShort, value); }
        }

        private int _pMaxShortOpen = 1;
        public int MaxShortOpen
        {
            get { return _pMaxShortOpen; }
            set { _UpdateField(ref _pMaxShortOpen, value); }
        }

        private bool _pReverseSignalForcesExit;
        public bool ReverseSignalForcesExit
        {
            get { return _pReverseSignalForcesExit; }
            set { _UpdateField(ref _pReverseSignalForcesExit, value); }
        }
        
        private int _pPositionSize = 1;
        public int DefaultPositionSize
        {
            get { return _pPositionSize; }
            set { _UpdateField(ref _pPositionSize, value); }
        }

        private int _pMaxLongAttemps = 1;
        public int MaxLongAttemps
        {
            get { return _pMaxLongAttemps; }
            set { _UpdateField(ref _pMaxLongAttemps, value); }
        }

        private int _pMaxShortAttemps = 1;
        public int MaxShortAttemps
        {
            get { return _pMaxShortAttemps; }
            set { _UpdateField(ref _pMaxShortAttemps, value); }
        }

        private bool _pOutsideRTH = false;
        public bool OutsideRTH
        {
            get { return _pOutsideRTH; }
            set { _UpdateField(ref _pOutsideRTH, value); }
        }

        // key is the name of account which is supposed to unique
        public Dictionary<string, BaseStat> AccountStat { get; set; } = new Dictionary<string, BaseStat>(); // statistics

        private ObservableCollection<BaseOrderType> _buyOT;
        public ObservableCollection<BaseOrderType> BuyOrderTypes {
            get
            {
                if (_buyOT == null)
                {
                    _buyOT = new ObservableCollection<BaseOrderType>();
                    if (GetType() == typeof(Strategy))
                        ((Strategy)this).OrderTypesDic.Add(OrderAction.Buy, _buyOT);
                }
                return _buyOT;
            }
            set
            {
                if (_buyOT != value)
                {
                    _buyOT = value;
                    if (GetType() == typeof(Strategy))
                        ((Strategy)this).OrderTypesDic[OrderAction.Buy] = _buyOT;
                }
            }
        }

        private ObservableCollection<BaseOrderType> _sellOT;
        public ObservableCollection<BaseOrderType> SellOrderTypes
        {
            get
            {
                if (_sellOT == null)
                {
                    _sellOT = new ObservableCollection<BaseOrderType>();
                    if (GetType() == typeof(Strategy))
                        ((Strategy)this).OrderTypesDic.Add(OrderAction.Sell, _sellOT);
                }
                return _sellOT;
            }
            set
            {
                if (_sellOT != value)
                {
                    _sellOT = value;
                    if (GetType() == typeof(Strategy))
                        ((Strategy)this).OrderTypesDic[OrderAction.Sell] = _sellOT;
                }
            }
        }

        private ObservableCollection<BaseOrderType> _shortOT;
        public ObservableCollection<BaseOrderType> ShortOrderTypes
        {
            get
            {
                if (_shortOT == null)
                {
                    _shortOT = new ObservableCollection<BaseOrderType>();
                    if (GetType() == typeof(Strategy))
                        ((Strategy)this).OrderTypesDic.Add(OrderAction.Short, _shortOT);
                }
                return _shortOT;
            }
            set
            {
                if (_shortOT != value)
                {
                    _shortOT = value;
                    if (GetType() == typeof(Strategy))
                        ((Strategy)this).OrderTypesDic[OrderAction.Short] = _shortOT;
                }
            }
        }

        private ObservableCollection<BaseOrderType> _coverOT;
        public ObservableCollection<BaseOrderType> CoverOrderTypes
        {
            get
            {
                if (_coverOT == null)
                {
                    _coverOT = new ObservableCollection<BaseOrderType>();
                    if (GetType() == typeof(Strategy))
                        ((Strategy)this).OrderTypesDic.Add(OrderAction.Cover, _coverOT);
                }
                return _coverOT;
            }
            set
            {
                if (_coverOT != value)
                {
                    _coverOT = value;
                    if (GetType() == typeof(Strategy))
                        ((Strategy)this).OrderTypesDic[OrderAction.Cover] = _coverOT;
                }
            }
        }

        private BaseObservableCollection<AccountInfo> _longAccounts;
        public BaseObservableCollection<AccountInfo> LongAccounts
        {
            get
            {
                if (_longAccounts == null)
                {
                    _longAccounts = new BaseObservableCollection<AccountInfo>();
                    if (GetType() == typeof(Strategy))
                    {
                        ((Strategy)this).AccountsDic.Add(OrderAction.Buy, _longAccounts);
                        ((Strategy)this).AccountsDic.Add(OrderAction.Sell, _longAccounts);
                        ((Strategy)this).AccountsDic.Add(OrderAction.APSLong, _longAccounts);
                        ((Strategy)this).AccountsDic.Add(OrderAction.StoplossLong, _longAccounts);
                    }                        
                }
                return _longAccounts;
            }
            set
            {
                if (_longAccounts != value)
                {
                    _longAccounts = value;
                    if (GetType() == typeof(Strategy))
                    {
                        ((Strategy)this).AccountsDic[OrderAction.Buy] = _longAccounts;
                        ((Strategy)this).AccountsDic[OrderAction.Sell] = _longAccounts;
                        ((Strategy)this).AccountsDic[OrderAction.APSLong] = _longAccounts;
                        ((Strategy)this).AccountsDic[OrderAction.StoplossLong] = _longAccounts;
                    }                        
                }
            }
        }

        private BaseObservableCollection<AccountInfo> _shortAccounts;
        public BaseObservableCollection<AccountInfo> ShortAccounts
        {
            get
            {
                if (_shortAccounts == null)
                {
                    _shortAccounts = new BaseObservableCollection<AccountInfo>();
                    if (GetType() == typeof(Strategy))
                    {
                        ((Strategy)this).AccountsDic.Add(OrderAction.Short, _shortAccounts);
                        ((Strategy)this).AccountsDic.Add(OrderAction.Cover, _shortAccounts);
                        ((Strategy)this).AccountsDic.Add(OrderAction.APSShort, _shortAccounts);
                        ((Strategy)this).AccountsDic.Add(OrderAction.StoplossShort, _shortAccounts);
                    }
                }
                return _shortAccounts;
            }
            set
            {
                if (_shortAccounts != value)
                {
                    _shortAccounts = value;
                    if (GetType() == typeof(Strategy))
                    {
                        ((Strategy)this).AccountsDic[OrderAction.Short] = _shortAccounts;
                        ((Strategy)this).AccountsDic[OrderAction.Cover] = _shortAccounts;
                        ((Strategy)this).AccountsDic[OrderAction.APSShort] = _shortAccounts;
                        ((Strategy)this).AccountsDic[OrderAction.StoplossShort] = _shortAccounts;
                    }
                }
            }
        }

        private bool _pIsEnabled = true;
        [JsonIgnore]
        public bool IsEnabled
        {
            get { return _pIsEnabled; }
            set { _UpdateField(ref _pIsEnabled, value); }
        }
        // for GUI use
        private VendorOrderType _pSelectedVendor;
        [JsonIgnore]
        public VendorOrderType SelectedVendor
        {
            get { return _pSelectedVendor; }
            set { _UpdateField(ref _pSelectedVendor, value); }
        }

        //
        // Adaptive mult-level profit target profit trailing stop
        //
        private bool _pIsAPSAppliedforLong;
        public bool IsAPSAppliedforLong
        {
            get { return _pIsAPSAppliedforLong; }
            set
            {
                _UpdateField(ref _pIsAPSAppliedforLong, value);
            }
        }

        private bool _pIsAPSAppliedforShort;
        public bool IsAPSAppliedforShort
        {
            get { return _pIsAPSAppliedforShort; }
            set
            {
                _UpdateField(ref _pIsAPSAppliedforShort, value);
            }
        }
        

        private AdaptiveProfitStop _APSLong = null;
        public AdaptiveProfitStop AdaptiveProfitStopforLong {
            get
            {
                if (_APSLong == null)
                {                    
                    _APSLong = new AdaptiveProfitStop(this, ActionType.Long);
                }
                return _APSLong;
            }
            set { _APSLong = value; }
        }

        private AdaptiveProfitStop _APSShort = null;
        public AdaptiveProfitStop AdaptiveProfitStopforShort {
            get
            {
                if (_APSShort == null)
                {                    
                    _APSShort = new AdaptiveProfitStop(this, ActionType.Short);
                }
                return _APSShort;
            }
            set { _APSShort = value; }
        }

        private bool _pIsForcedExitForLong;
        public bool IsForcedExitForLong
        {
            get { return _pIsForcedExitForLong; }
            set { _UpdateField(ref _pIsForcedExitForLong, value); }
        }

        private bool _pIsForcedExitForShort;
        public bool IsForcedExitForShort
        {
            get { return _pIsForcedExitForShort; }
            set { _UpdateField(ref _pIsForcedExitForShort, value); }
        }

        private ForceExitOrder _pForceExitOrderForLong;
        public ForceExitOrder ForceExitOrderForLong
        {
            get
            {
                if (_pForceExitOrderForLong == null)
                    _pForceExitOrderForLong = new ForceExitOrder(this, ActionType.Long);
                return _pForceExitOrderForLong;
            }
            set { _pForceExitOrderForLong = value; }
        }

        private ForceExitOrder _pForceExitOrderForShort;
        public ForceExitOrder ForceExitOrderForShort
        {
            get
            {
                if (_pForceExitOrderForShort == null)
                    _pForceExitOrderForShort = new ForceExitOrder(this, ActionType.Short);
                return _pForceExitOrderForShort;
            }
            set { _pForceExitOrderForShort = value; }
        }

        public void Clear()
        {
            MaxEntriesPerDay = 0;
            MaxOpenPosition = 0;            
            MaxLongOpen = 0;
            MaxShortOpen = 0;
            AllowMultiLong = false;
            AllowMultiShort = false;
            /*
            AllowReEntry = false;
            MaxReEntry = 0;
            ReEntryBefore = null;
            IsNextDay = false;  // indicating if ReEntryBefore is next day for night market
            */
            AccountStat.Clear();
            LongAccounts.Clear();
            ShortAccounts.Clear();
            BuyOrderTypes.Clear();
            SellOrderTypes.Clear();
            ShortOrderTypes.Clear();
            CoverOrderTypes.Clear();
        }

        public void ChangeTimeZone(Controllers.TimeZone tz)
        {
            foreach (var ot in BuyOrderTypes)
            {
                ot.TimeZone = tz;
            }
            foreach (var ot in SellOrderTypes)
            {
                ot.TimeZone = tz;
            }
            foreach (var ot in ShortOrderTypes)
            {
                ot.TimeZone = tz;
            }
            foreach (var ot in CoverOrderTypes)
            {
                ot.TimeZone = tz;
            }
            // strategy
            PropertyInfo pi = GetType().GetProperty("Strategies");
            if (pi != null)
            {
                ObservableCollection<Strategy> ss = pi.GetValue(this) as ObservableCollection<Strategy>;
                foreach (var item in ss)
                {
                    item.ChangeTimeZone(tz);
                }
            }
        }
    }
    
    public class Strategy : SSBase
    {
        private MessageHub _hub = MessageHub.Instance;
        public Strategy() : base()
        {
            //Init();
        }
        public Strategy(string strategyName, Script script)
            : base()
        {            
            Name = strategyName;
            Script = script;
            Symbol = script.Symbol;
            //Init();
        }
        private void Init()
        {
            LongAccounts.CollectionChanged += LongAccounts_CollectionChanged;
            AccountsDic.Clear();
            AccountsDic.Add(OrderAction.Buy, LongAccounts);
            AccountsDic.Add(OrderAction.Sell, LongAccounts);
            AccountsDic.Add(OrderAction.StoplossLong, LongAccounts);
            AccountsDic.Add(OrderAction.APSLong, LongAccounts);

            AccountsDic.Add(OrderAction.Short, ShortAccounts);
            AccountsDic.Add(OrderAction.Cover, ShortAccounts);
            AccountsDic.Add(OrderAction.APSShort, ShortAccounts);
            AccountsDic.Add(OrderAction.StoplossShort, ShortAccounts);

            OrderTypesDic.Clear();
            OrderTypesDic.Add(OrderAction.Buy, BuyOrderTypes);
            OrderTypesDic.Add(OrderAction.Sell, SellOrderTypes);
            OrderTypesDic.Add(OrderAction.Short, ShortOrderTypes);
            OrderTypesDic.Add(OrderAction.Cover, CoverOrderTypes);
        }
        private void LongAccounts_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            int i = 0;
        }

        [JsonIgnore]
        public Dictionary<OrderAction, BaseObservableCollection<AccountInfo>> AccountsDic { get; set; } = new Dictionary<OrderAction, BaseObservableCollection<AccountInfo>>();
        [JsonIgnore]
        public Dictionary<OrderAction, ObservableCollection<BaseOrderType>> OrderTypesDic { get; set; } = new Dictionary<OrderAction, ObservableCollection<BaseOrderType>>();
        [JsonIgnore]
        // store GAT and GTD information from script
        public Dictionary<string, Dictionary<string, GoodTime>> ScheduledOrders { get; set; } = new Dictionary<string, Dictionary<string, GoodTime>>();
        public ActionType ActionType { get; set; }
        [JsonIgnore]
        public Script Script { get; private set; }  // parent
        
        [JsonIgnore]
        public ATAfl BuySignal { get; set; }
        [JsonIgnore]
        public ATAfl SellSignal { get; set; }
        [JsonIgnore]
        public ATAfl ShortSignal { get; set; }
        [JsonIgnore]
        public ATAfl CoverSignal { get; set; }
        
        // Position Size        
        // store names of positions from AFL script
        [JsonIgnore]
        public List<string> PositionSize { get; set; } = new List<string>();
        [JsonIgnore]
        public Dictionary<string, ATAfl> PositionSizeATAfl { get; } = new Dictionary<string, ATAfl>();
        [JsonIgnore]
        public Dictionary<string, decimal> CurrentPosSize { get; } = new Dictionary<string, decimal>();

        // prices including AuxPrice, StopPrice and LmtPrice
        // store names of price from AFL script
        [JsonIgnore]
        public List<string> Prices { get; set; } = new List<string>();
        [JsonIgnore]
        public Dictionary<string, ATAfl> PricesATAfl { get; } = new Dictionary<string, ATAfl>();        
        [JsonIgnore]
        public Dictionary<string, decimal> CurrentPrices { get; } = new Dictionary<string, decimal>();

        /*
        // store names of stoploss from AFL script
        [JsonIgnore]
        public List<string> Stoploss { get; set; } = new List<string>();*/

        public void CloseAllPositions(ClosePositionAction closePositionAction = ClosePositionAction.CloseAllPositions)
        {
            List<Log> logs = new List<Log>();
            try
            {
                foreach (var item in AccountStat)
                {
                    IController controller = item.Value.Account.Controller;
                    string vendor = item.Value.Account.Controller.Vendor;
                    BaseOrderType orderType = (BaseOrderType)Helper.GetInstance(vendor + "MarketOrder");

                    if (item.Value.LongPosition > 0
                        && (closePositionAction == ClosePositionAction.CloseAllPositions || closePositionAction == ClosePositionAction.CloseLongPositions))
                    {
                        OrderAction orderAction = OrderAction.Sell;
                        double posSize = item.Value.LongPosition * Symbol.RoundLotSize;

                        controller.PlaceOrder(item.Value.Account, this, orderType, orderAction, Controllers.OrderManager.BatchNo, null, posSize).ContinueWith(
                            result =>
                            {
                                // should be done in IBController.eh_OrderStatus()
                                //item.Value.LongPosition = 0;
                                //BaseStat status = AccountStat[item.Key];
                                //AccountStatusOp.SetPositionStatus(ref status, orderAction);
                            }
                        );                        
                    }

                    if (item.Value.ShortPosition > 0
                        && (closePositionAction == ClosePositionAction.CloseAllPositions || closePositionAction == ClosePositionAction.CloseShortPositions))
                    {
                        OrderAction orderAction = OrderAction.Cover;
                        double posSize = item.Value.ShortPosition * Symbol.RoundLotSize;

                        controller.PlaceOrder(item.Value.Account, this, orderType, orderAction, Controllers.OrderManager.BatchNo, null, posSize).ContinueWith(
                            result =>
                            {
                                // should be done in IBController.eh_OrderStatus()
                                //item.Value.ShortPosition = 0;
                                //BaseStat status = AccountStat[item.Key];
                                //AccountStatusOp.SetPositionStatus(ref status, orderAction);
                            }
                        );
                    }

                    if ((item.Value.AccountStatus & AccountStatus.BuyPending) != 0)
                    {
                        OrderAction orderAction = OrderAction.Buy;
                        OrderInfo oi = item.Value.OrderInfos[orderAction].LastOrDefault();
                        if (oi != null)
                            controller.CancelOrderAsync(oi.RealOrderId);
                        else
                            MainViewModel.Instance.Log(new Log
                            {
                                Source = "Strategy.CloseAllPositions",
                                Text = "No LONG order info found for a BuyPending status",
                                Time = DateTime.Now
                            });
                    }

                    if ((item.Value.AccountStatus & AccountStatus.ShortPending) != 0)
                    {
                        OrderAction orderAction = OrderAction.Short;
                        OrderInfo oi = item.Value.OrderInfos[orderAction].LastOrDefault();
                        if (oi != null)
                            controller.CancelOrderAsync(oi.RealOrderId);
                        else
                            MainViewModel.Instance.Log(new Log
                            {
                                Source = "Strategy.CloseAllPositions",
                                Text = "No SHORT order info found for a ShortPending status",
                                Time = DateTime.Now
                            });
                    }

                    if ((item.Value.AccountStatus & AccountStatus.APSLongActivated) != 0)
                    {
                        OrderAction orderAction = OrderAction.APSLong;
                        OrderInfo oi = item.Value.OrderInfos[orderAction].LastOrDefault();
                        if (oi != null)
                            controller.CancelOrderAsync(oi.RealOrderId);
                        else
                            MainViewModel.Instance.Log(new Log
                            {
                                Source = "Strategy.CloseAllPositions",
                                Text = "No APSLong order info found for a APSLong Pending status",
                                Time = DateTime.Now
                            });
                    }

                    if ((item.Value.AccountStatus & AccountStatus.APSShortActivated) != 0)
                    {
                        OrderAction orderAction = OrderAction.APSShort;
                        OrderInfo oi = item.Value.OrderInfos[orderAction].LastOrDefault();
                        if (oi != null)
                            controller.CancelOrderAsync(oi.RealOrderId);
                        else
                            MainViewModel.Instance.Log(new Log
                            {
                                Source = "Strategy.CloseAllPositions",
                                Text = "No APSShort order info found for a APSShort Pending status",
                                Time = DateTime.Now
                            });
                    }

                    if ((item.Value.AccountStatus & AccountStatus.StoplossLongActivated) != 0)
                    {
                        OrderAction orderAction = OrderAction.StoplossLong;
                        OrderInfo oi = item.Value.OrderInfos[orderAction].LastOrDefault();
                        if (oi != null)
                            controller.CancelOrderAsync(oi.RealOrderId);
                        else
                            MainViewModel.Instance.Log(new Log
                            {
                                Source = "Strategy.CloseAllPositions",
                                Text = "No StoplossLong order info found for a StoplossLong Pending status",
                                Time = DateTime.Now
                            });
                    }

                    if ((item.Value.AccountStatus & AccountStatus.StoplossShortActivated) != 0)
                    {
                        OrderAction orderAction = OrderAction.StoplossShort;
                        OrderInfo oi = item.Value.OrderInfos[orderAction].LastOrDefault();
                        if (oi != null)
                            controller.CancelOrderAsync(oi.RealOrderId);
                        else
                            MainViewModel.Instance.Log(new Log
                            {
                                Source = "Strategy.CloseAllPositions",
                                Text = "No StoplossShort order info found for a StoplossShort Pending status",
                                Time = DateTime.Now
                            });
                    }
                }
            }
            catch (Exception ex)
            {                
                GlobalExceptionHandler.HandleException("Strategy.CloseAllPosition", ex);
            }
            
        }
        // reset in case new day
        public void ResetForNewDay()
        {
            foreach (var acc in LongAccounts)
            {
                AccountStat[acc.Name].LongEntry.Clear();
                AccountStat[acc.Name].ShortEntry.Clear();
                AccountStat[acc.Name].LongAttemps = 0;
                AccountStat[acc.Name].ShortAttemps = 0;
            }
            foreach (var acc in ShortAccounts)
            {
                AccountStat[acc.Name].LongEntry.Clear();
                AccountStat[acc.Name].ShortEntry.Clear();
                AccountStat[acc.Name].LongAttemps = 0;
                AccountStat[acc.Name].ShortAttemps = 0;
            }
        }

        public void AFLStatusCallback(AccountStatus status)
        {
            // sometimes, it doesn't work well
            //AFMisc.StaticVarSet(Name, (int)status);
        }
        public void CopyFrom(Strategy strategy)
        {
            MaxEntriesPerDay = strategy.MaxEntriesPerDay;
            MaxOpenPosition = strategy.MaxOpenPosition;
            MaxLongAttemps = strategy.MaxLongAttemps;
            MaxShortAttemps = strategy.MaxShortAttemps;
            ReverseSignalForcesExit = strategy.ReverseSignalForcesExit;
            DefaultPositionSize = strategy.DefaultPositionSize;
            IsAPSAppliedforLong = strategy.IsAPSAppliedforLong;
            IsAPSAppliedforShort = strategy.IsAPSAppliedforShort;
            IsForcedExitForLong = strategy.IsForcedExitForLong;
            IsForcedExitForShort = strategy.IsForcedExitForShort;
            /*
            AllowReEntry = strategy.AllowReEntry;
            ReEntryBefore = strategy.ReEntryBefore;
            MaxReEntry = strategy.MaxReEntry;*/
            if (ActionType == ActionType.Short || ActionType == ActionType.LongAndShort)
            {
                ShortOrderTypes = strategy.ShortOrderTypes;
                CoverOrderTypes = strategy.CoverOrderTypes;
                AllowMultiShort = strategy.AllowMultiShort;
                MaxShortOpen = strategy.MaxShortOpen;
                
                AdaptiveProfitStopforShort = strategy.AdaptiveProfitStopforShort;
                AdaptiveProfitStopforShort.Strategy = this;
                AdaptiveProfitStopforShort.ActionType = ActionType.Short;

                ForceExitOrderForShort = strategy.ForceExitOrderForShort;
                ForceExitOrderForShort.Strategy = this;
                ForceExitOrderForShort.ActionType = ActionType.Short;

                foreach (AccountInfo acc in strategy.ShortAccounts)
                {
                    AccountInfo tmp = Script.Symbol.AccountCandidates.FirstOrDefault(x => x.Name == acc.Name);
                    if (tmp != null)
                        ShortAccounts.Add(tmp);
                }
                _RaisePropertyChanged("ShortOrderTypes");
                _RaisePropertyChanged("CoverOrderTypes");
                _RaisePropertyChanged("ShortAccounts");
            }
            if (ActionType == ActionType.Long || ActionType == ActionType.LongAndShort)
            {
                BuyOrderTypes = strategy.BuyOrderTypes;
                SellOrderTypes = strategy.SellOrderTypes;
                AllowMultiLong = strategy.AllowMultiLong;
                MaxLongOpen = strategy.MaxLongOpen;

                AdaptiveProfitStopforLong = strategy.AdaptiveProfitStopforLong;
                AdaptiveProfitStopforLong.Strategy = this;
                AdaptiveProfitStopforLong.ActionType = ActionType.Long;

                ForceExitOrderForLong = strategy.ForceExitOrderForLong;
                ForceExitOrderForLong.Strategy = this;
                ForceExitOrderForLong.ActionType = ActionType.Long;

                foreach (AccountInfo acc in strategy.LongAccounts)
                {
                    AccountInfo tmp = Script.Symbol.AccountCandidates.FirstOrDefault(x => x.Name == acc.Name);
                    if (tmp != null)
                        LongAccounts.Add(tmp);
                }
                _RaisePropertyChanged("BuyOrderTypes");
                _RaisePropertyChanged("SellOrderTypes");
                _RaisePropertyChanged("LongAccounts");
            }
            //Init();
        }
    }
    /// <summary>
    /// This class is used to collect the parameters of the trading logic
    /// </summary>
    public class Script : SSBase
    {        
        // for json serilization purpose
        public Script() : base() { }
        public Script(string scriptName, SymbolInAction symbol)
            :base()
        {
            Name = scriptName;
            Symbol = symbol;
        }

        private int _pBarsHandled;
        public int BarsHandled
        {
            get { return _pBarsHandled; }
            set { _UpdateField(ref _pBarsHandled, value); }
        }

        private DateTime _pLastBarTime;
        public DateTime LastBarTime
        {
            get { return _pLastBarTime; }
            set { _UpdateField(ref _pLastBarTime, value); }
        }
        [JsonIgnore]
        public ATAfl DayStart { get; set; } // reset LongAttemps and ShortAttemps
        public ObservableCollection<Strategy> Strategies { get; set; } = new ObservableCollection<Strategy>();

        private bool _pIsEnabled;
        public new bool IsEnabled
        {
            get { return _pIsEnabled; }
            set {
                foreach (var strategy in Strategies)
                {
                    strategy.IsEnabled = value;
                }
                _UpdateField(ref _pIsEnabled, value);
            }
        }
        public void CloseAllPositions(ClosePositionAction closePositionAction = ClosePositionAction.CloseAllPositions)
        {
            foreach (var s in Strategies)
            {
                s.CloseAllPositions(closePositionAction);
            }
        }
        public void RefreshStrategies()
        {
            foreach (var item in Strategies.ToArray())
            {
                Strategies.Remove(item);
            }    
        }
        public void ResetForNewDay()
        {
            foreach (var acc in LongAccounts)
            {
                AccountStat[acc.Name].LongEntry.Clear();
                AccountStat[acc.Name].ShortEntry.Clear();
                AccountStat[acc.Name].LongAttemps = 0;
                AccountStat[acc.Name].ShortAttemps = 0;
            }
            foreach (var acc in ShortAccounts)
            {
                AccountStat[acc.Name].LongEntry.Clear();
                AccountStat[acc.Name].ShortEntry.Clear();
                AccountStat[acc.Name].LongAttemps = 0;
                AccountStat[acc.Name].ShortAttemps = 0;
            }
        }
        public void CopyFrom(Script script)
        {
            foreach (var item in script.Strategies)
            {
                var tmp = Strategies.FirstOrDefault(x => x.Name == item.Name);
                if (tmp != null)
                    tmp.CopyFrom(item);
            }
            MaxEntriesPerDay = script.MaxEntriesPerDay;
            MaxOpenPosition = script.MaxOpenPosition;
            ReverseSignalForcesExit = script.ReverseSignalForcesExit;
            /*
            AllowReEntry = script.AllowReEntry;
            ReEntryBefore = script.ReEntryBefore;
            MaxReEntry = script.MaxReEntry;
            */
            AllowMultiShort = script.AllowMultiShort;
            MaxShortOpen = script.MaxShortOpen;
            AllowMultiLong = script.AllowMultiLong;
            MaxLongOpen = script.MaxLongOpen;
            BuyOrderTypes = script.BuyOrderTypes;
            SellOrderTypes = script.SellOrderTypes;
            ShortOrderTypes = script.ShortOrderTypes;
            CoverOrderTypes = script.CoverOrderTypes;
            DefaultPositionSize = script.DefaultPositionSize;

            AdaptiveProfitStopforLong = script.AdaptiveProfitStopforLong;
            AdaptiveProfitStopforShort = script.AdaptiveProfitStopforShort;
            ForceExitOrderForLong = script.ForceExitOrderForLong;
            ForceExitOrderForShort = script.ForceExitOrderForShort;

            _RaisePropertyChanged("ShortOrderTypes");
            _RaisePropertyChanged("CoverOrderTypes");
            _RaisePropertyChanged("BuyOrderTypes");
            _RaisePropertyChanged("SellOrderTypes");
            foreach (AccountInfo acc in script.ShortAccounts)
            {
                AccountInfo tmp = Symbol.AccountCandidates.FirstOrDefault(x => x.Name == acc.Name);
                if (tmp != null)
                    ShortAccounts.Add(tmp);
                _RaisePropertyChanged("ShortAccounts");
            }

            foreach (AccountInfo acc in script.LongAccounts)
            {
                AccountInfo tmp = Symbol.AccountCandidates.FirstOrDefault(x => x.Name == acc.Name);
                if (tmp != null)
                    LongAccounts.Add(tmp);
                _RaisePropertyChanged("LongAccounts");
            }            
        }
        
        public void ApplySettingsToStrategies()
        {
            foreach (Strategy strategy in Strategies)
            {
                if (strategy.ActionType == ActionType.Long || strategy.ActionType == ActionType.LongAndShort)
                {
                    strategy.LongAccounts.Clear();
                    foreach (AccountInfo account in LongAccounts)
                    {
                        strategy.LongAccounts.Add(account);
                    }
                    strategy.MaxLongOpen = MaxLongOpen;
                    strategy.AllowMultiLong = AllowMultiLong;
                    strategy.BuyOrderTypes.Clear();
                    foreach (var ot in BuyOrderTypes)
                    {
                        strategy.BuyOrderTypes.Add(ot.Clone());
                    }
                    strategy.SellOrderTypes.Clear();
                    foreach (var ot in SellOrderTypes)
                    {
                        strategy.SellOrderTypes.Add(ot.Clone());
                    }
                }
                if (strategy.ActionType == ActionType.Short || strategy.ActionType == ActionType.LongAndShort)
                {
                    strategy.ShortAccounts.Clear();
                    foreach (AccountInfo account in ShortAccounts)
                    {
                        strategy.ShortAccounts.Add(account);
                    }
                    strategy.MaxShortOpen = MaxShortOpen;
                    strategy.AllowMultiShort = AllowMultiShort;
                    strategy.ShortOrderTypes.Clear();
                    foreach (var ot in ShortOrderTypes)
                    {
                        strategy.ShortOrderTypes.Add(ot.Clone());
                    }
                    strategy.CoverOrderTypes.Clear();
                    foreach (var ot in CoverOrderTypes)
                    {
                        strategy.CoverOrderTypes.Add(ot.Clone());
                    }
                }
                strategy.MaxEntriesPerDay = MaxEntriesPerDay;
                strategy.MaxOpenPosition = MaxOpenPosition;
                strategy.ReverseSignalForcesExit = ReverseSignalForcesExit;
                strategy.DefaultPositionSize = DefaultPositionSize;

                strategy.IsAPSAppliedforLong = IsAPSAppliedforLong;
                strategy.IsAPSAppliedforShort = IsAPSAppliedforShort;
                strategy.IsForcedExitForLong = IsForcedExitForLong;
                strategy.IsForcedExitForShort = IsForcedExitForShort;

                strategy.AdaptiveProfitStopforLong = AdaptiveProfitStopforLong.CloneObject();
                strategy.AdaptiveProfitStopforLong.Strategy = strategy;
                strategy.AdaptiveProfitStopforLong.ActionType = ActionType.Long;

                strategy.AdaptiveProfitStopforShort = AdaptiveProfitStopforShort.CloneObject();
                strategy.AdaptiveProfitStopforShort.Strategy = strategy;
                strategy.AdaptiveProfitStopforShort.ActionType = ActionType.Short;

                strategy.ForceExitOrderForLong = ForceExitOrderForLong.CloneObject();
                strategy.ForceExitOrderForLong.Strategy = strategy;
                strategy.ForceExitOrderForLong.ActionType = ActionType.Long;

                strategy.ForceExitOrderForShort = ForceExitOrderForShort.CloneObject();
                strategy.ForceExitOrderForShort.Strategy = strategy;
                strategy.ForceExitOrderForShort.ActionType = ActionType.Short;
                /*
                strategy.AllowReEntry = AllowReEntry;
                strategy.ReEntryBefore = ReEntryBefore;
                strategy.MaxReEntry = MaxReEntry;
                */
            }
        }
    }
    public class SymbolDefinition : NotifyPropertyChangedBase
    {
        //public string Vendor { get; set; }
        //public string VendorFullName { get; set; }
        public IController Controller { get; set; }

        private Contract _pContract;
        [JsonIgnore]
        public Contract Contract
        {
            get { return _pContract; }
            set {
                LocalSymbol = value.LocalSymbol;
                _UpdateField(ref _pContract, value); }
        }

        private string _pContractId;
        public string ContractId
        {
            get { return _pContractId; }
            set { _UpdateField(ref _pContractId, value); }
        }

        private string _pLocalSymbol;
        public string LocalSymbol
        {
            get { return _pLocalSymbol; }
            set { _UpdateField(ref _pLocalSymbol, value); }
        }

        private string _pTradingHours;
        public string TradingHours
        {
            get { return _pTradingHours; }
            set { _UpdateField(ref _pTradingHours, value); }
        }
    }
    public class SymbolInAction : NotifyPropertyChangedBase
    {
        // just for json serilization
        public SymbolInAction() { }
        public SymbolInAction(string symbol, float timeframe)
        {
            Name = symbol;
            TimeFrame = timeframe;
            // get current system timezone
            System.TimeZone timeZone = System.TimeZone.CurrentTimeZone;
            var tz = MainViewModel.Instance.TimeZones.FirstOrDefault(x => x.UtcOffset.Minutes == timeZone.GetUtcOffset(DateTime.Now).Minutes);
            if (tz != null)
                TimeZone = tz;
            else
                TimeZone = MainViewModel.Instance.TimeZones[0];
            // fill in accouts available for selecting
            AppliedControllers.CollectionChanged += AppliedControllers_CollectionChanged;
            // fill in Vendors
            /*
            var controllers = typeof(IController).Assembly.GetTypes().Where(type => type.GetInterface(typeof(IController).FullName) != null).ToList();
            for (int i = 0; i < controllers.Count(); i++)
            {
                SymbolDefinition.Add(new SymbolDefinition { Vendor = controllers[i].Name, ContractId = Name });
            }*/
            //
            AccountCandidates.CollectionChanged += AccountCandidates_CollectionChanged;
        }

        private void AccountCandidates_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            _RaisePropertyChanged("AccountCandidates");
        }

        private double _pMinOrderSize;
        [Category("Details")]
        [DisplayName("Min. Order Size")]
        public double MinOrderSize
        {
            get { return _pMinOrderSize; }
            set { _UpdateField(ref _pMinOrderSize, value); }
        }

        private double _pMaxOrderSize;
        [Category("Details")]
        [DisplayName("Max. Order Size")]
        public double MaxOrderSize
        {
            get { return _pMaxOrderSize; }
            set { _UpdateField(ref _pMaxOrderSize, value); }
        }

        private float _pRoundLotSize = 1;
        [Category("Details")]
        [DisplayName("Round Lot Size")]
        public float RoundLotSize
        {
            get { return _pRoundLotSize; }
            set { _UpdateField(ref _pRoundLotSize, value); }
        }

        private decimal _pMinTick = 1;
        [Category("Details")]
        [DisplayName("Min. Tick Size")]
        [ReadOnly(true)]
        public decimal MinTick
        {
            get { return _pMinTick; }
            set { _UpdateField(ref _pMinTick, value); }
        }

        private float _pTimeFrame;
        [JsonIgnore]
        public float TimeFrame
        {
            get { return _pTimeFrame; }
            set { _UpdateField(ref _pTimeFrame, value); }
        }

        private static DebounceDispatcher debounceTimer = new DebounceDispatcher();
        private bool _pIsDirty;
        [JsonIgnore]
        public bool IsDirty
        {
            get { return _pIsDirty; }
            set
            {
                if (_pIsDirty != value && value == false)
                {
                    debounceTimer.Debounce(1000, parm =>
                    {
                        ShowSign = false;
                    });
                }
                else if (_pIsDirty != value && value)
                {
                    ShowSign = true;
                }
                _UpdateField(ref _pIsDirty, value);
            }
        }

        private bool _pShowSign;
        [JsonIgnore]
        public bool ShowSign
        {
            get { return _pShowSign; }
            set { _UpdateField(ref _pShowSign, value); }
        }

        private AmiBroker.Controllers.TimeZone _pTimeZone;
        public AmiBroker.Controllers.TimeZone TimeZone
        {
            get { return _pTimeZone; }
            set {
                _UpdateField(ref _pTimeZone, value);
                ChangeTimeZone(value);
            }
        }
        public void CloseAllPositions()
        {
            foreach (var script in Scripts)
            {
                script.CloseAllPositions();
            }
        }
        public async void FillInContractDetails(IController controller)
        {
            if (controller.Vendor == "IB")
            {
                SymbolDefinition sd = SymbolDefinition.FirstOrDefault(x => x.Controller.Vendor == controller.Vendor);
                if (sd != null && sd.Contract == null)
                {
                    // the following line will result in non-block execution
                    IBContract c = await((IBController)controller).reqContractDetailsAsync(sd.ContractId);
                    if (c != null)
                    {
                        sd.Contract = c.Contract;
                        sd.TradingHours = c.TradingHours;
                        MinTick = c.MinTick;
                    }
                }
            }
            if (controller.Vendor == "FT")
            {
                SymbolDefinition sd = SymbolDefinition.FirstOrDefault(x => x.Controller.Vendor == controller.Vendor);
                if (sd != null && sd.Contract == null)
                {
                    // TODO:
                }
            }
        }
        public void FillInSymbolDefinition(IController controller)
        {
            // fillin symbol definitions
            SymbolDefinition sd = SymbolDefinition.FirstOrDefault(x => x.Controller.GetType() == controller.GetType());
            if (sd == null)
            {
                Dispatcher.FromThread(Controllers.OrderManager.UIThread).Invoke(() =>
                {
                    SymbolDefinition.Add(new SymbolDefinition { Controller = controller, ContractId = Name });
                });
            }
        }
        private void ChangeTimeZone(AmiBroker.Controllers.TimeZone tz)
        {
            foreach (var script in Scripts)
            {
                script.ChangeTimeZone(tz);
            }
        }
        private void AppliedControllers_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (IController item in e.NewItems)
                {
                    //
                    FillInSymbolDefinition(item);

                    item.Accounts.CollectionChanged += Accounts_CollectionChanged;
                    //item.Dummy = !item.Dummy;
                    // TODO: to be improved since this will cause whole list to be updated instead of selected item
                    MainViewModel.Instance.Dummy = !MainViewModel.Instance.Dummy;
                    if (item.IsConnected)
                    {
                        // these must be placed before await statements
                        foreach (AccountInfo account in item.Accounts)
                        {
                            if (!AccountCandidates.Any(x => x.Name == account.Name))
                                AccountCandidates.Add(account);
                        }
                        FillInContractDetails(item);                        
                    }
                }
            }
            if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (IController item in e.OldItems)
                {
                    item.Accounts.CollectionChanged += Accounts_CollectionChanged;
                    //item.Dummy = !item.Dummy;
                    MainViewModel.Instance.Dummy = !MainViewModel.Instance.Dummy;
                    foreach (AccountInfo account in item.Accounts)
                    {
                        AccountCandidates.Remove(account);
                    }
                }
            }
            //OnPropertyChanged("AppliedControllers");
        }

        private void Accounts_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (AccountInfo account in e.NewItems)
                {
                    AccountCandidates.Add(account);
                }
            }
            if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (AccountInfo account in e.OldItems)
                {
                    AccountCandidates.Remove(account);
                }
            }
        }
        public void RefreshScripts()
        {
            foreach (var item in Scripts.ToArray())
            {
                Scripts.Remove(item);
            }
        }

        public void ClearAppliedControllers()
        {
            //AppliedControllers.Clear();
            foreach (var item in AppliedControllers.ToArray())
            {
                AppliedControllers.Remove(item);
                item.Dummy = !item.Dummy;
            }
        }

        public void CopyFrom(SymbolInAction symbol)
        {
            // copy symbol details only when symbol names are the same
            if (symbol.Name == Name)
            {
                foreach (var item in symbol.SymbolDefinition)
                {
                    var tmp = SymbolDefinition.FirstOrDefault(x => x.Controller.Vendor == item.Controller.Vendor);
                    if (tmp != null)
                        tmp.ContractId = item.ContractId;
                }
                TimeZone = MainViewModel.Instance.TimeZones.FirstOrDefault(x => x.Id == symbol.TimeZone.Id);
                RoundLotSize = symbol.RoundLotSize;
                MaxOrderSize = symbol.MaxOrderSize;
                MinOrderSize = symbol.MinOrderSize;
            }
            else
            {
                MessageBoxResult result = MessageBox.Show("Symbol name is not the same as loaded one."
                    + "\nPress OK to continue but ignore copying symbol details;"
                    + "\nPress Cancel to cancel the operation.", "Warning", MessageBoxButton.OKCancel, MessageBoxImage.Warning
                    );
                if (result == MessageBoxResult.Cancel) return;
            }
            

            ClearAppliedControllers();    // clear ApplicedControllers
            foreach (var item in symbol.AppliedControllers)
            {               
                var tmp = MainViewModel.Instance.Controllers.FirstOrDefault(x => x.DisplayName == item.DisplayName);
                if (tmp != null)
                    AppliedControllers.Add(tmp);
            }
            foreach (var item in symbol.Scripts)
            {
                var tmp = Scripts.FirstOrDefault(x => x.Name == item.Name);
                if (tmp != null)
                    tmp.CopyFrom(item);
            }            
        }

        public string Name { get; set; }
        public ObservableCollection<Script> Scripts { get; set; } = new ObservableCollection<Script>();
        public ObservableCollection<IController> AppliedControllers { get; set; } = new ObservableCollection<IController>();

        [JsonIgnore]
        public ObservableCollection<AccountInfo> AccountCandidates { get; set; } = new ObservableCollection<AccountInfo>();
        public ObservableCollection<SymbolDefinition> SymbolDefinition { get; private set; } = new ObservableCollection<SymbolDefinition>();

        private bool _pIsEnabled;
        [JsonIgnore]
        public bool IsEnabled
        {
            get { return _pIsEnabled; }
            set { _UpdateField(ref _pIsEnabled, value); }
        }
    }
}

