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
#if DEBUG
        private const int ChanceOfRUD = 1;
        private const float GuaranteeDuringFlight = 0.99F;
#else
        private const int ChanceOfRUD = 51;
        private const float GuaranteeDuringFlight = 0.9F;
#endif
        private const float FailurePropagateChance = 0.95F;

        private int calculatedOddsPerTick;
        private bool isRunning = false;
        private bool isPaused = false;
        private static System.Random rand = new System.Random();

        public void Awake()
        {

        }

        public void Update()
        {

        }

        public void OnDestroy()
        {

        }

        private void CalculateOddsPerTick()
        {

        }

        private void FailureStartHandler(EventReport eventReport)
        {

        }

        private void FailureEndHandler(Vessel vessel)
        {

        }

        private void FailurePauseHandler()
        {

        }

        private void FaulureUnpauseHandler()
        {

        }

        private void DestroyFailureRun()
        {

        }
    }
}
