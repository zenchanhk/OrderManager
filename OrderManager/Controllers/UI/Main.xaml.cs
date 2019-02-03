using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using ControlLib;
using IBApi;
using Xceed.Wpf.AvalonDock;
using Xceed.Wpf.AvalonDock.Controls;
using Xceed.Wpf.AvalonDock.Layout;
using Xceed.Wpf.AvalonDock.Themes;

namespace AmiBroker.Controllers
{
    public class Message
    {
        public DateTime Time { get; set; }
        public int Code { get; set; }
        public string Text { get; set; }
        public string Source { get; set; }
    }
    public class Log
    {
        public DateTime Time { get; set; }
        public string Text { get; set; }
        public string Source { get; set; }
        
    }
    public class SymbolInMkt : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public string Symbol { get; set; }  
        public string Currency { get; set; }
        public string Account { get; set; }
        public string Source { get; set; }
        public Contract Contract { get; set; }
        public string Vendor { get; set; }

        private double _pPosition;     // from PositionEventArgs.Position
        public double Position
        {
            get { return _pPosition; }
            set
            {
                if (_pPosition != value)
                {
                    _pPosition = value;
                    OnPropertyChanged("Position");
                }
            }
        }

        private double _pMktPricee;
        public double MktPrice
        {
            get { return _pMktPricee; }
            set
            {
                if (_pMktPricee != value)
                {
                    _pMktPricee = value;
                    OnPropertyChanged("MktPricee");
                }
            }
        }

        private double _pMktValue;
        public double MktValue
        {
            get { return _pMktValue; }
            set
            {
                if (_pMktValue != value)
                {
                    _pMktValue = value;
                    OnPropertyChanged("MktValue");
                }
            }
        }

        private double _pAvgCost;   
        public double AvgCost
        {
            get { return _pAvgCost; }
            set
            {
                if (_pAvgCost != value)
                {
                    _pAvgCost = value;
                    OnPropertyChanged("AvgCost");
                }
            }
        }

        private double _pUnrealizedPNL;
        public double UnrealizedPNL
        {
            get { return _pUnrealizedPNL; }
            set
            {
                if (_pUnrealizedPNL != value)
                {
                    _pUnrealizedPNL = value;
                    OnPropertyChanged("UnrealizedPNL");
                }
            }
        }

        private double _pRealizedPNL;
        public double RealizedPNL
        {
            get { return _pRealizedPNL; }
            set
            {
                if (_pRealizedPNL != value)
                {
                    _pRealizedPNL = value;
                    OnPropertyChanged("RealizedPNL");
                }
            }
        }        
        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }
    }
    public class DisplayedOrder : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public int OrderId { get; set; }
        public Contract Contract { get; set; }
        public string Vendor { get; set; }
        public string Strategy { get; set; }
        public string Action { get; set; }
        public string Type { get; set; }
        public string Account { get; set; }
        public string Exchange { get; set; }
        public string Symbol { get; set; }  // from Contract.Symbol
        public string Currency { get; set; }    // from Contract.Currency
        public double Quantity { get; set; }    // from Order.TotalQuantity
        public double LmtPrice { get; set; }
        public double StopPrice { get; set; }   // from order.TrailStopPrice
        public string Tif { get; set; }
        public string GTD { get; set; }       // from Order.GoodTillDate
        public string GAT { get; set; }
        public int ParentId { get; set; }
        public string OcaGroup { get; set; }
        public int OcaType { get; set; }
        public string Source { get; set; }

        private DateTime _pTime;
        public DateTime Time
        {
            get { return _pTime; }
            set
            {
                if (_pTime != value)
                {
                    _pTime = value;
                    OnPropertyChanged("Time");
                }
            }
        }

        private string _pStatus;    // from OrderStatusEventArgs.Status
        public string Status
        {
            get { return _pStatus; }
            set
            {
                if (_pStatus != value)
                {
                    _pStatus = value;
                    OnPropertyChanged("Status");
                }
            }
        }

        private double _pFilled;   // from OrderStatusEventArgs.Filled
        public double Filled
        {
            get { return _pFilled; }
            set
            {
                if (_pFilled != value)
                {
                    _pFilled = value;
                    OnPropertyChanged("Filled");
                }
            }
        }

        private double _pRemaining; // from OrderStatusEventArgs.Remaining
        public double Remaining
        {
            get { return _pRemaining; }
            set
            {
                if (_pRemaining != value)
                {
                    _pRemaining = value;
                    OnPropertyChanged("Remaining");
                }
            }
        }

        private double _pAvgPrice;   // from OrderStatusEventArgs.AvgFillPrice
        public double AvgPrice
        {
            get { return _pAvgPrice; }
            set
            {
                if (_pAvgPrice != value)
                {
                    _pAvgPrice = value;
                    OnPropertyChanged("AvgPrice");
                }
            }
        }

        private double _pMktPricee;
        public double MktPricee
        {
            get { return _pMktPricee; }
            set
            {
                if (_pMktPricee != value)
                {
                    _pMktPricee = value;
                    OnPropertyChanged("MktPricee");
                }
            }
        }

        public DisplayedOrder ShallowCopy()
        {
            return (DisplayedOrder)this.MemberwiseClone();
        }
        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }
    }    
    /// <summary>
    /// Interaction logic for Main.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        #region Disable close button
        [DllImport("user32.dll")]
        static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);
        [DllImport("user32.dll")]
        static extern bool EnableMenuItem(IntPtr hMenu, uint uIDEnableItem, uint uEnable);

        const uint MF_BYCOMMAND = 0x00000000;
        const uint MF_GRAYED = 0x00000001;
        const uint MF_ENABLED = 0x00000000;

        const uint SC_CLOSE = 0xF060;

        const int WM_SHOWWINDOW = 0x00000018;
        const int WM_CLOSE = 0x10;
        #endregion

        public event PropertyChangedEventHandler PropertyChanged;
        public double ScalingFactor
        {
            get { return (double)GetValue(ScalingFactorProperty); }
            set { SetValue(ScalingFactorProperty, value); }
        }
        public static readonly DependencyProperty ScalingFactorProperty =
            DependencyProperty.Register("ScalingFactor", typeof(double), typeof(MainWindow));

        //private JavaScriptSerializer js = new JavaScriptSerializer();

        public Type Type { get { return this.GetType(); } }   //Used by Xml Data Trigger
        
        private bool _stopByUser = false;   // to determine if reconnect after disconnection        
                
        private bool _pAlwaysOnTop = false;
        public bool AlwaysOnTop
        {
            get { return _pAlwaysOnTop; }
            set
            {
                if (_pAlwaysOnTop != value)
                {
                    _pAlwaysOnTop = value;
                    OnPropertyChanged("AlwaysOnTop");
                }
            }
        }                
        
        public MainViewModel MainVM { get; private set; } = MainViewModel.Instance;

        private static int id = 0;
        public int ID { get; private set; }
        // selected tab
        public ListView ActivateListView { get; private set; }
        private object _pSelectedTab;
        public object SelectedTab
        {
            get { return _pSelectedTab; }
            set
            {
                if (_pSelectedTab != value)
                {
                    _pSelectedTab = value;
                    if (value != null)
                    {
                        ListView lv = UITreeHelper.FindChild<ListView>(((dynamic)value).Content.Content);
                        if (lv != null)
                            ActivateListView = lv;
                        else
                            ActivateListView = null;
                    }                    
                    OnPropertyChanged("SelectedTab");
                }
            }
        }

        private int _pSelectedTheme;
        public int SelectedTheme
        {
            get { return _pSelectedTheme; }
            set
            {
                if (_pSelectedTheme != value)
                {
                    _pSelectedTheme = value;
                    OnPropertyChanged("SelectedTheme");
                }
            }
        }

        public MainWindow()
        {
            //TabsViewModel = new TabsViewModel();
            init();
        }
        private void init()
        {            
            InitializeComponent();
            /* embedded resource
            string[] t = Assembly.GetExecutingAssembly().GetManifestResourceNames();
            string resourceName = "AmiBroker.Controllers.images.order.png";
            Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
            this.Icon = BitmapFrame.Create(s);
            */
            Uri uri = new Uri("pack://application:,,,/OrderManager;component/Controllers/images/order.png");
            BitmapImage bi = new BitmapImage(uri);
            this.Icon = bi;

            ScalingFactor = 1;

            // prevent Alt+F4 from shutting down windows
            this.KeyDown += MainWindow_KeyDown;
            MainVM.MessageList.CollectionChanged += MessageList_CollectionChanged;
        }

        private void Items_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            int i = 0;
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.System && e.SystemKey == Key.F4)
            {
                e.Handled = true;
            }
        }        

        private void MessageList_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            System.Windows.Threading.Dispatcher.FromThread(OrderManager.UIThread).Invoke(() =>
            {
                FlashTabItem("MsgTab");
            });
        }

        public void ReadSettings()
        {
            MainVM.ReadSettings();            
        }
        
        public void Log(string message)
        {
            System.Windows.Threading.Dispatcher.FromThread(OrderManager.UIThread).Invoke(() =>
            {
                MainVM.LogList.Insert(0, new Log()
                {
                    Time = DateTime.Now,
                    Text = message
                });
            });
        }
        private void FlashTabItem(string tabName)
        {
            TabItem ti = (TabItem)this.FindName(tabName);
            if (ti != null && !ti.IsSelected)
            {
                ti.SetValue(Control.StyleProperty, (System.Windows.Style)this.Resources["FlashingHeader"]);
            }
        }

        #region Disable close button
        
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            HwndSource hwndSource = PresentationSource.FromVisual(this) as HwndSource;

            if (hwndSource != null)
            {
                hwndSource.AddHook(new HwndSourceHook(this.hwndSourceHook));
            }
        }

        IntPtr hwndSourceHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_SHOWWINDOW)
            {
                IntPtr hMenu = GetSystemMenu(hwnd, false);
                if (hMenu != IntPtr.Zero)
                {
                    EnableMenuItem(hMenu, SC_CLOSE, MF_BYCOMMAND | MF_GRAYED);
                }
            }
            else if (msg == WM_CLOSE)
            {                
                // handled = true;
            }
            return IntPtr.Zero;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            int id = ((MainWindow)sender).ID;
            if (ID == 0)
                e.Cancel = true;
        }
        #endregion

        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }
        
        protected override void OnPreviewMouseWheel(MouseWheelEventArgs args)
        {
            base.OnPreviewMouseWheel(args);
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                ScalingFactor += ((args.Delta > 0) ? 0.1 : -0.1);
            }
        }

        private void Mi_Connect_Click(object sender, RoutedEventArgs e)
        {
            MenuItem mi = sender as MenuItem;
            IController ctrl = mi.DataContext as IController;
            if (ctrl.IsConnected)
                ctrl.Disconnect();
            else
                ctrl.Connect();
        }

        private void MngTemplateClick(object sender, RoutedEventArgs e)
        {
            SaveLoadTemplate win = new SaveLoadTemplate(TemplateAction.Manage);
            win.ShowDialog();
        }

        private LayoutDocument scriptDocument = null;
        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            Xceed.Wpf.AvalonDock.Layout.LayoutAnchorable d = new Xceed.Wpf.AvalonDock.Layout.LayoutAnchorable();
            // Check the menu item.
            foreach (MenuItem item in ((sender as MenuItem).Parent as MenuItem).Items)
            {
                item.IsChecked = (item == (sender as MenuItem));
                if (item.IsChecked)
                {
                    if (item.Name == "aero")
                        SelectedTheme = 1;
                    else if (item.Name == "metro")
                        SelectedTheme = 2;
                    else if (item.Name == "vs2010")
                        SelectedTheme = 3;
                    else
                        SelectedTheme =0;

                    
                }                
            }
        }

        private void LayoutDocument_IsSelectedChanged(object sender, EventArgs e)
        {
            LayoutDocument doc = sender as LayoutDocument;
            if (doc.IsSelected && doc.ContentId == "script")
            {
                SelectedTab = doc;
                RefreshCachedControl(doc);
            }                
        }

        private void RefreshCachedControl(LayoutDocument document)
        {
            ScrollViewer sv = UITreeHelper.FindChild<ScrollViewer>(((dynamic)document.Content).Content, "contentSV");
            if (sv != null)
            {
                CachedContentControl cachedContent = sv.Content as CachedContentControl;
                if (cachedContent.Content == null)
                {
                    var tmp = MainVM.SelectedItem;
                    MainVM.SelectedItem = null;
                    MainVM.SelectedItem = tmp;
                }                
            }
        }

        private LayoutDocument FindDocument()
        {
            DockingManager manager = this.FindName("dockingManager") as DockingManager;
            if (manager == null) return null;

            foreach (var child in manager.LayoutRootPanel.Children)
            {
                if (child.GetType() == typeof(LayoutDocumentPaneGroupControl))
                {
                    foreach (var docControl in ((LayoutDocumentPaneGroupControl)child).Children)
                    {
                        if (docControl.GetType() == typeof(LayoutDocumentPaneControl))
                        {
                            foreach (var doc in ((LayoutDocumentPaneControl)docControl).Items)
                            {
                                if (((LayoutDocument)doc)?.ContentId == "script")
                                {
                                    scriptDocument = (LayoutDocument)doc;
                                    return (LayoutDocument)doc;
                                }
                            }
                        }
                    }
                }
            }
            return null;
        }

        private Timer timer = new Timer(); // used to update scriptTabView's cachedContentControl
        private void _themeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (scriptDocument == null)
            {
                FindDocument();
            }
            if (scriptDocument != null && scriptDocument.IsSelected)
            {                
                timer.Interval = 50;
                timer.Elapsed += Timer_Elapsed;
                timer.Start();
                
            }
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            System.Windows.Threading.Dispatcher.FromThread(OrderManager.UIThread).Invoke(() =>
            {
                RefreshCachedControl(scriptDocument);
            });
            timer.Stop();
        }

        private void FilterTB_Click(object sender, RoutedEventArgs e)
        {
            TextBox tb = this.FindName("filterText") as TextBox;
            tb.Text = "";
        }

        private void refreshTV_Click(object sender, RoutedEventArgs e)
        {
            ObjectInTreeView oit = this.FindName("oit_SelecteItem") as ObjectInTreeView;
            oit.Refresh();
        }

        private void MI_oi_win_Click(object sender, RoutedEventArgs e)
        {
            if (scriptDocument == null)
                FindDocument();
            if (scriptDocument != null && scriptDocument.IsSelected)
                RefreshCachedControl(scriptDocument);
        }
    }

    #region Ticker
    public class Ticker : INotifyPropertyChanged
    {
        public Ticker()
        {
            Timer timer = new Timer();
            timer.Interval = 1000; // 1 second updates
            timer.Elapsed += timer_Elapsed;
            timer.Start();
        }

        public string Now
        {
            get { return DateTime.Now.ToString("dd HH:mm:ss"); }
        }

        void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs("Now"));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
    #endregion
}
