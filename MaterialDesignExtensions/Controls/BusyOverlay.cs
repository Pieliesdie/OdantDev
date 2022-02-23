using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace MaterialDesignExtensions.Controls
{
    /// <summary>
    /// A control for displaying some kind of progress indication over the complete user interface while a long running operation is in progress.
    /// </summary>
    public class BusyOverlay : ContentControl
    {
        /// <summary>
        /// True, to switch the control into busy state and make it visible in the UI's foreground.
        /// </summary>
        public static readonly DependencyProperty IsBusyProperty = DependencyProperty.Register(
            nameof(IsBusy), typeof(bool), typeof(BusyOverlay), new PropertyMetadata(false));

        /// <summary>
        /// True, to switch the control into busy state and make it visible in the UI's foreground.
        /// </summary>
        public bool IsBusy
        {
            get
            {
                return (bool)GetValue(IsBusyProperty);
            }

            set
            {
                SetValue(IsBusyProperty, value);
            }
        }
        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
           nameof(Text), typeof(string), typeof(BusyOverlay), new PropertyMetadata(string.Empty));
        public string Text
        {
            get
            {
                return (string)GetValue(TextProperty);
            }

            set
            {
                SetValue(TextProperty, value);
            }
        }
        /// <summary>
        /// The progress in percentage of the operation causing the busy state.
        /// </summary>
        public static readonly DependencyProperty ProgressProperty = DependencyProperty.Register(
            nameof(Progress), typeof(int), typeof(BusyOverlay), new PropertyMetadata(0));

        /// <summary>
        /// The progress in percentage of the operation causing the busy state.
        /// </summary>
        public int Progress
        {
            get
            {
                return (int)GetValue(ProgressProperty);
            }

            set
            {
                SetValue(ProgressProperty, value);
            }
        }
        public static readonly DependencyProperty ProgressSizeProperty = DependencyProperty.Register(
         nameof(ProgressSize), typeof(int), typeof(BusyOverlay), new PropertyMetadata(48));

        /// <summary>
        /// The progress in percentage of the operation causing the busy state.
        /// </summary>
        public int ProgressSize
        {
            get
            {
                return (int)GetValue(ProgressSizeProperty);
            }

            set
            {
                SetValue(ProgressSizeProperty, value);
            }
        }
        /// <summary>
        /// Creates a new <see cref="BusyOverlay" />.
        /// </summary>
        public BusyOverlay() : base() { }
    }
}
