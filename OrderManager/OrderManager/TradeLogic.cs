using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AmiBroker.OrderManager
{
    public class TradeLogic : IndicatorBase
    {
        // contructor parameters
        private StateMachine ibState;
        private OMParams ibParams;
        private OMAfls ibAfls;

        private string ticker;
        private float tradingTimeFrame;

        // IBController vars
        private BrokerIB.IBController ibc;
        private ConnectionStatus ibStatus;
        private bool ibcOk;

        private ATArray barIndex;                           // array index
        private ATArray barDateTime;                        // Bar date & time data

        private int signalBarIndex;                         // bar index of the bar where the last entry occured (if we manually resend last orders, then it is different from fLastBar)
        //                                                  // this is where we look for entry, stop && target prices
        private int lastBarIndex;                           // bar index of the last bar

        //private bool[] in_IntraBar, out_IntraBar;                            // intra bar settings for strategies
        // if intra bar is true, signal bar index will equal to last bar index;
        // otherwise, signalBarIndex = lastBarIndex - 1

        internal TradeLogic(IBStateMachine ibState, string ticker, float timeFrame)
        {
            this.ibState = ibState;
            this.ticker = ticker;
            this.tradingTimeFrame = timeFrame;
            //this.in_IntraBar = in_intraBar.Split(new String[] { ";" }, StringSplitOptions.None).Select(chr => chr == "true").ToArray();
            //this.out_IntraBar = out_intraBar.Split(new String[] { ";" }, StringSplitOptions.None).Select(chr => chr == "true").ToArray();
            ibc = BrokerIB.IBController.GetInstance();
        }

        public bool ExecuteTradeLogic()
        {
            // Bar date & time data
            barDateTime = AFDate.DateTime();
            barIndex = AFDate.BarIndex();

            // indexes used at trade command functions and reading status vars
            lastBarIndex = BarCount - 1;
            // bar index where we check buy, sell, etc. signals
            //signalBarIndex = lastBarIndex - 1;

            // execute ParamXXX() calls to collect all parameter's of trading logic
            ibParams = new IBParams();

            // read all AFL vars of trade commands and status into member vars
            ibAfls = new IBAfls();

            // get and check IBController
            ibStatus = ibc.IsConnected();
            ibcOk = ibStatus == ConnectionStatus.Connected || ibStatus == ConnectionStatus.ConnectedWithMsgs;

            // ---------------------------------------------------------------------
            // display the remaining seconds from the last bar on the chart
            // ---------------------------------------------------------------------

            float fBarRemaining = YTrading.BarRemainingTime();
            YTrace.PrintMessage("");
            YTrace.PrintMessage(fBarRemaining.ToString("###0") + "sec");

            // ---------------------------------------------------------------------
            // trace info header
            // ---------------------------------------------------------------------

            YTrace.TraceNI("************* Automatic refresh *************", YTrace.TraceLevel.Information);

            // trace info of the last bar and actual time
            float fLastBarTime = AFTools.LastValue(AFDate.TimeNum());
            float fLastBarDate = AFTools.LastValue(AFDate.DateNum());
            float fCurrentTime = AFDate.Now(NowFormatNumber.TimeNum);
            float fCurrentDate = AFDate.Now(NowFormatNumber.DateNum);
            YTrace.Trace("Bar: " + ATFloat.ABDateTimeToDateTime(AFTools.LastValue(AFDate.DateTime())).ToShortTimeString() + " Time left: " + fBarRemaining.ToString("#0") + "s", YTrace.TraceLevel.Information);

            // ---------------------------------------------------------------------
            // check global critical errors (these prevent the trading of all tickers)
            // ---------------------------------------------------------------------

            if (CheckGlobalErrors())
                return false;

            // ---------------------------------------------------------------------
            // check critical errors (these prevent the trading of this ticker)
            // ---------------------------------------------------------------------

            if (CheckChartError())
                return false;

            // ---------------------------------------------------------------------
            // check warnings (these may have effects on the trading of all the tickers)
            // ---------------------------------------------------------------------

            CheckGlobalWarnings();

            // ---------------------------------------------------------------------
            // check global warnings (these may have effects on the trading of this ticker)
            // ---------------------------------------------------------------------

            CheckWarning();

            // ---------------------------------------------------------------------
            // main trading logic
            // ---------------------------------------------------------------------

            // update the status of the IB orders and make state transition if needed
            ibState.Update();

            // if IB is in a transient state or trading is disabled, we do nothing
            if (ibState.Status == TradeStatus.Delayed)
            {
                YTrace.Trace("IB interface is in delay mode.", YTrace.TraceLevel.Information);
            }
            else
                if (!ibParams.IBCreateOrder)
            {
                YTrace.Trace("Trading is disabled by the user.", YTrace.TraceLevel.Information);
            }
            else
                    if (ibState.Status == TradeStatus.Error)
            {
                YTrace.Trace("Trading error. Trading is prevented.", YTrace.TraceLevel.Information);
            }
            else
            {
                // execute trading commands (events)
                ExecuteCommands(fLastBarTime);
            }

            // ---------------------------------------------------------------------
            // display results
            // ---------------------------------------------------------------------

            DisplayStatus();

            return true;
        }

        #region Executing trading commands

        private void ExecuteCommands(float fLastBarTime)
        {
            //if new bar exists or user has initiated a manual trade
            /*
            if (ibState.LastProcessedBarTime != fLastBarTime
             || ibParams.UserActionClosePosition
             || ibParams.UserActionManualBuy
             || ibParams.UserActionManualShort)*/
            {
                /*
                ibState.LastProcessedBarTime = fLastBarTime;

                if (ibParams.UserActionClosePosition || ibParams.UserActionManualBuy || ibParams.UserActionManualShort)
                    YTrace.Trace("Manually initiated trade has been started.", 0);
                else */
                YTrace.Trace("Automatic position and order handling has started.", 0);

                bool buy;
                bool sshort;
                bool sell;
                bool cover;
                Dictionary<string, List<string>> strategies = ReadSignal(out buy, out sshort, out sell, out cover); // || ibParams.UserActionManualBuy;
                                                                                                                    //bool sshort = ReadSignal(Short, signalBarIndex); // || ibParams.UserActionManualShort;
                                                                                                                    //bool sell = ReadSignal(Sell, signalBarIndex);
                                                                                                                    //bool cover = ReadSignal(Cover, signalBarIndex);

                YTrace.Trace("Commands Buy:" + buy.ToString() + ", Short:" + sshort.ToString() + ", Sell:" + sell.ToString() + ", Cover:" + cover.ToString(), YTrace.TraceLevel.Information);
                YTrace.Trace("Entry=" + String.Join(";", strategies["entry"].ToArray()) + " " +
                             "Exit=" + String.Join(";", strategies["exit"].ToArray()), YTrace.TraceLevel.Information);

                ExecuteEntryCommands(buy, sshort, strategies["buy"], strategies["short"]);

                ExecuteCloseCommands(sell, cover, strategies["sell"], strategies["cover"]);
            }
        }

        string warning_msg = "More than one strategies in {0} are entering at {1}: {2}";
        private void ExecuteEntryCommands(bool buy, bool sshort, List<string> buys, List<string> shorts)
        {
            if (ibParams.UserActionAllowTrading)
            {
                if (buy)
                {
                    // check if multiple buys entering at the same time
                    if (buys.Count > 1)
                    {
                        YTrace.Trace(string.Format(warning_msg, "buys", DateTime.Now,
                            string.Join("; ", buys.ToArray())), YTrace.TraceLevel.Information);
                    }
                    string s = buys[0];
                    int signalBarIndex = ibAfls.Buys[s].intraBar ? lastBarIndex : lastBarIndex - 1;
                    string suffix = ibAfls.Buys[s].suffix;
                    float pos_size = ibAfls.Buys[s].positionSize;

                    float fEntryPrice = AFMisc.GetRTDataForeign("Ask", ticker);
                    // this check is needed for testing with unregistered AmiBroker
                    if (ATFloat.IsNull(fEntryPrice))
                        fEntryPrice = Close[lastBarIndex];
                    float fLossPrice = ibAfls.Buys[s].stoploss ? (new ATAfl("stoploss" + suffix)).GetArray()[signalBarIndex] : 0;
                    float fTargetOrderPrice = ibAfls.Buys[s].target ? (new ATAfl("target" + suffix)).GetArray()[signalBarIndex] : 0;
                    float fBuyPrice = (new ATAfl("buyprice" + suffix)).GetArray()[signalBarIndex];
                    float fOrderSize = CalculatePositionSize((fEntryPrice - fLossPrice) / ibState.tickSize);
                    fOrderSize = Math.Min(pos_size, Math.Min(fOrderSize, ibParams.MaxLongOpen));

                    string msg = "BUY:" + fOrderSize.ToString() + " @" + fEntryPrice.ToString() + " Stop:" + fLossPrice.ToString() + " Target: " + fTargetOrderPrice.ToString();
                    IBController.LogMessage(ticker, msg);
                    YTrace.Trace(msg, YTrace.TraceLevel.Information);

                    if (fOrderSize > 0)
                    {
                        ibState.EnterLong(s, fOrderSize, fBuyPrice, fLossPrice, fTargetOrderPrice);

                        YTrace.Trace("Entry Price: ~" + fEntryPrice.ToString("#.####"), YTrace.TraceLevel.Information);
                    }
                }

                if (sshort)
                {
                    if (shorts.Count > 1)
                    {
                        YTrace.Trace(string.Format(warning_msg, "shorts", DateTime.Now,
                            string.Join("; ", shorts.ToArray())), YTrace.TraceLevel.Information);
                    }
                    string s = shorts[0];
                    int signalBarIndex = ibAfls.Shorts[s].intraBar ? lastBarIndex : lastBarIndex - 1;
                    string suffix = ibAfls.Shorts[s].suffix;
                    float pos_size = ibAfls.Shorts[s].positionSize;

                    float fEntryPrice = AFMisc.GetRTDataForeign("Bid", ticker);
                    // this check is needed for testing with unregistered AmiBroker
                    if (ATFloat.IsNull(fEntryPrice))
                        fEntryPrice = Close[lastBarIndex];
                    float fLossPrice = ibAfls.Shorts[s].stoploss ? (new ATAfl("stoploss" + suffix)).GetArray()[signalBarIndex] : 0;
                    float fTargetOrderPrice = ibAfls.Shorts[s].target ? (new ATAfl("target" + suffix)).GetArray()[signalBarIndex] : 0;
                    float fShortPrice = (new ATAfl("shortprice" + suffix)).GetArray()[signalBarIndex];
                    float fOrderSize = CalculatePositionSize((fLossPrice - fEntryPrice) / ibState.tickSize);
                    fOrderSize = Math.Min(pos_size, Math.Min(fOrderSize, ibParams.MaxShortOpen));

                    string msg = "SHORT:" + fOrderSize.ToString() + " @" + fEntryPrice.ToString() + " Stop:" + fLossPrice.ToString() + " Target: " + fTargetOrderPrice.ToString();
                    IBController.LogMessage(ticker, msg);
                    YTrace.Trace(msg, YTrace.TraceLevel.Information);

                    if (fOrderSize > 0)
                    {
                        ibState.EnterShort(s, fOrderSize, fShortPrice, fLossPrice, fTargetOrderPrice);

                        YTrace.Trace("Entry Price: ~" + fEntryPrice.ToString("#.####"), YTrace.TraceLevel.Information);
                    }
                }
            }
            else
            {
                if (buy || sshort)
                {
                    IBController.LogMessage(ticker, "Opening new position is disabled by user. Valid trade command has been dismissed.");
                }
            }
        }

        private void ExecuteCloseCommands(bool sell, bool cover, List<string> sells, List<string> covers)
        {
            float positionSize = ibState.lastPositionSize;
            float fExitPrice = AFMisc.GetRTDataForeign("Bid", ticker);
            // this check is needed for testing with unregistered AmiBroker
            if (ATFloat.IsNull(fExitPrice))
                fExitPrice = Close[lastBarIndex];
            float orderSize = AFMath.Abs(positionSize);

            if (sell)
            {
                IBController.LogMessage(ticker, "SELL: " + orderSize.ToString() + " @" + fExitPrice.ToString());
                ibState.ExitLong(orderSize);
            }
            if (cover)
            {
                IBController.LogMessage(ticker, "COVER: " + orderSize.ToString() + " @" + fExitPrice.ToString());
                ibState.ExitShort(orderSize);
            }
            /*
            else if (ibParams.UserActionClosePosition)
            {
                if (ibState.Step == TradeStep.Long)
                {
                    IBController.LogMessage(ticker, "Manual SELL: " + orderSize.ToString() + " @" + fExitPrice.ToString());
                    ibState.ExitLong(orderSize);
                }
                else if (ibState.Step == TradeStep.Short)
                {
                    IBController.LogMessage(ticker, "Manual SELL: " + orderSize.ToString() + " @" + fExitPrice.ToString());
                    ibState.ExitShort(orderSize);
                }
            }*/
        }

        #endregion

        #region Private helpers

        /// <summary>
        /// Check for global errors. Global errors prevent trading of all tickers.
        /// </summary>
        /// <returns>true, if there is a global error. false, otherwise.</returns>
        private bool CheckGlobalErrors()
        {
            string error = string.Empty;

            // ---------------------------------------------------------------------
            // check if any critical error condition exists
            // ---------------------------------------------------------------------

            // check if TWS and IBController are up and running
            if (!ibcOk)
            {
                error = "TWS/IBController is not accessable!";
            }

            return HandleGlobalErrors(error);
        }

        /// <summary>
        /// Handles the global errors.
        /// A notification is sent for any new global error.
        /// A notification is sent when ALL global error are resolved.
        /// Global error messages are logged for ALL tickers once.
        /// </summary>
        private bool HandleGlobalErrors(string message)
        {
            if (message.Length == 0)
                return false;

            string tmp = "Global error: " + message;

            // write to log file
            IBController.LogMessage(ticker, tmp);
            // display it on chart pane
            IBController.DisplayMessage(tmp, Color.Red);
            // write to trace log
            YTrace.Trace(tmp, YTrace.TraceLevel.Error);

            return true;
        }

        /// <summary>
        /// Check for global warnings. These warnings do not prevent execution of trading logic for all tickers but they may have affect on them.
        /// </summary>
        private void CheckGlobalWarnings()
        {
            const float minCashBalance = 5000.0f;

            string warning = string.Empty;

            // check account balance
            string totalCashBalanceStr = ibc.GetAccountValue(AccountField.TotalCashBalance);
            float totalCashBalance = 0;
            float.TryParse(totalCashBalanceStr, out totalCashBalance);
            if (totalCashBalance <= minCashBalance)
            {
                warning = "Total cash balance is lower than the minimum required.";
            }

            HandleGlobalWarnings(warning);

        }

        /// <summary>
        /// Handles the global warnings.
        /// A notification is sent for any new global warning
        /// </summary>
        /// <param name="message">The message.</param>
        private void HandleGlobalWarnings(string message)
        {
            if (message.Length == 0)
                return;

            string tmp = "Global warning: " + message;
            // write to log file
            IBController.LogMessage(ticker, tmp);
            // display it on chart pane
            IBController.DisplayMessage(tmp, Color.Orange);
            // write to trace log
            YTrace.Trace(tmp, YTrace.TraceLevel.Error);
        }

        private bool CheckChartError()
        {
            // ---------------------------------------------------------------------
            // check if any critical error condition exists
            // ---------------------------------------------------------------------

            string error = string.Empty;

            // check if chart is refreshed and up to date
            if (!YTrading.IsChartUptodate(15))
            {
                error = "Chart is not up to date!";
            }
            else
                /* This check has been removed to make the sample work in test environments where AmiBroker is not registered.
                // check if live data is available while trading is allowed
                if (ibParams.IBCreateOrder && ATFloat.IsNull(AFMisc.GetRTDataForeign("Ask", ticker)))
                {
                    ibState.LastCriticalError.Set("No live data!");
                }
                else
                */
                // if trading functions are !allowed
                if (!ibParams.IBCreateOrder)
            {
                error = "ALL autotrading functions are DISABLED! Even existing positions cannot be closed automatically!";
            }
            else
                    // check if chart is of right interval
                    if (AFTimeFrame.Interval() != tradingTimeFrame)        // Time chart and interval is ok
            {
                error = "Chart is not in the right timeframe!";
            }
            else
                        // check if there are enough bars for main logic to work
                        if (signalBarIndex < IBParams.requiredBarsToTrade)
            {
                error = "Not enough bars (fActualBar: " + signalBarIndex.ToString() + ")!";
            }

            return HandleChartError(error);
        }

        /// <summary>
        /// Handles the ticker errors.
        /// </summary>     
        private bool HandleChartError(string message)
        {
            if (message.Length > 0)
            {
                string tmp = "Chart Error: " + message;

                // display it on chart pane
                IBController.DisplayMessage(tmp, Color.Red);

                // write to trace log
                YTrace.Trace(tmp, YTrace.TraceLevel.Error);

                // write to log file
                IBController.LogMessage(ticker, tmp);

                return true;
            }
            else
                return false;
        }

        /// <summary>
        /// Check for ticker warnings. These warnings do not prevent execution of trading logic for the tickers but they may have affect on it.
        /// </summary>
        private void CheckWarning()
        {
            string warning = string.Empty;

            if (!ibParams.IBTransferOrder && ibParams.IBCreateOrder)
                warning = "Orders are not transmitted immediately!";

            else if (!ibParams.UserActionAllowTrading && ibParams.IBCreateOrder)
                warning = "No new position is allowed to be opened!";

            // show warning on the chart, in the trace
            if (warning.Length > 0)
            {
                string tmp = "Ticker warning: " + warning;

                // display it on chart pane
                IBController.DisplayMessage(tmp, Color.Orange);

                // write to trace log
                YTrace.Trace(tmp, YTrace.TraceLevel.Information);
            }
        }

        private void DisplayStatus()
        {
            AFGraph.GfxSetBkMode(0f);

            string posMsg = "ERROR";
            switch (ibState.Step)
            {
                case TradeStep.Flat:
                    posMsg = "Flat";
                    break;
                case TradeStep.Long:
                    posMsg = "Long";
                    break;
                case TradeStep.Short:
                    posMsg = "Short";
                    break;
            }

            Color msgColor = Color.Red;
            string stateMsg = "ERROR";

            switch (ibState.Status)
            {
                case TradeStatus.Error:
                    msgColor = Color.Red;
                    stateMsg = "ERROR";
                    break;
                case TradeStatus.Attention:
                    msgColor = Color.Orange;
                    stateMsg = "WARNING";
                    break;
                case TradeStatus.Ok:
                    msgColor = Color.Green;
                    stateMsg = "Ok";
                    break;
                case TradeStatus.Delayed:
                    msgColor = Color.Grey50;
                    stateMsg = "Delayed";
                    break;
            }


            AFGraph.GfxSetTextColor(msgColor);
            AFGraph.GfxSelectFont("Ariel", 20, 700, ATFloat.False, ATFloat.False, 0);
            AFGraph.GfxSetTextAlign(2); //right, top
            AFGraph.GfxTextOut(posMsg + ", " + stateMsg, AFMisc.StatusV(AFMisc.PXWidth).GetFloat() - 20, 50);
        }

        // 
        private bool ReadSignal(ATArray command, int barIndex)
        {
            if (barIndex < 0 || barIndex >= BarCount)
                return false;

            return ATFloat.IsTrue(command[barIndex]);  // it check for Null!
        }

        // commands - array of signals
        // intraBar - array of intra bar setting
        // out buy, sshort, sell, cover - returned values
        // return - strategy codes
        private Dictionary<string, List<string>> ReadSignal(out bool buy, out bool sshort, out bool sell, out bool cover)
        {
            Dictionary<string, List<string>> cmd = new Dictionary<string, List<string>>();
            cmd.Add("buy", new List<string>());
            cmd.Add("sell", new List<string>());
            cmd.Add("short", new List<string>());
            cmd.Add("cover", new List<string>());
            buy = sell = sshort = cover = false;
            foreach (KeyValuePair<string, SignalData> kvp in ibAfls.Buys)
            {
                int idx = kvp.Value.intraBar ? lastBarIndex : lastBarIndex - 1;
                buy = ATFloat.IsTrue(kvp.Value.signal[idx]);
                if (buy)
                {
                    cmd["buy"].Add(kvp.Key);
                }
            }
            foreach (KeyValuePair<string, SignalData> kvp in ibAfls.Sells)
            {
                int idx = kvp.Value.intraBar ? lastBarIndex : lastBarIndex - 1;
                sell = ATFloat.IsTrue(kvp.Value.signal[idx]);
                if (sell)
                {
                    cmd["sell"].Add(kvp.Key);
                }
            }
            foreach (KeyValuePair<string, SignalData> kvp in ibAfls.Shorts)
            {
                int idx = kvp.Value.intraBar ? lastBarIndex : lastBarIndex - 1;
                sshort = ATFloat.IsTrue(kvp.Value.signal[idx]);
                if (sshort)
                {
                    cmd["short"].Add(kvp.Key);
                }
            }
            foreach (KeyValuePair<string, SignalData> kvp in ibAfls.Covers)
            {
                int idx = kvp.Value.intraBar ? lastBarIndex : lastBarIndex - 1;
                cover = ATFloat.IsTrue(kvp.Value.signal[idx]);
                if (cover)
                {
                    cmd["cover"].Add(kvp.Key);
                }
            }
            return cmd;
        }

        // Note: this sizing method does not take care about acoount base currency, and security's base currency!
        private float CalculatePositionSize(float riskedTicks)
        {
            YTrace.Trace("Calculate position size for risked ticks:" + riskedTicks.ToString(), YTrace.TraceLevel.Information);

            // get cash balance
            string totalCashBalanceStr = ibc.GetAccountValue(AccountField.TotalCashBalance);
            YTrace.Trace("Total cash balance:" + totalCashBalanceStr, YTrace.TraceLevel.Information);
            float totalCashBalance;
            float.TryParse(totalCashBalanceStr, out totalCashBalance);

            // calc position size
            float riskedCapital = totalCashBalance * (ibParams.TradeRisk / 100);
            YTrace.Trace("Risked capital:" + riskedCapital.ToString("#.00"), YTrace.TraceLevel.Information);

            float calculatedPositionSize = 0;
            if (ibState.marginDeposit > 0)
            {
                // future mode
                calculatedPositionSize = (float)Math.Floor(riskedCapital / ibState.marginDeposit);
                YTrace.Trace("Calculated position size (future mode):" + calculatedPositionSize.ToString("#.00"), YTrace.TraceLevel.Information);
            }
            else
            {
                calculatedPositionSize = riskedCapital / ibState.pointValue / ibState.tickSize / riskedTicks;
                YTrace.Trace("Calculated position size:" + calculatedPositionSize.ToString("#.00"), YTrace.TraceLevel.Information);
            }

            float positionSize = AFMath.Round(calculatedPositionSize / ibState.roundLotSize) * ibState.roundLotSize;
            YTrace.Trace("Rounded position size:" + positionSize.ToString(), YTrace.TraceLevel.Information);

            return positionSize;
        }

        #endregion
    }
}
