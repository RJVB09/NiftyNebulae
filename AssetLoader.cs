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


            Shader[] assetbundleShaders = assetBundle.LoadAllAssets<Shader>();
            Main.Log("Loading shaders from asset bundle:");
            foreach (Shader currentShader in assetbundleShaders)
            {
                shaders.Add(currentShader.name, currentShader);
                Main.Log("- " + currentShader.name);
            }
            ComputeShader[] assetbundleComputeShaders = assetBundle.LoadAllAssets<ComputeShader>();
            Main.Log("Loading compute shaders from asset bundle:");
            foreach (ComputeShader currentComputeShader in assetbundleComputeShaders)
            {
                computeShaders.Add(currentComputeShader.name, currentComputeShader);
                Main.Log("- " + currentComputeShader.name);
            }
        }

        public static Shader GetShader(string name)
        {
            return shaders[name];
        }

        public static Texture2D LoadPNG(string filePath) //From Rootpath
        {
            Texture2D tex = null;
            byte[] fileData;

            string fullPath = Path.Combine(KSPUtil.ApplicationRootPath, filePath);
            if (File.Exists(fullPath))
            {
                fileData = File.ReadAllBytes(fullPath);
                tex = new Texture2D(2, 2);
                tex.LoadImage(fileData);
                Main.Log("Loaded texture: " + fullPath);
            }
            else
                Main.Log("The path, " + fullPath + " does not lead to a texture.", LogType.Error);
            return tex;
        }
    }
}
