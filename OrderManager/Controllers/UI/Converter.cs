using System;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Globalization;
using System.Windows.Media;
using System.Windows.Controls;
using FontAwesome.Sharp;
using System.Reflection;
using AmiBroker.OrderManager;
using System.Collections.ObjectModel;
using System.Collections.Generic;

namespace AmiBroker.Controllers
{    
    public class Util
    {
        internal static readonly FontFamily mdIcons =
            Assembly.GetExecutingAssembly().GetFont("Controllers/fonts", "Material Design Icons");

        public class Color
        {
            public static System.Windows.Media.Color Black { get; private set; } = Util.ConvertStringToColor("#FF000000");
            public static System.Windows.Media.Color Red { get; private set; } = Util.ConvertStringToColor("#FFFF0000");
            public static System.Windows.Media.Color Green { get; private set; } = Util.ConvertStringToColor("#FF00FF00");
            public static System.Windows.Media.Color Yellow { get; private set; } = Util.ConvertStringToColor("#FFFFFF00");
            public static System.Windows.Media.Color Orange { get; private set; } = Util.ConvertStringToColor("#FFFF8C00");
            public static System.Windows.Media.Color Indigo { get; private set; } = Util.ConvertStringToColor("#FF4B0082");
            public static System.Windows.Media.Color Transparent { get; private set; } = Util.ConvertStringToColor("#00FFFFFF");
            public static System.Windows.Media.Color AliceBlue { get; private set; } = Util.ConvertStringToColor("#FF87CEFA");
            public static System.Windows.Media.Color Purple { get; private set; } = Util.ConvertStringToColor("#FF800080");
            public static System.Windows.Media.Color DimGray { get; private set; } = Util.ConvertStringToColor("#FF696969");
            public static System.Windows.Media.Color Gray { get; private set; } = Util.ConvertStringToColor("#FF808080");
        }
        public static Image MaterialIconToImage(MaterialIcons icon, System.Windows.Media.Color? color = null, int size = 16)
        {
            Image i = new Image();
            SolidColorBrush brush = color != null ? new SolidColorBrush((System.Windows.Media.Color)color) :
                new SolidColorBrush(Util.Color.DimGray);
            i.Source = Util.mdIcons.ToImageSource<MaterialIcons>(icon, brush, size);
            return i;
        }
        public static System.Windows.Media.Color ConvertStringToColor(String hex)
        {
            //remove the # at the front
            hex = hex.Replace("#", "");

            byte a = 255;
            byte r = 255;
            byte g = 255;
            byte b = 255;

            int start = 0;

            //handle ARGB strings (8 characters long)
            if (hex.Length == 8)
            {
                a = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
                start = 2;
            }

            //convert RGB characters to bytes
            r = byte.Parse(hex.Substring(start, 2), System.Globalization.NumberStyles.HexNumber);
            g = byte.Parse(hex.Substring(start + 2, 2), System.Globalization.NumberStyles.HexNumber);
            b = byte.Parse(hex.Substring(start + 4, 2), System.Globalization.NumberStyles.HexNumber);

            return System.Windows.Media.Color.FromArgb(a, r, g, b);
        }
    }
    // To be used by NumericUpDown
    class FontSizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double v = (double)value;
            return v * 0.5;

            //double v = (double)value;
            //v = v - 7;
            //if (v > 0)
            //    return v;
            //else
            //    return v;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    class AccountStatusToVisConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null)
            {
                string param = (string)parameter;
                if (value.GetType() == typeof(AccountStatus))
                {
                    AccountStatus status = (AccountStatus)value;

                    if (param == "stopprofit" && (status & AccountStatus.APSLongActivated) == 0
                        && (status & AccountStatus.APSShortActivated) == 0)
                        return Visibility.Collapsed;
                    else if (param == "stoploss" && (status & AccountStatus.StoplossLongActivated) == 0
                        && (status & AccountStatus.StoplossShortActivated) == 0)
                        return Visibility.Collapsed;
                    else if (param == "forceexit" && (status & AccountStatus.FinalForceExitLongActivated) == 0
                        && (status & AccountStatus.FinalForceExitShortActivated) == 0
                        && (status & AccountStatus.PreForceExitLongActivated) == 0
                        && (status & AccountStatus.PreForceExitShortActivated) == 0)
                        return Visibility.Collapsed;
                    else if (param == "inpending" && (status & AccountStatus.BuyPending) == 0
                        && (status & AccountStatus.ShortPending) == 0)
                        return Visibility.Collapsed;
                    else if (param == "outpending" && (status & AccountStatus.SellPending) == 0
                        && (status & AccountStatus.CoverPending) == 0)
                        return Visibility.Collapsed;
                    else if (param == "long" && (status & AccountStatus.BuyPending) == 0)
                        return Visibility.Collapsed;
                    else if (param == "short" && (status & AccountStatus.ShortPending) == 0)
                        return Visibility.Collapsed;
                    else if (param == "sell" && (status & AccountStatus.SellPending) == 0)
                        return Visibility.Collapsed;
                    else if (param == "cover" && (status & AccountStatus.CoverPending) == 0)
                        return Visibility.Collapsed;
                    else if (param == "cover" && (status & AccountStatus.CoverPending) == 0)
                        return Visibility.Collapsed;
                    else if (param == "profit" && (status & AccountStatus.Long) == 0 && (status & AccountStatus.Short) == 0)
                        return Visibility.Collapsed;
                    else if (param == "longprofit" && (status & AccountStatus.Long) == 0)
                        return Visibility.Collapsed;
                    else if (param == "shortprofit" && (status & AccountStatus.Short) == 0)
                        return Visibility.Collapsed;
                    else
                        return Visibility.Visible;
                }
                else
                    return Visibility.Collapsed;
            }
            else
                return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    
    class AccountStatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value.GetType() == typeof(BaseStat))
                return false;
            else
                return true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    class SelectedItemToFocusableConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value.GetType() == typeof(BaseStat))
                return false;
            else
            return true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    class ATAflToNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            ATAfl v = (ATAfl)value;
            return v.Name;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    class StoplossComboToVisConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int v = (int)value;
            int p = int.Parse((string)parameter);
            if (v == 0)
                if (p == 0)
                    return Visibility.Visible;
                else
                    return Visibility.Collapsed;
            else if (v == 1)
                if (p == 1)
                    return Visibility.Visible;
                else
                    return Visibility.Collapsed;
            else
                return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    class SelectedItemToDataContextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null && value.GetType().Name == (string)parameter)
                return value;
            else
                return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    class BoolRevConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null)
                return !(bool)value;
            else
                return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    class SelectedItemToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {            
            if (value != null)
            {
                MainWindow window = OrderManager.MainWin;
                if (value.GetType() == typeof(Script))
                    return window.FindResource("scriptDrawingImage");
                else if (value.GetType() == typeof(SymbolInAction))
                    return window.FindResource("stockDrawingImage");
                else if (value.GetType() == typeof(Strategy))
                    return window.FindResource("StrategyDrawingImage");
                else
                    return null;
            }            
            else
                return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    class SelectedItemToIsEnabledConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null)
            {
                return true;
            }
            else
                return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    class DataContextToVisConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    class PricesToProfitConverter : IMultiValueConverter
    {
        public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture)
        {
            // 0 - AvgPrice
            // 1 - CurPrice
            // 2 - BaseStat
            // parameter - long or short
            if (value[0] != DependencyProperty.UnsetValue && value[1] != DependencyProperty.UnsetValue)
            {
                if (value[0] != null && value[1] != null)
                {
                    double val0 = (double)value[0];
                    double val1 = (double)value[1];
                    if (val1 > 0 && val0 > 0)
                    {
                        double diff = val1 - val0;
                        BaseStat stat = value[2] as BaseStat;
                        if (stat?.SSBase != null)
                        {
                            if ((string)parameter == "long")
                                return Math.Round(stat.SSBase.Symbol.RoundLotSize * stat.LongPosition * diff, 2).ToString();
                            else
                                return Math.Round(stat.SSBase.Symbol.RoundLotSize * stat.ShortPosition * diff * -1, 2).ToString();
                        }
                    }
                    else
                        return "";
                }
            }
            return "";
        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    class DummyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return false;

            //double v = (double)value;
            //v = v - 7;
            //if (v > 0)
            //    return v;
            //else
            //    return v;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    // some of Strategy settings depends on Script's settings
    class ParentValueToEnabledConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            SSBase b = value as SSBase;
            string[] ps = parameter.ToString().Split(new char[] { '$' });
            if (b != null && b.GetType() == typeof(Strategy))
            {
                Strategy s = b as Strategy;
                PropertyInfo pi = s.Script.GetType().GetProperty(ps[0]);
                if (pi != null)
                {
                    bool val = (bool)pi.GetValue(s.Script);
                    if (val)
                        return true;
                    else
                    {
                        return false;
                    }                        
                }
            }
            return true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    // tree selected item
    class SelectedItemToMultiComboItemsourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return null;

            Type t = value.GetType();
            if (t == typeof(SymbolInAction))
                return ((SymbolInAction)value).AccountCandidates;
            else if (t == typeof(Script) || t == typeof(Strategy))
                return ((dynamic)value).Symbol.AccountCandidates;
            else
                return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    class StrategyFullNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Strategy strategy = value as Strategy;
            return strategy.Symbol.TimeFrame + "min." + strategy.Script.Name + "." + strategy.Name;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    class ParentValueToMaxValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            SSBase b = value as SSBase;
            string[] ps = parameter.ToString().Split(new char[] { '$' });
            double orgMax = 2000;
            double.TryParse(ps[1], out orgMax);
            if (b != null && b.GetType() == typeof(Strategy))
            {
                Strategy s = b as Strategy;
                PropertyInfo pi = s.Script.GetType().GetProperty(ps[0]);
                if (pi != null)
                {
                    double val = (dynamic)pi.GetValue(s.Script);
                    if (val > 0)
                        return val;
                    else
                        return orgMax;
                }
                else
                {
                    int i = 0;
                }
            }
            return orgMax;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    class ParametersHandlerConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            List<object> list = new List<object>();
            list.Add(value);
            list.Add(parameter);
            return list;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    /// <summary>
    /// This converter will be triggered whenever SelectedItem of combobox (selecting order types) being changed
    /// once triggered, find the old item to remove and add the new item to list of corresponding list of order types
    /// </summary>
    class RealOrderTypeConverter : IMultiValueConverter
    {
        public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture)
        {   
            // 0 - SelectedItem (ComboBox in selecting order type)
            // 1 - SelectedItem.<OrderTypes>
            // 2 - DataContext to get real TreeView_SelectedItem
            if (value[0] != DependencyProperty.UnsetValue && value[1] != DependencyProperty.UnsetValue)
            {                
                if (value[0] != null && value[1] != null)
                {
                    MainViewModel vm = value[2] as MainViewModel;
                    object si = vm.SelectedItem;
                    PropertyInfo pi = si.GetType().GetProperty(parameter.ToString() + "OrderTypes");
                    if (pi == null) return null;    // in case of SymbolInAction during switching between tree view item
                    ObservableCollection<BaseOrderType> orderTypes = (ObservableCollection<BaseOrderType>)pi.GetValue(si);
                    Type t = value[0].GetType().BaseType; // get base type
                    BaseOrderType ot = orderTypes.FirstOrDefault(x => x.GetType().IsSubclassOf(t)); // get old item
                    // remove old item if not equal
                    if (ot != null) 
                    {
                        if (ot.GetType() != value[0].GetType())
                            orderTypes.Remove(ot);
                        else
                            return ot;
                    }                        
                    // add new item
                    if (ot == null || (ot != null && ot.GetType() != value[0].GetType()))
                    {
                        BaseOrderType newItem = ((BaseOrderType)value[0]).Clone();
                        if (si.GetType() == typeof(Script))
                            newItem.TimeZone = ((Script)si).Symbol.TimeZone;
                        if (si.GetType() == typeof(Strategy))
                            newItem.TimeZone = ((Strategy)si).Script.Symbol.TimeZone;
                        orderTypes.Add(newItem);
                        return newItem;
                    }
                }
            }
            return null;
        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    class SelectedOrdersToSelectedItemConverter : IMultiValueConverter
    {
        public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture)
        {
            /*
            ScriptTabView stv = value as ScriptTabView;
            string[] param = ((string)parameter).Split(new char[] { '$' });
            string t = "AmiBroker.OrderManager." + param[2];
            ObservableCollection<BaseOrderType> orderTypes = null;
            if (param[0] == "Long")
                orderTypes = ((dynamic)((MainViewModel)stv.DataContext).SelectedItem).LongOrderTypes;
            else
                orderTypes = ((dynamic)((MainViewModel)stv.DataContext).SelectedItem).ShortOrderTypes;
            BaseOrderType ot = orderTypes.FirstOrDefault(x => x.GetType().IsSubclassOf(Type.GetType(t)));
            */
            if (value[0] != DependencyProperty.UnsetValue)
            {
                ObservableCollection<BaseOrderType> orderTypes = (ObservableCollection<BaseOrderType>)value[0];
                if (value[1] != null && ((dynamic)value[1]).Count > 0)
                {
                    //Type t = value[1].GetType().GetGenericArguments().Single(); return BaseOrderType only
                    Type t = ((dynamic)value[1])[0].GetType().BaseType; // get base type
                    BaseOrderType ot = orderTypes.FirstOrDefault(x => x.GetType().IsSubclassOf(t));
                    if (ot != null)
                        return ((List<BaseOrderType>)value[1]).FirstOrDefault(x => x.GetType() == ot.GetType());
                }
            }            
            return null;
        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    class SelectedIndexToCheckBoxValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null && (int)value > 0)
                return true;
            else
                return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((bool)value)
            {
                return 10;
            }
            else
                return 0;
        }
    }
    class SelectedIndexToVisConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((int)value <= 0)
                return Visibility.Visible;
            else
                return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    class PositionToEnabledConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((double)value <= 0)
                return false;
            else
                return true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    class SelectedIndexToOptionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int p = 0;
            int.TryParse(parameter.ToString(), out p);
            return (int)value == p;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((bool)value)
            {
                int p = 0;
                int.TryParse(parameter.ToString(), out p);
                return p;
            }
            else
                return -1;
        }
    }
    class SelectTemplateToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            SaveLoadTemplate template = (SaveLoadTemplate)value;
            if (template != null)
            {
                return template.SelectedDirectory.Icon;
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    // long UI visibility
    class ActionTypeToLongUIVisConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null && value.GetType() == typeof(Strategy))
            {
                ActionType at = ((Strategy)value).ActionType;
                if (at == ActionType.Long || at == ActionType.LongAndShort)
                    return Visibility.Collapsed;
            }
            if (value != null && value.GetType() == typeof(Script))
                return Visibility.Collapsed;
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    class ActionTypeToShortUIVisConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null && value.GetType() == typeof(Strategy))
            {
                ActionType at = ((Strategy)value).ActionType;
                if (at == ActionType.Short || at == ActionType.LongAndShort)
                    return Visibility.Collapsed;
            }
            if (value != null && value.GetType() == typeof(Script))
                return Visibility.Collapsed;
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    class ModeToSelectorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            ControlLib.Modes mode = (ControlLib.Modes)value;
            if (mode == ControlLib.Modes.Edit)
            {
                return new ControlLib.EditSelector();
            }
            else
                return new ControlLib.ReadSelector();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class ItemToIconConverter : IMultiValueConverter
    {
        public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture)
        {
            ControlLib.TreeNode item = value[0] as ControlLib.TreeNode;
            ControlLib.ObjectInTreeView oit = value[1] as ControlLib.ObjectInTreeView;
            DrawingImage drawingImage = null;
            if (item.Name.Contains("(Root)"))
            {
                if (item.Children != null && item.Children.Count > 0 && 
                    item.Children.FirstOrDefault(x => x.Name == "ActionType") != null )
                    return oit.FindResource("StrategyDrawingImage");
                else if (item.Children != null && item.Children.Count > 0 &&
                    item.Children.FirstOrDefault(x => x.Name == "Strategies") != null)
                    return oit.FindResource("scriptDrawingImage");
                else if (item.Children != null && item.Children.Count > 0 &&
                    item.Children.FirstOrDefault(x => x.Name == "Scripts") != null)
                    return oit.FindResource("stockDrawingImage");
                else
                    return oit.FindResource("rootDrawingImage");
            }
            if (item.Type == typeof(string)) return oit.FindResource("StringDrawingImage");
            if (item.Type == typeof(int) || item.Type == typeof(Nullable<int>)) return oit.FindResource("IntegerDrawingImage");
            if (item.Type == typeof(float) || item.Type == typeof(Nullable<float>) 
                || item.Type == typeof(decimal) || item.Type == typeof(Nullable<decimal>) 
                || item.Type == typeof(double) || item.Type == typeof(Nullable<decimal>))
                return oit.FindResource("FloatDrawingImage");
            if (item.Type == typeof(bool) || item.Type == typeof(Nullable<bool>)) return oit.FindResource("BoolDrawingImage");
            if (item.Type == typeof(DateTime) || item.Type == typeof(Nullable<DateTime>)) return oit.FindResource("DateDrawingImage");
            if (item.Type == typeof(System.Collections.ArrayList))
            {
                if (item.Name == "Scripts")
                    return oit.FindResource("scriptDrawingImage");
                else if (item.Name == "Strategies")
                    return oit.FindResource("StrategyDrawingImage");
                else if (item.Name.ToLower().Contains("accounts"))
                    return oit.FindResource("peoplesDrawingImage");
                else 
                    return oit.FindResource("listDrawingImage");
            }
            if (item.GetType() == typeof(ControlLib.TreeNode))
            {
                if (item.Children.FirstOrDefault(x => x.Name == "Strategies") != null)
                    return oit.FindResource("scriptDrawingImage");
                if (item.Children.FirstOrDefault(x => x.Name == "ActionType") != null)
                    return oit.FindResource("StrategyDrawingImage");
                if (item.Children.FirstOrDefault(x => x.Name == "Vendor") != null)
                    return oit.FindResource("definitionDrawingImage");
                if (item.Children.FirstOrDefault(x => x.Name == "DisplayName") != null)
                    return oit.FindResource("vendorDrawingImage");
                if (item.Children.FirstOrDefault(x => x.Name == "Slippage") != null ||
                    item.Children.FirstOrDefault(x => x.Name == "Slippages") != null)
                    return oit.FindResource("OrderTypeDrawingImage");
                if (item.Children.FirstOrDefault(x => x.Name == "Controller") != null)
                    return oit.FindResource("accountDrawingImage");
                if (item.Children.FirstOrDefault(x => x.Name == "ExactTime") != null)
                    return oit.FindResource("timerDrawingImage");
                if (item.Children.FirstOrDefault(x => x.Name == "UtcOffset") != null)
                    return oit.FindResource("timezoneDrawingImage");
                if (item.Children.FirstOrDefault(x => x.Name == "AccountStatus") != null)
                    return oit.FindResource("accountDrawingImage");
                if (item.Children.FirstOrDefault(x => x.Name == "ProfitTarget") != null)
                    return oit.FindResource("APSDrawingImage");
                if (item.Children.FirstOrDefault(x => x.Name == "StopBreakTime") != null)
                    return oit.FindResource("actionAfterFailDrawingImage");
            }
            if (item.Type != null && item.Type.Name.ToLower().Contains("dictionary"))
            {
                if (item.Children.FirstOrDefault(x => x.Name == "DisplayName") != null)
                    return oit.FindResource("vendorDrawingImage");
                if (item.Name == "AccountStat")
                    return oit.FindResource("statusDrawingImage");
                if (item.Name.ToLower().Contains("actionafter"))
                    return oit.FindResource("actionAfterFailDrawingImage");
            }
                return drawingImage;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class IsEditingToVisConverter : IMultiValueConverter
    {
        public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture)
        {
            SaveLoadTemplate stv = value[0] as SaveLoadTemplate;
            if (stv.IsNameEditing && stv.SelectedTemplate == (value[1] as OptionTemplate))
            {
                return Visibility.Visible;
            }
            else
                return Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class EnumConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter,
                              System.Globalization.CultureInfo culture)
        {
            Enum enumValue = default(Enum);
            if (parameter is Type)
            {
                enumValue = (Enum)Enum.Parse((Type)parameter, value.ToString());
            }
            return enumValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter,
                                  System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class ModesConverter : IMultiValueConverter
    {
        public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture)
        {
            ControlLib.Modes mode = ControlLib.Modes.Read;
            bool isEditing = (bool)value[0];
            EditEndingAction action = (EditEndingAction)value[1];
            if (isEditing)
            {
                if (action == EditEndingAction.Save)
                    mode = ControlLib.Modes.Save;
                if (action == EditEndingAction.Cancel)
                    mode = ControlLib.Modes.Read;
                if (action == EditEndingAction.Netural)
                    mode = ControlLib.Modes.Edit;
            }
            return mode;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class ControllerToAccountDetailConverter : IMultiValueConverter
    {
        public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture)
        {
            string text = string.Empty;
            IController controller = (IController)value[0];
            if (controller != null)
            {
                foreach (AccountInfo accountInfo in controller.Accounts)
                {
                    AccountTag tag = accountInfo.Properties.FirstOrDefault(x => x.Tag == "Value");
                    text += text ?? " | ";
                    text += "Account: " + accountInfo.Name + " Value: " + tag.Currency + tag.Value;
                }
            }
            return text;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }  
    public class SelectedToCollectionConverter11 : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            ObservableCollection<IController> appliedAccounts = value as ObservableCollection<IController>;
            if (parameter !=null && ((dynamic)parameter)[0] != null)
            {
                IController controller = ((dynamic)parameter)[0] as IController;
                ScriptTabView stv = ((dynamic)parameter)[1] as ScriptTabView;
                return appliedAccounts.Contains(controller);
            }
            return false;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            ObservableCollection<IController> appliedAccounts = ((dynamic)parameter)[0] as ObservableCollection<IController>;
            IController ic = ((dynamic)parameter)[1] as IController;
            if ((bool)value)
            {
                appliedAccounts.Add(ic);
            }else
            {
                appliedAccounts.Remove(ic);
            }
            return true;
        }
    }
    public class MCSelectedToCollectionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            PropertyInfo pi = value.GetType().GetProperty(((dynamic)parameter)[1]);
            return pi.GetValue(value);
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            object si = ((dynamic)parameter)[0].SelectedItem;
            PropertyInfo pi = si.GetType().GetProperty(((dynamic)parameter)[1]);
            pi.SetValue(si, value);
            return si;
        }
    }
    public class CombinedConverter : IMultiValueConverter
    {
        public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture)
        {            
            return value;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class SelectedToCollectionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            //ObservableCollection<IController> appliedAccounts = ((dynamic)parameter)[0] as ObservableCollection<IController>;
            object si = MainViewModel.Instance.SelectedItem;
            if (si == null) return false;

            if (si.GetType() == typeof(SymbolInAction))
            {
                ObservableCollection<IController> appliedAccounts = ((SymbolInAction)si).AppliedControllers;
                IController ic = ((dynamic)parameter)[1] as IController;
                return appliedAccounts.Contains(ic);
            }
            return false;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            //ObservableCollection<IController> appliedAccounts = ((dynamic)parameter)[0] as ObservableCollection<IController>;
            ObservableCollection<IController> appliedAccounts = ((dynamic)MainViewModel.Instance.SelectedItem).AppliedControllers;
            IController ic = ((dynamic)parameter)[1] as IController;

            if ((bool)value)
            {
                appliedAccounts.Add(ic);
            }
            else
            {
                appliedAccounts.Remove(ic);
            }
            return true;
        }
    }
    public class EnabledToOpacityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? 1 : 0.4;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class SelectedTemplateToVisConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null ? Visibility.Collapsed : Visibility.Visible;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class ActionTypeToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string text = string.Empty;
            switch ((ActionType)value)
            {
                case ActionType.Long:
                    text = "Long";
                    break;
                case ActionType.Short:
                    text = "Short";
                    break;
                case ActionType.LongAndShort:
                    text = "Long and Short";
                    break;
            }
            return text;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class DayTradeModeToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string text = string.Empty;
            if ((bool)value)
                text = "DayTrade Mode";
            else
                text = "Non-DayTrade Mode";
            return text;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class EnabledToVisConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? Visibility.Collapsed : Visibility.Visible;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class EnabledToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ?
                //new Image { Source = Util.mdIcons.ToImageSource<MaterialIcons>(MaterialIcons.Cancel, new SolidColorBrush(Util.Color.Red), 16) } :
                //new Image { Source = Util.mdIcons.ToImageSource<MaterialIcons>(MaterialIcons.CheckCircleOutline, new SolidColorBrush(Util.Color.Green), 16) };
                Util.mdIcons.ToImageSource<MaterialIcons>(MaterialIcons.Cancel, new SolidColorBrush(Util.Color.Red), 16) :
                Util.mdIcons.ToImageSource<MaterialIcons>(MaterialIcons.CheckCircleOutline, new SolidColorBrush(Util.Color.Green), 16);
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class EnabledToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? new SolidColorBrush(Util.Color.Black) : new SolidColorBrush(Util.Color.Red);
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class EnabledToMenuTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? "Disable Execution" : "Enable Execution";
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class TypeToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {            
            return value != null ? value.GetType().Name : "null";
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class TypeToPGObjectConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value.GetType().Name == "Script" ? null : value;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class TypeToVisConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value?.GetType().Name == "Script" ? Visibility.Collapsed : Visibility.Visible;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class ObjectToVisConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null ? Visibility.Visible : Visibility.Collapsed;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class ObjectToRevVisConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null ? Visibility.Collapsed : Visibility.Visible;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class IsDirtyToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? "HelpRhombusOutline" : "Check";
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class IsDirtyToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? Util.Color.Red : Util.Color.Green;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class TypeToVisReverseConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null)
                return value.GetType().Name == "Script" ? Visibility.Visible : Visibility.Collapsed;
            else
                return Visibility.Collapsed;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class ControllerToTooltipConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            IController ic = value as IController;
            string tooltip = "";
            if (ic != null)
            {
                tooltip = "Status: " + ic.ConnectionStatus + System.Environment.NewLine +
                    "Host: " + ic.ConnParam.Host + System.Environment.NewLine +
                    "Port: " + ic.ConnParam.Port + System.Environment.NewLine +
                    "Clien Id: " + ic.ConnParam.ClientId;
            }
            return tooltip;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class TypeToStaticMemberConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string text = string.Empty;
            Type t = typeof(MainViewModel);
            string ns = t.Namespace;
            var props = Type.GetType(ns + "." + (string)value).GetProperties();
            foreach (var prop in props)
            {
                if (prop.Name == (string)parameter)
                {
                    text = (string)prop.GetValue(null);
                    break;
                }                    
            }
            return text;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // for statusbar item background
    public class StatusToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {      
            System.Windows.Media.Color color = Util.ConvertStringToColor("#00FFFFFF"); 
            if (value.ToString().ToLower() == "connected")
                color = Util.Color.Green;
            else if (value.ToString().ToLower() == "connecting")
                color = Util.Color.Yellow;
            else if (value.ToString().ToLower() == "error")
                color = Util.Color.Red;
            else if (value.ToString().ToLower() == "disconnected")
                color = Util.Color.Orange;
            return new SolidColorBrush(color);
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class StatusToIconColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            System.Windows.Media.Color color = Util.ConvertStringToColor("#00FFFFFF");
            if (value.ToString().ToLower() == "connected")
                color = Util.Color.Orange;
            else if (value.ToString().ToLower() == "connecting")
                color = Util.Color.Yellow;
            else if (value.ToString().ToLower() == "error")
                color = Util.Color.Red;
            else if (value.ToString().ToLower() == "disconnected")
                color = Util.Color.Green;
            return new SolidColorBrush(color);
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class StatusToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string icon = "PowerPlugOff";
            if (value.ToString().ToLower() == "connected" || value.ToString().ToLower() == "error")
                icon = "PowerPlugOff";
            else if (value.ToString().ToLower() == "connecting")
                icon = "PowerPlug";
            else if (value.ToString().ToLower() == "disconnected")
                icon = "PowerPlug";
            return icon;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class StatusToIconTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string text = "Conenct";
            if (value.ToString().ToLower() == "connected" || value.ToString().ToLower() == "error")
                text = "Disconnect";
            else if (value.ToString().ToLower() == "connecting")
                text = "Stop connecting";
            else if (value.ToString().ToLower() == "disconnected")
                text = "Conenct";
            return text;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class StatusToEnableConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isEnable = true;
            if (value.ToString().ToLower() == "connected")
                isEnable = true;
            else 
                isEnable = false;
            return isEnable;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class StatusToVisbilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Visibility vis = Visibility.Visible;
            if (value.ToString().ToLower() == "connected")
                vis = Visibility.Visible;
            else
                vis = Visibility.Collapsed;
            return vis;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class StatusToReverseVisbilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Visibility vis = Visibility.Visible;
            if (value.ToString().ToLower() == "connected")
                vis = Visibility.Collapsed;
            else
                vis = Visibility.Visible;
            return vis;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class StatusToIconImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            ImageSource img;
            if (value.ToString().ToLower() == "connected")
            {                
                img = Util.mdIcons.ToImageSource<MaterialIcons>(MaterialIcons.PowerPlugOff, new SolidColorBrush(Util.Color.Red));
            }
            else
            {
                img = Util.mdIcons.ToImageSource<MaterialIcons>(MaterialIcons.PowerPlug, new SolidColorBrush(Util.Color.Green));
            }
            return img;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class NumToPercentageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int per = (int)Math.Round(float.Parse(value.ToString()) * 100, 0);
            return "Zoom: " + per + "%";
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class BoolToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool b = (bool)value;
            return new SolidColorBrush(b?Util.Color.AliceBlue: Util.Color.Transparent);
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class BoolToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? "Pin" : "PinOff";
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class BoolToLogStatusIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (OrderManager.MainWin != null)
                return (bool)value ? OrderManager.MainWin.FindResource("logDrawingImage") : OrderManager.MainWin.FindResource("logPauseDrawingImage");
            else
                return null;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class BoolToVisbilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? Visibility.Visible : Visibility.Collapsed;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class BoolToRevVisConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? Visibility.Collapsed : Visibility.Visible;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class TimeToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {            
            return ((DateTime)value).ToString("dd HH:mm:ss");
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
   
}

