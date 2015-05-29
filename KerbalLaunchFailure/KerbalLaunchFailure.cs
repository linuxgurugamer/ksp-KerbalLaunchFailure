using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KerbalLaunchFailure
{
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
        public const string LocalPluginDataPath = "GameData/KerbalLaunchFailure/Plugins/PluginData";

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

        /// <summary>
        /// Called when script instance is being loaded.
        /// </summary>
        public void Awake()
        {
            // When a launch occurs, start failure script.
            GameEvents.onLaunch.Add(FailureStartHandler);

            // Need to keep track of when the game pauses or unpauses.
            GameEvents.onGamePause.Add(FailureGamePauseHandler);
            GameEvents.onGameUnpause.Add(FaulureGameUnpauseHandler);
        }

        /// <summary>
        /// Called every frame.
        /// </summary>
        public void Update()
        {
            // Only run if the failure script is active and the game is not paused.
            if (isFailureScriptActive && !isGamePaused)
            {
                RunFailure();
            }
        }

        /// <summary>
        /// Called when destroyed.
        /// </summary>
        public void OnDestroy()
        {
            DestroyFailureRun();
        }

        /// <summary>
        /// To be called when the failure script is to start it's work.
        /// </summary>
        /// <param name="eventReport">Event Report from action.</param>
        private void FailureStartHandler(EventReport eventReport)
        {
            if (Failure.Occurs())
            {
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
        }

        /// <summary>
        /// To be called when the game is unpaused.
        /// </summary>
        private void FaulureGameUnpauseHandler()
        {
            isGamePaused = false;
        }

        /// <summary>
        /// Activates the failure script.
        /// </summary>
        private void ActivateFailureRun()
        {
            isFailureScriptActive = true;
            isGamePaused = false;
            failure = new Failure();
            GameEvents.VesselSituation.onReachSpace.Add(FailureEndHandler);
        }

        /// <summary>
        /// Cleanup to remove all event handlers when the failure script is completed.
        /// </summary>
        private void DestroyFailureRun()
        {
            isFailureScriptActive = false;
            isGamePaused = false;
            failure = null;
            GameEvents.onLaunch.Remove(FailureStartHandler);
            GameEvents.onGamePause.Remove(FailureGamePauseHandler);
            GameEvents.onGameUnpause.Remove(FaulureGameUnpauseHandler);
            GameEvents.VesselSituation.onReachSpace.Remove(FailureEndHandler);
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
