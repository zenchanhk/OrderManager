using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AmiBroker.Controllers;
using AmiBroker.OrderManager;

namespace ControlLib
{
    /// <summary>
    /// Interaction logic for SlippageListView.xaml
    /// </summary>
    public partial class SlippageListView : UserControl
    {
        public Image ImageDel { get; private set; } = Util.MaterialIconToImage(MaterialIcons.Minus, Util.Color.Red);
        
        public SlippageListView()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register("ItemsSource", typeof(IList), typeof(SlippageListView),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, ItemsPropertyChangedCallback));

        public IList ItemsSource
        {
            get => (IList)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }
        private static void ItemsPropertyChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            SlippageListView d = dependencyObject as SlippageListView;            
            ((ListView)d.FindName("lv")).ItemsSource = (IEnumerable)dependencyPropertyChangedEventArgs.NewValue;
        }
        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            int s = 1;
            ObservableCollection<CSlippage> items = (ObservableCollection<CSlippage>)ItemsSource;
            if (items != null && items.Count > 0) s = items[items.Count - 1].Slippage + 1;
            ((ObservableCollection<CSlippage>)ItemsSource).Add(new CSlippage { Slippage = s, PosSize = 1 });
        }

        private void BtnDel_Click(object sender, RoutedEventArgs e)
        {
            object item = ((ListView)FindName("lv")).SelectedItem;
            if (item != null)
                ((ObservableCollection<CSlippage>)ItemsSource).Remove((CSlippage)item);
        }

        private void BtnDelAll_Click(object sender, RoutedEventArgs e)
        {
            ItemsSource.Clear();
        }
    }
}
