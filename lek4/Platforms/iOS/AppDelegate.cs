using Foundation;
using Plugin.MauiMTAdmob;
using UIKit;
using Google.MobileAds;

namespace lek4
{
    [Register("AppDelegate")]
    public class AppDelegate : MauiUIApplicationDelegate
    {
        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
        public override bool FinishedLaunching(UIApplication app, NSDictionary options)
        {
            return base.FinishedLaunching(app, options);
        }

        private void InitializationComplete(InitializationStatus status)
        {
            // Hantera eventuella åtgärder efter initialisering här
        }

        public override void OnActivated(UIApplication application)
        {
            base.OnActivated(application);
            CrossMauiMTAdmob.Current.OnResume();
        }
    }
}
