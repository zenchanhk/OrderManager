using System;
using System.Windows;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AmiBroker;
using AmiBroker.Data;
using AmiBroker.PlugIn;
using AmiBroker.Utils;
using System.Threading;
using System.Reflection;
using IBApi;
using System.Windows.Threading;
using AmiBroker.OrderManager;
using Newtonsoft.Json;
using Dragablz.Savablz;
using System.Collections.ObjectModel;

namespace AmiBroker.Controllers
{
    public class OrderManager : IndicatorBase
    {        
        private static MainViewModel mainVM;
        private static MainWindow mainWin;
        public static readonly Thread UIThread;
        //public static MainWindow MainWin { get => mainWin; }
        static OrderManager()
        {            
            try
            {
                // create a thread  
                Thread newWindowThread = new Thread(new ThreadStart(() =>
                {
                    Application app = System.Windows.Application.Current;
                    if (app == null)
                    { app = new System.Windows.Application { ShutdownMode = ShutdownMode.OnExplicitShutdown }; }
                    // save layout after exit
                    AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
                    // create and show the window
                    mainVM = MainViewModel.Instance;
                    mainWin = new MainWindow();
                    
                    var l = Properties.Settings.Default.Layout;
                    if (string.IsNullOrWhiteSpace(l))
                    {
                        mainWin.calledOnce();
                        mainWin.Show();
                    }
                    else
                    {
                        // Restore layout
                        var windowsState = JsonConvert.DeserializeObject<LayoutWindowState<TabContentModel>[]>(l);
                        mainWin.Show();
                        WindowsStateSaver.RestoreWindowsState(mainWin.InitialTabablzControl, windowsState, m => new TabContentViewModel(m));
                    }
                    app.Run();
                    mainWin.Dispatcher.Invoke(new System.Action(() => { }), DispatcherPriority.DataBind);
                    // start the Dispatcher processing 
                    
                    Dispatcher.Run();
                }));

                // set the apartment state  
                newWindowThread.SetApartmentState(ApartmentState.STA);

                // make the thread a background thread  
                newWindowThread.IsBackground = true;

                // start the thread  
                newWindowThread.Start();
                // save for later use (update UI)
                UIThread = newWindowThread;
            }
            catch (Exception ex)
            {
                if (ex is System.Reflection.ReflectionTypeLoadException)
                {
                    var typeLoadException = ex as ReflectionTypeLoadException;
                    var loaderExceptions = typeLoadException.LoaderExceptions;
                }
            }
        }

        private static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            
        }
        /*
        public OrderManager()
        {
            //System.Diagnostics.Debug.WriteLine("Current Thread: " + Thread.CurrentThread.ManagedThreadId);
        }*/

        private static Dictionary<string, DateTime> lastBarDateTime = new Dictionary<string, DateTime>(); 
        [ABMethod]
        public void IBC(string scriptName)
        {
            if (mainWin == null) return;
            DateTime logTime = ATFloat.ABDateTimeToDateTime(AFTools.LastValue(AFDate.DateTime()));
            bool newDay = false;
            string symbolName = AFInfo.Name();
            
            if (lastBarDateTime.ContainsKey(symbolName))
            {
                if ((logTime - lastBarDateTime[symbolName]).TotalHours > 6)
                {
                    newDay = true;
                }
                lastBarDateTime[symbolName] = logTime;
            }
            else
                lastBarDateTime.Add(symbolName, logTime);

            SymbolInAction symbol = Initialize(scriptName);
            if (!symbol.IsEnabled) return;
            
            Script script = symbol.Scripts.FirstOrDefault(x => x.Name == scriptName);
            if (script != null)
            {
                if (!script.IsEnabled) return;
                // reset entries count and positions for new day
                if (newDay)
                {
                    foreach (Strategy strategy in script.Strategies)
                    {
                        strategy.ResetForNewDay();
                    }
                }
                foreach (Strategy strategy in script.Strategies)
                {
                    if (!strategy.IsEnabled) continue;

                    bool signal = false;
                    if (strategy.ActionType == ActionType.Long || strategy.ActionType == ActionType.LongAndShort)
                    {
                        signal = ATFloat.IsTrue(strategy.BuySignal.GetArray()[BarCount - 1]);
                        if (signal)
                            ProcessSignal(script, strategy, OrderAction.Buy, logTime);

                        signal = ATFloat.IsTrue(strategy.SellSignal.GetArray()[BarCount - 1]);
                        if (signal)
                            ProcessSignal(script, strategy, OrderAction.Sell, logTime);
                    }
                    if (strategy.ActionType == ActionType.Short || strategy.ActionType == ActionType.LongAndShort)
                    {
                        signal = ATFloat.IsTrue(strategy.ShortSignal.GetArray()[BarCount - 1]);
                        if (signal)
                            ProcessSignal(script, strategy, OrderAction.Short, logTime);

                        signal = ATFloat.IsTrue(strategy.CoverSignal.GetArray()[BarCount - 1]);
                        if (signal)
                            ProcessSignal(script, strategy, OrderAction.Cover, logTime);
                    }
                }
            }
        }

        private void ProcessSignal(Script script, Strategy strategy, OrderAction orderAction, DateTime logTime)
        {
            MainViewModel.Instance.Log(new Log
            {
                Time = logTime,
                Text = orderAction.ToString() + " signal generated",
                Source = script.Symbol.Name + "." + strategy.Name
            });
            
            string message = string.Empty;
            foreach (var acc in strategy.AccountsDic[orderAction])
            {
                if (ValidateSignal(strategy, strategy.AccountStat[acc.Name], OrderAction.Buy, out message))
                {
                    foreach (var account in strategy.AccountsDic[orderAction])
                    {
                        string vendor = account.Controller.Vendor;
                        BaseOrderType orderType = strategy.OrderTypesDic[orderAction].FirstOrDefault(x => x.GetType().BaseType.Name == vendor + "OrderType");
                        string contract = script.Symbol.SymbolDefinition.FirstOrDefault(x => x.Vendor == vendor + "Controller").ContractId;
                        if (orderType != null)
                        {
                            int orderId = account.Controller.PlaceOrder(account, strategy, contract, orderType, orderAction, BarCount - 1).Result;
                            if (orderId != -1)
                            {
                                Dispatcher.FromThread(UIThread).Invoke(() =>
                                {
                                    OrderInfo oi = new OrderInfo { Strategy = strategy, Account = acc, OrderAction = orderAction };
                                    strategy.AccountStat[acc.Name].AccoutStatus = AccountStatus.BuyPending;                                    
                                    MainViewModel.Instance.OrderInfoList.Add(orderId, oi);
                                });
                                MainViewModel.Instance.Log(new Log
                                {
                                    Time = logTime,
                                    Text = "Order sent",
                                    Source = script.Symbol.Name + "." + strategy.Name
                                });
                            }
                        }
                        else
                        {
                            MainViewModel.Instance.Log(new Log
                            {
                                Time = logTime,
                                Text = vendor + "OrderType not found.",
                                Source = script.Symbol.Name + "." + strategy.Name
                            });
                        }
                    }
                }
                else
                {
                    MainViewModel.Instance.Log(new Log
                    {
                        Time = logTime,
                        Text = message.TrimEnd('\n'),
                        Source = script.Symbol.Name + "." + strategy.Name
                    });
                }
            }
        }

        private bool ValidateSignal(Strategy strategy, BaseStat strategyStat, OrderAction action, out string message)
        {
            message = string.Empty;
            Script script = strategy.Script;
            BaseStat scriptStat = script.AccountStat[strategyStat.Account.Name];
            switch (action)
            {
                case OrderAction.Buy:
                    if ((strategyStat.AccoutStatus & AccountStatus.BuyPending) != 0)
                    {
                        message = "There is an pending buy order for strategy - " + strategy.Name;
                        return false;
                    }
                    if (scriptStat.LongPosition == script.MaxLongOpen)
                        message = "Max. long position(script) reached.\n";
                    break;
                case OrderAction.Short:
                    if ((strategyStat.AccoutStatus & AccountStatus.ShortPending) != 0)
                    {
                        message = "There is an pending short order for strategy - " + strategy.Name;
                        return false;
                    }
                    if (scriptStat.ShortPosition == script.MaxShortOpen)
                        message = "Max. short position(script) reached.\n";
                    break;
                case OrderAction.Sell:
                    if ((strategyStat.AccoutStatus & AccountStatus.SellPending) != 0)
                    {
                        message = "There is an pending sell order for strategy - " + strategy.Name;
                        return false;
                    }
                    if (strategyStat.LongPosition == 0)
                    {
                        message = "There is no long position for strategy - " + strategy.Name;
                        return false;
                    }
                    break;
                case OrderAction.Cover:
                    if ((strategyStat.AccoutStatus & AccountStatus.CoverPending) != 0)
                    {
                        message = "There is an pending cover order for strategy - " + strategy.Name;
                        return false;
                    }
                    if (strategyStat.ShortPosition == 0)
                    {
                        message = "There is no short position for strategy - " + strategy.Name;
                        return false;
                    }
                    break;
            }

            if (action == OrderAction.Buy || action == OrderAction.Short)
            {
                if (strategyStat.LongEntry + strategyStat.ShortEntry == strategy.MaxEntriesPerDay && strategy.MaxEntriesPerDay != 0)
                    message = "Max. entries per day reached(strategy).\n";
                if (strategy.MaxOpenPosition == strategyStat.LongPosition + strategyStat.ShortPosition && strategy.MaxOpenPosition != 0)
                    message = "Max. open position(strategy) reached.\n";
                if (script.MaxOpenPosition == scriptStat.LongPosition + scriptStat.ShortPosition && script.MaxOpenPosition != 0)
                    message = "Max. open position(script) reached.\n";
                if (script.MaxEntriesPerDay == scriptStat.LongEntry + scriptStat.ShortEntry && script.MaxEntriesPerDay != 0)
                    message = "Max. entries per day(script) reached.\n";
            }

            if (message == string.Empty)
                return true;
            else
                return false;
        }

        public static SymbolInAction Initialize(string scriptName)
        {
            bool isAdded = false;
            SymbolInAction symbol = null;
            Dispatcher.FromThread(UIThread).Invoke(() =>            
            {                
                isAdded = MainViewModel.Instance.AddSymbol(AFInfo.Name(), AFTimeFrame.Interval() / 60, out symbol);
            });
            
            if (isAdded)
            {
                ATAfl afl = new ATAfl();
                Script script = new Script(scriptName, symbol);
                script.IsEnabled = true;
                Dispatcher.FromThread(UIThread).Invoke(() =>
                {
                    symbol.Scripts.Add(script);
                });
                afl.Name = "Strategy";
                string[] strategyNames = afl.GetString().Split(new char[] { '$' });
                afl.Name = "BuySignals";
                string[] buySignals = afl.GetString().Split(new char[] { '$' });
                afl.Name = "SellSignals";
                string[] sellSignals = afl.GetString().Split(new char[] { '$' });
                afl.Name = "ShortSignals";
                string[] shortSignals = afl.GetString().Split(new char[] { '$' });
                afl.Name = "CoverSignals";
                string[] coverSignals = afl.GetString().Split(new char[] { '$' });
                afl.Name = "Prices";
                string[] prices = afl.GetString().Split(new char[] { '$' });
                afl.Name = "ActionType";
                string[] actionTypes = afl.GetString().Split(new char[] { '$' });
                for (int i = 0; i < strategyNames.Length; i++)
                {
                    Strategy s = new Strategy(strategyNames[i], script);
                    ActionType at = (ActionType)Enum.Parse(typeof(ActionType), actionTypes[i]);
                    s.ActionType = at;
                    s.Prices = new List<string>(prices[i].Split(new char[] { '%' }));
                    if (at == ActionType.Long || at == ActionType.LongAndShort)
                    {
                        s.BuySignal = new ATAfl(buySignals[i]);
                        s.SellSignal = new ATAfl(sellSignals[i]);
                    }
                    if (at == ActionType.Short || at == ActionType.LongAndShort)
                    {
                        s.ShortSignal = new ATAfl(shortSignals[i]);
                        s.CoverSignal = new ATAfl(coverSignals[i]);
                    }
                    script.Strategies.Add(s);
                }
            }
            return symbol;
        }
        
        [ABMethod]
        public void Test()
        {

        }

        [ABMethod]
        public ATArray BSe2(string scriptName)
        {
            
            // calculate bar avg price
            ATArray myTypicalPrice = (this.High + this.Low + 2 * this.Close) / 4;

            // calculate the moving average of typical price by calling the built-in MA function
            ATArray mySlowMa = AFAvg.Ma(myTypicalPrice, 20);

            // print the current value in the title of the chart pane
            Title = "myTypicalPrice = " + myTypicalPrice + " mySlowMa = " + mySlowMa;


            // returning result to AFL  script
            return mySlowMa;
        }
    }
}
