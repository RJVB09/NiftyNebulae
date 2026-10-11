using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NiftyNebulae
{
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class Main : MonoBehaviour
    {
        public static Camera scaledSpaceCam;
        public static Main instance;
        public static int exceptions = 0;
        public static int errors = 0;
        public Material nebulaDrawMaterial;

        void Awake()
        {
            instance = this;
            nebulaDrawMaterial = new Material(AssetLoader.GetShader("RJ/Nebula3DDraw"));
        }
        void Start()
        {
            NebulaInstantiator.instance.nebulaCFGs = ConfigLoader.nebulae;
            NebulaInstantiator.instance.InstantiateAllNebulae();
            
        }

        public static bool SingularityInstalled()
        {
            foreach (AssemblyLoader.LoadedAssembly loaded in AssemblyLoader.loadedAssemblies)
            {
                if (loaded.name == "Singularity" || (loaded.assembly != null && loaded.assembly.GetName().Name == "Singularity"))
                    return true;
            }
            return false;
        }

        public static void Log(object msg, UnityEngine.LogType type = LogType.Log)
        {
            switch (type)
            {
                case LogType.Log:
                    Debug.Log("[NiftyNebulae]: " + msg);
                    break;
                case LogType.Error:
                    Debug.LogError("[NiftyNebulae]: " + msg);
                    errors++;
                    ErrorPopup.Add(msg);
                    break;
                case LogType.Warning:
                    Debug.LogWarning("[NiftyNebulae]: " + msg);
                    break;
                case LogType.Exception:
                    Debug.LogException(new Exception("[NiftyNebulae]: " + msg));
                    exceptions++;
                    ErrorPopup.Add(msg);
                    break;
                case LogType.Assert:
                    Debug.LogAssertion("[NiftyNebulae]: " + msg);
                    break;
            }
        }

        /// <summary>
        /// Debugs all attributes of an object
        /// </summary>
        /// <param name="problem">your object</param>
        /// <param name="type">typeof(your object's class name)</param>
        public static void LogAllProperties(object problem, Type type)
        {
            System.Reflection.PropertyInfo[] properties = type.GetProperties(System.Reflection.BindingFlags.GetProperty | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.GetField);
            Debug.Log("[NiftyNebulae]: Debug logging all attributes of class " + type.Name);
            foreach (System.Reflection.PropertyInfo property in properties)
                Debug.Log(property.Name + ": " + property.GetValue(problem));
        }
        public static void LogAllProperties(object[] problems, Type type)
        {
            System.Reflection.PropertyInfo[] properties = type.GetProperties(System.Reflection.BindingFlags.GetProperty | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.GetField);
            Debug.Log("[NiftyNebulae]: Debug logging all attributes of class " + type.Name);
            for (int i = 0; i < problems.Length; i++)
            {
                Debug.Log("Debug Object Index: " + i);
                foreach (System.Reflection.PropertyInfo property in properties)
                    Debug.Log(property.Name + ": " + property.GetValue(problems[i]));
            }
        }
    }

    public class ErrorPopup : MonoBehaviour
    {
        const int MaxListed = 8;
        const int MaxKnown = 64;
        const int MaxLength = 240;
        const int SettleFrames = 3;

        static ErrorPopup instance;
        static readonly List<string> known = new List<string>();
        static readonly List<string> pending = new List<string>();
        static int lastAddedFrame;

        public static void Add(object msg)
        {
            string text = msg == null ? "" : msg.ToString();
            int lineEnd = text.IndexOfAny(new char[] { '\r', '\n' });
            if (lineEnd >= 0)
                text = text.Substring(0, lineEnd);
            if (text.Length > MaxLength)
                text = text.Substring(0, MaxLength) + "...";
            if (text.Length == 0 || known.Count >= MaxKnown || known.Contains(text))
                return;

            known.Add(text);
            pending.Add(text);
            lastAddedFrame = Time.frameCount;

            if (instance == null)
            {
                GameObject holder = new GameObject("NiftyNebulae error popup");
                DontDestroyOnLoad(holder);
                instance = holder.AddComponent<ErrorPopup>();
            }
        }

        void Update()
        {
            if (pending.Count == 0 || Time.frameCount - lastAddedFrame < SettleFrames)
                return;

            GameScenes scene = HighLogic.LoadedScene;
            if (scene != GameScenes.MAINMENU && scene != GameScenes.SPACECENTER && scene != GameScenes.FLIGHT && scene != GameScenes.TRACKSTATION && scene != GameScenes.EDITOR)
                return;

            Show();
        }

        void Show()
        {
            StringBuilder message = new StringBuilder();
            message.Append("NiftyNebulae was not able to load everything due to ");
            message.Append(pending.Count == 1 ? "an error" : pending.Count + " errors");
            message.Append(" in the loading process.\n\n");

            int listed = Mathf.Min(pending.Count, MaxListed);
            for (int i = 0; i < listed; i++)
                message.Append("- " + pending[i] + "\n");
            if (pending.Count > listed)
                message.Append("...and " + (pending.Count - listed) + " more.\n");

            message.Append("\nThe affected nebulae may be missing or look wrong.\n\n");
            message.Append("Please check your nebula configs and textures, or contact the nebula pack author about the issue and send them your KSP.log.\n\n");
            pending.Clear();

            try
            {
                PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), "NiftyNebulaeFail", "Warning", message.ToString(), "OK", true, UISkinManager.GetSkin("MainMenuSkin"));
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }

    [KSPAddon(KSPAddon.Startup.FlightAndKSC, false)]
    public class FlightInit : MonoBehaviour
    {
        void Start()
        {
            AtmosphereFromGround[] atmospheres = GameObject.FindObjectsOfType<AtmosphereFromGround>();
            foreach (AtmosphereFromGround atmo in atmospheres)
            {
                Main.Log("atmoLayer: " + atmo.gameObject.layer);
                atmo.planet.scaledBody.GetComponent<MeshRenderer>().material.renderQueue = 2500; //first: nebula, second: planet, third: atmosphere
            }
            InitializeHDR();
            ApplyDownscaleComponentToCams();
        }
        
        void ApplyDownscaleComponentToCams()
        {
            Camera[] cameras = FindObjectsOfType<Camera>();

            List<DownscaleNebulaRenderer.NebulaMaterials> nebulaMaterials = new List<DownscaleNebulaRenderer.NebulaMaterials>();

            Nebula[] nebulae = GameObject.FindObjectsOfType<Nebula>();
            foreach (Nebula nebula in nebulae)
            {
                Main.Log("Adding " + nebula.settings.name + " to downscaler scripts");
                Main.Log("material: " + !ReferenceEquals(nebula.material, null));
                Main.Log("drawmaterial: " + !ReferenceEquals(Main.instance.nebulaDrawMaterial, null));
                Main.Log("meshrenderer: " + !ReferenceEquals(nebula.meshRenderer, null));
                

                
                nebulaMaterials.Add(new DownscaleNebulaRenderer.NebulaMaterials(
                        nebula.material,
                        Main.instance.nebulaDrawMaterial,
                        nebula.meshRenderer
                    )
                );
                

            }

            if (ConfigLoader.instance.singularityCompatibility && Main.SingularityInstalled())
                SingularityCubemap.Setup(nebulaMaterials);

            Main.Log("Applying downscale render script to scaled space cameras.");
            foreach (Camera camera in cameras)
            {
                if ((camera.cullingMask & (1 << 9)) != 0 && camera.GetComponent<DownscaleNebulaRenderer>() is null)
                {
                    DownscaleNebulaRenderer bufferscript = camera.gameObject.AddComponent<DownscaleNebulaRenderer>();
                    bufferscript.SetNebulaeMaterials(nebulaMaterials);

                    //Main.Log("AAAAAAAAAAAAAAAAAAAAAAAAdfdsdsffdsfds: " + !ReferenceEquals(nebulae,null)); //dies
                    //Main.Log("AAAAAAAAAAAAAAAAAAAAAAAAdfdsdsffdsfds: " + nebulae.Length);
                    Main.Log("bufferscript: " + !ReferenceEquals(bufferscript, null));
                    bufferscript.Initialize();
                }
            }
        }
        
        void InitializeHDR()
        { 
            Camera[] cameras = FindObjectsOfType<Camera>();

            Main.Log("Applying HDR to all cameras.");
            foreach (Camera camera in cameras)
            {
                Main.Log(camera.name + ", culling mask: " + Convert.ToString(camera.cullingMask, 2).PadLeft(32, '0'));
                camera.allowHDR = true;
                if (camera.name == "Camera ScaledSpace")
                {
                    Main.scaledSpaceCam = camera;
                    camera.farClipPlane = float.MaxValue * 1E-6f;
                }
            }
        }
    }
}
