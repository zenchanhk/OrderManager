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
using System.Reflection;
using System.Text.RegularExpressions;

namespace ControlLib
{
    
    enum MoveDirection
    {
        Forward=0,
        Backward=1,
        BackwardEdit=2
    }
    enum ReturnAction
    {
        Return = 1,
        None = 0
    }
    [TemplatePart(Name = "PART_TextBox", Type = typeof(TextBox))]
    [TemplatePart(Name = "PART_ButtonUp", Type = typeof(ButtonBase))]
    [TemplatePart(Name = "PART_ButtonDown", Type = typeof(ButtonBase))]
    public class DateTimeUpDown : Control
    {
        public int Increment { get; set; } = 1;
        private TextBox PART_TextBox = new TextBox();
        private Dispatcher dispatcher;
        private List<char> validChars = new List<char>() { 'y', 'M', 'd', 'm', 's', 't', 'h', 'H' };
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            TextBox textBox = GetTemplateChild("PART_TextBox") as TextBox;
            if (textBox != null)
            {
                PART_TextBox = textBox;
                PART_TextBox.PreviewKeyDown += textBox_PreviewKeyDown;
                PART_TextBox.TextChanged += textBox_TextChanged;
                PART_TextBox.Text = ((DateTime)Value).ToString(Format);
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

            // fetch TextEditor from myTextBox
            PropertyInfo textEditorProperty = typeof(TextBox).GetProperty("TextEditor", BindingFlags.NonPublic | BindingFlags.Instance);
            object textEditor = textEditorProperty.GetValue(textBox, null);

            // set _OvertypeMode on the TextEditor
            PropertyInfo overtypeModeProperty = textEditor.GetType().GetProperty("_OvertypeMode", BindingFlags.NonPublic | BindingFlags.Instance);
            overtypeModeProperty.SetValue(textEditor, true, null);
        }

        static DateTimeUpDown()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(DateTimeUpDown), new FrameworkPropertyMetadata(typeof(DateTimeUpDown)));
        }

        public static readonly RoutedEvent ValueChangedEvent = EventManager.RegisterRoutedEvent(
            "ValueChanged", RoutingStrategy.Direct,
            typeof(ValueChangedEventHandler), typeof(DateTimeUpDown));
        public event ValueChangedEventHandler ValueChanged
        {
            add
            {
                base.AddHandler(DateTimeUpDown.ValueChangedEvent, value);
            }
            remove
            {
                base.RemoveHandler(DateTimeUpDown.ValueChangedEvent, value);
            }
        }

        public DateTime MaxValue
        {
            get { return (DateTime)GetValue(MaxValueProperty); }
            set { SetValue(MaxValueProperty, value); }
        }
        public static readonly DependencyProperty MaxValueProperty =
            DependencyProperty.Register("MaxValue", typeof(DateTime), typeof(DateTimeUpDown), new FrameworkPropertyMetadata(DateTime.MaxValue, maxValueChangedCallback, coerceMaxValueCallback));
        private static object coerceMaxValueCallback(DependencyObject d, object value)
        {
            DateTime minValue = ((DateTimeUpDown)d).MinValue;
            if ((DateTime)value < minValue)
                return minValue;

            return value;
        }
        private static void maxValueChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DateTimeUpDown DateTimeUpDown = ((DateTimeUpDown)d);
            DateTimeUpDown.CoerceValue(MinValueProperty);
            DateTimeUpDown.CoerceValue(ValueProperty);
        }

        public DateTime MinValue
        {
            get { return (DateTime)GetValue(MinValueProperty); }
            set { SetValue(MinValueProperty, value); }
        }
        public static readonly DependencyProperty MinValueProperty =
            DependencyProperty.Register("MinValue", typeof(DateTime), typeof(DateTimeUpDown), new FrameworkPropertyMetadata(DateTime.MinValue, minValueChangedCallback, coerceMinValueCallback));
        private static object coerceMinValueCallback(DependencyObject d, object value)
        {
            DateTime maxValue = ((DateTimeUpDown)d).MaxValue;
            if ((DateTime)value > maxValue)
                return maxValue;

            return value;
        }
        private static void minValueChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DateTimeUpDown DateTimeUpDown = ((DateTimeUpDown)d);
            DateTimeUpDown.CoerceValue(DateTimeUpDown.MaxValueProperty);
            DateTimeUpDown.CoerceValue(DateTimeUpDown.ValueProperty);
        }

        public string Format
        {
            get { return (string)GetValue(FormatProperty); }
            set { SetValue(FormatProperty, value); }
        }
        public static readonly DependencyProperty FormatProperty =
            DependencyProperty.Register("Format", typeof(string), typeof(DateTimeUpDown), new FrameworkPropertyMetadata("M/d/yyyy", null, coerceFormatCallback));
        private static object coerceFormatCallback(DependencyObject d, object value)
        {
            DateTimeUpDown DateTimeUpDown = ((DateTimeUpDown)d);
            // check format is valid

            return value;
        }

        public DateTime? Value
        {
            get { return (DateTime?)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(DateTime?), typeof(DateTimeUpDown), new PropertyMetadata(DateTime.Now, valueChangedCallback, coerceValueCallback), validateValueCallback);
        private static void valueChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DateTimeUpDown dt = (DateTimeUpDown)d;
            ValueChangedEventArgs ea =
                new ValueChangedEventArgs(DateTimeUpDown.ValueChangedEvent, d, (DateTime?)e.OldValue, (DateTime?)e.NewValue);
            dt.RaiseEvent(ea);
            //if (ea.Handled) DateTimeUpDown.Value = (DateTime)e.OldValue;
            //else 
            if (e.NewValue == null)
                dt.PART_TextBox.Text = string.Empty;
            else
                dt.PART_TextBox.Text = ((DateTime)e.NewValue).ToString(dt.Format);
        }
        private static bool validateValueCallback(object value)
        {

            return true;
        }
        private static object coerceValueCallback(DependencyObject d, object value)
        {
            DateTimeUpDown dt = (DateTimeUpDown)d;
            if (value != null)
            {
                DateTime val = (DateTime)value;
                if (val > dt.MinValue && val < dt.MaxValue)
                    return val;
                else if (val < dt.MinValue)
                    return dt.MinValue;
                else if (val < dt.MaxValue)
                    return dt.MaxValue;

            }          
            return null;
        }

        private void buttonUp_Click(object sender, RoutedEventArgs e)
        {
            //Value += Increment;
        }
        private void buttonDown_Click(object sender, RoutedEventArgs e)
        {
            //Value -= Increment;
        }

        private Timer t = new Timer();
        private string current_state = "";
        private void text_KeyUp(object sender, KeyEventArgs e)
        {
            t.Stop(); // stop continous increment
        }
        private void textBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // ignore control key
            if ((Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)) ||
                (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)))
                return;
            //
            if (e.Key == Key.Up)
            {
                //Value += Increment;
                current_state = "+";
            }
            if (e.Key == Key.Down)
            {
                //Value -= Increment;
                current_state = "-";
            }
            if (e.Key == Key.Up && e.Key == Key.Down)
            {
                t.Interval = 1000;
                t.Elapsed += new ElapsedEventHandler(TimerElapsed);
                t.Start();
            }
            if (!((e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9) ||
                (e.Key >= Key.D0 && e.Key <= Key.D9) || e.Key == Key.Back || e.Key == Key.Delete) ||
                (e.Key == Key.Right || e.Key == Key.Left) ||
                ((Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)) && (e.Key == Key.Right || e.Key == Key.Left)) ||
                ((Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)) && e.Key == Key.A))
                e.Handled = true;

            if (!(Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)))
            {
                if (e.Key == Key.Right)
                {
                    if (PART_TextBox.CaretIndex < PART_TextBox.Text.Length) PART_TextBox.CaretIndex++;
                    MoveCaret();
                    e.Handled = true;
                }
                if (e.Key == Key.Left)
                {
                    if (PART_TextBox.CaretIndex > 0) PART_TextBox.CaretIndex--;
                    MoveCaret(MoveDirection.Backward);
                    e.Handled = true;
                }
            }
            
            

            // handle backspace and delete key
            if (e.Key == Key.Back || e.Key == Key.Delete)
            {
                if (ReplaceSelection(e.Key) == ReturnAction.Return)
                    return;
                
                e.Handled = true;
            }
                
            if ((e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9) ||
                (e.Key >= Key.D0 && e.Key <= Key.D9))
            {
                if (PART_TextBox.SelectionLength > 0)
                    if (ReplaceSelection(Key.Back, e.Key) == ReturnAction.Return)
                        return;

                MoveCaret();
            }
        }

        private ReturnAction ReplaceSelection(Key key, Key replaceKey = Key.None)
        {            
            // whole
            if (PART_TextBox.SelectionLength == PART_TextBox.Text.Length)
            {
                PART_TextBox.Text = string.Empty;
                return ReturnAction.Return;
            }

            // part
            int index = PART_TextBox.CaretIndex;
            // make selection of length 1
            if (PART_TextBox.SelectionLength == 0)
            {
                if (key == Key.Delete)
                {
                    MoveCaret();
                    if (PART_TextBox.CaretIndex == PART_TextBox.Text.Length)
                        return ReturnAction.Return;
                }
                else if (key == Key.Back)
                {
                    MoveCaret(MoveDirection.BackwardEdit);
                }
                PART_TextBox.SelectionStart = PART_TextBox.CaretIndex; // selection one char
                index = PART_TextBox.CaretIndex;    // update index for future use
                PART_TextBox.SelectionLength = 1;
            }

            // subtitute selected text with 0s
            if (PART_TextBox.SelectedText.Length > 0 && PART_TextBox.SelectionLength < PART_TextBox.Text.Length)
            {
                if (replaceKey == Key.None)
                    PART_TextBox.Text = ReplaceWithZero(PART_TextBox.Text, PART_TextBox.SelectedText, PART_TextBox.SelectionStart);
                else
                    PART_TextBox.Text = ReplaceWithZero(PART_TextBox.Text, PART_TextBox.SelectedText, 
                        PART_TextBox.SelectionStart, KeyCodeToInt(replaceKey).ToString());
            }
            
            // reset caret
            if (key == Key.Back)
            {
                PART_TextBox.CaretIndex = index;
                MoveCaret();
            }
            else if (key == Key.Delete)
            {
                PART_TextBox.CaretIndex = ++index;
                MoveCaret();
            }
            return ReturnAction.None;
        }

        private string ReplaceWithZero(string org, string selected, int selStart, string replaceStr = null)
        {
            Regex regex = new Regex("[0-9]");
            string replaced_str = regex.Replace(selected, "0");
            if (replaceStr != null)
            {
                if (replaceStr.Length > replaced_str.Length)
                    replaced_str = replaceStr;
                else
                    replaced_str = replaceStr + replaced_str.Substring(replaceStr.Length, replaced_str.Length - replaceStr.Length);
            }
                
            return org.Substring(0, selStart) + replaced_str + 
                org.Substring(selStart + selected.Length, org.Length - selStart - selected.Length);
        }

        private void MoveCaret(MoveDirection dir = MoveDirection.Forward)
        {
            while (true)
            {
                int index = PART_TextBox.CaretIndex;
                if (dir == MoveDirection.Forward)
                {
                    if (index >= PART_TextBox.Text.Length)
                    {
                        PART_TextBox.CaretIndex = PART_TextBox.Text.Length;
                        break;
                    }
                    if (index < PART_TextBox.Text.Length && !validChars.Contains(Format.Substring(index, 1)[0]))
                    {
                        PART_TextBox.CaretIndex++;
                    }
                    else
                    {
                        PART_TextBox.CaretIndex = index;
                        break;
                    }
                }
                else if (dir == MoveDirection.Backward)
                {
                    if (index >= PART_TextBox.Text.Length)
                    {
                        PART_TextBox.CaretIndex = PART_TextBox.Text.Length;
                        break;
                    }
                    if (index < PART_TextBox.Text.Length && !validChars.Contains(Format.Substring(index, 1)[0]))
                    {
                        index -= 1;
                        PART_TextBox.CaretIndex = index >= 0 ? index : 0;
                    }
                    else
                    {
                        PART_TextBox.CaretIndex = index;
                        break;
                    }
                }
                else if (dir == MoveDirection.BackwardEdit)
                {
                    if (index <= 0)
                    {
                        PART_TextBox.CaretIndex = 0;
                        break;
                    }
                    if (index < PART_TextBox.Text.Length && !validChars.Contains(Format.Substring(index - 1, 1)[0]))
                    {
                        index -= 2;
                        PART_TextBox.CaretIndex = index >= 0 ? index : 0;
                        if (validChars.Contains(Format.Substring(index, 1)[0]))
                            return;
                    }
                    else
                    {
                        index--;
                        PART_TextBox.CaretIndex = index >= 0 ? index : 0;
                        break;
                    }
                }  
                
            }
        }
        private void textBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (PART_TextBox.Text == "")
            {
                Value = null;
                return;
            }

            DateTime result;

            if (PART_TextBox.Text.Length == 1)
            {
                string s = Format.Replace("yyyy", "2000");
                s = s.Replace("MM", "01");
                s = s.Replace("dd", "01");
                s = s.Replace("tt", "AM");
                List<char> chs = new List<char> { 'y', 'M', 'd', 't' };
                foreach (char chr in validChars)
                {
                    if (!chs.Contains(chr))
                        s = s.Replace(chr, '0');
                }
                string dt = PART_TextBox.Text + s.Substring(1);
                if (DateTime.TryParseExact(dt, Format, CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
                {
                    PART_TextBox.Text = dt;
                    PART_TextBox.CaretIndex = 1;
                }
                else
                {
                    dt = s.Substring(0, 1) + PART_TextBox.Text + s.Substring(2);
                    if (DateTime.TryParseExact(dt, Format, CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
                    {
                        PART_TextBox.Text = dt;
                        PART_TextBox.CaretIndex = 1;
                    }
                    else
                    {
                        Value = null;
                        PART_TextBox.Text = string.Empty;
                        return;
                    }
                }
            }

            int index = PART_TextBox.CaretIndex;            
            if (!DateTime.TryParseExact(PART_TextBox.Text, Format, CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
            {
                //var changes = e.Changes.FirstOrDefault();
                //PART_TextBox.Text = PART_TextBox.Text.Remove(changes.Offset, changes.AddedLength);
                //PART_TextBox.CaretIndex = index > 0 ? index - changes.AddedLength : 0;
                PART_TextBox.Text = ((DateTime)Value).ToString(Format);
                PART_TextBox.CaretIndex = index > 0 ? index - 1 : 0;
            }
            else if (result < MaxValue && result > MinValue)
                Value = result;
            else
            {
                PART_TextBox.Text = ((DateTime)Value).ToString(Format);
                PART_TextBox.CaretIndex = index > 0 ? index - 1 : 0;
            }
            MoveCaret();
        }

        private void TimerElapsed(object sender, ElapsedEventArgs e)
        {
            t.Interval = 100;
            if (current_state == "+")
            {
                dispatcher.Invoke(() =>
                {
                    //Value += Increment;
                });
            }
            if (current_state == "-")
            {
                dispatcher.Invoke(() =>
                {
                    //Value -= Increment;
                });
            }
        }

        private static int KeyCodeToInt(Key key)
        {
            int i = -1;
            switch (key)
            {
                case Key.NumPad0:
                case Key.D0:
                    i = 0;
                    break;
                case Key.NumPad1:
                case Key.D1:
                    i = 0;
                    break;
                case Key.NumPad2:
                case Key.D2:
                    i = 0;
                    break;
                case Key.NumPad3:
                case Key.D3:
                    i = 0;
                    break;
                case Key.NumPad4:
                case Key.D4:
                    i = 0;
                    break;
                case Key.NumPad5:
                case Key.D5:
                    i = 0;
                    break;
                case Key.NumPad6:
                case Key.D6:
                    i = 0;
                    break;
                case Key.NumPad7:
                case Key.D7:
                    i = 0;
                    break;
                case Key.NumPad8:
                case Key.D8:
                    i = 0;
                    break;
                case Key.NumPad9:
                case Key.D9:
                    i = 0;
                    break;
            }
            return i;
        }
    }

    //
    public delegate void ValueChangedEventHandler1(object sender, ValueChangedEventArgs e);
    public class ValueChangedEventArgs : RoutedEventArgs
    {
        public ValueChangedEventArgs(RoutedEvent routedEvent, object source, DateTime? oldValue, DateTime? newValue)
            : base(routedEvent, source)
        {
            OldValue = oldValue;
            NewValue = newValue;
        }
        public DateTime? OldValue { get; private set; }
        public DateTime? NewValue { get; private set; }
    }
}

/*
                // whole
                if (PART_TextBox.SelectionLength == PART_TextBox.Text.Length)
                {
                    PART_TextBox.Text = string.Empty;
                    return;
                }

                // part
                int index = PART_TextBox.CaretIndex;                
                // make selection of length 1
                if (PART_TextBox.SelectionLength == 0)
                {                    
                    if (e.Key == Key.Delete)
                    {
                        MoveCaret();
                        if (PART_TextBox.CaretIndex == PART_TextBox.Text.Length)
                            return;
                    }
                    else if (e.Key == Key.Back)
                    {
                        MoveCaret(MoveDirection.BackwardEdit);
                    }
                    PART_TextBox.SelectionStart = PART_TextBox.CaretIndex; // selection one char
                    index = PART_TextBox.CaretIndex;    // update index for future use
                    PART_TextBox.SelectionLength = 1;
                }
                
                // subtitute selected text with 0s
                if (PART_TextBox.SelectedText.Length > 0 && PART_TextBox.SelectionLength < PART_TextBox.Text.Length)
                {
                    PART_TextBox.Text = ReplaceWithZero(PART_TextBox.Text, PART_TextBox.SelectedText, PART_TextBox.SelectionStart);                    
                }

                // reset caret
                if (e.Key == Key.Back)
                {
                    PART_TextBox.CaretIndex = index;
                    MoveCaret();
                }                    
                else if (e.Key == Key.Delete)
                {
                    PART_TextBox.CaretIndex = ++index;
                    MoveCaret();
                }
                */
