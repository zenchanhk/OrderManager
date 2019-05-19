using AmiBroker.Controllers;
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
        [JsonIgnore]
        public BaseOrderType StopProfitOrder { get; }

        [JsonIgnore]
        public BaseOrderType StopLossOrder { get; }

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

        private float _pProfitTarget;
        public float ProfitTarget
        {
            get { return _pProfitTarget; }
            set { _UpdateField(ref _pProfitTarget, value); }
        }

        private float _pBaseLine;
        public float BaseLine
        {
            get { return _pBaseLine; }
            set { _UpdateField(ref _pBaseLine, value); }
        }

        private float _pBaseIncrement;
        public float TargetIncrement
        {
            get { return _pBaseIncrement; }
            set { _UpdateField(ref _pBaseIncrement, value); }
        }

        private float _pIncrement;
        public float DropIncrement
        {
            get { return _pIncrement; }
            set { _UpdateField(ref _pIncrement, value); }
        }

        private float _pThreshold;
        public float Threshold
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

        private float _pHighestProfit;
        [JsonIgnore]
        public float HighestProfit
        {
            get { return _pHighestProfit; }
            set { _UpdateField(ref _pHighestProfit, value); }
        }

        private float _pStopPrice;
        [JsonIgnore]
        public float StopPrice
        {
            get { return _pStopPrice; }
            set { _UpdateField(ref _pStopPrice, value); }
        }

        private ActionType _actionType;
        public AdaptiveProfitStop(SSBase strategy, BaseOrderType orderSent, ActionType actionType)
        {
            Strategy = strategy.GetType() == typeof(Strategy) ? (Strategy)strategy : null;
            StopProfitOrder = orderSent;
            StopLossOrder = orderSent.CloneObject();
            _actionType = actionType;
        }
        [JsonIgnore]
        public Strategy Strategy { get; private set; }

        private float _highestProfitSinceLastSent = 0;
        private float _stoplossLastSent = 0;
        public void Calc(float curPrice)
        {
            if (EntryPrice > 0)
            {
                if (Level > 0)
                {
                    if (_actionType == ActionType.Long)
                        HighestProfit = Math.Max(HighestProfit, curPrice - EntryPrice);
                    else
                        HighestProfit = Math.Max(HighestProfit, EntryPrice - curPrice);

                    // ignore the duplicated signal processing
                    if (HighestProfit == _highestProfitSinceLastSent) return;

                    int profit_class = HighestProfit / EntryPrice < ProfitTarget / 100 ? 0 :
                        1 + (int)((HighestProfit / EntryPrice - ProfitTarget / 100) / ProfitTarget / 100 * TargetIncrement);
                    profit_class = profit_class > Level ? Level : profit_class;
                    ProfitClass = Math.Max(ProfitClass, profit_class);

                    if (ProfitClass > 0)
                    {
                        if (_actionType == ActionType.Long)
                        {
                            StopPrice = EntryPrice + HighestProfit * (BaseLine / 100 + DropIncrement / 100 * (ProfitClass - 1));
                            if (curPrice <= StopPrice + Threshold)
                            {
                                ((IBStopLimitOrder)StopProfitOrder).LmtPrice = ((decimal)StopPrice - Strategy.Symbol.MinTick).ToString();
                                ((IBStopLimitOrder)StopProfitOrder).AuxPrice = ((decimal)StopPrice + Strategy.Symbol.MinTick).ToString();
                                bool r = Controllers.OrderManager.ProcessSignal(Strategy.Script, Strategy, OrderAction.APSLong, DateTime.Now);
                                if (r) _highestProfitSinceLastSent = HighestProfit;
                            }
                        }
                        else
                        {
                            StopPrice = EntryPrice - HighestProfit * (BaseLine / 100 + DropIncrement / 100 * (ProfitClass - 1));
                            if (curPrice >= StopPrice - Threshold)
                            {
                                ((IBStopLimitOrder)StopProfitOrder).LmtPrice = ((decimal)StopPrice + Strategy.Symbol.MinTick).ToString();
                                ((IBStopLimitOrder)StopProfitOrder).AuxPrice = ((decimal)StopPrice - Strategy.Symbol.MinTick).ToString();
                                bool r = Controllers.OrderManager.ProcessSignal(Strategy.Script, Strategy, OrderAction.APSShort, DateTime.Now);
                                if (r) _highestProfitSinceLastSent = HighestProfit;
                            }
                        }
                    }
                }
                
                // stop loss
                if (Stoploss > 0)
                {
                    if (Stoploss == _stoplossLastSent) return;

                    float sp = 0;
                    if (_actionType == ActionType.Long)
                    {
                        sp = EntryPrice - (float)(Stoploss * Strategy.Symbol.MinTick);
                        if (curPrice <= sp + Threshold)
                        {
                            ((IBStopLimitOrder)StopLossOrder).LmtPrice = (sp - (float)Strategy.Symbol.MinTick).ToString();
                            ((IBStopLimitOrder)StopLossOrder).AuxPrice = (sp + (float)Strategy.Symbol.MinTick).ToString();
                            bool r = Controllers.OrderManager.ProcessSignal(Strategy.Script, Strategy, OrderAction.StoplossLong, DateTime.Now);
                            if (r) _stoplossLastSent = Stoploss;
                        }
                    }
                    else
                    {
                        sp = EntryPrice + (float)(Stoploss * Strategy.Symbol.MinTick);
                        if (curPrice >= sp - Threshold)
                        {
                            ((IBStopLimitOrder)StopLossOrder).LmtPrice = (sp + (float)Strategy.Symbol.MinTick).ToString();
                            ((IBStopLimitOrder)StopLossOrder).AuxPrice = (sp - (float)Strategy.Symbol.MinTick).ToString();
                            bool r = Controllers.OrderManager.ProcessSignal(Strategy.Script, Strategy, OrderAction.StoplossShort, DateTime.Now);
                            if (r) _stoplossLastSent = Stoploss;
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
