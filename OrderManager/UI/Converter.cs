using System;
using System.Windows;
using System.Windows.Data;
using System.Globalization;
using System.Windows.Media;
using System.Windows.Controls;
using FontAwesome.Sharp;
using System.Reflection;

namespace AmiBroker.Controllers
{    
    public class Util
    {
        internal static readonly FontFamily mdIcons =
            Assembly.GetExecutingAssembly().GetFont("fonts", "Material Design Icons");
        
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

    // for statusbar item background
    public class StatusToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {          
            System.Windows.Media.Color red = Util.ConvertStringToColor("#FFFF0000");
            System.Windows.Media.Color green = Util.ConvertStringToColor("#FF00FF00");
            System.Windows.Media.Color yellow = Util.ConvertStringToColor("#FFFFFF00");
            System.Windows.Media.Color color = Util.ConvertStringToColor("#00FFFFFF"); 
            System.Windows.Media.Color orange = Util.ConvertStringToColor("#FFFF8C00");
            if (value.ToString().ToLower() == "connected")
                color = green;
            else if (value.ToString().ToLower() == "connecting")
                color = yellow;
            else if (value.ToString().ToLower() == "error")
                color = red;
            else if (value.ToString().ToLower() == "disconnected")
                color = orange;
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
            System.Windows.Media.Color red = Util.ConvertStringToColor("#FFFF0000");
            System.Windows.Media.Color green = Util.ConvertStringToColor("#FF00FF00");
            System.Windows.Media.Color yellow = Util.ConvertStringToColor("#FFFFFF00");
            System.Windows.Media.Color color = Util.ConvertStringToColor("#00FFFFFF");
            System.Windows.Media.Color orange = Util.ConvertStringToColor("#FFFF8C00");
            if (value.ToString().ToLower() == "connected")
                color = orange;
            else if (value.ToString().ToLower() == "connecting")
                color = yellow;
            else if (value.ToString().ToLower() == "error")
                color = red;
            else if (value.ToString().ToLower() == "disconnected")
                color = green;
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
            System.Windows.Media.Color red = Util.ConvertStringToColor("#FFFF0000");
            System.Windows.Media.Color green = Util.ConvertStringToColor("#FF00FF00");
            if (value.ToString().ToLower() == "connected")
            {                
                img = Util.mdIcons.ToImageSource<MaterialIcons>(MaterialIcons.PowerPlugOff, new SolidColorBrush(red));
            }
            else
            {
                img = Util.mdIcons.ToImageSource<MaterialIcons>(MaterialIcons.PowerPlug, new SolidColorBrush(green));
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
            System.Windows.Media.Color transparent = Util.ConvertStringToColor("#00FFFFFF");
            System.Windows.Media.Color aliceblue = Util.ConvertStringToColor("#FF87CEFA");
            return new SolidColorBrush(b?aliceblue:transparent);
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

