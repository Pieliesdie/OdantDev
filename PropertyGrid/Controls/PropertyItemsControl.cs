
#region Namespace Imports

using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

#endregion

namespace Vasu.Wpf.Controls
{
    /// <summary>
    /// Defines a <see cref="PropertyItemsControl"/>.
    /// </summary>
    [ToolboxItem(false)]
	public class PropertyItemsControl : ListView
	{
		#region Ctor

		static PropertyItemsControl()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(PropertyItemsControl), new FrameworkPropertyMetadata(typeof(PropertyItemsControl)));
		}

        #endregion

        #region Dependency Properties
        public new PropertyItem SelectedItem
        {
			get => (PropertyItem)GetValue(SelectedItemProperty);
            set => SetValue(SelectedItemProperty, value);
        }

		public new static readonly DependencyProperty SelectedItemProperty =
			DependencyProperty.Register(nameof(SelectedItem), typeof(PropertyItem), typeof(PropertyItemsControl), new UIPropertyMetadata(null));

		public new object SelectedValue
		{
			get => (object)GetValue(SelectedValueProperty);
            set => SetValue(SelectedValueProperty, value);
        }

		public new static readonly DependencyProperty SelectedValueProperty =
			DependencyProperty.Register(nameof(SelectedValue), typeof(object), typeof(PropertyItemsControl), new UIPropertyMetadata(null));

		#endregion

		#region Overrides

		protected override DependencyObject GetContainerForItemOverride()
		{
			return new PropertyItem();
		}

		protected override bool IsItemItsOwnContainerOverride(object item)
		{
			return item is PropertyItem;
		}

		protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
		{
			base.OnPreviewMouseLeftButtonDown(e);

			var item = (PropertyItem)VisualTreeHelperExt.FindParentOfType<PropertyItem>((DependencyObject)Mouse.DirectlyOver);

			if (item == null) return;

			SelectedItem = item;

			SelectedValue = ItemContainerGenerator.ItemFromContainer(SelectedItem);
		}
		
		#endregion
	}
}
