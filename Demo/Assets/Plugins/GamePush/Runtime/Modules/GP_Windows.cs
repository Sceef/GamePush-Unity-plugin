using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Events;

namespace GamePush
{
    public class GP_Windows : GP_Module
    {
        private static void ConsoleLog(string log) => GP_Logger.ModuleLog(log, ModuleName.Windows);
        
        public static event UnityAction<bool> OnConfirm;
        private static event Action<bool> _onConfirm;
        
        private void CallWindowsShowConfirm(string success)
        { 
            _onConfirm?.Invoke(success == "true");
            OnConfirm?.Invoke(success == "true");
        }
        
        private void CallWindowsShowConfirmBool(bool success)
        { 
            _onConfirm?.Invoke(success);
            OnConfirm?.Invoke(success);
        }

        #if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void GP_Windows_ShowDefaultConfirm();
        #endif
        
        #if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void GP_Windows_ShowConfirm(
            string title,
            string description,
            string textConfirm,
            string textCancel,
            string invertButtonColors,
            string hideCancelButton
            );
        #endif

        public static void ShowConfirm(Action<bool> confirmCallback = null)
        {
            _onConfirm = confirmCallback;
#if !UNITY_EDITOR && UNITY_WEBGL
            GP_Windows_ShowDefaultConfirm();
#else
            if (GP_Play2Web.Call("WindowsShowConfirmDefault"))
                return;
            ConsoleLog("ShowConfirm called");
            _onConfirm?.Invoke(true);
            OnConfirm?.Invoke(true);
#endif
        }
        
        public static void ShowConfirm(ConfirmWindowData data, Action<bool> confirmCallback = null)
        {
            _onConfirm = confirmCallback;
#if !UNITY_EDITOR && UNITY_WEBGL
            GP_Windows_ShowConfirm(
                data.title, 
                data.description, 
                data.textConfirm, 
                data.textCancel, 
                data.invertButtonColors.ToString(),
                data.hideCancelButton.ToString());
#else
            if (GP_Play2Web.Call("WindowsShowConfirm", data.title, data.description, data.textConfirm, data.textCancel, data.invertButtonColors.ToString()))
                return;
            ConsoleLog("ShowConfirm called");
            _onConfirm?.Invoke(true);
            OnConfirm?.Invoke(true);
#endif
        }
    }

    [Serializable]
    public class ConfirmWindowData
    {
        public string title;
        public string description;
        public string textConfirm = "Confirm";
        public string textCancel = "Cancel";
        public bool invertButtonColors = false;
        public bool hideCancelButton = false;

        public ConfirmWindowData(
            string title = "", 
            string description = "", 
            string textConfirm = "", 
            string textCancel = "",
            bool invertButtonColors = false,
            bool hideCancelButton = false)
        {
            this.title = title;
            this.description = description;
            this.textConfirm = textConfirm;
            this.textCancel = textCancel;
            this.invertButtonColors = invertButtonColors;
            this.hideCancelButton = hideCancelButton;
        }
    }
    
}
