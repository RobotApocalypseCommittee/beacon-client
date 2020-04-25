using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace BeaconClient.UI
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class TextMessageCellTemplate : ViewCell
    {
        public TextMessageCellTemplate()
        {
            InitializeComponent();
        }
    }
}