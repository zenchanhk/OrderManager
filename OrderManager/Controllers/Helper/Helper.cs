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
using IB.CSharpApiClient;
using IB.CSharpApiClient.Events;
using IBApi;

namespace AmiBroker.Controllers
{    
    
    public static class IBTaskExt
    {
        private static int reqIdCount = 0;
        public static async Task<T> FromEvent<TEventArgs, T>(
            Action<EventHandler<TEventArgs>> registerEvent,
            System.Action<int> action,
            Action<EventHandler<TEventArgs>> unregisterEvent,
            CancellationToken token)
        {
            int reqId = reqIdCount++;
            if (reqIdCount >= int.MaxValue)
                reqIdCount = 0;

            var tcs = new TaskCompletionSource<T>();
            EventHandler<TEventArgs> handler = (sender, args) =>
            {
                if (args.GetType() == typeof(IB.CSharpApiClient.Events.ErrorEventArgs))
                {
                    IB.CSharpApiClient.Events.ErrorEventArgs arg = args as IB.CSharpApiClient.Events.ErrorEventArgs;
                    if (arg.RequestId == reqId)
                    {
                        Exception ex = new Exception(arg.Message);
                        ex.Data.Add("RequestId", arg.RequestId);
                        ex.Data.Add("ErrorCode", arg.ErrorCode);
                        ex.Source = "IBTaskExt.FromEvent";
                        tcs.TrySetException(ex);
                    }                        
                }
                else if(args.GetType() == typeof(IB.CSharpApiClient.Events.ContractDetailsEventArgs))
                {
                    ContractDetailsEventArgs arg = args as ContractDetailsEventArgs;
                    Contract contract = new Contract();
                    contract.ConId = arg.ContractDetails.Summary.ConId;
                    contract.LastTradeDateOrContractMonth = arg.ContractDetails.Summary.LastTradeDateOrContractMonth;
                    contract.LocalSymbol = arg.ContractDetails.Summary.LocalSymbol;
                    contract.SecType = arg.ContractDetails.Summary.SecType;
                    contract.Symbol = arg.ContractDetails.Summary.Symbol;
                    contract.Exchange = arg.ContractDetails.Summary.Exchange;
                    contract.Currency = arg.ContractDetails.Summary.Currency;
                    tcs.TrySetResult((T)(object)contract);
                }
                    
            };
            registerEvent(handler);

            try
            {
                using (token.Register(() => tcs.SetCanceled()))
                {
                    action(reqId);
                    return await tcs.Task;
                }
            }
            finally
            {
                unregisterEvent(handler);
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
    public static class Helper
    {
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
            string ns = t.Namespace;
            strFullyQualifiedName = ns + "." + strFullyQualifiedName;

            Type type = Type.GetType(strFullyQualifiedName);
            if (type != null)
                return Activator.CreateInstance(type);
                                   
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
}
