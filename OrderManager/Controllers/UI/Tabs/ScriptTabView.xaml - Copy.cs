using Sdl.MultiSelectComboBox.Themes.Generic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using AmiBroker.OrderManager;
using System.Windows.Input;
using System.Windows.Media;
using System.Collections.ObjectModel;
using System.Reflection;

namespace AmiBroker.Controllers
{
    public class OrderTypeDetailSelector : DataTemplateSelector
    {
        static ScriptTabView VisualUpwardSearch(DependencyObject source)
        {
            while (source != null && !(source is ScriptTabView))
                source = VisualTreeHelper.GetParent(source);

            return source as ScriptTabView;
        }
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            ScriptTabView d = VisualUpwardSearch(container);
            if (item != null)
            {
                Type t = item.GetType();
                if (t.IsSubclassOf(typeof(FTOrderType)))
                    return d.FindResource("FTOrderTypeDetail") as DataTemplate;
                else if (t.IsSubclassOf(typeof(IBOrderType)))
                    return d.FindResource("IBOrderTypeDetail") as DataTemplate;
                else
                    return null;
            }
            else
                return null;            
        }
    }
    /// <summary>
    /// Interaction logic for ScriptTabView.xaml
    /// </summary>
    [DataContract(IsReference = true)]
    public partial class ScriptTabView : UserControl
    {
        public ScriptTabView()
        {
            InitializeComponent();

        }
        
        private void Mc1_Loaded(object sender, RoutedEventArgs e)
        {
            before_mc_loaded(sender);
        }

        private void before_mc_loaded(object sender)
        {
            var mc = sender as MultiSelectComboBox;
            MainViewModel vm = (MainViewModel)this.DataContext;
            SymbolInAction symbol = null;
            var si = vm.SelectedItem;
            if (si.GetType() == typeof(Script))
            {
                symbol = ((Script)si).Symbol;
            }
            else if (si.GetType() == typeof(Strategy))
            {
                symbol = ((Strategy)si).Script.Symbol;
            }
            mc.ItemsSource = symbol.AccountCandidates;
        }

        // ensure right-clik will select an item
        private void OnPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            TreeViewItem treeViewItem = VisualUpwardSearch(e.OriginalSource as DependencyObject);

            if (treeViewItem != null)
            {
                treeViewItem.Focus();
                e.Handled = true;
            }
        }

        static TreeViewItem VisualUpwardSearch(DependencyObject source)
        {
            while (source != null && !(source is TreeViewItem))
                source = VisualTreeHelper.GetParent(source);

            return source as TreeViewItem;
        }

        private void ChangeItems(string orders_name, SelectionChangedEventArgs e)
        {
            MainViewModel vm = DataContext as MainViewModel;
            if (vm.SelectedItem.GetType().IsSubclassOf(typeof(SSBase)))
            {
                PropertyInfo pi = vm.SelectedItem.GetType().GetProperty(orders_name);
                ObservableCollection<BaseOrderType> otCollection = (ObservableCollection<BaseOrderType>)pi.GetValue(vm.SelectedItem);
                if (((object)vm.SelectedItem).GetType().IsSubclassOf(typeof(SSBase)))
                {
                    foreach (var item in e.RemovedItems)
                    {
                        // when switching between datatemplate, this will be invoked and selected item will be removed
                        if (e.AddedItems.Count > 0)
                        {
                            BaseOrderType bot = otCollection.FirstOrDefault(x => x.GetType() == item.GetType());
                            if (bot != null)
                                otCollection.Remove(bot);
                        }
                    }
                    foreach (var item in e.AddedItems)
                    {
                        BaseOrderType ot = otCollection.FirstOrDefault(x => x.GetType() == item.GetType());
                        if (ot == null)
                            otCollection.Add(((BaseOrderType)item).Clone());
                    }
                }
            }
            object o = new object();
            
        }

        private void CmbBuyOT_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ChangeItems("BuyOrderTypes", e);                                    
        }
        private void CmbSellOT_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ChangeItems("SellOrderTypes", e);
        }
        private void CmbShortOT_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ChangeItems("ShortOrderTypes", e);
        }
        private void CmbCoverOT_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ChangeItems("CoverOrderTypes", e);
        }
    }
}
