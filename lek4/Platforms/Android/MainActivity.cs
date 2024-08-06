using Android.App;
using Android.Content.PM;
using Android.Gms.Ads;
using Android.OS;
using Android.Runtime;
using Microsoft.Maui;
using Plugin.MauiMTAdmob;

namespace lek4
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Initialize the AdMob plugin
            CrossMauiMTAdmob.Current.Init(this, "ca-app-pub-6880261545527455~1946094063");

            // Add any other necessary initialization code
            MobileAds.Initialize(this, new AdInitializationListener());

            var requestConfiguration = new RequestConfiguration.Builder()
                .SetTestDeviceIds(new List<string> { "B3EEABB8EE11C2BE770B684D95219ECB" }) // Replace with your test device ID
                .Build();
            MobileAds.RequestConfiguration = requestConfiguration;
        }
    }
}
