using System.Windows;
using System.Windows.Controls;

namespace InventoryApp
{
    /// <summary>
    /// Attached property to give a TextBox initial focus in XAML.
    /// Usage: local:FocusBehavior.IsFocused="True"
    /// </summary>
    public static class FocusBehavior
    {
        public static readonly DependencyProperty IsFocusedProperty =
            DependencyProperty.RegisterAttached("IsFocused", typeof(bool), typeof(FocusBehavior),
                new PropertyMetadata(false, OnIsFocusedChanged));

        public static bool GetIsFocused(DependencyObject obj) => (bool)obj.GetValue(IsFocusedProperty);
        public static void SetIsFocused(DependencyObject obj, bool value) => obj.SetValue(IsFocusedProperty, value);

        private static void OnIsFocusedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is System.Windows.Controls.Control ctrl && (bool)e.NewValue)
                ctrl.Loaded += (_, _) => ctrl.Focus();
        }
    }
}
