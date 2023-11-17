
#region Namespace Imports

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;

#endregion

namespace Vasu.Wpf.Controls;

/// <summary>
/// Defines a <see cref="PropertyGrid"/> control.
/// </summary>
[ToolboxItem(true)]
[TemplatePart(Name = "PART_propertyItemsControl", Type = typeof(PropertyItemsControl)),
TemplatePart(Name = "PART_descriptionViewer", Type = typeof(ContentControl)),
TemplatePart(Name = "PART_CategoryViewButton", Type = typeof(ToggleButton)),
TemplatePart(Name = "PART_AlphabeticalViewButton", Type = typeof(ToggleButton))]
public class PropertyGrid : Control
{
    #region Ctor

    static PropertyGrid()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(PropertyGrid), new FrameworkPropertyMetadata(typeof(PropertyGrid)));
    }

    #endregion

    #region Dependency Properties

    public object SelectedObject
    {
        get => (object)GetValue(SelectedObjectProperty);
        set => SetValue(SelectedObjectProperty, value);
    }

    public static readonly DependencyProperty SelectedObjectProperty =
        DependencyProperty.Register(nameof(SelectedObject), typeof(object), typeof(PropertyGrid), new UIPropertyMetadata(OnSelectedObjectPropertyChanged));

    protected static void OnSelectedObjectPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var propertyGrid = d as PropertyGrid;

        propertyGrid?.SelectedObjectChanged(e);
    }

    public PropertyField SelectedField
    {
        get => (PropertyField)GetValue(SelectedFieldProperty);
        set => SetValue(SelectedFieldProperty, value);
    }

    public static readonly DependencyProperty SelectedFieldProperty =
        DependencyProperty.Register(nameof(SelectedField), typeof(PropertyField), typeof(PropertyGrid), new UIPropertyMetadata(null, OnSelectedFieldPropertyChanged));

    private static void OnSelectedFieldPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var pGrid = d as PropertyGrid;

        var nameBinding = new Binding("Name")
        {
            Source = e.NewValue
        };
        pGrid?.SetBinding(FieldNameProperty, nameBinding);

        Binding descBinding = new Binding("Description")
        {
            Source = e.NewValue
        };
        pGrid?.SetBinding(FieldDescriptionProperty, descBinding);
    }

    public string FieldName
    {
        get => (string)GetValue(FieldNameProperty);
        set => SetValue(FieldNameProperty, value);
    }

    public static readonly DependencyProperty FieldNameProperty =
        DependencyProperty.Register(nameof(FieldName), typeof(string), typeof(PropertyGrid), new UIPropertyMetadata(string.Empty));

    public string FieldDescription
    {
        get => (string)GetValue(FieldDescriptionProperty);
        set => SetValue(FieldDescriptionProperty, value);
    }

    public static readonly DependencyProperty FieldDescriptionProperty =
        DependencyProperty.Register(nameof(FieldDescription), typeof(string), typeof(PropertyGrid), new UIPropertyMetadata(string.Empty));

    #endregion

    #region Overrides

    public override void OnApplyTemplate()
    {
        if (categoryViewButton != null)
        {
            categoryViewButton.Checked -= OnCategoryViewButtonChecked;
        }
        if (alphabeticalViewButton != null)
        {
            alphabeticalViewButton.Checked -= OnAlphabeticalViewButtonChecked;
        }

        propertyItemsControl = GetTemplateChild("PART_propertyItemsControl") as PropertyItemsControl;
        categoryViewButton = GetTemplateChild("PART_CategoryViewButton") as ToggleButton;
        alphabeticalViewButton = GetTemplateChild("PART_AlphabeticalViewButton") as ToggleButton;

        if (propertyItemsControl != null)
        {
            propertyItemsControl.ItemsSource = propertyFields;
            Binding fieldBinding = new Binding("SelectedValue");
            fieldBinding.Source = propertyItemsControl;
            SetBinding(SelectedFieldProperty, fieldBinding);
        }
        if (categoryViewButton != null)
        {
            categoryViewButton.Checked += OnCategoryViewButtonChecked;
        }
        if (alphabeticalViewButton != null)
        {
            alphabeticalViewButton.Checked += OnAlphabeticalViewButtonChecked;
        }

        base.OnApplyTemplate();
    }
    #endregion

    #region Events and Handlers

    private void OnAlphabeticalViewButtonChecked(object sender, RoutedEventArgs e)
    {
        if (propertyFields == null || propertyItemsControl == null) return;

        categoryViewButton.IsChecked = false;

        var collectionView = CollectionViewSource.GetDefaultView(propertyFields);
        collectionView.GroupDescriptions.Clear();
        collectionView.SortDescriptions.Clear();

        propertyItemsControl.ItemsSource = propertyFields;
    }
    private void OnCategoryViewButtonChecked(object sender, RoutedEventArgs e)
    {
        if (propertyFields == null || propertyItemsControl == null) return;

        alphabeticalViewButton.IsChecked = false;

        var collectionView = CollectionViewSource.GetDefaultView(propertyFields);
        collectionView.GroupDescriptions.Clear();
        collectionView.SortDescriptions.Clear();
        collectionView.GroupDescriptions.Add(new PropertyGroupDescription("CategoryName"));
        collectionView.SortDescriptions.Add(new SortDescription("CategoryName", ListSortDirection.Ascending));
        collectionView.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));

        propertyItemsControl.ItemsSource = collectionView;
    }

    protected void SelectedObjectChanged(DependencyPropertyChangedEventArgs e)
    {
        PopulatePropertyFields(e.NewValue);
    }
    #endregion

    #region Implementations

    private void PopulatePropertyFields(object selectedObject)
    {
        if (selectedObject == null) return;

        foreach (var field in propertyFields ?? Enumerable.Empty<PropertyField>())
            field.Dispose();

        propertyFields = selectedObject.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public).OrderBy(p => p.Name)
            .Select(p => new PropertyField
            {
                PropertyInfo = p,
                SourceObject = selectedObject,
                PropertyGrid = this,
                Value = p.GetValue(selectedObject, null)
            });
        if (propertyItemsControl != null)
            propertyItemsControl.ItemsSource = propertyFields;
    }
    #endregion

    #region Fields

    private IEnumerable<PropertyField> propertyFields;
    private PropertyItemsControl propertyItemsControl;
    private ToggleButton categoryViewButton;
    private ToggleButton alphabeticalViewButton;

    #endregion
}
