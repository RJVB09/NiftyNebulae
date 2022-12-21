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
        public static List<NebulaCFG> nebulae;
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
            Main.Log("Is awake");
            instance = this;
            DontDestroyOnLoad(this);
            globalSettings = GameDatabase.Instance.GetConfigs("NiftyNebulaGlobals");

            if (globalSettings.Length == 0 || globalSettings == null)
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

            Main.Log("(CURRENT SETTINGS) maxRaymarchSteps: " + ConfigLoader.instance.maxRaymarchSteps + ", stepSize: " + ConfigLoader.instance.stepSize);
        }

        void Start()
        {
            Main.Log("Nebulae loading commenced.");
            nebulae = new List<NebulaCFG>();
            nebulaeConfigs = GameDatabase.Instance.GetConfigs("NiftyNebula");
            for (int i = 0; i < nebulaeConfigs.Length; i++)
            {
                try
                {
                    NebulaCFG nebula = new NebulaCFG();
                    ConfigNode.LoadObjectFromConfig(nebula, nebulaeConfigs[i].config);
                    nebulae.Add(nebula);
                    Main.Log("----------------------NEBULA----------------------");
                    Main.Log("name: " + nebula.name);
                    Main.Log("nebulaRadius: " + nebula.nebulaRadius);
                    Main.Log("parentName: " + nebula.parentName);
                    Main.Log("densityMultiplier: " + nebula.densityMultiplier);
                    Main.Log("texture: " + nebula.texture);
                    Main.Log("textureTileSize: " + nebula.textureTileSize);
                    Main.Log("--------------------------------------------------");
                }
                catch (Exception e)
                {
                    Main.Log("Couldn't load nebula from config. " + e, LogType.Exception);
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
        public bool shouldFadeWithSkybox = true; //Whether the nebula should fade to darkness when entering an atmosphere, making it invisible in daytime. 
        [Persistent]
        public float fadeAmount = 1; //1 = invisible at day, 0 = visible at daytime, same as shouldFadeWithSkybox = false. Can be any value inbetween.
        [Persistent]
        public string texture = ""; //File path, e.g. "GameData/NiftyNebulae/PluginData/cat_eye_2.png"
        [Persistent]
        public uint textureTileSize = 4u;
    }
}
