using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace KerbalLaunchFailure
{
    internal class KLFSettings
    {
        /// <summary>
        /// Config node name for settings.
        /// </summary>
        private const string ConfigNodeName = "KERBALLAUNCHFAILURE_SETTINGS";

        /// <summary>
        /// Name of settings file.
        /// </summary>
        private const string LocalSettingsFile = "KLF_Settings.cfg";

        // Default values for settings.
        private float initialFailureProbability = 0.02F;
        private float maxFailureAltitudePercentage = 0.65F;
        private bool propagationChanceDecreases = false;
        private float failurePropagateProbability = 0.7F;
        private float delayBetweenPartFailures = 0.2F;
        private bool autoAbort = false;

        /// <summary>
        /// Probability of initial failure.
        /// </summary>
        public float InitialFailureProbability
        {
            get { return initialFailureProbability; }
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
        /// Singleton instance.
        /// </summary>
        private static KLFSettings instance;

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
            if (!File.Exists(AbsoluteSettingsPath))
            {
                SaveSettings();
            }
            else
            {
                if (!LoadSettings())
                {
                    SaveSettings();
                }
            }
        }

        /// <summary>
        /// Loads the settings from file.
        /// </summary>
        /// <returns></returns>
        private bool LoadSettings()
        {
            bool validSettings = true;
            ConfigNode settings = ConfigNode.Load(AbsoluteSettingsPath);
            if (!settings.HasNode(ConfigNodeName)) { return false; }
            ConfigNode node = settings.GetNode(ConfigNodeName);
            validSettings &= LoadSetting(node, "initialFailureProbability", ref initialFailureProbability);
            validSettings &= LoadSetting(node, "maxFailureAltitudePercentage", ref maxFailureAltitudePercentage);
            validSettings &= LoadSetting(node, "propagationChanceDecreases", ref propagationChanceDecreases);
            validSettings &= LoadSetting(node, "failurePropagateProbability", ref failurePropagateProbability);
            validSettings &= LoadSetting(node, "delayBetweenPartFailures", ref delayBetweenPartFailures);
            validSettings &= LoadSetting(node, "autoAbort", ref autoAbort);
            return validSettings;
        }

        /// <summary>
        /// Loads a float-type setting.
        /// </summary>
        /// <param name="node">The config node.</param>
        /// <param name="name">The setting name.</param>
        /// <param name="value">The setting value to be set.</param>
        /// <returns></returns>
        private bool LoadSetting(ConfigNode node, string name, ref float value)
        {
            float result = 0F;
            if (node.HasValue(name) && float.TryParse(node.GetValue(name), out result))
            {
                value = result;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Loads a boolean-type setting.
        /// </summary>
        /// <param name="node">The config node.</param>
        /// <param name="name">The setting name.</param>
        /// <param name="value">The setting value to be set.</param>
        /// <returns></returns>
        private bool LoadSetting(ConfigNode node, string name, ref bool value)
        {
            bool result = false;
            if (node.HasValue(name) && bool.TryParse(node.GetValue(name), out result))
            {
                value = result;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Saves the settings to a file.
        /// </summary>
        private void SaveSettings()
        {
            // Create directory if needed.
            string settingsDirectory = Path.GetDirectoryName(AbsoluteSettingsPath);
            if (!Directory.Exists(settingsDirectory))
            {
                Directory.CreateDirectory(settingsDirectory);
            }
            ConfigNode settings = new ConfigNode();
            ConfigNode node = new ConfigNode(ConfigNodeName);
            node.AddValue("initialFailureProbability", initialFailureProbability);
            node.AddValue("maxFailureAltitudePercentage", maxFailureAltitudePercentage);
            node.AddValue("propagationChanceDecreases", propagationChanceDecreases);
            node.AddValue("failurePropagateProbability", failurePropagateProbability);
            node.AddValue("delayBetweenPartFailures", delayBetweenPartFailures);
            node.AddValue("autoAbort", autoAbort);
            settings.AddNode(node);
            settings.Save(AbsoluteSettingsPath);
        }
    }
}
