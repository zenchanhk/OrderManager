using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace AmiBroker.Controllers
{
    public class CachedContentControl : ContentControl
    {
        public CachedContentControl()
        {
            Unloaded += ViewCache_Unloaded;
        }

        void ViewCache_Unloaded(object sender, RoutedEventArgs e)
        {
            Content = null;
        }

        private Type _contentType;
        public Type ContentType
        {
            get { return _contentType; }
            set
            {
                _contentType = value;
                //  use you favorite factory
                Content = ViewFactory.GetView(value);
            }
        }

        public object Contents
        {
            get { return (object)GetValue(ContentsProperty); }
            set { SetValue(ContentsProperty, value); }
        }
        public static readonly DependencyProperty ContentsProperty =
            DependencyProperty.Register("Contents", typeof(object), typeof(CachedContentControl),
            new UIPropertyMetadata(null, new PropertyChangedCallback(OnContentsChanged)));

        private static void OnContentsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            CachedContentControl control = ((CachedContentControl)d);
            //((dynamic)control.Content).DataContext = null;
            //  use you favorite factory
            if (e.OldValue == null || (e.OldValue != null && e.NewValue.GetType() != e.OldValue.GetType()))
            {
                FrameworkElement content = ViewFactory.GetView(e.NewValue.GetType());
                control.Content = content;
                content.DataContext = control.DataContext;
                //((ContentControl)content).Content = control.DataContext;
                Binding binding = new Binding();
                BindingOperations.SetBinding(content, ContentProperty, binding);
                
            }
            else
            {                 
                ((FrameworkElement)control.Content).DataContext = control.DataContext;
            }
        }
    }

    public class ViewFactory
    {
        public static ViewFactory Instance { get => instance; }
        private static readonly ViewFactory instance = new ViewFactory();        
        static ViewFactory() { }
        private ViewFactory()
        {

        }
        private static readonly Dictionary<Type, FrameworkElement> factory = new Dictionary<Type, FrameworkElement>();
        public static FrameworkElement GetView(Type type)
        {
            if (factory.ContainsKey(type))
                return factory[type];
            else
                return null;
        }

        public static void CreateView(Type type, DataTemplate dataTemplate)
        {
            if (factory.ContainsKey(type))
                factory[type] = new ContentControl { ContentTemplate = dataTemplate };
            else
                factory.Add(type, new ContentControl { ContentTemplate = dataTemplate });
        }
    }
}
