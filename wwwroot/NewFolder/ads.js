function initializeAdMob() {
    var admobid = {
        banner: 'ca-app-pub-6880261545527455/7437532811', // Your Banner Ad Unit ID
        interstitial: 'ca-app-pub-6880261545527455/7437532811', // Your Interstitial Ad Unit ID
        rewarded: 'ca-app-pub-6880261545527455/7437532811' // Your Rewarded Video Ad Unit ID
    };

    function checkAdMob() {
        if (typeof AdMob !== 'undefined') {
            AdMob.createBanner({
                adId: admobid.banner,
                isTesting: true, // Remove this line for production
                overlap: false,
                offsetTopBar: false,
                adSize: 'SMART_BANNER',
                position: AdMob.AD_POSITION.BOTTOM_CENTER,
                bgColor: 'black'
            });

            AdMob.prepareInterstitial({
                adId: admobid.interstitial,
                isTesting: true, // Remove this line for production
                autoShow: false
            });

            AdMob.prepareRewardVideoAd({
                adId: admobid.rewarded,
                isTesting: true, // Remove this line for production
                autoShow: false
            });

            console.log("AdMob initialized");
        } else {
            console.log("AdMob plugin not ready, retrying...");
            setTimeout(checkAdMob, 1000); // Retry after 1 second
        }
    }

    checkAdMob();
}

function showRewardedAd() {
    if (typeof AdMob !== 'undefined') {
        AdMob.showRewardVideoAd();
        console.log("Showing rewarded ad");

        // Simulate a successful ad watch after a short delay
        setTimeout(function () {
            console.log("Ad watched, invoking DotNet method");
            DotNet.invokeMethodAsync('lek4', 'OnAdWatched');
        }, 3000); // Simulate ad watched after 3 seconds
    } else {
        console.log("AdMob plugin not ready");
    }
}

function isAdMobReady() {
    return typeof AdMob !== 'undefined';
}
