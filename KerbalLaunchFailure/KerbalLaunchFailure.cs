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
        private const int ChanceOfRUD = 50;

        /// <summary>
        /// Target chance that the failure will occur in flight.
        /// </summary>
        private const float ChanceWillOccurDuringFlight = 0.9F;
#endif

        /// <summary>
        /// Chance that the part explosion will propogate to attached part.
        /// </summary>
        private const float FailurePropagateChance = 0.8F;

        /// <summary>
        /// The percentage resource remaining that causes the tank to have a 100% explosion chance.
        /// </summary>
        private const float ExplosiveTankThreshold = 0.05F;

        /// <summary>
        /// If set true, the failure rate will decrease with each propagation.
        /// </summary>
        private const bool PropagationChanceDecreases = true;

        /// <summary>
        /// Rough estimate for how long the script should run, not accounting for the Atm to Space transition.
        /// </summary>
        private const int TimeToOrbit = 4 * 60;

        /// <summary>
        /// The list of parts that will explode.
        /// </summary>
        private List<Part> doomedParts;

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
#if DEBUG
            Debug.LogWarning(PluginName + " :: totalTicks used is " + totalTicks + ".");
            Debug.LogWarning(PluginName + " :: calculatedChancePerTick is " + calculatedChancePerTick + ".");
#endif
        }

        /// <summary>
        /// Checks if there is a failure now, and if so, causes the failure.
        /// </summary>
        private void CheckForFailure()
        {
            if (rand.Next(0, calculatedChancePerTick) == 0)
            {
                CauseFailure();
            }
        }

        /// <summary>
        /// Causes an active engine to explode and propagate.
        /// </summary>
        private void CauseFailure()
        {
            // Find engines
            List<Part> activeEngineParts = FlightGlobals.ActiveVessel.GetActiveParts().Where(o => PartIsActiveEngine(o)).ToList();

            // If there are no active engines, skip this attempt.
            if (activeEngineParts.Count == 0)
            {
                return;
            }

#if DEBUG
            Debug.LogError(PluginName + " :: Launch Failure!!");
#endif

            // The parts that will explode.
            doomedParts = new List<Part>();

            // Determine the starting part.
            int startingPartIndex = rand.Next(0, activeEngineParts.Count);
            Part startingPart = activeEngineParts[startingPartIndex];

            // Add the starting part to the doomed parts list.
            doomedParts.Add(startingPart);

            // Propagate to sourrounding parts.
            FailurePropagate(startingPart, FailurePropagateChance);

            // Explode each doomed part.
            foreach (Part part in doomedParts)
            {
#if DEBUG
                Debug.LogError(PluginName + " :: " + part.partInfo.title + " (" + part.flightID + ") was doomed to explode.");
#endif
                // Flight Log <-- Thanks to ferram4 for code inspiration.
                if (part == startingPart)
                {
                    FlightLogger.eventLog.Add("[" + FormatMissionTime(FlightGlobals.ActiveVessel.missionTime) + "] Random failure and disassembly of " + part.partInfo.title + ".");
                }
                else
                {
                    FlightLogger.eventLog.Add("[" + FormatMissionTime(FlightGlobals.ActiveVessel.missionTime) + "] " + part.partInfo.title + " disassembly due to failure of " + startingPart.partInfo.title + ".");
                }

                part.explode();
            }

            // Destroy the script.
            DestroyFailureRun();
        }

        /// <summary>
        /// Propagates failure through surrounding parts.
        /// </summary>
        /// <param name="part">The parent part.</param>
        /// <param name="failureChance">Chance of failure for the next part.</param>
        private void FailurePropagate(Part part, double failureChance)
        {
            List<Part> potentialParts = new List<Part>();

            // Calculate the next propagation's failure chance.
            double nextFailureChance = (PropagationChanceDecreases) ? failureChance * FailurePropagateChance : failureChance;

            // Parent
            if (!doomedParts.Contains(part.parent))
            {
                potentialParts.Add(part.parent);
            }

            // Children
            foreach (Part childPart in part.children)
            {
                if (!doomedParts.Contains(childPart))
                {
                    potentialParts.Add(childPart);
                }
            }

            // For each potential part, see if it fails and then propagate it.
            foreach (Part potentialPart in potentialParts)
            {
                double thisFailureChance = (PartIsExplosiveFuelTank(potentialPart)) ? 1 : failureChance;
                if (!doomedParts.Contains(potentialPart) && rand.NextDouble() < thisFailureChance)
                {
                    doomedParts.Add(potentialPart);
                    FailurePropagate(potentialPart, nextFailureChance);
                }
            }
        }

        /// <summary>
        /// Determines if part is an active engine.
        /// </summary>
        /// <param name="part">The part to check.</param>
        /// <returns>True if an active engine, false if not.</returns>
        private static bool PartIsActiveEngine(Part part)
        {
            // Thanks to Ippo for the code inspiration here.
            List<ModuleEngines> engineModules = part.Modules.OfType<ModuleEngines>().ToList();
            List<ModuleEnginesFX> engineFXModules = part.Modules.OfType<ModuleEnginesFX>().ToList();

            foreach (ModuleEngines engineModule in engineModules)
            {
                if (engineModule.enabled && engineModule.EngineIgnited && engineModule.currentThrottle > 0)
                {
                    return true;
                }
            }

            foreach (ModuleEnginesFX engineModule in engineFXModules)
            {
                if (engineModule.enabled && engineModule.EngineIgnited && engineModule.currentThrottle > 0)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Determines if the part has enough explosive fuel to guarantee an explosion.
        /// </summary>
        /// <param name="part">The part to check.</param>
        /// <returns>True if valid, false is not.</returns>
        private static bool PartIsExplosiveFuelTank(Part part)
        {
            // There's gotta be a better way to do this.
            foreach (PartResource resource in part.Resources)
            {
                if (resource.maxAmount > 0 && resource.amount / resource.maxAmount > ExplosiveTankThreshold)
                {
                    switch (resource.resourceName)
                    {
                        case "LiquidFuel":
                        case "Oxidizer":
                        case "MonoPropellant":
                        case "SolidFuel":
                            return true;
                        default:
                            break;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Formats the mission elapsed time for the Flight Event log.
        /// </summary>
        /// <param name="rawMissionTime">The mission time.</param>
        /// <returns>String formatted mission elapse time.</returns>
        public static string FormatMissionTime(double rawMissionTime)
        {
            int time = (int)rawMissionTime % 3600;
            int seconds = time % 60;
            int minutes = (time / 60) % 60;
            int hours = (time / 3600);
            return hours.ToString("D2") + ":" + minutes.ToString("D2") + ":" + seconds.ToString("D2");
        }
    }
}
