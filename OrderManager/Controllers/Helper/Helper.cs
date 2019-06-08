using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interactivity;
using System.Data;
using System.IO;
using System.Windows.Threading;
using AmiBroker.Utils;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Management;
using FastMember;
using AmiBroker.OrderManager;

namespace AmiBroker.Controllers
{
    public class GlobalExceptionHandler
    {
        private static string working_dir = System.IO.Directory.GetCurrentDirectory();
        private static string log_dir = working_dir + Path.DirectorySeparatorChar + "omlog" + Path.DirectorySeparatorChar;
        public static void HandleException(object sender, Exception ex, EventArgs args = null, string message = null, bool slient = false)
        {
            if (!slient)
                MessageBox.Show((message == null ? ex.Message : message) 
                    + "\nSource: " + ex.Source
                    + "\nSender: " + (sender != null ? sender.ToString() : "")
                    + "\nPlease see the log for details (located at " 
                    + working_dir + ")",
                    "Fatal error", MessageBoxButton.OK, MessageBoxImage.Error);
            //YTrace.Trace(message, YTrace.TraceLevel.Information);
            StringBuilder sb = new StringBuilder();

            if (args != null)
            {
                if (args.GetType() == typeof(UnobservedTaskExceptionEventArgs))
                {
                    sb.AppendLine("UnobservedTaskException: " + ex.GetType().Name);
                    //((UnobservedTaskExceptionEventArgs)args).SetObserved();
                }
                if (args.GetType() == typeof(DispatcherUnhandledExceptionEventArgs))
                {
                    sb.AppendLine("DispatcherUnhandledException: " + ex.GetType().Name);
                    //((DispatcherUnhandledExceptionEventArgs)args).Handled = true;
                }
                if (args.GetType() == typeof(UnhandledExceptionEventArgs))
                {
                    sb.AppendLine("Uncaptured exception for current domain: " + ex.GetType().Name);
                }
            }
            else
            {
                sb.AppendLine("No exception event args");
            }
            

            sb.AppendLine("Sender:" + sender.ToString());
            if (ex != null)
            {
                sb.AppendLine("Exception Message: " + ex.Message);
                if (ex.Source != null) sb.AppendLine("Source: " + ex.Source);
                if (ex.StackTrace != null) sb.AppendLine("StackTrace: " + ex.StackTrace);
                if (ex.InnerException != null)
                {
                    sb.AppendLine("InnerException Message: " + ex.InnerException.Message);
                    if (ex.InnerException.Source != null) sb.AppendLine("InnerException Source: " + ex.InnerException.Source);
                    if (ex.InnerException.StackTrace != null) sb.AppendLine("InnerException StackTrace: " + ex.InnerException.StackTrace);
                }
                // for task AggregateException
                if (ex.GetType() == typeof(AggregateException))
                {
                    foreach (var inner in ((AggregateException)ex).InnerExceptions)
                    {
                        sb.AppendLine("InnerException Message: " + inner.Message);
                        if (inner.Source != null) sb.AppendLine("InnerException Source: " + inner.Source);
                        if (inner.StackTrace != null) sb.AppendLine("InnerException StackTrace: " + inner.StackTrace);
                    }
                }
            }
            Log(sb.ToString());
            
        }

        public static void LogMessage(string ticker, string message)
        {
            // place your logging code here
            try
            {
                string file_path = ticker + ".log";
                StreamWriter sw = new StreamWriter(file_path, true);
                sw.WriteLine(DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToShortTimeString() + ":" + message);
                sw.Close();
            }
            catch (Exception ex)
            {
                AFMisc.Trace("Could not log message for ticker " + ticker + ": " + message);
                AFMisc.Trace(ex.ToString());
            }
        }
        public static void Log(string lines)
        {
            try
            {
                //string file_path = "\"" + string.Format("{0}error{1:yyyyMMdd}.log\"", log_dir, DateTime.Now);
                string file_path = string.Format("error{0:yyyyMMdd}.log", DateTime.Now);
                StreamWriter sw = new StreamWriter(file_path, true);
                sw.WriteLine(DateTime.Now.ToShortTimeString() + ":" + lines);
                sw.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to log message." + "\nException:" + ex.Message, "Log Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
    public static class BaseOrderTypeAccessor
    {
        private static Dictionary<string, TypeAccessor> _accessors = new Dictionary<string, TypeAccessor>();
        
        public static TypeAccessor GetAccessor(string name, string nameSpace)
        {
            if (_accessors.ContainsKey(name))
                return _accessors[name];
            else
            {
                Type type = Type.GetType(nameSpace + "." + name);
                TypeAccessor accessor = TypeAccessor.Create(type);
                _accessors.Add(name, accessor);
                return accessor;
            }
        }

        public static TypeAccessor GetAccessor(object obj)
        {
            Type type = obj.GetType();
            string name = type.FullName;
            if (_accessors.ContainsKey(name))
                return _accessors[name];
            else
            {                
                TypeAccessor accessor = TypeAccessor.Create(type);
                _accessors.Add(name, accessor);
                return accessor;
            }
        }

        public static bool HasProperty(object obj, string propName)
        {
            Type type = obj.GetType();
            string name = type.FullName;
            TypeAccessor accessor = null;
            if (_accessors.ContainsKey(name))
                accessor = _accessors[name];
            else
            {
                TypeAccessor ta = TypeAccessor.Create(type);
                _accessors.Add(name, ta);
                accessor = ta;
            }
            MemberSet members = accessor.GetMembers();
            return members.Any(x => x.Name == propName);
        }

        public static string GetValueByName(object obj, string propName)
        {
            Type type = obj.GetType();
            string name = type.FullName;
            TypeAccessor accessor = null;
            if (_accessors.ContainsKey(name))
                accessor = _accessors[name];
            else
            {
                TypeAccessor ta = TypeAccessor.Create(type);
                _accessors.Add(name, ta);
                accessor = ta;
            }
            MemberSet members = accessor.GetMembers();
            if (members.Any(x => x.Name == propName))
                return (string)accessor[obj, propName];
            else
                return null;
        }

        public static List<string> GetValueByName(object obj, string[] propNames)
        {
            Type type = obj.GetType();
            string name = type.FullName;
            TypeAccessor accessor = null;
            if (_accessors.ContainsKey(name))
                accessor = _accessors[name];
            else
            {
                TypeAccessor ta = TypeAccessor.Create(type);
                _accessors.Add(name, ta);
                accessor = ta;
            }
            MemberSet members = accessor.GetMembers();
            List<string> result = new List<string>();
            foreach (string propName in propNames)
            {
                if (members.Any(x => x.Name == propName))
                    result.Add((string)accessor[obj, propName]);
                else
                    result.Add(null);
            }
            return result;
        }

        public static decimal GetPriceByName(Strategy s, string name)
        {
            decimal d = 0;
            if (string.IsNullOrEmpty(name)) return d;

            if (!decimal.TryParse(name, out d))
                d = s.CurrentPrices[name];
            return d;
        }

        public static List<decimal> GetPriceByName(Strategy s, List<string> names)
        {
            List<decimal> result = new List<decimal>();
            decimal d = 0;
            foreach (var name in names)
            {
                if (string.IsNullOrEmpty(name))
                    result.Add(0);
                else
                {
                    if (decimal.TryParse(name, out d))
                        result.Add(d);
                    else
                        result.Add(s.CurrentPrices[name]);
                }                
            }
            return result;
        }
    }
    public static class StringExt
    {
        public static bool IsNumeric(this string text)
        {
            return double.TryParse(text, out double test);
        }
    }
    public class AsyncSemaphore
    {
        private readonly static Task s_completed = Task.FromResult(true);
        private readonly Queue<TaskCompletionSource<bool>> m_waiters = new Queue<TaskCompletionSource<bool>>();
        private int m_currentCount;

        public AsyncSemaphore(int initialCount)
        {
            if (initialCount < 0) throw new ArgumentOutOfRangeException("initialCount");
            m_currentCount = initialCount;
        }

        public Task WaitAsync()
        {
            lock (m_waiters)
            {
                if (m_currentCount > 0)
                {
                    --m_currentCount;
                    return s_completed;
                }
                else
                {
                    var waiter = new TaskCompletionSource<bool>();
                    m_waiters.Enqueue(waiter);
                    return waiter.Task;
                }
            }
        }

        public void Release()
        {
            TaskCompletionSource<bool> toRelease = null;
            lock (m_waiters)
            {
                if (m_waiters.Count > 0)
                    toRelease = m_waiters.Dequeue();
                else
                    ++m_currentCount;
            }
            if (toRelease != null)
                toRelease.SetResult(true);
        }
    }

    // http://blogs.msdn.com/b/pfxteam/archive/2012/02/12/10266988.aspx
    public class AsyncLock
    {
        private readonly AsyncSemaphore m_semaphore;
        private readonly Task<Releaser> m_releaser;

        public AsyncLock()
        {
            m_semaphore = new AsyncSemaphore(1);
            m_releaser = Task.FromResult(new Releaser(this));
        }

        public Task<Releaser> LockAsync()
        {
            var wait = m_semaphore.WaitAsync();
            return wait.IsCompleted ?
                m_releaser :
                wait.ContinueWith((_, state) => new Releaser((AsyncLock)state),
                    this, CancellationToken.None,
                    TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        }

        public struct Releaser : IDisposable
        {
            private readonly AsyncLock m_toRelease;

            internal Releaser(AsyncLock toRelease) { m_toRelease = toRelease; }

            public void Dispose()
            {
                if (m_toRelease != null)
                    m_toRelease.m_semaphore.Release();
            }
        }
    }
    public class DebounceDispatcher
    {
        private DispatcherTimer timer;
        private DateTime timerStarted { get; set; } = DateTime.UtcNow.AddYears(-1);

        public void Debounce(int interval, Action<object> action,
            object param = null,
            DispatcherPriority priority = DispatcherPriority.ApplicationIdle,
            Dispatcher disp = null)
        {
            // kill pending timer and pending ticks
            timer?.Stop();
            timer = null;

            if (disp == null)
                disp = Dispatcher.CurrentDispatcher;

            // timer is recreated for each event and effectively
            // resets the timeout. Action only fires after timeout has fully
            // elapsed without other events firing in between
            timer = new DispatcherTimer(TimeSpan.FromMilliseconds(interval), priority, (s, e) =>
            {
                if (timer == null)
                    return;

                timer?.Stop();
                timer = null;
                action.Invoke(param);
            }, disp);

            timer.Start();
        }

        public void Throttle(int interval, Action<object> action,
        object param = null,
        DispatcherPriority priority = DispatcherPriority.ApplicationIdle,
        Dispatcher disp = null)
        {
            // kill pending timer and pending ticks
            timer?.Stop();
            timer = null;

            if (disp == null)
                disp = Dispatcher.CurrentDispatcher;

            var curTime = DateTime.UtcNow;

            // if timeout is not up yet - adjust timeout to fire 
            // with potentially new Action parameters           
            if (curTime.Subtract(timerStarted).TotalMilliseconds < interval)
                interval = (int)curTime.Subtract(timerStarted).TotalMilliseconds;

            timer = new DispatcherTimer(TimeSpan.FromMilliseconds(interval), priority, (s, e) =>
            {
                if (timer == null)
                    return;

                timer?.Stop();
                timer = null;
                action.Invoke(param);
            }, disp);

            timer.Start();
            timerStarted = curTime;
        }
    }
    class ListViewHelper
    {
        private static Dictionary<Type, PropertyInfo[]> ExportList = new Dictionary<Type, PropertyInfo[]>();
        public static void ListViewToCSV(ListView listView, string filePath)
        {
            List<string> lines = new List<string>();
            StringBuilder sb = new StringBuilder();
            PropertyInfo[] propertyInfos = null;
            char separator = ';';
            if (listView.Items.Count > 0)
            {
                object item = listView.Items[0];
                if (ExportList.ContainsKey(item.GetType()))
                    propertyInfos = ExportList[item.GetType()];
                else
                {
                    propertyInfos = item.GetType().GetProperties();
                    ExportList.Add(item.GetType(), propertyInfos);
                }
                foreach (var it in listView.Items)
                {
                    if (lines.Count == 0)
                    {
                        for (int i = 0; i < propertyInfos.Length; i++)
                        {
                            sb.Append(propertyInfos[i].Name + separator);
                        }
                        lines.Add(sb.ToString());
                        sb.Clear();
                    }
                    for (int i = 0; i < propertyInfos.Length; i++)
                    {
                        var val = propertyInfos[i].GetValue(it);
                        if (val != null)
                        {
                            if (val.GetType() == typeof(DateTime))
                                sb.Append(((DateTime)val).ToString("dd/MMM/yyyy HH:mm:ss.fff") + separator);
                            else
                                sb.Append(propertyInfos[i].GetValue(it)?.ToString() + separator);
                        }
                        else
                        {
                            sb.Append(separator);
                        }
                    }
                    lines.Add(sb.ToString());
                    sb.Clear();
                }                
            }
            try
            {
                File.WriteAllLines(filePath, lines.ToArray());
            }
            catch (Exception ex)
            {
                if (ex.GetType() == typeof(IOException))
                {
                    if (ex.Message.Contains("cannot access"))
                        MessageBox.Show("Failed to save. The file is being open by another application.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

    }
    
    public class BindingProxy : Freezable
    {
        protected override Freezable CreateInstanceCore()
        {
            return new BindingProxy();
        }

        public object Data
        {
            get { return (object)GetValue(DataProperty); }
            set { SetValue(DataProperty, value); }
        }

        public static readonly DependencyProperty DataProperty =
            DependencyProperty.Register("Data", typeof(object), typeof(BindingProxy), new UIPropertyMetadata(null));
    }
    public class BindableSelectedItemBehavior : Behavior<TreeView>
    {
        #region SelectedItem Property

        public object SelectedItem
        {
            get { return (object)GetValue(SelectedItemProperty); }
            set { SetValue(SelectedItemProperty, value); }
        }

        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register("SelectedItem", typeof(object), typeof(BindableSelectedItemBehavior), new UIPropertyMetadata(null, OnSelectedItemChanged));

        private static void OnSelectedItemChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var item = e.NewValue as TreeViewItem;
            if (item != null)
            {
                item.SetValue(TreeViewItem.IsSelectedProperty, true);
            }
        }

        #endregion

        protected override void OnAttached()
        {
            base.OnAttached();

            this.AssociatedObject.SelectedItemChanged += OnTreeViewSelectedItemChanged;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();

            if (this.AssociatedObject != null)
            {
                this.AssociatedObject.SelectedItemChanged -= OnTreeViewSelectedItemChanged;
            }
        }

        private void OnTreeViewSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            this.SelectedItem = e.NewValue;
        }
    }
    /// Special JsonConvert resolver that allows you to ignore properties.  See https://stackoverflow.com/a/13588192/1037948
    /// </summary>
    public class IgnorableSerializerContractResolver : DefaultContractResolver
    {
        protected readonly Dictionary<Type, HashSet<string>> Ignores;
        private readonly bool _includeSubtype = false;
        public IgnorableSerializerContractResolver(bool includeSubtype = false)
        {
            this.Ignores = new Dictionary<Type, HashSet<string>>();
            _includeSubtype = includeSubtype;
        }

        /// <summary>
        /// Explicitly ignore the given property(s) for the given type
        /// </summary>
        /// <param name="type"></param>
        /// <param name="propertyName">one or more properties to ignore.  Leave empty to ignore the type entirely.</param>
        public void Ignore(Type type, params string[] propertyName)
        {
            // start bucket if DNE
            if (!this.Ignores.ContainsKey(type)) this.Ignores[type] = new HashSet<string>();

            foreach (var prop in propertyName)
            {
                this.Ignores[type].Add(prop);
            }
        }

        /// <summary>
        /// Is the given property for the given type ignored?
        /// </summary>
        /// <param name="type"></param>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public bool IsIgnored(Type type, string propertyName)
        {
            if (type == null) return false;

            if (!_includeSubtype)
            {
                if (!this.Ignores.ContainsKey(type)) return false;

                // if no properties provided, ignore the type entirely
                if (this.Ignores[type].Count == 0) return true;

                return this.Ignores[type].Contains(propertyName);
            } else
            {
                bool isSubtype = false;
                Type t = typeof(object);
                foreach (KeyValuePair<Type, HashSet<string>> kvp in this.Ignores)
                {
                    if (type.IsSubclassOf(kvp.Key))
                    {
                        isSubtype = true;
                        t = kvp.Key;
                        break;
                    }
                }
                if (isSubtype)
                {
                    if (this.Ignores[t].Count == 0) return true;
                    return this.Ignores[t].Contains(propertyName);
                }
                else
                    return false;
            }            
        }

        /// <summary>
        /// The decision logic goes here
        /// </summary>
        /// <param name="member"></param>
        /// <param name="memberSerialization"></param>
        /// <returns></returns>
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            JsonProperty property = base.CreateProperty(member, memberSerialization);

            if (this.IsIgnored(property.DeclaringType, property.PropertyName)
            // need to check basetype as well for EF -- @per comment by user576838
            || this.IsIgnored(property.DeclaringType.BaseType, property.PropertyName))
            {
                property.ShouldSerialize = instance => { return false; };
            }

            return property;
        }
    }

    [Flags]
    enum ProcessAccessFlags : uint
    {
        All = 0x001F0FFF,
        Terminate = 0x00000001,
        CreateThread = 0x00000002,
        VMOperation = 0x00000008,
        VMRead = 0x00000010,
        VMWrite = 0x00000020,
        DupHandle = 0x00000040,
        SetInformation = 0x00000200,
        QueryInformation = 0x00000400,
        QueryLimitedInformation = 0x00001000,
        Synchronize = 0x00100000,
        ReadControl = 0x00020000
    }
    public static class SystemHelper
    {
        public static bool IsTWSOpen()
        {
            List<string> appName = MainViewModel.Instance.UserPreference.IBAppName.Split(new char[] { ';' }).ToList();
            var wmiQueryString = "SELECT ProcessId, ExecutablePath, CommandLine FROM Win32_Process";
            using (var searcher = new ManagementObjectSearcher(wmiQueryString))
            using (var results = searcher.Get())
            {
                var query = from p in Process.GetProcesses()
                            join mo in results.Cast<ManagementObject>()
                            on p.Id equals (int)(uint)mo["ProcessId"]
                            select new
                            {
                                Process = p,
                                Path = (string)mo["ExecutablePath"],
                                CommandLine = (string)mo["CommandLine"],
                            };
                var cdc = query.Where(p => p.Path != null).Select(c => c.Path).ToList();
                var pName = query.Where(p => p.Path != null).Select(c => c.Path).ToList().FirstOrDefault(x =>
                {
                    var apps = appName.FirstOrDefault(y => x.ToLower().Contains(y.ToLower()));
                    if (apps != null) return true; else return false;
                });
                if (pName != null)
                    return true;
                else
                    return false;
            }
        }
    }
    public static class Helper
    {
        public static List<string> TranslateAccountStatus(AccountStatus status)
        {
            List<string> sb = new List<string>();
            foreach (AccountStatus acc in Enum.GetValues(typeof(AccountStatus)))
            {
                if ((status & acc) != 0 && acc != AccountStatus.None)
                    sb.Add(acc.ToString());
                /*
                if (acc == AccountStatus.None)
                {
                    sb.Add(acc.ToString());
                    break;
                }  */                  
            }
            if (sb.Count == 0) sb.Add("Flat");
            return sb;
        }
        public static string GetEnumDescription(this Enum value)
        {
            FieldInfo fi = value.GetType().GetField(value.ToString());

            DescriptionAttribute[] attributes =
                (DescriptionAttribute[])fi.GetCustomAttributes(
                typeof(DescriptionAttribute),
                false);

            if (attributes != null &&
                attributes.Length > 0)
                return attributes[0].Description;
            else
                return value.ToString();
        }
        /// <summary>
        /// Clones a object via shallow copy
        /// </summary>
        /// <typeparam name="T">Object Type to Clone</typeparam>
        /// <param name="obj">Object to Clone</param>
        /// <returns>New Object reference</returns>
        public static T CloneObject<T>(this T obj) where T : class
        {
            if (obj == null) return null;
            System.Reflection.MethodInfo inst = obj.GetType().GetMethod("MemberwiseClone",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (inst != null)
                return (T)inst.Invoke(obj, null);
            else
                return null;
        }
        // used by ObjectInTreeView class
        // only custom class or list/collection will return the value; otherwise, orignal obj will be returned
        public static object GetValueByName(object obj, string name)
        {
            if (obj == null) return null;
            if (obj.GetType().Name.Contains("Dictionary"))
            {
                //Type[] args = obj.GetType().GetGenericArguments();
                /*
                if (((dynamic)obj).ContainsKey(name))
                {
                    //return ((dynamic)obj)[name];
                }
                else*/
                {
                    foreach (var item in (dynamic)obj)
                    {
                        obj = item.Value;
                        break;
                    }
                }                
            }

            PropertyInfo[] pis = obj.GetType().GetProperties();
            PropertyInfo pi = pis.FirstOrDefault(x => x.Name == name);
            if (pi != null)
                if (!pi.PropertyType.IsEnum && pi.Name == name && (!pi.PropertyType.FullName.Contains("System") ||
                    pi.PropertyType.FullName.Contains("List") || pi.PropertyType.FullName.Contains("Collection")))
                    return pi.GetValue(obj);
            
            return obj;
        }
        public static object GetInstance(string strFullyQualifiedName)
        {
            Type t = typeof(Helper);
            Type t1 = typeof(AmiBroker.OrderManager.BaseOrderType);
            string fullName1 = t1.Namespace + "." + strFullyQualifiedName;

            string ns = t.Namespace;
            strFullyQualifiedName = ns + "." + strFullyQualifiedName;
            

            Type type = Type.GetType(strFullyQualifiedName);
            if (type != null)
                return Activator.CreateInstance(type);
            else
            {
                type = Type.GetType(fullName1);
                if (type != null)
                    return Activator.CreateInstance(type);
            }
                                   
            object obj = Assembly.GetExecutingAssembly().CreateInstance(strFullyQualifiedName);
            if (obj != null)
                return obj;
            
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = asm.GetType(strFullyQualifiedName);
                if (type != null)
                    return Activator.CreateInstance(type);
            }
            return null;
        }
    }
    
}

namespace System.Collections.ObjectModel
{
    public class BaseObservableCollection<T> : ObservableCollection<T>
    {
        //Flag used to prevent OnCollectionChanged from firing during a bulk operation like Add(IEnumerable<T>) and Clear()
        private bool _SuppressCollectionChanged = false;

        /// Overridden so that we may manually call registered handlers and differentiate between those that do and don't require Action.Reset args.
        public override event NotifyCollectionChangedEventHandler CollectionChanged;

        public BaseObservableCollection() : base() { }
        public BaseObservableCollection(IEnumerable<T> data) : base(data) { }

        #region Event Handlers
        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (!_SuppressCollectionChanged)
            {
                base.OnCollectionChanged(e);
                if (CollectionChanged != null)
                    CollectionChanged.Invoke(this, e);
            }
        }

        //CollectionViews raise an error when they are passed a NotifyCollectionChangedEventArgs that indicates more than
        //one element has been added or removed. They prefer to receive a "Action=Reset" notification, but this is not suitable
        //for applications in code, so we actually check the type we're notifying on and pass a customized event args.
        protected virtual void OnCollectionChangedMultiItem(NotifyCollectionChangedEventArgs e)
        {
            NotifyCollectionChangedEventHandler handlers = this.CollectionChanged;
            if (handlers != null)
                foreach (NotifyCollectionChangedEventHandler handler in handlers.GetInvocationList())
                    handler(this, !(handler.Target is ICollectionView) ? e : new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
        #endregion

        #region Extended Collection Methods
        protected override void ClearItems()
        {
            if (this.Count == 0) return;

            List<T> removed = new List<T>(this);
            _SuppressCollectionChanged = true;
            base.ClearItems();
            _SuppressCollectionChanged = false;
            OnCollectionChangedMultiItem(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, removed));
        }

        public void Add(IEnumerable<T> toAdd)
        {
            if (this == toAdd)
                throw new Exception("Invalid operation. This would result in iterating over a collection as it is being modified.");

            _SuppressCollectionChanged = true;
            foreach (T item in toAdd)
                Add(item);
            _SuppressCollectionChanged = false;
            OnCollectionChangedMultiItem(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, new List<T>(toAdd)));
        }

        public void Remove(IEnumerable<T> toRemove)
        {
            if (this == toRemove)
                throw new Exception("Invalid operation. This would result in iterating over a collection as it is being modified.");

            _SuppressCollectionChanged = true;
            foreach (T item in toRemove)
                Remove(item);
            _SuppressCollectionChanged = false;
            OnCollectionChangedMultiItem(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, new List<T>(toRemove)));
        }
        #endregion
    }

    /// <summary>
    /// Will raise property changed event when the properties of item has been changed
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ObservableCollectionEx<T> : ObservableCollection<T> where T : INotifyPropertyChanged
    {
        //public event PropertyChangedEventHandler PropertyChanged;
        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            Unsubscribe(e.OldItems);
            Subscribe(e.NewItems);
            base.OnCollectionChanged(e);
        }

        protected override void ClearItems()
        {
            foreach (T element in this)
                element.PropertyChanged -= ContainedElementChanged;

            base.ClearItems();
        }

        private void Subscribe(IList iList)
        {
            if (iList != null)
            {
                foreach (T element in iList)
                    element.PropertyChanged += ContainedElementChanged;
            }
        }

        private void Unsubscribe(IList iList)
        {
            if (iList != null)
            {
                foreach (T element in iList)
                    element.PropertyChanged -= ContainedElementChanged;
            }
        }

        private void ContainedElementChanged(object sender, PropertyChangedEventArgs e)
        {
            OnPropertyChanged(e);
        }
    }

    /// <summary>
	/// Provides a dictionary for use with data binding.
	/// </summary>
	/// <typeparam name="TKey">Specifies the type of the keys in this collection.</typeparam>
	/// <typeparam name="TValue">Specifies the type of the values in this collection.</typeparam>
	[DebuggerDisplay("Count={Count}")]
    public class ObservableDictionary<TKey, TValue> :
        ICollection<KeyValuePair<TKey, TValue>>, IDictionary<TKey, TValue>,
        INotifyCollectionChanged, INotifyPropertyChanged
    {
        readonly IDictionary<TKey, TValue> dictionary;

        /// <summary>Event raised when the collection changes.</summary>
        public event NotifyCollectionChangedEventHandler CollectionChanged = (sender, args) => { };

        /// <summary>Event raised when a property on the collection changes.</summary>
        public event PropertyChangedEventHandler PropertyChanged = (sender, args) => { };

        /// <summary>
        /// Initializes an instance of the class.
        /// </summary>
        public ObservableDictionary()
            : this(new Dictionary<TKey, TValue>())
        {
        }

        /// <summary>
        /// Initializes an instance of the class using another dictionary as 
        /// the key/value store.
        /// </summary>
        public ObservableDictionary(IDictionary<TKey, TValue> dictionary)
        {
            this.dictionary = dictionary;
        }

        void AddWithNotification(KeyValuePair<TKey, TValue> item)
        {
            AddWithNotification(item.Key, item.Value);
        }

        void AddWithNotification(TKey key, TValue value)
        {
            dictionary.Add(key, value);

            CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add,
                new KeyValuePair<TKey, TValue>(key, value)));
            PropertyChanged(this, new PropertyChangedEventArgs("Count"));
            PropertyChanged(this, new PropertyChangedEventArgs("Keys"));
            PropertyChanged(this, new PropertyChangedEventArgs("Values"));
        }

        bool RemoveWithNotification(TKey key)
        {
            TValue value;
            if (dictionary.TryGetValue(key, out value) && dictionary.Remove(key))
            {
                CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove,
                    new KeyValuePair<TKey, TValue>(key, value)));
                PropertyChanged(this, new PropertyChangedEventArgs("Count"));
                PropertyChanged(this, new PropertyChangedEventArgs("Keys"));
                PropertyChanged(this, new PropertyChangedEventArgs("Values"));

                return true;
            }

            return false;
        }

        void UpdateWithNotification(TKey key, TValue value)
        {
            TValue existing;
            if (dictionary.TryGetValue(key, out existing))
            {
                dictionary[key] = value;

                CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace,
                    new KeyValuePair<TKey, TValue>(key, value),
                    new KeyValuePair<TKey, TValue>(key, existing)));
                PropertyChanged(this, new PropertyChangedEventArgs("Values"));
            }
            else
            {
                AddWithNotification(key, value);
            }
        }

        /// <summary>
        /// Allows derived classes to raise custom property changed events.
        /// </summary>
        protected void RaisePropertyChanged(PropertyChangedEventArgs args)
        {
            PropertyChanged(this, args);
        }

        #region IDictionary<TKey,TValue> Members

        /// <summary>
        /// Adds an element with the provided key and value to the <see cref="T:System.Collections.Generic.IDictionary`2" />.
        /// </summary>
        /// <param name="key">The object to use as the key of the element to add.</param>
        /// <param name="value">The object to use as the value of the element to add.</param>
        public void Add(TKey key, TValue value)
        {
            AddWithNotification(key, value);
        }

        /// <summary>
        /// Determines whether the <see cref="T:System.Collections.Generic.IDictionary`2" /> contains an element with the specified key.
        /// </summary>
        /// <param name="key">The key to locate in the <see cref="T:System.Collections.Generic.IDictionary`2" />.</param>
        /// <returns>
        /// true if the <see cref="T:System.Collections.Generic.IDictionary`2" /> contains an element with the key; otherwise, false.
        /// </returns>
        public bool ContainsKey(TKey key)
        {
            return dictionary.ContainsKey(key);
        }

        /// <summary>
        /// Gets an <see cref="T:System.Collections.Generic.ICollection`1" /> containing the keys of the <see cref="T:System.Collections.Generic.IDictionary`2" />.
        /// </summary>
        /// <returns>An <see cref="T:System.Collections.Generic.ICollection`1" /> containing the keys of the object that implements <see cref="T:System.Collections.Generic.IDictionary`2" />.</returns>
        public ICollection<TKey> Keys
        {
            get { return dictionary.Keys; }
        }

        /// <summary>
        /// Removes the element with the specified key from the <see cref="T:System.Collections.Generic.IDictionary`2" />.
        /// </summary>
        /// <param name="key">The key of the element to remove.</param>
        /// <returns>
        /// true if the element is successfully removed; otherwise, false.  This method also returns false if <paramref name="key" /> was not found in the original <see cref="T:System.Collections.Generic.IDictionary`2" />.
        /// </returns>
        public bool Remove(TKey key)
        {
            return RemoveWithNotification(key);
        }

        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key whose value to get.</param>
        /// <param name="value">When this method returns, the value associated with the specified key, if the key is found; otherwise, the default value for the type of the <paramref name="value" /> parameter. This parameter is passed uninitialized.</param>
        /// <returns>
        /// true if the object that implements <see cref="T:System.Collections.Generic.IDictionary`2" /> contains an element with the specified key; otherwise, false.
        /// </returns>
        public bool TryGetValue(TKey key, out TValue value)
        {
            return dictionary.TryGetValue(key, out value);
        }

        /// <summary>
        /// Gets an <see cref="T:System.Collections.Generic.ICollection`1" /> containing the values in the <see cref="T:System.Collections.Generic.IDictionary`2" />.
        /// </summary>
        /// <returns>An <see cref="T:System.Collections.Generic.ICollection`1" /> containing the values in the object that implements <see cref="T:System.Collections.Generic.IDictionary`2" />.</returns>
        public ICollection<TValue> Values
        {
            get { return dictionary.Values; }
        }

        /// <summary>
        /// Gets or sets the element with the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        public TValue this[TKey key]
        {
            get { return dictionary[key]; }
            set { UpdateWithNotification(key, value); }
        }

        #endregion

        #region ICollection<KeyValuePair<TKey,TValue>> Members

        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
        {
            AddWithNotification(item);
        }

        public void Clear()
        {
            dictionary.Clear();
            CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            PropertyChanged(this, new PropertyChangedEventArgs("Count"));
            PropertyChanged(this, new PropertyChangedEventArgs("Keys"));
            PropertyChanged(this, new PropertyChangedEventArgs("Values"));
        }

        void ICollection<KeyValuePair<TKey, TValue>>.Clear()
        {
            ((ICollection<KeyValuePair<TKey, TValue>>)dictionary).Clear();

            CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            PropertyChanged(this, new PropertyChangedEventArgs("Count"));
            PropertyChanged(this, new PropertyChangedEventArgs("Keys"));
            PropertyChanged(this, new PropertyChangedEventArgs("Values"));
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
        {
            return ((ICollection<KeyValuePair<TKey, TValue>>)dictionary).Contains(item);
        }

        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            ((ICollection<KeyValuePair<TKey, TValue>>)dictionary).CopyTo(array, arrayIndex);
        }

        int ICollection<KeyValuePair<TKey, TValue>>.Count
        {
            get { return ((ICollection<KeyValuePair<TKey, TValue>>)dictionary).Count; }
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly
        {
            get { return ((ICollection<KeyValuePair<TKey, TValue>>)dictionary).IsReadOnly; }
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
        {
            return RemoveWithNotification(item.Key);
        }

        #endregion

        #region IEnumerable<KeyValuePair<TKey,TValue>> Members

        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
        {
            return ((ICollection<KeyValuePair<TKey, TValue>>)dictionary).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((ICollection<KeyValuePair<TKey, TValue>>)dictionary).GetEnumerator();
        }

        #endregion
    }
}
