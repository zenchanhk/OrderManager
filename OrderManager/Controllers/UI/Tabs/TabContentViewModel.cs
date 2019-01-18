using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Documents;

namespace AmiBroker.Controllers
{
    public class TabContentViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event
        /// </summary>
        /// <param name="propertyName">The name of the property that was changed</param>
        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }
        public TabContentViewModel(TabContentModel model)
        {
            this._pHeader = model.Header;

            // I personally don't find it great to put a control in the ViewModel. Maybe we should use a converter or a datatemplate for that?
            this.Content = model.Content;
        }

        private string _pHeader;
        public string Header
        {
            get { return _pHeader; }
            set
            {
                if (_pHeader != value)
                {
                    _pHeader = value;
                    OnPropertyChanged("Header");
                }
            }
        }
        public UserControl Content { get; }
    }
}
