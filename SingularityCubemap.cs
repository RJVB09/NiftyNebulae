using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;

namespace NiftyNebulae
{
    [DefaultExecutionOrder(600)]
    public class SingularityCubemap : MonoBehaviour
    {
        const string LensShaderName = "Singularity/BlackHoleAccretionDisk";
        const string CubemapProperty = "CubeMap";
        const string FadeProperty = "galaxyFadeColor";
        const float MinFade = 0.02f;
        const float ScanInterval = 1f;
        const float MinLensPixels = 2f;

        public static SingularityCubemap instance;

        static readonly Vector2 flipScale = new Vector2(1, -1);
        static readonly Vector2 flipOffset = new Vector2(0, 1);

        List<DownscaleNebulaRenderer.NebulaMaterials> nebulaMaterials;
        readonly List<Renderer> lensRenderers = new List<Renderer>();
        readonly List<Material> lensMaterials = new List<Material>();

        Quaternion[] faceRotations;
        Camera faceCamera;
        DownscaleNebulaRenderer faceRenderer;

        Texture galaxy;
        RenderTexture cubemap;
        RenderTexture source;
        RenderTexture target;
        RenderTexture[] staged;

        FieldInfo galaxyBlockField;
        Material tint;
        bool fadeSupported;
        Color cycleFade = Color.white;
        Color cubemapFade = Color.white;

        bool ready;
        bool failed;
        float nextScan;
        int frame;
        int nextFace;

        public static void Setup(List<DownscaleNebulaRenderer.NebulaMaterials> materials)
        {
            if (instance == null)
            {
                GameObject holder = new GameObject("NiftyNebulae Singularity cubemap");
                DontDestroyOnLoad(holder);
                instance = holder.AddComponent<SingularityCubemap>();
                Main.Log("Singularity found, nebulae will be added to its lensed background.");
            }
            instance.nebulaMaterials = materials;
            if (instance.faceRenderer != null)
                instance.faceRenderer.SetNebulaeMaterials(materials);
            instance.ready = false;
            instance.nextScan = 0;
        }

        void Awake()
        {
            faceRotations = new Quaternion[]
            {
                Quaternion.LookRotation(Vector3.right, Vector3.up),
                Quaternion.LookRotation(Vector3.left, Vector3.up),
                Quaternion.LookRotation(Vector3.up, Vector3.back),
                Quaternion.LookRotation(Vector3.down, Vector3.forward),
                Quaternion.LookRotation(Vector3.forward, Vector3.up),
                Quaternion.LookRotation(Vector3.back, Vector3.up)
            };

            galaxyBlockField = typeof(GalaxyCubeControl).GetField("mpb", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Shader tintShader = Shader.Find("Hidden/Internal-Colored");
            if (tintShader != null)
            {
                tint = new Material(tintShader);
                tint.hideFlags = HideFlags.HideAndDontSave;
                tint.SetInt("_SrcBlend", (int)BlendMode.DstColor);
                tint.SetInt("_DstBlend", (int)BlendMode.Zero);
                tint.SetInt("_Cull", (int)CullMode.Off);
                tint.SetInt("_ZWrite", 0);
                tint.SetInt("_ZTest", (int)CompareFunction.Always);
            }
            fadeSupported = galaxyBlockField != null && tint != null;
            if (!fadeSupported)
                Main.Log("Singularity support: sky dimming can't be matched, lensed nebulae may look darker than they should.", LogType.Warning);
        }

        void LateUpdate()
        {
            if (failed || nebulaMaterials == null || nebulaMaterials.Count == 0)
                return;
            if (ScaledSpace.Instance == null || ScaledCamera.Instance == null || ScaledCamera.Instance.cam == null || Planetarium.fetch == null)
                return;

            try
            {
                Step();
            }
            catch (Exception e)
            {
                failed = true;
                Main.Log("Singularity support stopped after an error: " + e, LogType.Error);
                Restore();
            }
        }

        void Step()
        {
            if (Time.unscaledTime >= nextScan)
            {
                Scan();
                nextScan = Time.unscaledTime + ScanInterval;
            }

            lensMaterials.RemoveAll(material => material == null);
            if (lensMaterials.Count == 0)
                return;

            foreach (Material material in lensMaterials)
            {
                Texture current = material.GetTexture(CubemapProperty);
                if (current != null && current != cubemap && current != galaxy && current.dimension == TextureDimension.Cube)
                {
                    galaxy = current;
                    ready = false;
                }
            }
            if (galaxy == null)
                return;

            if (!CreateTextures())
                return;
            CreateCamera();

            Color fade = GalaxyFade();

            if (!ready)
            {
                cycleFade = fade;
                for (int face = 0; face < 6; face++)
                    RenderFace(face);
                Commit();
                ready = true;
                frame = 0;
                nextFace = 0;
            }
            else if (LensVisible())
            {
                frame++;
                if (frame >= Mathf.Max(1, ConfigLoader.instance.singularityUpdateInterval))
                {
                    frame = 0;
                    if (nextFace == 0)
                        cycleFade = fade;
                    RenderFace(nextFace);
                    nextFace++;
                    if (nextFace == 6)
                    {
                        Commit();
                        nextFace = 0;
                    }
                }
            }

            foreach (Material material in lensMaterials)
            {
                if (material.GetTexture(CubemapProperty) != cubemap)
                    material.SetTexture(CubemapProperty, cubemap);
                if (fadeSupported)
                    material.SetColor(FadeProperty, new Color(fade.r / cubemapFade.r, fade.g / cubemapFade.g, fade.b / cubemapFade.b, 1));
            }
        }

        Color GalaxyFade()
        {
            if (!fadeSupported || GalaxyCubeControl.Instance == null)
                return Color.white;
            MaterialPropertyBlock block = galaxyBlockField.GetValue(GalaxyCubeControl.Instance) as MaterialPropertyBlock;
            if (block == null)
                return Color.white;
            Color color = block.GetColor(PropertyIDs._Color);
            return new Color(Mathf.Max(color.r, MinFade), Mathf.Max(color.g, MinFade), Mathf.Max(color.b, MinFade), 1);
        }

        void Commit()
        {
            for (int face = 0; face < 6; face++)
                Graphics.CopyTexture(staged[face], 0, 0, cubemap, face, 0);
            cubemap.GenerateMips();
            cubemapFade = cycleFade;
        }

        void Scan()
        {
            lensRenderers.Clear();
            foreach (MeshRenderer renderer in ScaledSpace.Instance.GetComponentsInChildren<MeshRenderer>(true))
            {
                Material material = renderer.sharedMaterial;
                if (material == null || material.shader == null || material.shader.name != LensShaderName)
                    continue;
                lensRenderers.Add(renderer);
                if (!lensMaterials.Contains(material))
                    lensMaterials.Add(material);
            }
        }

        bool LensVisible()
        {
            Camera scaled = ScaledCamera.Instance.cam;
            float pixelsPerRadian = Screen.height / (2f * Mathf.Tan(scaled.fieldOfView * 0.5f * Mathf.Deg2Rad));

            foreach (Renderer renderer in lensRenderers)
            {
                if (renderer == null || !renderer.isVisible)
                    continue;
                Bounds bounds = renderer.bounds;
                float radius = bounds.extents.x;
                float distance = (bounds.center - scaled.transform.position).magnitude;
                if (distance <= radius || radius / distance * pixelsPerRadian > MinLensPixels)
                    return true;
            }
            return false;
        }

        bool CreateTextures()
        {
            int sourceSize = galaxy.width;
            int size = Mathf.Min(sourceSize, Mathf.Max(16, ConfigLoader.instance.singularityCubemapResolution));

            if (source != null && source.width == sourceSize && cubemap != null && cubemap.width == size && source.IsCreated() && cubemap.IsCreated() && target.IsCreated() && StagedCreated())
                return true;

            ReleaseTextures();
            ready = false;

            source = new RenderTexture(sourceSize, sourceSize, 0, galaxy.graphicsFormat);
            source.filterMode = FilterMode.Bilinear;

            target = new RenderTexture(size, size, 16, RenderTextureFormat.ARGB32);
            target.filterMode = FilterMode.Bilinear;

            staged = new RenderTexture[6];
            for (int face = 0; face < 6; face++)
                staged[face] = new RenderTexture(size, size, 0, RenderTextureFormat.ARGB32);

            cubemap = new RenderTexture(size, size, 0, RenderTextureFormat.ARGB32);
            cubemap.dimension = TextureDimension.Cube;
            cubemap.useMipMap = true;
            cubemap.autoGenerateMips = false;
            cubemap.filterMode = FilterMode.Trilinear;
            cubemap.wrapMode = TextureWrapMode.Clamp;
            cubemap.name = "NiftyNebulae galaxy cubemap";

            bool created = source.Create() && target.Create() && cubemap.Create();
            for (int face = 0; face < 6; face++)
                created = created && staged[face].Create();
            if (!created)
            {
                failed = true;
                Main.Log("Singularity support is off: the cubemap textures could not be created.", LogType.Error);
                Restore();
                ReleaseTextures();
                return false;
            }

            Main.Log("Singularity cubemap: " + size + "x" + size + " (galaxy cubemap is " + sourceSize + "x" + sourceSize + ", " + galaxy.graphicsFormat + ")");
            return true;
        }

        bool StagedCreated()
        {
            if (staged == null)
                return false;
            foreach (RenderTexture texture in staged)
            {
                if (texture == null || !texture.IsCreated())
                    return false;
            }
            return true;
        }

        void CreateCamera()
        {
            if (faceCamera == null)
            {
                GameObject cameraObject = new GameObject("NiftyNebulae cubemap camera");
                cameraObject.transform.parent = transform;
                faceCamera = cameraObject.AddComponent<Camera>();
                faceCamera.enabled = false;
                faceRenderer = cameraObject.AddComponent<DownscaleNebulaRenderer>();
                faceRenderer.SetNebulaeMaterials(nebulaMaterials);
                int resolution = Mathf.Max(16, ConfigLoader.instance.singularityNebulaResolution);
                faceRenderer.Initialize(resolution, resolution);
            }

            Camera scaled = ScaledCamera.Instance.cam;
            faceCamera.cullingMask = 0;
            faceCamera.clearFlags = CameraClearFlags.Depth;
            faceCamera.renderingPath = RenderingPath.Forward;
            faceCamera.allowHDR = false;
            faceCamera.allowMSAA = false;
            faceCamera.useOcclusionCulling = false;
            faceCamera.depthTextureMode = DepthTextureMode.None;
            faceCamera.orthographic = false;
            faceCamera.fieldOfView = 90;
            faceCamera.aspect = 1;
            faceCamera.nearClipPlane = scaled.nearClipPlane;
            faceCamera.farClipPlane = scaled.farClipPlane;
            faceCamera.targetTexture = target;
        }

        void RenderFace(int face)
        {
            RenderTexture active = RenderTexture.active;

            faceCamera.transform.position = ScaledCamera.Instance.cam.transform.position;
            faceCamera.transform.rotation = (Quaternion)Planetarium.Rotation * faceRotations[face];

            Graphics.CopyTexture(galaxy, face, 0, source, 0, 0);
            if (fadeSupported)
                Multiply(source, cycleFade);
            Graphics.Blit(source, target, flipScale, flipOffset);
            faceCamera.Render();
            Graphics.Blit(target, staged[face], flipScale, flipOffset);

            RenderTexture.active = active;
        }

        void Multiply(RenderTexture texture, Color color)
        {
            Graphics.SetRenderTarget(texture);
            GL.PushMatrix();
            GL.LoadOrtho();
            tint.SetPass(0);
            GL.Begin(GL.QUADS);
            GL.Color(color);
            GL.Vertex3(0, 0, 0);
            GL.Vertex3(0, 1, 0);
            GL.Vertex3(1, 1, 0);
            GL.Vertex3(1, 0, 0);
            GL.End();
            GL.PopMatrix();
        }

        void Restore()
        {
            foreach (Material material in lensMaterials)
            {
                if (material != null && galaxy != null && material.GetTexture(CubemapProperty) == cubemap)
                    material.SetTexture(CubemapProperty, galaxy);
            }
        }

        void ReleaseTextures()
        {
            if (faceCamera != null)
                faceCamera.targetTexture = null;
            List<RenderTexture> textures = new List<RenderTexture> { cubemap, source, target };
            if (staged != null)
                textures.AddRange(staged);
            foreach (RenderTexture texture in textures)
            {
                if (texture != null)
                {
                    texture.Release();
                    Destroy(texture);
                }
            }
            cubemap = null;
            source = null;
            target = null;
            staged = null;
        }

        void OnDestroy()
        {
            Restore();
            ReleaseTextures();
        }
    }
}
