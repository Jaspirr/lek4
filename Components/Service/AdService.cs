using Plugin.MauiMTAdmob;
using Plugin.MauiMTAdmob.Extra;

public class AdService
{
    private const string RewardedAdUnitId = "ca-app-pub-3940256099942544/5224354917"; // Replace with your actual ad unit ID
    public bool IsAdReady { get; private set; }
    public bool IsAdWatched { get; private set; }
    public event EventHandler AdWatched;

    public AdService()
    {
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
        LoadRewardedAd();
    }

    public void LoadRewardedAd()
    {
        Console.WriteLine("Loading rewarded ad...");
        CrossMauiMTAdmob.Current.LoadRewarded(RewardedAdUnitId);
    }

    private void OnRewardedAdLoaded(object sender, EventArgs e)
    {
        Console.WriteLine("Rewarded ad loaded.");
        IsAdReady = true;
    }

    private void OnRewardedAdFailedToLoad(object sender, MTEventArgs e)
    {
        Console.WriteLine($"Rewarded ad failed to load: {e.ErrorCode} - {e.ErrorMessage}");
        IsAdReady = false;
    }

    private void OnRewardedFailedToShow(object sender, MTEventArgs e)
    {
        Console.WriteLine($"Rewarded ad failed to show: {e.ErrorCode} - {e.ErrorMessage}");
    }

    private void OnRewardedOpened(object sender, EventArgs e)
    {
        Console.WriteLine("Rewarded ad opened.");
    }

    private void OnRewardedClosed(object sender, EventArgs e)
    {
        Console.WriteLine("Rewarded ad closed.");
        IsAdReady = false;
        LoadRewardedAd(); // Load the next ad
    }

    private void OnRewardedImpression(object sender, EventArgs e)
    {
        Console.WriteLine("Rewarded ad impression.");
    }

    private void OnUserEarnedReward(object sender, MTEventArgs e)
    {
        Console.WriteLine($"User earned reward: {e.RewardType} - {e.RewardAmount}");
        IsAdWatched = true;
        AdWatched?.Invoke(this, EventArgs.Empty); // Raise AdWatched event
    }

    public void ShowAd()
    {
        if (IsAdReady)
        {
            Console.WriteLine("Showing rewarded ad...");
            CrossMauiMTAdmob.Current.ShowRewarded();
        }
        else
        {
            Console.WriteLine("Ad not ready yet.");
        }
    }
}
