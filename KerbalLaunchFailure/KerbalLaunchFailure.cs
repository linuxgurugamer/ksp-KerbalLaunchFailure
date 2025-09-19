using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.UI.Screens;
using ClickThroughFix;
using ToolbarControl_NS;

namespace KerbalLaunchFailure
{
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class RegisterToolbar : MonoBehaviour
    {
        void Start()
        {
            ToolbarControl.RegisterMod(KerbalLaunchFailureController.MODID, KerbalLaunchFailureController.MODNAME);
        }
    }

    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class KerbalLaunchFailureController : MonoBehaviour
    {
        /// <summary>
        /// The plugin's name.
        /// </summary>
        public const string PluginName = "Kerbal Launch Failure";

        /// <summary>
        /// The plugin's abbreviated name.
        /// </summary>
        public const string PluginAbbreviation = "KLF";

        /// <summary>
        /// The plugin's PluginData directory.
        /// </summary>
       // public const string LocalPluginDataPath = "GameData/KerbalLaunchFailure/PluginData/";

        /// <summary>
        /// Is the failure script active?
        /// </summary>
        private bool isFailureScriptActive = false;

        /// <summary>
        /// Is the game paused?
        /// </summary>
        private bool isGamePaused = false;

        /// <summary>
        /// The failure that runs.
        /// </summary>
        private Failure failure;

        public static bool soundPlaying = false;
        public static FXGroup alarmSound = null;
        public static string LocalAlarmSoundDir = "KerbalLaunchFailure/Sounds/";

        private int myWindowId;
        private Rect windowRect;
        const float WINDOW_WIDTH = 225;
        const float WINDOW_HEIGHT = 200;

        //internal ApplicationLauncherButton launcherButton = null;
        ToolbarControl toolbarControl;
        bool abortButtonYellow = false;
        public Texture2D GetImage(String path, int width, int height)
        {
            Texture2D img = new Texture2D(width, height, TextureFormat.ARGB32, false);
            img = GameDatabase.Instance.GetTexture(path, false);
            return img;
        }

        internal const string MODID = "KerbalLaunchFailure_NS";
        internal const string MODNAME = "Kerbal Launch Failure";

        private void OnGUIApplicationLauncherReady()
        {
            toolbarControl = gameObject.AddComponent<ToolbarControl>();
            toolbarControl.AddToAllToolbars(abortWindow, abortWindow,
                ApplicationLauncher.AppScenes.FLIGHT,
                MODID,
                "kerbalLaunchfailureButton",
                "KerbalLaunchFailure/PluginData/Textures/allSystemsGo",
                "KerbalLaunchFailure/PluginData/Textures/allSystemsGo_24",
                MODNAME
            );
        }

        void abortWindow()
        {
            Log.Info("abortWindow  isFailureScriptActive: " + isFailureScriptActive.ToString() + "   KerbalLaunchFailureController.soundPlaying: " + KerbalLaunchFailureController.soundPlaying.ToString());
            if (!isFailureScriptActive || !KerbalLaunchFailureController.soundPlaying)
                return;
            failure.disableAlarm();
        }

        double lastUpdate = 0f;
        void updateToolbarButton()
        {
            if (toolbarControl == null || failure == null)
                return;
            if (failure.failurecomplete)
            {
                toolbarControl.SetTexture("KerbalLaunchFailure/PluginData/Textures/failed", "KerbalLaunchFailure/PluginData/Textures/failed_24");
                failure.failurecomplete = false;
                return;
            }
            if (!isFailureScriptActive || (!failure.alarmDisabled && !KerbalLaunchFailureController.soundPlaying))
                return;
            if (Planetarium.GetUniversalTime() - lastUpdate > 1)
            {
                lastUpdate = Planetarium.GetUniversalTime();

                abortButtonYellow = !abortButtonYellow;
                if (abortButtonYellow)
                    toolbarControl.SetTexture("KerbalLaunchFailure/PluginData/Textures/warning", "KerbalLaunchFailure/PluginData/Textures/warning_24");
                else
                    toolbarControl.SetTexture("KerbalLaunchFailure/PluginData/Textures/warning2", "KerbalLaunchFailure/PluginData/Textures/warning2_24");
            }
        }

        public void removeLauncherButtons()
        {
            if (toolbarControl != null)
            {
                toolbarControl.OnDestroy();
                Destroy(toolbarControl);
            }
        }


        /// <summary>
        /// Called when script instance is being loaded.
        /// </summary>
        public void Awake()
        {
            if (!HighLogic.CurrentGame.Parameters.CustomParams<KLF_1>().enabled)
                return;
            // When a launch occurs, start failure script.
            GameEvents.onLaunch.Add(FailureStartHandler);

            // Need to keep track of when the game pauses or unpauses.
            GameEvents.onGamePause.Add(FailureGamePauseHandler);
            GameEvents.onGameUnpause.Add(FailureGameUnpauseHandler);
            GameEvents.OnGameSettingsApplied.Add(ReloadSettings);

            myWindowId = GetInstanceID();
            windowRect = new Rect(0, 0, WINDOW_WIDTH, WINDOW_HEIGHT);
            windowRect.center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

            if (toolbarControl == null)
            {
                OnGUIApplicationLauncherReady();
            }
        }

        void OnGUI()
        {
            //Log.Info("OnGUI");

            if (Failure.Instance == null || FlightDriver.Pause)
                return;
            
            if (Failure.Instance.failureTime == 0 || Failure.Instance.failureTime == Double.MaxValue)
                return;

            GUI.skin = HighLogic.Skin;
            windowRect = ClickThruBlocker.GUILayoutWindow(myWindowId, windowRect, Window, "AutoAbort Cancel/Reset");
        }

        public void Window(int windowID)
        {
            GUI.skin = HighLogic.Skin;
            var oldColor = GUI.backgroundColor;
            var bstyle = new GUIStyle(GUI.skin.button);
            bstyle.normal.textColor = Color.yellow;
            //bstyle.normal.background = new Texture2D(2,2);
           

            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("AutoAbort in: " + ((Failure.Instance.failureTime + KLFSettings.Instance.AutoAbortDelay) - Planetarium.GetUniversalTime()).ToString("n2") + " seconds");
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("Cancel AutoAbort", bstyle, GUILayout.Width(170.0f), GUILayout.Height(40.0f)))
            {
                Failure.Instance.failureTime = Double.MaxValue;
            }
            GUILayout.FlexibleSpace();
            GUI.backgroundColor = oldColor;
            GUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUI.backgroundColor = Color.yellow;
            if (GUILayout.Button("Reset AutoAbort Timer", bstyle, GUILayout.Width(170.0f), GUILayout.Height(35.0f)))
            {
                Failure.Instance.failureTime = Planetarium.GetUniversalTime();
            }
            GUILayout.FlexibleSpace();
            GUI.backgroundColor = oldColor;
            GUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            // GUI.backgroundColor = Color.blue;
            //bstyle.normal.textColor = Color.red;
            if (GUILayout.Button("Abort Immediately", bstyle, GUILayout.Width(170.0f), GUILayout.Height(35.0f)))
            {
                Failure.Instance.AddAndReportScience();
                Failure.Instance.activeVessel.ActionGroups.SetGroup(KSPActionGroup.Abort, true);
            }
            GUILayout.FlexibleSpace();
            GUI.backgroundColor = oldColor;
            GUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();

            GUILayout.EndVertical();
            GUI.DragWindow();
        }
        
        /// <summary>
        /// Reload settings if necessary.
        /// </summary>

        void ReloadSettings()
        {
            if (KLFSettings.Instance != null)
                KLFSettings.Instance.LoadSettings();
        }

        /// <summary>
        /// Called every frame.
        /// </summary>
        public void FixedUpdate()
        {
            if (!HighLogic.CurrentGame.Parameters.CustomParams<KLF_1>().enabled)
                return;

            // Only run if the failure script is active and the game is not paused.
            if ((failure != null && failure.failurecomplete) || (isFailureScriptActive && !isGamePaused))
            {
                updateToolbarButton();
                RunFailure();
            }
        }

        /// <summary>
        /// Called when destroyed.
        /// </summary>
        public void OnDestroy()
        {
            removeLauncherButtons();
            DestroyFailureRun();
            Failure.Instance = null;
        }

        /// <summary>
        /// To be called when the failure script is to start it's work.
        /// </summary>
        /// <param name="eventReport">Event Report from action.</param>
        private void FailureStartHandler(EventReport eventReport)
        {
            if (Failure.Occurs())
            {
                
                SoundManager.LoadSound(LocalAlarmSoundDir + KLFSettings.Instance.AlarmSoundFile, "AlarmSound");
                alarmSound = new FXGroup("AlarmSound");
                SoundManager.CreateFXSound(null, alarmSound, "AlarmSound", true, 50f);

                ActivateFailureRun();
            }
            else
            {
                // Since there will be no need to run the script, destroy it.
                DestroyFailureRun();
            }
        }

        /// <summary>
        /// To be called when the failure script is to stop.
        /// </summary>
        /// <param name="vessel">The vessel that performed the action.</param>
        private void FailureEndHandler(Vessel vessel)
        {
            DestroyFailureRun();
        }

        /// <summary>
        /// To be called when the game is paused.
        /// </summary>
        private void FailureGamePauseHandler()
        {
            isGamePaused = true;
            if (alarmSound != null)
            {
                alarmSound.audio.Stop();
            }
        }

        /// <summary>
        /// To be called when the game is unpaused.
        /// </summary>
        private void FailureGameUnpauseHandler()
        {
            isGamePaused = false;
            if (alarmSound != null && KerbalLaunchFailureController.soundPlaying)
                alarmSound.audio.Play();
        }

        /// <summary>
        /// Activates the failure script.
        /// </summary>
        private void ActivateFailureRun()
        {
            Log.Info("ActivateFailureRun");
            isFailureScriptActive = true;
            isGamePaused = false;
            failure = new Failure();
            if (HighLogic.CurrentGame.Parameters.CustomParams<KLFCustomParams2>().allowEngineUnderthrust)
            {
                failure.overThrust = KLFUtils.RndNextDouble() >= HighLogic.CurrentGame.Parameters.CustomParams<KLFCustomParams2>().engineUnderthrustProb;
            }
            else
                failure.overThrust =true;
            failure.launchTime = Planetarium.GetUniversalTime();
            GameEvents.VesselSituation.onReachSpace.Add(FailureEndHandler);
        }

        /// <summary>
        /// Cleanup to remove all event handlers when the failure script is completed.
        /// </summary>
        private void DestroyFailureRun()
        {
            Log.Info("DestroyFailureRun");
            updateToolbarButton();
            isFailureScriptActive = false;
            isGamePaused = false;
            failure = null;
            if (alarmSound != null)
                alarmSound.audio.Stop();
            KerbalLaunchFailureController.soundPlaying = false;
            GameEvents.onLaunch.Remove(FailureStartHandler);
            GameEvents.onGamePause.Remove(FailureGamePauseHandler);
            GameEvents.onGameUnpause.Remove(FailureGameUnpauseHandler);
            GameEvents.VesselSituation.onReachSpace.Remove(FailureEndHandler);
            GameEvents.OnGameSettingsApplied.Remove(ReloadSettings);

        }

        /// <summary>
        /// Runs the failure.
        /// </summary>
        private void RunFailure()
        {
            if (!failure.Run())
            {
                DestroyFailureRun();
            }
        }
    }
}
