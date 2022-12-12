using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace NiftyNebulae
{
    [DefaultExecutionOrder(40000)]
    class Nebula : MonoBehaviour
    {
        public GameObject scaledSpaceGO;
        public Vector3 offset;
        public Material material;
        public Texture texture;
        public int cubeResolution = 256;

        void Start()
        {
            material = gameObject.GetComponent<MeshRenderer>().material;
            material.shader = AssetLoader.GetShader("Unlit/Nebula3D");
            material.SetVector("_DomainScale",new Vector4(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z,0));
            material.SetVector("_DomainPosition", new Vector4(transform.position.x, transform.position.y, transform.position.z, 0));
            material.SetInt("_MaxSteps", 1000);
            material.SetFloat("_StepSize", transform.lossyScale.x * 0.01f); //old 0.005
            material.SetFloat("_Density", 11.5470053838f / transform.lossyScale.magnitude); // 20f / (sqrt(3) * length of diagonal)
            material.SetTexture("_Texture2D", texture);
            material.SetInt("_Texture2DSliceLength", cubeResolution);

            //Main.log("DomainScale: " + transform.lossyScale);
            //material.SetColor("_FillColor", Color.red);
        }


        void LateUpdate()
        {
            //transform.position = scaledSpaceGO.transform.position + offset;
            transform.localPosition = Vector3.zero;
            transform.rotation = Quaternion.identity;
            material.SetVector("_DomainPosition", new Vector4(transform.position.x, transform.position.y, transform.position.z, 0));
            material.SetVector("_DomainScale", new Vector4(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z, 0));
        }
    }
}
