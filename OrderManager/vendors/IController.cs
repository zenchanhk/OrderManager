using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IBApi;
using IB.CSharpApiClient.Events;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace AmiBroker.Controllers
{
    public class AccountTag : INotifyPropertyChanged
    {
        public Type Type { get; }
           
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }

        public string Tag { get; set; }
        public string Currency { get; set; }
        
        private string _pValue;
        public string Value
        {
            get { return _pValue; }
            set
            {
                if (_pValue != value)
                {
                    _pValue = value;
                    OnPropertyChanged("Value");
                }
            }
        }

    }
    public class AccountInfo
    {
        public string Name { get; set; }
        public ObservableCollection<AccountTag> Properties { get; set; } = new ObservableCollection<AccountTag>();
    }
    public interface IController
    {
        AccountInfo SelectedAccount { get; set; }
        // IB can have more than one linked account (Finacial Advisor Account and sub accounts)
        ObservableCollection<AccountInfo> Accounts { get; } 
        string DisplayName { get; set; }
        ConnectionParam ConnParam { get; set; }
        bool IsConnected { get; }
        string ConnectionStatus { set; get; }
        void Connect();
        void Disconnect();
    }
}
