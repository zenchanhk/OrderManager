using AmiBroker.Controllers;
using FastMember;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace AmiBroker.OrderManager
{
    public class VendorOrderType
    {
        public string Name { get; set; }
        public List<BaseOrderType> OrderTypes { get; set; } = new List<BaseOrderType>();
    }
    public enum BarInterval
    {
        Tick = 0,
        K1Min = 1,
        K5Min = 5,
        K15Min = 15,
        K30Min = 30,
        KHour = 60,
        KDay = 1440,
        KWeek = 7,
        KMonth = 8,
        KYear = 9
    }
    public class CSlippage : NotifyPropertyChangedBase
    {
        private int _pSlippage;
        public int Slippage
        {
            get { return _pSlippage; }
            set { _UpdateField(ref _pSlippage, value); }
        }

        private int _pPosSize;
        public int PosSize
        {
            get { return _pPosSize; }
            set { _UpdateField(ref _pPosSize, value); }
        }
    }

    /**
     * BaseLine must be reached to activate  
     * Only if price is closing threshold, order will be placed
     * 
    */
    public class AdaptiveProfitStop : NotifyPropertyChangedBase
    {      
        // 0 - User defined, in mini tick = Stoploss
        // 1 - reading from AFL script, in mini tick = StoplossAFL
        private int _pStoplossSelector;
        public int StoplossSelector
        {
            get { return _pStoplossSelector; }
            set { _UpdateField(ref _pStoplossSelector, value); }
        }

        private int _pStoploss = 0; // in mini tick
        public int Stoploss
        {
            get { return _pStoploss; }
            set { _UpdateField(ref _pStoploss, value); }
        }

        private ATAfl _pStoplossAFL = new ATAfl("N/A");
        [JsonIgnore]
        public ATAfl StoplossAFL
        {
            get { return _pStoplossAFL; }
            set { _UpdateField(ref _pStoplossAFL, value); }
        }
        /*
        private int _pStoplossReal;
        public int StoplossReal
        {
            get { return _pStoplossReal; }
            set { _UpdateField(ref _pStoplossReal, value); }
        }*/

        private float _pEntryPrice;
        [JsonIgnore]
        public float EntryPrice
        {
            get { return _pEntryPrice; }
            set { _UpdateField(ref _pEntryPrice, value); }
        }

        private int _pLevel = 0;
        public int Level
        {
            get { return _pLevel; }
            set { _UpdateField(ref _pLevel, value); }
        }

        private double _pProfitTarget;
        public double ProfitTarget
        {
            get { return _pProfitTarget; }
            set { _UpdateField(ref _pProfitTarget, value); }
        }

        private double _pBaseLine;
        public double BaseLine
        {
            get { return _pBaseLine; }
            set { _UpdateField(ref _pBaseLine, value); }
        }

        private double _pBaseIncrement;
        public double TargetIncrement
        {
            get { return _pBaseIncrement; }
            set { _UpdateField(ref _pBaseIncrement, value); }
        }

        private double _pIncrement;
        public double DropIncrement
        {
            get { return _pIncrement; }
            set { _UpdateField(ref _pIncrement, value); }
        }

        private int _pThreshold;
        public int Threshold
        {
            get { return _pThreshold; }
            set { _UpdateField(ref _pThreshold, value); }
        }

        private int _pProfitClass;
        [JsonIgnore]
        public int ProfitClass
        {
            get { return _pProfitClass; }
            set { _UpdateField(ref _pProfitClass, value); }
        }

        private double _pHighestProfit;
        [JsonIgnore]
        public double HighestProfit
        {
            get { return _pHighestProfit; }
            set { _UpdateField(ref _pHighestProfit, value); }
        }

        private double _pStopPrice;
        [JsonIgnore]
        public double StopPrice
        {
            get { return _pStopPrice; }
            set { _UpdateField(ref _pStopPrice, value); }
        }

        private double _pCurPrice;
        [JsonIgnore]
        public double CurPrice
        {
            get { return _pCurPrice; }
            set { _UpdateField(ref _pCurPrice, value); }
        }

        [JsonIgnore]
        public ActionType ActionType { get; set; }
        [JsonIgnore]
        public Strategy Strategy { get; set; }

        public AdaptiveProfitStop()
        {
        }

        [JsonIgnore]
        public BaseOrderType OT_stopProfit { get; private set; } = new IBStopLimitOrder();
        [JsonIgnore]
        public BaseOrderType OT_stopLoss { get; private set; } = new IBStopLimitOrder();
        
        public AdaptiveProfitStop(SSBase strategy, ActionType actionType, BaseOrderType stpLmtOrder = null)
        {
            Strategy = strategy.GetType() == typeof(Strategy) ? (Strategy)strategy : null;
            OT_stopProfit = stpLmtOrder == null ? OT_stopProfit : stpLmtOrder;            
            OT_stopLoss = stpLmtOrder == null ? OT_stopLoss : stpLmtOrder.CloneObject();
            OT_stopProfit.Slippages = new ObservableCollection<CSlippage>();
            OT_stopProfit.Slippages.Add(new CSlippage { Slippage = 1, PosSize = 1 });
            OT_stopLoss.Slippages = new ObservableCollection<CSlippage>();
            OT_stopLoss.Slippages.Add(new CSlippage { Slippage = 1, PosSize = 1 });
            ActionType = actionType;
        }
        
        public void Reset()
        {            
            EntryPrice = 0;
            _highestProfitSinceLastSent = 0;
            _stoplossLastSent = 0;
            HighestProfit = 0;
            StopPrice = 0;
            ProfitClass = 0;
        }

        private double _highestProfitSinceLastSent = 0;
        private double _stoplossLastSent = 0;
        public void Calc(float curPrice)
        {
            CurPrice = curPrice;
            foreach (var stat in Strategy.AccountStat)
            {
                stat.Value.CurPrice = curPrice;
            }
            if (EntryPrice > 0)
            {
                if (Level > 0)
                {
                    if (ActionType == ActionType.Long)
                        HighestProfit = Math.Max(HighestProfit, curPrice - EntryPrice);
                    else
                        HighestProfit = Math.Max(HighestProfit, EntryPrice - curPrice);

                    // ignore the duplicated signal processing
                    if (HighestProfit != _highestProfitSinceLastSent)
                    {
                        int profit_class = HighestProfit / EntryPrice < ProfitTarget / 100 ? 0 :
                        1 + (int)((HighestProfit / EntryPrice - ProfitTarget / 100) / (ProfitTarget / 100 * TargetIncrement));
                        profit_class = profit_class > Level ? Level : profit_class;
                        ProfitClass = Math.Max(ProfitClass, profit_class);

                        if (ProfitClass > 0)
                        {
                            TypeAccessor accessor = BaseOrderTypeAccessor.GetAccessor(OT_stopProfit);
                            if (ActionType == ActionType.Long)
                            {
                                StopPrice = EntryPrice + HighestProfit * (BaseLine / 100 + DropIncrement / 100 * (ProfitClass - 1));
                                if (curPrice <= StopPrice + (float)(Threshold * Strategy.Symbol.MinTick))
                                {
                                    accessor[OT_stopProfit, "LmtPrice"] = StopPrice.ToString();
                                    accessor[OT_stopProfit, "AuxPrice"] = StopPrice.ToString();
                                    bool r = Controllers.OrderManager.ProcessSignal(Strategy.Script, Strategy, OrderAction.APSLong, DateTime.Now, OT_stopProfit);
                                    if (r) _highestProfitSinceLastSent = HighestProfit;
                                }
                            }
                            else if (ActionType == ActionType.Short)
                            {
                                StopPrice = EntryPrice - HighestProfit * (BaseLine / 100 + DropIncrement / 100 * (ProfitClass - 1));
                                if (curPrice >= StopPrice - (float)(Threshold * Strategy.Symbol.MinTick))
                                {
                                    accessor[OT_stopProfit, "LmtPrice"] = StopPrice.ToString();
                                    accessor[OT_stopProfit, "AuxPrice"] = StopPrice.ToString();
                                    bool r = Controllers.OrderManager.ProcessSignal(Strategy.Script, Strategy, OrderAction.APSShort, DateTime.Now, OT_stopProfit);
                                    if (r) _highestProfitSinceLastSent = HighestProfit;
                                }
                            }
                        }
                    }
                    
                }
                
                // stop loss
                if (Stoploss > 0)
                {
                    // ignore duplicated signal processing
                    if (Stoploss != _stoplossLastSent)
                    {
                        float sp = 0;
                        TypeAccessor accessor = BaseOrderTypeAccessor.GetAccessor(OT_stopLoss);
                        if (ActionType == ActionType.Long)
                        {
                            sp = EntryPrice - (float)(Stoploss * Strategy.Symbol.MinTick);
                            if (curPrice <= sp + (float)(Threshold * Strategy.Symbol.MinTick))
                            {
                                accessor[OT_stopLoss, "LmtPrice"] = sp.ToString();
                                accessor[OT_stopLoss, "AuxPrice"] = sp.ToString();
                                bool r = Controllers.OrderManager.ProcessSignal(Strategy.Script, Strategy, OrderAction.StoplossLong, DateTime.Now, OT_stopLoss);
                                if (r) _stoplossLastSent = Stoploss;
                            }
                        }
                        else if (ActionType == ActionType.Short)
                        {
                            sp = EntryPrice + (float)(Stoploss * Strategy.Symbol.MinTick);
                            if (curPrice >= sp - (float)(Threshold * Strategy.Symbol.MinTick))
                            {
                                accessor[OT_stopLoss, "LmtPrice"] = sp.ToString();
                                accessor[OT_stopLoss, "AuxPrice"] = sp.ToString();
                                bool r = Controllers.OrderManager.ProcessSignal(Strategy.Script, Strategy, OrderAction.StoplossShort, DateTime.Now, OT_stopLoss);
                                if (r) _stoplossLastSent = Stoploss;
                            }
                        }
                    }
                    
                }
            }
        }

    }

    public class ForceExitOrder : NotifyPropertyChangedBase
    {
        public float EntryPrice { get; set; }
        // Market Order 
        private DateTime _pFinalTime;
        public DateTime FinalTime
        {
            get { return _pFinalTime; }
            set { _UpdateField(ref _pFinalTime, value); }
        }

        private ObservableCollection<WeekDay> _pDays = new ObservableCollection<WeekDay>();
        public ObservableCollection<WeekDay> Days
        {
            get { return _pDays; }
            set { _UpdateField(ref _pDays, value); }
        }

        // before LmtOrderTime minute to final time
        private int _pLmtOrderTime;
        public int LmtOrderTime
        {
            get { return _pLmtOrderTime; }
            set { _UpdateField(ref _pLmtOrderTime, value); }
        }

        private int _pSlippage = 1;
        public int Slippage
        {
            get { return _pSlippage; }
            set { _UpdateField(ref _pSlippage, value); }
        }

        [JsonIgnore]
        public Strategy Strategy { get; set; }
        [JsonIgnore]
        public ActionType ActionType { get; set; }

        private BaseOrderType OT_pre = new IBLimitOrder();
        private BaseOrderType OT_final = new IBMarketOrder();
        private DateTime pre_sent_dt = DateTime.Now.AddYears(-1);
        private DateTime final_sent_dt = DateTime.Now.AddYears(-1);


        public ForceExitOrder()
        {

        }
        public ForceExitOrder(SSBase strategy, ActionType actionType, BaseOrderType lmtOrderType = null, BaseOrderType mktOrderTYpe = null)
        {
            Strategy = strategy.GetType() == typeof(Strategy) ? (Strategy)strategy : null;
            OT_pre = lmtOrderType != null ? lmtOrderType : OT_pre;
            OT_final = mktOrderTYpe != null ? mktOrderTYpe : OT_final;
            ActionType = actionType;
        }

        public void Reset()
        {
            EntryPrice = 0;
        }

        /* 
         * Caveat: if order-placing failed, it won't send again
         * 
         */
        public void Run(float curPrice)
        {
            if (EntryPrice > 0)
            {
                string wd = DateTime.Now.DayOfWeek.ToString().Substring(0, 3);
                DateTime lmt_time = DateTime.Parse(DateTime.Now.ToLongDateString() + " " +
                                                FinalTime.AddSeconds(LmtOrderTime * -1).ToShortTimeString());
                DateTime final_time = DateTime.Parse(DateTime.Now.ToLongDateString() + " " +
                                                FinalTime.ToShortTimeString());
                if (Days.Select(x => x.Name).ToList().Contains(wd))
                {
                    TypeAccessor accessor = BaseOrderTypeAccessor.GetAccessor(OT_pre);
                    double i = (DateTime.Now - lmt_time).TotalMinutes;
                    if ((DateTime.Now - lmt_time).TotalMinutes < 5 && lmt_time > pre_sent_dt && lmt_time <= DateTime.Now)
                    {
                        if (ActionType == ActionType.Long)
                        {
                            accessor[OT_pre, "LmtPrice"] = (curPrice - (float)Strategy.Symbol.MinTick * Slippage).ToString();
                            bool r = Controllers.OrderManager.ProcessSignal(Strategy.Script, Strategy, OrderAction.PreForceExitLong, DateTime.Now, OT_pre);
                            if (r) pre_sent_dt = DateTime.Now;
                        }
                        if (ActionType == ActionType.Short)
                        {
                            accessor[OT_pre, "LmtPrice"] = (curPrice + (float)Strategy.Symbol.MinTick * Slippage).ToString();
                            bool r = Controllers.OrderManager.ProcessSignal(Strategy.Script, Strategy, OrderAction.PreForceExitShort, DateTime.Now, OT_pre);
                            if (r) pre_sent_dt = DateTime.Now;
                        }
                    }

                    if ((DateTime.Now - lmt_time).TotalMinutes < 5 && final_time > final_sent_dt && final_time <= DateTime.Now)
                    {
                        if (ActionType == ActionType.Long)
                        {
                            bool r = Controllers.OrderManager.ProcessSignal(Strategy.Script, Strategy, OrderAction.FinalForceExitLong, DateTime.Now, OT_final);
                            if (r) final_sent_dt = DateTime.Now;
                        }
                        if (ActionType == ActionType.Short)
                        {
                            bool r = Controllers.OrderManager.ProcessSignal(Strategy.Script, Strategy, OrderAction.FinalForceExitShort, DateTime.Now, OT_final);
                            if (r) final_sent_dt = DateTime.Now;
                        }
                    }
                }
            }            
        }
    }
    public class BaseOrderType : INotifyPropertyChanged
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
        string Name { get; set; }   // Should be unique globally

        [Category("Miscellaneous")]
        [DisplayName("Position Size")]
        [Description("Position Size")]
        [ItemsSource(typeof(PositionSizeItemsSource))]
        public string PositionSize { get; set; }

        [JsonIgnore]
        public double TotalQuantity { get; set; }

        [JsonIgnore]
        public string Description { get; protected set; }

        private string _dtFormat = "yyyyMMdd HH:mm:ss";
        public string DateTimeFormat
        {
            get => _dtFormat;
            set
            {
                _dtFormat = value;
                GoodAfterTime.DateTimeFormat = _dtFormat;
                GoodTilDate.DateTimeFormat = _dtFormat;
            }
        }

        private AmiBroker.Controllers.TimeZone _timeZone;
        public AmiBroker.Controllers.TimeZone TimeZone
        {
            get => _timeZone;
            set
            {
                _timeZone = value;
                GoodTilDate.TimeZone = value;
                GoodAfterTime.TimeZone = value;
            }
        }
        public GoodTime GoodTilDate { get; set; } = new GoodTime();
        public GoodTime GoodAfterTime { get; set; } = new GoodTime();  // yyyyMMdd HH:mm:ss
        public ObservableCollection<CSlippage> Slippages { get; set; }

        public BaseOrderType()
        {
            GoodAfterTime.DateTimeFormat = DateTimeFormat;
            GoodTilDate.DateTimeFormat = DateTimeFormat;
        }

        public virtual BaseOrderType Clone()
        {
            BaseOrderType ot = (BaseOrderType)this.MemberwiseClone();
            return ot;
        }

        public virtual void CopyTo(BaseOrderType dest) { }
    }
}
