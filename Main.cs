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
        public Camera scaledSpaceCam;
        public UniverseSpace universeSpace;

        public static Main instance;

        internal CelestialBody mun;
        internal GameObject scaledObject;
        internal GameObject cube;
        
        void Awake()
        {
            Camera[] cameras = FindObjectsOfType<Camera>();
            foreach (Camera camera in cameras)
            {
                if (camera.name == "Camera ScaledSpace")
                {
                    scaledSpaceCam = camera;
                }
            }

            instance = this;
        }

        void Start()
        {
            InitializeHDR();
        }

        void InitializeHDR()
        { 
            Camera[] cameras = FindObjectsOfType<Camera>();

            foreach (Camera camera in cameras)
            {
                Log("ΦΩΤΟΓΡΑΦΙΚΗ ΜΗΧΑΝΗ: " + camera.name + ", cullingMask: " + Convert.ToString(camera.cullingMask));
                camera.allowHDR = true;
                Log("farClipPlane: " + camera.farClipPlane);
                if (camera.name == "Camera 00" || camera.name == "GalaxyCamera" || camera.name == "SkySphere Cam")
                {
                    camera.nearClipPlane *= 5f;
                    camera.farClipPlane *= 10f;
                }
            }
            //Graphics.activeTier
        }

        public static void Log(object msg)
        {
            Debug.Log("[NiftyNebulae]: " + msg);
        }
        public static void LogError(object msg)
        {
            Debug.LogError("[NiftyNebulae]: " + msg);
        }

        /// <summary>
        /// Debugs all attributes of an object
        /// </summary>
        /// <param name="problem">your object</param>
        /// <param name="type">typeof(your object's class name)</param>
        public static void LogAllProperties(object problem, Type type) //needs fixing, doesn't print private nor properties
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

    [KSPAddon(KSPAddon.Startup.AllGameScenes, false)]
    public class GeneralDebug : MonoBehaviour
    {
        Camera[] cameras;

        void Start()
        {
            cameras = FindObjectsOfType<Camera>();
        }

        void Update()
        {
            foreach (Camera camera in cameras)
            {
                Main.Log(camera.name + " position: " + camera.transform.position);
                Main.Log(camera.name + " local position: " + camera.transform.localPosition);
            }
        }
    }

    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class FlightDebug : MonoBehaviour
    {
        Camera scaledCam;
        void Start()
        {
            AtmosphereFromGround[] atmospheres = GameObject.FindObjectsOfType<AtmosphereFromGround>();

            foreach (AtmosphereFromGround atmo in atmospheres)
            {
                Main.Log("GOname: " + atmo.gameObject.name);
                Main.Log("atmoName: " + atmo.planet.name);
                Main.Log("shader: " + atmo.GetComponent<MeshRenderer>().material.shader.name);
                Main.Log("atmo render queue: " + atmo.GetComponent<MeshRenderer>().material.renderQueue);
                Main.Log("render queue planet: " + atmo.planet.scaledBody.GetComponent<MeshRenderer>().material.renderQueue);
                atmo.planet.scaledBody.GetComponent<MeshRenderer>().material.renderQueue = 2500; //first: nebula, second: planet, third: atmosphere
                //atmo.GetComponent<MeshRenderer>().material.shader;
            }
        }

        void Update()
        {
            Main.Log("active: " + Main.instance.scaledObject.activeSelf);
            Main.Log("cube active: " + Main.instance.cube.activeSelf);
            Main.Log("meshrenderer active: " + Main.instance.cube.GetComponent<MeshRenderer>().enabled);
        }
    }
}
