using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using IBApi;
using AmiBroker.Controllers;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using System.Collections.ObjectModel;
using System.Reflection;

namespace AmiBroker.OrderManager
{
    
    public enum IBTifType
    {
        DAY=0,
        GTC=1,
        OPG=2,
        IOC=3,
        GTD=4,
        DTC=5,
        AUC=6
    }
    public enum IBContractType
    {
        STK=0,
        FUK=1,
        BOND=2,
        CFD=4,
        EFP=5,
        CASH=6,
        FUND=7,
        FUT=8,
        FOP=9,
        OPT=10,
        WAR=11,
        BAG=12
    }    
	public class GoodTime : INotifyPropertyChanged
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
        [JsonIgnore]
        public DateTime OrderTime { get; set; } = DateTime.Now;
        public AmiBroker.Controllers.TimeZone TimeZone { get; set; }

        private DateTime _pExactTime = DateTime.Now;
        public DateTime ExactTime
        {
            get { return _pExactTime; }
            set
            {
                if (_pExactTime != value)
                {
                    _pExactTime = value;
                    OnPropertyChanged("ExactTime");
                }
            }
        }

        private int _pExactTimeValidDays = 0;
        public int ExactTimeValidDays
        {
            get { return _pExactTimeValidDays; }
            set
            {
                if (_pExactTimeValidDays != value)
                {
                    _pExactTimeValidDays = value;
                    OnPropertyChanged("ExactTimeValidDays");
                }
            }
        }

        private int _pSeconds;
        public int Seconds
        {
            get { return _pSeconds; }
            set
            {
                if (_pSeconds != value)
                {
                    _pSeconds = value;
                    OnPropertyChanged("Seconds");
                }
            }
        }

        private int _pBars;
        public int Bars
        {
            get { return _pBars; }
            set
            {
                if (_pBars != value)
                {
                    _pBars = value;
                    OnPropertyChanged("Bars");
                }
            }
        }
        [JsonIgnore]
        public string DateTimeFormat { get; set; }
        [JsonIgnore]
        public BarInterval BarInterval { get; set; } = BarInterval.K1Min;
        // 0 = None selected
        // 1 = exact time
        // 2 = seconds after order placed
        // 3 = bars after order placed
        private int _pSelectedIndex = 0;
        public int SelectedIndex
        {
            get { return _pSelectedIndex; }
            set
            {
                if (_pSelectedIndex != value && value >= 0)
                {
                    _pSelectedIndex = value;
                    OnPropertyChanged("SelectedIndex");
                }
            }
        }

        public override string ToString() 
		{
            string result = null;
			switch(SelectedIndex)
			{
				case 1:
                    string dateFormat = string.Empty;
                    string timeFormat = string.Empty;
                    Regex r1 = new Regex(@"(y*[-\/]*M*[-\/]*[Dd]*)(M*[-\/]*[Dd]*[-\/]*y*)([Dd]*[-\/]*M*[-\/]*y*)([Hh]*[:]*m*[:]*s*[ ]*t*)");
                    MatchCollection mc = r1.Matches(DateTimeFormat);
                    foreach (Match match in mc)
                    {
                        if (match.Value.Trim().Length > 0 && match.Value.Contains('d'))
                            dateFormat = match.Value;
                        if (match.Value.Trim().Length > 0 && match.Value.Contains('m'))
                            timeFormat = match.Value;
                    }
                    System.TimeZone zone = System.TimeZone.CurrentTimeZone;
                    TimeSpan span = zone.GetUtcOffset(DateTime.Now);
                    DateTime dtLocal = DateTime.Now.Add(TimeZone.UtcOffset - span);
                    // if GAT is behind Now, return null
                    if (ExactTimeValidDays == 0 && DateTime.Parse(dtLocal.ToShortTimeString()) >= DateTime.Parse(ExactTime.ToShortTimeString()))
                    {
                        result = null;
                    }
                    else
                    {
                        result = DateTimeFormat.Replace(dateFormat, dtLocal.AddDays(ExactTimeValidDays).ToString(dateFormat));
                        result = result.Replace(timeFormat, ExactTime.ToString(timeFormat));
                        result += TimeZone != null ? " " + TimeZone.Id : "";
                    }
                    break;
				case 2:
                    //result =  OrderTime.AddSeconds(Seconds).ToString(DateTimeFormat);
                    // assume using current timezone setting
                    result = DateTime.Now.AddSeconds(Seconds).ToString(DateTimeFormat);
                    break;
                case 3:
                    int div = (int)Math.Ceiling((float)(OrderTime.Minute / (int)BarInterval));
                    // in case of the beginning of bar
                    if (OrderTime.Minute % (int)BarInterval == 0)
                        div++;
                    result = DateTime.Now.AddMinutes((int)BarInterval*div).ToString(DateTimeFormat);
                    break;
            }
			return result;
        }
	}
    public class IBOrderType : BaseOrderType
    {
        public string Name { get; set; }
        public static string Broker { get; set; } = "Interactive Brokers Order";

        private bool _pTransmit = true;
        public bool Transmit
        {
            get { return _pTransmit; }
            set
            {
                if (_pTransmit != value)
                {
                    _pTransmit = value;
                    OnPropertyChanged("Transmit");
                }
            }
        }

        private IBTifType _pTif = IBTifType.DAY;
        public IBTifType Tif
        {
            get { return _pTif; }
            set
            {
                if (_pTif != value)
                {
                    _pTif = value;
                    OnPropertyChanged("Tif");
                }
            }
        }

        [JsonIgnore]
        public string OcaGroup { get; set; }
        [JsonIgnore]
        public int OcaType { get; set; } = 1;        
        [JsonIgnore]
        public string IBCode { get; protected set; }
        [JsonIgnore]
        public IList<IBContractType> Products { get; protected set; } = new List<IBContractType>();
        public override BaseOrderType Clone()
        {
            IBOrderType ot = (IBOrderType)this.MemberwiseClone();
            ot.GoodAfterTime = Helper.CloneObject<GoodTime>(GoodAfterTime);
            ot.GoodTilDate = Helper.CloneObject<GoodTime>(GoodTilDate);
            if (Slippages != null)
            {
                ot.Slippages = new ObservableCollection<CSlippage>();
                foreach (CSlippage slippage in Slippages)
                {
                    ot.Slippages.Add(new CSlippage { Slippage = slippage.Slippage, PosSize = slippage.PosSize });
                }
            }
            else
            {
                ot.Slippages = null;
            }
            
            return ot;
        }
        public override void CopyTo(BaseOrderType dest)
        {
            if (dest != null && GetType() == dest.GetType())
            {
                ((IBOrderType)dest).OcaGroup = OcaGroup;
                ((IBOrderType)dest).OcaType = OcaType;
                ((IBOrderType)dest).Transmit = Transmit;

                ((IBOrderType)dest).GoodAfterTime.ExactTime = GoodAfterTime.ExactTime;
                ((IBOrderType)dest).GoodAfterTime.ExactTimeValidDays = GoodAfterTime.ExactTimeValidDays;
                ((IBOrderType)dest).GoodAfterTime.OrderTime = GoodAfterTime.OrderTime;
                ((IBOrderType)dest).GoodAfterTime.Seconds = GoodAfterTime.Seconds;
                ((IBOrderType)dest).GoodAfterTime.Bars = GoodAfterTime.Bars;
                ((IBOrderType)dest).GoodAfterTime.SelectedIndex = GoodAfterTime.SelectedIndex;
                ((IBOrderType)dest).GoodAfterTime.BarInterval = GoodAfterTime.BarInterval;

                ((IBOrderType)dest).GoodTilDate.ExactTime = GoodTilDate.ExactTime;
                ((IBOrderType)dest).GoodTilDate.ExactTimeValidDays = GoodTilDate.ExactTimeValidDays;
                ((IBOrderType)dest).GoodTilDate.OrderTime = GoodTilDate.OrderTime;
                ((IBOrderType)dest).GoodTilDate.Seconds = GoodTilDate.Seconds;
                ((IBOrderType)dest).GoodTilDate.Bars = GoodTilDate.Bars;
                ((IBOrderType)dest).GoodTilDate.SelectedIndex = GoodTilDate.SelectedIndex;
                ((IBOrderType)dest).GoodTilDate.BarInterval = GoodTilDate.BarInterval;

                if (Slippages != null)
                {
                    ((IBOrderType)dest).Slippages = new ObservableCollection<CSlippage>();
                    foreach (CSlippage slippage in Slippages)
                    {
                        ((IBOrderType)dest).Slippages.Add(new CSlippage { Slippage = slippage.Slippage, PosSize = slippage.PosSize });
                    }
                }
                else
                {
                    ((IBOrderType)dest).Slippages = null;
                }

                BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                MethodInfo info = dest.GetType().GetMethod("OnPropertyChanged", flags);
                if (info == null) info = dest.GetType().GetMethod("_RaisePropertyChanged", flags);
                info.Invoke(dest, new object[] { "Slippages" });
            }
        }        
    }
    public class PriceItemsSource : IItemsSource
    {
        public ItemCollection GetValues()
        {
            ItemCollection items = new ItemCollection();
            if (MainViewModel.Instance.SelectedItem.GetType() == typeof(Strategy))
            {
                List<string> list = ((Strategy)MainViewModel.Instance.SelectedItem).Prices;
                foreach (var item in list)
                {
                    items.Add(item);
                }
            }            
            return items;
        }
    }

    public class AuctionOrder : IBOrderType
    {
        public AuctionOrder() : base()
        {            
            Description = "An Auction order is entered into the electronic trading system during the pre-market opening period for execution at the Calculated Opening Price (COP). If your order is not filled on the open, the order is re-submitted as a limit order with the limit price set to the COP or the best bid/ask after the market opens.";
            Products.Add(IBContractType.STK);
            Products.Add(IBContractType.FUT);
            Tif = IBTifType.AUC;
            IBCode = "MTL";
            Name = "Acution";
        }
    }
    public class IBMarketOrder : IBOrderType
    {
        public IBMarketOrder() : base()
        {
            Description = "A Market order is an order to buy or sell at the market bid or offer price.";
            foreach (IBContractType contractType in Enum.GetValues(typeof(IBContractType)))
            {
                Products.Add(contractType);
            }
            IBCode = "MKT";
            Name = "Market";
        }
    }

    public class IBMarketIfTouchedOrder : IBOrderType
    {
        [Category("Defining Prices")]
        [DisplayName("Stop Price")]
        [Description("Stop Price")]
        [ItemsSource(typeof(PriceItemsSource))]
        public string AuxPrice { get; set; }
        public IBMarketIfTouchedOrder() : base()
        {
            Description = "A Market If Touched (MIT) is an order to buy (or sell) a contract below (or above) the market. It is similar to a stop order, except that an MIT sell order is placed above the current market price, and a stop sell order is placed below.";
            Products.Add(IBContractType.BOND);
            Products.Add(IBContractType.CASH);
            Products.Add(IBContractType.CFD);            
            Products.Add(IBContractType.FOP);
            Products.Add(IBContractType.FUT);
            Products.Add(IBContractType.OPT);
            Products.Add(IBContractType.STK);
            Products.Add(IBContractType.WAR);
            IBCode = "MIT";
            Name = "Market If Touched";
        }
    }

    public class IBMarketOnCloseOrder : IBOrderType
    {
        public IBMarketOnCloseOrder() : base()
        {
            Description = "A Market On Close (MOC) order is a market order that is submitted to execute as close to the closing price as possible.";
            Products.Add(IBContractType.CFD);
            Products.Add(IBContractType.FUT);
            Products.Add(IBContractType.STK);
            Products.Add(IBContractType.WAR);
            IBCode = "MOC";
            Name = "Market On Close";
        }
    }

    public class IBMarketOnOpenOrder : IBOrderType
    {
        public IBMarketOnOpenOrder() : base()
        {
            Description = "A Market On Open (MOO) combines a market order with the OPG time in force to create an order that is automatically submitted at the market's open and fills at the market price.";
            Products.Add(IBContractType.CFD);
            Products.Add(IBContractType.FUT);
            Products.Add(IBContractType.STK);
            Products.Add(IBContractType.WAR);
            IBCode = "MKT";
            Tif = IBTifType.OPG;
            Name = "Market On Open";
        }
    }

    public class IBLimitOrder : IBOrderType
    {
        [Category("Defining Prices")]
        [DisplayName("Limit Price")]
        [Description("Limit Price")]
        [ItemsSource(typeof(PriceItemsSource))]
        public string LmtPrice { get; set; }
        public IBLimitOrder() : base()
        {
            Description = "A Limit order is an order to buy or sell at a specified price or better.";
            Products.Add(IBContractType.BOND);
            Products.Add(IBContractType.CASH);
            Products.Add(IBContractType.CFD);
            Products.Add(IBContractType.FOP);
            Products.Add(IBContractType.FUT);            
            Products.Add(IBContractType.OPT);
            Products.Add(IBContractType.STK);            
            Products.Add(IBContractType.WAR);
            IBCode = "LMT";
            Name = "Limit Order";
            Slippages = new ObservableCollection<CSlippage>();
        }
    }

    public class IBLimitIfTouchedOrder : IBOrderType
    {
        [Category("Defining Prices")]
        [DisplayName("Trigger Price")]
        [Description("Trigger Price")]
        [ItemsSource(typeof(PriceItemsSource))]
        public string AuxPrice { get; set; } // trigger price
        [Category("Defining Prices")]
        [DisplayName("Limit Price")]
        [Description("Limit Price")]
        [ItemsSource(typeof(PriceItemsSource))]
        public string LmtPrice { get; set; }
        public IBLimitIfTouchedOrder() : base()
        {
            Description = "A Limit if Touched is an order to buy (or sell) a contract at a specified price or better, below (or above) the market. This order is held in the system until the trigger price is touched. An LIT order is similar to a stop limit order, except that an LIT sell order is placed above the current market price, and a stop limit sell order is placed below.";
            Products.Add(IBContractType.BOND);
            Products.Add(IBContractType.CASH);
            Products.Add(IBContractType.CFD);
            Products.Add(IBContractType.FOP);
            Products.Add(IBContractType.FUT);
            Products.Add(IBContractType.OPT);
            Products.Add(IBContractType.STK);
            Products.Add(IBContractType.WAR);
            IBCode = "LIT";
            Name = "Limit If Touched";
            Slippages = new ObservableCollection<CSlippage>();
        }
    }

    public class IBLimitOnCloseOrder : IBOrderType
    {
        [Category("Defining Prices")]
        [DisplayName("Limit Price")]
        [Description("Limit Price")]
        [ItemsSource(typeof(PriceItemsSource))]
        public string LmtPrice { get; set; }
        public IBLimitOnCloseOrder() : base()
        {
            Description = "A Limit-on-close (LOC) order will be submitted at the close and will execute if the closing price is at or better than the submitted limit price.";
            Products.Add(IBContractType.CFD);
            Products.Add(IBContractType.FUT);
            Products.Add(IBContractType.STK);
            Products.Add(IBContractType.WAR);
            IBCode = "LOC";
            Name = "Limit On Close";
            Slippages = new ObservableCollection<CSlippage>();
        }
    }

    public class IBLimitOnOpenOrder : IBOrderType
    {
        [Category("Defining Prices")]
        [DisplayName("Limit Price")]
        [Description("Limit Price")]
        [ItemsSource(typeof(PriceItemsSource))]
        public string LmtPrice { get; set; }
        public IBLimitOnOpenOrder() : base()
        {
            Description = "A Limit-on-Open (LOO) order combines a limit order with the OPG time in force to create an order that is submitted at the market's open, and that will only execute at the specified limit price or better. Orders are filled in accordance with specific exchange rules.";
            Products.Add(IBContractType.CFD);            
            Products.Add(IBContractType.FUT);
            Products.Add(IBContractType.STK);
            Products.Add(IBContractType.WAR);
            IBCode = "LMT";
            Tif = IBTifType.OPG;
            Name = "Limit On Open";
            Slippages = new ObservableCollection<CSlippage>();
        }
    }

    public class IBMarketToLimitOrder : IBOrderType
    {
        public IBMarketToLimitOrder() : base()
        {
            Description = "A Market-to-Limit (MTL) order is submitted as a market order to execute at the current best market price. If the order is only partially filled, the remainder of the order is canceled and re-submitted as a limit order with the limit price equal to the price at which the filled portion of the order executed.";
            Products.Add(IBContractType.CFD);
            Products.Add(IBContractType.FOP);
            Products.Add(IBContractType.FUT);  
            Products.Add(IBContractType.OPT);
            Products.Add(IBContractType.STK);
            Products.Add(IBContractType.WAR);
            IBCode = "MTL";
            Name = "Market to Limit";
            Slippages = new ObservableCollection<CSlippage>();
        }
    }

    public class IBMarketWithProtectionOrder : IBOrderType
    {
        public IBMarketWithProtectionOrder() : base()
        {
            Description = "This order type is useful for futures traders using Globex. A Market with Protection order is a market order that will be cancelled and resubmitted as a limit order if the entire order does not immediately execute at the market price. The limit price is set by Globex to be close to the current market price, slightly higher for a sell order and lower for a buy order.";
            Products.Add(IBContractType.FOP);
            Products.Add(IBContractType.FUT);            
            IBCode = "MKT PRT";
            Name = "Market with Protection";
        }
    }

    public class IBStopOrder : IBOrderType
    {
        [Category("Defining Prices")]
        [DisplayName("Stop Price")]
        [Description("Stop Price")]
        [ItemsSource(typeof(PriceItemsSource))]
        public string AuxPrice { get; set; } // stop price
        public IBStopOrder() : base()
        {
            Description = "A Stop order is an instruction to submit a buy or sell market order if and when the user-specified stop trigger price is attained or penetrated. ";
            Products.Add(IBContractType.BAG);
            Products.Add(IBContractType.CASH);
            Products.Add(IBContractType.CFD);
            Products.Add(IBContractType.FOP);
            Products.Add(IBContractType.FUT);
            Products.Add(IBContractType.OPT);
            Products.Add(IBContractType.STK);
            Products.Add(IBContractType.WAR);
            IBCode = "STP";
            Name = "Stop Order";
        }
    }

    public class IBStopLimitOrder : IBOrderType
    {
        [Category("Defining Prices")]
        [DisplayName("Stop Price")]
        [Description("Stop Price")]
        [ItemsSource(typeof(PriceItemsSource))]
        public string AuxPrice { get; set; } // stop price
        [Category("Defining Prices")]
        [DisplayName("Limit Price")]
        [Description("Limit Price")]
        [ItemsSource(typeof(PriceItemsSource))]
        public string LmtPrice { get; set; }
        public IBStopLimitOrder() : base()
        {
            Description = "A Stop-Limit order is an instruction to submit a buy or sell limit order when the user-specified stop trigger price is attained or penetrated.";
            Products.Add(IBContractType.CASH);
            Products.Add(IBContractType.CFD);
            Products.Add(IBContractType.FOP);
            Products.Add(IBContractType.FUT);
            Products.Add(IBContractType.OPT);
            Products.Add(IBContractType.STK);
            Products.Add(IBContractType.WAR);
            IBCode = "STP LMT";
            Name = "Stop Limit";
            Slippages = new ObservableCollection<CSlippage>();
        }
    }

    public class IBStopProtectionOrder : IBOrderType
    {
        [Category("Defining Prices")]
        [DisplayName("Stop Price")]
        [Description("Stop Price")]
        [ItemsSource(typeof(PriceItemsSource))]
        public string AuxPrice { get; set; } // stop price
        public IBStopProtectionOrder() : base()
        {
            Description = "A Stop with Protection order combines the functionality of a stop limit order with a market with protection order. The order is set to trigger at a specified stop price. When the stop price is penetrated, the order is triggered as a market with protection order.";
            Products.Add(IBContractType.FUT);
            IBCode = "STP PRT";
            Name = "Stop with Protection";
        }
    }

    public class IBStopTrailingOrder : IBOrderType
    {
        [Category("Defining Prices")]
        [DisplayName("Trailing Percent")]
        [Description("Trailing Percent")]
        public float TrailingPercent { get; set; }
        [Category("Defining Prices")]
        [DisplayName("Trail Stop Price")]
        [Description("Trail Stop Price")]
        [ItemsSource(typeof(PriceItemsSource))]
        public string TrailStopPrice { get; set; }
        public IBStopTrailingOrder() : base()
        {
            Description = "A sell trailing stop order sets the stop price at a fixed amount below the market price with an attached \"trailing\" amount.";
            Products.Add(IBContractType.CASH);
            Products.Add(IBContractType.CFD);
            Products.Add(IBContractType.FOP);
            Products.Add(IBContractType.FUT);
            Products.Add(IBContractType.OPT);
            Products.Add(IBContractType.STK);
            Products.Add(IBContractType.WAR);
            IBCode = "TRAIL";
            Name = "Trailing Stop";
        }
    }

    public class IBStopLimitTrailingOrder : IBOrderType
    {
        [Category("Defining Prices")]
        [DisplayName("Limit Price")]
        [Description("Limit Price")]
        [ItemsSource(typeof(PriceItemsSource))]
        public string LmtPrice { get; set; }
        [Category("Defining Prices")]
        [DisplayName("Stop Price")]
        [Description("Stop Price")]
        [ItemsSource(typeof(PriceItemsSource))]
        public string AuxPrice { get; set; }
        [Category("Defining Prices")]
        [DisplayName("Trail Stop Price")]
        [Description("Trail Stop Price")]
        [ItemsSource(typeof(PriceItemsSource))]
        public string TrailStopPrice { get; set; }
        public IBStopLimitTrailingOrder() : base()
        {
            Description = "A trailing stop limit order is designed to allow an investor to specify a limit on the maximum possible loss, without setting a limit on the maximum possible gain.";
            Products.Add(IBContractType.BOND);
            Products.Add(IBContractType.CASH);
            Products.Add(IBContractType.CFD);
            Products.Add(IBContractType.FOP);
            Products.Add(IBContractType.FUT);
            Products.Add(IBContractType.OPT);
            Products.Add(IBContractType.STK);
            Products.Add(IBContractType.WAR);
            IBCode = "TRAIL";
            Name = "Trailing Stop Limit";
            Slippages = new ObservableCollection<CSlippage>();
        }
    }
}
