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

namespace AmiBroker.Controllers
{
    public class OrderManager : IndicatorBase
    {
        private static MainWindow mainWin;
        //private static System.Windows.Threading.Dispatcher dispatcher;
        public static readonly Thread UIThread;
        static OrderManager()
        {
            try
            {
                // create a thread  
                Thread newWindowThread = new Thread(new ThreadStart(() =>
                {
                    // create and show the window
                    mainWin = new MainWindow();
                    mainWin.Show();
                    // start the Dispatcher processing  
                    System.Windows.Threading.Dispatcher.Run();
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
    }
}
