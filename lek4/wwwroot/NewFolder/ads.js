// wwwroot/ads.js

function initializeAdMob() {
    var admobid = {
        banner: 'ca-app-pub-6880261545527455/7437532811', // Ditt Banner Ad Unit ID
        interstitial: 'ca-app-pub-6880261545527455/7437532811', // Ditt Interstitial Ad Unit ID
        rewarded: 'ca-app-pub-6880261545527455/7437532811' // Ditt Rewarded Video Ad Unit ID
    };

    if (typeof AdMob !== 'undefined') {
        AdMob.createBanner({
            adId: admobid.banner,
            isTesting: true, // Ta bort denna rad vid produktsläpp
            overlap: false,
            offsetTopBar: false,
            adSize: 'SMART_BANNER',
            position: AdMob.AD_POSITION.BOTTOM_CENTER,
            bgColor: 'black'
        });

        AdMob.prepareInterstitial({
            adId: admobid.interstitial,
            isTesting: true, // Ta bort denna rad vid produktsläpp
            autoShow: false
        });

        AdMob.prepareRewardVideoAd({
            adId: admobid.rewarded,
            isTesting: true, // Ta bort denna rad vid produktsläpp
            autoShow: false
        });

        console.log("AdMob initialized");
    } else {
        console.log("AdMob plugin not ready");
    }
}

function showRewardedAd(instanceId) {
    if (typeof AdMob !== 'undefined') {
        AdMob.showRewardVideoAd();
        console.log("Showing rewarded ad");

        // Simulera en framgångsrik visning av annonsen efter en kort fördröjning
        setTimeout(function () {
            DotNet.invokeMethodAsync('lek4', 'OnAdWatched', instanceId);
            startConfetti(); // Starta konfettianimationen
        }, 3000); // Simulerar att annonsen har setts efter 3 sekunder
    } else {
        console.log("AdMob plugin not ready");
    }
}

function startConfetti() {
    confetti({
        particleCount: 200,
        spread: 70,
        origin: { y: 0.6 }
    });
}
