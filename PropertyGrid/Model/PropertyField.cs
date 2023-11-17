
#region Namespace Imports

using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

#endregion

namespace Vasu.Wpf.Controls
{
    public class PropertyField : ModelBase, IDisposable
    {
        #region Members

        public string Name =>
            (PropertyDescriptor == null)
                ? (PropertyInfo == null)
                    ? string.Empty
                    : PropertyInfo.Name
                : PropertyDescriptor.Name;
        public PropertyInfo PropertyInfo
        {
            get => propertyInfo;
            set
            {
                if (value == propertyInfo) return;

                propertyInfo = value;

                RaisePropertyChanged("PropertyInfo");
            }
        }

        public PropertyDescriptor PropertyDescriptor
        {
            get => propertyDescriptor;
            private set
            {
                if (propertyDescriptor == value) return;

                RemoveValueChanged();

                propertyDescriptor = value;

                if (!isListening)
                    AddValueChanged();

                RaisePropertyChanged("PropertyDescriptor");
                RaisePropertyChanged("Name");
                RaisePropertyChanged("Description");
                RaisePropertyChanged("Value");
            }
        }

        public string CategoryName => PropertyDescriptor.Category;
        public object SourceObject
        {
            get => sourceObject;
            set
            {
                if (sourceObject == value) return;

                PropertyDescriptor = TypeDescriptor.GetProperties(value)
                    .OfType<PropertyDescriptor>()
                    .FirstOrDefault(d => d.Name == PropertyInfo.Name);

                RemoveValueChanged();

                sourceObject = value;

                if (!isListening)
                    AddValueChanged();

                RaisePropertyChanged("SourceObject");
            }
        }

        public object Value
        {
            get => this.value;
            set
            {
                if (this.value == value) return;

                this.value = value;

                if (!ignoreValueChange)
                    RaisePropertyChanged("Value");
            }
        }

        public Type PropertyType => (PropertyInfo == null) ? Type.EmptyTypes[0] : PropertyInfo.PropertyType;
        public string Description =>
            (PropertyDescriptor == null)
                ? (PropertyInfo == null || PropertyInfo.Attributes == PropertyAttributes.None)
                    ? string.Empty
                    : (PropertyInfo.GetCustomAttributes(typeof(DescriptionAttribute), false)[0] as DescriptionAttribute)?.Description
                : PropertyDescriptor.Description;
        public object VisualEditor => (PropertyEditor == null) ? null : PropertyEditor.VisualEditor;
        public PropertyGrid PropertyGrid { get; set; }

        internal PropertyEditor PropertyEditor => propertyEditor ??= PropertyEditorProvider.GetFieldEditor(this);

        #endregion

        #region Implementations

        /// <summary>
        /// Raised when PropertyValue changed from external sources.
        /// </summary>
        /// <param name="sender">sender whose value is changed.</param>
        /// <param name="e">event arguments associated with the event.</param>
        /// <remarks>Unless the source objects implements <see cref="INotifyPropertyChanged"/> or any change notification mechanism, this is of no use. </remarks>
        private void OnValueChanged(object sender, EventArgs e)
        {
            ignoreValueChange = true;

            Value = PropertyDescriptor.GetValue(SourceObject);

            ignoreValueChange = false;
        }

        /// <summary>
        /// Starts listenting to value changes by adding an event handler.
        /// </summary>
		private void AddValueChanged()
        {
            if (SourceObject == null || PropertyDescriptor == null) return;

            PropertyDescriptor.AddValueChanged(SourceObject, OnValueChanged);

            isListening = true;
        }

        /// <summary>
        /// Stops listenting to the value changes.
        /// </summary>
		private void RemoveValueChanged()
        {
            if (SourceObject == null || PropertyDescriptor == null) return;

            PropertyDescriptor.RemoveValueChanged(SourceObject, OnValueChanged);

            isListening = false;
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// Releases any references held by <see cref="PropertyField"/>.
        /// </summary>
        public void Dispose()
        {
            RemoveValueChanged();
            SourceObject = null;
        }

        #endregion

        #region Fields

        private PropertyDescriptor propertyDescriptor = null;
        private object sourceObject = null;
        private object value = null;
        private bool isListening = false;
        private bool ignoreValueChange = false;
        private PropertyEditor propertyEditor = null;
        private PropertyInfo propertyInfo = null;

        #endregion
    }
}

