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
        /*
         * TODO:
         * -fix nebula rendering in front of scaled space objects
         * -fix rotating nebula (prob scaledspace) when timewarping on planet
         * -fix nebula rendering in front of atmosphere without getting the funny black bands
        */

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
            mun = FlightGlobals.Bodies.Find(a => a.name == "Sun");
            CelestialBody kerbin = FlightGlobals.Bodies.Find(a => a.name == "Kerbin");
            scaledObject = mun.scaledBody;
             
            cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "CHILD OF THE SUN";
            cube.transform.SetParent(scaledObject.transform,true);
            cube.transform.localPosition = Vector3.zero;
            float sizeInBodyRadii = 560f;
            cube.transform.localScale = Vector3.one * 2 * 1000 * sizeInBodyRadii; //radius body in scaled space as child is 1000
            cube.layer = scaledObject.layer;

            //cube.transform.SetParent(scaledObject.transform.parent, true);
            Nebula nebula = cube.AddComponent<Nebula>();
            nebula.scaledSpaceGO = scaledObject;
            nebula.offset = Vector3.one * 400000;
            nebula.texture = AssetLoader.LoadPNG("GameData/NiftyNebulae/PluginData/crap_nebula.png");

            Log("lossyScale: " + cube.transform.lossyScale);
            InitializeHDR();
        }

        //void Update()
        //{
        //    //cube.transform.position = scaledObject.transform.position;
        //    //Log("cubeScale: " + cube.transform.lossyScale);
        //    //Log("cubePos: " + cube.transform.position);
        //    //Log("sunScale: " + scaledObject.transform.lossyScale);
        //    //Log("sunPos: " + scaledObject.transform.position);
        //    //Log("cubeLocalPos: " + cube.transform.localPosition);
        //    //Log("sunLocalPos: " + scaledObject.transform.localPosition);
        //    //Log("sunlayer: " + scaledObject.layer);
        //    //Log("cubelayer: " + cube.layer);
        //    //Log("sunac: " + scaledObject.activeSelf);
        //    //Log("cubeac: " + cube.activeSelf);
        //    //Log("sunParent: " + scaledObject.activeSelf);
        //    //Log("cubeParent: " + cube.activeSelf);
        //    //Log("testingThing: " + SceneManager.GetActiveScene().name + (cube != null));
        //    //Log("scale: " + ScaledSpace.ScaleFactor);
        //}

        void InitializeHDR()
        { 
            Camera[] cameras = FindObjectsOfType<Camera>();

            foreach (Camera camera in cameras)
            {
                Log("ΦΩΤΟΓΡΑΦΙΚΗ ΜΗΧΑΝΗ: " + camera.name + ", cullingMask: " + Convert.ToString(camera.cullingMask));
                camera.allowHDR = true;
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
            System.Reflection.PropertyInfo[] properties = type.GetProperties();
            Debug.Log("[NiftyNebulae]: Debug logging all attributes of class " + type.Name);
            foreach (System.Reflection.PropertyInfo property in properties)
                Debug.Log(property.Name + ": " + property.GetValue(problem));
        }
        public static void LogAllProperties(object[] problem, Type type)
        {
            System.Reflection.PropertyInfo[] properties = type.GetProperties();
            Debug.Log("[NiftyNebulae]: Debug logging all attributes of class " + type.Name);
            for (int i = 0; i < problem.Length; i++)
            {
                Debug.Log("Debug Object Index: " + i);
                foreach (System.Reflection.PropertyInfo property in properties)
                    Debug.Log(property.Name + ": " + property.GetValue(problem));
            }
        }
    }

    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class FlightDebug : MonoBehaviour
    {
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
        }
    }
}
