using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using UnityEngine;

namespace NiftyNebulae
{
    [KSPAddon(KSPAddon.Startup.Instantly, true)]
    public class ConfigLoader : MonoBehaviour
    {
        public UrlDir.UrlConfig[] globalSettings;
        public List<NebulaCFG> nebulae;
        UrlDir.UrlConfig[] nebulaeConfigs;
        public static ConfigLoader instance;
        
        [Persistent]
        public int maxRaymarchSteps = 500;
        [Persistent]
        public float stepSize = 0.02f;
        
        private void WriteConfigIfNoneExists()
        {
            string codeBase = Assembly.GetExecutingAssembly().CodeBase;
            UriBuilder uri = new UriBuilder(codeBase);
            string pluginPath = Uri.UnescapeDataString(uri.Path);
            pluginPath = Path.GetDirectoryName(pluginPath);
            Main.Log("Generating default NiftyNebulaeGlobals.cfg");
            using (StreamWriter configFile = new StreamWriter(pluginPath + "/../NiftyNebulaeGlobals.cfg"))
            {
                configFile.WriteLine("// Global configuration information for NiftyNebulae.");
                configFile.WriteLine("NiftyNebulaGlobals");
                configFile.WriteLine("{");
                configFile.WriteLine("	maxRaymarchSteps = 500 // Uint: Maximum number of steps to be taken before raymarcher terminates.");
                configFile.WriteLine("	stepSize = 0.02 // Float: Minimum step size for raymarcher in terms of nebula diameters. Increase for better performance but coarser resolution.");
                configFile.WriteLine("}");
                configFile.Flush();
                configFile.Close();
            }
        }

        void Awake()
        {
            instance = this;
            DontDestroyOnLoad(this);
            globalSettings = GameDatabase.Instance.GetConfigs("NiftyNebulaGlobals");
            ConfigNode.LoadObjectFromConfig(this, globalSettings[0].config);

            if (globalSettings.Length == 0)
            {
                WriteConfigIfNoneExists();
                Main.Log("No NiftyNebulaGlobals file found, using defaults.", LogType.Warning);
            }
            else if (globalSettings.Length > 1)
                Main.Log("Multiple NiftyNebulaGlobals files detected, check your install.", LogType.Warning);
            try
            {
                ConfigNode.LoadObjectFromConfig(this, globalSettings[0].config);
            }
            catch (Exception e)
            {
                Main.Log(e.Message, LogType.Exception);
            }
        }

        void Start()
        {
            nebulae = new List<NebulaCFG>();
            nebulaeConfigs = GameDatabase.Instance.GetConfigs("NiftyNebula");
            for (int i = 0; i < nebulaeConfigs.Length; i++)
            {
                try
                {
                    NebulaCFG nebula = new NebulaCFG();
                    ConfigNode.LoadObjectFromConfig(nebula, nebulaeConfigs[i].config);
                    nebulae.Add(nebula);
                }
                catch (Exception e)
                {
                    Main.Log(e, LogType.Exception);
                }
            }
        }
    }

    public class NebulaCFG
    {
        [Persistent]
        public string name = "";
        [Persistent]
        public float nebulaRadius = 10f;
        [Persistent]
        public string parentName = ""; //The name of the parentbody
        [Persistent]
        public float densityMultiplier = 1f; //Multiplier for nebula density.
        [Persistent]
        public string texture = ""; //File path, e.g. "GameData/NiftyNebulae/PluginData/cat_eye_2.png"
        [Persistent]
        public uint textureTileSize = 4u;
    }
}
