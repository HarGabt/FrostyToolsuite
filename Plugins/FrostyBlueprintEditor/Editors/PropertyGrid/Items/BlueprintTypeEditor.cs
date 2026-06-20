using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using BlueprintEditorPlugin.Editors.BlueprintEditor.NodeWrangler;
using BlueprintEditorPlugin.Editors.GraphEditor.NodeWrangler;
using Frosty.Core.Controls;
using Frosty.Core.Controls.Editors;

namespace BlueprintEditorPlugin.Editors.PropertyGrid.Items
{
    public abstract class BlueprintTypeEditor<T> : FrostyBaseTypeEditor where T : FrameworkElement, new()
    {
        public string ValuePath { get; set; } = "Value";
        public BindingMode BindingMode { get; set; } = BindingMode.TwoWay;
        public DependencyProperty ValueProperty { get; set; }
        public IValueConverter ValueConverter { get; set; }
        public object ValueConverterParameter { get; set; }
        public ValidationRule ValidationRule { get; set; }
        public bool NotifyOnTargetUpdated { get; set; }
        protected INodeWrangler _graphEditor;

        public T CreateEditor(FrostyPropertyGridItemData item, INodeWrangler graphEditor)
        {
            T editor = new T();
            _graphEditor = graphEditor;
            CustomizeEditor(editor, item);

            Binding b = new Binding(ValuePath)
            {
                Source = item,
                Mode = BindingMode,
                Converter = ValueConverter,
                ConverterParameter = ValueConverterParameter ?? item,
                NotifyOnTargetUpdated = NotifyOnTargetUpdated
            };

            if (ValidationRule != null)
            {
                b.ValidationRules.Add(ValidationRule);
                b.ValidatesOnDataErrors = true;
            }

            BindingOperations.SetBinding(editor, ValueProperty, b);
            return editor;
        }

        public void Refresh(FrameworkElement editor)
        {
            editor.GetBindingExpression(ValueProperty).UpdateTarget();
            RefreshEditor(editor as T);
        }

        protected virtual void CustomizeEditor(T editor, FrostyPropertyGridItemData item)
        {
            Binding b = new Binding("IsEnabled")
            {
                Source = item,
                Mode = BindingMode.OneWay
            };

            if (item.DependsOn != null)
            {
                FrostyPropertyGridItemData dependentItem = item.Parent.FindChild(item.DependsOn);
                b.Path = new PropertyPath("Value");
                b.Source = dependentItem;
            }

            BindingOperations.SetBinding(editor, UIElement.IsEnabledProperty, b);

            if (editor is TextBox)
            {
                // setup read only binding
                BindingOperations.SetBinding(editor, TextBox.IsReadOnlyProperty, new Binding("IsReadOnly") { Source = item, Mode = BindingMode.OneWay });
                BindingOperations.SetBinding(editor, TextBox.IsReadOnlyCaretVisibleProperty, new Binding("IsReadOnly") { Source = item, Mode = BindingMode.OneWay });
            }

            if (item.IsReadOnly)
                editor.Opacity = 0.5f;

            editor.GotFocus += (o, e) =>
            {
                UIElement parent = editor;
                while (!(parent is BlueprintPropertyGridItem))
                    parent = System.Windows.Media.VisualTreeHelper.GetParent(parent) as UIElement;
                ((BlueprintPropertyGridItem)parent).IsSelected = true;
            };
        }

        protected virtual void RefreshEditor(T editor)
        {
        }
    }
}