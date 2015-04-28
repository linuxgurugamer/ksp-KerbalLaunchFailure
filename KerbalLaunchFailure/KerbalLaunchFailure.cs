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
        private const string PluginName = "Kerbal Launch Failure";

        /// <summary>
        /// The plugin's abbreviated name.
        /// </summary>
        private const string PluginAbbreviation = "KLF";

#if DEBUG
        /// <summary>
        /// Chance of launch failure: always.
        /// </summary>
        private const int ChanceOfRUD = 1;

        /// <summary>
        /// Target chance that the failure will occur in flight.
        /// </summary>
        private const float ChanceWillOccurDuringFlight = 0.99F;
#else
        /// <summary>
        /// Chance of launch failure: 1 in 'ChanceOfRUD'
        /// </summary>
        private const int ChanceOfRUD = 51;

        /// <summary>
        /// Target chance that the failure will occur in flight.
        /// </summary>
        private const float ChanceWillOccurDuringFlight = 0.9F;
#endif

        /// <summary>
        /// Chance that the part explosion will propogate to attached part.
        /// </summary>
        private const float FailurePropagateChance = 0.95F;

        /// <summary>
        /// Rough estimate for how long the script should run, not accounting for the Atm to Space transition.
        /// </summary>
        private const int TimeToOrbit = 4 * 60;

        /// <summary>
        /// The calclulated change per game tick that the failure does occur: 1 in 'calculatedChancePerTick'
        /// </summary>
        private int calculatedChancePerTick;

        /// <summary>
        /// Is the failure script active?
        /// </summary>
        private bool isActive = false;

        /// <summary>
        /// Is the game paused?
        /// </summary>
        private bool isGamePaused = false;

        /// <summary>
        /// The RNG used.
        /// </summary>
        private static System.Random rand = new System.Random();

        /// <summary>
        /// Called when script instance is being loaded.
        /// </summary>
        public void Awake()
        {
            // Calculate the chance per tick.
            CalculateChancePerTick();

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
            if (isActive && !isGamePaused)
            {
                CheckForFailure();
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
            if (rand.Next(0, ChanceOfRUD) == 0)
            {
                ActivateFailureRun();
            }
            else
            {
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
            isActive = true;
            GameEvents.VesselSituation.onReachSpace.Add(FailureEndHandler);
        }

        /// <summary>
        /// Cleanup to remove all event handlers when the failure script is completed.
        /// </summary>
        private void DestroyFailureRun()
        {
            isActive = false;
            GameEvents.onLaunch.Remove(FailureStartHandler);
            GameEvents.onGamePause.Remove(FailureGamePauseHandler);
            GameEvents.onGameUnpause.Remove(FaulureGameUnpauseHandler);
            GameEvents.VesselSituation.onReachSpace.Remove(FailureEndHandler);
        }

        /// <summary>
        /// Calculates the chance per tick the launch will fail. This is dependent on the setting:
        /// PHYSICS_FRAME_DT_LIMIT
        /// </summary>
        private void CalculateChancePerTick()
        {
            float ticksPerSecond = 1 / GameSettings.PHYSICS_FRAME_DT_LIMIT;
            float totalTicks = ticksPerSecond * TimeToOrbit;
            calculatedChancePerTick = (int)(1.0 / (1.0 - Math.Pow(1.0 - ChanceWillOccurDuringFlight, 1.0 / totalTicks)));
        }

        private void CheckForFailure()
        {
            if (rand.Next(0, calculatedChancePerTick) == 0)
            {
                CauseFailure();
            }
        }

        private void CauseFailure()
        {
#if DEBUG
            Debug.LogWarning(PluginName + " :: OMGZ!!! Launch Failure!! ABORT!!");
#endif
        }
    }
}
