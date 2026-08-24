using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Events;


namespace GamePush
{
    public class GP_Ads : GP_Module
    {
        private static void ConsoleLog(string log) => GP_Logger.ModuleLog(log, ModuleName.Ads);

        #region Events

        public static event UnityAction OnAdsStart;
        public static event UnityAction<bool> OnAdsClose;
        public static event UnityAction OnFullscreenStart;
        public static event UnityAction<bool> OnFullscreenClose;
        public static event UnityAction OnPreloaderStart;
        public static event UnityAction<bool> OnPreloaderClose;
        public static event UnityAction OnRewardedStart;
        public static event UnityAction<bool> OnRewardedClose;
        public static event UnityAction<string> OnRewardedReward;
        public static event UnityAction OnStickyStart;
        public static event UnityAction OnStickyClose;
        public static event UnityAction OnStickyRefresh;
        public static event UnityAction OnStickyRender;
        
        private static event Action _onFullscreenStart;
        private static event Action<bool> _onFullscreenClose;
        private static event Action _onPreloaderStart;
        private static event Action<bool> _onPreloaderClose;
        private static event Action<string> _onRewardedReward;
        private static event Action _onRewardedStart;
        private static event Action<bool> _onRewardedClose;

        #endregion
       

        [DllImport("__Internal")]
        private static extern void GP_Ads_ShowFullscreen(string showCountdownOverlay);
        public static void ShowFullscreen(Action onFullscreenStart = null, Action<bool> onFullscreenClose = null)
        {
            ShowFullscreen(false, onFullscreenStart, onFullscreenClose);
        }

        public static void ShowFullscreen(bool showCountdownOverlay, Action onFullscreenStart = null, Action<bool> onFullscreenClose = null)
        {
            _onFullscreenStart = onFullscreenStart;
            _onFullscreenClose = onFullscreenClose;

#if !UNITY_EDITOR && UNITY_WEBGL
             GP_Ads_ShowFullscreen(showCountdownOverlay.ToString());
#else
            ConsoleLog("FULL SCREEN AD: SHOW");
#if UNITY_EDITOR
            if (GP_AdsStub.Enabled)
                GP_AdsStub.ShowFullscreen();
#endif
#endif
        }


        [DllImport("__Internal")]
        private static extern void GP_Ads_ShowRewarded(string idOrTag);
        public static void ShowRewarded(string idOrTag = "COINS", Action<string> onRewardedReward = null, Action onRewardedStart = null, Action<bool> onRewardedClose = null)
        {
            _onRewardedReward = onRewardedReward;
            _onRewardedStart = onRewardedStart;
            _onRewardedClose = onRewardedClose;

#if !UNITY_EDITOR && UNITY_WEBGL
            GP_Ads_ShowRewarded(idOrTag);
#else
            ConsoleLog("SHOW REWARDED AD -> TAG: " + idOrTag);
#if UNITY_EDITOR
            if (GP_AdsStub.Enabled)
            {
                GP_AdsStub.ShowRewarded(idOrTag);
                return;
            }
#endif
            OnRewardedReward?.Invoke(idOrTag);
            _onRewardedReward?.Invoke(idOrTag);
#endif
        }


        [DllImport("__Internal")]
        private static extern void GP_Ads_ShowPreloader();
        public static void ShowPreloader(Action onPreloaderStart = null, Action<bool> onPreloaderClose = null)
        {
            _onPreloaderStart = onPreloaderStart;
            _onPreloaderClose = onPreloaderClose;

#if !UNITY_EDITOR && UNITY_WEBGL
            GP_Ads_ShowPreloader();
#else
            ConsoleLog("PRELOADER AD: SHOW");
#if UNITY_EDITOR
            if (GP_AdsStub.Enabled)
                GP_AdsStub.ShowPreloader();
#endif
#endif
        }


        [DllImport("__Internal")]
        private static extern void GP_Ads_ShowSticky();
        public static void ShowSticky()
        {
#if !UNITY_EDITOR && UNITY_WEBGL
            GP_Ads_ShowSticky();
#else

            ConsoleLog("STICKY BANNER AD: SHOW");
#endif
        }


        [DllImport("__Internal")]
        private static extern void GP_Ads_CloseSticky();
        public static void CloseSticky()
        {
#if !UNITY_EDITOR && UNITY_WEBGL
            GP_Ads_CloseSticky();
#else

            ConsoleLog("STICKY BANNER AD: CLOSE");
#endif
        }


        [DllImport("__Internal")]
        private static extern void GP_Ads_RefreshSticky();
        public static void RefreshSticky()
        {
#if !UNITY_EDITOR && UNITY_WEBGL
            GP_Ads_RefreshSticky();
#else

            ConsoleLog("STICKY BANNER AD: REFRESH");
#endif
        }


        [DllImport("__Internal")]
        private static extern string GP_Ads_IsAdblockEnabled();
        public static bool IsAdblockEnabled()
        {
#if !UNITY_EDITOR && UNITY_WEBGL
            return GP_Ads_IsAdblockEnabled() == "true";
#else
            bool isVal = GP_Settings.instance.GetPlatformSettings().IsAdblockEnabled;
            ConsoleLog("IS ADBLOCK ENABLED: " + isVal);
            return isVal;
#endif
        }


        [DllImport("__Internal")]
        private static extern string GP_Ads_IsStickyAvailable();
        public static bool IsStickyAvailable()
        {
#if !UNITY_EDITOR && UNITY_WEBGL
            return GP_Ads_IsStickyAvailable() == "true";
#else
            bool isVal = GP_Settings.instance.GetPlatformSettings().IsStickyAvailable;
            ConsoleLog("IS STICKY BANNER AD AVAILABLE: " + isVal);
            return isVal;
#endif
        }


        [DllImport("__Internal")]
        private static extern string GP_Ads_IsFullscreenAvailable();
        public static bool IsFullscreenAvailable()
        {
#if !UNITY_EDITOR && UNITY_WEBGL
            return GP_Ads_IsFullscreenAvailable() == "true";
#else
            bool isVal = GP_Settings.instance.GetPlatformSettings().IsFullscreenAvailable;
            ConsoleLog("IS FULL SCREEN AD AVAILABLE: " + isVal);
            return isVal;
#endif
        }


        [DllImport("__Internal")]
        private static extern string GP_Ads_IsRewardedAvailable();
        public static bool IsRewardedAvailable()
        {
#if !UNITY_EDITOR && UNITY_WEBGL
            return GP_Ads_IsRewardedAvailable() == "true";
#else
            bool isVal = GP_Settings.instance.GetPlatformSettings().IsRewardedAvailable;
            ConsoleLog("IS REWARD AD AVAILABLE: " + isVal);
            return isVal;
#endif
        }


        [DllImport("__Internal")]
        private static extern string GP_Ads_IsPreloaderAvailable();
        public static bool IsPreloaderAvailable()
        {
#if !UNITY_EDITOR && UNITY_WEBGL
            return GP_Ads_IsPreloaderAvailable() == "true";
#else
            bool isVal = GP_Settings.instance.GetPlatformSettings().IsPreloaderAvailable;
            ConsoleLog("IS PRELOADER AD AVAILABLE: " + isVal);
            return isVal;
#endif
        }


        [DllImport("__Internal")]
        private static extern string GP_Ads_IsStickyPlaying();
        public static bool IsStickyPlaying()
        {
#if !UNITY_EDITOR && UNITY_WEBGL
            return GP_Ads_IsStickyPlaying() == "true";
#else

            ConsoleLog("IS STICKY PLAYING: FALSE");
            return false;
#endif
        }

        [DllImport("__Internal")]
        private static extern string GP_Ads_IsFullscreenPlaying();
        public static bool IsFullscreenPlaying()
        {
#if !UNITY_EDITOR && UNITY_WEBGL
            return GP_Ads_IsFullscreenPlaying() == "true";
#else
#if UNITY_EDITOR
            bool playing = GP_AdsStub.Enabled && GP_AdsStub.IsFullscreenPlaying;
            ConsoleLog("IS FULLSCREEN AD PLAYING: " + playing);
            return playing;
#else
            ConsoleLog("IS FULLSCREEN AD PLAYING: FALSE");
            return false;
#endif
#endif
        }

        [DllImport("__Internal")]
        private static extern string GP_Ads_IsRewardedPlaying();
        public static bool IsRewardPlaying()
        {
#if !UNITY_EDITOR && UNITY_WEBGL
            return GP_Ads_IsRewardedPlaying() == "true";
#else
#if UNITY_EDITOR
            bool playing = GP_AdsStub.Enabled && GP_AdsStub.IsRewardedPlaying;
            ConsoleLog("IS REWARDED AD PLAYING: " + playing);
            return playing;
#else
            ConsoleLog("IS REWARDED AD PLAYING: FALSE");
            return false;
#endif
#endif
        }

        [DllImport("__Internal")]
        private static extern string GP_Ads_IsPreloaderPlaying();
        public static bool IsPreloaderPlaying()
        {
#if !UNITY_EDITOR && UNITY_WEBGL
            return GP_Ads_IsPreloaderPlaying() == "true";
#else
#if UNITY_EDITOR
            bool playing = GP_AdsStub.Enabled && GP_AdsStub.IsPreloaderPlaying;
            ConsoleLog("IS PRELOADER AD PLAYING: " + playing);
            return playing;
#else
            ConsoleLog("IS PRELOADER AD PLAYING: FALSE");
            return false;
#endif
#endif
        }

        [DllImport("__Internal")]
        private static extern string GP_Ads_IsCountdownOverlayEnabled();
        public static bool IsCountdownOverlayEnabled()
        {
#if !UNITY_EDITOR && UNITY_WEBGL
            return GP_Ads_IsCountdownOverlayEnabled() == "true";
#else

            ConsoleLog("Is Countdown Overlay Enabled: FALSE");
            return false;
#endif
        }

        [DllImport("__Internal")]
        private static extern string GP_Ads_IsRewardedFailedOverlayEnabled();
        public static bool IsRewardedFailedOverlayEnabled()
        {
#if !UNITY_EDITOR && UNITY_WEBGL
            return GP_Ads_IsRewardedFailedOverlayEnabled() == "true";
#else

            ConsoleLog("Is Rewarded Failed Overlay Enabled: FALSE");
            return false;
#endif
        }

        [DllImport("__Internal")]
        private static extern string GP_Ads_CanShowFullscreenBeforeGamePlay();
        public static bool CanShowFullscreenBeforeGamePlay()
        {
#if !UNITY_EDITOR && UNITY_WEBGL
            return GP_Ads_CanShowFullscreenBeforeGamePlay() == "true";
#else

            ConsoleLog("Can Show Fullscreen Before Gameplay: FALSE");
            return false;
#endif
        }


        internal static void FireAdsStart() => OnAdsStart?.Invoke();
        internal static void FireAdsClose(bool success) => OnAdsClose?.Invoke(success);
        internal static void FireFullscreenStart()
        {
            _onFullscreenStart?.Invoke();
            OnFullscreenStart?.Invoke();
        }
        internal static void FireFullscreenClose(bool success)
        {
            _onFullscreenClose?.Invoke(success);
            OnFullscreenClose?.Invoke(success);
        }
        internal static void FirePreloaderStart()
        {
            _onPreloaderStart?.Invoke();
            OnPreloaderStart?.Invoke();
        }
        internal static void FirePreloaderClose(bool success)
        {
            _onPreloaderClose?.Invoke(success);
            OnPreloaderClose?.Invoke(success);
        }
        internal static void FireRewardedStart()
        {
            _onRewardedStart?.Invoke();
            OnRewardedStart?.Invoke();
        }
        internal static void FireRewardedClose(bool success)
        {
            _onRewardedClose?.Invoke(success);
            OnRewardedClose?.Invoke(success);
        }
        internal static void FireRewardedReward(string tag)
        {
            _onRewardedReward?.Invoke(tag);
            OnRewardedReward?.Invoke(tag);
        }

        private void CallAdsStart() => FireAdsStart();
        private void CallAdsClose(string success) => FireAdsClose(success == "true");
        private void CallAdsFullscreenStart() => FireFullscreenStart();
        private void CallAdsFullscreenClose(string success) => FireFullscreenClose(success == "true");
        private void CallAdsPreloaderStart() => FirePreloaderStart();
        private void CallAdsPreloaderClose(string success) => FirePreloaderClose(success == "true");
        private void CallAdsRewardedStart() => FireRewardedStart();
        private void CallAdsRewardedClose(string success) => FireRewardedClose(success == "true");
        private void CallAdsRewardedReward(string Tag) => FireRewardedReward(Tag);

        private void CallAdsStickyStart() => OnStickyStart?.Invoke();
        private void CallAdsStickyClose() => OnStickyClose?.Invoke();
        private void CallAdsStickyRefresh() => OnStickyRefresh?.Invoke();
        private void CallAdsStickyRender() => OnStickyRender?.Invoke();

    }
}