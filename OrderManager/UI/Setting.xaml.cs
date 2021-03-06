﻿using System;
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
            string resourceName = "AmiBroker.Controllers.images.setting.png";
            Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
            this.Icon = BitmapFrame.Create(s);

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
                ac.ShowDialog();
                if ((bool)ac.DialogResult)
                {
                    ao.AccName = ac.AccName;
                    ao.Host = ac.Host;
                    ao.Port = ac.Port;
                    ao.ClientId = ac.ClientId;
                    ao.IsActivate = ac.IsActivate;
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
                    foreach (var item in accOpt.Accounts)
                    {
                        if (item.IsActivate)
                        {
                            if (!firstFound)
                                firstFound = true;
                            else
                                item.IsActivate = false;
                        }
                    }
                    accOpt.Accounts = new ObservableCollection<ConnectionParam>(accOpt.Accounts);
                }
            }            
        }
    }
}
