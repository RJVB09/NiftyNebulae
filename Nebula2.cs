﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace NiftyNebulae
{
    [DefaultExecutionOrder(580)]
    public class Nebula2 : MonoBehaviour
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
            material.shader = AssetLoader.GetShader("Unlit/Nebula3D");
            material.SetVector("_DomainScale", new Vector4(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z, 0));
            material.SetVector("_DomainPosition", new Vector4(transform.position.x, transform.position.y, transform.position.z, 0));
            material.SetInt("_MaxSteps", ConfigLoader.instance.maxRaymarchSteps);
            material.SetFloat("_StepSize", transform.lossyScale.x * ConfigLoader.instance.stepSize); //old 0.005
            material.SetFloat("_Density", density); // 20f * sqrt(3) / length of diagonal
            material.SetTexture("_Texture2D", AssetLoader.LoadPNG("GameData/" + settings.texture));
            material.SetInt("_Texture2DSliceLength", (int)settings.textureTileSize);

            material.renderQueue = renderQueueIndex;
        }
        void LateUpdate()
        {
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            material.SetFloat("_Rotation", (gameObject.transform.parent.rotation.eulerAngles.y + (float)parentBody.rotationAngle) * Mathf.Deg2Rad);
            material.SetVector("_DomainPosition", new Vector4(transform.position.x, transform.position.y, transform.position.z, 0));
            material.SetVector("_DomainScale", new Vector4(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z, 0));
            /*
            if (settings.shouldFadeWithSkybox && HighLogic.LoadedScene != GameScenes.TRACKSTATION)
            {
                material.SetFloat("_Density", density * (1 - Mathf.Pow(Mathf.Clamp01(SkyFade.Instance.totalFade) * settings.fadeAmount, 0.5f)));
            }
            else
            {
                material.SetFloat("_Density", density);
            }
            */
            //Main.Log("fade: " + Mathf.Clamp01(SkyFade.Instance.totalFade));
        }
    }
}