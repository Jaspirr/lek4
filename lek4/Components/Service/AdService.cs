using Microsoft.JSInterop;

public class AdService
{
    private readonly IJSRuntime _jsRuntime;
    public AdService(IJSRuntime jsRuntime) { _jsRuntime = jsRuntime; }

    public async Task InitializeAdMobAsync() { await _jsRuntime.InvokeVoidAsync("initializeAdMob"); }

    public async Task ShowRewardedAdAsync() { await _jsRuntime.InvokeVoidAsync("showRewardedAd"); }
}
