using Android.App;
using Android.Content.PM;
using Android.OS;
using Plugin.MauiMTAdmob;
namespace MMTAdmobSample
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            string appId = "ca-app-pub-3940256099942544~3347511713";
            string license = "YOUR_LICENSE_KEY"; //<-- Your license key here
            CrossMauiMTAdmob.Current.Init(this, appId, license);
        }

        protected override void OnResume()
        {
            base.OnResume();
            CrossMauiMTAdmob.Current.OnResume();
        }
    }
}
