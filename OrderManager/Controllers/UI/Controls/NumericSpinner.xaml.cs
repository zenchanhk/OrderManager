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
using System.Timers;

namespace AmiBroker.Controllers.UI.Controls
{
    /// <summary>
    /// Interaction logic for NumericSpinner.xaml
    /// </summary>
    public partial class NumericSpinner : UserControl
    {
        #region Fields

        public event EventHandler PropertyChanged;
        public event EventHandler ValueChanged;
        #endregion

        public NumericSpinner()
        {
            InitializeComponent();

            tb_main.SetBinding(TextBox.TextProperty, new Binding("Value")
            {
                ElementName = "root_numeric_spinner",
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            });

            DependencyPropertyDescriptor.FromProperty(ValueProperty, typeof(NumericSpinner)).AddValueChanged(this, PropertyChanged);
            DependencyPropertyDescriptor.FromProperty(ValueProperty, typeof(NumericSpinner)).AddValueChanged(this, ValueChanged);
            DependencyPropertyDescriptor.FromProperty(DecimalsProperty, typeof(NumericSpinner)).AddValueChanged(this, PropertyChanged);
            DependencyPropertyDescriptor.FromProperty(MinValueProperty, typeof(NumericSpinner)).AddValueChanged(this, PropertyChanged);
            DependencyPropertyDescriptor.FromProperty(MaxValueProperty, typeof(NumericSpinner)).AddValueChanged(this, PropertyChanged);

            PropertyChanged += (x, y) => validate();
        }

        #region ValueProperty

        public readonly static DependencyProperty ValueProperty = DependencyProperty.Register(
            "Value",
            typeof(decimal),
            typeof(NumericSpinner),
            new PropertyMetadata(new decimal(0)));

        public decimal Value
        {
            get { return (decimal)GetValue(ValueProperty); }
            set
            {
                if (value < MinValue)
                    value = MinValue;
                if (value > MaxValue)
                    value = MaxValue;
                SetValue(ValueProperty, value);
                ValueChanged?.Invoke(this, new EventArgs());
            }
        }


        #endregion

        #region StepProperty

        public readonly static DependencyProperty StepProperty = DependencyProperty.Register(
            "Step",
            typeof(decimal),
            typeof(NumericSpinner),
            new PropertyMetadata(new decimal(0.1)));

        public decimal Step
        {
            get { return (decimal)GetValue(StepProperty); }
            set
            {
                SetValue(StepProperty, value);
            }
        }

        #endregion

        #region DecimalsProperty

        public readonly static DependencyProperty DecimalsProperty = DependencyProperty.Register(
            "Decimals",
            typeof(int),
            typeof(NumericSpinner),
            new PropertyMetadata(2));

        public int Decimals
        {
            get { return (int)GetValue(DecimalsProperty); }
            set
            {
                SetValue(DecimalsProperty, value);
            }
        }

        #endregion

        #region MinValueProperty

        public readonly static DependencyProperty MinValueProperty = DependencyProperty.Register(
            "MinValue",
            typeof(decimal),
            typeof(NumericSpinner),
            new PropertyMetadata(decimal.MinValue));

        public decimal MinValue
        {
            get { return (decimal)GetValue(MinValueProperty); }
            set
            {
                if (value > MaxValue)
                    MaxValue = value;
                SetValue(MinValueProperty, value);
            }
        }

        #endregion

        #region MaxValueProperty

        public readonly static DependencyProperty MaxValueProperty = DependencyProperty.Register(
            "MaxValue",
            typeof(decimal),
            typeof(NumericSpinner),
            new PropertyMetadata(decimal.MaxValue));

        public decimal MaxValue
        {
            get { return (decimal)GetValue(MaxValueProperty); }
            set
            {
                if (value < MinValue)
                    value = MinValue;
                SetValue(MaxValueProperty, value);
            }
        }

        #endregion

        /// <summary>
        /// Revalidate the object, whenever a value is changed...
        /// </summary>
        private void validate()
        {
            // Logically, This is not needed at all... as it's handled within other properties...
            if (MinValue > MaxValue) MinValue = MaxValue;
            if (MaxValue < MinValue) MaxValue = MinValue;
            if (Value < MinValue) Value = MinValue;
            if (Value > MaxValue) Value = MaxValue;

            Value = decimal.Round(Value, Decimals);
        }

        private void cmdUp_Click(object sender, RoutedEventArgs e)
        {
            Value += Step;
        }

        private void cmdDown_Click(object sender, RoutedEventArgs e)
        {
            Value -= Step;
        }

        private void tb_main_Loaded(object sender, RoutedEventArgs e)
        {
            ValueChanged?.Invoke(this, new EventArgs());
        }

        private Timer t = new Timer();
        private string current_state = "";
        private void Tb_main_KeyUp(object sender, KeyEventArgs e)
        {
            t.Stop();            
        }
        
        private void Tb_main_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Up)
            {
                Value += Step;
                current_state = "+";
            }
            if (e.Key == Key.Down)
            {
                Value -= Step;
                current_state = "-";
            }
            if (e.Key == Key.Up && e.Key == Key.Down)
            {
                t.Interval = 1000;
                t.Elapsed += new ElapsedEventHandler(TimerElapsed);
                t.Start();
            }
            if ((e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9) ||
                (e.Key >= Key.D0 && e.Key <= Key.D9) || e.Key == Key.Back || e.Key == Key.Delete)
            {
                
            }
            else
                e.Handled = true;
        }

        private void Tb_main_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {            
            string s = e.Text;
            if (s != string.Empty)
            {
                int i = int.Parse(s);
                if (i > MaxValue || i < MinValue)
                    e.Handled = true;
            }                            
        }

        private void TimerElapsed(object sender, ElapsedEventArgs e)
        {
            t.Interval = 100;
            if (current_state == "+")
            {
                System.Windows.Threading.Dispatcher.FromThread(OrderManager.UIThread).Invoke(() =>
                {
                    Value += Step;
                });                
            }
            if (current_state == "-")
            {
                System.Windows.Threading.Dispatcher.FromThread(OrderManager.UIThread).Invoke(() =>
                {
                    Value -= Step;
                });
            }
        }

        private void CmdDown_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Value -= Step;
            current_state = "-";
            t.Interval = 1000;
            t.Elapsed += new ElapsedEventHandler(TimerElapsed);
            t.Start();
        }

        private void CmdDown_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            t.Stop();
        }

        private void CmdUp_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Value += Step;
            current_state = "+";
            t.Interval = 1000;
            t.Elapsed += new ElapsedEventHandler(TimerElapsed);
            t.Start();
        }


    }
}
