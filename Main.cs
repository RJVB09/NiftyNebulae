using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NiftyNebulae
{
    [KSPAddon(KSPAddon.Startup.MainMenu, false)]
    public class Main : MonoBehaviour
    {
        public static Camera scaledSpaceCam;
        public static Main instance;

        internal CelestialBody mun;
        internal GameObject scaledObject;
        internal GameObject cube;
        
        
        void Awake()
        {
            instance = this;
        }
        void Start()
        {

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
                    break;
                case LogType.Warning:
                    Debug.LogWarning("[NiftyNebulae]: " + msg);
                    break;
                case LogType.Exception:
                    Debug.LogException(new Exception("[NiftyNebulae]: " + msg));
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
        public static void LogAllProperties(object[] problem, Type type)
        {
            System.Reflection.PropertyInfo[] properties = type.GetProperties(System.Reflection.BindingFlags.GetProperty | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.GetField);
            Debug.Log("[NiftyNebulae]: Debug logging all attributes of class " + type.Name);
            for (int i = 0; i < problem.Length; i++)
            {
                Debug.Log("Debug Object Index: " + i);
                foreach (System.Reflection.PropertyInfo property in properties)
                    Debug.Log(property.Name + ": " + property.GetValue(problem));
            }
        }
    }

    [KSPAddon(KSPAddon.Startup.FlightAndKSC, false)]
    public class FlightIni : MonoBehaviour
    {
        void Start()
        {
            AtmosphereFromGround[] atmospheres = GameObject.FindObjectsOfType<AtmosphereFromGround>();
            foreach (AtmosphereFromGround atmo in atmospheres)
                atmo.planet.scaledBody.GetComponent<MeshRenderer>().material.renderQueue = 2500; //first: nebula, second: planet, third: atmosphere
            InitializeHDR();
        }
        void InitializeHDR()
        { 
            Camera[] cameras = FindObjectsOfType<Camera>();
            
            foreach (Camera camera in cameras)
            {
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
