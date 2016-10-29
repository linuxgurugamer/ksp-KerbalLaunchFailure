using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KerbalLaunchFailure
{
    internal class KLFSettings
    {
        /// <summary>
        /// Config node name for settings.
        /// </summary>
        //private const string ConfigNodeName = "KERBALLAUNCHFAILURE_SETTINGS";

        /// <summary>
        /// Name of settings file.
        /// </summary>
       // private const string LocalSettingsFile = "KLF_Settings.cfg";

        // Default values for settings.
        private float initialFailureProbability = 0.02F;
        private float expPartFailureProbability = 0.1f;
        private float maxFailureAltitudePercentage = 0.65F;
        private bool propagationChanceDecreases = false;
        private float failurePropagateProbability = 0.7F;
        private float delayBetweenPartFailures = 0.2F;
        private bool autoAbort = false;
        private float autoAbortDelay = 5F;
        private float preFailureWarningTime = 5F;
        private string alarmSoundFile = "alarm2";

        private float timeRandomness = 0.0f;
        private bool scienceAtFailure = false;
        private float scienceAdjustment = 10.0f;

        /// <summary>
        /// ScienceAdjustment adjusts amount of science awarded when a failure happens.  Larger = less science (uses logorithms)
        /// </summary>
        public float ScienceAdjustment
        {
            get { return scienceAdjustment; }
        }

        /// <summary>
        /// TimeRandomness applied to time before failure, after initial notification.  Larger is more random.
        /// </summary>
        public float TimeRandomness
        {
            get { return timeRandomness; }
        }

        /// <summary>
        /// If set true, you will get some (random) science after a failure
        /// </summary>
        public bool ScienceAtFailure
        {
            get { return scienceAtFailure; }
        }

        /// <summary>
        /// Probability of initial failure.
        /// </summary>
        public float InitialFailureProbability
        {
            get { return initialFailureProbability; }
        }
        /// <summary>
        /// Probability of experimental part failure.
        /// </summary>
        public float ExpPartFailureProbability
        {
            get { return expPartFailureProbability; }
        }
        

        /// <summary>
        /// The maximum altitude as a percentage in which the failure can begin.
        /// </summary>
        public float MaxFailureAltitudePercentage
        {
            get { return maxFailureAltitudePercentage; }
        }

        /// <summary>
        /// If set true, the failure rate will decrease with each propagation.
        /// </summary>
        public bool PropagationChanceDecreases
        {
            get { return propagationChanceDecreases; }
        }

        /// <summary>
        /// Chance that the part explosion will propogate to attached part. Fuel tanks are handled differently.
        /// </summary>
        public float FailurePropagateProbability
        {
            get { return failurePropagateProbability; }
        }

        /// <summary>
        /// The delay between part failures.
        /// </summary>
        public float DelayBetweenPartFailures
        {
            get { return delayBetweenPartFailures; }
        }

        /// <summary>
        /// If true, after the first part failure, the abort action group will automatically fire.
        /// </summary>
        public bool AutoAbort
        {
            get { return autoAbort; }
        }

        /// <summary>
        /// How long to wait before triggering the autoAbort.
        /// </summary>
        public float AutoAbortDelay
        {
            get { return autoAbortDelay; }
        }

        /// <summary>
        /// How much time to warn before a failure
        /// </summary>
        public float PreFailureWarningTime
        {
            get { return preFailureWarningTime; }
        }

        /// <summary>
        /// Which alarm sound to use
        /// </summary>
        public string AlarmSoundFile
        {
            get { return alarmSoundFile; }
        }




        /// <summary>
        /// Singleton instance.
        /// </summary>
        private static KLFSettings instance;

#if false
        /// <summary>
        /// The local path to the settings file.
        /// </summary>
        private static string LocalSettingsPath
        {
            get { return Path.Combine(KerbalLaunchFailureController.LocalPluginDataPath, LocalSettingsFile); }
        }

        /// <summary>
        /// The absolute path to the settings file.
        /// </summary>
        private static string AbsoluteSettingsPath
        {
            get { return Path.Combine(KSPUtil.ApplicationRootPath, LocalSettingsPath); }
        }
#endif
        /// <summary>
        /// Settings instance.
        /// </summary>
        public static KLFSettings Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new KLFSettings();
                }
                return instance;
            }
        }

        /// <summary>
        /// Constructor loads settings from file and creates one if it does not exist or invalid.
        /// </summary>
        private KLFSettings()
        {
#if false
            if (!File.Exists(AbsoluteSettingsPath))
            {
                SaveSettings();
            }
            else
            {
#endif
                if (!LoadSettings())
                {
      //              SaveSettings();
                }
          //  }
        }

        /// <summary>
        /// Loads the settings from file.
        /// </summary>
        /// <returns></returns>
        public bool LoadSettings()
        {
            
            initialFailureProbability = HighLogic.CurrentGame.Parameters.CustomParams<KLFCustomParams>().initialFailureProbability;
            expPartFailureProbability = HighLogic.CurrentGame.Parameters.CustomParams<KLFCustomParams>().expPartFailureProbability;
            maxFailureAltitudePercentage = HighLogic.CurrentGame.Parameters.CustomParams<KLFCustomParams>().maxFailureAltitudePercentage;
            propagationChanceDecreases = HighLogic.CurrentGame.Parameters.CustomParams<KLFCustomParams>().propagationChanceDecreases;
            failurePropagateProbability = HighLogic.CurrentGame.Parameters.CustomParams<KLFCustomParams>().failurePropagateProbability;
            delayBetweenPartFailures = HighLogic.CurrentGame.Parameters.CustomParams<KLFCustomParams>().delayBetweenPartFailures;
            autoAbort = HighLogic.CurrentGame.Parameters.CustomParams<KLFCustomParams2>().autoAbort;
            autoAbortDelay = HighLogic.CurrentGame.Parameters.CustomParams<KLFCustomParams2>().autoAbortDelay;
            preFailureWarningTime = HighLogic.CurrentGame.Parameters.CustomParams<KLFCustomParams2>().preFailureWarningTime;
            alarmSoundFile = HighLogic.CurrentGame.Parameters.CustomParams<KLFCustomParams2>().alarmSoundFile;


            timeRandomness = HighLogic.CurrentGame.Parameters.CustomParams<KLFCustomParams3>().timeRandomness;
            scienceAtFailure = HighLogic.CurrentGame.Parameters.CustomParams<KLFCustomParams3>().scienceAtFailure;
            scienceAdjustment = HighLogic.CurrentGame.Parameters.CustomParams<KLFCustomParams3>().scienceAdjustment;
            return true;
        }
    }
}
