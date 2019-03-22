using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using AmiBroker.OrderManager;
using System.Windows;
using System.Collections.ObjectModel;
using System.Reflection;
using Xceed.Wpf.AvalonDock;

namespace AmiBroker.Controllers
{
    public class Commands
    {
        public ICommand SaveLayout { get; set; } = new SaveLayout();
        public ICommand RestoreLayout { get; set; } = new RestoreLayout();
        public ICommand ConnectAll { get; set; } = new ConnectAll();
        public ICommand DisconnectAll { get; set; } = new DisconnectAll();
        public ICommand CloseAllOpenOrders { get; set; } = new CloseAllOpenOrders();
        public ICommand CloseCurrentOpenOrders { get; set; } = new CloseCurrentOpenOrders();
        public ICommand CloseSymbolOpenOrders { get; set; } = new CloseSymbolOpenOrders();
        public ICommand CancelPendingOrder { get; set; } = new CancelPendingOrder();
        public ICommand ShowConfigDialog { get; set; } = new DisplayConfigDialog();
        public ICommand ConnectByContextMenu { get; set; } = new ConnectByContextMenu();
        public ICommand RefreshParameters { get; set; } = new RefreshParameters();
        public ICommand EnableScriptExecution { get; set; } = new EnableScriptExecution();
        public ICommand ApplySettingsToStrategy { get; set; } = new ApplySettingsToStrategy();
        public ICommand ClearSettings { get; set; } = new ClearSettings();
        public ICommand SaveAsTemplate { get; set; } = new SaveAsTemplate();
        public ICommand OpenTemplate { get; set; } = new OpenTemplate();
        public ICommand ClearAllTemplate { get; set; } = new ClearAllTemplate();
        public ICommand OutSaveAsTemplate { get; set; } = new OutSaveAsTemplate();
        public ICommand OutOpenTemplate { get; set; } = new OutOpenTemplate();
        public ICommand DeleteTemplate { get; set; } = new DeleteTemplate();
        public ICommand RenameTemplate { get; set; } = new RenameTemplate();
        public ICommand EditTemplateName { get; set; } = new EditTemplateName();
        public ICommand EditTemplateOnSite { get; set; } = new EditTemplateOnSite();
        public ICommand SaveTemplateOnSite { get; set; } = new SaveTemplateOnSite();
        public ICommand CancelEditTemplateOnSite { get; set; } = new CancelEditTemplateOnSite();
        public ICommand CopyOrderSetup { get; set; } = new CopyOrderSetup();
        public ICommand Export { get; set; } = new Export();
        public ICommand ClearListView { get; set; } = new ClearListView();
        public ICommand AssignStrategy { get; set; } = new AssignStrategy();
        public ICommand Test { get; set; } = new Test();
    }
    public class Test: ICommand
    {
        public bool CanExecute(object parameter)
        {
            return true;
        }
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public void Execute(object parameter)
        {
            MainViewModel vm = MainViewModel.Instance;
            IBController c = vm.Controllers.FirstOrDefault(x => x.ConnParam.AccName == "zenhao") as IBController;
            if (c.IsConnected)
            {
                c.test();
            }
        }
    }
    public class AssignStrategy : ICommand
    {
        public bool CanExecute(object parameter)
        {
            if (parameter != null && parameter.GetType() == typeof(DisplayedOrder))
            {
                MainViewModel mainVM = MainViewModel.Instance;
                DisplayedOrder order = parameter as DisplayedOrder;
                if (mainVM.OrderInfoList.ContainsKey(order.OrderId))
                {
                    OrderInfo oi = mainVM.OrderInfoList[order.OrderId];
                    if (oi.Filled == oi.PosSize)
                        return false;
                }                    
            }
            return true;
        }
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public void Execute(object parameter)
        {
            int roundLotSize = 0;
            string symbolName = string.Empty;
            List<Strategy> strategies = new List<Strategy>();

            double pos = 0;
            MainViewModel vm = MainViewModel.Instance;
            SymbolInMkt selectedSymbol = (SymbolInMkt)vm.SelectedPortfolio;
            if (parameter.GetType() == typeof(SymbolInMkt))
                pos = selectedSymbol.Position;

            
            var symbols = vm.SymbolInActions.Where(x => 
            {
                SymbolDefinition sd = x.SymbolDefinition.FirstOrDefault(y => y.Controller.Vendor == selectedSymbol.Vendor);
                if (sd != null)
                {
                    symbolName = sd.ContractId;
                    string ex1 = sd?.Contract?.Exchange != null ? sd?.Contract?.Exchange : sd?.Contract?.PrimaryExch;
                    symbolName += " - " + ex1;
                    //string ex2 = ((dynamic)parameter).Contract?.Exchange != null ? ((dynamic)parameter).Contract?.Exchange : ((dynamic)parameter).Contract?.PrimaryExch;
                    return sd.Contract != null && sd?.Contract?.ConId == selectedSymbol.Contract?.ConId;
                        //&& ex1 == ex2;
                }                    
                else
                    return false;
            }
            );

            foreach (var symbol in symbols)
            {
                roundLotSize = (int)symbol.RoundLotSize;
                foreach (Script script in symbol.Scripts)
                {
                    if (script.AccountStat.ContainsKey(((dynamic)parameter).Account))
                    {
                        if (pos != 0)
                            pos = pos > 0 ? pos - script.AccountStat[((dynamic)parameter).Account].LongPosition * roundLotSize :
                                        pos + script.AccountStat[((dynamic)parameter).Account].ShortPosition * roundLotSize;
                        foreach (Strategy strategy in script.Strategies)
                        {
                            if (strategy.AccountStat.ContainsKey(((dynamic)parameter).Account) && (
                                (pos > 0 && (strategy.ActionType == ActionType.Long || strategy.ActionType == ActionType.LongAndShort)) ||
                                (pos < 0 && (strategy.ActionType == ActionType.Short || strategy.ActionType == ActionType.LongAndShort))
                                ))
                                strategies.Add(strategy);
                        }
                    }
                }        
            }            

            if (strategies.Count == 0 || (parameter.GetType() == typeof(SymbolInMkt) && pos == 0))
            {
                MessageBoxResult result = MessageBox.Show("Cannot find strategy for selected symbol", "Information", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }
            if (parameter.GetType() == typeof(SymbolInMkt) && ((((SymbolInMkt)parameter).Position > 0 && pos < 0) ||
                (((SymbolInMkt)parameter).Position < 0 && pos > 0)))
            {
                MessageBoxResult result = MessageBox.Show("Position calculation error for selected symbol", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            AssignToStrategyVM assignToStrategyVM = new AssignToStrategyVM();
            assignToStrategyVM.Strategies = strategies;
            assignToStrategyVM.AvailablePosition = pos != 0 ? pos / roundLotSize : 0;
            assignToStrategyVM.Symbol = symbolName;
            AssignToStrategy assignToStrategy = new AssignToStrategy();
            assignToStrategy.DataContext = assignToStrategyVM;
            assignToStrategy.ShowDialog();

            if ((bool)assignToStrategy.DialogResult && 
                assignToStrategyVM.AssignedPosition > 0 &&
                assignToStrategyVM.SelectedItem != null)
            {
                Strategy strategy = (Strategy)assignToStrategyVM.SelectedItem;
                BaseStat strategyStat = strategy.AccountStat[((SymbolInMkt)parameter).Account];
                BaseStat scriptStat = strategy.Script.AccountStat[((SymbolInMkt)parameter).Account];
                if (assignToStrategyVM.AssignedPosition > 0)
                {
                    strategyStat.LongPosition += assignToStrategyVM.AssignedPosition;
                    scriptStat.LongPosition += assignToStrategyVM.AssignedPosition;
                    AccountStatusOp.SetPositionStatus(ref strategyStat, OrderAction.Buy);
                }
                else
                {
                    strategyStat.ShortPosition += -assignToStrategyVM.AssignedPosition;
                    scriptStat.ShortPosition += -assignToStrategyVM.AssignedPosition;
                    AccountStatusOp.SetPositionStatus(ref strategyStat, OrderAction.Sell);
                }
            }
        }
    }
    public class CopyOrderSetup : ICommand
    {
        public bool CanExecute(object parameter)
        {
            return true;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public void Execute(object parameter)
        {
            List<object> value = (List<object>)parameter;
            ScriptTabView stv = value[0] as ScriptTabView;
            if (stv != null)
            {
                string[] ps = value[value.Count - 1].ToString().Split(new char[] { '$' });
                string src = ps[0];
                string dest = ps[1];
                SSBase ss = ((MainViewModel)stv.DataContext).SelectedItem as SSBase;
                VendorOrderType vendor = ss.SelectedVendor;
                if (vendor == null || vendor.OrderTypes.Count == 0) return;
                Type t = vendor.OrderTypes[0].GetType().BaseType;

                PropertyInfo pi = ss.GetType().GetProperty(src + "OrderTypes");
                ObservableCollection<BaseOrderType> src_orderTypes = (ObservableCollection<BaseOrderType>)pi.GetValue(ss);
                var src_item = src_orderTypes.FirstOrDefault(x => x.GetType().IsSubclassOf(t));

                pi = ss.GetType().GetProperty(dest + "OrderTypes");
                ObservableCollection<BaseOrderType> destOrderTypes = (ObservableCollection<BaseOrderType>)pi.GetValue(ss);
                var dest_item = destOrderTypes.FirstOrDefault(x => x.GetType().IsSubclassOf(t));

                BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                MethodInfo info = ss.GetType().GetMethod("OnPropertyChanged", flags);
                if (info == null) info = ss.GetType().GetMethod("_RaisePropertyChanged", flags);
                
                if (src_item != null && dest_item == null)
                {
                    destOrderTypes.Add(src_item.Clone());
                    info.Invoke(ss, new object[] { dest + "OrderTypes" });
                }                
                if (src_item != null && dest_item != null)
                {
                    if (src_item.GetType() == dest_item.GetType())
                    {
                        src_item.CopyTo(dest_item);
                        //info.Invoke(ss, new object[] { dest + "OrderTypes" });
                    }
                    else
                    {
                        destOrderTypes.Remove(dest_item);
                        destOrderTypes.Add(src_item.Clone());
                        info.Invoke(ss, new object[] { dest + "OrderTypes" });
                    }
                }
            }
        }
    }
    public class CancelEditTemplateOnSite : ICommand
    {
        public bool CanExecute(object parameter)
        {
            if (parameter == null) return false;
            SaveLoadTemplate template = parameter as SaveLoadTemplate;
            if (template.IsTemplateEditing)
                return true;
            else
                return false;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public void Execute(object item)
        {
            SaveLoadTemplate template = item as SaveLoadTemplate;            
            template.TemplateEditEndingAction = EditEndingAction.Cancel; // this must be called first
            template.IsTemplateEditing = false;
            template.TemplateEditEndingAction = EditEndingAction.Netural; // reset
            template.SelectedTemplate.ForceUpdateContent();
        }
    }
    public class EditTemplateOnSite : ICommand
    {
        public bool CanExecute(object parameter)
        {
            if (parameter == null) return false;
            SaveLoadTemplate template = parameter as SaveLoadTemplate;
            if (template.SelectedTemplate != null && !template.IsTemplateEditing)
                return true;
            else
                return false;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public void Execute(object item)
        {
            SaveLoadTemplate template = item as SaveLoadTemplate;
            template.IsTemplateEditing = true;
        }
    }
    public class SaveTemplateOnSite : ICommand
    {
        public bool CanExecute(object parameter)
        {
            if (parameter == null) return false;
            SaveLoadTemplate template = parameter as SaveLoadTemplate;
            if (template.IsTemplateEditing)
                return true;
            else
                return false;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public void Execute(object item)
        {
            SaveLoadTemplate template = item as SaveLoadTemplate;
            template.TemplateEditEndingAction = EditEndingAction.Save;
            template.SelectedTemplate.Name = template.TemplateName;
            template.SelectedTemplate.ModifiedDate = DateTime.Now;
            template.SelectedTemplate.Content = template.SelectedTemplate.Content;
            Properties.Settings.Default[template.PropName] = JsonConvert.SerializeObject(template.TemplateList);
            Properties.Settings.Default.Save();
            template.IsTemplateEditing = false;
            template.TemplateEditEndingAction = EditEndingAction.Netural;
        }
    }
    public class DeleteTemplate : ICommand
    {
        public bool CanExecute(object parameter)
        {
            if (parameter == null) return false;
            SaveLoadTemplate template = parameter as SaveLoadTemplate;
            if (template.SelectedTemplate != null && !template.IsTemplateEditing)
                return true;
            else
                return false;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public void Execute(object item)
        {
            SaveLoadTemplate template = item as SaveLoadTemplate;
            MessageBoxResult msgResult = MessageBox.Show("Are you sure to delete the selected template permanently?",
                                          "Warning",
                                          MessageBoxButton.YesNo,
                                          MessageBoxImage.Warning, MessageBoxResult.No);
            if (msgResult == MessageBoxResult.No)
            {
                return;
            }
            else
            {
                template.TemplateList.Remove(template.SelectedTemplate);
                Properties.Settings.Default[template.PropName] = JsonConvert.SerializeObject(template.TemplateList);
                Properties.Settings.Default.Save();
            }
        }
    }
    public class EditTemplateName : ICommand
    {
        public bool CanExecute(object parameter)
        {
            if (parameter == null) return false;
            SaveLoadTemplate template = parameter as SaveLoadTemplate;
            if (template.SelectedTemplate != null && !template.IsTemplateEditing)
                return true;
            else
                return false;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public void Execute(object item)
        {
            SaveLoadTemplate template = item as SaveLoadTemplate;
            template.StartEditing();
        }
    }
    public class RenameTemplate : ICommand
    {
        public bool CanExecute(object parameter)
        {
            if (parameter == null) return false;
            SaveLoadTemplate template = parameter as SaveLoadTemplate;
            if (template.SelectedTemplate != null && !template.IsTemplateEditing)
                return true;
            else
                return false;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public void Execute(object item)
        {
            SaveLoadTemplate template = item as SaveLoadTemplate;
            Properties.Settings.Default[template.PropName] = JsonConvert.SerializeObject(template.TemplateList);
            Properties.Settings.Default.Save();
        }
    }
    public class OpenTemplate : ICommand
    {
        public bool CanExecute(object parameter)
        {
            if (parameter == null) return false;
            SaveLoadTemplate template = parameter as SaveLoadTemplate;
            if (template.SelectedTemplate != null)
                return true;
            else
                return false;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public void Execute(object item)
        {
            SaveLoadTemplate template = item as SaveLoadTemplate;
            template.DialogResult = true;
            template.Close();
        }
    }
    public class ClearAllTemplate : ICommand
    {
        public bool CanExecute(object parameter)
        {
            return true;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public void Execute(object vm)
        {
            MessageBoxResult result = MessageBox.Show("Are you sure to delete all templates?",
                "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);
            if (result == MessageBoxResult.Yes)
            {
                Properties.Settings.Default["SymbolInActionTemplates"] = "";
                Properties.Settings.Default["ScriptTemplates"] = "";
                Properties.Settings.Default["StrategyTemplates"] = "";
            }            
        }
    }
    public class SaveAsTemplate : ICommand
    {
        public bool CanExecute(object parameter)
        {
            if (parameter == null) return false;
            SaveLoadTemplate template = parameter as SaveLoadTemplate;
            if (template.TemplateName != null)
                return true;
            else
                return false;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public void Execute(object item)
        {
            SaveLoadTemplate template = item as SaveLoadTemplate;
            if (template.TemplateList == null)
                template.TemplateList = new ObservableCollection<OptionTemplate>();
            var result = template.TemplateList.FirstOrDefault(x => x.Name == template.TemplateName);
            if (result != null)
            {
                MessageBoxResult msgResult = MessageBox.Show("Are you sure to over-write the existing save?",
                                          "Confirmation",
                                          MessageBoxButton.YesNo,
                                          MessageBoxImage.Question, MessageBoxResult.No);
                if (msgResult == MessageBoxResult.No)
                {
                    return;
                }
                else
                // over-writing
                {
                    try
                    {
                        result.ContentAsString = JsonConvert.SerializeObject(template.SaveItem, JSONConstants.saveSerializerSettings);
                        result.ModifiedDate = DateTime.Now;
                    }
                    catch (Exception ex)
                    {
                        int i = 0;
                    }                    
                }
            }
            else
            {
                try
                {
                    template.TemplateList.Add(new OptionTemplate
                    {
                        Name = template.TemplateName,
                        TypeName = template.SaveItem.GetType().Name,
                        ContentAsString = JsonConvert.SerializeObject(template.SaveItem, JSONConstants.saveSerializerSettings),
                        ModifiedDate = DateTime.Now
                    });
                }
                catch (Exception ex)
                {
                    int i = 0;
                }                
            }
            
            Properties.Settings.Default[template.PropName] = JsonConvert.SerializeObject(template.TemplateList, JSONConstants.saveSerializerSettings);
            Properties.Settings.Default.Save();
            template.DialogResult = true;
            template.Close();
        }
    }
    public class OutOpenTemplate : ICommand
    {
        public bool CanExecute(object parameter)
        {
            if (parameter == null) return false;
            ScriptTabView scriptTabView = parameter as ScriptTabView;
            dynamic item = ((MainViewModel)scriptTabView.DataContext).SelectedItem;
            if (item == null) return false;
            return true;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public void Execute(object parameter)
        {
            if (parameter == null) return;
            ScriptTabView scriptTabView = parameter as ScriptTabView;
            dynamic item = ((MainViewModel)scriptTabView.DataContext).SelectedItem;
            SaveLoadTemplate tempWin = new SaveLoadTemplate(TemplateAction.Open, item);
            tempWin.ShowDialog();
            if ((bool)tempWin.DialogResult)
            {
                OptionTemplate template = tempWin.SelectedTemplate;
                item.CopyFrom(template.Content);
            }
        }
    }
    public class OutSaveAsTemplate : ICommand
    {
        public bool CanExecute(object parameter)
        {
            if (parameter == null) return false;
            return true;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public void Execute(object item)
        {
            SaveLoadTemplate tempWin = new SaveLoadTemplate(TemplateAction.Save, item);
            tempWin.ShowDialog();
        }
    }
    public class ClearSettings : ICommand
    {
        public bool CanExecute(object parameter)
        {
            if (parameter == null) return false;
            if (parameter.GetType() == typeof(Script) || parameter.GetType() == typeof(Strategy)
                || parameter.GetType() == typeof(SymbolInAction))
                return true;
            else
                return false;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public void Execute(object item)
        {            
            if (item != null)
            {
                MessageBoxResult result = MessageBox.Show("Are you sure to clear settings?",
                                          "Confirmation",
                                          MessageBoxButton.YesNo,
                                          MessageBoxImage.Question, MessageBoxResult.No);
                if (result == MessageBoxResult.Yes)
                {
                    ((dynamic)item).Clear();
                }
            }
        }
    }
    public class ApplySettingsToStrategy : ICommand
    {
        public bool CanExecute(object parameter)
        {
            if (parameter == null) return false;
            if (parameter.GetType() == typeof(Script))
                return true;
            else
                return false;            
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public void Execute(object item)
        {
            Script script = item != null ? item as Script : null;
            if (script != null)
            {
                MessageBoxResult result = MessageBox.Show("Are you sure to re-write the current setting of strategies?",
                                          "Confirmation",
                                          MessageBoxButton.YesNo,
                                          MessageBoxImage.Question, MessageBoxResult.No);
                if (result == MessageBoxResult.Yes)
                {
                    script.ApplySettingsToStrategies();
                }
            }
        }
    }
    public class EnableScriptExecution : ICommand
    {
        public bool CanExecute(object parameter)
        {
            if (parameter == null) return false;
            if (((dynamic)parameter).IsEnabled)
            {
                return true;
            }
            else
            {
                if (parameter.GetType() == typeof(SymbolInAction))
                    return true;
                if (parameter.GetType() == typeof(Script))
                {
                    Script s = parameter as Script;
                    if (s.Symbol.IsEnabled)
                        return true;
                    else
                        return false;
                }
                if (parameter.GetType() == typeof(Strategy))
                {
                    Strategy s = parameter as Strategy;
                    if (s.Script.IsEnabled)
                        return true;
                    else
                        return false;
                }
                return false;
            }            
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public void Execute(object item)
        {
            if (item != null) ((dynamic)item).IsEnabled = !((dynamic)item).IsEnabled;
        }
    }
    public class RestoreLayout : ICommand
    {
        public bool CanExecute(object parameter)
        {
            return true;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public void Execute(object mw)
        {
            if (System.IO.File.Exists("org_layout.cfg"))
            {
                DockingManager dock = OrderManager.MainWin.FindName("dockingManager") as DockingManager;
                Xceed.Wpf.AvalonDock.Layout.Serialization.XmlLayoutSerializer layoutSerializer = new Xceed.Wpf.AvalonDock.Layout.Serialization.XmlLayoutSerializer(dock);
                layoutSerializer.Deserialize("org_layout.cfg");
            }
        }
    }
    public class SaveLayout : ICommand
    {
        public bool CanExecute(object parameter)
        {
            return true;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public void Execute(object VM)
        {
            // Saves the layout and exit
            DockingManager dock = OrderManager.MainWin.FindName("dockingManager") as DockingManager;                
            Xceed.Wpf.AvalonDock.Layout.Serialization.XmlLayoutSerializer layoutSerializer = new Xceed.Wpf.AvalonDock.Layout.Serialization.XmlLayoutSerializer(dock);
            layoutSerializer.Serialize("layout.cfg");
        }
    }
    public class ConnectAll : ICommand
    {
        public bool CanExecute(object parameter)
        {
            return true;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public void Execute(object VM)
        {

            foreach (var ctrl in ((MainViewModel)VM).Controllers)
            {
                ctrl.Connect();
            }
        }
    }
    public class RefreshParameters : ICommand
    {
        public bool CanExecute(object parameter)
        {
            return true;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public void Execute(object parameter)
        {
            MainViewModel mainVM = MainViewModel.Instance;
            if (parameter == null)
            {
                foreach (var symbol in mainVM.SymbolInActions)
                {
                    symbol.IsDirty = true;
                }
            }
            else
            {
                ((dynamic)parameter).IsDirty = true;
            }
        }
    }
    public class ConnectByContextMenu : ICommand
    {
        public bool CanExecute(object parameter)
        {
            return true;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public void Execute(object DC)
        {
            //MenuItem mi = sender as MenuItem;
            IController ctrl = DC as IController;
            if (ctrl.IsConnected)
                ctrl.DisconnectByManual();
            else
                ctrl.Connect();
        }
    }
    public class DisconnectAll : ICommand
    {
        public bool CanExecute(object parameter)
        {
            return true;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public void Execute(object VM)
        {
            foreach (var ctrl in ((MainViewModel)VM).Controllers)
            {
                ctrl.DisconnectByManual();
            }
        }
    }
    public class CancelPendingOrder : ICommand
    {
        public bool CanExecute(object parameter)
        {
            MainViewModel mainVM = MainViewModel.Instance;
            DisplayedOrder order = mainVM.SelectedPendingOrder;

            if (mainVM.OrderInfoList.ContainsKey(order.OrderId))
            {
                OrderInfo oi = mainVM.OrderInfoList[order.OrderId];
                if (oi.Filled < oi.PosSize)
                    return true;
                else
                    return false;
            }
            else
                return false;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public async void Execute(object VM)
        {
            MainViewModel mainVM = MainViewModel.Instance;
            DisplayedOrder order = mainVM.SelectedPendingOrder;
            MessageBoxResult msgResult = MessageBoxResult.No;
            if (mainVM.OrderInfoList.ContainsKey(order.OrderId))
                msgResult = MessageBox.Show("Are you sure to close selected pending order?",
                                          "Warning",
                                          MessageBoxButton.YesNo,
                                          MessageBoxImage.Warning, MessageBoxResult.No);
            else
            {
                MessageBox.Show("This pending order is not generated by this program, failed to cancel.",
                                          "Error",
                                          MessageBoxButton.OK,
                                          MessageBoxImage.Error);
            }                

            if (msgResult == MessageBoxResult.No)
            {
                return;
            }
            else
            {
                OrderInfo oi = mainVM.OrderInfoList[order.OrderId];
                IController controller = oi.Account.Controller;
                if (oi.Filled == oi.PosSize) return;
                try
                {
                    bool result = await controller.CancelOrderAsync(oi.OrderId);
                    if (!result)
                        MessageBox.Show("Cancel order failed", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (Exception ex)
                {
                    GlobalExceptionHandler.HandleException(null, ex, null, "Exception occurred during cancelling order");
                }
                
            }
        }
    }
    public class CloseSymbolOpenOrders : ICommand
    {
        public bool CanExecute(object parameter)
        {
            MainViewModel mainVM = MainViewModel.Instance;
            SymbolInMkt symbol = mainVM.SelectedPortfolio;
            if (symbol.Position != 0)
                return true;
            else
                return false;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public void Execute(object VM)
        {
            MessageBoxResult msgResult = MessageBox.Show("Are you sure to close all open positions for selected symbols?",
                                          "Warning",
                                          MessageBoxButton.YesNo,
                                          MessageBoxImage.Warning, MessageBoxResult.No);
            if (msgResult == MessageBoxResult.No)
            {
                return;
            }
            else
            {
                MainViewModel mainVM = MainViewModel.Instance;
                SymbolInMkt symbol = mainVM.SelectedPortfolio;
                IController controller = mainVM.Controllers.FirstOrDefault(x => x.Accounts.FirstOrDefault(y => y.Name == symbol.Account) != null);
                AccountInfo accountInfo = controller.Accounts.FirstOrDefault(x => x.Name == symbol.Account);
                BaseOrderType orderType = (BaseOrderType)Helper.GetInstance(symbol.Vendor + "MarketOrder");
                OrderAction orderAction = OrderAction.Buy;
                if (symbol.Position == 0) return;
                if (symbol.Position > 0) orderAction = OrderAction.Sell;
                if (symbol.Position < 0) orderAction = OrderAction.Cover;
                int bn = OrderManager.BatchNo;
                controller.PlaceOrder(accountInfo, null, orderType, orderAction, 0, bn, Math.Abs(symbol.Position), symbol.Contract);
                
                foreach (SymbolInAction contract in mainVM.SymbolInActions)
                {
                    SymbolDefinition sd = contract.SymbolDefinition.FirstOrDefault(x => x.Controller.Vendor == controller.Vendor && x.Contract.ConId == symbol.Contract.ConId);
                    if (sd != null)
                    {
                        foreach (Script script in contract.Scripts)
                        {
                            foreach (var status in script.AccountStat)
                            {
                                status.Value.LongPosition = 0;
                                status.Value.ShortPosition = 0;
                            }
                            foreach (Strategy strategy in script.Strategies)
                            {
                                foreach (var status in strategy.AccountStat)
                                {
                                    status.Value.LongPosition = 0;
                                    status.Value.ShortPosition = 0;
                                }
                            }
                        }
                    }                    
                }
            }
        }
    }
    public class CloseAllOpenOrders : ICommand
    {
        public bool CanExecute(object parameter)
        {
            return true;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public void Execute(object VM)
        {
            MessageBoxResult msgResult = MessageBox.Show("Are you sure to close all open positions?",
                                          "Warning",
                                          MessageBoxButton.YesNo,
                                          MessageBoxImage.Warning, MessageBoxResult.No);
            if (msgResult == MessageBoxResult.No)
            {
                return;
            }
            else
            {
                MainViewModel mainVM = MainViewModel.Instance;
                foreach (SymbolInMkt symbol in mainVM.Portfolio)
                {
                    IController controller = mainVM.Controllers.FirstOrDefault(x => x.Accounts.FirstOrDefault(y => y.Name == symbol.Account) != null);
                    AccountInfo accountInfo = controller.Accounts.FirstOrDefault(x => x.Name == symbol.Account);
                    BaseOrderType orderType = (BaseOrderType)Helper.GetInstance(symbol.Vendor + "MarketOrder");
                    OrderAction orderAction = OrderAction.Buy;
                    if (symbol.Position == 0) continue;
                    if (symbol.Position > 0) orderAction = OrderAction.Sell;
                    if (symbol.Position < 0) orderAction = OrderAction.Cover;
                    int bn = OrderManager.BatchNo;
                    controller.PlaceOrder(accountInfo, null, orderType, orderAction, 0, bn, Math.Abs(symbol.Position), symbol.Contract);
                }
                foreach (SymbolInAction symbol in mainVM.SymbolInActions)
                {
                    foreach (Script script in symbol.Scripts)
                    {
                        foreach (var status in script.AccountStat)
                        {
                            status.Value.LongPosition = 0;
                            status.Value.ShortPosition = 0;
                        }
                        foreach (Strategy strategy in script.Strategies)
                        {
                            foreach (var status in strategy.AccountStat)
                            {
                                status.Value.LongPosition = 0;
                                status.Value.ShortPosition = 0;
                            }
                        }
                    }
                }
            }
        }
    }
    public class CloseCurrentOpenOrders : ICommand
    {
        public bool CanExecute(object parameter)
        {
            return true;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public void Execute(object parameter)
        {
            MessageBoxResult msgResult = MessageBox.Show("Are you sure to close current open positions?",
                                          "Warning",
                                          MessageBoxButton.YesNo,
                                          MessageBoxImage.Warning, MessageBoxResult.No);
            if (msgResult == MessageBoxResult.No)
            {
                return;
            }
            else
            {
                if (parameter != null)
                {
                    MethodInfo mi = parameter.GetType().GetMethod("CloseAllPositions");
                    if (mi != null)
                        mi.Invoke(parameter, null);
                }
                else
                {
                    MainViewModel mainVM = MainViewModel.Instance;
                    foreach (SymbolInAction symbol in mainVM.SymbolInActions)
                    {
                        symbol.CloseAllPositions();
                    }
                }
            }
        }
    }
    public class DisplayConfigDialog : ICommand
    {
        public bool CanExecute(object parameter)
        {
            return true;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public void Execute(object VM)
        {
            MainWindow mw = ((MainWindow)VM);
            Setting s = new Setting(mw.MainVM.Controllers.ToList());
            s.Owner = mw;
            bool? dr = s.ShowDialog();
            if ((bool)dr)
            {
                mw.ReadSettings();
            }
        }
    }

    public class Export : ICommand
    {
        public bool CanExecute(object parameter)
        {
            MainWindow mainWin = parameter as MainWindow;
            if (mainWin != null && mainWin.ActivateListView != null)
                return true;
            else
                return false;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public void Execute(object parameter)
        {
            MainWindow mainWin = parameter as MainWindow;
            
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.FileName = "Untitled"; // Default file name
            dlg.DefaultExt = ".csv"; // Default file extension
            dlg.Filter = "Comma Separated Values File (.csv)|*.csv"; // Filter files by extension
            
            // Show save file dialog box
            Nullable<bool> result = dlg.ShowDialog();
            // Process save file dialog box results
            if (result == true)
            {
                string filename = dlg.FileName;
                var lv = mainWin.ActivateListView;
                ListViewHelper.ListViewToCSV(lv, filename);
            }
        }
    }

    public class ClearListView : ICommand
    {
        public bool CanExecute(object parameter)
        {
            MainWindow mainWin = parameter as MainWindow;
            if (mainWin != null && mainWin.ActivateListView != null)
                return true;
            else
                return false;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public void Execute(object parameter)
        {
            MainWindow mainWin = parameter as MainWindow;

            MessageBoxResult result = MessageBox.Show("Are your sure to delete all content?",
                "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Information, MessageBoxResult.No);

            if (result == MessageBoxResult.Yes)
            {                
                var lv = mainWin.ActivateListView;
                if (lv?.ItemsSource != null)
                {
                    MethodInfo mi = lv.ItemsSource.GetType().GetMethod("Clear");
                    if (mi != null)
                        mi.Invoke(lv.ItemsSource, null);
                }
            }
        }
    }
}
