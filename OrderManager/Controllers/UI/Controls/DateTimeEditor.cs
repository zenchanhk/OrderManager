using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.ComponentModel;
using System.Reflection;

namespace ControlLib
{
    public class DateTimeEditor : TextBox
    {

        class Part
        {
            public string Name;
            public int Index = -1;
            public string Value = "";
        }

        class DateTimeFormatProvider
        {
            public Regex regex;
            public string DateFormat;
            public string TimeFormat;
        }

        class DateTimeParts
        {
            public Part DatePart = new Part() { Name = "dd" };
            public Part MonthPart = new Part() { Name = "MM" };
            public Part YearPart = new Part() { Name = "yyyy" };
            public Part HourPart = new Part() { Name = "HH" };
            public Part MinPart = new Part() { Name = "mm" };
            public Part SecPart = new Part() { Name = "ss" };
            public IList<Part> Parts = new List<Part>();

            public DateTimeParts()
            {
                Parts.Add(DatePart);
                Parts.Add(MonthPart);
                Parts.Add(YearPart);
                Parts.Add(HourPart);
                Parts.Add(MinPart);
                Parts.Add(SecPart);
            }
        }

        private static IDictionary<string, DateTimeFormatProvider> formatProvider = new Dictionary<string, DateTimeFormatProvider>();

        #region Properties

        /// <summary>
        /// Gets the MaskTextProvider for the specified Mask
        /// </summary>
        private MaskedTextProvider _maskProvider;
        protected MaskedTextProvider MaskProvider
        {
            get
            {
                if (Mask != null)
                {
                    _maskProvider = new MaskedTextProvider(Mask);
                    _maskProvider.Set(Text);
                    _maskProvider.PromptChar = '0';
                }
                return _maskProvider;
            }
            set
            {

            }
        }
        private DateTimeParts _dtParts = new DateTimeParts();
        /// <summary>
        /// Gets or sets the mask to apply to the textbox
        /// </summary>
        protected string Mask { get; set; }

        /// <summary>
        /// Gets or sets the delimiter
        /// </summary>
        public string Delimiter { get; set; }


        public static readonly DependencyProperty IsNullableProperty =
            DependencyProperty.Register("IsNullable", typeof(bool), typeof(DateTimeEditor),
            new UIPropertyMetadata(false, new PropertyChangedCallback(OnIsNullableChanged)));

        public static readonly DependencyProperty CultureInfoProperty =
            DependencyProperty.Register("CultureInfo", typeof(CultureInfo), typeof(DateTimeEditor),
            new UIPropertyMetadata(new CultureInfo("en-GB"), new PropertyChangedCallback(OnCultureInfoChanged)));

        public static readonly DependencyProperty IsDateShownProperty =
            DependencyProperty.Register("IsDateShown", typeof(bool), typeof(DateTimeEditor),
            new UIPropertyMetadata(true, new PropertyChangedCallback(OnIsDateShownChanged)));

        public static readonly DependencyProperty IsTimeShownProperty =
            DependencyProperty.Register("IsTimeShown", typeof(bool), typeof(DateTimeEditor),
            new UIPropertyMetadata(true, new PropertyChangedCallback(OnIsTimeShownChanged)));

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(DateTime), typeof(DateTimeEditor),
            new UIPropertyMetadata(DateTime.Now, new PropertyChangedCallback(OnValueChanged)));


        #endregion

        #region Callback functions
        private static void OnIsNullableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {

        }
        private static void OnCultureInfoChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {

        }
        private static void OnIsDateShownChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue != (bool)e.OldValue)
            {
                DateTimeEditor dt = d as DateTimeEditor;
                if ((bool)e.NewValue)
                {
                    if (string.IsNullOrEmpty(dt.dtFormat))
                        dt.dtFormat = DateTimeEditor.formatProvider[dt.CultureInfo.Name].DateFormat;
                    else
                        dt.dtFormat = DateTimeEditor.formatProvider[dt.CultureInfo.Name].DateFormat
                            + " " + DateTimeEditor.formatProvider[dt.CultureInfo.Name].TimeFormat;
                }
                else
                {
                    dt.dtFormat = DateTimeEditor.formatProvider[dt.CultureInfo.Name].TimeFormat;
                }
                dt.Mask = dt.dtFormat.Replace('H', '0').Replace('M', '0').Replace('d', '0').Replace('m', '0').Replace('y', '0').Replace('s', '0');
                if (!dt.IsNullable)
                    dt.Value = DateTime.Now;
                dt.RefreshText(dt.MaskProvider, 0);
            }
        }
        private static void OnIsTimeShownChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue != (bool)e.OldValue)
            {
                DateTimeEditor dt = d as DateTimeEditor;
                if ((bool)e.NewValue)
                {
                    if (string.IsNullOrEmpty(dt.dtFormat))
                        dt.dtFormat = DateTimeEditor.formatProvider[dt.CultureInfo.Name].TimeFormat;
                    else
                        dt.dtFormat = DateTimeEditor.formatProvider[dt.CultureInfo.Name].DateFormat
                            + " " + DateTimeEditor.formatProvider[dt.CultureInfo.Name].TimeFormat;
                }
                else
                {
                    dt.dtFormat = DateTimeEditor.formatProvider[dt.CultureInfo.Name].DateFormat;
                }
                dt.Mask = dt.dtFormat.Replace('H', '0').Replace('M', '0').Replace('d', '0').Replace('m', '0').Replace('y', '0').Replace('s', '0');
                if (!dt.IsNullable)
                    dt.Value = DateTime.Now;
                else
                    dt.Value = DateTime.Parse("1/1/0001 00:00:00");
                dt.RefreshText(dt.MaskProvider, 0);
            }
        }
        private static bool isValueChanging;
        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!isValueChanging)
            {
                isValueChanging = true;
                DateTimeEditor dt = d as DateTimeEditor;
                if (dt != null)
                {
                    if (((DateTime)e.NewValue).ToString("dd/MM/yyyy HH:mm:ss") != "1/1/0001 00:00:00")
                    {
                        dt.Text = ((DateTime)e.NewValue).ToString(dt.dtFormat, dt.CultureInfo);
                        dt.RefreshText(dt.MaskProvider, 0);
                    }
                    else
                        dt.Text = "";
                }
                isValueChanging = false;
            }
        }
        #endregion

        private string dtFormat;
        private DateTime lastValidValue;
        //public string DateTimeFormat { get { return dtFormat; } }

        ///<summary>
        /// Default constructor
        ///</summary>
        public DateTimeEditor()
        {
            //cancel the paste and cut command
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Paste, null, CancelCommand));
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Cut, null, CancelCommand));
            if (IsDateShown)
                dtFormat = DateTimeEditor.formatProvider[CultureInfo.Name].DateFormat;
            if (IsTimeShown)
            {
                if (string.IsNullOrEmpty(dtFormat))
                    dtFormat = DateTimeEditor.formatProvider[CultureInfo.Name].TimeFormat;
                else
                    dtFormat = dtFormat + " " + DateTimeEditor.formatProvider[CultureInfo.Name].TimeFormat;
            }
            Mask = dtFormat.Replace('H', '0').Replace('M', '0').Replace('d', '0').Replace('m', '0').Replace('y', '0').Replace('s', '0');
            if (!IsNullable)
                this.Value = DateTime.Now;
            RefreshText(MaskProvider, 0);
            if (dtFormat.IndexOf("yyyy") != -1)
                _dtParts.YearPart.Name = "yyyy";
            else if (dtFormat.IndexOf("yy") != -1)
                _dtParts.YearPart.Name = "yy";
            else
                throw new Exception("Year format must be yyyy or yy.");

            // fetch TextEditor from myTextBox
            PropertyInfo textEditorProperty = typeof(TextBox).GetProperty("TextEditor", BindingFlags.NonPublic | BindingFlags.Instance);
            object textEditor = textEditorProperty.GetValue(this, null);

            // set _OvertypeMode on the TextEditor
            PropertyInfo overtypeModeProperty = textEditor.GetType().GetProperty("_OvertypeMode", BindingFlags.NonPublic | BindingFlags.Instance);
            overtypeModeProperty.SetValue(textEditor, true, null);
        }
        //cancel the command
        private static void CancelCommand(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = false;
            e.Handled = true;
        }

        /// <summary>
        /// Static Constructor
        /// </summary>
        static DateTimeEditor()
        {
            //override the meta data for the Text Proeprty of the textbox
            FrameworkPropertyMetadata metaData = new FrameworkPropertyMetadata();
            metaData.CoerceValueCallback = ForceText;
            TextProperty.OverrideMetadata(typeof(DateTimeEditor), metaData);

            formatProvider.Add("en-GB", new DateTimeFormatProvider()
            {
                regex = new Regex(@"^(?=\d)(?:(?:31(?!.(?:0?[2469]|11))|(?:30|29)(?!.0?2)|29(?=.0?2.(?:(?:(?:1[6-9]|[2-9]\d)?(?:0[48]|[2468][048]|[13579][26])|(?:(?:16|[2468][048]|[3579][26])00)))(?:\x20|$))|(?:2[0-8]|1\d|0?[1-9]))([-./])(?:1[012]|0?[1-9])\1(?:1[6-9]|[2-9]\d)?\d\d(?:(?=\x20\d)\x20|$))?(((0?[1-9]|1[012])(:[0-5]\d){0,2}(\x20[AP]M))|([01]\d|2[0-3])(:[0-5]\d){1,2})?$"),
                DateFormat = @"dd/MM/yyyy",
                TimeFormat = "HH:mm:ss"
            });
            formatProvider.Add("en-UK", new DateTimeFormatProvider()
            {
                regex = new Regex(@"(?n:^(?=\d)((?<month>(0?[13578])|1[02]|(0?[469]|11)(?!.31)|0?2(?(.29)(?=.29.((1[6-9]|[2-9]\d)(0[48]|[2468][048]|[13579][26])|(16|[2468][048]|[3579][26])00))|(?!.3[01])))(?<sep>[-./])(?<day>0?[1-9]|[12]\d|3[01])\k<sep>(?<year>(1[6-9]|[2-9]\d)\d{2})(?(?=\x20\d)\x20|$))?(?<time>((0?[1-9]|1[012])(:[0-5]\d){0,2}(?i:\x20[AP]M))|([01]\d|2[0-3])(:[0-5]\d){1,2})?$) "),
                DateFormat = @"MM/dd/yyyy",
                TimeFormat = "HH:mm:ss"
            });
        }
        //force the text of the control to use the mask
        private static object ForceText(DependencyObject sender, object value)
        {
            if (!isValueChanging)
            {
                DateTimeEditor textBox = (DateTimeEditor)sender;
                if (textBox.Mask != null)
                {
                    MaskedTextProvider provider = textBox.MaskProvider;
                    provider.Set((string)value);
                    isValueChanging = true; //prevent endless loop
                    if (value == null || string.IsNullOrEmpty((string)value))
                        if (textBox.IsNullable)
                            textBox.Value = DateTime.Parse("1/1/0001 00:00:00");
                        else
                            throw new Exception("Only IsNullable is set to true, Text can be empty.");
                    else
                    {
                        DateTime result = new DateTime();
                        if (!DateTime.TryParse((string)value, textBox.CultureInfo, DateTimeStyles.None, out result))
                            textBox.lastValidValue = textBox.Value;
                        else
                            textBox.Value = result;
                    }
                    isValueChanging = false;
                    if (value == null || string.IsNullOrEmpty((string)value))
                        return "";
                    else
                        return provider.ToDisplayString();
                }
            }
            return value;
        }


        /// <summary>
        /// override this method to replace the characters enetered with the mask
        /// </summary>
        /// <param name=”e”>Arguments for event</param>
        protected override void OnPreviewTextInput(TextCompositionEventArgs e)
        {
            int position = SelectionStart;
            if (IsNullable && string.IsNullOrEmpty(this.Text))
                this.Value = DateTime.Now;

            if (!IsNullable && string.IsNullOrEmpty(this.Text))
                throw new Exception("Only IsNullable is set to true, Text can be empty.");

            MaskedTextProvider provider = MaskProvider;
            if (position < Text.Length)
            {
                position = GetNextCharacterPosition(position);
                //if (!Keyboard.IsKeyToggled(Key.Insert))
                //    PressKey(Key.Insert);
                //InputSimulator.SimulateKeyPress(VirtualKeyCode.INSERT);

                //if (Keyboard.IsKeyToggled(Key.Insert))
                {
                    if (e.Text == "0")
                    {
                        if (provider.Replace(e.Text, position))
                        {
                            position++;
                            position = GetNextCharacterPosition(position);
                            RefreshText(provider, position);
                        }
                    }
                    else
                    {
                        string result = FillValidDate(IsAnyInvalidPart(),
                            (Text.Substring(0, position) + e.Text + Text.Substring(position + 1)).ToString());
                        if (DateTimeEditor.formatProvider[CultureInfo.Name].regex.IsMatch(result))
                        {
                            if (provider.Replace(e.Text, position))
                            {
                                position++;
                                position = GetNextCharacterPosition(position);
                                RefreshText(provider, position);
                            }
                        }
                    }
                }
                //else
                //{
                    //throw new Exception("Insert mode is broken.");
                //}
            }
            e.Handled = true;
            base.OnPreviewTextInput(e);
        }

        private void PressKey(Key key)
        {
            KeyEventArgs eInsertBack = new KeyEventArgs(Keyboard.PrimaryDevice, Keyboard.PrimaryDevice.ActiveSource, 0, key);
            eInsertBack.RoutedEvent = KeyDownEvent;
            InputManager.Current.ProcessInput(eInsertBack);
        }

        /// <summary>
        /// override to enforce entering insert mode
        /// </summary>
        /// <param name="e">Arguments for the event</param>
        protected override void OnGotFocus(RoutedEventArgs e)
        {
            Console.WriteLine(DateTime.Now.ToString());
            base.OnGotFocus(e);
            if (!Keyboard.IsKeyToggled(Key.Insert))
            {
                //InputSimulator.SimulateKeyPress(VirtualKeyCode.INSERT);
                //PressKey(Key.Insert);
            }

        }

        protected override void OnLostFocus(RoutedEventArgs e)
        {
            base.OnLostFocus(e);
            DateTime result = new DateTime();
            if (!DateTime.TryParse(Text, CultureInfo, DateTimeStyles.None, out result) && !IsNullable)
                Value = this.lastValidValue;
        }

        /// <summary>
        /// override the key down to handle delete of a character
        /// </summary>
        /// <param name="e">Arguments for the event</param>
        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            //eat the keystroke if insert is pressed
            //if (!Keyboard.IsKeyToggled(Key.Insert))
            //{
                //InputSimulator.SimulateKeyPress(VirtualKeyCode.INSERT);
            //    PressKey(Key.Insert);
            //}

            MaskedTextProvider provider = MaskProvider;
            int position = SelectionStart;
            if (e.Key == Key.Delete && position < Text.Length)//handle the delete key
            {
                int tmp = GetNextCharacterPosition(position);
                if (provider.Replace(MaskProvider.PromptChar, tmp))
                    RefreshText(provider, ++tmp);

                e.Handled = true;
            }

            else if (e.Key == Key.Back)//handle the back space
            {
                if (position > 0)
                {
                    position = GetPreivousCharacterPosition(position);
                    if (provider.Replace(MaskProvider.PromptChar, position))
                        RefreshText(provider, position);
                }
                if (position == 0 && IsNullable)
                    Text = "";
                e.Handled = true;
            }

            else if (e.Key == Key.Left)
            {
                if (position > 0)
                {
                    position = GetPreivousCharacterPosition(position);
                    SelectionStart = position;
                }
                e.Handled = true;
            }

            else if (e.Key == Key.Space || e.Key == Key.Insert)
            {
                e.Handled = true;
            }

            else if (e.Key == Key.Right)
            {
                if (position >= 0)
                {
                    position = GetNextCharacterPosition(++position);
                    SelectionStart = position;
                }
                e.Handled = true;
            }
            base.OnPreviewKeyDown(e);
        }


        #region Helper Methods

        private IList<Part> IsAnyInvalidPart()
        {
            List<Part> parts = new List<Part>();
            foreach (var part in _dtParts.Parts)
            {
                int result;
                result = Int32.Parse(part.Value);
                if (part.Index != -1 && result == 0 &&
                    (part.Name == "yyyy" || part.Name == "dd" || part.Name == "MM" || part.Name == "yy"))
                    parts.Add(part);
            }
            return parts;
        }

        private string FillValidDate(IList<Part> parts, string orgValue)
        {
            string result = orgValue;
            if (parts.Count > 0)
            {
                //remove the part is in editing
                int currentPos = GetNextCharacterPosition(SelectionStart);
                for (int i = 0; i < parts.Count; i++)
                {
                    if (currentPos >= parts[i].Index && currentPos <= parts[i].Index + parts[i].Name.Length)
                        parts.Remove(parts[i]);
                }

                foreach (var part in parts)
                {
                    switch (part.Name)
                    {
                        case "yy":
                            if (part.Index > 0)
                                result = result.Substring(0, part.Index) + "01" + result.Substring(part.Index + part.Name.Length);
                            else
                                result = "01" + result.Substring(part.Index + part.Name.Length);
                            break;
                        case "yyyy":
                            if (part.Index > 0)
                                result = result.Substring(0, part.Index) + "2001" + result.Substring(part.Index + part.Name.Length);
                            else
                                result = "2001" + result.Substring(part.Index + part.Name.Length);
                            break;
                        case "MM":
                            if (part.Index > 0)
                                result = result.Substring(0, part.Index) + "01" + result.Substring(part.Index + part.Name.Length);
                            else
                                result = "01" + result.Substring(part.Index + part.Name.Length);
                            break;
                        case "dd":
                            if (part.Index > 0)
                                result = result.Substring(0, part.Index) + "01" + result.Substring(part.Index + part.Name.Length);
                            else
                                result = "01" + result.Substring(part.Index + part.Name.Length);
                            break;
                    }
                }
            }

            return result;
        }

        //refreshes the text of the textbox
        private void RefreshText(MaskedTextProvider provider, int position)
        {
            Text = provider.ToDisplayString();
            SelectionStart = position;
            foreach (var part in _dtParts.Parts)
            {
                int index = dtFormat.IndexOf(part.Name);
                if (index != -1)
                {
                    part.Index = index;
                    part.Value = Text.Substring(index, part.Name.Length);
                }
            }
        }
        //gets the next position in the textbox to move
        private int GetNextCharacterPosition(int startPosition)
        {
            int position = MaskProvider.FindEditPositionFrom(startPosition, true);
            if (position == -1)
                return startPosition;
            else
                return position;
        }
        //gets the next position in the textbox to move
        private int GetPreivousCharacterPosition(int startPosition)
        {
            int position = -1;
            for (int i = startPosition - 1; i >= 0; i--)
            {
                position = MaskProvider.FindEditPositionFrom(i, true);
                if (position < startPosition && position != -1)
                    return position;
            }
            return startPosition;
        }
        //gets the value of various parts
        private int GetPartValue(string partName)
        {
            int index = dtFormat.IndexOf(partName);
            if (index != -1)
            {
                int result = -1;
                if (!Int32.TryParse(Text.Substring(index, partName.Length), out result))
                    return -1;
            }
            return -1;
        }
        #endregion

        /// <summary>
        /// Gets or sets the date/time is nullable
        /// </summary>
        public bool IsNullable
        {
            get { return (bool)GetValue(IsNullableProperty); }
            set { SetValue(IsNullableProperty, value); }
        }

        public CultureInfo CultureInfo
        {
            get { return (CultureInfo)GetValue(CultureInfoProperty); }
            set { SetValue(CultureInfoProperty, value); }
        }

        public bool IsDateShown
        {
            get { return (bool)GetValue(IsDateShownProperty); }
            set { SetValue(IsDateShownProperty, value); }
        }

        public bool IsTimeShown
        {
            get { return (bool)GetValue(IsTimeShownProperty); }
            set { SetValue(IsTimeShownProperty, value); }
        }

        public DateTime Value
        {
            set { SetValue(ValueProperty, value); }
            get { return (DateTime)GetValue(ValueProperty); }
        }
    }
}

