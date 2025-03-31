using System;
using System.Globalization;
using System.Windows.Data;

namespace WpfApp1
{
    public class ProgressConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 3 || !(values[0] is double value) || !(values[1] is double width) || !(values[2] is double max) || max == 0)
                return 0;

            return (value / max) * width;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

