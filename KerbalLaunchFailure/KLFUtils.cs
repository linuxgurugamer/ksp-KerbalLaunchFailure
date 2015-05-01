using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KerbalLaunchFailure
{
    public static class KLFUtils
    {
        /// <summary>
        /// The percentage resource remaining that causes the tank to have a 100% explosion chance.
        /// </summary>
        private const float ExplosiveTankThreshold = 0.05F;

        private static System.Random rng = new System.Random();

        public static System.Random RNG { get { return rng; } }

        public static float GameTicksPerSecond
        {
            get { return 1.0F / GameSettings.PHYSICS_FRAME_DT_LIMIT; }
        }

        /// <summary>
        /// Determines if part is an active engine.
        /// </summary>
        /// <param name="part">The part to check.</param>
        /// <returns>True if an active engine, false if not.</returns>
        public static bool PartIsActiveEngine(Part part)
        {
            // Thanks to Ippo for the code inspiration here.
            List<ModuleEngines> engineModules = part.Modules.OfType<ModuleEngines>().ToList();
            List<ModuleEnginesFX> engineFXModules = part.Modules.OfType<ModuleEnginesFX>().ToList();

            foreach (ModuleEngines engineModule in engineModules)
            {
                if (engineModule.enabled && engineModule.EngineIgnited && engineModule.currentThrottle > 0 && !engineModule.getFlameoutState)
                {
                    return true;
                }
            }

            foreach (ModuleEnginesFX engineModule in engineFXModules)
            {
                if (engineModule.enabled && engineModule.EngineIgnited && engineModule.currentThrottle > 0 && !engineModule.getFlameoutState)
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
        public static bool PartIsExplosiveFuelTank(Part part)
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

        public static void LogFlightData(Vessel vessel, string message)
        {
            string missionTimeString = "[" + FormatMissionTime(vessel.missionTime) + "]";
            FlightLogger.eventLog.Add(missionTimeString + ": " + message);
        }

        public static void LogDebugMessage(string message)
        {
            Debug.LogError(KerbalLaunchFailureController.PluginName + " :: " + message);
        }
    }
}
