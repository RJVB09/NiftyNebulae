using NiftyNebulae;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace NiftyNebulae
{
    public class DownscaleNebulaRenderer : MonoBehaviour //Has some problems
    {
        [System.Serializable]
        public struct NebulaMaterials
        {
            public NebulaMaterials(Material rayMarchMaterial, Material renderMaterial, MeshRenderer meshRenderer)
            {
                this.rayMarchMaterial = rayMarchMaterial;
                this.renderMaterial = renderMaterial;
                this.meshRenderer = meshRenderer;
            }

            public Material rayMarchMaterial;
            public Material renderMaterial;
            public MeshRenderer meshRenderer;
        }

        public List<NebulaMaterials> nebulaMaterials;

        int width;
        int height;
        public int downscale = 4;
        public Camera cam;
        CommandBuffer rendererCommandBuffer;
        RenderTexture downscaled;
        RenderTargetIdentifier downscaledRenderTarget;

        public void Initialize()
        {
            cam = gameObject.GetComponent<Camera>();
            Main.Log("Initializing downscale component for " + cam.name);
            if (!ReferenceEquals(cam.activeTexture, null))
            {
                width = cam.activeTexture.width / downscale;
                height = cam.activeTexture.height / downscale;
            }
            else
            {
                width = Screen.width / downscale;
                height = Screen.height / downscale;
            }
            downscaled = CreateTexture(width, height);

            downscaledRenderTarget = new RenderTargetIdentifier(downscaled);

            ReloadCommandBuffer();
        }

        public void SetNebulaeMaterials(List<NebulaMaterials> list)
        {
            nebulaMaterials = list;
        }

        void ReloadCommandBuffer()
        {
            if (!ReferenceEquals(rendererCommandBuffer, null))
                cam.RemoveCommandBuffer(CameraEvent.BeforeForwardAlpha, rendererCommandBuffer);
            rendererCommandBuffer = new CommandBuffer();
            rendererCommandBuffer.name = "Nebula Rendering";
            foreach (NebulaMaterials nebulaMaterialsSingle in nebulaMaterials.OrderByDescending(o => (o.meshRenderer.transform.position - cam.transform.position).magnitude)) //Temporary depth sorting fix
            {
                rendererCommandBuffer.SetRenderTarget(downscaledRenderTarget);
                rendererCommandBuffer.ClearRenderTarget(false, true, Color.clear);
                rendererCommandBuffer.DrawRenderer(nebulaMaterialsSingle.meshRenderer, nebulaMaterialsSingle.rayMarchMaterial, 0, 0);
                rendererCommandBuffer.SetGlobalTexture("_QuarterResNebula", downscaled);
                rendererCommandBuffer.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);
                rendererCommandBuffer.DrawRenderer(nebulaMaterialsSingle.meshRenderer, nebulaMaterialsSingle.renderMaterial, 0, 0);
                rendererCommandBuffer.DrawRenderer(nebulaMaterialsSingle.meshRenderer, nebulaMaterialsSingle.renderMaterial, 0, 1);
            }

            cam.AddCommandBuffer(CameraEvent.BeforeForwardAlpha, rendererCommandBuffer);
        }

        private void OnPreRender()
        {
            ReloadCommandBuffer();
        }

        RenderTexture CreateTexture(int width, int height)
        {
            RenderTexture rt = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
            rt.anisoLevel = 1;
            rt.antiAliasing = 1;
            rt.volumeDepth = 0;
            rt.useMipMap = false;
            rt.autoGenerateMips = false;
            rt.filterMode = FilterMode.Bilinear;
            rt.Create();

            return rt;
        }
    }
}
