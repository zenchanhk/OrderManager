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
using ControlLib;
using AmiBroker.OrderManager;
using System.Windows.Data;

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
            ViewFactory.CreateView(typeof(Script), (DataTemplate)FindResource("DTforScript"));
            ViewFactory.CreateView(typeof(Strategy), (DataTemplate)FindResource("DTforStrategy"));
            ViewFactory.CreateView(typeof(SymbolInAction), (DataTemplate)FindResource("DTforSymbolInAction"));
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
        /*
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
        }*/

        private void DateTimeEditor_ValueChanged(object sender, ControlLib.DateTimeChangedEventArgs e)
        {
            DateTimeEditor dt = sender as DateTimeEditor;
            if (dt.DataContext != null)
                ((IBOrderType)dt.DataContext).GoodAfterTime.SelectedIndex = 1;
        }

        private void up_gat_ValueChanged(object sender, ValueChangedEventArgs e)
        {
            NumericUpDown up = sender as NumericUpDown;
            if (up.DataContext != null)
                ((IBOrderType)up.DataContext).GoodAfterTime.SelectedIndex = 1;
        }

        private void Ud_sec_ValueChanged(object sender, ValueChangedEventArgs e)
        {
            NumericUpDown up = sender as NumericUpDown;
            if (up.DataContext != null)
                ((IBOrderType)up.DataContext).GoodAfterTime.SelectedIndex = 2;
        }

        private void Ud_bar_ValueChanged(object sender, ValueChangedEventArgs e)
        {
            NumericUpDown up = sender as NumericUpDown;
            if (up.DataContext != null)
                ((IBOrderType)up.DataContext).GoodAfterTime.SelectedIndex = 3;
        }

        private void gtd_DateTimeEditor_ValueChanged(object sender, ControlLib.DateTimeChangedEventArgs e)
        {
            DateTimeEditor dt = sender as DateTimeEditor;
            if (dt.DataContext != null)
                ((IBOrderType)dt.DataContext).GoodTilDate.SelectedIndex = 1;
        }

        private void gtd_up_ValueChanged(object sender, ValueChangedEventArgs e)
        {
            NumericUpDown up = sender as NumericUpDown;
            if (up.DataContext != null)
                ((IBOrderType)up.DataContext).GoodTilDate.SelectedIndex = 1;
        }

        private void gtd_Ud_sec_ValueChanged(object sender, ValueChangedEventArgs e)
        {
            NumericUpDown up = sender as NumericUpDown;
            if (up.DataContext != null)
                ((IBOrderType)up.DataContext).GoodTilDate.SelectedIndex = 2;
        }

        private void gtd_Ud_bar_ValueChanged(object sender, ValueChangedEventArgs e)
        {
            NumericUpDown up = sender as NumericUpDown;
            if (up.DataContext != null)
                ((IBOrderType)up.DataContext).GoodTilDate.SelectedIndex = 3;
        }

        private void cb_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox cb = sender as CheckBox;
            if (cb != null)
            {
                if ((bool)cb.IsChecked) return;
            }
            else
                return;

            // in case IsChecked = False
            Binding binding = BindingOperations.GetBinding((CheckBox)sender, CheckBox.IsCheckedProperty);
            string path = binding.Path.Path;
            object item = ((MainViewModel)this.DataContext).SelectedItem;
            if (item.GetType() == typeof(Script))
            {
                Script script = item as Script;
                foreach(var strategy in script.Strategies)
                {
                    PropertyInfo pi = strategy.GetType().GetProperty(path);
                    if (pi != null)
                    {
                        pi.SetValue(strategy, false);
                    }
                    else
                        throw new Exception("Property: " + path + " not found in Strategy");
                }
            }
        }
    }
}
