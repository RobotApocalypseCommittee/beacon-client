using System;
using Xamarin.Forms;

namespace BeaconClient
{
    public class DateChatPreviewConverter : IValueConverter
    {
        public object Convert (object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var v = value is DateTime ? (DateTime) value : default;

            if (v.Date == DateTime.Today.Date)
            {
                if (v.Hour < 10)
                {
                    return v.ToString("H:mm:ss");
                }

                return v.ToString("HH:mm:ss");
            }
            else if (DateTime.Today.Date.Subtract(TimeSpan.FromDays(1)) == v.Date)
            {
                return "Yesterday";
            }
            else
            {
                return v.ToString("d/M/yyyy");
            }
        }
        public object ConvertBack (object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}