using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using AmiBroker.Controllers;
using AmiBroker;
using Newtonsoft.Json;
using AmiBroker.Controllers;
using Easy.MessageHub;
using System.Reflection;

namespace AmiBroker.OrderManager
{   
    public enum StrategyStatus
    {
        None=1,
        LongPending=2,
        Long=4,
        ShortPending=8,
        Short=16,
        LongAndShort=32
    }
    public enum ActionType
    {
        Long = 0,
        Short = 1,
        LongAndShort = 2
    }
    public class SSBase : INotifyPropertyChanged
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

        private SymbolInAction _symbol;
        public SSBase(SymbolInAction symbol = null)
        {
            _symbol = symbol;
            if (_symbol != null)
                _symbol.AccountCandidates.CollectionChanged += AccountCandidates_CollectionChanged;
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

        public string Name { get; set; }

        private int _pMaxEntriesPerDay;
        public int MaxEntriesPerDay
        {
            get { return _pMaxEntriesPerDay; }
            set
            {
                if (_pMaxEntriesPerDay != value)
                {
                    _pMaxEntriesPerDay = value;
                    OnPropertyChanged("MaxEntriesPerDay");
                }
            }
        }

        private int _pMaxOpenPosition;
        public int MaxOpenPosition
        {
            get { return _pMaxOpenPosition; }
            set
            {
                if (_pMaxOpenPosition != value)
                {
                    _pMaxOpenPosition = value;
                    OnPropertyChanged("MaxOpenPosition");
                }
            }
        }
        
        private bool _pAllowMultiLong;
        public bool AllowMultiLong
        {
            get { return _pAllowMultiLong; }
            set
            {
                if (_pAllowMultiLong != value)
                {
                    _pAllowMultiLong = value;
                    OnPropertyChanged("AllowMultiLong");
                }
            }
        }

        private int _pMaxLongOpen;
        public int MaxLongOpen
        {
            get { return _pMaxLongOpen; }
            set
            {
                if (_pMaxLongOpen != value)
                {
                    _pMaxLongOpen = value;
                    OnPropertyChanged("MaxLongOpen");
                }
            }
        }

        private bool _pAllowMultiShort;
        public bool AllowMultiShort
        {
            get { return _pAllowMultiShort; }
            set
            {
                if (_pAllowMultiShort != value)
                {
                    _pAllowMultiShort = value;
                    OnPropertyChanged("AllowMultiShort");
                }
            }
        }

        private int _pMaxShortOpen;
        public int MaxShortOpen
        {
            get { return _pMaxShortOpen; }
            set
            {
                if (_pMaxShortOpen != value)
                {
                    _pMaxShortOpen = value;
                    OnPropertyChanged("MaxShortOpen");
                }
            }
        }

        private bool _pReverseSignalForcesExit;
        public bool ReverseSignalForcesExit
        {
            get { return _pReverseSignalForcesExit; }
            set
            {
                if (_pReverseSignalForcesExit != value)
                {
                    _pReverseSignalForcesExit = value;
                    OnPropertyChanged("ReverseSignalForcesExit");
                }
            }
        }
        // allow re-entry after previous attemps failure in day trade
        private bool _pAllowReEntry;
        public bool AllowReEntry
        {
            get { return _pAllowReEntry; }
            set
            {
                if (_pAllowReEntry != value)
                {
                    _pAllowReEntry = value;
                    OnPropertyChanged("AllowReEntry");
                }
            }
        }

        private int _pMaxReEntry;
        public int MaxReEntry
        {
            get { return _pMaxReEntry; }
            set
            {
                if (_pMaxReEntry != value)
                {
                    _pMaxReEntry = value;
                    OnPropertyChanged("MaxReEntry");
                }
            }
        }

        private DateTime? _pReEntryBefore;
        public DateTime? ReEntryBefore
        {
            get { return _pReEntryBefore; }
            set
            {
                if (_pReEntryBefore != value)
                {
                    _pReEntryBefore = value;
                    OnPropertyChanged("ReEntryBefore");
                }
            }
        }

        private bool _pIsNextDay;
        public bool IsNextDay
        {
            get { return _pIsNextDay; }
            set
            {
                if (_pIsNextDay != value)
                {
                    _pIsNextDay = value;
                    OnPropertyChanged("IsNextDay");
                }
            }
        }

        // Order Details
        private int _pLongPosition;
        public int LongPosition
        {
            get { return _pLongPosition; }
            set
            {
                if (_pLongPosition != value)
                {
                    if (this.GetType() == typeof(Strategy))
                    {
                        ((Strategy)this).Script.LongPosition += (value - _pLongPosition);
                    }
                    _pLongPosition = value;
                    OnPropertyChanged("LongPosition");
                }
            }
        }

        private int _pShortPosition;
        public int ShortPosition
        {
            get { return _pShortPosition; }
            set
            {
                if (_pShortPosition != value)
                {
                    if (this.GetType() == typeof(Strategy))
                    {
                        ((Strategy)this).Script.ShortPosition += (value - _pShortPosition);
                    }
                    _pShortPosition = value;
                    OnPropertyChanged("ShortPosition");
                }
            }
        }
        // daily long entry count
        private int _pLongEntries;
        [JsonIgnore]
        public int LongEntries
        {
            get { return _pLongEntries; }
            set
            {
                if (_pLongEntries != value)
                {
                    if (this.GetType() == typeof(Strategy))
                    {
                        ((Strategy)this).Script.LongEntries += (value - _pLongEntries);
                    }
                    _pLongEntries = value;
                    OnPropertyChanged("LongEntries");
                }
            }
        }
        // daily short entry count
        private int _pShortEntries;
        [JsonIgnore]
        public int ShortEntries
        {
            get { return _pShortEntries; }
            set
            {
                if (_pShortEntries != value)
                {
                    if (this.GetType() == typeof(Strategy))
                    {
                        ((Strategy)this).Script.ShortEntries += (value - _pShortEntries);
                    }
                    _pShortEntries = value;
                    OnPropertyChanged("ShortEntries");
                }
            }
        }
        public ObservableCollection<BaseOrderType> BuyOrderTypes { get; set; } = new ObservableCollection<BaseOrderType>();
		public ObservableCollection<BaseOrderType> SellOrderTypes { get; set; } = new ObservableCollection<BaseOrderType>();
        public ObservableCollection<BaseOrderType> ShortOrderTypes { get; set; } = new ObservableCollection<BaseOrderType>();
		public ObservableCollection<BaseOrderType> CoverOrderTypes { get; set; } = new ObservableCollection<BaseOrderType>();
        public ObservableCollection<AccountInfo> LongAccounts { get; set; } = new ObservableCollection<AccountInfo>();
        public ObservableCollection<AccountInfo> ShortAccounts { get; set; } = new ObservableCollection<AccountInfo>();

        private bool _pIsEnabled = true;
        [JsonIgnore]
        public bool IsEnabled
        {
            get { return _pIsEnabled; }
            set
            {
                if (_pIsEnabled != value)
                {
                    _pIsEnabled = value;
                    OnPropertyChanged("IsEnabled");
                }
            }
        }
        // for GUI use
        private VendorOrderType _pSelectedVendor;
        [JsonIgnore]
        public VendorOrderType SelectedVendor
        {
            get { return _pSelectedVendor; }
            set
            {   
                if (value != null)
                    _pSelectedVendor = value;
                OnPropertyChanged("SelectedVendor");
            }
        }

        public void Clear()
        {
            MaxEntriesPerDay = 0;
            MaxOpenPosition = 0;
            MaxReEntry = 0;
            MaxLongOpen = 0;
            MaxShortOpen = 0;
            MaxReEntry = 0;
            AllowMultiLong = false;
            AllowMultiShort = false;
            AllowReEntry = false;
            ReEntryBefore = null;
            IsNextDay = false;  // indicating if ReEntryBefore is next day for night market
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
                ot.TimeZone = tz.Id;
            }
            foreach (var ot in SellOrderTypes)
            {
                ot.TimeZone = tz.Id;
            }
            foreach (var ot in ShortOrderTypes)
            {
                ot.TimeZone = tz.Id;
            }
            foreach (var ot in CoverOrderTypes)
            {
                ot.TimeZone = tz.Id;
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
        public Strategy() { }
        public Strategy(string strategyName, Script script)
            : base(script.Symbol)
        {            
            Name = strategyName;
            Script = script;
        }
        
        public ActionType ActionType { get; set; }
        [JsonIgnore]
        public Script Script { get; private set; }  // parent
                
        private int _pLongAttemp;
        [JsonIgnore]
        public int LongAttemp
        {
            get { return _pLongAttemp; }
            set
            {
                if (_pLongAttemp != value)
                {
                    _pLongAttemp = value;
                    OnPropertyChanged("LongAttemp");
                }
            }
        }

        private int _pShortAttemp;
        [JsonIgnore]
        public int ShortAttemp
        {
            get { return _pShortAttemp; }
            set
            {
                if (_pShortAttemp != value)
                {
                    _pShortAttemp = value;
                    OnPropertyChanged("ShortAttemp");
                }
            }
        }
        
        private StrategyStatus _pStatus;
        [JsonIgnore]
        public StrategyStatus Status
        {
            get { return _pStatus; }
            set
            {
                if (_pStatus != value)
                {
                    _pStatus = value;
                    OnPropertyChanged("Status");
                }
            }
        }

        [JsonIgnore]
        public ATAfl BuySignal { get; set; }
        [JsonIgnore]
        public ATAfl SellSignal { get; set; }
        [JsonIgnore]
        public ATAfl BuyPrice { get; set; }
        [JsonIgnore]
        public ATAfl SellPrice { get; set; }
        [JsonIgnore]
        public ATAfl ShortSignal { get; set; }
        [JsonIgnore]
        public ATAfl CoverSignal { get; set; }
        [JsonIgnore]
        public ATAfl ShortPrice { get; set; }
        [JsonIgnore]
        public ATAfl CoverPrice { get; set; }
        // reset in case new day
        public void ResetForNewDay()
        {
            LongAttemp = 0;
            ShortAttemp = 0;
            LongEntries = 0;
            ShortEntries = 0;
        }
        public void CopyFrom(Strategy strategy)
        {
            MaxEntriesPerDay = strategy.MaxEntriesPerDay;
            MaxOpenPosition = strategy.MaxOpenPosition;
            ReverseSignalForcesExit = strategy.ReverseSignalForcesExit;
            AllowReEntry = strategy.AllowReEntry;
            ReEntryBefore = strategy.ReEntryBefore;
            MaxReEntry = strategy.MaxReEntry;
            if (ActionType == ActionType.Short || ActionType == ActionType.LongAndShort)
            {
                ShortOrderTypes = strategy.ShortOrderTypes;
                CoverOrderTypes = strategy.CoverOrderTypes;
                AllowMultiShort = strategy.AllowMultiShort;
                MaxShortOpen = strategy.MaxShortOpen;
                foreach (AccountInfo acc in strategy.ShortAccounts)
                {
                    AccountInfo tmp = Script.Symbol.AccountCandidates.FirstOrDefault(x => x.Name == acc.Name);
                    if (tmp != null)
                        ShortAccounts.Add(tmp);
                }
                OnPropertyChanged("ShortOrderTypes");
                OnPropertyChanged("CoverOrderTypes");
                OnPropertyChanged("ShortAccounts");
            }
            if (ActionType == ActionType.Long || ActionType == ActionType.LongAndShort)
            {
                BuyOrderTypes = strategy.BuyOrderTypes;
                SellOrderTypes = strategy.SellOrderTypes;
                AllowMultiLong = strategy.AllowMultiLong;
                MaxLongOpen = strategy.MaxLongOpen;
                foreach (AccountInfo acc in strategy.LongAccounts)
                {
                    AccountInfo tmp = Script.Symbol.AccountCandidates.FirstOrDefault(x => x.Name == acc.Name);
                    if (tmp != null)
                        LongAccounts.Add(tmp);
                }
                OnPropertyChanged("BuyOrderTypes");
                OnPropertyChanged("SellOrderTypes");
                OnPropertyChanged("LongAccounts");
            }            
        }
    }
    /// <summary>
    /// This class is used to collect the parameters of the trading logic
    /// </summary>
    public class Script : SSBase
    {        
        // for json serilization purpose
        public Script() { }
        public Script(string scriptName, SymbolInAction symbol)
            :base(symbol)
        {
            Name = scriptName;
            Symbol = symbol;
        }

        [JsonIgnore]
        public SymbolInAction Symbol { get; set; }        
        public ObservableCollection<Strategy> Strategies { get; set; } = new ObservableCollection<Strategy>();

        private bool _isEnabled = true;
        [JsonIgnore]
        public new bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                _isEnabled = value;
                foreach (var strategy in Strategies)
                {
                    strategy.IsEnabled = value;
                }
                OnPropertyChanged("IsEnabled");
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
            AllowReEntry = script.AllowReEntry;
            ReEntryBefore = script.ReEntryBefore;
            MaxReEntry = script.MaxReEntry;
            AllowMultiShort = script.AllowMultiShort;
            MaxShortOpen = script.MaxShortOpen;
            AllowMultiLong = script.AllowMultiLong;
            MaxLongOpen = script.MaxLongOpen;
            BuyOrderTypes = script.BuyOrderTypes;
            SellOrderTypes = script.SellOrderTypes;
            ShortOrderTypes = script.ShortOrderTypes;
            CoverOrderTypes = script.CoverOrderTypes;
            OnPropertyChanged("ShortOrderTypes");
            OnPropertyChanged("CoverOrderTypes");
            OnPropertyChanged("BuyOrderTypes");
            OnPropertyChanged("SellOrderTypes");
            foreach (AccountInfo acc in script.ShortAccounts)
            {
                AccountInfo tmp = Symbol.AccountCandidates.FirstOrDefault(x => x.Name == acc.Name);
                if (tmp != null)
                    ShortAccounts.Add(tmp);
                OnPropertyChanged("ShortAccounts");
            }

            foreach (AccountInfo acc in script.LongAccounts)
            {
                AccountInfo tmp = Symbol.AccountCandidates.FirstOrDefault(x => x.Name == acc.Name);
                if (tmp != null)
                    LongAccounts.Add(tmp);
                OnPropertyChanged("LongAccounts");
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
                    strategy.BuyOrderTypes = new ObservableCollection<BaseOrderType>(BuyOrderTypes);
                    strategy.SellOrderTypes = new ObservableCollection<BaseOrderType>(SellOrderTypes);
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
                    strategy.ShortOrderTypes = new ObservableCollection<BaseOrderType>(ShortOrderTypes);
                    strategy.CoverOrderTypes = new ObservableCollection<BaseOrderType>(CoverOrderTypes);
                }
                strategy.MaxEntriesPerDay = MaxEntriesPerDay;
                strategy.MaxOpenPosition = MaxOpenPosition;
                strategy.ReverseSignalForcesExit = ReverseSignalForcesExit;
                strategy.AllowReEntry = AllowReEntry;
                strategy.ReEntryBefore = ReEntryBefore;
                strategy.MaxReEntry = MaxReEntry;
            }
        }
    }
    public class SymbolDefinition : NotifyPropertyChangedBase
    {
        public string Vendor { get; set; }

        private string _pContractId;
        public string ContractId
        {
            get { return _pContractId; }
            set { _UpdateField(ref _pContractId, value); }
        }

    }
    public class SymbolInAction : INotifyPropertyChanged
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
            // fill in accouts available for selecting
            AppliedControllers.CollectionChanged += AppliedControllers_CollectionChanged;
            // fill in Vendors
            var controllers = typeof(IController).Assembly.GetTypes().Where(type => type.GetInterface(typeof(IController).FullName) != null).ToList();
            for (int i = 0; i < controllers.Count(); i++)
            {
                SymbolDefinition.Add(new SymbolDefinition { Vendor = controllers[i].Name, ContractId = Name });
            }
        }

        private float _pTimeFrame;
        [JsonIgnore]
        public float TimeFrame
        {
            get { return _pTimeFrame; }
            set
            {
                if (_pTimeFrame != value)
                {
                    _pTimeFrame = value;
                    OnPropertyChanged("TimeFrame");
                }
            }
        }

        private bool _pIsDirty;
        public bool IsDirty
        {
            get { return _pIsDirty; }
            set
            {
                if (_pIsDirty != value)
                {
                    _pIsDirty = value;
                    OnPropertyChanged("IsDirty");
                }
            }
        }

        private AmiBroker.Controllers.TimeZone _pTimeZone;
        public AmiBroker.Controllers.TimeZone TimeZone
        {
            get { return _pTimeZone; }
            set
            {
                if (_pTimeZone != value)
                {
                    _pTimeZone = value;
                    ChangeTimeZone(value);
                    OnPropertyChanged("TimeZone");
                }
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
                    item.Accounts.CollectionChanged += Accounts_CollectionChanged;
                    item.Dummy = !item.Dummy;
                    if (item.IsConnected)
                    {
                        foreach (AccountInfo account in item.Accounts)
                        {
                            AccountCandidates.Add(account);
                        }
                    }
                }
            }
            if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (IController item in e.OldItems)
                {
                    item.Accounts.CollectionChanged += Accounts_CollectionChanged;
                    item.Dummy = !item.Dummy;
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

        public void Clear()
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
            foreach (var item in symbol.SymbolDefinition)
            {
                var tmp = SymbolDefinition.FirstOrDefault(x => x.Vendor == item.Vendor);
                if (tmp != null)
                    tmp.ContractId = item.ContractId;
            }
            Clear();    // clear ApplicedControllers
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
            TimeZone = MainViewModel.Instance.TimeZones.FirstOrDefault(x => x.Id == symbol.TimeZone.Id);
        }

        public string Name { get; set; }
        public ObservableCollection<Script> Scripts { get; set; } = new ObservableCollection<Script>();
        public ObservableCollection<IController> AppliedControllers { get; set; } = new ObservableCollection<IController>();

        [JsonIgnore]
        public ObservableCollection<AccountInfo> AccountCandidates { get; } = new ObservableCollection<AccountInfo>();
        public List<SymbolDefinition> SymbolDefinition { get; private set; } = new List<SymbolDefinition>();
                
        private bool _isEnabled = true;
        [JsonIgnore]
        public bool IsEnabled {
            get => _isEnabled; 
            set
            {
                _isEnabled = value;
                foreach (var script in Scripts)
                {
                    script.IsEnabled = value;
                }
                OnPropertyChanged("IsEnabled");
            }            
        }
    }
}

