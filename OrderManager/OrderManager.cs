using System;
using System.Windows;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AmiBroker;
using AmiBroker.PlugIn;
using AmiBroker.Utils;
using System.Threading;
using System.Reflection;
using IBApi;
using System.Windows.Threading;
using Newtonsoft.Json;
using Dragablz.Savablz;

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

        [ABMethod]
        public ATArray BS1()
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


        [ABMethod]
        public ATArray BS2(string strategies)
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
