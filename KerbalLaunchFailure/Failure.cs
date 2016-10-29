using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.UI.Screens;

namespace KerbalLaunchFailure
{
    public class Failure
    {
        public static Failure Instance;
        /// <summary>
        /// The active flight vessel.
        /// </summary>
        public readonly Vessel activeVessel;

        public float activeVesselCost = 0f;
        public float scienceAfterFailure = 0f;
        public float preFailureWarningTime = 0f;

        /// <summary>
        /// The active flight vessel's current parent celestial body.
        /// </summary>
        private readonly CelestialBody currentCelestialBody;

        /// <summary>
        /// The minimum altitude in which the failure will occur.
        /// </summary>
        private readonly int altitudeFailureOccurs;

        /// <summary>
        /// The starting part of the failure.
        /// </summary>
        private Part startingPart;

        /// <summary>
        /// The engine module of the starting part.
        /// </summary>
        private ModuleEngines startingPartEngineModule;

        enum FailureType { none, engine, radialDecoupler, controlSurface, strutOrFuelLine };
        private FailureType failureType = FailureType.none;
        //private bool startingRadialDecouplerModule = false;
        private int decouplerForceCnt = 0;

        /// <summary>
        /// The amount of extra force to add to the failing engine.  In an underthrust situation, the amount to remove
        /// </summary>
        private int thrustOverload;
        private float underThrustStart = 100f;
        private float underThrustEnd = 100f;

        /// <summary>
        /// The list of parts set to explode after the starting part.
        /// </summary>
        private List<Part> doomedParts;

        /// <summary>
        /// The number of ticks between successive failures of the same part.
        /// </summary>
        private int ticksBetweenPartFailures;

        /// <summary>
        /// The number of ticks since the failure started.
        /// </summary>
        private int ticksSinceFailureStart;

        /// <summary>
        /// Sound the alarm.
        /// </summary>
        // private Alarm alarm;

        public double launchTime = 0;
        public double failureTime = 0;
        double preFailureTime = 0;
        double underthrustTime = 0;
        public bool overThrust = true;


        static bool experimentalPartFailure = false;
        static bool partFailure = false;

        float CalcVesselCosts(Vessel v)
        {
            ShipConstruct sc = new ShipConstruct();

            foreach (Part p in v.parts)
            {
                sc.parts.Add(p);
            }
            float drycost, fuelcost;
            float shipCost = sc.GetShipCosts(out drycost, out fuelcost);
            return shipCost;
        }

        float CalcScienceReward(float vesselCost, Vessel v, Part startingPart)
        {
            Log.Info("CalcScienceReward");

            if (KLFSettings.Instance.ScienceAtFailure &&
                HighLogic.CurrentGame.Mode != Game.Modes.SANDBOX)
            {
                if (startingPart != null)
                {

                    if (ResearchAndDevelopment.Instance != null)
                    {
                        ProtoTechNode techNode = ResearchAndDevelopment.Instance.GetTechState(startingPart.partInfo.TechRequired);
                        if (techNode != null)
                        {
                            // Need to reverse the science adjustment here
                            float scienceAdjustmentAdj = 110 - KLFSettings.Instance.ScienceAdjustment;

                            return (float)(techNode.scienceCost * Math.Log(vesselCost, scienceAdjustmentAdj));
                        }
                        else
                            Log.Info("techNode is null");
                    }
                    else
                        Log.Info("ResearchAndDevelopment.Instance is null");
                }
                else
                {
                    float r = (float)(Math.Log(vesselCost, 10) * UnityEngine.Random.Range(0.75f, 1.25f));
                    if (r < 1)
                        r *= 10f;
                    if (r < 1)
                        r = 1.5f;
                    return r;
                }

            }

            return 0f;

        }

        /// <summary>
        /// Constructor for Failure object.
        /// </summary>
        public Failure()
        {
            Instance = this;
            // Gather info about current active vessel.
            activeVessel = FlightGlobals.ActiveVessel;
            activeVesselCost = CalcVesselCosts(activeVessel);
            //scienceAfterFailure = CalcScienceReward(activeVesselCost, activeVessel);

            currentCelestialBody = activeVessel.mainBody;

            // This plugin is only targeted at atmospheric failures.
            if (currentCelestialBody.atmosphere)
            {
                altitudeFailureOccurs = KLFUtils.RNG.Next(0, (int)(currentCelestialBody.atmosphereDepth * KLFSettings.Instance.MaxFailureAltitudePercentage));
#if DEBUG
                KLFUtils.LogDebugMessage("Failure will occur at an altitude of " + altitudeFailureOccurs);
#endif
            }
            else
            {
                altitudeFailureOccurs = 0;
            }
            //alarm = new KerbalLaunchFailure.Alarm();

        }

        public void HighlightSinglePart(Color highlightC, Color edgeHighlightColor, Part p)
        {
            p.SetHighlightDefault();
            p.SetHighlightType(Part.HighlightType.AlwaysOn);
            p.SetHighlight(true, false);
            p.SetHighlightColor(highlightC);
            p.highlighter.ConstantOn(edgeHighlightColor);
            p.highlighter.SeeThroughOn();

        }

        public void UnHighlightParts(Part p)
        {
            p.SetHighlightDefault();
            p.highlighter.Off();
        }

        void HighlightFailingPart(Part part)
        {
            if (HighLogic.CurrentGame.Parameters.CustomParams<KLFCustomParams>().highlightFailingPart)
                HighlightSinglePart(XKCDColors.OffWhite, XKCDColors.Red, part);
        }

        /// <summary>
        /// The main method that runs the failure script.
        /// </summary>
        /// <returns></returns>
        public bool Run()
        {
            //Log.Info("Run, activeVessel.altitude: " + activeVessel.altitude.ToString() + "   altitudeFailureOccurs: " + altitudeFailureOccurs.ToString());

            // Kill the failure script if there is no atmosphere.
            if (!currentCelestialBody.atmosphere) return false;

            // Wait until the minimum time before failure has passed
            if (Planetarium.GetUniversalTime() - launchTime < HighLogic.CurrentGame.Parameters.CustomParams<KLFCustomParams>().minTimeBeforeFailure)
                return true;

            // Wait until the vessel is past the altitude threshold AND the max time before failure hasn't passed
            if (activeVessel.altitude < altitudeFailureOccurs && Planetarium.GetUniversalTime() - launchTime < HighLogic.CurrentGame.Parameters.CustomParams<KLFCustomParams>().maxTimeBeforeFailure) return true;

            // Prepare the doomed parts if not done so.
            if (startingPart == null && doomedParts == null)
            {
                PrepareStartingPart();
            }

            // If parts have been found.
            if (startingPart != null || doomedParts != null)
            {
                if (startingPart != null)
                {
                    if (preFailureTime == 0)
                    {
                        preFailureTime = Planetarium.GetUniversalTime();

                        // Need to display message and warning sound
                        if (KLFSettings.Instance.PreFailureWarningTime > 0)
                        {
                            Log.Info("Imminent Failure");
                            ScreenMessages.PostScreenMessage("Imminent Failure on " + startingPart.partInfo.title, 1.0f, ScreenMessageStyle.UPPER_CENTER);
                            HighlightFailingPart(startingPart);
                            KerbalLaunchFailureController.soundPlaying = true;
                            KerbalLaunchFailureController.alarmSound.audio.Play();

                        }
                    }
                    else
                    {
                        if (!CauseStartingPartFailure())
                        {
                            if (KerbalLaunchFailureController.alarmSound != null)
                            {
                                KerbalLaunchFailureController.alarmSound.audio.Stop();
                                AddAndReportScience();
                            }
                            return false;
                        }
                    }
                }
                else if (doomedParts != null)
                {
                    try
                    {
                        // Will attempt to explode the next part.
                        ExplodeNextDoomedPart();
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        // This occurs when there are no more parts to explode.
                        if (KerbalLaunchFailureController.alarmSound != null)
                            KerbalLaunchFailureController.alarmSound.audio.Stop();
                        KerbalLaunchFailureController.soundPlaying = false;
                        AddAndReportScience();

                        return false;
                    }
                }
                // Increase the ticks.
                ticksSinceFailureStart++;
            }
            // Tell caller to continue run.
            return true;
        }

        public void AddAndReportScience()
        {
            if (scienceAfterFailure > 0)
            {
                KLFUtils.LogFlightData(activeVessel, scienceAfterFailure.ToString() + "  Science gained due to lessons learned.");
                ResearchAndDevelopment.Instance.AddScience(scienceAfterFailure, TransactionReasons.VesselLoss);
            }

        }
        /// <summary>
        /// Determines the starting part in the failure.
        /// </summary>
        private void PrepareStartingPart()
        {


            Log.Info("PrepareStartingPart");
            // Find engines
            bool b = !experimentalPartFailure;

            List<Part> activeEngineParts = activeVessel.GetActiveParts().Where(o => KLFUtils.PartIsActiveEngine(o, b)).ToList();
            List<Part> radialDecouplers = activeVessel.Parts.Where(o => KLFUtils.PartIsRadialDecoupler(o, b)).ToList();
            List<Part> controlSurfaces = activeVessel.Parts.Where(o => KLFUtils.PartIsControlSurface(o, b)).ToList();
            List<CompoundPart> strutsAndFuelLines = activeVessel.Parts.OfType<CompoundPart>().Where(o => KLFUtils.PartIsStrutOrFuelLine(o, b)).ToList();

            int cnt = activeEngineParts.Count + radialDecouplers.Count + controlSurfaces.Count + strutsAndFuelLines.Count;
            // If there are no active engines or radial decouplers, skip this attempt.
            if (cnt == 0) return;

            // Determine the starting part.
            int startingPartIndex = KLFUtils.RNG.Next(0, cnt);
            Log.Info("activeEngineParts.Count: " + activeEngineParts.Count.ToString() + "    radialDecouplers.Count: " + radialDecouplers.Count.ToString() +
                "   controlSurfaces.Count: " + controlSurfaces.Count.ToString() + "   strutsAndFuelLines.Count: " + strutsAndFuelLines.Count);
            Log.Info("startingPartIndex: " + startingPartIndex.ToString());

            // for debugging only: startingPartIndex = activeEngineParts.Count;
            int offset = 0;
            if (startingPartIndex < activeEngineParts.Count)
            {
                startingPart = activeEngineParts[startingPartIndex];
                failureType = FailureType.engine;

                // Get the engine module for the part.
                startingPartEngineModule = startingPart.Modules.OfType<ModuleEngines>().Single();
            }
            offset += activeEngineParts.Count;
            if (startingPartIndex >= offset && startingPartIndex < offset + radialDecouplers.Count)
            {
                startingPart = radialDecouplers[startingPartIndex - offset];

                failureType = FailureType.radialDecoupler;
                decouplerForceCnt = 0;
            }
            offset += radialDecouplers.Count;
            if (startingPartIndex >= offset && startingPartIndex < offset + controlSurfaces.Count)
            {
                startingPart = controlSurfaces[startingPartIndex - offset];

                failureType = FailureType.controlSurface;
                decouplerForceCnt = 0;
            }
            offset += controlSurfaces.Count;
            if (startingPartIndex >= offset && startingPartIndex < offset + strutsAndFuelLines.Count)
            {
                startingPart = strutsAndFuelLines[startingPartIndex - offset];

                failureType = FailureType.strutOrFuelLine;
                decouplerForceCnt = 0;
            }




#if false

            if (startingPartIndex >= activeEngineParts.Count)
            {
                startingPart = radialDecouplers[startingPartIndex - activeEngineParts.Count];
                               
                failureType = FailureType.radialDecoupler;
                decouplerForceCnt = 0;
            }
            else
            {
                startingPart = activeEngineParts[startingPartIndex];
                failureType = FailureType.engine;

                // Get the engine module for the part.
                startingPartEngineModule = startingPart.Modules.OfType<ModuleEngines>().Single();
            }
#endif
            // Setup tick information for the part explosion loop.
            ticksBetweenPartFailures = (int)(KLFUtils.GameTicksPerSecond * KLFSettings.Instance.DelayBetweenPartFailures);
            ticksSinceFailureStart = 0;
            thrustOverload = 0;
            underThrustStart = 100f;
            underThrustEnd = 100f;
            scienceAfterFailure = CalcScienceReward(activeVesselCost, activeVessel, startingPart);
            preFailureWarningTime = KLFSettings.Instance.PreFailureWarningTime;
            if (KLFSettings.Instance.TimeRandomness > 0)
            {
                float f = UnityEngine.Random.Range(0, KLFSettings.Instance.TimeRandomness) - KLFSettings.Instance.TimeRandomness / 2f;
                preFailureWarningTime += f;
            }
        }

        double lastWarningtime = 0;
        double lastTemp = 0;

        /// <summary>
        /// Causes the starting part's failure.
        /// </summary>
        private bool CauseStartingPartFailure()
        {

            if (activeVessel.Parts.Where(o => o == startingPart).Count() == 0)
            {
                ScreenMessages.PostScreenMessage("Failing " + startingPart.partInfo.title + " no longer attached to vessel", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                // Log flight data.
                KLFUtils.LogFlightData(activeVessel, "Failing " + startingPart.partInfo.title + " no longer attached to vessel.");

                // Nullify the starting part.
                startingPart = null;
                doomedParts = null;
                return false;
            }

            if (preFailureTime + preFailureWarningTime > Planetarium.GetUniversalTime())
            {
                // This makes sure that one message a second is put to the screen
                if (lastWarningtime < Planetarium.GetUniversalTime())
                {
                    ScreenMessages.PostScreenMessage("Imminent Failure on " + startingPart.partInfo.title, 1.0f, ScreenMessageStyle.UPPER_CENTER);
                    HighlightFailingPart(startingPart);
                    lastWarningtime = Planetarium.GetUniversalTime() + 1;
                }
                return true;
            }
            // If a valid game tick.
            if (ticksSinceFailureStart % ticksBetweenPartFailures == 0 && (startingPartEngineModule != null && startingPartEngineModule.finalThrust > 0))
            {
                // Need to start overloading the thrust and increase the part's temperature.

                // Calculate actual throttle, the lower of either the throttle setting or the current thrust / max thrust
                float throttle = startingPartEngineModule.finalThrust / startingPartEngineModule.maxThrust;

                Log.Info("throttle: " + throttle.ToString());

                if (overThrust)
                {
                    thrustOverload += (int)(startingPartEngineModule.maxThrust / 30.0 * throttle);

                    startingPartEngineModule.part.Rigidbody.AddRelativeForce(Vector3.up * thrustOverload);
                    //                startingPart.temperature += startingPart.maxTemp / 20.0;
                    startingPart.temperature += startingPart.maxTemp / 20.0 * throttle;
                }
                else
                {
                    // Underthrust will change every 1/2 second
                    if (underthrustTime < Planetarium.GetUniversalTime())
                    {
                        underThrustStart = underThrustEnd;
                        underThrustEnd = (float)((0.9 - KLFUtils.RNG.NextDouble() / 2) * 100);



                        //if (startingPartEngineModule.thrustPercentage < 0)
                        //    startingPartEngineModule.thrustPercentage = KLFUtils.RNG.NextDouble() / 4;

                        //          startingPartEngineModule.part.Rigidbody.AddRelativeForce(Vector3.up * thrustOverload);
                        underthrustTime = Planetarium.GetUniversalTime() + 0.5;
                        // startingPart.temperature += startingPart.maxTemp / 50.0 * throttle;
                    }
                    startingPartEngineModule.thrustPercentage = Mathf.Lerp(underThrustStart, underThrustEnd, 1 - (float)(Planetarium.GetUniversalTime() - underthrustTime) / 0.5f);
                }

                //                startingPart.temperature += startingPart.maxTemp / 20.0 * throttle;
            }
            if (ticksSinceFailureStart % ticksBetweenPartFailures == 0 && failureType != FailureType.engine)
            {
                ++decouplerForceCnt;

                Log.Info("decouplerForceCnt: " + decouplerForceCnt.ToString());
                if (decouplerForceCnt > 20)
                    startingPart.decouple(startingPart.breakingForce + 1);
            }

            // When the part explodes to overheating.
            if (startingPart.temperature >= startingPart.maxTemp || decouplerForceCnt > 20)
            {
                // Log flight data.
                if (failureType != FailureType.engine)
                    KLFUtils.LogFlightData(activeVessel, "Random structural failure of " + startingPart.partInfo.title + ".");
                else
                {
                    if (overThrust)
                        KLFUtils.LogFlightData(activeVessel, "Random failure of " + startingPart.partInfo.title + ".");
                    else
                        KLFUtils.LogFlightData(activeVessel, "Underthrust of " + startingPart.partInfo.title + ".");
                }

                // Gather the doomed parts at the time of failure.
                PrepareDoomedParts();
                failureTime = Double.MaxValue;

                // Nullify the starting part.
                startingPart = null;
            }
            else
            {
                if (lastWarningtime < Planetarium.GetUniversalTime() && (startingPart.temperature >= lastTemp || decouplerForceCnt > 20))
                {
                    HighlightFailingPart(startingPart);

                    if (failureType != FailureType.engine)
                        ScreenMessages.PostScreenMessage(startingPart.partInfo.title + " structural failure imminent ", 1.0f, ScreenMessageStyle.UPPER_CENTER);
                    else
                    {
                        if (overThrust)
                            ScreenMessages.PostScreenMessage(startingPart.partInfo.title + " temperature exceeding limits: " + Math.Round(startingPart.temperature, 1).ToString(), 1.0f, ScreenMessageStyle.UPPER_CENTER);
                        else
                            ScreenMessages.PostScreenMessage(startingPart.partInfo.title + " losing thrust", 1.0f, ScreenMessageStyle.UPPER_CENTER);

                    }
                    lastWarningtime = Planetarium.GetUniversalTime() + 1;
                    lastTemp = startingPart.temperature;

                }
                // If the auto abort sequence is on and this is the starting part, trigger the Abort action group.
                if (KLFSettings.Instance.AutoAbort)
                {
                    if (failureTime == 0)
                    {
                        failureTime = Planetarium.GetUniversalTime();
                    }
                    else
                    {
                        Debug.Log("failureTime: " + failureTime.ToString() + "   AutoAbortDelay: " + KLFSettings.Instance.AutoAbortDelay.ToString() + "   Planetarium.GetUniversalTime: " + Planetarium.GetUniversalTime().ToString());
                        if (failureTime + KLFSettings.Instance.AutoAbortDelay < Planetarium.GetUniversalTime())
                        {
                            Debug.Log("Triggering autoabort");
                            activeVessel.ActionGroups.SetGroup(KSPActionGroup.Abort, true);
                            AddAndReportScience();
                            // Following just to be sure the abort isn't triggered more than once
                            failureTime = Double.MaxValue;
                        }
                    }
                }

            }

            return true;
        }

        /// <summary>
        /// Gets the next doomed part to explode based on game ticks since start.
        /// </summary>
        private Part GetNextDoomedPart()
        {
            if (ticksSinceFailureStart % ticksBetweenPartFailures == 0)
            {
                return doomedParts[ticksSinceFailureStart / ticksBetweenPartFailures];
            }

            // Return null if invalid tick.
            return null;
        }

        /// <summary>
        /// Explodes the next doomed part.
        /// </summary>
        private void ExplodeNextDoomedPart()
        {
            // The next part to explode.
            Part nextDoomedPart = GetNextDoomedPart();

            // Tick was invalid here.
            if (nextDoomedPart == null) return;

            // Log flight data.
            KLFUtils.LogFlightData(activeVessel, nextDoomedPart.partInfo.title + " disassembly due to an earlier failure.");

            // The fun stuff...
            nextDoomedPart.explode();

        }

        /// <summary>
        /// Determines which parts are to explode.
        /// </summary>
        private void PrepareDoomedParts()
        {
            // The parts that will explode.
            doomedParts = new List<Part>();

            // Add the starting part to the doomed parts list. Simpler code in propagate.
            doomedParts.Add(startingPart);

            // Propagate to surrounding parts.

            PropagateFailure(startingPart, KLFSettings.Instance.FailurePropagateProbability);

            // Remove the starting part. This will be handled differently.
            doomedParts.RemoveAt(0);

            // Setup tick information.
            ticksSinceFailureStart = -ticksBetweenPartFailures;
        }

        /// <summary>
        /// Propagates failure through surrounding parts.
        /// </summary>
        /// <param name="part">The parent part.</param>
        /// <param name="failureChance">Chance of failure for the next part.</param>
        private void PropagateFailure(Part part, double failureChance)
        {
            // The list of potential parts that may be doomed.
            List<Part> potentialParts = new List<Part>();

            // Calculate the next propagation's failure chance.
            double nextFailureChance = (KLFSettings.Instance.PropagationChanceDecreases) ? failureChance * KLFSettings.Instance.FailurePropagateProbability : failureChance;

            // Parent
            if (part.parent && !doomedParts.Contains(part.parent))
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
                double thisFailureChance;
                if (failureType != FailureType.engine)
                    thisFailureChance = failureChance;
                else
                    thisFailureChance = (KLFUtils.PartIsExplosiveFuelTank(potentialPart)) ? 1 : failureChance;
                if (!doomedParts.Contains(potentialPart) && KLFUtils.RNG.NextDouble() < thisFailureChance)
                {
                    doomedParts.Add(potentialPart);
                    PropagateFailure(potentialPart, nextFailureChance);
                }
            }
        }

        /// <summary>
        /// Determines if a failure will occur or not.
        /// </summary>
        /// <returns>True if yes, false if no.</returns>
        public static bool Occurs()
        {
            double r = KLFUtils.RNG.NextDouble();
 
            if (KLFUtils.ExperimentalPartsPresentAndActive())
            {
                Log.Info("Experimental Parts present");
                experimentalPartFailure = r < KLFSettings.Instance.ExpPartFailureProbability;
            }
            partFailure = KLFUtils.RNG.NextDouble() < KLFSettings.Instance.InitialFailureProbability;

            Log.Info("experimentalPartFailure: " + experimentalPartFailure.ToString() + "   partFailure: " + partFailure.ToString());

            return experimentalPartFailure || partFailure;
            
        }
    }
}
