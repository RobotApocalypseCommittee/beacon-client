using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace BeaconClient
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class Chats : ContentPage
    {
        public IList<ChatPreview> ChatPreviews { get; private set; }
        public Chats()
        {
            InitializeComponent();
            //this.NavigationController.NavigationBar.BarTintColor = new Color(154, 3, 30);
            ((NavigationPage) Application.Current.MainPage).BarBackgroundColor = Color.FromHex("9A031E");
            // NEED TO CHANGE STATUS BAR COLOR ON ALL PLATFORMS, NOT JUST ANDROID

           var test = new List<ChatPreview>
            {
                new ChatPreview()
                {
                    Name = "Bhuvan Belur",
                    LastActivity = new DateTime(2020, 4, 24, 20, 52, 30),
                    Recent = "Hey! What's up! I found this new game called DF2 - you should check it out!",
                    ChatID = 1
                },
                new ChatPreview()
                {
                    Name = "Atto Allas",
                    LastActivity = new DateTime(2020, 4, 23, 3, 30, 2),
                    Recent = "Who's Joe?",
                    ChatID = 2
                }
            };
           SetChats(test);
           BindingContext = this;
    


        }

        protected override void OnAppearing()
        {
            MainListView.SelectedItem = null;
        }

        public void SetChats(List<ChatPreview> chats)
        {
            List<ChatPreview> timeSorted  = new List<ChatPreview>();
            timeSorted = chats.OrderBy(i => i.LastActivity).ToList();
            timeSorted.Reverse();
            ChatPreviews = timeSorted;
        }
        
        void OnListViewItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            ChatPreview selectedItem = e.SelectedItem as ChatPreview;
            
        }
        
        async void OnListViewItemTapped(object sender, ItemTappedEventArgs e)
        {
            ChatPreview tappedItem = e.Item as ChatPreview;
            var detail = new BeaconClient.ChatInterface();
            detail.fromPreview = tappedItem;
            await Navigation.PushAsync(detail);
        }
        
    }
}