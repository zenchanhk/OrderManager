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

        public virtual BaseOrderType Clone()
        {
            BaseOrderType ot = (BaseOrderType)this.MemberwiseClone();
            return ot;
        }

        public virtual void CopyTo(BaseOrderType dest) { }
    }
}
