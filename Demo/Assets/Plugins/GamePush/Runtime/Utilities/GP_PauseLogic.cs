using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GamePush
{
    public class GP_PauseLogic : MonoBehaviour
    {
        private static bool _tempMute;
        private static bool _gamePause;
        private static bool _adPause;
        private static bool _cooperativeSessionActive;

        /// <summary>
        /// A multiplayer host must keep its authoritative clock alive when the page loses
        /// focus. Audio still follows the platform pause state, but Time.timeScale stays at 1.
        /// </summary>
        public static void SetCooperativeSessionActive(bool active)
        {
            _cooperativeSessionActive = active;
            ApplySimulationPause();
        }
    
        private void OnEnable()
        {
            GP_Game.OnPause += PauseGame;
            GP_Game.OnResume += UnpauseGame;
    
            GP_Ads.OnPreloaderStart += AdStart;
            GP_Ads.OnFullscreenStart += AdStart;
            GP_Ads.OnRewardedStart += AdStart;
    
            GP_Ads.OnAdsClose += AdClose;
        }
    
        private void OnDisable()
        {
            GP_Game.OnPause -= PauseGame;
            GP_Game.OnResume -= UnpauseGame;
    
            GP_Ads.OnPreloaderStart -= AdStart;
            GP_Ads.OnFullscreenStart -= AdStart;
            GP_Ads.OnRewardedStart -= AdStart;
    
            GP_Ads.OnAdsClose -= AdClose;
        }
    
        void OnApplicationFocus(bool hasFocus)
        {
            // Headless Unity test runs never receive focus. Pausing their time scale here
            // deadlocks any PlayMode test that waits for a physics frame.
            if (Application.isBatchMode) return;
            if (hasFocus)
                UnpauseGame();
            else
                PauseGame();
        }
    
        private static void PauseGame()
        {
            if (_gamePause) return;
            _gamePause = true;
    
            GP_Logger.Log($"Game On Pause: {_gamePause}");
    
            _tempMute = AudioListener.pause;
            MusicOff();
            ApplySimulationPause();
        }
    
        private static void UnpauseGame()
        {
            if (!_gamePause || _adPause) return;
            _gamePause = false;
    
            GP_Logger.Log($"Game On Pause: {_gamePause}");
    
            if (!_tempMute)
                MusicOn();
            ApplySimulationPause();
        }

        private static void ApplySimulationPause()
        {
            // Browser focus and ads may mute a co-op client, but must never freeze the shared
            // authority clock. In solo the original GamePush pause behaviour is preserved.
            Time.timeScale = (_gamePause || _adPause) && !_cooperativeSessionActive ? 0f : 1f;
        }
    
        private static void MusicOff() => AudioListener.pause = true;
        private static void MusicOn() => AudioListener.pause = false;
    
        private static void AdStart()
        {
            GP_Logger.Log($"Ad Start");
    
            _adPause = true;
            PauseGame();
        }
    
        private static void AdClose(bool succes)
        {
            GP_Logger.Log($"Ad Close: {succes}");
    
            _adPause = false;
            UnpauseGame();
        }
    }

}
