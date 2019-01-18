using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AmiBroker.OrderManager
{    
    public enum FTContractType
    {
        STK = 0,
        FUK = 1,
        BOND = 2,
        CFD = 4,
        EFP = 5,
        CASH = 6,
        FUND = 7,
        FUT = 8,
        FOP = 9,
        OPT = 10,
        WAR = 11,
        BAG = 12
    }
    public class FTOrderType : BaseOrderType
    {
        public string Name { get; set; }
        public static string Broker { get; set; } = "Futu NiuNiu Order";
        [JsonIgnore]
        public static string DateTimeFormat { get; } = "yyyyMMdd HH:mm:ss";        
        [JsonIgnore]
        public string FTCode { get; protected set; }
        [JsonIgnore]
        public IList<FTContractType> Products { get; protected set; } = new List<FTContractType>();
        public override BaseOrderType Clone()
        {
            FTOrderType ot = (FTOrderType)this.MemberwiseClone();
            return ot;
        }
    }
    public class FTMarketOrder : FTOrderType
    {
        public FTMarketOrder() : base()
        {
            Description = "A Market order is an order to buy or sell at the market bid or offer price.";            
            FTCode = "MARKET";
            Name = "Market";
        }
    }
}