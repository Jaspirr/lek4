using Foundation;
using Plugin.MauiMTAdmob;
using UIKit;

namespace lek4
{
    [Register("AppDelegate")]
    public class AppDelegate : MauiUIApplicationDelegate
    {
        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

        public override bool FinishedLaunching(UIApplication app, NSDictionary options)
        {
            CrossMauiMTAdmob.Current.Init("ca-app-pub-6880261545527455~1946094063");
            return base.FinishedLaunching(app, options);
        }
    }
}
