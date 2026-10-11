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
        const float FadeTolerance = 0.03f;
        const float RebaseInterval = 0.25f;
        const float ScanInterval = 1f;
        const float MinLensPixels = 2f;
        const int GridSize = 48;
        const float FillMinSteps = 64f;
        const float FillMaxSteps = 128f;
        const float FillMinPixels = 0.5f;

        public static SingularityCubemap instance;

        static readonly string[] blendShaderNames = { "Sprites/Default", "UI/Default", "KSP/Particles/Alpha Blended", "Legacy Shaders/Particles/Alpha Blended", "Unlit/Transparent" };

        struct FillEntry
        {
            public Mesh mesh;
            public Matrix4x4 matrix;
            public Material material;
            public Vector3 offset;
            public float radius;
            public float distance;
            public float stepSize;
        }

        List<DownscaleNebulaRenderer.NebulaMaterials> nebulaMaterials;
        readonly List<Renderer> lensRenderers = new List<Renderer>();
        readonly List<Material> lensMaterials = new List<Material>();

        Quaternion[] faceRotations;
        readonly bool[] faceHadNebula = new bool[6];
        readonly Mesh[] faceMeshes = new Mesh[6];
        Mesh screenQuad;
        Mesh fillQuad;
        readonly Vector3[] screenCorners = new Vector3[4];
        readonly Vector3[] fillCorners = new Vector3[4];
        readonly Vector2[] fillCoordinates = new Vector2[4];
        readonly Plane[] mainPlanes = new Plane[6];
        readonly List<FillEntry> fillEntries = new List<FillEntry>();
        readonly List<FillEntry> faceFill = new List<FillEntry>();
        readonly List<Vector3> vertices = new List<Vector3>();
        readonly List<Vector2> coordinates = new List<Vector2>();
        readonly List<Color> colors = new List<Color>();
        readonly List<int> triangles = new List<int>();
        readonly bool[] vertexValid = new bool[(GridSize + 1) * (GridSize + 1)];
        readonly Vector2[] vertexPosition = new Vector2[(GridSize + 1) * (GridSize + 1)];

        Material blend;
        Material tint;
        Material mask;
        MaterialPropertyBlock tintBlock;
        MaterialPropertyBlock imageBlock;
        MaterialPropertyBlock stepBlock;
        FieldInfo galaxyBlockField;
        bool fadeSupported;

        Texture galaxy;
        RenderTexture cubemap;
        RenderTexture work;
        RenderTexture fillImage;
        RenderTexture fillDepth;
        int fillSize;
        bool matricesChanged;

        DownscaleNebulaRenderer mainRenderer;
        Color cubemapFade = Color.white;
        float nextRebase;
        float nextScan;
        bool ready;
        bool failed;
        bool lensVisible;

        public static void Setup(List<DownscaleNebulaRenderer.NebulaMaterials> materials)
        {
            if (instance == null)
            {
                GameObject holder = new GameObject("NiftyNebulae Singularity cubemap");
                DontDestroyOnLoad(holder);
                instance = holder.AddComponent<SingularityCubemap>();
            }
            instance.nebulaMaterials = materials;
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

            foreach (string shaderName in blendShaderNames)
            {
                Shader blendShader = Shader.Find(shaderName);
                if (blendShader == null || !blendShader.isSupported)
                    continue;
                blend = new Material(blendShader);
                blend.hideFlags = HideFlags.HideAndDontSave;
                if (blend.HasProperty("_Color"))
                    blend.SetColor("_Color", Color.white);
                if (blend.HasProperty("_TintColor"))
                    blend.SetColor("_TintColor", new Color(0.5f, 0.5f, 0.5f, 0.5f));
                Main.Log("Singularity found, nebulae will be added to its lensed background (" + shaderName + ").");
                break;
            }
            if (blend == null)
            {
                failed = true;
                Main.Log("Singularity support is off: no blend shader was found.", LogType.Error);
                return;
            }

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

                mask = new Material(tintShader);
                mask.hideFlags = HideFlags.HideAndDontSave;
                mask.SetInt("_SrcBlend", (int)BlendMode.Zero);
                mask.SetInt("_DstBlend", (int)BlendMode.One);
                mask.SetInt("_Cull", (int)CullMode.Off);
                mask.SetInt("_ZWrite", 1);
                mask.SetInt("_ZTest", (int)CompareFunction.Always);
            }
            fadeSupported = galaxyBlockField != null && tint != null;
            if (!fadeSupported)
                Main.Log("Singularity support: sky dimming can't be matched, lensed nebulae may look darker than they should.", LogType.Warning);

            tintBlock = new MaterialPropertyBlock();
            imageBlock = new MaterialPropertyBlock();
            stepBlock = new MaterialPropertyBlock();

            screenQuad = new Mesh();
            screenQuad.MarkDynamic();
            screenQuad.vertices = screenCorners;
            screenQuad.colors = new Color[] { Color.white, Color.white, Color.white, Color.white };
            screenQuad.triangles = new int[] { 0, 1, 2, 0, 2, 3 };
            fillQuad = new Mesh();
            fillQuad.MarkDynamic();
            fillQuad.vertices = fillCorners;
            fillQuad.uv = fillCoordinates;
            fillQuad.colors = new Color[] { Color.white, Color.white, Color.white, Color.white };
            fillQuad.triangles = new int[] { 0, 1, 2, 0, 2, 3 };
            for (int face = 0; face < 6; face++)
            {
                faceMeshes[face] = new Mesh();
                faceMeshes[face].MarkDynamic();
            }
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
                Fail("Singularity support stopped after an error: " + e);
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
            {
                lensVisible = false;
                return;
            }

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

            if (mainRenderer == null)
                mainRenderer = ScaledCamera.Instance.cam.GetComponent<DownscaleNebulaRenderer>();
            if (mainRenderer == null)
                return;
            mainRenderer.keepImages = true;

            lensVisible = LensVisible();
            if (ready)
                ApplyToMaterials(GalaxyFade());
        }

        public bool AppendFaceUpdates(CommandBuffer buffer, DownscaleNebulaRenderer main)
        {
            if (failed || main != mainRenderer || galaxy == null || cubemap == null || work == null)
                return false;
            if (ready && !lensVisible)
                return false;

            matricesChanged = false;
            try
            {
                Record(buffer, main);
            }
            catch (Exception e)
            {
                Fail("Singularity support stopped after an error: " + e);
            }
            return matricesChanged;
        }

        void Record(CommandBuffer buffer, DownscaleNebulaRenderer main)
        {
            Camera camera = main.cam;
            Vector3 position = camera.transform.position;
            Quaternion rotation = camera.transform.rotation;
            float tanH = main.capturedTanHorizontal;
            float tanV = main.capturedTanVertical;
            float depth = Mathf.Max(camera.nearClipPlane * 4f, 0.001f);

            screenCorners[0] = position + rotation * new Vector3(-tanH * depth, -tanV * depth, depth);
            screenCorners[1] = position + rotation * new Vector3(-tanH * depth, tanV * depth, depth);
            screenCorners[2] = position + rotation * new Vector3(tanH * depth, tanV * depth, depth);
            screenCorners[3] = position + rotation * new Vector3(tanH * depth, -tanV * depth, depth);
            screenQuad.vertices = screenCorners;

            CollectFill(camera, position);
            Matrix4x4 mainView = camera.worldToCameraMatrix;
            Matrix4x4 mainProjection = camera.projectionMatrix;
            Matrix4x4 fillProjection = Matrix4x4.identity;
            if (fillEntries.Count > 0)
            {
                float spread = fillSize / (fillSize - 2f);
                float low = 0.5f - 0.5f / spread;
                float high = 0.5f + 0.5f / spread;
                float fillDistance = depth * 2f;
                fillCorners[0] = position + rotation * new Vector3(-tanH * fillDistance, -tanV * fillDistance, fillDistance);
                fillCorners[1] = position + rotation * new Vector3(-tanH * fillDistance, tanV * fillDistance, fillDistance);
                fillCorners[2] = position + rotation * new Vector3(tanH * fillDistance, tanV * fillDistance, fillDistance);
                fillCorners[3] = position + rotation * new Vector3(tanH * fillDistance, -tanV * fillDistance, fillDistance);
                fillCoordinates[0] = new Vector2(low, high);
                fillCoordinates[1] = new Vector2(low, low);
                fillCoordinates[2] = new Vector2(high, low);
                fillCoordinates[3] = new Vector2(high, high);
                fillQuad.vertices = fillCorners;
                fillQuad.uv = fillCoordinates;
                fillProjection = Matrix4x4.Perspective(2f * Mathf.Atan(spread) * Mathf.Rad2Deg, 1f, camera.nearClipPlane, camera.farClipPlane);
            }

            Color fade = GalaxyFade();
            bool full = !ready || (fadeSupported && Time.unscaledTime >= nextRebase && FadeDiffers(fade, cubemapFade));
            if (full)
            {
                cubemapFade = fade;
                nextRebase = Time.unscaledTime + RebaseInterval;
            }
            tintBlock.SetColor("_Color", cubemapFade);

            Quaternion planetarium = (Quaternion)Planetarium.Rotation;
            Vector3 cameraRight = rotation * Vector3.right;
            Vector3 cameraUp = rotation * Vector3.up;
            Vector3 cameraForward = rotation * Vector3.forward;
            bool haveImages = main.capturedImages.Count > 0;
            bool any = false;

            for (int face = 0; face < 6; face++)
            {
                Quaternion faceRotation = planetarium * faceRotations[face];
                Vector3 right = faceRotation * Vector3.right;
                Vector3 up = faceRotation * Vector3.up;
                Vector3 forward = faceRotation * Vector3.forward;

                bool hasNebula = haveImages && NebulaInFace(position, right, up, forward) && BuildFaceMesh(face, position, cameraRight, cameraUp, cameraForward, tanH, tanV, depth, right, up, forward);
                faceFill.Clear();
                foreach (FillEntry entry in fillEntries)
                {
                    if (SphereInFace(entry.offset, entry.radius, right, up, forward))
                        faceFill.Add(entry);
                }
                bool hasFill = faceFill.Count > 0;
                if (!full && !hasNebula && !hasFill && !faceHadNebula[face])
                    continue;

                buffer.CopyTexture(galaxy, face, 0, work, 0, 0);
                buffer.SetRenderTarget(work);
                buffer.ClearRenderTarget(true, false, Color.clear);
                if (fadeSupported)
                    buffer.DrawMesh(screenQuad, Matrix4x4.identity, tint, 0, 0, tintBlock);
                if (hasNebula)
                {
                    foreach (RenderTexture image in main.capturedImages)
                    {
                        imageBlock.SetTexture("_MainTex", image);
                        buffer.DrawMesh(faceMeshes[face], Matrix4x4.identity, blend, 0, 0, imageBlock);
                    }
                }
                if (hasFill)
                {
                    if (hasNebula)
                        buffer.DrawMesh(faceMeshes[face], Matrix4x4.identity, mask, 0, 0);

                    Matrix4x4 faceView = Matrix4x4.Scale(new Vector3(1, 1, -1)) * Matrix4x4.TRS(position, faceRotation, Vector3.one).inverse;
                    matricesChanged = true;
                    foreach (FillEntry entry in faceFill)
                    {
                        stepBlock.SetFloat("_StepSize", entry.stepSize);

                        buffer.SetRenderTarget(fillDepth);
                        buffer.SetViewProjectionMatrices(faceView, fillProjection);
                        buffer.ClearRenderTarget(false, true, Color.clear);
                        buffer.DrawMesh(entry.mesh, entry.matrix, entry.material, 0, 1, stepBlock);
                        buffer.SetGlobalTexture("_VolumeDepth", fillDepth);

                        buffer.SetRenderTarget(fillImage);
                        buffer.ClearRenderTarget(false, true, Color.clear);
                        buffer.DrawMesh(entry.mesh, entry.matrix, entry.material, 0, 0, stepBlock);

                        buffer.SetRenderTarget(work);
                        buffer.SetViewProjectionMatrices(mainView, mainProjection);
                        imageBlock.SetTexture("_MainTex", fillImage);
                        buffer.DrawMesh(fillQuad, Matrix4x4.identity, blend, 0, 0, imageBlock);
                    }
                }
                buffer.CopyTexture(work, 0, 0, cubemap, face, 0);

                faceHadNebula[face] = hasNebula || hasFill;
                any = true;
            }

            if (any)
                buffer.GenerateMips(cubemap);
            if (full)
                ready = true;
            ApplyToMaterials(fade);
        }

        void CollectFill(Camera camera, Vector3 position)
        {
            fillEntries.Clear();
            if (mask == null || !CreateFillTextures())
                return;

            GeometryUtility.CalculateFrustumPlanes(camera, mainPlanes);
            float pixelsPerRadian = fillSize * 0.5f;
            float quality = ConfigLoader.instance.adaptiveStepQuality;

            foreach (DownscaleNebulaRenderer.NebulaMaterials entry in nebulaMaterials)
            {
                if (entry.meshRenderer == null || entry.rayMarchMaterial == null)
                    continue;
                Transform nebula = entry.meshRenderer.transform;
                Vector3 scale = nebula.lossyScale;
                Vector3 center = nebula.position;
                float radius = scale.magnitude * 0.5f;

                bool inside = true;
                for (int i = 0; i < 4; i++)
                {
                    if (mainPlanes[i].GetDistanceToPoint(center) < radius)
                    {
                        inside = false;
                        break;
                    }
                }
                if (inside)
                    continue;

                MeshFilter filter = entry.meshRenderer.GetComponent<MeshFilter>();
                Nebula component = nebula.GetComponent<Nebula>();
                if (filter == null || filter.sharedMesh == null || component == null || component.baseStepSize <= 0)
                    continue;

                float fullSteps = scale.x / component.baseStepSize;
                float steps = FillMaxSteps;
                float distance = (center - position).magnitude;
                float nearest = distance - radius;
                if (nearest > 0)
                {
                    float pixels = 2f * radius / nearest * pixelsPerRadian;
                    if (pixels < FillMinPixels)
                        continue;
                    if (quality > 0)
                        steps = Mathf.Clamp(pixels * quality, FillMinSteps, FillMaxSteps);
                }
                steps = Mathf.Min(steps, fullSteps);

                FillEntry fill = new FillEntry();
                fill.mesh = filter.sharedMesh;
                fill.matrix = entry.meshRenderer.localToWorldMatrix;
                fill.material = entry.rayMarchMaterial;
                fill.offset = center - position;
                fill.radius = radius;
                fill.distance = distance;
                fill.stepSize = scale.x / steps;
                fillEntries.Add(fill);
            }

            fillEntries.Sort(FurthestFirst);
        }

        static int FurthestFirst(FillEntry a, FillEntry b)
        {
            return b.distance.CompareTo(a.distance);
        }

        bool NebulaInFace(Vector3 position, Vector3 right, Vector3 up, Vector3 forward)
        {
            foreach (DownscaleNebulaRenderer.NebulaMaterials entry in nebulaMaterials)
            {
                if (entry.meshRenderer == null)
                    continue;
                Transform nebula = entry.meshRenderer.transform;
                if (SphereInFace(nebula.position - position, nebula.lossyScale.magnitude * 0.5f, right, up, forward))
                    return true;
            }
            return false;
        }

        static bool SphereInFace(Vector3 offset, float radius, Vector3 right, Vector3 up, Vector3 forward)
        {
            const float diagonal = 0.70710678f;
            if (Vector3.Dot(offset, forward + right) * diagonal < -radius)
                return false;
            if (Vector3.Dot(offset, forward - right) * diagonal < -radius)
                return false;
            if (Vector3.Dot(offset, forward + up) * diagonal < -radius)
                return false;
            if (Vector3.Dot(offset, forward - up) * diagonal < -radius)
                return false;
            return true;
        }

        bool BuildFaceMesh(int face, Vector3 position, Vector3 cameraRight, Vector3 cameraUp, Vector3 cameraForward, float tanH, float tanV, float depth, Vector3 right, Vector3 up, Vector3 forward)
        {
            vertices.Clear();
            coordinates.Clear();
            colors.Clear();
            triangles.Clear();

            int row = GridSize + 1;
            for (int j = 0; j <= GridSize; j++)
            {
                float t = (float)j / GridSize;
                for (int i = 0; i <= GridSize; i++)
                {
                    float s = (float)i / GridSize;
                    Vector3 direction = (cameraRight * ((2f * s - 1f) * tanH) + cameraUp * ((2f * t - 1f) * tanV) + cameraForward).normalized;
                    float along = Vector3.Dot(direction, forward);
                    int index = j * row + i;
                    bool valid = along > 0.1f;
                    vertexValid[index] = valid;
                    Vector2 projected = Vector2.zero;
                    if (valid)
                        projected = new Vector2(Vector3.Dot(direction, right) / along, -Vector3.Dot(direction, up) / along);
                    vertexPosition[index] = projected;
                    vertices.Add(position + (cameraRight * (projected.x * tanH) + cameraUp * (projected.y * tanV) + cameraForward) * depth);
                    coordinates.Add(new Vector2(s, t));
                    colors.Add(Color.white);
                }
            }

            for (int j = 0; j < GridSize; j++)
            {
                for (int i = 0; i < GridSize; i++)
                {
                    int a = j * row + i;
                    int b = a + 1;
                    int c = a + row;
                    int d = c + 1;
                    if (!vertexValid[a] || !vertexValid[b] || !vertexValid[c] || !vertexValid[d])
                        continue;
                    Vector2 pa = vertexPosition[a];
                    Vector2 pb = vertexPosition[b];
                    Vector2 pc = vertexPosition[c];
                    Vector2 pd = vertexPosition[d];
                    if (pa.x > 1 && pb.x > 1 && pc.x > 1 && pd.x > 1)
                        continue;
                    if (pa.x < -1 && pb.x < -1 && pc.x < -1 && pd.x < -1)
                        continue;
                    if (pa.y > 1 && pb.y > 1 && pc.y > 1 && pd.y > 1)
                        continue;
                    if (pa.y < -1 && pb.y < -1 && pc.y < -1 && pd.y < -1)
                        continue;
                    triangles.Add(a);
                    triangles.Add(c);
                    triangles.Add(d);
                    triangles.Add(a);
                    triangles.Add(d);
                    triangles.Add(b);
                }
            }

            if (triangles.Count == 0)
                return false;

            Mesh mesh = faceMeshes[face];
            mesh.Clear();
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, coordinates);
            mesh.SetColors(colors);
            mesh.SetTriangles(triangles, 0);
            return true;
        }

        void ApplyToMaterials(Color fade)
        {
            if (!ready)
                return;
            Color ratio = new Color(fade.r / cubemapFade.r, fade.g / cubemapFade.g, fade.b / cubemapFade.b, 1);
            foreach (Material material in lensMaterials)
            {
                if (material == null)
                    continue;
                if (material.GetTexture(CubemapProperty) != cubemap)
                    material.SetTexture(CubemapProperty, cubemap);
                if (fadeSupported)
                    material.SetColor(FadeProperty, ratio);
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

        bool FadeDiffers(Color a, Color b)
        {
            return Mathf.Abs(a.r - b.r) > FadeTolerance * b.r || Mathf.Abs(a.g - b.g) > FadeTolerance * b.g || Mathf.Abs(a.b - b.b) > FadeTolerance * b.b;
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
            int size = galaxy.width;
            if (cubemap != null && work != null && cubemap.width == size && cubemap.IsCreated() && work.IsCreated())
                return true;

            ReleaseTextures();
            ready = false;

            work = new RenderTexture(size, size, 24, galaxy.graphicsFormat);
            work.filterMode = FilterMode.Bilinear;

            cubemap = new RenderTexture(size, size, 0, galaxy.graphicsFormat);
            cubemap.dimension = TextureDimension.Cube;
            cubemap.useMipMap = true;
            cubemap.autoGenerateMips = false;
            cubemap.filterMode = FilterMode.Trilinear;
            cubemap.wrapMode = TextureWrapMode.Clamp;
            cubemap.name = "NiftyNebulae galaxy cubemap";

            if (!work.Create() || !cubemap.Create())
            {
                Fail("Singularity support is off: the cubemap textures could not be created.");
                return false;
            }

            Main.Log("Singularity cubemap: " + size + "x" + size + " (" + galaxy.graphicsFormat + ")");
            return true;
        }

        bool CreateFillTextures()
        {
            int size = ConfigLoader.instance.singularityFillResolution;
            if (size <= 0)
                return false;
            size = Mathf.Clamp(size, 32, 1024);
            if (fillImage != null && fillDepth != null && fillSize == size && fillImage.IsCreated() && fillDepth.IsCreated())
                return true;

            ReleaseFillTextures();
            fillSize = size;

            fillImage = new RenderTexture(size, size, 0, RenderTextureFormat.ARGB32);
            fillImage.filterMode = FilterMode.Bilinear;
            fillImage.wrapMode = TextureWrapMode.Clamp;

            fillDepth = new RenderTexture(size, size, 0, RenderTextureFormat.ARGBFloat);
            fillDepth.filterMode = FilterMode.Bilinear;
            fillDepth.wrapMode = TextureWrapMode.Clamp;

            return fillImage.Create() && fillDepth.Create();
        }

        void ReleaseFillTextures()
        {
            RenderTexture[] textures = { fillImage, fillDepth };
            foreach (RenderTexture texture in textures)
            {
                if (texture != null)
                {
                    texture.Release();
                    Destroy(texture);
                }
            }
            fillImage = null;
            fillDepth = null;
        }

        void Fail(string message)
        {
            failed = true;
            Main.Log(message, LogType.Error);
            if (mainRenderer != null)
                mainRenderer.keepImages = false;
            Restore();
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
            RenderTexture[] textures = { cubemap, work };
            foreach (RenderTexture texture in textures)
            {
                if (texture != null)
                {
                    texture.Release();
                    Destroy(texture);
                }
            }
            cubemap = null;
            work = null;
        }

        void OnDestroy()
        {
            Restore();
            ReleaseTextures();
            ReleaseFillTextures();
        }
    }
}
