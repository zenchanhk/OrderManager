using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Newtonsoft.Json;
using AmiBroker.OrderManager;
using System.Timers;

namespace AmiBroker.Controllers
{
    public enum EditEndingAction
    {
        Save=2,
        Cancel=1,
        Netural=0
    }
    public enum TemplateAction
    {
        Save=0,
        Open=1,
        Manage=2
    }
    public class OptionTemplate : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }

        public void ForceUpdateContent()
        {
            dynamic tmp = Content;
            Content = null;
            Content = tmp;
        }

        private string _pName;
        public string Name
        {
            get { return _pName; }
            set
            {
                if (_pName != value)
                {
                    _pName = value;
                    OnPropertyChanged("Name");
                }
            }
        }

        public string TypeName { get; set; }
        private string _str;
        public string ContentAsString {
            get => _str;
            set 
            { 
                if (_str != value)
                {
                    _str = value;
                    OnPropertyChanged("ContentAsString");
                }     
                if (value != null)
                {
                    Type t = typeof(SSBase);
                    string ns = t.Namespace;    // get namespace
                    Content = JsonConvert.DeserializeObject(value, Type.GetType(ns + "." + TypeName),
                                                            JSONConstants.saveSerializerSettings);
                }
            }
        }

        private dynamic _content;
        [JsonIgnore]
        public dynamic Content
        {
            get => _content;
            set
            {
                //if (_content != value) // cannot be activate here
                {
                    _content = value;
                    _str = JsonConvert.SerializeObject(value, JSONConstants.saveSerializerSettings);
                    OnPropertyChanged("Content");
                }                
            }
        }

        private DateTime _pModifiedDate;
        public DateTime ModifiedDate
        {
            get { return _pModifiedDate; }
            set
            {
                if (_pModifiedDate != value)
                {
                    _pModifiedDate = value;
                    OnPropertyChanged("ModifiedDate");
                }
            }
        }

    }
    public class Directory
    {
        public DrawingImage Icon { get; set; }
        public string Path { get; set; }
        public string Type { get; set; }
    }
    /// <summary>
    /// Interaction logic for SaveLoadTemplate.xaml
    /// </summary>
    public partial class SaveLoadTemplate : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }

        private bool _pIsNameEditing;
        public bool IsNameEditing
        {
            get { return _pIsNameEditing; }
            set
            {
                if (_pIsNameEditing != value)
                {
                    _pIsNameEditing = value;
                    OnPropertyChanged("IsNameEditing");
                }
            }
        }

        private bool _pIsTemplateEditing;
        public bool IsTemplateEditing
        {
            get { return _pIsTemplateEditing; }
            set
            {
                if (_pIsTemplateEditing != value)
                {
                    _pIsTemplateEditing = value;
                    OnPropertyChanged("IsTemplateEditing");
                }
            }
        }

        private EditEndingAction _pTemplateEditEndingAction = EditEndingAction.Netural;
        public EditEndingAction TemplateEditEndingAction
        {
            get { return _pTemplateEditEndingAction; }
            set
            {
                if (_pTemplateEditEndingAction != value)
                {
                    _pTemplateEditEndingAction = value;
                    OnPropertyChanged("TemplateEditEndingAction");
                }
            }
        }


        private Directory _pSelectedDirectory;
        public Directory SelectedDirectory
        {
            get { return _pSelectedDirectory; }
            set
            {
                if (_pSelectedDirectory != value)
                {
                    _pSelectedDirectory = value;
                    OnPropertyChanged("SelectedDirectory");
                    SetProp();
                }
            }
        }

        private string _pPropName;
        public string PropName
        {
            get { return _pPropName; }
            set
            {
                if (_pPropName != value)
                {
                    _pPropName = value;
                    TemplateList = JsonConvert.DeserializeObject<ObservableCollection<OptionTemplate>>(Properties.Settings.Default[PropName].ToString());
                    OnPropertyChanged("PropName");
                    OnPropertyChanged("TemplateList");
                }
            }
        }

        private string _pHeader;
        public string Header
        {
            get { return _pHeader; }
            set
            {
                if (_pHeader != value)
                {
                    _pHeader = value;
                    OnPropertyChanged("Header");
                }
            }
        }

        private TemplateAction _pTemplateAction;
        public TemplateAction TemplateAction
        {
            get { return _pTemplateAction; }
            set
            {
                if (_pTemplateAction != value)
                {
                    _pTemplateAction = value;
                    OnPropertyChanged("TemplateAction");
                }
            }
        }

        private OptionTemplate _pSelectedTemplate;
        public OptionTemplate SelectedTemplate
        {
            get { return _pSelectedTemplate; }
            set
            {
                if (_pSelectedTemplate != value)
                {
                    _pSelectedTemplate = value;
                    if (value != null)
                        TemplateName = SelectedTemplate.Name;
                    else
                        TemplateName = string.Empty;
                    OnPropertyChanged("SelectedTemplate");
                }
            }
        }

        private string _pTemplateName;
        public string TemplateName
        {
            get { return _pTemplateName; }
            set
            {
                if (_pTemplateName != value)
                {
                    _pTemplateName = value;
                    OnPropertyChanged("TemplateName");
                }
            }
        }
        public ObservableCollection<OptionTemplate> TemplateList { get; set; }
        public dynamic SaveItem { get; private set; }
        public List<Directory> Directories { get; } = new List<Directory>();
        public MainViewModel MainVM { get; set; }
        public SaveLoadTemplate(TemplateAction action, dynamic item = null)
        {
            InitializeComponent();
            Directories.Add(new Directory { Icon = (DrawingImage)this.FindResource("stockDrawingImage"),
                Path = "Contracts", Type = "SymbolInAction" });
            Directories.Add(new Directory { Icon = (DrawingImage)this.FindResource("scriptDrawingImage"),
                Path = "Scripts", Type = "Script" });
            Directories.Add(new Directory { Icon = (DrawingImage)this.FindResource("StrategyDrawingImage"),
                Path = "Strategies", Type = "Strategy" });

            string type = null;
            if (item != null)
            {
                SaveItem = item;
                type = item.GetType().Name;
                PropName = type + "Templates";
                this.Title = type + " - " + (action == TemplateAction.Open ? "Select" : "Save") + " Template";

                if (type.Contains("Symbol"))
                    Header = "Symbol Name";
                else
                    Header = type + " Name";

                TemplateName = item.Name + "." + DateTime.Now.ToString("yyyyMMdd.HHmm");
                SelectedDirectory = Directories.FirstOrDefault(x => x.Type == type);
            }
            else // TemplateAction = Manage
            {
                this.Title = "Manage Templates";
                this.SelectedDirectory = Directories[0];
                SetProp();
            }

            DataContext = this;
            MainVM = MainViewModel.Instance;
            TemplateAction = action;
            this.Icon = new Image { Source = (DrawingImage)this.FindResource("templateDrawingImage") }.Source;
        }

        private void SetProp()
        {
            PropName = this.SelectedDirectory.Type + "Templates";
            if (this.SelectedDirectory.Type.Contains("Symbol"))
                Header = "Symbol Name";
            else
                Header = this.SelectedDirectory.Type + " Name";
        }

        private void Save()
        {
            if (IsNameEditing)
            {
                IsNameEditing = false;
                MainVM.Commands.RenameTemplate.Execute(this);
            }            
        }

        public void StartEditing()
        {
            IsNameEditing = true;
            ListViewItem lvi = lv.ItemContainerGenerator.ContainerFromItem(SelectedTemplate) as ListViewItem;
            TextBox txt = UITreeHelper.GetDescendantByType(lvi, typeof(TextBox), "_lv_txt") as TextBox;
            if (lvi != null)
            {
                Task.Delay(100).ContinueWith(_ =>
                {
                    lvi.Dispatcher.Invoke(() =>
                    {
                        txt.Focus();
                        txt.SelectAll();
                    });
                });                
            }
        }

        private void _lv_txt_LostFocus(object sender, RoutedEventArgs e)
        {
            Save();
        }

        private void _lv_tb_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            TextBlock tb = sender as TextBlock;
            ListViewItem lvi = UITreeHelper.FindParent<ListViewItem>(tb);
            Grid grid = UITreeHelper.FindParent<Grid>(tb);
            TextBox txt = UITreeHelper.FindChild<TextBox>(grid);
            if (lvi != null)
            {
                if (lvi.DataContext == SelectedTemplate)
                {
                    IsNameEditing = true;
                    Task.Delay(10).ContinueWith(_ =>
                    {
                        lvi.Dispatcher.Invoke(() =>
                        {
                            txt.Focus();
                            txt.SelectAll();
                        });
                    });                    
                }
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (TemplateAction == TemplateAction.Manage)
                DialogResult = true;
            else
                DialogResult = false;
            Close();
        }

        Timer t = new Timer();
        private bool _textboxClicked = false;
        private void TemplateWin_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            _textboxClicked = false;
            if (IsNameEditing)
            {
                Task.Delay(200).ContinueWith(_ =>
                {
                    if (!_textboxClicked)
                    {
                        this.Dispatcher.Invoke(() =>
                        {
                            Save();
                        });
                    }
                });
            }
        }

        private void _lv_txt_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            _textboxClicked = true;
        }

        private void _lv_txt_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Save();
            }
        }
    }
}
