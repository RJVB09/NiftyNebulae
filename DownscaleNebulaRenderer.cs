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

        const float MinSteps = 64f;
        const float MinPixels = 0.25f;
        readonly Plane[] frustumPlanes = new Plane[6];

        public bool keepImages;
        public readonly List<RenderTexture> capturedImages = new List<RenderTexture>();
        public Vector3 capturedPosition;
        public Quaternion capturedRotation = Quaternion.identity;
        public float capturedTanHorizontal;
        public float capturedTanVertical;
        public int capturedFrame = -1;
        readonly List<RenderTexture> images = new List<RenderTexture>();

        int width;
        int height;
        public int downscale = ConfigLoader.instance.downscaleFactor;
        public Camera cam;
        CommandBuffer rendererCommandBuffer;
        CommandBuffer earlyBuffer;
        RenderTexture downscaled;
        RenderTexture volumeDepth;
        RenderTargetIdentifier downscaledRenderTarget;
        RenderTargetIdentifier volumeDepthRenderTarget;
        //lol
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
            Initialize(width, height);
        }

        public void Initialize(int textureWidth, int textureHeight)
        {
            cam = gameObject.GetComponent<Camera>();
            width = textureWidth;
            height = textureHeight;
            downscaled = CreateTexture(width, height, RenderTextureFormat.ARGB32);
            volumeDepth = CreateTexture(width, height, RenderTextureFormat.ARGBFloat);

            downscaledRenderTarget = new RenderTargetIdentifier(downscaled);
            volumeDepthRenderTarget = new RenderTargetIdentifier(volumeDepth);

            ReloadCommandBuffer();
        }

        public void SetNebulaeMaterials(List<NebulaMaterials> list)
        {
            nebulaMaterials = list;
        }

        void ReloadCommandBuffer()
        {
            if (!ReferenceEquals(rendererCommandBuffer, null))
            {
                cam.RemoveCommandBuffer(CameraEvent.BeforeForwardAlpha, rendererCommandBuffer);
                rendererCommandBuffer.Clear();
            }
            else
            {
                rendererCommandBuffer = new CommandBuffer();
                rendererCommandBuffer.name = "Nebula Rendering";
            }

            if (!ReferenceEquals(earlyBuffer, null))
            {
                cam.RemoveCommandBuffer(CameraEvent.BeforeForwardOpaque, earlyBuffer);
                earlyBuffer.Clear();
            }
            else
            {
                earlyBuffer = new CommandBuffer();
                earlyBuffer.name = "Nebula Raymarching";
            }
            CommandBuffer marchBuffer = keepImages ? earlyBuffer : rendererCommandBuffer;

            GeometryUtility.CalculateFrustumPlanes(cam, frustumPlanes);
            Vector3 cameraPosition = cam.transform.position;
            float pixelsPerRadian = height / (2f * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad));
            float quality = ConfigLoader.instance.adaptiveStepQuality;
            capturedImages.Clear();

            foreach (NebulaMaterials nebulaMaterialsSingle in nebulaMaterials.OrderByDescending(o => (o.meshRenderer.transform.position - cam.transform.position).magnitude)) //Temporary depth sorting fix
            {
                Transform nebula = nebulaMaterialsSingle.meshRenderer.transform;
                Vector3 scale = nebula.lossyScale;
                Vector3 center = nebula.position;
                float radius = scale.magnitude * 0.5f;

                bool visible = true;
                for (int i = 0; i < 5; i++)
                {
                    if (frustumPlanes[i].GetDistanceToPoint(center) < -radius)
                    {
                        visible = false;
                        break;
                    }
                }
                if (!visible)
                    continue;

                Nebula component = nebula.GetComponent<Nebula>();
                float baseStepSize = component != null ? component.baseStepSize : 0;
                float stepSize = baseStepSize;
                float nearest = (center - cameraPosition).magnitude - radius;
                if (nearest > 0)
                {
                    float pixels = 2f * radius / nearest * pixelsPerRadian;
                    if (pixels < MinPixels)
                        continue;
                    if (quality > 0 && baseStepSize > 0)
                    {
                        float fullSteps = scale.x / baseStepSize;
                        float steps = Mathf.Clamp(pixels * quality, Mathf.Min(MinSteps, fullSteps), fullSteps);
                        stepSize = scale.x / steps;
                    }
                }
                if (baseStepSize > 0)
                    nebulaMaterialsSingle.rayMarchMaterial.SetFloat("_StepSize", stepSize);

                marchBuffer.SetRenderTarget(volumeDepthRenderTarget);
                marchBuffer.ClearRenderTarget(false, true, Color.clear);
                marchBuffer.DrawRenderer(nebulaMaterialsSingle.meshRenderer, nebulaMaterialsSingle.rayMarchMaterial, 0, 1);
                marchBuffer.SetGlobalTexture("_VolumeDepth", volumeDepth);

                RenderTexture image = downscaled;
                RenderTargetIdentifier imageTarget = downscaledRenderTarget;
                if (keepImages)
                {
                    while (images.Count <= capturedImages.Count)
                        images.Add(CreateTexture(width, height, RenderTextureFormat.ARGB32));
                    image = images[capturedImages.Count];
                    imageTarget = new RenderTargetIdentifier(image);
                    capturedImages.Add(image);
                }

                marchBuffer.SetRenderTarget(imageTarget);
                marchBuffer.ClearRenderTarget(false, true, Color.clear);
                marchBuffer.DrawRenderer(nebulaMaterialsSingle.meshRenderer, nebulaMaterialsSingle.rayMarchMaterial, 0, 0);
                rendererCommandBuffer.SetGlobalTexture("_QuarterResNebula", image);

                rendererCommandBuffer.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);
                rendererCommandBuffer.DrawRenderer(nebulaMaterialsSingle.meshRenderer, nebulaMaterialsSingle.renderMaterial, 0, 0);
                rendererCommandBuffer.DrawRenderer(nebulaMaterialsSingle.meshRenderer, nebulaMaterialsSingle.renderMaterial, 0, 1);
            }

            if (keepImages)
            {
                capturedPosition = cameraPosition;
                capturedRotation = cam.transform.rotation;
                capturedTanVertical = Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
                capturedTanHorizontal = capturedTanVertical * cam.aspect;
                capturedFrame = Time.frameCount;

                bool matricesChanged = SingularityCubemap.instance != null && SingularityCubemap.instance.AppendFaceUpdates(earlyBuffer, this);
                earlyBuffer.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);
                if (matricesChanged)
                    earlyBuffer.SetViewProjectionMatrices(cam.worldToCameraMatrix, cam.projectionMatrix);
                cam.AddCommandBuffer(CameraEvent.BeforeForwardOpaque, earlyBuffer);
            }

            cam.AddCommandBuffer(CameraEvent.BeforeForwardAlpha, rendererCommandBuffer);
        }

        private void OnPreRender()
        {
            ReloadCommandBuffer();
        }

        RenderTexture CreateTexture(int width, int height, RenderTextureFormat format)
        {
            RenderTexture rt = new RenderTexture(width, height, 0, format);
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
