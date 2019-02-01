using AmiBroker.Controllers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AmiBroker.OrderManager
{
    public class VendorOrderType
    {
        public string Name { get; set; }
        public List<BaseOrderType> OrderTypes { get; set; } = new List<BaseOrderType>();
    }
    public enum BarInterval
    {
        Tick = 0,
        K1Min = 1,
        K5Min = 5,
        K15Min = 15,
        K30Min = 30,
        KHour = 60,
        KDay = 1440,
        KWeek = 7,
        KMonth = 8,
        KYear = 9
    }
    public class BaseOrderType : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }
        string Name { get; set; }   // Should be unique globally
        [JsonIgnore]
        public string Description { get; protected set; }

        private string _dtFormat = "yyyyMMdd HH:mm:ss";
        public string DateTimeFormat
        {
            get => _dtFormat;
            set
            {
                _dtFormat = value;
                GoodAfterTime.DateTimeFormat = _dtFormat;
                GoodTilDate.DateTimeFormat = _dtFormat;
            }
        }

        private AmiBroker.Controllers.TimeZone _timeZone;
        public AmiBroker.Controllers.TimeZone TimeZone
        {
            get => _timeZone;
            set
            {
                _timeZone = value;
                GoodTilDate.TimeZone = value;
                GoodAfterTime.TimeZone = value;
            }
        }
        public GoodTime GoodTilDate { get; set; } = new GoodTime();
        public GoodTime GoodAfterTime { get; set; } = new GoodTime();  // yyyyMMdd HH:mm:ss

        private float _pSlippage;
        public float Slippage
        {
            get { return _pSlippage; }
            set
            {
                if (_pSlippage != value)
                {
                    _pSlippage = value;
                    OnPropertyChanged("Slippage");
                }
            }
        }
        public BaseOrderType()
        {
            GoodAfterTime.DateTimeFormat = DateTimeFormat;
            GoodTilDate.DateTimeFormat = DateTimeFormat;
        }

        public virtual BaseOrderType Clone()
        {
            BaseOrderType ot = (BaseOrderType)this.MemberwiseClone();
            return ot;
        }

        public virtual void CopyTo(BaseOrderType dest) { }
        public virtual async Task<int> PlaceOrder(AccountInfo accountInfo, string  symbol) { return -1; }
    }
}
