using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace NiftyNebulae
{
    [DefaultExecutionOrder(580)]
    public class Nebula : MonoBehaviour
    {
        Material material;
        public CelestialBody parentBody;
        public int renderQueueIndex = 2000;

        public NebulaCFG settings;
        float density;

        void Start()
        {
            density = 34.6410161514f / transform.lossyScale.magnitude * settings.densityMultiplier; // 20f * sqrt(3) / length of diagonal
            material = gameObject.GetComponent<MeshRenderer>().material;
            if (ConfigLoader.instance.fixedStepMode)
            {
                material.shader = AssetLoader.GetShader("Unlit/Nebula3DFixedStep");//FixedOuter
            }
            else
            {
                material.shader = AssetLoader.GetShader("Unlit/Nebula3D");//FixedOuter
            }
            
            material.SetVector("_DomainScale", new Vector4(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z, 0));
            material.SetVector("_DomainPosition", new Vector4(transform.position.x, transform.position.y, transform.position.z, 0));
            material.SetInt("_MaxSteps", ConfigLoader.instance.maxRaymarchSteps);
            material.SetInt("_FixedSteps", ConfigLoader.instance.fixedRaymarchSteps);
            material.SetFloat("_StepSize", transform.lossyScale.x * ConfigLoader.instance.stepSize); //old 0.005
            material.SetFloat("_Density", density); // 20f * sqrt(3) / length of diagonal
            material.SetTexture("_Texture2D", AssetLoader.LoadPNG("GameData/" + settings.texture));
            material.SetInt("_Texture2DSliceLength", (int)settings.textureTileSize);
            material.SetInt("_LODLevel", ConfigLoader.instance.mapLODLevel);
            material.SetFloat("_BLIThreshold", ConfigLoader.instance.interpolationThreshold);

            transform.localScale = new Vector3(transform.localScale.x * settings.domainScale.x, transform.localScale.y * settings.domainScale.y, transform.localScale.z * settings.domainScale.z);

            material.renderQueue = 2700;
            //Debug.Log(settings.name + " render queue: " + material.renderQueue);
        }
        void LateUpdate()
        {
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            material.SetFloat("_Rotation", (gameObject.transform.parent.rotation.eulerAngles.y + (float)parentBody.rotationAngle) * Mathf.Deg2Rad);
            material.SetVector("_DomainPosition", new Vector4(transform.position.x, transform.position.y, transform.position.z, 0));
            //Main.Log((int)(Mathf.Pow((transform.position.magnitude / transform.lossyScale.magnitude) + 1.0f, -1.0f) * 100));
            //material.SetInt("_FixedSteps", (int)(Mathf.Pow((transform.position.magnitude / transform.lossyScale.magnitude) + 1.0f, -1.0f) * ConfigLoader.instance.fixedRaymarchSteps));
            material.SetFloat("_StepSize", (int)(Mathf.Pow(0.5f*(transform.position.magnitude / transform.lossyScale.magnitude) + 1.0f, -1.0f) * transform.lossyScale.x * ConfigLoader.instance.stepSize));

            if (settings.shouldFadeWithSkybox && HighLogic.LoadedScene != GameScenes.TRACKSTATION)
            {
                material.SetFloat("_Density", density * (1 - Mathf.Pow(Mathf.Clamp01(SkyFade.Instance.totalFade) * settings.fadeAmount, 0.5f))); //breaks when focused on sun
            }
            else
            {
                material.SetFloat("_Density", density);
            }
            

            //Main.Log("fade: " + Mathf.Clamp01(SkyFade.Instance.totalFade));
        }
    }
}
