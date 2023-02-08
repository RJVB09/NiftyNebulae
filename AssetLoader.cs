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

        public static Texture3D LoadPNGAs3D(string filePath, int tileSize)
        {
            Texture3D tex;
            Texture2D tex2D;
            if (texture3Ds.TryGetValue(filePath, out tex))
                return tex;
            else
            {
                tex2D = LoadPNG(filePath);
                tex = Texture3DManager.Convert2DTo3D(tex2D, tileSize);
                Main.Log("Made 3D texture for: " + Path.Combine(KSPUtil.ApplicationRootPath, filePath));
                texture3Ds.Add(filePath, tex);
                return tex;
            }
        }
    }
}
