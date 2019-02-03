using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Media;

namespace AmiBroker.Controllers
{
    class UITreeHelper
    {
        public static T FindFirstEditableChild<T>(UIElement control) where T : UIElement
        {
            UIElement c = null;
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(control); i++)
            {
                c = VisualTreeHelper.GetChild(control, i) as UIElement;
                if (c != null)
                {
                    if (c is TextBox || c is ComboBox)
                        return c as T;
                    else if (VisualTreeHelper.GetChildrenCount(c) > 0)
                        return FindFirstEditableChild<T>(c);
                }
            }
            return null;
        }

        public static T FindChild<T>(UIElement control, string name = null) where T : UIElement
        {
            UIElement c = null;
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(control); i++)
            {
                c = VisualTreeHelper.GetChild(control, i) as UIElement;
                if (c != null)
                {
                    if (c is T)
                    {
                        if ((name != null && ((Control)c).Name == name) || name == null)
                            return c as T;
                    }                        
                    else if (VisualTreeHelper.GetChildrenCount(c) > 0)
                    {
                        var child = FindChild<T>(c, name);
                        if (child != null)
                            return child;
                    }                        
                }
            }
            return null;
        }

        public static void FindAllChild<T>(UIElement control, List<T> collection) where T : UIElement
        {
            UIElement c = null;
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(control); i++)
            {
                c = VisualTreeHelper.GetChild(control, i) as UIElement;
                if (c != null)
                {
                    if ((c is TextBox || c is ComboBox || c is TextBlock || c is ListBox || c is ListView))
                        collection.Add((T)c);
                    else if (VisualTreeHelper.GetChildrenCount(c) > 0)
                        FindAllChild<T>(c, collection);
                }
            }
        }

        public static T FindParent<T>(UIElement control) where T : UIElement
        {
            UIElement p = VisualTreeHelper.GetParent(control) as UIElement;
            if (p != null)
            {
                if (p is T)
                    return p as T;
                else
                    return FindParent<T>(p);
            }
            return null;
        }

        public static T FindLogicalParent<T>(UIElement control) where T : UIElement
        {
            UIElement p = LogicalTreeHelper.GetParent(control) as UIElement;
            if (p != null)
            {
                if (p is T)
                    return p as T;
                else
                    return FindLogicalParent<T>(p);
            }
            return null;
        }

        public static T GetCell<T>(FrameworkElement instance, int row, int col) where T : UIElement
        {
            ListBoxItem lbi = null;
            ListViewItem lvi = null;
            List<T> children = new List<T>();
            if (instance is ListView)
            {
                ListView lb = instance as ListView;
                lvi = (ListViewItem)(lb.ItemContainerGenerator.ContainerFromIndex(row));
                FindAllChild<T>(lbi, children);
                if (children != null)
                {
                    return (T)children[col];
                }
                return null;
            }
            else if (instance is ListBox)
            {
                ListBox lb = instance as ListBox;
                lbi = (ListBoxItem)(lb.ItemContainerGenerator.ContainerFromIndex(row));
                FindAllChild<T>(lbi, children);
                if (children != null)
                {
                    return (T)children[col];
                }
                return null;
            }
            else
                return null;
        }

        public static Visual GetDescendantByType(Visual element, Type type, string name)
        {
            if (element == null) return null;
            if (element.GetType() == type)
            {
                FrameworkElement fe = element as FrameworkElement;
                if (fe != null)
                {
                    if (fe.Name == name)
                    {
                        return fe;
                    }
                }
            }
            Visual foundElement = null;
            if (element is FrameworkElement)
                (element as FrameworkElement).ApplyTemplate();
            for (int i = 0;
                i < VisualTreeHelper.GetChildrenCount(element); i++)
            {
                Visual visual = VisualTreeHelper.GetChild(element, i) as Visual;
                foundElement = GetDescendantByType(visual, type, name);
                if (foundElement != null)
                    break;
            }
            return foundElement;
        }
    
}
}
