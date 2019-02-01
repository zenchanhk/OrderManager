using AmiBroker.OrderManager;
using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;

namespace AmiBroker.Controllers
{
    /// <summary>
    /// Interaction logic for AssignToStrategy.xaml
    /// </summary>
    public partial class AssignToStrategy : Window
    {       
        public AssignToStrategy()
        {
            InitializeComponent();
            Icon = (DrawingImage)this.FindResource("assignDrawingImage");
        }

        private void OKBtn_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close(); 
        }

        private void AssignToStrategy_Loaded(object sender, RoutedEventArgs e)
        {
            ComboBox cb = FindName("cb_strategy") as ComboBox;
            //if (cb != null)
            //    cb.ItemsSource = Strategies;
        }
    }

    public class AssignToStrategyVM : NotifyPropertyChangedBase
    {

        private List<Strategy> _pStrategies = new List<Strategy>();
        public List<Strategy> Strategies
        {
            get { return _pStrategies; }
            set { _UpdateField(ref _pStrategies, value); }
        }

        private string _pSymbol;
        public string Symbol
        {
            get { return _pSymbol; }
            set { _UpdateField(ref _pSymbol, value); }
        }

        private double _pAvailablePosition;
        public double AvailablePosition
        {
            get { return _pAvailablePosition; }
            set { _UpdateField(ref _pAvailablePosition, value); }
        }

        private Strategy _pSelectedItem;
        public Strategy SelectedItem
        {
            get { return _pSelectedItem; }
            set { _UpdateField(ref _pSelectedItem, value); }
        }

        private double _pAssignedPosition;
        public double AssignedPosition
        {
            get { return _pAssignedPosition; }
            set { _UpdateField(ref _pAssignedPosition, value); }
        }

    }
}
