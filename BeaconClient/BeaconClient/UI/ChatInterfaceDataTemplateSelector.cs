using System;
using BeaconClient.Messages;
using Xamarin.Forms;

namespace BeaconClient.UI
{
    public class ChatInterfaceDataTemplateSelector : DataTemplateSelector
    {
        private DataTemplate _TextCellTemplate = new DataTemplate(typeof(TextMessageCellTemplate));
        protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
        {
            
            var v = item as NormalMessage;
            
            // This seems stupid, but will be helpful when we have multiple message types
            if (v.MessageType == 0)
            {
                
                return _TextCellTemplate;
            }
            return _TextCellTemplate;
        }
    }
}