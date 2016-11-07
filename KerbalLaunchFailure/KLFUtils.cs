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
        public static bool PartIsActiveEngine(Part part, bool techRequired = true)
        {
            if (part == null || !HighLogic.CurrentGame.Parameters.CustomParams<KLFCustomParams2>().allowEngineFailures && !HighLogic.CurrentGame.Parameters.CustomParams<KLFCustomParams2>().allowEngineUnderthrust)
                return false;
            if (HighLogic.CurrentGame.Mode != Game.Modes.SANDBOX && !techRequired)
            {
                ProtoTechNode techNode = ResearchAndDevelopment.Instance.GetTechState(part.partInfo.TechRequired);
                if (techNode != null && techNode.state == RDTech.State.Available)
                    return false;

            }
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

        public static bool PartIsRadialDecoupler(Part part, bool techRequired = true)
        {
            if (part == null || !HighLogic.CurrentGame.Parameters.CustomParams<KLFCustomParams2>().allowRadialDecouplerFailures)
                return false;


            if (HighLogic.CurrentGame.Mode !=  Game.Modes.SANDBOX && !techRequired)
            {
                if (HighLogic.CurrentGame.Mode != Game.Modes.SANDBOX && part.partInfo.TechRequired != null)
                {
                    ProtoTechNode techNode = ResearchAndDevelopment.Instance.GetTechState(part.partInfo.TechRequired);

                    if (techNode != null && techNode.state == RDTech.State.Available)
                        return false;
                }
            }

            List<ModuleAnchoredDecoupler> anchoredDecouplers = part.Modules.OfType<ModuleAnchoredDecoupler>().ToList();

            if (anchoredDecouplers.Count > 0)
            {
                Log.Info("anchoredDecoupler: " + part.partInfo.title);
                return true;
            }
            return false;
        }

        public static bool PartIsControlSurface(Part part, bool techRequired = true)
        {
            if (part == null || !HighLogic.CurrentGame.Parameters.CustomParams<KLFCustomParams2>().allowControlSurfaceFailures)
                return false;
            if (HighLogic.CurrentGame.Mode != Game.Modes.SANDBOX && !techRequired)
            {
                ProtoTechNode techNode = ResearchAndDevelopment.Instance.GetTechState(part.partInfo.TechRequired);
                if (techNode != null && techNode.state == RDTech.State.Available)
                    return false;
            }

            List<ModuleControlSurface> controlSurface = part.Modules.OfType<ModuleControlSurface>().ToList();
            if (controlSurface.Count > 0)
            {
                Log.Info("controlSurface: " + part.partInfo.title);
                return true;
            }
            return false;
        }

        public static bool PartIsStrutOrFuelLine(CompoundPart part, bool techRequired = true)
        {
            if (part == null || !HighLogic.CurrentGame.Parameters.CustomParams<KLFCustomParams2>().allowStrutFuelFailures)
                return false;
            if (HighLogic.CurrentGame.Mode != Game.Modes.SANDBOX && !techRequired)
            {
                ProtoTechNode techNode = ResearchAndDevelopment.Instance.GetTechState(part.partInfo.TechRequired);
                if (techNode != null && techNode.state == RDTech.State.Available)
                    return false;
            }

            if (part.name != "fuelLine" && part.name != "strutConnector")
                return false;
            if (part.attachState == CompoundPart.AttachState.Detached || part.attachState == CompoundPart.AttachState.Attaching)
                return false;
            if (part.target == part.parent)
                return false;
            return true;
        }

        public static bool ExperimentalPartsPresentAndActive()
        {
            if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER)
                return false;
            List<Part> activeEngineParts = FlightGlobals.ActiveVessel.GetActiveParts().Where(o => KLFUtils.PartIsActiveEngine(o, false)).ToList();
            List<Part> radialDecouplers = FlightGlobals.ActiveVessel.Parts.Where(o => KLFUtils.PartIsRadialDecoupler(o, false)).ToList();
            List<Part> controlSurfaces = FlightGlobals.ActiveVessel.Parts.Where(o => KLFUtils.PartIsControlSurface(o, false)).ToList();
            List<CompoundPart> strutsAndFuelLines = FlightGlobals.ActiveVessel.Parts.OfType<CompoundPart>().Where(o => KLFUtils.PartIsStrutOrFuelLine(o, false)).ToList();

            int i = activeEngineParts.Count + radialDecouplers.Count + controlSurfaces.Count + strutsAndFuelLines.Count;

            return i > 0;
        }

        /// <summary>
        /// Determines if the part has enough explosive fuel to guarantee an explosion.
        /// </summary>
        /// <param name="part">The part to check.</param>
        /// <returns>True if valid, false is not.</returns>
        public static bool PartIsExplosiveFuelTank(Part part, bool techRequired = true)
        {
            if (part == null)
                return false;
            if (HighLogic.CurrentGame.Mode != Game.Modes.SANDBOX && !techRequired)
            {
                ProtoTechNode techNode = ResearchAndDevelopment.Instance.GetTechState(part.partInfo.TechRequired);
                if (techNode != null && techNode.state == RDTech.State.Available)
                    return false;
            }
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
            Log.Error(KerbalLaunchFailureController.PluginName + " :: " + message);
        }
    }
}
