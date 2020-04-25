using System;
using Xamarin.Forms;

namespace BeaconClient.UI
{
    public class ChatInterfaceDateConverter : IValueConverter
    {
        public object Convert (object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var v = value is DateTime ? (DateTime) value : default;

            return v.ToString("HH:mm:ss");
        }
        public object ConvertBack (object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    
        
    }
}