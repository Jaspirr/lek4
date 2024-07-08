document.addEventListener('DOMContentLoaded', function () {
    console.log("DOMContentLoaded event fired");
    if (typeof window.admob !== 'undefined') {
        console.log("AdMob is available");

        // Initialize AdMob Banner
        window.admob.banner.config({
            id: 'pub- 6880261545527455', // Your Banner Ad Unit ID
            isTesting: true, // Remove this line when deploying to production
            autoShow: true
        });
        window.admob.banner.prepare();

        // Initialize AdMob Interstitial
        window.admob.interstitial.config({
            id: 'pub- 6880261545527455', // Your Interstitial Ad Unit ID
            isTesting: true, // Remove this line when deploying to production
            autoShow: false
        });
        window.admob.interstitial.prepare();
    } else {
        console.log("AdMob is not available");
    }
});

function showAd() {
    if (typeof window.admob !== 'undefined') {
        window.admob.interstitial.show();
    } else {
        console.log("AdMob is not available");
    }
}
