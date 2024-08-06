using Android.Gms.Ads.Initialization;
using Android.Util;

namespace lek4
{
    public class AdInitializationListener : Java.Lang.Object, IOnInitializationCompleteListener
    {
        private const string Tag = "AdInitializationListener";

        public void OnInitializationComplete(IInitializationStatus initializationStatus)
        {
            // Handle initialization complete
            Log.Info(Tag, "AdMob SDK initialization complete.");

            // Log the status of each adapter
            foreach (var key in initializationStatus.AdapterStatusMap.Keys)
            {
                var status = initializationStatus.AdapterStatusMap[key];
                Log.Info(Tag, $"Adapter: {key}, Description: {status.Description}, State: {status.InitializationState}");
            }
        }
    }
}
