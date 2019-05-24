﻿using System;
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
        public bool BuySignal { get; set; }
        public bool SellSignal { get; set; }
        public bool ShortSignal { get; set; }
        public bool CoverSignal { get; set; }
    }
    public class OrderLog
    {
        public int RealOrderId { get; set; }  // order id
        public string OrderId { get; set; }    // controller name + order id
        public int? Slippage { get; set; }
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
        public static int BatchNo { get { return batch_no++;  } }

        private static Dictionary<string, DateTime> lastBarDateTime = new Dictionary<string, DateTime>();
        // key: ticker name + strategy name + interval
        private static Dictionary<string, BarInfo> lastBarInfo = new Dictionary<string, BarInfo>();
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
                    script.LastBarTime = logTime;
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

                        if (!lastBarInfo.ContainsKey(symbolName + strategy.Name + AFTimeFrame.Interval()))
                            lastBarInfo.Add(symbolName + strategy.Name + AFTimeFrame.Interval(), new BarInfo() { BarCount = BarCount });

                        // fillin prices from AB
                        strategy.CurrentPrices.Clear();
                        foreach (var p in strategy.PricesATAfl)
                        {
                            strategy.CurrentPrices.Add(p.Key, (decimal)p.Value.GetArray()[BarCount - 1]);
                        }
                        ATAfl a = new ATAfl();
                        
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
                        if (strategy.ActionType == ActionType.Long || strategy.ActionType == ActionType.LongAndShort)
                        {
                            signal = ATFloat.IsTrue(strategy.BuySignal.GetArray()[BarCount - 1]);
                            if (signal && lastBarInfo[symbolName + strategy.Name + AFTimeFrame.Interval()].BuySignal != signal)
                            {
                                Task.Run(() => ProcessSignal(script, strategy, OrderAction.Buy, logTime));                                
                            }
                            lastBarInfo[symbolName + strategy.Name + AFTimeFrame.Interval()].BuySignal = signal;


                            signal = string.IsNullOrEmpty(strategy.SellSignal.Name) ? false : ATFloat.IsTrue(strategy.SellSignal.GetArray()[BarCount - 1]);
                            if (signal && lastBarInfo[symbolName + strategy.Name + AFTimeFrame.Interval()].SellSignal != signal)
                            {
                                Task.Run(() => ProcessSignal(script, strategy, OrderAction.Sell, logTime));
                            }
                            lastBarInfo[symbolName + strategy.Name + AFTimeFrame.Interval()].SellSignal = signal;
                        }
                        if (strategy.ActionType == ActionType.Short || strategy.ActionType == ActionType.LongAndShort)
                        {
                            signal = ATFloat.IsTrue(strategy.ShortSignal.GetArray()[BarCount - 1]);
                            if (signal && lastBarInfo[symbolName + strategy.Name + AFTimeFrame.Interval()].ShortSignal != signal)
                            {
                                Task.Run(() => ProcessSignal(script, strategy, OrderAction.Short, logTime));
                            }
                            lastBarInfo[symbolName + strategy.Name + AFTimeFrame.Interval()].ShortSignal = signal;

                            signal = string.IsNullOrEmpty(strategy.CoverSignal.Name) ? false : ATFloat.IsTrue(strategy.CoverSignal.GetArray()[BarCount - 1]);
                            if (signal && lastBarInfo[symbolName + strategy.Name + AFTimeFrame.Interval()].CoverSignal != signal)
                            {
                                Task.Run(() => ProcessSignal(script, strategy, OrderAction.Cover, logTime));
                            }
                            lastBarInfo[symbolName + strategy.Name + AFTimeFrame.Interval()].CoverSignal = signal;
                        }
                        // store BarCount
                        lastBarInfo[symbolName + strategy.Name + AFTimeFrame.Interval()].BarCount = BarCount;

                        float close = Close[BarCount - 1];
                        //
                        // checking if Adaptive Profit Stop apply
                        //
                        if (strategy.IsAPSAppliedforLong)
                            Task.Run(() => strategy.AdaptiveProfitStopforLong.Calc(close));
                        if (strategy.IsAPSAppliedforShort)
                            Task.Run(() => strategy.AdaptiveProfitStopforShort.Calc(close));

                        //
                        // checking if day end exit applied
                        //
                        if (strategy.IsForcedExitForLong)
                            Task.Run(() => strategy.ForceExitOrderForLong.Run(close));
                        if (strategy.IsForcedExitForShort)
                            Task.Run(() => strategy.ForceExitOrderForShort.Run(close));
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
                int batchNo = BatchNo;                
                foreach (var account in strategy.AccountsDic[orderAction])
                {
                    // get order type
                    string vendor = account.Controller.Vendor;
                    if (orderType == null)
                        orderType = strategy.OrderTypesDic[orderAction].FirstOrDefault(x => x.GetType().BaseType.Name == vendor + "OrderType");

                    if (ValidateSignal(strategy, strategy.AccountStat[account.Name], orderAction, orderType, out message, out warning))
                    {
                        // log after validation
                        mainVM.Log(log);
                                                    
                        if (orderType != null)
                        {
                            BaseStat strategyStat = strategy.AccountStat[account.Name];
                            BaseStat scriptStat = script.AccountStat[account.Name];
                            AccountStatusOp.SetActionStatus(ref strategyStat, ref scriptStat, strategy.Name, orderAction);
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
                                            AccountStatusOp.RevertActionStatus(ref strategyStat, ref scriptStat, strategy.Name, orderAction);
                                            MainViewModel.Instance.Log(new Log
                                            {
                                                Time = DateTime.Now,
                                                Text = "Error: " + orderLog.Error,
                                                Source = script.Symbol.Name + "." + script.Name + "." + strategy.Name
                                                    + (orderLog.Slippage != null ? "." + orderLog.Slippage : "")
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
                        if (!message.Contains("duplicated"))
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

        private static Task<bool> CancelConflictOrder(Strategy strategy, BaseStat strategyStat, OrderAction action)
        {
            
            OrderInfo oi = strategyStat.OrderInfos[action].Last();
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
                return null;
            }                
            else
            {
                IController controller = oi.Account.Controller;
                Task<bool> task = controller.CancelOrderAsync(oi.RealOrderId);
                return task;
            }
        }

       
        private static List<string> fields = new List<string>() { "LmtPrice", "AuxPrice", "TrailingPercent", "TrailStopPrice" };
        private static int IsEqualOrderType(BaseOrderType ot1, BaseOrderType ot2, List<string> compared_fields = null)
        {
            //get TypeAccessor
            TypeAccessor accessor1 = BaseOrderTypeAccessor.GetAccessor(ot1);
            TypeAccessor accessor2 = BaseOrderTypeAccessor.GetAccessor(ot2);

            MemberSet members1 = accessor1.GetMembers();
            MemberSet members2 = accessor2.GetMembers();
            if (compared_fields == null) compared_fields = fields;
            for (int i = 0; i < compared_fields.Count; i++)
            {
                Member mem1 = members1.First(m => m.Name == compared_fields[i]);
                Member mem2 = members2.First(m => m.Name == compared_fields[i]);
                if (mem1 == null) return -100;
                if (mem2 == null) return -200;
                if ((string)accessor1[ot1, compared_fields[i]] != (string)accessor2[ot2, compared_fields[i]])
                    return -1;
            }

            return 0;
        }
        private static bool ValidateSignal(Strategy strategy, BaseStat strategyStat, OrderAction action, BaseOrderType orderType, out string message, out string warning)
        {
            try
            {
                message = string.Empty;
                warning = string.Empty;
                Script script = strategy.Script;
                BaseStat scriptStat = script.AccountStat[strategyStat.Account.Name];
                System.Diagnostics.Debug.WriteLine(DateTime.Now.ToLongTimeString() + ": validating - " + strategyStat.AccountStatus);

                switch (action)
                {
                    case OrderAction.APSLong:
                        if (strategy.IsAPSAppliedforLong)
                        {
                            if ((strategyStat.AccountStatus & AccountStatus.Long) == 0)
                            {
                                message = "There is no a LONG position for strategy - " + strategy.Name;
                                return false;
                            }
                            if ((strategyStat.AccountStatus & AccountStatus.SellPending) != 0)
                            {
                                message = "There is a pending SELL position for strategy - " + strategy.Name;
                                return false;
                            }
                            // cancel previous APSLong order if LmtPrice and Stop Price are different
                            if ((strategyStat.AccountStatus & AccountStatus.APSLongActivated) != 0)
                            {
                                BaseOrderType ot = strategyStat.OrderInfos[action].Last()?.OrderType;
                                if (ot != null && IsEqualOrderType(ot, orderType, new List<string>() { "LmtPrice", "AuxPrice" }) != 0)
                                {
                                    message = "There is a duplicated APSLong order for strategy - " + strategy.Name;
                                    return false;
                                } 
                                else
                                {
                                    // cancel the previou APSLong order, and replace with new one
                                    CancelConflictOrder(strategy, strategyStat, action);
                                }                                
                            }                                

                            // cancel stoploss long order
                            if ((strategyStat.AccountStatus & AccountStatus.StoplossLongActivated) != 0)
                            {
                                CancelConflictOrder(strategy, strategyStat, OrderAction.StoplossLong);
                                warning = "There is a pending stoploss order being cancelled";
                            }
                                
                        }
                        else
                        {
                            message = "APS Long is not enabled for strategy - " + strategy.Name;
                            return false;
                        }
                        break;
                    case OrderAction.APSShort:
                        if (strategy.IsAPSAppliedforShort)
                        {
                            if ((strategyStat.AccountStatus & AccountStatus.Short) == 0)
                            {
                                message = "There is no a Short position for strategy - " + strategy.Name;
                                return false;
                            }
                            if ((strategyStat.AccountStatus & AccountStatus.CoverPending) != 0)
                            {
                                message = "There is a pending Cover position for strategy - " + strategy.Name;
                                return false;
                            }
                            // cancel previous APSLong order if LmtPrice and Stop Price are different
                            if ((strategyStat.AccountStatus & AccountStatus.APSShortActivated) != 0)
                            {
                                BaseOrderType ot = strategyStat.OrderInfos[action].Last()?.OrderType;
                                if (ot != null && IsEqualOrderType(ot, orderType, new List<string>() { "LmtPrice", "AuxPrice" }) != 0)
                                {
                                    message = "There is a duplicated APSShort order for strategy - " + strategy.Name;
                                    return false;
                                }
                                else
                                {
                                    // cancel the previou APSLong order, and replace with new one
                                    CancelConflictOrder(strategy, strategyStat, action);
                                }
                            }

                            // cancel stoploss long order
                            if ((strategyStat.AccountStatus & AccountStatus.StoplossShortActivated) != 0)
                            {
                                CancelConflictOrder(strategy, strategyStat, OrderAction.StoplossShort);
                                warning = "There is a pending stoploss short order being cancelled";
                            }

                        }
                        else
                        {
                            message = "APS Short is not enabled for strategy - " + strategy.Name;
                            return false;
                        }
                        break;
                    case OrderAction.StoplossLong:
                        if ((strategyStat.AccountStatus & AccountStatus.Long) == 0)
                        {
                            message = "There is no LONG position for strategy - " + strategy.Name;
                            return false;
                        }
                        // should not be APSLongActivated, it shuould be executed already
                        if ((strategyStat.AccountStatus & AccountStatus.APSLongActivated) != 0)
                        {
                            warning = "There is a pending APSLong order being cancelled";
                            CancelConflictOrder(strategy, strategyStat, OrderAction.APSLong);
                        }
                        if ((strategyStat.AccountStatus & AccountStatus.SellPending) == 0)
                        {
                            message = "There is a pending sell order for strategy - " + strategy.Name;
                            return false;
                        }
                        // cancel previous StoplossLong order if LmtPrice and Stop Price are different
                        if ((strategyStat.AccountStatus & AccountStatus.StoplossLongActivated) != 0)
                        {
                            BaseOrderType ot = strategyStat.OrderInfos[action].Last()?.OrderType;
                            if (ot != null && IsEqualOrderType(ot, orderType, new List<string>() { "LmtPrice", "AuxPrice"}) != 0)
                            {
                                message = "There is a duplicated StoplossLong order for strategy - " + strategy.Name;
                                return false;
                            }
                            else
                            {
                                // cancel the previou APSLong order, and replace with new one
                                CancelConflictOrder(strategy, strategyStat, action);
                            }
                        }
                        break;
                    case OrderAction.StoplossShort:
                        if ((strategyStat.AccountStatus & AccountStatus.Short) == 0)
                        {
                            message = "There is no SHORT position for strategy - " + strategy.Name;
                            return false;
                        }
                        // should not be APSShortActivated, it shuould be executed already
                        if ((strategyStat.AccountStatus & AccountStatus.APSShortActivated) != 0)
                        {
                            warning = "There is a pending APSShort order being cancelled";
                            CancelConflictOrder(strategy, strategyStat, OrderAction.APSShort);
                        }
                        if ((strategyStat.AccountStatus & AccountStatus.CoverPending) == 0)
                        {
                            message = "There is a pending cover order for strategy - " + strategy.Name;
                            return false;
                        }
                        // cancel previous APSShort order if LmtPrice and Stop Price are different
                        if ((strategyStat.AccountStatus & AccountStatus.StoplossShortActivated) != 0)
                        {
                            BaseOrderType ot = strategyStat.OrderInfos[action].Last()?.OrderType;
                            if (ot != null && IsEqualOrderType(ot, orderType, new List<string>() { "LmtPrice", "AuxPrice" }) != 0)
                            {
                                message = "There is a duplicated StoplossShort order for strategy - " + strategy.Name;
                                return false;
                            }
                            else
                            {
                                // cancel the previou APSLong order, and replace with new one
                                CancelConflictOrder(strategy, strategyStat, action);
                            }
                        }
                        break;
                    case OrderAction.Buy:
                        if (!script.AllowMultiLong)
                        {                            
                            // checking for pending StopLimit Order
                            for (int i = 0; i < script.Strategies.Count; i++)
                            {
                                Strategy s = script.Strategies[i];
                                if (s.AccountStat.ContainsKey(strategyStat.Account.Name))
                                {
                                    BaseStat baseStat = s.AccountStat[strategyStat.Account.Name];
                                    if ((baseStat.AccountStatus & AccountStatus.BuyPending) != 0)
                                    {
                                        BaseOrderType ot = strategyStat.OrderInfos[action].Last()?.OrderType;
                                        if (ot != null && IsEqualOrderType(ot, orderType, new List<string>() { "AuxPrice" }) != 0)
                                        {
                                            warning = "There is a pending BUY order being cancelled for strategy - " + strategy.Name;
                                            // cancel the previou buy order, and replace with new one
                                            CancelConflictOrder(strategy, strategyStat, action);
                                        }
                                    }
                                }                                                                                          
                            }
                        }
                        if ((strategyStat.AccountStatus & AccountStatus.Long) != 0)
                        {
                            message = "There is already a LONG position for strategy - " + strategy.Name;
                            return false;
                        }
                        if ((strategyStat.AccountStatus & AccountStatus.BuyPending) != 0)
                        {
                            message = "There is an pending BUY order for strategy - " + strategy.Name;
                            return false;
                        }
                        if ((strategyStat.AccountStatus & AccountStatus.ShortPending) != 0)
                        {
                            warning = "There is an pending SHORT order for strategy - " + strategy.Name;
                        }
                        break;
                    case OrderAction.Short:
                        if (!script.AllowMultiShort)
                        {
                            // checking for pending StopLimit Order
                            for (int i = 0; i < script.Strategies.Count; i++)
                            {
                                Strategy s = script.Strategies[i];
                                if (s.AccountStat.ContainsKey(strategyStat.Account.Name))
                                {
                                    BaseStat baseStat = s.AccountStat[strategyStat.Account.Name];
                                    if ((baseStat.AccountStatus & AccountStatus.ShortPending) != 0)
                                    {
                                        BaseOrderType ot = strategyStat.OrderInfos[action].Last()?.OrderType;
                                        if (ot != null && IsEqualOrderType(ot, orderType, new List<string>() { "AuxPrice" }) != 0)
                                        {
                                            warning = "There is a pending SHORT order being cancelled for strategy - " + strategy.Name;
                                            // cancel the previou buy order, and replace with new one
                                            CancelConflictOrder(strategy, strategyStat, action);
                                        }
                                    }
                                }
                            }
                        }
                        if ((strategyStat.AccountStatus & AccountStatus.Short) != 0)
                        {
                            message = "There is already a SHORT position for strategy - " + strategy.Name;
                            return false;
                        }
                        if ((strategyStat.AccountStatus & AccountStatus.ShortPending) != 0)
                        {
                            message = "There is an pending SHORT order for strategy - " + strategy.Name;
                            return false;
                        }
                        if ((strategyStat.AccountStatus & AccountStatus.BuyPending) != 0)
                        {
                            warning = "There is an pending BUY order for strategy - " + strategy.Name;
                        }
                        break;
                    case OrderAction.Sell:
                        // APS order has to be cancelled
                        if ((strategyStat.AccountStatus & AccountStatus.APSLongActivated) != 0)
                        {
                            warning = "There is a pending APSLong order being cancelled";
                            CancelConflictOrder(strategy, strategyStat, OrderAction.APSLong);
                        }
                        // Stoploss order has to be cancelled
                        if ((strategyStat.AccountStatus & AccountStatus.StoplossLongActivated) != 0)
                        {
                            warning = "There is a pending StoplossLong order being cancelled";
                            CancelConflictOrder(strategy, strategyStat, OrderAction.StoplossLong);
                        }

                        if ((strategyStat.AccountStatus & AccountStatus.SellPending) != 0)
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
                        // APS order has to be cancelled
                        if ((strategyStat.AccountStatus & AccountStatus.APSShortActivated) != 0)
                        {
                            warning = "There is a pending APSShort order being cancelled";
                            CancelConflictOrder(strategy, strategyStat, OrderAction.APSShort);
                        }
                        // Stoploss order has to be cancelled
                        if ((strategyStat.AccountStatus & AccountStatus.StoplossShortActivated) != 0)
                        {
                            warning = "There is a pending StoplossShort order being cancelled";
                            CancelConflictOrder(strategy, strategyStat, OrderAction.StoplossShort);
                        }

                        if ((strategyStat.AccountStatus & AccountStatus.CoverPending) != 0)
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
                    case OrderAction.ForceExit:
                        // APS order has to be cancelled
                        if ((strategyStat.AccountStatus & AccountStatus.APSLongActivated) != 0)
                        {
                            warning = "There is a pending APSLong order being cancelled";
                            CancelConflictOrder(strategy, strategyStat, OrderAction.APSLong);
                        }
                        // Stoploss order has to be cancelled
                        if ((strategyStat.AccountStatus & AccountStatus.StoplossLongActivated) != 0)
                        {
                            warning = "There is a pending StoplossLong order being cancelled";
                            CancelConflictOrder(strategy, strategyStat, OrderAction.StoplossLong);
                        }
                        // APS order has to be cancelled
                        if ((strategyStat.AccountStatus & AccountStatus.APSShortActivated) != 0)
                        {
                            warning = "There is a pending APSShort order being cancelled";
                            CancelConflictOrder(strategy, strategyStat, OrderAction.APSShort);
                        }
                        // Stoploss order has to be cancelled
                        if ((strategyStat.AccountStatus & AccountStatus.StoplossShortActivated) != 0)
                        {
                            warning = "There is a pending StoplossShort order being cancelled";
                            CancelConflictOrder(strategy, strategyStat, OrderAction.StoplossShort);
                        }
                        // Cancel pending sell order has to be cancelled
                        if ((strategyStat.AccountStatus & AccountStatus.SellPending) != 0)
                        {
                            warning = "There is a pending SELL order being cancelled";
                            CancelConflictOrder(strategy, strategyStat, OrderAction.Sell);
                        }
                        // Cancel pending cover order has to be cancelled
                        if ((strategyStat.AccountStatus & AccountStatus.CoverPending) != 0)
                        {
                            warning = "There is a pending COVER order being cancelled";
                            CancelConflictOrder(strategy, strategyStat, OrderAction.Cover);
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
                        if (script.MaxLongOpenPosition < scriptLE + scriptLP)
                            message += "Max. LONG open position(script) reached.\n";
                    }

                    if (action == OrderAction.Short)
                    {
                        if (strategy.MaxShortOpenPosition <= ss)
                            message += "Max. SHORT open position(strategy) reached.\n";
                        if (script.MaxShortOpenPosition < scriptSE + scriptSP)
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
                Dispatcher.FromThread(UIThread).Invoke(() =>
                {
                    isAdded = MainViewModel.Instance.AddSymbol(AFInfo.Name(), AFTimeFrame.Interval() / 60, out symbol);
                });

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
