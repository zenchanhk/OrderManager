using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Newtonsoft.Json;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace AmiBroker.Controllers
{
    public class ConnectionParam
    {
        public string AccName { get; set; }
        public string Host { get; set; }
        public int Port { get; set; }
        public int ClientId { get; set; }
        public bool IsActivate { get; set; }
        public bool IsMulti { get; set; }
        // used to enable/disable list item selection
        public bool ReadOnly { get; set; } = false;
    }
    public class AccountOption : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private bool _pIsExclusive;
        public bool IsExclusive
        {
            get { return _pIsExclusive; }
            set
            {
                if (_pIsExclusive != value)
                {
                    _pIsExclusive = value;
                    OnPropertyChanged("IsExclusive");
                }
            }
        }

        private ObservableCollection<ConnectionParam> _pAccounts = new ObservableCollection<ConnectionParam>();
        public ObservableCollection<ConnectionParam> Accounts
        {
            get { return _pAccounts; }
            set
            {
                if (_pAccounts != value)
                {
                    _pAccounts = value;
                    OnPropertyChanged("Accounts");
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
    // If new vendor is added, Vendors must be added and new property NEWAccounts must be added
    // Must be in UPPER case
    public class UserPreference : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public string[] Vendors { get; private set; } = { "IB", "FT" };
        private AccountOption _pIBAccount = new AccountOption();
        public AccountOption IBAccount
        {
            get { return _pIBAccount; }
            set
            {
                if (_pIBAccount != value)
                {
                    _pIBAccount = value;
                    OnPropertyChanged("IBAccount");
                }
            }
        }

        private AccountOption _pFTAccount = new AccountOption();
        public AccountOption FTAccount
        {
            get { return _pFTAccount; }
            set
            {
                if (_pFTAccount != value)
                {
                    _pFTAccount = value;
                    OnPropertyChanged("FTAccount");
                }
            }
        }

        private string _pErrorFilter;
        public string ErrorFilter
        {
            get { return _pErrorFilter; }
            set
            {
                if (_pErrorFilter != value)
                {
                    _pErrorFilter = value;
                    OnPropertyChanged("ErrorFilter");
                }
            }
        }

        private bool _pKeepTradeSteps;
        public bool KeepTradeSteps
        {
            get { return _pKeepTradeSteps; }
            set
            {
                if (_pKeepTradeSteps != value)
                {
                    _pKeepTradeSteps = value;
                    OnPropertyChanged("KeepTradeSteps");
                }
            }
        }

        private bool _pIgnoreDuplicatedRecord;
        public bool IgnoreDuplicatedRecord
        {
            get { return _pIgnoreDuplicatedRecord; }
            set
            {
                if (_pIgnoreDuplicatedRecord != value)
                {
                    _pIgnoreDuplicatedRecord = value;
                    OnPropertyChanged("IgnoreDuplicatedRecord");
                }
            }
        }

        private bool _pReconnectEnabled;
        public bool ReconnectEnabled
        {
            get { return _pReconnectEnabled; }
            set
            {
                if (_pReconnectEnabled != value)
                {
                    _pReconnectEnabled = value;
                    OnPropertyChanged("ReconnectEnabled");
                }
            }
        }

        private int _pConnectAttempInterval;
        public int ConnectAttempInterval
        {
            get { return _pConnectAttempInterval; }
            set
            {
                if (_pConnectAttempInterval != value)
                {
                    _pConnectAttempInterval = value;
                    OnPropertyChanged("ConnectAttempInterval");
                }
            }
        }

        private string _pIBAppName;
        public string IBAppName
        {
            get { return _pIBAppName; }
            set
            {
                if (_pIBAppName != value)
                {
                    _pIBAppName = value;
                    OnPropertyChanged("IBAppName");
                }
            }
        }

        private string _pStartUpPath;
        public string StartUpPath
        {
            get { return _pStartUpPath; }
            set
            {
                if (_pStartUpPath != value)
                {
                    _pStartUpPath = value;
                    OnPropertyChanged("StartUpPath");
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
    /// <summary>
    /// Interaction logic for Setting.xaml
    /// </summary>
    public partial class Setting : Window
    {       
        UserPreference settings;
        public Setting(List<IController> ctrls)
        {
            InitializeComponent();
            // set Window Icon
            /*
            string resourceName = "AmiBroker.Controllers.images.setting.png";
            Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
            this.Icon = BitmapFrame.Create(s);*/
            Uri uri = new Uri("pack://application:,,,/OrderManager;component/Controllers/images/setting.png");
            BitmapImage bi = new BitmapImage(uri);
            this.Icon = bi;

            // read preference from setting
            string up = Properties.Settings.Default["preference"].ToString();
            if (up != string.Empty)
                settings = JsonConvert.DeserializeObject<UserPreference>(up);
            else
                settings = new UserPreference();
            this.DataContext = settings;

            // set CheckBox (exclusive) click event
            chk_ft_ex.Click += (sender, EventArgs) => { CheckBox_Checked(sender, EventArgs, settings.FTAccount); };
            chk_ib_ex.Click += (sender, EventArgs) => { CheckBox_Checked(sender, EventArgs, settings.IBAccount); };

            // set ReadOnly = true if the connection is being connected, so that item will be non-editable
            foreach (string vendor in settings.Vendors)
            {
                AccountOption accOpt = (dynamic)settings.GetType().GetProperty(vendor + "Account").GetValue(settings);
                foreach (var item in accOpt.Accounts)
                {
                    var ctrl = ctrls.FirstOrDefault(x => x.DisplayName == vendor + "(" + item.AccName + ")");
                    if (ctrl != null)
                        item.ReadOnly = ctrl.IsConnected;
                }
            }
        }
        // Remove attribute ReadOnly's value for storing the value
        private void RemoveRedundant()
        {
            foreach (string vendor in settings.Vendors)
            {
                AccountOption accOpt = (dynamic)settings.GetType().GetProperty(vendor + "Account").GetValue(settings);
                foreach (var item in accOpt.Accounts)
                {   
                   item.ReadOnly = false;
                }
            }
        }
        // if Accounts are exclusive and one is being connected (ReadOnly), IsActivate checkbox should be disabled. 
        private bool CheckIfActivateEnabled(string vendor)
        {
            AccountOption accOpt = (dynamic)settings.GetType().GetProperty(vendor + "Account").GetValue(settings);
            if (accOpt != null && accOpt.IsExclusive)
            {
                foreach (var item in accOpt.Accounts)
                {
                    if (item.ReadOnly)
                        return false;
                }
            }
            return true;
        }
        private bool CheckIfActivateEnabled(AccountOption accOpt)
        {           
            if (accOpt != null && accOpt.IsExclusive)
            {
                foreach (var item in accOpt.Accounts)
                {
                    if (item.ReadOnly)
                        return false;
                }
            }
            return true;
        }
        private void AddBtn_Click(object sender, RoutedEventArgs e)
        {
            string vendor = ((Button)sender).Name.Substring(4, 2).ToUpper();
            AccountConfig ac = new AccountConfig();
            AccountOption accOpt = (dynamic)settings.GetType().GetProperty(vendor + "Account").GetValue(settings);
            ac.chkIsEnabled.IsEnabled = CheckIfActivateEnabled(accOpt);
            ac.ShowDialog();
            if ((bool)ac.DialogResult)
            {
                ConnectionParam ao = new ConnectionParam();
                ao.AccName = ac.AccName;
                ao.Host = ac.Host;
                ao.Port = ac.Port;
                ao.ClientId = ac.ClientId; 
                ao.IsActivate = ac.IsActivate;
                ao.IsMulti = ac.IsMulti;
                accOpt.Accounts.Add(ao);
                if (accOpt.IsExclusive && ac.IsActivate)
                {
                    foreach (ConnectionParam cp in accOpt.Accounts)
                    {
                        if (cp.AccName != ao.AccName)
                            cp.IsActivate = false;
                    }
                    accOpt.Accounts = new ObservableCollection<ConnectionParam>(accOpt.Accounts);
                }
            }
        }

        private void EditBtn_Click(object sender, RoutedEventArgs e)
        {
            string vendor = ((Button)sender).Name.Substring(4, 2);
            EditAccountConfig(vendor);
        }
        private void DeleteBtn_Click(object sender, RoutedEventArgs e)
        {
            string vendor = ((Button)sender).Name.Substring(4, 2);
            DeleteAccountConfig(vendor);
        }
        private void OKBtn_Click(object sender, RoutedEventArgs e)
        {
            RemoveRedundant();
            Properties.Settings.Default["preference"] = JsonConvert.SerializeObject(settings);
            Properties.Settings.Default.Save();
            this.DialogResult = true;
            this.Close();
        }

        private bool cancelClose = false;
        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            cancelClose = false;
            if (_OnWindowClosing())
            {
                this.DialogResult = false;
                this.Close();
            }
            else
                cancelClose = true;
        }

        private bool _OnWindowClosing()
        {
            RemoveRedundant();
            if (JsonConvert.SerializeObject(settings) != Properties.Settings.Default["preference"].ToString())
            {
                MessageBoxResult result = MessageBox.Show("Do you want to quit without saving?",
                                          "Confirmation",
                                          MessageBoxButton.YesNo,
                                          MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    // prevent asking again
                    settings = JsonConvert.DeserializeObject<UserPreference>(Properties.Settings.Default["preference"].ToString());
                    return true;
                }                    
                else
                    return false;
            }
            else
                return true;
        }        
        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            TreeViewItem ti_new = e.NewValue as TreeViewItem;
            if (ti_new != null && ti_new.Name != string.Empty)
            {
                TabItem ti = (TabItem)this.FindName("ti" + ti_new.Name.Substring(2));
                if (ti != null) ti.IsSelected = true;
            }                   
        }

        private void ListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ListView lv = sender as ListView;
            if (lv.SelectedItem != null && !((dynamic)lv.SelectedItem).ReadOnly)
                EditAccountConfig(((ListView)sender).Name.Substring(3, 2));
        }

        private void EditAccountConfig(string vendor)
        {
            ListView lv = (ListView)this.FindName("lv_" + vendor + "_acc");
            if (lv.SelectedIndex != -1)
            {
                AccountConfig ac = new AccountConfig();
                ac.txtName.IsReadOnly = true;
                ac.Owner = this;
                string prop = vendor.ToUpper() + "Account";
                AccountOption accOpt = (dynamic)settings.GetType().GetProperty(prop).GetValue(settings);
                ac.chkIsEnabled.IsEnabled = CheckIfActivateEnabled(accOpt);
                ObservableCollection<ConnectionParam> aos = accOpt.Accounts;
                ConnectionParam ao = aos[lv.SelectedIndex];
                ac.AccName = ao.AccName;
                ac.Host = ao.Host;
                ac.Port = ao.Port;
                ac.ClientId = ao.ClientId;
                ac.IsActivate = ao.IsActivate;
                ac.IsMulti = ao.IsMulti;
                ac.ShowDialog();
                if ((bool)ac.DialogResult)
                {
                    ao.AccName = ac.AccName;
                    ao.Host = ac.Host;
                    ao.Port = ac.Port;
                    ao.ClientId = ac.ClientId;
                    ao.IsActivate = ac.IsActivate;
                    ao.IsMulti = ac.IsMulti;
                    if (accOpt.IsExclusive && ac.IsActivate)
                    {
                        foreach (ConnectionParam cp in accOpt.Accounts)
                        {
                            if (cp.AccName != ao.AccName)
                                cp.IsActivate = false;
                        }
                    }
                    accOpt.Accounts = new ObservableCollection<ConnectionParam>(aos);
                }
            }
        }

        private void DeleteAccountConfig(string vendor)
        {
            ListView lv = (ListView)this.FindName("lv_" + vendor + "_acc");
            //ListItem li = lv.ItemContainerGenerator.ContainerFromIndex(lv.SelectedIndex);
            if (lv.SelectedIndex != -1)
            {                
                string prop = vendor.ToUpper() + "Account";
                AccountOption accOpt = (dynamic)settings.GetType().GetProperty(prop).GetValue(settings);
                ObservableCollection<ConnectionParam> aos = accOpt.Accounts;
                ConnectionParam ao = aos[lv.SelectedIndex];
                MessageBoxResult result = MessageBox.Show("Are you sure to delete this configuration?",
                                          "Confirmation",
                                          MessageBoxButton.YesNo,
                                          MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    aos.RemoveAt(lv.SelectedIndex);
                }
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (!cancelClose)
                e.Cancel = !_OnWindowClosing();
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e, AccountOption accOpt)
        {
            
            CheckBox chkbox = sender as CheckBox;
            if (chkbox != null && (bool)chkbox.IsChecked)
            {
                MessageBoxResult result = MessageBox.Show("If Exclusive is checked, only the first item with Activate True will be kept.\nAre you sure to continue?",
                                          "Confirmation",
                                          MessageBoxButton.YesNo,
                                          MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    bool firstFound = false;
                    var cps = accOpt.Accounts.Where(x => x.ReadOnly);
                    ConnectionParam cp = null;
                    if (cps.Count() > 1)
                    {
                        MessageBox.Show("Exclusive cannot be checked since there are more than one using connections.",
                                         "Warning",
                                         MessageBoxButton.OK,
                                         MessageBoxImage.Exclamation);
                        accOpt.IsExclusive = false;
                        return;
                    }
                    else
                    {
                        cp = cps.ToList()[0];
                    }
                    foreach (var item in accOpt.Accounts)
                    {
                        if (cp != null && item != cp)
                        {
                            item.IsActivate = false;
                        }
                        if (item.IsActivate && cp == null)
                        {
                            if (!firstFound)
                                firstFound = true;
                            else
                                item.IsActivate = false;
                        }

                    }
                    accOpt.Accounts = new ObservableCollection<ConnectionParam>(accOpt.Accounts);
                } else
                {
                    accOpt.IsExclusive = false;
                }
            }            
        }

        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            // Set filter for file extension and default file extension 
            dlg.DefaultExt = ".exe";
            dlg.Filter = "Executable Files (*.exe)|*.exe|Bash Files (*.png)|*.png";

            // Display OpenFileDialog by calling ShowDialog method 
            Nullable<bool> result = dlg.ShowDialog();

            // Get the selected file name and display in a TextBox 
            if (result == true)
            {
                // Open document 
                string filename = dlg.FileName;
                settings.StartUpPath = filename;
            }
        }
    }
}
