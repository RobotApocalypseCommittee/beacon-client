using System;
using Xamarin.Forms;

namespace BeaconClient 
{
    public class RecentChatPreviewConverter : IValueConverter
    {
        public object Convert (object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var v = (String) value;
            if (v.Length < 40+6)
            {
                return v;
            }
            return v.Substring(0, 40) +". . .";

        }
        public object ConvertBack (object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}