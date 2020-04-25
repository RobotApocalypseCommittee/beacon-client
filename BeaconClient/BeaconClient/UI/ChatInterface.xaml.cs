using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BeaconClient.Messages;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace BeaconClient
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ChatInterface : ContentPage
    {
        public ChatPreview fromPreview;
        public DisplayChat DisplayedChat;
        public IList<NormalMessage> ListViewSource;

        public ChatInterface()
        {
            InitializeComponent();
            MainListView.BindingContext = this;
            BindingContext = this;
            MainListView.SelectionMode = ListViewSelectionMode.None;
            
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            
            DisplayedChat = IDToChatInfo(fromPreview.ChatID);
            UpdateMessageDisplay(DisplayedChat.MessageList);
            Title = DisplayedChat.ChannelName;
            
            //Console.WriteLine(ListViewSource.Count);
            //Console.WriteLine(MainListView.ItemsSource);
        }

        public void UpdateMessageDisplay(List<NormalMessage> messages)
        {
            var sorted = new List<NormalMessage>();
            sorted = messages.OrderBy(i => i.Timestamp).ToList();
            MainListView.ItemsSource = sorted;
        }
        

        public DisplayChat IDToChatInfo(int id)
        {
            // This is temporary, as conversion from chat ID to chat info is a database thing and I currently do not know how that works
            // Essentially all of the important actual properties of these messages are fudged/have no meaning as of now
            // Integrating the actual important internal things to the outward nice UI things is i think my next step
            // What this function would need to do is look up the provided channel ID in the local database, get a bunch of messages,
            // Look up all the relevant users (to get user IDs --> names), and format that nicely into a Display Chat class instance

            for (var j = 0; j < Singleton.AllChats.Count; j++)
            {
                if (id == Singleton.AllChats[j].ChannelID)
                {
                    return Singleton.AllChats[j];
                }
            }

            return new DisplayChat
            {
                ChannelID = -1,
                ChannelName = "Something is wrong",
                MessageList = new List<NormalMessage>()
            };
        }
        
        
        
        
    }
}