using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window,  INotifyPropertyChanged
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


        private DateTime? _pDate1 = DateTime.Now;
        public DateTime? Date1
        {
            get { return _pDate1; }
            set
            {
                if (_pDate1 != value)
                {
                    _pDate1 = value;
                    OnPropertyChanged("Date1");
                }
            }
        }


        private DateTime? _pDate2 = DateTime.Now;
        public DateTime? Date2
        {
            get { return _pDate2; }
            set
            {
                if (_pDate2 != value)
                {
                    _pDate2 = value;
                    OnPropertyChanged("Date2");
                }
            }
        }


        public MainWindow()
        {
            InitializeComponent();
            Date2 = DateTime.Now.AddDays(1);
            Date2 = DateTime.Now.AddDays(2);
            this.DataContext = this;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
                    }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {

        }
    }
}
