using AppKit;
using Foundation;
using Xamarin.Forms.Platform.MacOS;

namespace BeaconClient.Mac
{
    [Register("AppDelegate")]
    public class AppDelegate : FormsApplicationDelegate
    {
        NSWindow window;
        
        public AppDelegate()
        {
            /*caret*/
            var style = NSWindowStyle.Closable | NSWindowStyle.Resizable | NSWindowStyle.Titled;
            var rect = new CoreGraphics.CGRect(200, 1000, 1024, 768);
            window = new NSWindow(rect, style, NSBackingStore.Buffered, false)
            {
                Title = "Joemama2",
                TitleVisibility = NSWindowTitleVisibility.Hidden
            };
        }

        public override void DidFinishLaunching(NSNotification notification)
        {
            // Insert code here to initialize your application
            Xamarin.Forms.Forms.Init();
            LoadApplication(new App());
            base.DidFinishLaunching(notification);
        }

        public override NSWindow MainWindow => window;


        public override void WillTerminate(NSNotification notification)
        {
            // Insert code here to tear down your application
        }
    }
}