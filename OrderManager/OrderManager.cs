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
    public class OrderStrategyEventArgs : EventArgs
    {
        public string OrderId { get; set; }
        public string Strategy { get; set; }
        public OrderStrategyEventArgs(string orderID, string strategy)
        {
            OrderId = orderID;
            Strategy = strategy;
        }
    }
    public class OrderManager : IndicatorBase
    {
        public delegate void OrderStrategyUpatedHandler(object sender, OrderStrategyEventArgs e);
        public static event OrderStrategyUpatedHandler OnOrderStrategyUpated;

        private static MainWindow mainWin;
        public static readonly Thread UIThread;
        public static MainWindow MainWin { get => mainWin; }
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

        private static void UpdateOrderStrategy(string orderID, string strategy)
        {
            if (OnOrderStrategyUpated == null) return;
            OrderStrategyEventArgs args = new OrderStrategyEventArgs(orderID, strategy);
            OnOrderStrategyUpated(null, args);
        }

        public OrderManager()
        {
            //System.Diagnostics.Debug.WriteLine("Current Thread: " + Thread.CurrentThread.ManagedThreadId);
        }

        private static Dictionary<string, DateTime> lastBarDateTime = new Dictionary<string, DateTime>();
        [ABMethod]
        public void IBController(string scriptName)
        {
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

            SymbolInAction symbol = OrderManager.Initialize(scriptName);
            
            Script script = symbol.Scripts.FirstOrDefault(x => x.Name == scriptName);
            if (script != null)
            {
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

                    if (strategy.ActionType == ActionType.Long || strategy.ActionType == ActionType.LongAndShort)
                    {
                        bool buySignal = ATFloat.IsTrue(strategy.BuySignal.GetArray()[BarCount - 1]);
                        if (buySignal)
                        {
                            MainViewModel.Instance.Log(new Log
                            {
                                Time = logTime,
                                Text = "Buy signal generated",
                                Source = script.Symbol.Name + "." + strategy.Name
                            });

                            string message = string.Empty;
                            foreach (var acc in strategy.LongAccounts)
                            {
                                if (ValidateSignal(strategy, strategy.AccountStat[acc.Name], OrderAction.Buy, out message))
                                {
                                    foreach (var account in strategy.LongAccounts)
                                    {
                                        string vendor = account.Controller.Vendor;
                                        BaseOrderType orderType = strategy.BuyOrderTypes.FirstOrDefault(x => x.GetType().Name == vendor + "OrderType");
                                        string contract = script.Symbol.SymbolDefinition.FirstOrDefault(x => x.Vendor == vendor).ContractId;
                                        if (orderType != null)
                                        {
                                            bool result = orderType.PlaceOrder(account, contract).Result;
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
                                    MainViewModel.Instance.Log(new Log
                                    {
                                        Time = logTime,
                                        Text = "Order sent",
                                        Source = script.Symbol.Name + "." + strategy.Name
                                    });
                                }
                                else
                                {
                                    MainViewModel.Instance.Log(new Log
                                    {
                                        Time = logTime,
                                        Text = message,
                                        Source = script.Symbol.Name + "." + strategy.Name
                                    });
                                }
                            }
                            
                        }                        
                    }
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
                    break;
                case OrderAction.Cover:
                    if ((strategyStat.AccoutStatus & AccountStatus.CoverPending) != 0)
                    {
                        message = "There is an pending cover order for strategy - " + strategy.Name;
                        return false;
                    }
                    break;
            }

            if (action == OrderAction.Buy || action == OrderAction.Short)
            {
                if (strategyStat.LongEntry + strategyStat.ShortEntry == strategy.MaxEntriesPerDay)
                    message = "Max. entries per day reached(strategy).\n";
                if (strategy.MaxOpenPosition == strategyStat.LongPosition + strategyStat.ShortPosition)
                    message = "Max. open position(strategy) reached.\n";
                if (script.MaxOpenPosition == scriptStat.LongPosition + scriptStat.ShortPosition)
                    message = "Max. open position(script) reached.\n";
                if (script.MaxEntriesPerDay == scriptStat.LongEntry + scriptStat.ShortEntry)
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
                afl.Name = "BuyPrices";
                string[] buyPrices = afl.GetString().Split(new char[] { '$' });
                afl.Name = "SellPrices";
                string[] sellPrices = afl.GetString().Split(new char[] { '$' });
                afl.Name = "ShortPrices";
                string[] shortPrices = afl.GetString().Split(new char[] { '$' });
                afl.Name = "CoverPrices";
                string[] coverPrices = afl.GetString().Split(new char[] { '$' });
                afl.Name = "ActionType";
                string[] actionTypes = afl.GetString().Split(new char[] { '$' });
                for (int i = 0; i < strategyNames.Length; i++)
                {
                    Strategy s = new Strategy(strategyNames[i], script);
                    ActionType at = (ActionType)Enum.Parse(typeof(ActionType), actionTypes[i]);
                    s.ActionType = at;
                    if (at == ActionType.Long || at == ActionType.LongAndShort)
                    {
                        s.BuySignal = new ATAfl(buySignals[i]);
                        s.BuyPrice = new ATAfl(buyPrices[i]);
                        s.SellSignal = new ATAfl(sellSignals[i]);
                        s.SellPrice = new ATAfl(sellPrices[i]);
                    }
                    if (at == ActionType.Short || at == ActionType.LongAndShort)
                    {
                        s.ShortSignal = new ATAfl(shortSignals[i]);
                        s.ShortPrice = new ATAfl(shortPrices[i]);
                        s.CoverSignal = new ATAfl(coverSignals[i]);
                        s.CoverPrice = new ATAfl(coverPrices[i]);
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
        public ATArray BS2(string scriptName)
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
