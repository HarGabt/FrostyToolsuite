using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ScalableEmitterEditorPlugin
{
    public static class ItemsControlHelpers
    {

        public static object FindItemControl(ItemsControl itemsControl, string controlName, object item)
        {
            ContentPresenter container = itemsControl.ItemContainerGenerator.ContainerFromItem(item) as ContentPresenter;
            container.ApplyTemplate();
            return container.ContentTemplate.FindName(controlName, container);
        }

        public static DependencyObject findElementInItemsControlItemAtIndex<T>(ItemsControl itemsControl,
                                                                int itemOfIndexToFind,
                                                                string nameOfControlToFind)
        {
            if (itemOfIndexToFind >= itemsControl.Items.Count) return null;

            DependencyObject depObj = null;
            object o = itemsControl.Items[itemOfIndexToFind];
            if (o != null)
            {
                var item = itemsControl.ItemContainerGenerator.ContainerFromItem(o);
                if (item != null)
                {
                    depObj = getVisualTreeChild<T>(item, nameOfControlToFind);
                    return depObj;
                }
            }
            return null;
        }

        public static DependencyObject getVisualTreeChild<T>(DependencyObject obj, String name)
        {
            DependencyObject dependencyObject = null;
            int childrenCount = VisualTreeHelper.GetChildrenCount(obj);
            for (int i = 0; i < childrenCount; i++)
            {
                var oChild = VisualTreeHelper.GetChild(obj, i);
                var childElement = oChild as FrameworkElement;
                if (childElement != null)
                {
                    if (childElement is T)
                    {
                        dependencyObject = childElement.FindName(name) as DependencyObject;
                        if (dependencyObject != null)
                            return dependencyObject;
                    }

                    if (childElement.Name == name)
                    {
                        return childElement;
                    }
                }
                dependencyObject = getVisualTreeChild<T>(oChild, name);
                if (dependencyObject != null)
                    return dependencyObject;
            }
            return dependencyObject;
        }

    }
}
