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
        public int fixedRaymarchSteps = 100;
        [Persistent]
        public float stepSize = 0.02f;
        [Persistent]
        public bool fixedStepMode = false;
        [Persistent]
        public float interpolationThreshold = 0;
        [Persistent]
        public int mapLODLevel = 0;
        [Persistent]
        public bool debug = false;

        private void WriteConfigIfNoneExists()
        {
            string codeBase = Assembly.GetExecutingAssembly().CodeBase;
            UriBuilder uri = new UriBuilder(codeBase);
            string pluginPath = Uri.UnescapeDataString(uri.Path);
            pluginPath = Path.GetDirectoryName(pluginPath);
            Main.Log("Generating default NiftyNebulaeGlobals.cfg");
            ScreenMessages.PostScreenMessage("Generating default NiftyNebulaeGlobals.cfg...",5);
            using (StreamWriter configFile = new StreamWriter(pluginPath + "/../NiftyNebulaeGlobals.cfg"))
            {
                configFile.WriteLine("// Global configuration information for NiftyNebulae.");
                configFile.WriteLine("NiftyNebulaGlobals");
                configFile.WriteLine("{");
                configFile.WriteLine("	maxRaymarchSteps = 500 // Uint: The maximum amount of steps the raymarcher is allowed to make before it terminates. Raise if you have decreased the step size and start seeing weird artifacts.");
                configFile.WriteLine("	stepSize = 0.02 // Float: Minimum step size for raymarcher in terms of nebula diameters. Should therefore almost always be below 1. Raise for better performance but coarser looking nebulae. And vice versa, raise for nicer looking nebulae :)");
                configFile.WriteLine("	downscaleFactor = 4 // Int: Nebulae are rendered at a lower resolution to save performance, the lower resolution is the screenresolution divided by this number. Raise for better performance but poorer screen quality.");
                configFile.WriteLine("	interpolationThreshold = 0 // Float: The shader uses its own way of 3D interpolation between voxels. This process requires 6 extra texture samples. By increasing this value, densities below it will be interpolated at lower quality saving on performance.");
                configFile.WriteLine("	mapLODLevel = 0 // Uint: Drastically lowers the quality of the nebula texture allowing for faster texture sampling. The higher the number the lower the quality of the nebula.");
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
                ScreenMessages.PostScreenMessage("<color=#FFE100>No NiftyNebulaGlobals file found, using defaults.</color>", 5);
            }
            else if (globalSettings.Length > 1)
            {
                Main.Log("Multiple NiftyNebulaGlobals files detected, check your install.", LogType.Warning);
                ScreenMessages.PostScreenMessage("<color=#FFE100>Multiple NiftyNebulaGlobals files detected, check your install.</color>", 5);
            }

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
        public Vector3 domainScale = Vector3.one;
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
