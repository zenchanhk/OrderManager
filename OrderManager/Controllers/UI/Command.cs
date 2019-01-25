using Dragablz.Savablz;
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

namespace AmiBroker.Controllers
{
    public class Commands
    {
        public ICommand SaveLayout { get; set; } = new SaveLayout();
        public ICommand RestoreLayout { get; set; } = new RestoreLayout();
        public ICommand ConnectAll { get; set; } = new ConnectAll();
        public ICommand DisconnectAll { get; set; } = new DisconnectAll();
        public ICommand CloseAllOpenOrders { get; set; } = new CloseAllOpenOrders();
        public ICommand ShowConfigDialog { get; set; } = new DisplayConfigDialog();
        public ICommand ConnectByContextMenu { get; set; } = new ConnectByContextMenu();
        public ICommand RefreshStrategyParameters { get; set; } = new RefreshStrategyParameters();
        public ICommand EnableScriptExecution { get; set; } = new EnableScriptExecution();
        public ICommand ApplySettingsToStrategy { get; set; } = new ApplySettingsToStrategy();
        public ICommand ClearSettings { get; set; } = new ClearSettings();
        public ICommand SaveAsTemplate { get; set; } = new SaveAsTemplate();
        public ICommand OpenTemplate { get; set; } = new OpenTemplate();
        public ICommand OutSaveAsTemplate { get; set; } = new OutSaveAsTemplate();
        public ICommand OutOpenTemplate { get; set; } = new OutOpenTemplate();
        public ICommand DeleteTemplate { get; set; } = new DeleteTemplate();
        public ICommand RenameTemplate { get; set; } = new RenameTemplate();
        public ICommand EditTemplateName { get; set; } = new EditTemplateName();
        public ICommand EditTemplateOnSite { get; set; } = new EditTemplateOnSite();
        public ICommand SaveTemplateOnSite { get; set; } = new SaveTemplateOnSite();
        public ICommand CancelEditTemplateOnSite { get; set; } = new CancelEditTemplateOnSite();
        public ICommand CopyOrderSetup { get; set; } = new CopyOrderSetup();
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
                        src_item.CopyTo(dest_item);
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
                                          MessageBoxImage.Warning);
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
                                          MessageBoxImage.Question);
                if (msgResult == MessageBoxResult.No)
                {
                    return;
                }
                else
                // over-writing
                {
                    result.ContentAsString = JsonConvert.SerializeObject(template.SaveItem, JSONConstants.saveSerializerSettings);
                    result.ModifiedDate = DateTime.Now;
                }
            }
            else
            {
                template.TemplateList.Add(new OptionTemplate
                {
                    Name = template.TemplateName,
                    TypeName = template.SaveItem.GetType().Name,
                    ContentAsString = JsonConvert.SerializeObject(template.SaveItem, JSONConstants.saveSerializerSettings),
                    ModifiedDate = DateTime.Now
                });
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
                                          MessageBoxImage.Question);
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
                                          MessageBoxImage.Question);
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
            MainWindow _mw = mw as MainWindow;
            // Saves the layout and exit
            _mw.calledOnce();
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
            System.Windows.Threading.Dispatcher.FromThread(OrderManager.UIThread).Invoke(() =>
            {
                var windowsState =
                WindowsStateSaver.GetWindowsState<TabContentModel, TabContentViewModel>(vm =>
                    new TabContentModel(vm.Header));

                if (windowsState.First().Child == null)
                {
                    // All tabs in the main window have been closed.
                    // A choice have been made for this sample app : When all tabs in the main window have been closed,
                    // resets the settings so that a fresh window is created next time.
                    // Feel free to implement that the way you want here
                    Properties.Settings.Default.Layout = null;
                }
                else
                {
                    //var jsonResolver = new IgnorableSerializerContractResolver(true);
                    //jsonResolver.Ignore(typeof(UserControl));
                    //var jsonSettings = new JsonSerializerSettings() { ReferenceLoopHandling = ReferenceLoopHandling.Ignore, ContractResolver = jsonResolver };
                    Properties.Settings.Default.Layout = JsonConvert.SerializeObject(windowsState, Formatting.None); //, jsonSettings);
                }

                Properties.Settings.Default.Save();
            });
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
    public class RefreshStrategyParameters : ICommand
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
                ctrl.Disconnect();
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
                ctrl.Disconnect();
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
            //Your Code
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
            return true;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public void Execute(object activeTab)
        {
            //Your Code
        }
    }

}
