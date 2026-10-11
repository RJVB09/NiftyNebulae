using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace NiftyNebulae
{
    [KSPAddon(KSPAddon.Startup.Instantly, true)]
    public class AssetLoader : MonoBehaviour
    {
        public static Dictionary<string, Texture2D> texture2Ds = new Dictionary<string, Texture2D>();
        public static Dictionary<string, Texture3D> texture3Ds = new Dictionary<string, Texture3D>();

        public static Dictionary<string, Shader> shaders = new Dictionary<string, Shader>();
        public static Dictionary<string, ComputeShader> computeShaders = new Dictionary<string, ComputeShader>();

        public void Awake()
        {
            string path = Path.Combine(KSPUtil.ApplicationRootPath, "GameData/NiftyNebulae/shaders/shaders-win");
            path += ".unity3d";

            var assetBundle = AssetBundle.LoadFromFile(path);
            if (assetBundle == null)
                Main.Log("Asset bundle failed to load. Try reinstalling correctly.", LogType.Error);
            else
                Main.Log("Asset bundle loaded.");

            Main.Log("The assetbundle contains the following: ");
            UnityEngine.Object[] allAssets = assetBundle.LoadAllAssets();
            foreach (UnityEngine.Object asset in allAssets)
            {
                Main.Log("- " + asset.name);
            }


            Shader[] assetbundleShaders = assetBundle.LoadAllAssets<Shader>();
            Main.Log("Loading shaders from asset bundle:");
            foreach (Shader currentShader in assetbundleShaders)
            {
                shaders.Add(currentShader.name, currentShader);
                Main.Log("- " + currentShader.name);
            }
            
            //ComputeShader[] assetbundleComputeShaders = assetBundle.LoadAllAssets<ComputeShader>();
            //Main.Log("Loading compute shaders from asset bundle:");
            //foreach (ComputeShader currentComputeShader in assetbundleComputeShaders)
            //{
            //    computeShaders.Add(currentComputeShader.name, currentComputeShader);
            //    Main.Log("- " + currentComputeShader.name);
            //}
        }

        public static Shader GetShader(string name)
        {
            return shaders[name];
        }

        public static Texture2D LoadPNG(string filePath) //From Rootpath
        {
            Texture2D tex;
            if (texture2Ds.TryGetValue(filePath, out tex))
                return tex;
            else
            {
                byte[] fileData;

                string fullPath = Path.Combine(KSPUtil.ApplicationRootPath, filePath);
                if (File.Exists(fullPath))
                {
                    fileData = File.ReadAllBytes(fullPath);
                    tex = new Texture2D(2, 2);
                    tex.LoadImage(fileData);
                    texture2Ds.Add(filePath, tex);
                    Main.Log("Loaded texture: " + fullPath);
                }
                else
                    Main.Log("The path, " + fullPath + " does not lead to a texture.", LogType.Error);
                return tex;
            }
        }

        public static Texture3D LoadTexture3D(string filePath, int tileSize)
        {
            return LoadTexture3D(filePath, tileSize, Vector3.zero);
        }

        public static Texture3D LoadTexture3D(string filePath, int tileSize, Vector3 rotation)
        {
            string key = filePath;
            if (rotation != Vector3.zero)
                key = filePath + "|" + rotation.x + "," + rotation.y + "," + rotation.z;

            Texture3D tex;
            if (texture3Ds.TryGetValue(key, out tex))
                return tex;

            string texturePath = FindTexture(filePath);
            if (texturePath == null)
            {
                Main.Log("The path, " + Path.Combine(KSPUtil.ApplicationRootPath, filePath) + " does not lead to a texture.", LogType.Error);
                return null;
            }

            string fullPath = Path.Combine(KSPUtil.ApplicationRootPath, texturePath);
            try
            {
                if (IsDDS(fullPath))
                    tex = LoadDDSAs3D(fullPath, tileSize, rotation);
                else
                    tex = Texture3DManager.Convert2DTo3D(LoadPNG(texturePath), tileSize, rotation);
            }
            catch (Exception e)
            {
                Main.Log("Couldn't make a 3D texture from " + fullPath + ": " + e.Message, LogType.Error);
                return null;
            }

            Main.Log("Made 3D texture for: " + fullPath);
            texture3Ds.Add(key, tex);
            return tex;
        }

        static string FindTexture(string filePath)
        {
            if (File.Exists(Path.Combine(KSPUtil.ApplicationRootPath, filePath)))
                return filePath;

            foreach (string extension in new string[] { ".dds", ".png" })
            {
                string candidate = Path.ChangeExtension(filePath, extension);
                if (candidate != filePath && File.Exists(Path.Combine(KSPUtil.ApplicationRootPath, candidate)))
                {
                    Main.Log(filePath + " was not found, using " + candidate + " instead.", LogType.Warning);
                    return candidate;
                }
            }

            return null;
        }

        static bool IsDDS(string fullPath)
        {
            byte[] magic = new byte[4];
            using (FileStream stream = File.OpenRead(fullPath))
            {
                if (stream.Read(magic, 0, 4) < 4)
                    return false;
            }
            return DDSLoader.IsDDS(magic);
        }

        static Texture3D LoadDDSAs3D(string fullPath, int tileSize, Vector3 rotation)
        {
            DDSTexture dds = DDSLoader.Load(File.ReadAllBytes(fullPath));
            Main.Log("Loaded texture: " + fullPath + " (" + dds.format + ", " + dds.width + "x" + dds.height + (dds.depth > 1 ? "x" + dds.depth : "") + ")");
            if (!dds.hasAlpha)
                Main.Log(fullPath + " is saved as " + dds.format + ", which has no alpha channel. Nebula density is read from alpha, so this nebula will fill its whole cube. Save it as DXT5 or another format with alpha.", LogType.Warning);

            if (dds.depth > 1)
                return Texture3DManager.Create(Texture3DManager.Rotate(dds.pixels, dds.width, dds.height, dds.depth, rotation), dds.width, dds.height, dds.depth);
            return Texture3DManager.Convert2DTo3D(dds.pixels, dds.width, dds.height, tileSize, rotation);
        }
    }
}
