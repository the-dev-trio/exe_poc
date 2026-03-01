using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace InventoryApp
{
    // ── Bool → "In Stock" / "Sold" ────────────────────────────────────────
    public class BoolToStatusConverter : IValueConverter
    {
        public static readonly BoolToStatusConverter Instance = new();
        public object Convert(object value, Type t, object p, CultureInfo c) =>
            value is true ? "Sold" : "In Stock";
        public object ConvertBack(object value, Type t, object p, CultureInfo c) =>
            throw new NotImplementedException();
    }

    // ── Bool → Visibility  (pass ConverterParameter=true to invert) ───────
    public class BoolToVisibilityConverter : IValueConverter
    {
        public static readonly BoolToVisibilityConverter Instance = new();
        public object Convert(object value, Type t, object p, CultureInfo c)
        {
            bool bVal = value is true;
            if (p is string s && s == "true") bVal = !bVal;
            return bVal ? Visibility.Visible : Visibility.Collapsed;
        }
        public object ConvertBack(object value, Type t, object p, CultureInfo c) =>
            throw new NotImplementedException();
    }

    // ── Null → Visibility ─────────────────────────────────────────────────
    public class NullToVisibilityConverter : IValueConverter
    {
        public static readonly NullToVisibilityConverter Instance = new();
        public object Convert(object value, Type t, object p, CultureInfo c) =>
            value == null ? Visibility.Collapsed : Visibility.Visible;
        public object ConvertBack(object value, Type t, object p, CultureInfo c) =>
            throw new NotImplementedException();
    }
}
