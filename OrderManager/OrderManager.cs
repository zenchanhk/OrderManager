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
using System.Windows.Threading;
using AmiBroker.OrderManager;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using Xceed.Wpf.AvalonDock;
using Newtonsoft.Json.Converters;
using Krs.Ats.IBNet;
using FastMember;

namespace AmiBroker.Controllers
{
    public class BarInfo
    {
        public int BarCount { get; set; }
        public DateTime DateTime { get; set; }
        public float Close { get; set; }
        public bool BuySignal { get; set; }
        public bool SellSignal { get; set; }
        public bool ShortSignal { get; set; }
        public bool CoverSignal { get; set; }
        public bool IsPricesEqual { get; set; }
    }
    public class OrderLog
    {
        public int RealOrderId { get; set; }  // order id
        public string OrderId { get; set; }    // controller name + order id
        public int Slippage { get; set; }
        public decimal OrgPrice { get; set; }
        public decimal LmtPrice { get; set; }
        public decimal AuxPrice { get; set; }
        public decimal TrailStopPrice { get; set; }
        public double TrailingPercent { get; set; }
        public int PosSize { get; set; }
        public string Message { get; set; }
        public string Error { get; set; }
        public DateTime OrderSentTime { get; set; }
    }
    public class OrderPosSize
    {
        public int Total { get; set; }
        public int Filled { get; set; }
        public int Remaining { get; set; }
        public int Canceled { get; set; }
    }
    public class BatchPosSize
    {
        // key is OrderId
        private Dictionary<int, OrderPosSize> AllPosSizes = new Dictionary<int, OrderPosSize>();
        public int Total { get; private set; }
        public int Remaining { get; private set; }
        public int Filled { get; private set; }
        public int Canceled { get; private set; }

        public void AddNew(int orderId, int total)
        {
            if (AllPosSizes.ContainsKey(orderId))
            {
                Total += total;
                Remaining += total;
            }
            else
            {
                AllPosSizes.Add(orderId, new OrderPosSize { Total = total, Filled = 0, Remaining = total });
                Total += total;
                Remaining += total;
            }
            MainViewModel.Instance.MinorLog(new Log
            {
                Text = string.Format("total:{0}, Total:{1}, Remaining:{2}", total, Total, Remaining),
                Source = orderId.ToString(),
                Time = DateTime.Now
            });
        }
        public void Add(int orderId, int total, int filled = 0, int remaining = 0, int canceled = 0)
        {
            int prev_re = -1;
            int prev_filled = -1;
            if (AllPosSizes.ContainsKey(orderId))
            {
                prev_re = -AllPosSizes[orderId].Remaining;
                prev_filled = -AllPosSizes[orderId].Filled;
                if (canceled > 0)
                {
                    Remaining -= canceled;
                    Canceled += canceled;
                }
                else
                    Remaining += -AllPosSizes[orderId].Remaining + remaining;

                Filled += -AllPosSizes[orderId].Filled + filled;
                AllPosSizes[orderId].Remaining = remaining;
                AllPosSizes[orderId].Filled = filled;
                AllPosSizes[orderId].Canceled = canceled;
            }
            else
                throw new Exception(string.Format("OrderId {0} cannot be found in AllPosSize", orderId));

            MainViewModel.Instance.MinorLog(new Log
            {
                Text = string.Format("OrderId {4} -- total:{0}, filled:{1}, remaining:{2}, canceled:{3}, Remaining:{5}, Filled:{6}" +
                "prev_re:{7}, prev_filled:{8}", 
                    total, filled, remaining, canceled, orderId, Remaining, Filled, prev_re, prev_filled),
                Source = orderId.ToString(),
                Time = DateTime.Now
            });
        }
    }
    public class OrderManager : IndicatorBase
    {
        public static MainWindow MainWin { get; private set; }
        private static MainViewModel mainVM;
        private static MainWindow mainWin;
        public static readonly Thread UIThread;
        
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

                    TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
                    Application.Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;
                    AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
                    // save layout after exit
                    AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
                    // create and show the window
                    mainVM = MainViewModel.Instance;
                    mainWin = new MainWindow();
                    MainWin = mainWin;

                    if (System.IO.File.Exists("layout.cfg"))
                    {
                        DockingManager dock = OrderManager.MainWin.FindName("dockingManager") as DockingManager;
                        Xceed.Wpf.AvalonDock.Layout.Serialization.XmlLayoutSerializer layoutSerializer = new Xceed.Wpf.AvalonDock.Layout.Serialization.XmlLayoutSerializer(dock);
                        layoutSerializer.Deserialize("layout.cfg");
                    }

                    mainWin.Show();
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
                GlobalExceptionHandler.HandleException("OrderManager", ex, null, "Exception occurred at initialization.");
            }
        }

        private static void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            GlobalExceptionHandler.HandleException(sender, e.Exception, e, null, true);
            
        }

        private static void Current_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            GlobalExceptionHandler.HandleException(sender, e.Exception, e, null, true);

        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs args)
        {
            //Exception ex = new Exception("Uncaptured exception for current domain");
            Exception ex = (Exception)args.ExceptionObject;
            GlobalExceptionHandler.HandleException(sender, ex, args, null, true);

        }

        private static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {

        }
        /*
        public OrderManager()
        {
            //System.Diagnostics.Debug.WriteLine("Current Thread: " + Thread.CurrentThread.ManagedThreadId);
        }*/
        // batch no is used for identifying the orders sent in group
        private static int batch_no = 0;
        public static int BatchNo {
            get
            {
                return batch_no++;
            }
        }

        // store PosSize for each BatchNo + AccountName = key
        public static Dictionary<string, BatchPosSize> BatchPosSize { get; } = new Dictionary<string, BatchPosSize>();

        private static Dictionary<string, object> lockOjbs = new Dictionary<string, object>();

        private static Dictionary<string, DateTime> lastBarDateTime = new Dictionary<string, DateTime>();
        // key: ticker name + strategy name + interval
        private static Dictionary<string, BarInfo> lastBarInfo = new Dictionary<string, BarInfo>();

        public static void AddBatchPosSize(string key, int orderId, int total, int filled=0, int remaining=0, int canceled=0)
        {
            if (BatchPosSize.ContainsKey(key))
            {   
                if (filled == 0 && remaining == 0 && canceled == 0)
                    BatchPosSize[key].AddNew(orderId, total);
                else
                    BatchPosSize[key].Add(orderId, total, filled, remaining, canceled);
            }
            else
            {
                if (total == 0)
                    throw new Exception("Cannot add total zero to BatchPosSize");
                BatchPosSize.Add(key, new BatchPosSize());
                BatchPosSize[key].AddNew(orderId, total);
            }                
        }
        
        [ABMethod]
        public void IBC(string scriptName)
        {
            try
            {
                if (mainWin == null) return;
                if (AFTools.LastValue(AFDate.DateTime()) <= 0)
                {
                    mainVM.MinorLog(new Log
                    {
                        Time = DateTime.Now,
                        Text = "DateTime data error",
                        Source = AFInfo.Name() + "." + scriptName
                    });
                    return;
                }
                DateTime logTime = ATFloat.ABDateTimeToDateTime(AFTools.LastValue(AFDate.DateTime()));
                TimeSpan diff = DateTime.Now.Subtract(logTime);
                if (diff.Days * 60 * 24 + diff.Hours * 60 + diff.Minutes > 5)
                {
                    mainVM.MinorLog(new Log
                    {
                        Time = DateTime.Now,
                        Text = "No current data",
                        Source = AFInfo.Name() + "." + scriptName
                    });
                    // add symbol if non-exist
                    Initialize(scriptName);
                    return;
                }
                                
                string symbolName = AFInfo.Name();

                if (lastBarDateTime.ContainsKey(symbolName))
                {
                    lastBarDateTime[symbolName] = logTime;
                }
                else
                    lastBarDateTime.Add(symbolName, logTime);

                SymbolInAction symbol = Initialize(scriptName);
                if (symbol == null || !symbol.IsEnabled) return;

                Script script = symbol.Scripts.FirstOrDefault(x => x.Name == scriptName);
                if (script != null)
                {                    
                    script.BarsHandled++;

                    if (!script.IsEnabled) return;
                    // reset entries count and positions for new day
                    if (script.DayTradeMode)
                    {
                        bool newDay = ATFloat.IsTrue(script.DayStart.GetArray()[BarCount - 1]);
                        if (newDay)
                        {
                            script.ResetForNewDay();
                            foreach (Strategy strategy in script.Strategies)
                            {
                                strategy.ResetForNewDay();
                            }
                        }
                    }
                    
                    foreach (Strategy strategy in script.Strategies)
                    {
                        if (!strategy.IsEnabled) continue;

                        string key = symbolName + strategy.Name + AFTimeFrame.Interval();

                        if (!lastBarInfo.ContainsKey(key))
                            lastBarInfo.Add(key, new BarInfo() { BarCount = BarCount, DateTime = logTime });

                        if (!lockOjbs.ContainsKey(key))
                            lockOjbs.Add(key, new object());

                        // check pending order
                        float close = Close[BarCount - 1];
                        if (strategy.ActionType == ActionType.Long || strategy.ActionType == ActionType.LongAndShort)
                            Task.Run(() => strategy.CheckLongPendingOrders(close));
                        if (strategy.ActionType == ActionType.Short || strategy.ActionType == ActionType.LongAndShort)
                            Task.Run(() => strategy.CheckShortPendingOrders(close));

                        // fillin prices from AB
                        //strategy.CurrentPrices.Clear();
                        lastBarInfo[key].IsPricesEqual = true;
                        foreach (var p in strategy.PricesATAfl)
                        {
                            decimal cur_p = (decimal)p.Value.GetArray()[BarCount - 1];
                            if (strategy.CurrentPrices.ContainsKey(p.Key))
                            {
                                if (cur_p != strategy.CurrentPrices[p.Key])
                                {
                                    lastBarInfo[key].IsPricesEqual = false;
                                    strategy.CurrentPrices[p.Key] = cur_p;
                                }
                            }
                            else
                            {
                                lastBarInfo[key].IsPricesEqual = false;
                                strategy.CurrentPrices.Add(p.Key, cur_p);
                            }                                                            
                        }
                        
                        // fillin position size from AB
                        strategy.CurrentPosSize.Clear();
                        foreach (var p in strategy.PositionSizeATAfl)
                        {
                            if (p.Value.Type == ATVarType.Array)
                                strategy.CurrentPosSize.Add(p.Key, (decimal)p.Value.GetArray()[BarCount - 1]);
                            else if (p.Value.Type == ATVarType.Float)
                                strategy.CurrentPosSize.Add(p.Key, (decimal)p.Value.GetFloat());
                        }

                        ATAfl tmp = strategy.AdaptiveProfitStopforLong.StoplossAFL;
                        if (tmp.Name != "N/A")
                        {
                            if (tmp.Type == ATVarType.Array)
                                strategy.AdaptiveProfitStopforLong.Stoploss = (int)tmp.GetArray()[BarCount - 1];
                            else if (tmp.Type == ATVarType.Float)
                                strategy.AdaptiveProfitStopforLong.Stoploss = (int)tmp.GetFloat();
                        }

                        tmp = strategy.AdaptiveProfitStopforShort.StoplossAFL;
                        if (tmp.Name != "N/A")
                        {
                            if (tmp.Type == ATVarType.Array)
                                strategy.AdaptiveProfitStopforShort.Stoploss = (int)tmp.GetArray()[BarCount - 1];
                            else if (tmp.Type == ATVarType.Float)
                                strategy.AdaptiveProfitStopforShort.Stoploss = (int)tmp.GetFloat();
                        }
                        //
                        // checking signals
                        //
                        bool signal = false;
                        
                        var buyTask = new Task(() => { });
                        var sellTask = new Task(() => { });
                        var shortTask = new Task(() => { });
                        var coverTask = new Task(() => { });
                        var APSLongTask = new Task(() => { });
                        var APSShortTask = new Task(() => { });
                        var FELongTask = new Task(() => { });
                        var FEShortTask = new Task(() => { });

                        //lock(lockOjbs[key])
                        {
                            if (strategy.ActionType == ActionType.Long || strategy.ActionType == ActionType.LongAndShort)
                            {
                                signal = ATFloat.IsTrue(strategy.BuySignal.GetArray()[BarCount - 1]);
                                if (signal && (lastBarInfo[key].BuySignal != signal || lastBarInfo[key].DateTime != logTime 
                                    || !lastBarInfo[key].IsPricesEqual || strategy.StatusChanged))
                                {
                                    buyTask = Task.Run(() => ProcessSignal(script, strategy, OrderAction.Buy, logTime));
                                }
                                lastBarInfo[key].BuySignal = signal;


                                signal = string.IsNullOrEmpty(strategy.SellSignal.Name) ? false : ATFloat.IsTrue(strategy.SellSignal.GetArray()[BarCount - 1]);
                                if (signal && (lastBarInfo[key].SellSignal != signal || lastBarInfo[key].DateTime != logTime 
                                    || !lastBarInfo[key].IsPricesEqual || strategy.StatusChanged))
                                {
                                    sellTask = Task.Run(() => ProcessSignal(script, strategy, OrderAction.Sell, logTime));
                                }
                                lastBarInfo[key].SellSignal = signal;
                            }
                            if (strategy.ActionType == ActionType.Short || strategy.ActionType == ActionType.LongAndShort)
                            {
                                signal = ATFloat.IsTrue(strategy.ShortSignal.GetArray()[BarCount - 1]);
                                if (signal && (lastBarInfo[key].ShortSignal != signal || lastBarInfo[key].DateTime != logTime 
                                    || !lastBarInfo[key].IsPricesEqual || strategy.StatusChanged))
                                {
                                    shortTask = Task.Run(() => ProcessSignal(script, strategy, OrderAction.Short, logTime));
                                }
                                lastBarInfo[key].ShortSignal = signal;

                                signal = string.IsNullOrEmpty(strategy.CoverSignal.Name) ? false : ATFloat.IsTrue(strategy.CoverSignal.GetArray()[BarCount - 1]);
                                if (signal && (lastBarInfo[key].CoverSignal != signal || lastBarInfo[key].DateTime != logTime 
                                    || !lastBarInfo[key].IsPricesEqual || strategy.StatusChanged))
                                {
                                    coverTask = Task.Run(() => ProcessSignal(script, strategy, OrderAction.Cover, logTime));
                                }
                                lastBarInfo[key].CoverSignal = signal;
                            }

                            // reset StatusChanged
                            strategy.StatusChanged = false;

                            //float close = Close[BarCount - 1];
                            if (close != lastBarInfo[key].Close)
                            {
                                //
                                // checking if Adaptive Profit Stop apply
                                //
                                if (strategy.IsAPSAppliedforLong)
                                    APSLongTask = Task.Run(() => strategy.AdaptiveProfitStopforLong.Calc(close));
                                if (strategy.IsAPSAppliedforShort)
                                    APSShortTask = Task.Run(() => strategy.AdaptiveProfitStopforShort.Calc(close));

                                //
                                // checking if day end exit applied
                                //
                                if (strategy.IsForcedExitForLong)
                                    FELongTask = Task.Run(() => strategy.ForceExitOrderForLong.Run(close));
                                if (strategy.IsForcedExitForShort)
                                    FEShortTask = Task.Run(() => strategy.ForceExitOrderForShort.Run(close));
                            }

                            // store last bar info
                            lastBarInfo[key].BarCount = BarCount;
                            lastBarInfo[key].DateTime = logTime;
                            lastBarInfo[key].Close = close;

                            //Task.WaitAll(new Task[] { buyTask, sellTask, shortTask, coverTask, APSLongTask, APSShortTask, FELongTask, FEShortTask });
                        }
                        // END of LOCK
                    }
                }
            }
            catch (Exception ex)
            {
                //throw ex;
                GlobalExceptionHandler.HandleException("OrderManger.IBC", ex);
            }            
        }

        public static bool ProcessSignal(Script script, Strategy strategy, OrderAction orderAction, DateTime logTime, BaseOrderType orderType = null)
        {
            // to identify if PlaceOrder successful or not for APS orders use
            // to reduce the process times for duplicated orders
            bool proc_result = true;
            try
            {                
                Log log = new Log
                {
                    Time = DateTime.Now,
                    Text = orderAction.ToString() + " signal generated at " + logTime.ToString("yyyMMdd HH:mm:ss"),
                    Source = script.Symbol.Name + "." + script.Name + "." + strategy.Name
                };

                if (orderAction != OrderAction.APSLong && orderAction != OrderAction.APSShort 
                    && orderAction != OrderAction.StoplossLong && orderAction != OrderAction.StoplossShort
                    && strategy.AccountsDic[orderAction].Count == 0)
                {
                    log.Text += "\nBut there is no account assigned.";
                    mainVM.MinorLog(log);
                    return false;
                }

                string message = string.Empty;
                string warning = string.Empty;

                // batch no used for optimization (no need to transform order for different account)
                int batchNo = -1;                
                foreach (var account in strategy.AccountsDic[orderAction])
                {
                    // get order type
                    string vendor = account.Controller.Vendor;
                    if (orderType == null)
                        orderType = strategy.OrderTypesDic[orderAction].FirstOrDefault(x => x.GetType().BaseType.Name == vendor + "OrderType");

                    if (ValidateSignal(account, strategy, strategy.AccountStat[account.Name], orderAction, orderType, out message, out warning))
                    {
                        if (batchNo == -1)
                            batchNo = BatchNo;
                        // log after validation
                        mainVM.Log(log);
                                                    
                        if (orderType != null)
                        {
                            BaseStat strategyStat = strategy.AccountStat[account.Name];
                            BaseStat scriptStat = script.AccountStat[account.Name];
                            AccountStatusOp.SetActionInitStatus(ref strategyStat, ref scriptStat, ref strategy, orderAction);
                            AccountStatusOp.SetAttemps(ref strategyStat, orderAction);
                            System.Diagnostics.Debug.WriteLine(DateTime.Now.ToLongTimeString() + ": setting - " + strategyStat.AccountStatus);
                            // IMPORTANT
                            // should be improved here, same type controller share Order Info and should be waiting here
                            // same accounts should be grouped together instead of using for-loop
                            // TODO list
                            //List<OrderLog> orderLogs = account.Controller.PlaceOrder(account, strategy, orderType, orderAction, batchNo).Result;
                            account.Controller.PlaceOrder(account, strategy, orderType, orderAction, batchNo).ContinueWith(
                                result =>
                                {
                                    List<OrderLog> orderLogs = result.Result;
                                    //strategyStat.OrderInfos[orderAction].Clear();   // clear old order info
                                    foreach (OrderLog orderLog in orderLogs)
                                    {
                                        if (orderLog.OrderId != "-1")
                                        {                                            
                                            //Dispatcher.FromThread(UIThread).Invoke(() =>
                                            //{                                        
                                            strategyStat.OrderInfos[orderAction].Add(MainViewModel.Instance.OrderInfoList[orderLog.OrderId]);
                                            //});
                                            // log order place details
                                            MainViewModel.Instance.Log(new Log
                                            {
                                                Time = orderLog.OrderSentTime,
                                                Text = orderAction.ToString() + " order sent (OrderId:" + orderLog.OrderId.ToString()
                                                + (orderLog.OrgPrice > 0 ? ", OrgPrice:" + orderLog.OrgPrice.ToString() : "")
                                                + (orderLog.LmtPrice > 0 ? ", LmtPrice:" + orderLog.LmtPrice.ToString() : "") + ")",
                                                Source = script.Symbol.Name + "." + script.Name + "." + strategy.Name + "." + orderLog.Slippage
                                            });
                                        }
                                        else
                                        {
                                            strategyStat = strategy.AccountStat[account.Name];
                                            AccountStatusOp.RevertActionStatus(ref strategyStat, ref scriptStat, ref strategy, orderAction, batchNo);
                                            MainViewModel.Instance.Log(new Log
                                            {
                                                Time = DateTime.Now,
                                                Text = "Plaing Order Error: " + orderLog.Error,
                                                Source = script.Symbol.Name + "." + script.Name + "." + strategy.Name
                                                    + "." + orderLog.Slippage
                                            });
                                            // only return false only if PlaceOrder fails
                                            proc_result = false;
                                        }
                                    }
                                }
                            );
                        }
                        else
                        {
                            MainViewModel.Instance.Log(new Log
                            {
                                Time = DateTime.Now,
                                Text = vendor + "OrderType not found.",
                                Source = script.Symbol.Name + "." + script.Name + "." + strategy.Name
                            });
                        }
                        
                    }
                    else
                    {
                        MainViewModel.Instance.MinorLog(log);
                        MainViewModel.Instance.MinorLog(new Log
                        {
                            Time = DateTime.Now,
                            Text = message.TrimEnd('\n'),
                            Source = script.Symbol.Name + "." + script.Name + "." + strategy.Name
                        });
                        // if not duplicated order, will return false
                        if (!message.Contains("duplicated") && !message.ToLower().Contains("modify"))
                            proc_result = false;
                    }
                }
            }
            catch (Exception ex)
            {
                GlobalExceptionHandler.HandleException("OrderManger.ProcessSignal", ex);
                proc_result = false;
            }
            return proc_result;
        }

        private static bool CancelConflictOrder(Strategy strategy, BaseStat strategyStat, OrderAction action)
        {
            try
            {
                OrderInfo oi = strategyStat.OrderInfos[action].LastOrDefault();
                if (oi == null)
                {
                    string msg = "There is a pending " + action.ToString() + " order for strategy - " +
                                strategy.Name + ", but no order info found" + "\n";
                    mainVM.Log(new Log
                    {
                        Time = DateTime.Now,
                        Text = msg,
                        Source = "OrderManager.CancelConflictOrder"
                    });
                    return false;
                }
                else
                {
                    IController controller = oi.Account.Controller;
                    List<OrderInfo> orderInfos = strategyStat.OrderInfos[action].Where(x => x.BatchNo == oi.BatchNo).ToList();
                    bool result = true;
                    foreach (var orderInfo in orderInfos)
                    {
                        controller.CancelOrder(orderInfo.RealOrderId);
                    }
                    string msg = "There are pending " + action.ToString() + " orders [" + string.Join(", ", orderInfos.Select(x => x.RealOrderId).ToList())
                        + "] for strategy - " + strategy.Name + " being cancelled" + "\n";
                    mainVM.Log(new Log
                    {
                        Time = DateTime.Now,
                        Text = msg + " (OrderManager.CancelConflictOrder)",
                        Source = strategy.Symbol.Name + "." + strategy.Script.Name + "." + strategy.Name
                    });
                    return result;
                }
            }
            catch (Exception ex)
            {
                GlobalExceptionHandler.HandleException("OrderManger.ProcessSignal", ex);
                return false;
            }
            
        }

        
        /*
         * Caveat: SELL or COVER cannot be stop order since it will force to exit APS and Stoploss Orders
         * TODO: 
         * In case of Sell or Cover Order are stop orders, Sell/Cover orders and APSLong/Short order should
         * compare STOP price to detemine which one is kept
         * */
        private static readonly string[] stpLmt = new string[] { "LmtPrice", "AuxPrice" };
        private static bool ValidateSignal(AccountInfo account, Strategy strategy, BaseStat strategyStat, OrderAction action, BaseOrderType orderType, out string message, out string warning)
        {
            try
            {
                message = string.Empty;
                warning = string.Empty;
                Script script = strategy.Script;
                BaseStat scriptStat = script.AccountStat[strategyStat.Account.Name];
                IController controller = account.Controller;
                //System.Diagnostics.Debug.WriteLine(DateTime.Now.ToLongTimeString() + ": validating - " + strategyStat.AccountStatus);

                switch (action)
                {
                    case OrderAction.APSLong:
                        if (strategy.IsAPSAppliedforLong)
                        {
                            //if ((strategyStat.AccountStatus & AccountStatus.Long) == 0)
                            if (strategyStat.LongPosition == 0)
                            {
                                message += "There is no a LONG position for strategy - " + strategy.Name;
                                return false;
                            }
                            // partially filled
                            if (strategyStat.LongPosition > 0 && (strategyStat.AccountStatus & AccountStatus.BuyPending) != 0)
                            {
                                warning += "There is a partially filled BUY order being cancelled";
                                CancelConflictOrder(strategy, strategyStat, OrderAction.Buy);
                            }
                            // pending sell order: compare limit price to decide which one is kept
                            if ((strategyStat.AccountStatus & AccountStatus.SellPending) != 0)
                            {
                                OrderInfo oi = strategyStat.OrderInfos[OrderAction.Sell].LastOrDefault();
                                decimal sellPrice = oi.OrderLog.OrgPrice;
                                string lmtP = BaseOrderTypeAccessor.GetValueByName(orderType, "LmtPrice");
                                decimal apsPrice = BaseOrderTypeAccessor.GetPriceByName(strategy, lmtP);
                                if (sellPrice > apsPrice)
                                {
                                    message += string.Format("There is a pending SELL with higher Limit Price: {0}", sellPrice);
                                    return false;
                                }
                                else
                                {
                                    warning += string.Format("There is a pending SELL with lower Limit Price: {0} being cancelled\n", sellPrice);
                                    CancelConflictOrder(strategy, strategyStat, OrderAction.Sell);
                                }
                            }
                            // cancel previous APSLong order if LmtPrice and Stop Price are different
                            if ((strategyStat.AccountStatus & AccountStatus.APSLongActivated) != 0)
                            {
                                BaseOrderType ot = strategyStat.OrderInfos[action].LastOrDefault()?.OrderType;
                                List<string> list0 = BaseOrderTypeAccessor.GetValueByName(orderType, stpLmt);
                                List<decimal> prices0 = BaseOrderTypeAccessor.GetPriceByName(strategy, list0);
                                if (ot != null && prices0[0] == ot.RealPrices[stpLmt[0]] && prices0[1] == ot.RealPrices[stpLmt[1]])
                                {
                                    message += "There is a duplicated APSLong order for strategy - " + strategy.Name;
                                    return false;
                                } 
                                else
                                {
                                    // modify the APSLong order
                                    message += "Modifying APSLong order for strategy - " + strategy.Name;
                                    controller.ModifyOrder(account, strategy, action, orderType);
                                    return false;
                                }                                
                            }                                

                            // cancel stoploss long order
                            if ((strategyStat.AccountStatus & AccountStatus.StoplossLongActivated) != 0)
                            {
                                CancelConflictOrder(strategy, strategyStat, OrderAction.StoplossLong);
                                warning += "There is a pending stoploss LONG order being cancelled";
                            }
                                
                        }
                        else
                        {
                            message += "APS Long is not enabled for strategy - " + strategy.Name;
                            return false;
                        }
                        break;
                    case OrderAction.APSShort:
                        if (strategy.IsAPSAppliedforShort)
                        {
                            if (strategyStat.ShortPosition == 0)
                            {
                                message += "There is no a Short position for strategy - " + strategy.Name;
                                return false;
                            }
                            // partially filled
                            if (strategyStat.ShortPosition > 0 && (strategyStat.AccountStatus & AccountStatus.ShortPending) != 0)
                            {
                                warning += "There is a partially filled SHORT order being cancelled";
                                CancelConflictOrder(strategy, strategyStat, OrderAction.Short);
                            }
                            // pending cover order: compare limit price to decide which one is kept
                            if ((strategyStat.AccountStatus & AccountStatus.CoverPending) != 0)
                            {
                                OrderInfo oi = strategyStat.OrderInfos[OrderAction.Cover].LastOrDefault();
                                decimal coverPrice = oi.OrderLog.OrgPrice;
                                string lmtP = BaseOrderTypeAccessor.GetValueByName(orderType, "LmtPrice");
                                decimal apsPrice = BaseOrderTypeAccessor.GetPriceByName(strategy, lmtP);
                                if (coverPrice < apsPrice)
                                {
                                    message += string.Format("There is a pending Cover with lower Limit Price: {0}", coverPrice);
                                    return false;
                                }
                                else
                                {
                                    warning += string.Format("There is a pending Cover with higher Limit Price: {0} being cancelled\n", coverPrice);
                                    CancelConflictOrder(strategy, strategyStat, OrderAction.Cover);
                                }
                            }
                            // cancel previous APSLong order if LmtPrice and Stop Price are different
                            if ((strategyStat.AccountStatus & AccountStatus.APSShortActivated) != 0)
                            {
                                BaseOrderType ot = strategyStat.OrderInfos[action].LastOrDefault()?.OrderType;
                                List<string> list0 = BaseOrderTypeAccessor.GetValueByName(orderType, stpLmt);
                                List<decimal> prices0 = BaseOrderTypeAccessor.GetPriceByName(strategy, list0);
                                if (ot != null && prices0[0] == ot.RealPrices[stpLmt[0]] && prices0[1] == ot.RealPrices[stpLmt[1]])
                                {
                                    message += "There is a duplicated APSShort order for strategy - " + strategy.Name;
                                    return false;
                                }
                                else
                                {
                                    // cancel the previou APSLong order, and replace with new one
                                    message += "Modifying APSShort order for strategy - " + strategy.Name;
                                    controller.ModifyOrder(account, strategy, action, orderType);
                                    return false;
                                }
                            }

                            // cancel stoploss long order
                            if ((strategyStat.AccountStatus & AccountStatus.StoplossShortActivated) != 0)
                            {
                                CancelConflictOrder(strategy, strategyStat, OrderAction.StoplossShort);
                                warning += "There is a pending stoploss short order being cancelled";
                            }

                        }
                        else
                        {
                            message += "APS Short is not enabled for strategy - " + strategy.Name;
                            return false;
                        }
                        break;
                    case OrderAction.StoplossLong:
                        if (strategyStat.LongPosition == 0)
                        {
                            message += "There is no LONG position for strategy - " + strategy.Name;
                            return false;
                        }
                        // partially filled
                        if (strategyStat.LongPosition > 0 && (strategyStat.AccountStatus & AccountStatus.BuyPending) != 0)
                        {
                            warning += "There is a partially filled BUY order being cancelled";
                            CancelConflictOrder(strategy, strategyStat, OrderAction.Buy);
                        }
                        // pending sell order: compare limit price to decide which one is kept
                        if ((strategyStat.AccountStatus & AccountStatus.SellPending) != 0)
                        {
                            OrderInfo oi = strategyStat.OrderInfos[OrderAction.Sell].LastOrDefault();
                            decimal sellPrice = oi.OrderLog.OrgPrice;
                            string lmtP = BaseOrderTypeAccessor.GetValueByName(orderType, "LmtPrice");
                            decimal apsPrice = BaseOrderTypeAccessor.GetPriceByName(strategy, lmtP);
                            if (sellPrice > apsPrice)
                            {
                                message += string.Format("There is a pending SELL with higher Limit Price: {0}", sellPrice);
                                return false;
                            }
                            else
                            {
                                warning += string.Format("There is a pending SELL with lower Limit Price: {0} being cancelled\n", sellPrice);
                                CancelConflictOrder(strategy, strategyStat, OrderAction.Sell);
                            }
                        }
                        // should not be APSLongActivated, it shuould be executed already
                        if ((strategyStat.AccountStatus & AccountStatus.APSLongActivated) != 0)
                        {
                            warning += "There is a pending APSLong order being cancelled";
                            CancelConflictOrder(strategy, strategyStat, OrderAction.APSLong);
                        }
                        // cancel previous StoplossLong order if LmtPrice and Stop Price are different
                        if ((strategyStat.AccountStatus & AccountStatus.StoplossLongActivated) != 0)
                        {
                            BaseOrderType ot = strategyStat.OrderInfos[action].LastOrDefault()?.OrderType;
                            List<string> list0 = BaseOrderTypeAccessor.GetValueByName(orderType, stpLmt);
                            List<decimal> prices0 = BaseOrderTypeAccessor.GetPriceByName(strategy, list0);
                            if (ot != null && prices0[0] == ot.RealPrices[stpLmt[0]] && prices0[1] == ot.RealPrices[stpLmt[1]])
                            {
                                message += "There is a duplicated StoplossLong order for strategy - " + strategy.Name;
                                return false;
                            }
                            else
                            {
                                // cancel the previou APSLong order, and replace with new one
                                message += "Modifying StoplossLong order for strategy - " + strategy.Name;
                                controller.ModifyOrder(account, strategy, action, orderType);
                                return false;
                            }
                        }
                        break;
                    case OrderAction.StoplossShort:
                        if (strategyStat.ShortPosition == 0)
                        {
                            message += "There is no a Short position for strategy - " + strategy.Name;
                            return false;
                        }
                        // partially filled
                        if (strategyStat.ShortPosition > 0 && (strategyStat.AccountStatus & AccountStatus.ShortPending) != 0)
                        {
                            warning += "There is a partially filled SHORT order being cancelled";
                            CancelConflictOrder(strategy, strategyStat, OrderAction.Short);
                        }
                        // pending cover order: compare limit price to decide which one is kept
                        if ((strategyStat.AccountStatus & AccountStatus.CoverPending) != 0)
                        {
                            OrderInfo oi = strategyStat.OrderInfos[OrderAction.Cover].LastOrDefault();
                            decimal coverPrice = oi.OrderLog.OrgPrice;
                            string lmtP = BaseOrderTypeAccessor.GetValueByName(orderType, "LmtPrice");
                            decimal apsPrice = BaseOrderTypeAccessor.GetPriceByName(strategy, lmtP);
                            if (coverPrice < apsPrice)
                            {
                                message += string.Format("There is a pending Cover with lower Limit Price: {0}", coverPrice);
                                return false;
                            }
                            else
                            {
                                warning += string.Format("There is a pending Cover with higher Limit Price: {0} being cancelled\n", coverPrice);
                                CancelConflictOrder(strategy, strategyStat, OrderAction.Cover);
                            }
                        }
                        // should not be APSShortActivated, it shuould be executed already
                        if ((strategyStat.AccountStatus & AccountStatus.APSShortActivated) != 0)
                        {
                            warning += "There is a pending APSShort order being cancelled";
                            CancelConflictOrder(strategy, strategyStat, OrderAction.APSShort);
                        }
                        // cancel previous APSShort order if LmtPrice and Stop Price are different
                        if ((strategyStat.AccountStatus & AccountStatus.StoplossShortActivated) != 0)
                        {
                            BaseOrderType ot = strategyStat.OrderInfos[action].LastOrDefault()?.OrderType;
                            List<string> list0 = BaseOrderTypeAccessor.GetValueByName(orderType, stpLmt);
                            List<decimal> prices0 = BaseOrderTypeAccessor.GetPriceByName(strategy, list0);
                            if (ot != null && prices0[0] == ot.RealPrices[stpLmt[0]] && prices0[1] == ot.RealPrices[stpLmt[1]])
                            {
                                message += "There is a duplicated StoplossShort order for strategy - " + strategy.Name;
                                return false;
                            }
                            else
                            {
                                // cancel the previou APSLong order, and replace with new one
                                message += "Modifying Stoploss Short order";
                                controller.ModifyOrder(account, strategy, action, orderType);
                                return false;
                            }
                        }
                        break;
                    case OrderAction.Buy:
                        
                        if (strategyStat.LongPosition > 0)
                        {
                            message += "There is already a LONG position for strategy - " + strategy.Name;
                            return false;
                        }

                        if ((strategyStat.AccountStatus & AccountStatus.BuyPending) != 0 && strategyStat.LongPosition == 0)
                        {
                            if (!orderType.ReplaceAllowed)
                            {
                                message += "There is a pending Long order for strategy - " + strategy.Name;
                                return false;
                            }
                            else
                            {
                                BaseOrderType ot = strategyStat.OrderInfos[action].LastOrDefault()?.OrderType;
                                if (ot != null)
                                {
                                    if (orderType.Name == ot.Name)
                                    {
                                        List<string> list0 = BaseOrderTypeAccessor.GetValueByName(orderType, stpLmt);
                                        List<decimal> prices0 = BaseOrderTypeAccessor.GetPriceByName(strategy, list0);
                                        bool isEqual = (ot.RealPrices.ContainsKey(stpLmt[0]) ? prices0[0] == ot.RealPrices[stpLmt[0]] : true) &&
                                            (ot.RealPrices.ContainsKey(stpLmt[1]) ? prices0[1] == ot.RealPrices[stpLmt[1]] : true);
                                        if (!isEqual)
                                        {
                                            message += "Modifying Long STOP Order";
                                            controller.ModifyOrder(account, strategy, action, orderType);
                                            return false;
                                        }
                                        else
                                        {
                                            message += "There is a duplicated pending LONG order for strategy - " + strategy.Name;
                                            return false;
                                        }
                                    }
                                    else
                                    {
                                        warning += "There is a pending LONG order being cancelled for strategy - " + strategy.Name;
                                        CancelConflictOrder(strategy, strategyStat, action);
                                    }
                                }
                                else
                                {
                                    message += "There is a pending LONG order but related OrderInfo cannot be found - for strategy - " + strategy.Name;
                                    return false;
                                }
                            }

                        }

                        if ((strategyStat.AccountStatus & AccountStatus.ShortPending) != 0)
                        {
                            warning += "There is an pending SHORT order for strategy - " + strategy.Name;
                        }

                        // check other strategies with pending buy order in the same script
                        if (scriptStat.LongPendingStrategies.Count > 0 &&
                            (!script.AllowMultiLong ||
                            (script.AllowMultiLong &&
                            script.MaxLongOpenPosition <= scriptStat.LongStrategies.Count + scriptStat.LongPendingStrategies.Count)))
                        {
                            if (script.OrderReplaceAllowed)
                            {
                                // replace the one with lowest execution priority
                                var result = from name in scriptStat.LongPendingStrategies
                                             join s1 in script.Strategies on name equals s1.Name
                                             where s1.AccountStat.ContainsKey(strategyStat.Account.Name) && s1.Priority <= strategy.Priority
                                             orderby s1.Priority                                             
                                             select s1;
                                
                                Strategy s = result?.FirstOrDefault(x => x.AccountStat[strategyStat.Account.Name].LongPosition == 0 &&
                                (x.AccountStat[strategyStat.Account.Name].AccountStatus & AccountStatus.BuyPending) != 0);
                                if (s != null)
                                {
                                    warning += "There is a pending Long order being cancelled in another strategy - " + s.Name;
                                    CancelConflictOrder(s, strategyStat, action);
                                }
                                else
                                {
                                    message += "Cannot found pending order with lower execution priority";
                                    return false;
                                }
                            }
                            else
                            {
                                message += "There is a pending LONG order in other strategy - [" +
                                    string.Join(", ", scriptStat.LongPendingStrategies.ToList()) + "]";
                                return false;
                            }                           
                            
                        }
                        break;
                    case OrderAction.Short:                        
                        if (strategyStat.ShortPosition > 0)
                        {
                            message += "There is already a SHORT position for strategy - " + strategy.Name;
                            return false;
                        }
                       
                        if ((strategyStat.AccountStatus & AccountStatus.ShortPending) != 0 && strategyStat.ShortPosition == 0)
                        {
                            if (!orderType.ReplaceAllowed)
                            {
                                message += "There is a pending SHORT order for strategy - " + strategy.Name;
                                return false;
                            }
                            else
                            {
                                BaseOrderType ot = strategyStat.OrderInfos[action].LastOrDefault()?.OrderType;
                                if (ot != null)
                                {                                     
                                    if (orderType.Name == ot.Name)
                                    {
                                        List<string> list0 = BaseOrderTypeAccessor.GetValueByName(orderType, stpLmt);
                                        List<decimal> prices0 = BaseOrderTypeAccessor.GetPriceByName(strategy, list0);
                                        //bool isEqual = prices0[0] == ot.RealPrices[stpLmt[0]] && prices0[1] == ot.RealPrices[stpLmt[1]];
                                        bool isEqual = (ot.RealPrices.ContainsKey(stpLmt[0]) ? prices0[0] == ot.RealPrices[stpLmt[0]] : true) &&
                                            (ot.RealPrices.ContainsKey(stpLmt[1]) ? prices0[1] == ot.RealPrices[stpLmt[1]] : true);
                                        if (!isEqual)
                                        {
                                            message += "Modifying Short STOP Order";
                                            controller.ModifyOrder(account, strategy, action, orderType);
                                            return false;
                                        }
                                        else
                                        {
                                            message += "There is a duplicated pending SHORT order for strategy - " + strategy.Name;
                                            return false;
                                        }
                                    }
                                    else
                                    {
                                        warning += "There is a pending SHORT order being cancelled for strategy - " + strategy.Name;
                                        CancelConflictOrder(strategy, strategyStat, action);
                                    }
                                }
                                else
                                {
                                    message += "There is a pending SHORT order but related OrderInfo cannot be found - for strategy - " + strategy.Name;
                                    return false;
                                }
                            }
                            
                        }
                        if ((strategyStat.AccountStatus & AccountStatus.BuyPending) != 0)
                        {
                            warning += "There is an pending BUY order for strategy - " + strategy.Name;
                        }

                        // check other strategies with pending buy order in the same script
                        if (scriptStat.ShortPendingStrategies.Count > 0 &&
                            (!script.AllowMultiShort ||
                            (script.AllowMultiShort &&
                            script.MaxShortOpenPosition <= scriptStat.ShortStrategies.Count + scriptStat.ShortPendingStrategies.Count)))
                        {
                            if (script.OrderReplaceAllowed)
                            {
                                // replace the one with lowest execution priority
                                var result = from name in scriptStat.LongPendingStrategies
                                             join s1 in script.Strategies on name equals s1.Name
                                             where s1.AccountStat.ContainsKey(strategyStat.Account.Name) && s1.Priority <= strategy.Priority
                                             orderby s1.Priority
                                             select s1;

                                Strategy s = result?.FirstOrDefault(x => x.AccountStat[strategyStat.Account.Name].ShortPosition == 0 &&
                                (x.AccountStat[strategyStat.Account.Name].AccountStatus & AccountStatus.ShortPending) != 0);
                                if (s != null)
                                {
                                    warning += "There is a pending Short order being cancelled in another strategy - " + s.Name;
                                    CancelConflictOrder(s, strategyStat, action);
                                }
                                else
                                {
                                    message += "Cannot found pending order with lower execution priority";
                                    return false;
                                }
                            }
                            else
                            {
                                message += "There is a pending SHORT order in other strategy - [" +
                                    string.Join(", ", scriptStat.ShortPendingStrategies.ToList()) + "]";
                                return false;
                            }

                        }
                        break;
                    case OrderAction.Sell:
                        // APS order has to be cancelled
                        if ((strategyStat.AccountStatus & AccountStatus.APSLongActivated) != 0)
                        {
                            warning += "There is a pending APSLong order being cancelled";
                            CancelConflictOrder(strategy, strategyStat, OrderAction.APSLong);
                        }
                        // Stoploss order has to be cancelled
                        if ((strategyStat.AccountStatus & AccountStatus.StoplossLongActivated) != 0)
                        {
                            warning += "There is a pending StoplossLong order being cancelled";
                            CancelConflictOrder(strategy, strategyStat, OrderAction.StoplossLong);
                        }
                        //
                        // modify STOP order if LmtPrice and Stop Price are different
                        // skip for LIMIT order
                        if ((strategyStat.AccountStatus & AccountStatus.SellPending) != 0)
                        {
                            BaseOrderType ot = strategyStat.OrderInfos[action].LastOrDefault()?.OrderType;
                            if (ot != null)
                            {
                                List<string> list0 = BaseOrderTypeAccessor.GetValueByName(orderType, stpLmt);
                                List<decimal> prices0 = BaseOrderTypeAccessor.GetPriceByName(strategy, list0);
                                bool isEqual = (ot.RealPrices.ContainsKey(stpLmt[0]) ? prices0[0] == ot.RealPrices[stpLmt[0]] : true) &&
                                            (ot.RealPrices.ContainsKey(stpLmt[1]) ? prices0[1] == ot.RealPrices[stpLmt[1]] : true);
                                if (isEqual)
                                {
                                    message += "There is a duplicated pending SELL order for strategy - " + strategy.Name;
                                    return false;
                                }
                                else
                                {
                                    // if Order is a LMT order, then ignore
                                    if (ot.RealPrices[stpLmt[1]] == 0)
                                    {
                                        message += "There is a pending SELL order for strategy - " + strategy.Name;
                                        return false;
                                    }
                                    else // if order is a STOP order, then modify
                                    {
                                        message += "Modifying Sell Order";
                                        controller.ModifyOrder(account, strategy, action, orderType);
                                        return false;
                                    }
                                }
                            }
                            else
                            {
                                message += "There is a pending SELL order but related OrderInfo cannot be found - for strategy - " + strategy.Name;
                                return false;
                            }
                        }
                        if (strategyStat.LongPosition == 0)
                        {
                            message += "There is no long position for strategy - " + strategy.Name;
                            return false;
                        }
                        break;
                    case OrderAction.Cover:
                        // APS order has to be cancelled
                        if ((strategyStat.AccountStatus & AccountStatus.APSShortActivated) != 0)
                        {
                            warning += "There is a pending APSShort order being cancelled";
                            CancelConflictOrder(strategy, strategyStat, OrderAction.APSShort);
                        }
                        // Stoploss order has to be cancelled
                        if ((strategyStat.AccountStatus & AccountStatus.StoplossShortActivated) != 0)
                        {
                            warning += "There is a pending StoplossShort order being cancelled";
                            CancelConflictOrder(strategy, strategyStat, OrderAction.StoplossShort);
                        }
                        //
                        // modify STOP order if LmtPrice and Stop Price are different
                        // skip for LIMIT order
                        if ((strategyStat.AccountStatus & AccountStatus.CoverPending) != 0)
                        {
                            BaseOrderType ot = strategyStat.OrderInfos[action].LastOrDefault()?.OrderType;
                            if (ot != null)
                            {
                                List<string> list0 = BaseOrderTypeAccessor.GetValueByName(orderType, stpLmt);
                                List<decimal> prices0 = BaseOrderTypeAccessor.GetPriceByName(strategy, list0);
                                bool isEqual = (ot.RealPrices.ContainsKey(stpLmt[0]) ? prices0[0] == ot.RealPrices[stpLmt[0]] : true) &&
                                            (ot.RealPrices.ContainsKey(stpLmt[1]) ? prices0[1] == ot.RealPrices[stpLmt[1]] : true);
                                if (isEqual)
                                {
                                    message += "There is a duplicated pending COVER order for strategy - " + strategy.Name;
                                    return false;
                                }
                                else
                                {
                                    // if Order is a LMT order, then ignore
                                    if (ot.RealPrices[stpLmt[1]] == 0)
                                    {
                                        message += "There is a pending COVER LMT order for strategy - " + strategy.Name;
                                        return false;
                                    }
                                    else // if order is a STOP order, then modify
                                    {
                                        message += "Modifying Cover Order";
                                        controller.ModifyOrder(account, strategy, action, orderType);
                                        return false;
                                    }
                                }
                            }
                            else
                            {
                                message += "There is a pending COVER order but related OrderInfo cannot be found - for strategy - " + strategy.Name;
                                return false;
                            }
                        }
                        if (strategyStat.ShortPosition == 0)
                        {
                            message += "There is no short position for strategy - " + strategy.Name;
                            return false;
                        }
                        break;
                    case OrderAction.PreForceExitLong:
                    case OrderAction.FinalForceExitLong:
                        if (strategyStat.LongPosition == 0)
                        {
                            message += "Info[Force Exit Long]: there is no open LONG position for strategy - " + strategy.Name;
                            return false;
                        }
                        // APS order has to be cancelled
                        if ((strategyStat.AccountStatus & AccountStatus.APSLongActivated) != 0)
                        {
                            warning += "There is a pending APSLong order being cancelled.\n";
                            CancelConflictOrder(strategy, strategyStat, OrderAction.APSLong);
                        }
                        // Stoploss order has to be cancelled
                        if ((strategyStat.AccountStatus & AccountStatus.StoplossLongActivated) != 0)
                        {
                            warning += "There is a pending StoplossLong order being cancelled.\n";
                            CancelConflictOrder(strategy, strategyStat, OrderAction.StoplossLong);
                        }
                        // Cancel pending sell order has to be cancelled
                        if ((strategyStat.AccountStatus & AccountStatus.SellPending) != 0)
                        {
                            warning += "There is a pending SELL order being cancelled.\n";
                            CancelConflictOrder(strategy, strategyStat, OrderAction.Sell);
                        }
                        // in case of FinalForceExitLong
                        if (action == OrderAction.FinalForceExitLong)
                        {
                            // Cancel pending PreForceExitLong order has to be cancelled
                            if ((strategyStat.AccountStatus & AccountStatus.PreForceExitLongActivated) != 0)
                            {
                                warning += "There is a pending PreForceExitLong order being cancelled.\n";
                                CancelConflictOrder(strategy, strategyStat, OrderAction.PreForceExitLong);
                            }
                        }
                        break;
                    case OrderAction.PreForceExitShort:
                    case OrderAction.FinalForceExitShort:
                        if (strategyStat.ShortPosition == 0)
                        {
                            message += "Info[Force Exit Short]: there is no open SHORT position for strategy - " + strategy.Name;
                            return false;
                        }
                        // APS order has to be cancelled
                        if ((strategyStat.AccountStatus & AccountStatus.APSShortActivated) != 0)
                        {
                            warning += "There is a pending APSShort order being cancelled.\n";
                            CancelConflictOrder(strategy, strategyStat, OrderAction.APSShort);
                        }
                        // Stoploss order has to be cancelled
                        if ((strategyStat.AccountStatus & AccountStatus.StoplossShortActivated) != 0)
                        {
                            warning += "There is a pending StoplossShort order being cancelled.\n";
                            CancelConflictOrder(strategy, strategyStat, OrderAction.StoplossShort);
                        }                        
                        // Cancel pending cover order has to be cancelled
                        if ((strategyStat.AccountStatus & AccountStatus.CoverPending) != 0)
                        {
                            warning += "There is a pending COVER order being cancelled.\n";
                            CancelConflictOrder(strategy, strategyStat, OrderAction.Cover);
                        }
                        // in case of FinalForceExitShort
                        if (action == OrderAction.FinalForceExitShort)
                        {
                            // Cancel pending PreForceExitShort order has to be cancelled
                            if ((strategyStat.AccountStatus & AccountStatus.PreForceExitShortActivated) != 0)
                            {
                                warning += "There is a pending PreForceExitShort order being cancelled.\n";
                                CancelConflictOrder(strategy, strategyStat, OrderAction.PreForceExitShort);
                            }
                        }
                        break;
                }

                // Max. Entries/Attemps/Open Positions validation
                int scriptLE = scriptStat.LongStrategies.Count;
                int scriptSE = scriptStat.ShortStrategies.Count;
                int scriptLP = scriptStat.LongPendingStrategies.Count;
                int scriptSP = scriptStat.ShortPendingStrategies.Count;

                if (action == OrderAction.Buy || action == OrderAction.Short)
                {                       
                    if (script.DayTradeMode)
                    {
                        int scriptALE = scriptStat.LongEntry.GroupBy(x => x.BatchNo).Count();
                        int scriptASE = scriptStat.ShortEntry.GroupBy(x => x.BatchNo).Count();
                        int strategyALE = strategyStat.LongEntry.GroupBy(x => x.BatchNo).Count();
                        int strategyASE = strategyStat.ShortEntry.GroupBy(x => x.BatchNo).Count();

                        if (strategyALE + strategyASE >= strategy.MaxEntriesPerDay)
                            message += "Max. entries per day reached(strategy).\n";
                        if (script.MaxEntriesPerDay <= scriptALE + scriptASE)
                            message += "Max. entries per day(script) reached.\n";
                    }

                    // Assume there is at most one long position and one short position
                    int sl = 0; // count of strategy long open positions
                    int ss = 0; // count of strategy short open positions
                    if ((strategyStat.AccountStatus & AccountStatus.BuyPending) != 0) sl++;
                    if ((strategyStat.AccountStatus & AccountStatus.Long) != 0) sl++;
                    if ((strategyStat.AccountStatus & AccountStatus.ShortPending) != 0) ss++;
                    if ((strategyStat.AccountStatus & AccountStatus.Short) != 0) ss++;

                    if (strategy.MaxOpenPosition <= sl + ss)
                        message += "Max. open position(strategy) reached.\n";

                    if (script.MaxOpenPosition <= scriptLE + scriptSE + scriptLP + scriptSP)
                        message += "Max. open position(including pending orders, script level) reached.\n";

                    if (action == OrderAction.Buy)
                    {
                        if (strategy.MaxLongOpenPosition <= sl)
                            message += "Max. LONG open position(strategy) reached.\n";
                        if (script.MaxLongOpenPosition <= scriptLE + scriptLP)
                            message += "Max. LONG open position(script) reached.\n";
                    }

                    if (action == OrderAction.Short)
                    {
                        if (strategy.MaxShortOpenPosition <= ss)
                            message += "Max. SHORT open position(strategy) reached.\n";
                        if (script.MaxShortOpenPosition <= scriptSE + scriptSP)
                            message += "Max. SHORT open position(script) reached.\n";
                    }
                }
                
                // Multi long/short validating
                if (action == OrderAction.Buy)
                {
                    if (script.AllowMultiLong && script.MaxLongOpen <= scriptLE + scriptLP - 1)
                        message += "Max. LONG open position(script) reached.\n";
                    if (!script.AllowMultiLong && scriptLE + scriptLP >= 1)
                        message += "Multiple LONG open position(script) is not allowed.\n";
                    if (script.DayTradeMode && strategyStat.LongAttemps >= strategy.MaxLongAttemps)
                        message += "Max. LONG attemps(strategy) reached.\n";
                }

                if (action == OrderAction.Sell)
                {
                    if (script.AllowMultiShort && script.MaxShortOpen <= scriptSE + scriptSP - 1)
                        message += "Max. SHORT open position(script) reached.\n";
                    if (!script.AllowMultiShort && scriptSE + scriptSP >= 1)
                        message += "Multiple SHORT open position(script) is not allowed.\n";
                    if (script.DayTradeMode && strategyStat.ShortAttemps >= strategy.MaxShortAttemps)
                        message += "Max. SHORT attemps(strategy) reached.\n";
                }    
                
                if (message == string.Empty)
                    return true;

                // Appending Account info
                message = "[" + strategyStat.Account.Name + "]:" + message;
                warning = "[" + strategyStat.Account.Name + "]:" + warning;
                return false;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                warning = string.Empty;
                GlobalExceptionHandler.HandleException("OrderManger.ValidateSignal", ex);
                return false;
            }
        } 

        public static SymbolInAction Initialize(string scriptName)
        {
            try
            {
                bool isAdded = false;
                SymbolInAction symbol = null;
                Dispatcher.FromThread(UIThread).Invoke(new System.Action(() =>
                {
                    isAdded = MainViewModel.Instance.AddSymbol(AFInfo.Name(), AFTimeFrame.Interval() / 60, out symbol);
                }), DispatcherPriority.Background, null);

                if (symbol != null)
                {
                    Script script = symbol.Scripts.FirstOrDefault(x => x.Name == scriptName);
                    bool strategyNeedRefresh = script != null ? script.Strategies.Any(x => x.IsDirty) : false;
                    bool scriptNeedRefresh = script != null ? script.IsDirty : false;
                    if (script == null || scriptNeedRefresh || strategyNeedRefresh)
                    {
                        // script refreshed or new
                        if (script == null)
                        {
                            script = new Script(scriptName, symbol);
                            Dispatcher.FromThread(UIThread).Invoke(() =>
                            {
                                symbol.Scripts.Add(script);
                            });
                        }
                        else if (scriptNeedRefresh)
                        {
                            Dispatcher.FromThread(UIThread).Invoke(() =>
                            {
                                script.RefreshStrategies();
                                script.IsDirty = false;
                            });
                        }
                        ATAfl afl = new ATAfl();
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
                        afl.Name = "PosSizes";
                        string[] posSizes = afl.GetString("na").Split(new char[] { '$' });
                        afl.Name = "Stoplosses";
                        string[] stoploss = afl.GetString("na").Split(new char[] { '$' });
                        afl.Name = "ActionType";
                        string[] actionTypes = afl.GetString().Split(new char[] { '$' });                        
                        // get day start
                        afl.Name = "DayStart";
                        string dayStart = afl.GetString("na");
                        if (dayStart != "na")
                        {
                            script.DayStart = new ATAfl(dayStart);
                            script.DayTradeMode = true;
                        }                            
                        else
                        {
                            script.DayStart = new ATAfl();
                            mainVM.Log(new Log
                            {
                                Time = DateTime.Now,
                                Text = "DayStart is not available in script - " + scriptName,
                                Source = "Symbol Initialization"
                            });
                            script.DayTradeMode = false;
                        }
                            
                        /*
                         * read GTA and GTD info from script directly
                         * ScheduledOrders="{'buy':{'GTA':{'ExactTime':'21:29'},'GTD':{'ExactTime':'00:59', 'ExactTimeValidDays':1}}}$";
                        afl.Name = "ScheduledOrders";
                        string[] schOrders = new string[] { }; 
                        try
                        {
                            schOrders = afl.GetString().Split(new char[] { '$' });
                        }
                        catch (Exception ex)
                        {
                            // doing nothing if scheduldOrders not defined
                        }*/

                        for (int i = 0; i < strategyNames.Length; i++)
                        {
                            Strategy s = script.Strategies.FirstOrDefault(x => x.Name == strategyNames[i]);
                            
                            if (s == null || (s != null && s.IsDirty))
                            {
                                if (s == null)
                                    s = new Strategy(strategyNames[i], script);
                                /*
                                if (schOrders[i].Trim().Length > 0)
                                {
                                    Dictionary<string, Dictionary<string, GoodTime>> so = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, GoodTime>>>(schOrders[i],
                                        new IsoDateTimeConverter { DateTimeFormat = "HH:mm" });
                                    s.ScheduledOrders = so;
                                }*/
                                // DayTrade Mode
                                s.DayTradeMode = script.DayTradeMode;

                                ActionType at = (ActionType)Enum.Parse(typeof(ActionType), actionTypes[i]);
                                s.ActionType = at;     
                                if (at == ActionType.Long || at == ActionType.LongAndShort)
                                {
                                    s.BuySignal = new ATAfl(buySignals[i]);
                                    s.SellSignal = !string.IsNullOrEmpty(sellSignals[0]) ? new ATAfl(sellSignals[i]) : new ATAfl();
                                }
                                if (at == ActionType.Short || at == ActionType.LongAndShort)
                                {
                                    s.ShortSignal = new ATAfl(shortSignals[i]);
                                    s.CoverSignal = !string.IsNullOrEmpty(coverSignals[0]) ? new ATAfl(coverSignals[i]) : new ATAfl();
                                }

                                // initialize prices
                                s.Prices = new List<string>(prices[i].Split(new char[] { '%' }));                                
                                foreach (var p in s.Prices)
                                {
                                    // in case of refreshing strategy parameters
                                    if (!string.IsNullOrEmpty(p) && !s.PricesATAfl.ContainsKey(p))
                                        s.PricesATAfl.Add(p, new ATAfl(p));
                                }

                                // initialize position size
                                s.PositionSize = posSizes[0] != "na" ? new List<string>(posSizes[i].Split(new char[] { '%' })) : null;                                
                                if (s.PositionSize != null)
                                {
                                    foreach (var p in s.PositionSize)
                                    {
                                        // in case of refreshing strategy parameters
                                        if (!string.IsNullOrEmpty(p) && !s.PositionSizeATAfl.ContainsKey(p))
                                            s.PositionSizeATAfl.Add(p, new ATAfl(p));
                                    }
                                }

                                // initial stoploss
                                if (stoploss[0] != "na")
                                {
                                    string sl = stoploss[i];
                                    if (!string.IsNullOrEmpty(sl) && at == ActionType.Long)
                                        s.AdaptiveProfitStopforLong.StoplossAFL = new ATAfl(sl);
                                    else if (!string.IsNullOrEmpty(sl) && at == ActionType.Short)
                                        s.AdaptiveProfitStopforShort.StoplossAFL = new ATAfl(sl);
                                    else if (!string.IsNullOrEmpty(sl) && at == ActionType.LongAndShort)
                                    {
                                        string[] sls = sl.Split(new char[] { '%' });
                                        if (!string.IsNullOrEmpty(sls[0]))
                                            s.AdaptiveProfitStopforLong.StoplossAFL = new ATAfl(sls[0]);
                                        if (!string.IsNullOrEmpty(sls[1]))
                                            s.AdaptiveProfitStopforShort.StoplossAFL = new ATAfl(sls[1]);
                                    }
                                }

                                if (!s.IsDirty)
                                    Dispatcher.FromThread(UIThread).Invoke(() =>
                                    {
                                        script.Strategies.Add(s);
                                    });
                                else
                                    Dispatcher.FromThread(UIThread).Invoke(() =>
                                    {
                                        s.IsDirty = false;
                                    });
                            }
                        }
                    }
                }
                return symbol;
            }
            catch (Exception ex)
            {
                GlobalExceptionHandler.HandleException("OrderManger.Initialize", ex);
                return null;
            }
        }
        
        [ABMethod]
        public void Test()
        {
            //System.Diagnostics.Debug.WriteLine(AFInfo.Name());
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
