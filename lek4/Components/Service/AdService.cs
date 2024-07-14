using Microsoft.JSInterop;
using System.Threading.Tasks;

namespace lek4.Components.Service
{
    public class AdService
    {
        private readonly IJSRuntime _jsRuntime;

        public AdService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public async Task InitializeAdMobAsync()
        {
            await _jsRuntime.InvokeVoidAsync("initializeAdMob");
        }

        public async Task ShowRewardedAdAsync()
        {
            await _jsRuntime.InvokeVoidAsync("showRewardedAd");
        }
    }
}