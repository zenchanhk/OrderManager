using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Timers;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace ControlLib
{

    [TemplatePart(Name = "PART_TextBox", Type = typeof(TextBox))]
    [TemplatePart(Name = "PART_ButtonUp", Type = typeof(ButtonBase))]
    [TemplatePart(Name = "PART_ButtonDown", Type = typeof(ButtonBase))]
    public class NumericUpDown : Control
    {

        private TextBox PART_TextBox = new TextBox();
        private Dispatcher dispatcher;
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            TextBox textBox = GetTemplateChild("PART_TextBox") as TextBox;
            if (textBox != null)
            {
                PART_TextBox = textBox;
                PART_TextBox.PreviewKeyDown += textBox_PreviewKeyDown;
                PART_TextBox.TextChanged += textBox_TextChanged;
                PART_TextBox.Text = Value.ToString();
            }
            ButtonBase PART_ButtonUp = GetTemplateChild("PART_ButtonUp") as ButtonBase;
            if (PART_ButtonUp != null)
            {
                PART_ButtonUp.Click += buttonUp_Click;
            }
            ButtonBase PART_ButtonDown = GetTemplateChild("PART_ButtonDown") as ButtonBase;
            if (PART_ButtonDown != null)
            {
                PART_ButtonDown.Click += buttonDown_Click; 
            }
            dispatcher = Dispatcher;
        }

        static NumericUpDown()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(NumericUpDown), new FrameworkPropertyMetadata(typeof(NumericUpDown)));
        }

        public static readonly RoutedEvent ValueChangedEvent = EventManager.RegisterRoutedEvent(
            "ValueChanged", RoutingStrategy.Direct,
            typeof(ValueChangedEventHandler), typeof(NumericUpDown));
        public event ValueChangedEventHandler ValueChanged
        {
            add
            {
                base.AddHandler(NumericUpDown.ValueChangedEvent, value);
            }
            remove
            {
                base.RemoveHandler(NumericUpDown.ValueChangedEvent, value);
            }
        }

        public static readonly RoutedEvent ClickEvent = EventManager.RegisterRoutedEvent(
            "Click", RoutingStrategy.Direct,
            typeof(ClickEventHandler), typeof(NumericUpDown));
        public event ClickEventHandler Click
        {
            add
            {
                base.AddHandler(NumericUpDown.ClickEvent, value);
            }
            remove
            {
                base.RemoveHandler(NumericUpDown.ClickEvent, value);
            }
        }

        public double MaxValue
        {
            get { return (double)GetValue(MaxValueProperty); }
            set { SetValue(MaxValueProperty, value); }
        }
        public static readonly DependencyProperty MaxValueProperty =
            DependencyProperty.Register("MaxValue", typeof(double), typeof(NumericUpDown), new FrameworkPropertyMetadata(100D, maxValueChangedCallback, coerceMaxValueCallback));
        private static object coerceMaxValueCallback(DependencyObject d, object value)
        {
            double minValue = ((NumericUpDown)d).MinValue;
            if ((double)value < minValue)
                return minValue;

            return value;
        }
        private static void maxValueChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            NumericUpDown numericUpDown = ((NumericUpDown)d);
            numericUpDown.CoerceValue(MinValueProperty);
            numericUpDown.CoerceValue(ValueProperty);
        }

        public double MinValue
        {
            get { return (double)GetValue(MinValueProperty); }
            set { SetValue(MinValueProperty, value); }
        }
        public static readonly DependencyProperty MinValueProperty =
            DependencyProperty.Register("MinValue", typeof(double), typeof(NumericUpDown), new FrameworkPropertyMetadata(0D, minValueChangedCallback, coerceMinValueCallback));
        private static object coerceMinValueCallback(DependencyObject d, object value)
        {
            double maxValue = ((NumericUpDown)d).MaxValue;
            if ((double)value > maxValue)
                return maxValue;

            return value;
        }
        private static void minValueChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            NumericUpDown numericUpDown = ((NumericUpDown)d);
            numericUpDown.CoerceValue(NumericUpDown.MaxValueProperty);
            numericUpDown.CoerceValue(NumericUpDown.ValueProperty);
        }

        public double Increment
        {
            get { return (double)GetValue(IncrementProperty); }
            set { SetValue(IncrementProperty, value); }
        }
        public static readonly DependencyProperty IncrementProperty =
            DependencyProperty.Register("Increment", typeof(double), typeof(NumericUpDown), new FrameworkPropertyMetadata(1D, null, coerceIncrementCallback));
        private static object coerceIncrementCallback(DependencyObject d, object value)
        {
            NumericUpDown numericUpDown = ((NumericUpDown)d);
            double i = numericUpDown.MaxValue - numericUpDown.MinValue;
            if ((double)value > i)
                return i;

            return value;
        }
        public string Type
        {
            get { return (string)GetValue(TypeProperty); }
            set { SetValue(TypeProperty, value); }
        }
        public static readonly DependencyProperty TypeProperty =
            DependencyProperty.Register("Type", typeof(string), typeof(NumericUpDown), new FrameworkPropertyMetadata("Integer", null, coerceTypeCallback));
        private static object coerceTypeCallback(DependencyObject d, object value)
        {
            if (value.ToString() != "Integer" || value.ToString() != "Double")
                return "Integer";
            else
                return value;
        }

        public double Value
        {
            get { return (double)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(double), typeof(NumericUpDown), new FrameworkPropertyMetadata(0D, valueChangedCallback, coerceValueCallback), validateValueCallback);
        private static void valueChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            NumericUpDown numericUpDown = (NumericUpDown)d;
            ValueChangedEventArgs ea =
                new ValueChangedEventArgs(NumericUpDown.ValueChangedEvent, d, (double)e.OldValue, (double)e.NewValue);
            numericUpDown.RaiseEvent(ea);
            //if (ea.Handled) numericUpDown.Value = (double)e.OldValue;
            //else 
            numericUpDown.PART_TextBox.Text = Math.Round((double)e.NewValue, 3).ToString();
        }
        private static bool validateValueCallback(object value)
        {
            double val = (double)value;
            if (val >= double.MinValue && val <= double.MaxValue)
                return true;
            else
                return false;
        }
        private static object coerceValueCallback(DependencyObject d, object value)
        {
            double val = (double)value;
            NumericUpDown ud = d as NumericUpDown;
            double minValue = ((NumericUpDown)d).MinValue;
            double maxValue = ((NumericUpDown)d).MaxValue;
            double result;
            if (val < minValue)
            {
                result = minValue;
                ud.Value = minValue;
            }                
            else if (val > maxValue)
            {
                result = maxValue;
                ud.Value = maxValue;
            }                
            else
                result = (double)value;

            return result;
        }

        private void buttonUp_Click(object sender, RoutedEventArgs e)
        {
            Value += Increment;
            RoutedEventArgs args = new RoutedEventArgs(ClickEvent, this);
            RaiseEvent(args);
        }
        private void buttonDown_Click(object sender, RoutedEventArgs e)
        {
            Value -= Increment;
            RoutedEventArgs args = new RoutedEventArgs(ClickEvent, this);
            RaiseEvent(args);
        }

        private Timer t = new Timer();
        private string current_state = "";
        private void text_KeyUp(object sender, KeyEventArgs e)
        {
            t.Stop();
        }
        private void textBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Up)
            {
                Value += Increment;
                current_state = "+";
            }
            if (e.Key == Key.Down)
            {
                Value -= Increment;
                current_state = "-";
            }
            if (e.Key == Key.Up && e.Key == Key.Down)
            {
                t.Interval = 1000;
                t.Elapsed += new ElapsedEventHandler(TimerElapsed);
                t.Start();
            }
            if (!((e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9) || e.Key == Key.Decimal ||
                (e.Key >= Key.D0 && e.Key <= Key.D9) || e.Key == Key.Back || e.Key == Key.Delete
                || e.Key == Key.Right || e.Key == Key.Left))
            {
                if (e.Key == Key.Decimal && Type != "Integer")
                    e.Handled = false;
                else
                    e.Handled = true;
            }
        }
        private void textBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            int index = PART_TextBox.CaretIndex;
            double result;
            if (!double.TryParse(PART_TextBox.Text, out result))
            {
                var changes = e.Changes.FirstOrDefault();
                PART_TextBox.Text = PART_TextBox.Text.Remove(changes.Offset, changes.AddedLength);
                PART_TextBox.CaretIndex = index > 0 ? index - changes.AddedLength : 0;
            }
            else if (result <= MaxValue && result >= MinValue)
                Value = result;
            else
            {
                PART_TextBox.Text = Value.ToString();
                PART_TextBox.CaretIndex = index > 0 ? index - 1 : 0;
            }
        }

        private void TimerElapsed(object sender, ElapsedEventArgs e)
        {
            t.Interval = 100;
            if (current_state == "+")
            {
                dispatcher.Invoke(() =>
                {
                    Value += Increment;
                });
            }
            if (current_state == "-")
            {
                dispatcher.Invoke(() =>
                {
                    Value -= Increment;
                });
            }
        }
    }

    //
    public delegate void ValueChangedEventHandler(object sender, ValueChangedEventArgs e);
    public class ValueChangedEventArgs : RoutedEventArgs
    {
        public ValueChangedEventArgs(RoutedEvent routedEvent, object source, double oldValue, double newValue)
            : base(routedEvent, source)
        {
            OldValue = oldValue;
            NewValue = newValue;
        }
        public double OldValue { get; private set; }
        public double NewValue { get; private set; }
    }

    public delegate void ClickEventHandler(object sender, RoutedEventArgs e);
}

