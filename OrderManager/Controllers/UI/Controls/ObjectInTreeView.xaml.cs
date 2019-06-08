using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using AmiBroker.Controllers;
using AmiBroker.OrderManager;
using Newtonsoft.Json.Serialization;
using System.Text.RegularExpressions;
using System.Collections.ObjectModel;

namespace ControlLib
{
    public enum Modes
    {
        Read=0,
        Edit=1,
        Save=2
    }
    public class ReadSelector : DataTemplateSelector
    {
        static ObjectInTreeView VisualUpwardSearch(DependencyObject source)
        {
            while (source != null && !(source is ObjectInTreeView))
                source = VisualTreeHelper.GetParent(source);

            return source as ObjectInTreeView;
        }
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            ObjectInTreeView d = VisualUpwardSearch(container);
            Type t = (item as TreeNode).Type;
            if (t == typeof(DateTime))
                return d.FindResource("DateTimeReadOnly") as DataTemplate;
            else if (((dynamic)item).Name == "ActionType")
                return d.FindResource("ActionType") as DataTemplate;
            else
                return d.FindResource("ReadOnly") as DataTemplate;
        }
    }
    public class EditSelector : DataTemplateSelector
    {
        static ObjectInTreeView VisualUpwardSearch(DependencyObject source)
        {
            while (source != null && !(source is ObjectInTreeView))
                source = VisualTreeHelper.GetParent(source);

            return source as ObjectInTreeView;
        }
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            ObjectInTreeView d = VisualUpwardSearch(container);
                Type t = (item as TreeNode).Type;
            if (t == null)
            {
                return d.FindResource("ReadOnly") as DataTemplate;
            }
            else if (t != null)
            {
                if (((dynamic)item).Name == "Name" || ((dynamic)item).Name == "Vendor" ||
                     ((dynamic)item).Name == "DisplayName" || ((dynamic)item).Name.StartsWith("$"))
                {
                    return d.FindResource("ReadOnly") as DataTemplate;
                }
                else if (((dynamic)item).Name == "ActionType")
                {
                    return d.FindResource("ActionType") as DataTemplate;
                }
                else if (t == typeof(string))
                {
                    return d.FindResource("Text") as DataTemplate;
                }
                else if (t == typeof(int) || t == typeof(Nullable<int>))
                {
                    return d.FindResource("Integer") as DataTemplate;
                }
                else if (t == typeof(float) || t == typeof(Nullable<float>)
                    || t == typeof(double) || t == typeof(Nullable<double>)
                    || t == typeof(decimal) || t == typeof(Nullable<decimal>))
                {
                    return d.FindResource("Decimal") as DataTemplate;
                }
                else if (t == typeof(bool) || t == typeof(Nullable<bool>))
                {
                    return d.FindResource("Bool") as DataTemplate;
                }
                else if (t == typeof(DateTime) || t == typeof(Nullable<DateTime>))
                {
                    return d.FindResource("DateTime") as DataTemplate;
                }
                else
                {
                    return d.FindResource("ReadOnly") as DataTemplate;
                }
            }
            else
            {
                return d.FindResource("ReadOnly") as DataTemplate;
            }
        }
    }
    /// <summary>
    /// Interaction logic for ObjectInTreeView.xaml
    /// </summary>
    public partial class ObjectInTreeView : UserControl
    {
        private object _oldObj;
        private object _newObj;
        public object SavedObject { get => _newObj; private set { _newObj = value; } }
        public ObjectInTreeView()
        {
            InitializeComponent();
        }

        public object ObjectToVisualize
        {
            get { return (object)GetValue(ObjectToVisualizeProperty); }
            set { SetValue(ObjectToVisualizeProperty, value); }
        }
        public static readonly DependencyProperty ObjectToVisualizeProperty =
            DependencyProperty.Register("ObjectToVisualize", typeof(object), typeof(ObjectInTreeView), new PropertyMetadata(null, OnObjectChanged));

        private static void OnObjectChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TreeNode tree = TreeNode.CreateTree(e.NewValue);
            if ((d as ObjectInTreeView).Filter != null && (d as ObjectInTreeView).Filter != string.Empty)
                TreeNode.FilterNode(null, tree, (d as ObjectInTreeView).Filter);
            (d as ObjectInTreeView).TreeNodes = new List<TreeNode>() { tree };
            (d as ObjectInTreeView)._oldObj = e.NewValue;
        }

        public Modes Mode
        {
            get { return (Modes)GetValue(ModeProperty); }
            set { SetValue(ModeProperty, value); }
        }
        public static readonly DependencyProperty ModeProperty =
            DependencyProperty.Register("Mode", typeof(Modes), typeof(ObjectInTreeView), new PropertyMetadata(Modes.Read, OnModeChanged));

        private static void OnModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ObjectInTreeView oit = d as ObjectInTreeView;
            if ((Modes)e.NewValue == Modes.Save)
            {
                WriteBack(oit._oldObj, oit.TreeNodes[0]);
            }
        }

        public string Filter
        {
            get { return (string)GetValue(FilterProperty); }
            set { SetValue(FilterProperty, value); }
        }
        public static readonly DependencyProperty FilterProperty =
            DependencyProperty.Register("Filter", typeof(string), typeof(ObjectInTreeView), new PropertyMetadata(null, OnFilterChanged));

        private static DebounceDispatcher debounceTimer = new DebounceDispatcher();
        private static void OnFilterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            debounceTimer.Debounce(500, parm =>
            {
                if ((e.NewValue == null || (string)e.NewValue == string.Empty) &&
                (e.OldValue != null && (string)e.OldValue == string.Empty)
                && e.NewValue != e.OldValue)
                {
                    TreeNode tree = TreeNode.CreateTree((d as ObjectInTreeView).ObjectToVisualize);
                    (d as ObjectInTreeView).TreeNodes = new List<TreeNode>() { tree };
                }
                if (e.NewValue != null && (string)e.NewValue != string.Empty && (d as ObjectInTreeView).ObjectToVisualize != null)
                {
                    if ((string)e.NewValue == "!") return;
                    TreeNode tree = TreeNode.CreateTree((d as ObjectInTreeView).ObjectToVisualize);
                    TreeNode.FilterNode(null, tree, (d as ObjectInTreeView).Filter);
                    (d as ObjectInTreeView).TreeNodes = new List<TreeNode>() { tree };
                }
            });                        
        }

        private static void WriteBack(object obj, TreeNode treeNode)
        {
            try
            {
                PropertyInfo pi = null;
                foreach (TreeNode node in treeNode.Children)
                {
                    pi = obj.GetType().GetProperty(node.Name);
                    MethodInfo setter = pi != null ? pi.GetSetMethod(/*nonPublic*/ true) : null;
                    if (pi == null || setter == null)
                    {
                        int i = 0;
                    }
                    else
                    {
                        if (pi.PropertyType.FullName.Contains("ObservableCollection") ||
                    pi.PropertyType.FullName.Contains("List"))
                        {
                            for (int i = 0; i < node.Children.Count; i++)
                            {
                                WriteBack(((dynamic)pi.GetValue(obj))[i], node.Children[i]);
                            }
                        }
                        else if (!pi.PropertyType.Namespace.StartsWith("System") && !pi.PropertyType.IsEnum)
                        {
                            WriteBack(pi.GetValue(obj), node);
                        }
                        else
                        {
                            if (node.Value != null && !pi.PropertyType.IsEnum && pi.PropertyType != node.Value.GetType())
                            {
                                if (!pi.PropertyType.FullName.ToLower().Contains("nullable"))
                                {
                                    dynamic tmp = Convert.ChangeType(node.Value, Type.GetType(pi.PropertyType.FullName));
                                    pi.SetValue(obj, tmp);
                                }
                                else
                                    pi.SetValue(obj, node.Value);
                            }
                            else
                                pi.SetValue(obj, node.Value);
                        }                
                    }                    
                }
            }
            catch (Exception e)
            {
                string s = e.Message;
            }
            
        }

        public List<TreeNode> TreeNodes
        {
            get { return (List<TreeNode>)GetValue(TreeNodesProperty); }
            set { SetValue(TreeNodesProperty, value); }
        }
        public static readonly DependencyProperty TreeNodesProperty =
            DependencyProperty.Register("TreeNodes", typeof(List<TreeNode>), typeof(ObjectInTreeView), new PropertyMetadata(null));
        
        public void Refresh()
        {
            TreeNode tree = null;
            if (ObjectToVisualize != null)
                tree = TreeNode.CreateTree(ObjectToVisualize);
            if (Filter != null && Filter != string.Empty && ObjectToVisualize != null)
                TreeNode.FilterNode(null, tree, Filter);
            if (tree != null)
                TreeNodes = new List<TreeNode>() { tree };
        }
    }
    
    public class TreeNode
    {
        public string Name { get; set; }
        public dynamic Value { get; set; }
        public Type Type { get; set; }
        public List<TreeNode> Children { get; set; } = new List<TreeNode>();

        public static TreeNode CreateTree(object obj)
        {
            var jsonSerializerSettings = new JsonSerializerSettings()
            {
                PreserveReferencesHandling = PreserveReferencesHandling.None,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Formatting = Formatting.Indented
                //TypeNameHandling = TypeNameHandling.Auto,
                //PreserveReferencesHandling = PreserveReferencesHandling.Objects
            };            
            JavaScriptSerializer jss = new JavaScriptSerializer();
            var serialized = JsonConvert.SerializeObject(obj, JSONConstants.displaySerializerSettings);
            //System.Diagnostics.Debug.WriteLine(serialized);
            Dictionary<string, object> dic = jss.Deserialize<Dictionary<string, object>>(serialized);
            //Dictionary<string, object> dic = JsonConvert.DeserializeObject<Dictionary<string, object>>(serialized);
            var root = new TreeNode();
            root.Name = "Root";
            BuildTree(dic, root, obj);
            PropertyInfo pi = obj?.GetType()?.GetProperty("Name");
            if (pi != null)
                root.Name = (string)pi.GetValue(obj) + "(Root)"; // important for converter to be recognized as root
            return root;
        }
        
        private static string FindName(ArrayList list)
        {
            foreach (object item in list)
            {
                if (((dynamic)item).Key == "Name")
                    return ((dynamic)item).Value;
            }
            return string.Empty;
        }

        private static Type SearchPropertyType(object obj, string name)
        {
            if (obj == null) return typeof(Object);

            PropertyInfo[] pis = obj.GetType().GetProperties();
            PropertyInfo pi = pis.FirstOrDefault(x => x.Name == name);
            if (pi != null)
                return pi.PropertyType;
            else
            {
                if (obj.GetType().Name.Contains("Dictionary"))
                {
                    Type t = typeof(Object);
                    foreach (var item in (IEnumerable)obj)
                    {
                        t = SearchPropertyType(((dynamic)item).Value, name);
                        break;
                    }
                    return t;
                }
                else
                {
                    Exception ex = new Exception("Type not found: object - " + obj.ToString() + "; name - " + name);
                    GlobalExceptionHandler.HandleException("ObjectInTreeView.SearchPropertyType", ex);
                    return null;
                }          
            }
        }

        // obj used to check the type if some property is null since type will miss during object got dictionarized
        private static void BuildTree(object item, TreeNode node, object obj)
        {
            try
            {
                if (item is KeyValuePair<string, object>)
                {
                    KeyValuePair<string, object> kv = (KeyValuePair<string, object>)item;
                    TreeNode keyValueNode = new TreeNode();
                    keyValueNode.Name = kv.Key;

                    DateTime dt = DateTime.Now;
                    if (kv.Value == null)
                    {
                        keyValueNode.Value = kv.Value;
                        keyValueNode.Type = SearchPropertyType(obj, kv.Key);
                    }
                    else
                    {
                        if (DateTime.TryParse(kv.Value.ToString(), out dt))
                        {
                            keyValueNode.Value = dt;
                            keyValueNode.Type = typeof(DateTime);
                        }
                        else
                        {
                            keyValueNode.Value = kv.Value;
                            keyValueNode.Type = kv.Value.GetType();
                        }
                    }
                    node.Children.Add(keyValueNode);
                    BuildTree(kv.Value, keyValueNode, obj);
                }
                else if (item is ArrayList)
                {
                    //ArrayList list = (ArrayList)item;

                    int index = 0;
                    foreach (object value in (IEnumerable)item)
                    {
                        if (value.GetType().Name.Contains("Dictionary"))
                        {
                            TreeNode arrayItem = new TreeNode();
                            Type[] args = value.GetType().GetGenericArguments();
                            if (args[0] == typeof(string))
                            {
                                Dictionary<string, Object> dict = (Dictionary<string, Object>)value;
                                arrayItem.Name = dict.ContainsKey("Name") ? $"[{dict["Name"]}]" : $"[{index}]";
                            }
                            else if (args[0].IsEnum)
                            {
                                arrayItem.Name = ((dynamic)value).Key.ToString();
                            }
                            arrayItem.Type = arrayItem.GetType();
                            arrayItem.Value = "";
                            node.Children.Add(arrayItem);
                            if (obj.GetType().Name.Contains("HashSet"))
                            {
                                object o = null;
                                foreach (var i in (dynamic)obj)
                                {
                                    o = i;
                                    break;
                                }
                                BuildTree(value, arrayItem, o);
                            }                                
                            else
                                BuildTree(value, arrayItem, ((dynamic)obj)[index]);
                            index++;
                        }
                        else
                        {
                            TreeNode arrayItem = new TreeNode();
                            arrayItem.Name = $"[{index}]";
                            arrayItem.Type = arrayItem.GetType();
                            arrayItem.Value = value;
                            node.Children.Add(arrayItem);
                            //BuildTree(value, arrayItem, ((dynamic)obj)[index]);
                            index++;
                        }
                    }
                }
                else if (item is Dictionary<string, object>)
                {
                    Dictionary<string, object> dictionary = (Dictionary<string, object>)item;
                    foreach (KeyValuePair<string, object> d in dictionary)
                    {
                        BuildTree(d, node, Helper.GetValueByName(obj, d.Key));
                    }
                }
            }
            catch (Exception e)
            {
                //throw e;
                GlobalExceptionHandler.HandleException("ObjectInTreeView.BuildTree", e);
            }            
        }

        public static void FilterNode(TreeNode parent, TreeNode child, string filter)
        {
            try
            {
                if (CheckFilter(child.Name, filter)) return;
                for (int i = child.Children.Count - 1; i >= 0; i--)
                {
                    TreeNode node = child.Children[i];
                    if (!CheckFilter(node.Name, filter) && (IsAtomic(node.Type) || (!IsAtomic(node.Type) && node.Children.Count == 0)))
                    {
                        child.Children.Remove(node);
                    }
                    if (node.Children.Count > 0)
                        FilterNode(child, node, filter);
                }
                if (child.Type != null && (IsAtomic(child.Type) || (!IsAtomic(child.Type) && child.Children.Count == 0)))
                {
                    parent.Children.Remove(child);
                }
            }
            catch (Exception ex)
            {
                //throw ex;
                GlobalExceptionHandler.HandleException("ObjectInTreeView.FilterNode", ex);
            }
            
        }

        private static bool IsAtomic(Type t)
        {
            return t.IsPrimitive || t == typeof(decimal) || t == typeof(string) || t == typeof(DateTime);
        }

        private static bool CheckFilter(string name, string filter)
        {
            if (filter != null)
            {
                bool neg = false;
                if (filter[0] == '!')
                {
                    neg = true;
                    filter = filter.Substring(1);
                }                
                string[] words = filter.Split(new char[] { ' ', ',', ';', ':' });
                List<string> vs = new List<string>(words);
                vs = vs.Where(x => x != null && (x.Trim() != string.Empty)).ToList();
                string pattern = string.Join("|", vs.Select(w => Regex.Escape(w)));
                Regex regex = new Regex(pattern, RegexOptions.IgnoreCase);
                if (neg && !regex.IsMatch(name))
                    return true;
                else if (!neg && regex.IsMatch(name))
                    return true;
                else
                    return false;
            }
            else
                return true;
        }

        private static string GetValueAsString(object value)
        {
            if (value == null)
                return "null";
            var type = value.GetType();
            if (type.IsArray)
            {
                return "[]";
            }

            if (value is ArrayList)
            {
                var arr = value as ArrayList;
                return $"[{arr.Count}]";
            }

            if (type.IsGenericType)
            {
                return "{}";
            }

            return value.ToString();
        }
    }
}
