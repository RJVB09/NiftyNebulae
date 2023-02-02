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
        
        void Awake()
        {
            instance = this;
        }
        void Start()
        {
            NebulaInstantiator.instance.nebulaCFGs = ConfigLoader.nebulae;
            NebulaInstantiator.instance.InstantiateAllNebulae();
            foreach (GameObject nebula in NebulaInstantiator.instance.nebulaGOs)
            {
                Debug.Log(nebula.name + " exists?: " + !ReferenceEquals(nebula.GetComponent<Nebula>().material, null));
            }
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
                    break;
                case LogType.Warning:
                    Debug.LogWarning("[NiftyNebulae]: " + msg);
                    break;
                case LogType.Exception:
                    Debug.LogException(new Exception("[NiftyNebulae]: " + msg));
                    exceptions++;
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
            foreach (Nebula nebula in NebulaInstantiator.instance.nebulaObjects)
            {
                Debug.Log(nebula.name + " has material?: " + !ReferenceEquals(nebula.material, null));
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
