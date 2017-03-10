using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

// http://forum.kerbalspaceprogram.com/index.php?/topic/147576-modders-notes-for-ksp-12/#comment-2754813
// search for "Mod integration into Stock Settings

namespace KerbalLaunchFailure
{

    public class KLFCustomParams : GameParameters.CustomParameterNode
    {
        public static KLFCustomParams instance;
        public KLFCustomParams()
        {
            instance = this;
        }

        public override string Title { get { return "";  } }
        public override GameParameters.GameMode GameMode { get { return GameParameters.GameMode.ANY; } }
        public override string Section { get { return "KerbalLaunchFailure"; } }
        public override int SectionOrder { get { return 1; } }
        public override bool HasPresets { get { return true; } }

        [GameParameters.CustomParameterUI("Mod Enabled?")]
        public bool enabled = true;

        // The following crazy code is due to a bug introduced in 1.2.2
        // See this link for details:  http://forum.kerbalspaceprogram.com/index.php?/topic/7542-the-official-unoffical-quothelp-a-fellow-plugin-developerquot-thread/&page=100#comment-2887044

        //        [GameParameters.CustomFloatParameterUI("Initial failure probability", minValue = 0.0f, maxValue = 1.0f, stepCount = 101, asPercentage = true)]
        //        public float initialFailureProbability = 0.02F;

        public float initialFailureProb = 0.02F;
        [GameParameters.CustomFloatParameterUI("Initial failure probability (%)", displayFormat = "N0", minValue = 0, maxValue = 100, stepCount = 1, asPercentage = false)]
        public float initialFailureProbability
        {
            get { return initialFailureProb * 100; }
            set { initialFailureProb = value / 100.0f; }
        }


        //        [GameParameters.CustomFloatParameterUI("Exp. Part failure probability", minValue = 0.0f, maxValue = 1.0f, stepCount = 101, asPercentage = true)]
        //        public float expPartFailureProbability = 0.15F;

        public float expPartFailureProb = 0.15F;
        [GameParameters.CustomFloatParameterUI("Exp. Part failure probability (%)", displayFormat = "N0", minValue = 0, maxValue = 100, stepCount = 1, asPercentage = false)]
        public float expPartFailureProbability
        {
            get { return expPartFailureProb * 100; }
            set { expPartFailureProb = value / 100.0f; }
        }


        //        [GameParameters.CustomFloatParameterUI("Max altitude of failure", minValue = 0.0f, maxValue = 1.0f, stepCount = 101, asPercentage = true,
        //            toolTip = "This is a percentage of the atmosphere depth (ie:  Kerbol's atmosphere is 70km deep)")]
        //          public float maxFailureAltitudePercentage = 0.65F;

        public float maxFailureAltitudePerc = 0.65F;
        [GameParameters.CustomFloatParameterUI("Max altitude of failure (%)", displayFormat = "N0", minValue = 0, maxValue = 100, stepCount = 1, asPercentage = false,
            toolTip = "This is a percentage of the atmosphere depth (ie:  Kerbol's atmosphere is 70km deep)")]
        public float maxFailureAltitudePercentage
        {
            get { return maxFailureAltitudePerc * 100; }
            set { maxFailureAltitudePerc = value / 100.0f; }
        }

        [GameParameters.CustomParameterUI("Highlight failing part")]
        public bool highlightFailingPart = true;

        [GameParameters.CustomParameterUI("Failure rate decreases with each propagation", toolTip = "If enabled, then the chance of successive failures decreases after each failure")]
        public bool propagationChanceDecreases = false;


        //        [GameParameters.CustomFloatParameterUI("Chance of failure propogation", minValue = 0.0f, maxValue = 1.0f, stepCount = 101, asPercentage = true,
        //            toolTip = "This is the chance of the failure propogating to another part.")]
        //        public float failurePropagateProbability = 0.6F;

        public float failurePropagateProb = 0.6F;
        [GameParameters.CustomFloatParameterUI("Chance of failure propogation (%)", minValue = 0, maxValue = 100, stepCount = 1, asPercentage = false,
            toolTip = "This is the chance of the failure propogating to another part.")]
        public float failurePropagateProbability
        {
            get { return failurePropagateProb * 100; }
            set { failurePropagateProb = value / 100.0f; }
        }



        //        [GameParameters.CustomFloatParameterUI("Delay between part failures, in seconds", minValue = 0.0f, maxValue = 0.5f, stepCount = 101, displayFormat = "F1",
        //            toolTip = "This is the time between each additional failure of a part.  So, if .2, then every .2 seconds the part will fail some more")]
        //        public float delayBetweenPartFailures = 0.8F;

        public float delayBetweenPartFail = 0.8F;

        [GameParameters.CustomFloatParameterUI("Delay between part failures, in seconds", minValue = 0.2f, maxValue = 0.8f, stepCount = 101, displayFormat = "F1",
            toolTip = "This is the time between each additional failure of a part.  So, if .2, then every .2 seconds the part will fail some more")]
        public float delayBetweenPartFailures
        {
            get { return delayBetweenPartFail * 100; }
            set { delayBetweenPartFail = value / 100.0f; }
        }

        [GameParameters.CustomFloatParameterUI("Min time before failure (seconds)", minValue = 5.0f, maxValue = 100.0f, stepCount = 100, displayFormat = "F1", logBase = 2)]
        public float minTimeBeforeFailure = 5.0F;

        [GameParameters.CustomFloatParameterUI("Max time before failure (seconds)", minValue = 6.0f, maxValue = 3600.0f, stepCount = 100, displayFormat = "F1", logBase = 2)]
        public float maxTimeBeforeFailure = 300.0F;


        public override void SetDifficultyPreset(GameParameters.Preset preset)
        {
            Log.Info("Setting difficulty preset");
            switch (preset)
            {
                case GameParameters.Preset.Easy:
                    initialFailureProbability = 0.02F;
                    expPartFailureProbability = 0.8f;
                    maxFailureAltitudePercentage = 0.65F;
                    propagationChanceDecreases = true;
                    failurePropagateProbability = 0.5F;
                    delayBetweenPartFailures = 0.5F;
                    highlightFailingPart = true;
                    minTimeBeforeFailure = 5.0F;
                    maxTimeBeforeFailure = 300.0F;
                    break;

                case GameParameters.Preset.Normal:
                    initialFailureProbability = 0.02F;
                    expPartFailureProbability = 0.1f;
                    maxFailureAltitudePercentage = 0.65F;
                    propagationChanceDecreases = false;
                    failurePropagateProbability = 0.6F;
                    delayBetweenPartFailures = 0.2F;
                    highlightFailingPart = true;
                    minTimeBeforeFailure = 5.0F;
                    maxTimeBeforeFailure = 300.0F;

                    break;

                case GameParameters.Preset.Moderate:
                    initialFailureProbability = 0.02F;
                    expPartFailureProbability = 0.15f;
                    maxFailureAltitudePercentage = 0.65F;
                    propagationChanceDecreases = false;
                    failurePropagateProbability = 0.7F;
                    delayBetweenPartFailures = 0.15F;
                    highlightFailingPart = true;
                    minTimeBeforeFailure = 5.0F;
                    maxTimeBeforeFailure = 300.0F;

                    break;

                case GameParameters.Preset.Hard:
                    initialFailureProbability = 0.02F;
                    expPartFailureProbability = 0.2f;
                    maxFailureAltitudePercentage = 0.65F;
                    propagationChanceDecreases = false;
                    failurePropagateProbability = 0.8F;
                    delayBetweenPartFailures = 0.1F;
                    highlightFailingPart = false;
                    minTimeBeforeFailure = 5.0F;
                    maxTimeBeforeFailure = 300.0F;

                    break;
            }
        }

        public override bool Enabled(MemberInfo member, GameParameters parameters)
        {

            if (member.Name == "enabled") //This Field must always be enabled.
                return true;
            if (member.Name == "expPartFailureProbability")
            {
                if (initialFailureProbability > expPartFailureProbability)
                    expPartFailureProbability = initialFailureProbability;
            }
#if false
            if (enabled == false) //Otherwise it depends on the value of MyBool if it's false return false
            {
                return false;
            }
#endif
            return true; //otherwise return true

        }

        public override bool Interactible(MemberInfo member, GameParameters parameters)
        {
#if false
            if (member.Name == "MyBool") //This Field must always be Interactible.
                return true;
            if (MyBool == false)  //Otherwise it depends on the value of MyBool if it's false return false
                return false;
#endif
            
            return true; //otherwise return true
        }

        public override IList ValidValues(MemberInfo member)
        {
            if (member.Name == "alarmSoundFile")
            {
                List<string> myList = new List<string>();

                string[] filePaths = Directory.GetFiles(KSPUtil.ApplicationRootPath + "GameData/" + KerbalLaunchFailureController.LocalAlarmSoundDir, "*.wav",
                                         SearchOption.TopDirectoryOnly);
                for (int i = 0; i < filePaths.Length; i++)
                {
                    // Strip directory path and suffix from name
                    string s = filePaths[i].Substring(filePaths[i].LastIndexOf('/') + 1);
                    myList.Add(s.Substring(0, s.IndexOf('.')));
                }

                IList myIlist = myList;
                return myIlist;
            }
            else
            {
                return null;
            }
        }
    }



    public class KLFCustomParams2 : GameParameters.CustomParameterNode
    {
        public override string Title { get { return ""; } }
        public override GameParameters.GameMode GameMode { get { return GameParameters.GameMode.ANY; } }
        public override string Section { get { return "KerbalLaunchFailure"; } }
        public override int SectionOrder { get { return 2; } }
        public override bool HasPresets { get { return true; } }


        [GameParameters.CustomParameterUI("AutoAbort enabled")]
        public bool autoAbort = false;

        [GameParameters.CustomFloatParameterUI("Delay before auto abort", minValue = 0.0f, maxValue = 10.0f, stepCount = 101, displayFormat = "F1")]
        public float autoAbortDelay = 5F;

        [GameParameters.CustomFloatParameterUI("Prefailure warning time", minValue = 0.0f, maxValue = 10.0f, stepCount = 101, displayFormat = "F1")]
        public float preFailureWarningTime = 5F;

        [GameParameters.CustomParameterUI("Alarm sound to use")]
        public string alarmSoundFile = "WhoopWhoop";

        [GameParameters.CustomParameterUI("Allow engine failures")]
        public bool allowEngineFailures = true;

        [GameParameters.CustomParameterUI("Allow engine underthrust")]
        public bool allowEngineUnderthrust = true;

        //        [GameParameters.CustomFloatParameterUI("Chance of engine failure being underthrust", minValue = 0.01f, maxValue = 1.0f, stepCount = 100, asPercentage = true)]
        //        public float engineUnderthrustProbability = 0.3F;
   
        public float engineUnderthrustProb = 0.3F;
        [GameParameters.CustomFloatParameterUI("Chance of engine failure being underthrust (%)", minValue = 0, maxValue = 100, stepCount = 1, asPercentage = false)]
        public float engineUnderthrustProbability
        {
            get { return engineUnderthrustProb * 100; }
            set { engineUnderthrustProb = value / 100.0f; }
        }



        [GameParameters.CustomParameterUI("Allow radial decoupler failures")]
        public bool allowRadialDecouplerFailures = true;

        [GameParameters.CustomParameterUI("Allow control surface failures")]
        public bool allowControlSurfaceFailures = true;

        [GameParameters.CustomParameterUI("Allow strut & fuel line failures", toolTip ="This does not apply to any autostruts")]
        public bool allowStrutFuelFailures = true;



        public override void SetDifficultyPreset(GameParameters.Preset preset)
        {
            Log.Info("Setting difficulty preset");
            switch (preset)
            {
                case GameParameters.Preset.Easy:

                    autoAbort = false;
                    autoAbortDelay = 5F;
                    preFailureWarningTime = 5F;
                    alarmSoundFile = "WhoopWhoop";
                    allowEngineFailures = true;
                    allowEngineUnderthrust = true;
                    engineUnderthrustProbability = 0.3F;
                    allowRadialDecouplerFailures = true;
                    allowControlSurfaceFailures = true;
                    allowStrutFuelFailures = true;
                    break;

                case GameParameters.Preset.Normal:

                    autoAbort = false;
                    autoAbortDelay = 5F;
                    preFailureWarningTime = 5F;
                    alarmSoundFile = "WhoopWhoop";
                    allowEngineFailures = true;
                    allowEngineUnderthrust = true;
                    engineUnderthrustProbability = 0.3F;
                    allowRadialDecouplerFailures = true;
                    allowControlSurfaceFailures = true;
                    allowStrutFuelFailures = true;
                    break;

                case GameParameters.Preset.Moderate:

                    autoAbort = false;
                    autoAbortDelay = 5F;
                    preFailureWarningTime = 5F;
                    alarmSoundFile = "WhoopWhoop";
                    allowEngineFailures = true;
                    allowEngineUnderthrust = true;
                    engineUnderthrustProbability = 0.3F;
                    allowRadialDecouplerFailures = true;
                    allowControlSurfaceFailures = true;
                    allowStrutFuelFailures = true;
                    break;

                case GameParameters.Preset.Hard:

                    autoAbort = false;
                    autoAbortDelay = 5F;
                    preFailureWarningTime = 5F;
                    alarmSoundFile = "WhoopWhoop";
                    allowEngineFailures = true;
                    allowEngineUnderthrust = true;
                    engineUnderthrustProbability = 0.3F;
                    allowRadialDecouplerFailures = true;
                    allowControlSurfaceFailures = true;
                    allowStrutFuelFailures = true;
                    break;
            }
        }

        public override bool Enabled(MemberInfo member, GameParameters parameters)
        {

            if (member.Name == "enabled") //This Field must always be enabled.
                return true;
            if (member.Name == "engineUnderthrustProbability")
                return allowEngineUnderthrust & allowEngineFailures;
            if (member.Name == "allowEngineUnderthrust")
                return allowEngineFailures;

            //if (KLFCustomParams.instance.enabled == false) //Otherwise it depends on the value of MyBool if it's false return false
            //{
            //    return false;
            //}

                return true; //otherwise return true

        }

        public override bool Interactible(MemberInfo member, GameParameters parameters)
        {

            return true; //otherwise return true
        }

        public override IList ValidValues(MemberInfo member)
        {
            if (member.Name == "alarmSoundFile")
            {
                List<string> myList = new List<string>();

                string[] filePaths = Directory.GetFiles(KSPUtil.ApplicationRootPath + "GameData/" + KerbalLaunchFailureController.LocalAlarmSoundDir, "*.wav",
                                         SearchOption.TopDirectoryOnly);
                for (int i = 0; i < filePaths.Length; i++)
                {
                    // Strip directory path and suffix from name
                    string s = filePaths[i].Substring(filePaths[i].LastIndexOf('/') + 1);
                    myList.Add(s.Substring(0, s.IndexOf('.')));
                }

                IList myIlist = myList;
                return myIlist;
            }
            else
            {
                return null;
            }
        }
    }

    public class KLFCustomParams3 : GameParameters.CustomParameterNode
    {
        public override string Title { get { return ""; } }
        public override GameParameters.GameMode GameMode { get { return GameParameters.GameMode.ANY; } }
        public override string Section { get { return "KerbalLaunchFailure"; } }
        public override int SectionOrder { get { return 3; } }
        public override bool HasPresets { get { return true; } }


        [GameParameters.CustomParameterUI("Failure generates some science", toolTip = "If a failure occurs, you learn something from it")]
        public bool scienceAtFailure = true;

        [GameParameters.CustomFloatParameterUI("Science Adjustment", toolTip = "The larger the number, the more science is generated from a failure", minValue = 10.0f, maxValue = 100.0f, stepCount = 91, displayFormat = "F1")]
        public float scienceAdjustment = 100.0F;

        [GameParameters.CustomFloatParameterUI("Random factor applied to warning time", toolTip = "larger value make warning time more random", minValue = 0.0f, maxValue = 10.0f, stepCount = 101, displayFormat = "F1")]
        public float timeRandomness = 0.1F;


        public override void SetDifficultyPreset(GameParameters.Preset preset)
        {
            Log.Info("Setting difficulty preset");
            switch (preset)
            {
                case GameParameters.Preset.Easy:
                    timeRandomness = 0.1F;
                    scienceAtFailure = true;
                    scienceAdjustment = 100.0f;
                    break;

                case GameParameters.Preset.Normal:
                    timeRandomness = 1.0F;
                    scienceAtFailure = true;
                    scienceAdjustment = 50.0f;
                    break;

                case GameParameters.Preset.Moderate:
                    timeRandomness = 3.0F;
                    scienceAtFailure = true;
                    scienceAdjustment = 20.0f;
                    break;

                case GameParameters.Preset.Hard:
                    timeRandomness = 5.0F;
                    scienceAtFailure = true;
                    scienceAdjustment = 10.0f;
                    break;
            }
        }
        public override bool Enabled(MemberInfo member, GameParameters parameters)
        {
            if (member.Name != "scienceAtFailure")
                return scienceAtFailure;
            return true;
        }
        public override bool Interactible(MemberInfo member, GameParameters parameters)
        {

            return true; //otherwise return true
        }
    }

    }
