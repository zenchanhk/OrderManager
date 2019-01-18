using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AmiBroker.OrderManager
{
    public class SignalData
    {
        public ATAfl afl;           // get OHLC variables in AFL scripts
        public ATArray signal;      // get the corresponding OHLC arrays
        public bool intraBar;       // true if intra bar entry allowed
        public string suffix;       // suffix to identify strategies, e.g. 1...10
        public float positionSize;    // position size
        public bool stoploss;       // true if there is a stop loss price
        public bool stoploss_trigger; // true if there is a trigger for stop loss
        public bool target;         // true if there is a target price       
        public int numOfTrades;    // number of trades allowed daily
        public bool reEnterAllowed; // re-enter even if previous entry failed        
    }
    /// <summary>
    /// This class demostrates how to use .Net classes to communicate to AFL scripts.
    /// This demo system uses a lot of AFL variables. AFL script creates the variables (producer).
    /// .Net plug-in uses these variables (consumer) the drive the trading logic.
    /// </summary>
    public class OMAfls
    {
        #region ATAfl variables for interfacing with AFL scripts

        private static bool IsSet = false;

        private static Dictionary<string, SignalData> aBuys = new Dictionary<string, SignalData>();
        //private static Dictionary<string, SignalData> aBuyPrices = new Dictionary<string, SignalData>();
        private static Dictionary<string, SignalData> aSells = new Dictionary<string, SignalData>();
        //private static Dictionary<string, SignalData> aSellPrices = new Dictionary<string, SignalData>();
        private static Dictionary<string, SignalData> aShorts = new Dictionary<string, SignalData>();
        //private static Dictionary<string, SignalData> aShortPrices = new Dictionary<string, SignalData>();
        private static Dictionary<string, SignalData> aCovers = new Dictionary<string, SignalData>();
        //private static Dictionary<string, SignalData> aCoverPrices = new Dictionary<string, SignalData>();

        #endregion

        #region members values

        public Dictionary<string, SignalData> Buys = new Dictionary<string, SignalData>();
        //public Dictionary<string, SignalData> BuyPrices = new Dictionary<string, SignalData>();
        public Dictionary<string, SignalData> Sells = new Dictionary<string, SignalData>();
        //public Dictionary<string, SignalData> SellPrices = new Dictionary<string, SignalData>();
        public Dictionary<string, SignalData> Shorts = new Dictionary<string, SignalData>();
        //public Dictionary<string, SignalData> ShortPrices = new Dictionary<string, SignalData>();
        public Dictionary<string, SignalData> Covers = new Dictionary<string, SignalData>();
        //public Dictionary<string, SignalData> CoverPrices = new Dictionary<string, SignalData>();

        #endregion

        public OMAfls()
        {
            // input arrays that drive trading logic
            foreach (KeyValuePair<string, SignalData> kvp in OMAfls.aBuys)
            {
                Buys.Add(kvp.Key, new SignalData()
                {
                    signal = kvp.Value.afl.GetArray(),
                    intraBar = kvp.Value.intraBar,
                    suffix = kvp.Value.suffix,
                    stoploss = kvp.Value.stoploss,
                    positionSize = kvp.Value.positionSize
                });
            }
            foreach (KeyValuePair<string, SignalData> kvp in OMAfls.aSells)
            {
                Sells.Add(kvp.Key, new SignalData()
                {
                    signal = kvp.Value.afl.GetArray(),
                    intraBar = kvp.Value.intraBar,
                    suffix = kvp.Value.suffix
                });
            }
            foreach (KeyValuePair<string, SignalData> kvp in OMAfls.aShorts)
            {
                Shorts.Add(kvp.Key, new SignalData()
                {
                    signal = kvp.Value.afl.GetArray(),
                    intraBar = kvp.Value.intraBar,
                    suffix = kvp.Value.suffix,
                    stoploss = kvp.Value.stoploss,
                    positionSize = kvp.Value.positionSize
                });
            }
            foreach (KeyValuePair<string, SignalData> kvp in OMAfls.aCovers)
            {
                Covers.Add(kvp.Key, new SignalData()
                {
                    signal = kvp.Value.afl.GetArray(),
                    intraBar = kvp.Value.intraBar,
                    suffix = kvp.Value.suffix
                });
            }
        }

        // strategy's names are separated by ';', and name and suffix is separated by ':'
        // e.g. "gap_high_low:1;trend_high_low:2;reversal_short:1"
        public static void setStrategy(string long_s, string short_s)
        {
            if (!IsSet)
            {
                IsSet = true;

                string[] longs = long_s.Split(new String[] { ";" }, StringSplitOptions.None);
                for (int i = 0; i < longs.Length; i++)
                {
                    string[] tmp = longs[i].Split(new String[] { ":" }, StringSplitOptions.None);
                    string name = tmp[0];
                    string suffix = tmp[1];
                    bool intra_bar = int.Parse(tmp[2]) == 1;
                    float position_size = float.Parse(tmp[3]);
                    bool stop_loss = int.Parse(tmp[4]) == 1;
                    bool stop_loss_trigger = int.Parse(tmp[5]) == 1;
                    bool target = int.Parse(tmp[6]) == 1;
                    int num_of_trades = int.Parse(tmp[7]);
                    bool reetner_allowed = int.Parse(tmp[8]) == 1;
                    aBuys.Add(name, new SignalData()
                    {
                        afl = new ATAfl("Buy" + suffix),
                        suffix = suffix,
                        intraBar = intra_bar,
                        stoploss = stop_loss,
                        target = target,
                        positionSize = position_size,
                        stoploss_trigger = stop_loss_trigger,
                        numOfTrades = num_of_trades,
                        reEnterAllowed = reetner_allowed
                    });
                    //aBuyPrices.Add(name, new ATAfl("BuyPrice" + suffix));
                    aSells.Add(name, new SignalData()
                    {
                        afl = new ATAfl("Sell" + suffix),
                        suffix = suffix,
                        intraBar = intra_bar,
                        stoploss = stop_loss,
                        target = target,
                        positionSize = position_size,
                        stoploss_trigger = stop_loss_trigger,
                        numOfTrades = num_of_trades,
                        reEnterAllowed = reetner_allowed
                    });
                    //aSellPrices.Add(name, new ATAfl("SellPrice" + suffix));
                }

                string[] shorts = long_s.Split(new String[] { ";" }, StringSplitOptions.None);
                for (int i = 0; i < shorts.Length; i++)
                {
                    string[] tmp = shorts[i].Split(new String[] { ":" }, StringSplitOptions.None);
                    string name = tmp[0];
                    string suffix = tmp[1];
                    bool intra_bar = int.Parse(tmp[2]) == 1;
                    int position_size = int.Parse(tmp[3]);
                    bool stop_loss = int.Parse(tmp[4]) == 1;
                    bool stop_loss_trigger = int.Parse(tmp[5]) == 1;
                    bool target = int.Parse(tmp[6]) == 1;
                    int num_of_trades = int.Parse(tmp[7]);
                    bool reetner_allowed = int.Parse(tmp[8]) == 1;
                    aShorts.Add(name, new SignalData()
                    {
                        afl = new ATAfl("Short" + suffix),
                        suffix = suffix,
                        intraBar = intra_bar,
                        stoploss = stop_loss,
                        target = target,
                        positionSize = position_size,
                        stoploss_trigger = stop_loss_trigger,
                        numOfTrades = num_of_trades,
                        reEnterAllowed = reetner_allowed
                    });
                    //aShortPrices.Add(name, new ATAfl("ShortPrice" + suffix));
                    aCovers.Add(name, new SignalData()
                    {
                        afl = new ATAfl("Cover" + suffix),
                        suffix = suffix,
                        intraBar = intra_bar,
                        stoploss = stop_loss,
                        target = target,
                        positionSize = position_size,
                        stoploss_trigger = stop_loss_trigger,
                        numOfTrades = num_of_trades,
                        reEnterAllowed = reetner_allowed
                    });
                    //aCoverPrices.Add(name, new ATAfl("CoverPrice" + suffix));
                }
            }
        }
    }
}
