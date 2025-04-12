using Plugin.MauiMTAdmob;
using Plugin.MauiMTAdmob.Extra;

namespace lek4
{
    public partial class MainPage : ContentPage
    {
        private const string RewardedAdUnitId = "ca-app-pub-3940256099942544/5224354917"; // Replace with your actual ad unit ID

        public MainPage()
        {
            InitializeComponent();
            InitializeAds();
        }

        private void InitializeAds()
        {
            CrossMauiMTAdmob.Current.OnRewardedLoaded += OnRewardedAdLoaded;
            CrossMauiMTAdmob.Current.OnRewardedFailedToLoad += OnRewardedAdFailedToLoad;
            CrossMauiMTAdmob.Current.OnRewardedFailedToShow += OnRewardedFailedToShow;
            CrossMauiMTAdmob.Current.OnRewardedOpened += OnRewardedOpened;
            CrossMauiMTAdmob.Current.OnRewardedClosed += OnRewardedClosed;
            CrossMauiMTAdmob.Current.OnRewardedImpression += OnRewardedImpression;
            CrossMauiMTAdmob.Current.OnUserEarnedReward += OnUserEarnedReward;
        }

        private void OnRewardedImpression(object sender, EventArgs e)
        {
            Console.WriteLine("On Reward Impression");
        }

        private void OnRewardedClosed(object sender, EventArgs e)
        {
            Console.WriteLine("On Reward Closed");
        }

        private void OnRewardedOpened(object sender, EventArgs e)
        {
            Console.WriteLine("On Reward Opened");
        }

        private void OnUserEarnedReward(object sender, MTEventArgs e)
        {
            Console.WriteLine($"User Earned Reward: {e.RewardType} - {e.RewardAmount}");
            // Handle user earned reward logic here, if necessary
        }

        private void OnRewardedFailedToShow(object sender, MTEventArgs e)
        {
            Console.WriteLine($"Reward Failed To Show: {e.ErrorCode} - {e.ErrorMessage}");
        }

        private void OnRewardedAdLoaded(object sender, EventArgs e)
        {
            Console.WriteLine("Rewarded Ad Loaded");
            CrossMauiMTAdmob.Current.ShowRewarded();
        }

        private void OnRewardedAdFailedToLoad(object sender, MTEventArgs e)
        {
            Console.WriteLine($"Rewarded Ad Failed To Load: {e.ErrorCode} - {e.ErrorMessage}");
        }

        public void LoadRewardedAd()
        {
            CrossMauiMTAdmob.Current.LoadRewarded(RewardedAdUnitId);
        }
    }
}
