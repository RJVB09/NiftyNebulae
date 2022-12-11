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
        CelestialBody mun;
        GameObject scaledObject;
        GameObject cube;
        void Start()
        {
            mun = FlightGlobals.Bodies.Find(a => a.name == "Mun");
            scaledObject = mun.scaledBody;
             
            cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.SetParent(scaledObject.transform,true);
            cube.transform.localPosition = Vector3.zero;
            cube.transform.localScale = Vector3.one * 2 * 2000; //radius body in scaled space as child is 1000
            cube.layer = scaledObject.layer;

            cube.AddComponent<Nebula>().scaledSpaceGO = scaledObject;
            Nebula nebula = cube.GetComponent<Nebula>();

            nebula.offset = Vector3.one * 400000;
            nebula.texture = AssetLoader.LoadPNG("GameData/NiftyNebulae/PluginData/crap_nebula.png");
            InitializeHDR();
        }

        void Update()
        {
            //cube.transform.position = scaledObject.transform.position + Vector3.one * 400000;
            Main.log("cubeScale: " + cube.transform.lossyScale);
            Main.log("cubePos: " + cube.transform.position);
            Main.log("sunScale: " + scaledObject.transform.lossyScale);
            Main.log("sunPos: " + scaledObject.transform.position);
            Main.log("cubeLocalPos: " + cube.transform.localPosition);
            Main.log("sunLocalPos: " + scaledObject.transform.localPosition);
            log("sunlayer: " + scaledObject.layer);
            log("cubelayer: " + cube.layer);
            log("sunac: " + scaledObject.activeSelf);
            log("cubeac: " + cube.activeSelf);
            log("sunParent: " + scaledObject.activeSelf);
            log("cubeParent: " + cube.activeSelf);
            log("testingThing: " + SceneManager.GetActiveScene().name + (cube != null));
            log("scale: " + ScaledSpace.ScaleFactor);
        }

        void InitializeHDR()
        { 
            Camera[] cameras = GameObject.FindObjectsOfType<Camera>();

            foreach (Camera camera in cameras)
            {
                log("ΦΩΤΟΓΡΑΦΙΚΗ ΜΗΧΑΝΗ: " + camera.name + ", cullingMask: " + Convert.ToString(camera.cullingMask));
                camera.allowHDR = true;
            }
            //Graphics.activeTier
        }

        public static void log(object msg)
        {
            Debug.Log("[NiftyNebulae] " + msg);
        }
        public static void logError(object msg)
        {
            Debug.LogError("[NiftyNebulae] " + msg);
        }
    }
}
