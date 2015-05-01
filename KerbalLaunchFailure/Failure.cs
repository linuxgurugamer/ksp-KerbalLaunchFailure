using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KerbalLaunchFailure
{
    public class Failure
    {
#if DEBUG
        /// <summary>
        /// Chance of launch failure: always.
        /// </summary>
        private const int ChanceOfRUD = 1;

        /// <summary>
        /// The maximum altitude as a percentage in which the failure can begin.
        /// </summary>
        private const float MaxFailureAltitudePercentage = 0.25F;
#else
        /// <summary>
        /// Chance of launch failure: 1 in 'ChanceOfRUD'
        /// </summary>
        private const int ChanceOfRUD = 50;

        /// <summary>
        /// The maximum altitude as a percentage in which the failure can begin.
        /// </summary>
        private const float MaxFailureAltitudePercentage = 0.65F;
#endif

        /// <summary>
        /// If set true, the failure rate will decrease with each propagation.
        /// </summary>
        private const bool PropagationChanceDecreases = true;

        /// <summary>
        /// Chance that the part explosion will propogate to attached part.
        /// </summary>
        private const float FailurePropagateChance = 0.8F;

        /// <summary>
        /// The delay between part failures.
        /// </summary>
        private const float DelayBetweenPartFailures = 0.2F;

        /// <summary>
        /// If true, after the first part failure, the abort action group will automatically fire.
        /// </summary>
        private const bool DoAutoAbortActionGroup = false;

        /// <summary>
        /// The active flight vessel.
        /// </summary>
        private readonly Vessel activeVessel;

        /// <summary>
        /// The active flight vessel's current parent celestial body.
        /// </summary>
        private readonly CelestialBody currentCelestialBody;

        /// <summary>
        /// The minimum altitude in which the failure will occur.
        /// </summary>
        private readonly int altitudeFailureOccurs;

        /// <summary>
        /// The list of parts set to explode.
        /// </summary>
        private List<Part> doomedParts;

        /// <summary>
        /// The number of ticks between part explosions.
        /// </summary>
        private int ticksBetweenPartExplosions;

        /// <summary>
        /// The number of ticks since the failure started.
        /// </summary>
        private int ticksSinceFailureStart;

        /// <summary>
        /// Constructor for Failure object.
        /// </summary>
        public Failure()
        {
            // Gather info about current active vessel.
            activeVessel = FlightGlobals.ActiveVessel;
            currentCelestialBody = activeVessel.mainBody;

            // This plugin is only targeted at atmospheric failures.
            if (currentCelestialBody.atmosphere)
            {
                altitudeFailureOccurs = KLFUtils.RNG.Next(0, (int)(currentCelestialBody.atmosphereDepth * MaxFailureAltitudePercentage));
#if DEBUG
                KLFUtils.LogDebugMessage("Failure will occur at an altitude of " + altitudeFailureOccurs);
#endif
            }
            else
            {
                altitudeFailureOccurs = 0;
            }
        }

        /// <summary>
        /// The main method that runs the failure script.
        /// </summary>
        /// <returns></returns>
        public bool Run()
        {
            // Kill the failure script if there is no atmosphere.
            if (!currentCelestialBody.atmosphere) return false;

            // Wait until the vessel is past the altitude threshold.
            if (activeVessel.altitude < altitudeFailureOccurs) return true;

            // Prepare the doomed parts if not done so.
            if (doomedParts == null)
            {
                PrepareDoomedParts();
            }

            // If parts have been found.
            if (doomedParts != null)
            {
                try
                {
                    // Will attempt to explode the next part.
                    ExplodeNextDoomedPart();
                }
                catch (ArgumentOutOfRangeException)
                {
                    // This occurs when there are no more parts to explode.
                    return false;
                }
                // Increase the ticks.
                ticksSinceFailureStart++;
            }

            // Tell caller to continue run.
            return true;
        }

        /// <summary>
        /// Gets the next doomed part to explode based on game ticks since start.
        /// </summary>
        private Part GetNextDoomedPart()
        {
            if (ticksSinceFailureStart % ticksBetweenPartExplosions == 0)
            {
                return doomedParts[ticksSinceFailureStart / ticksBetweenPartExplosions];
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

            // Log different data if it is the starting part.
            if (nextDoomedPart == doomedParts[0])
            {
                KLFUtils.LogFlightData(activeVessel, "Random failure and disassembly of " + nextDoomedPart.partInfo.title + ".");
            }
            else
            {
                KLFUtils.LogFlightData(activeVessel, nextDoomedPart.partInfo.title + " disassembly due to failure of " + doomedParts[0].partInfo.title + ".");
            }

            // The fun stuff...
            nextDoomedPart.explode();

            // If the auto abort sequence is on and this is the starting part, trigger the Abort action group.
            if (DoAutoAbortActionGroup && nextDoomedPart == doomedParts[0])
            {
                activeVessel.ActionGroups.SetGroup(KSPActionGroup.Abort, true);
            }

        }

        /// <summary>
        /// Determines which parts are to explode.
        /// </summary>
        private void PrepareDoomedParts()
        {
            // Find engines
            List<Part> activeEngineParts = activeVessel.GetActiveParts().Where(o => KLFUtils.PartIsActiveEngine(o)).ToList();

            // If there are no active engines, skip this attempt.
            if (activeEngineParts.Count == 0) return;

            // The parts that will explode.
            doomedParts = new List<Part>();

            // Determine the starting part.
            int startingPartIndex = KLFUtils.RNG.Next(0, activeEngineParts.Count);
            Part startingPart = activeEngineParts[startingPartIndex];

            // Add the starting part to the doomed parts list.
            doomedParts.Add(startingPart);

            // Propagate to sourrounding parts.
            PropagateFailure(startingPart, FailurePropagateChance);

            // Setup tick information for the part explosion loop.
            ticksBetweenPartExplosions = (int)(KLFUtils.GameTicksPerSecond * DelayBetweenPartFailures);
            ticksSinceFailureStart = 0;
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
            double nextFailureChance = (PropagationChanceDecreases) ? failureChance * FailurePropagateChance : failureChance;

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
                double thisFailureChance = (KLFUtils.PartIsExplosiveFuelTank(potentialPart)) ? 1 : failureChance;
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
            return KLFUtils.RNG.Next(0, ChanceOfRUD) == 0;
        }
    }
}
