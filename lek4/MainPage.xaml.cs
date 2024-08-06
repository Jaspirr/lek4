using Plugin.MauiMTAdmob;
using Microsoft.Maui.Controls;
using System;
using Plugin.MauiMTAdmob.Extra;

namespace lek4
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
            InitializeAds();
        }

        private void InitializeAds()
        {
            CrossMauiMTAdmob.Current.OnInterstitialLoaded += OnInterstitialAdLoaded;
            CrossMauiMTAdmob.Current.OnInterstitialFailedToLoad += OnInterstitialAdFailedToLoad;
            CrossMauiMTAdmob.Current.OnInterstitialOpened += OnInterstitialAdOpened;
            CrossMauiMTAdmob.Current.OnInterstitialClosed += OnInterstitialAdClosed;
        }

        private void LoadInterstitialAd(object sender, EventArgs e)
        {
            CrossMauiMTAdmob.Current.LoadInterstitial("ca-app-pub-3940256099942544/1033173712"); // Replace with your Ad ID
        }

        private void OnInterstitialAdLoaded(object sender, EventArgs e)
        {
            CrossMauiMTAdmob.Current.ShowInterstitial();
        }

        private void OnInterstitialAdFailedToLoad(object sender, MTEventArgs e)
        {
            // Handle ad failed to load
        }

        private void OnInterstitialAdOpened(object sender, EventArgs e)
        {
            // Handle ad opened
        }

        private void OnInterstitialAdClosed(object sender, EventArgs e)
        {
            // Handle ad closed
        }
    }
}
