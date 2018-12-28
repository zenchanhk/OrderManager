using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AmiBroker.Controllers
{
    class Helper
    {
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
