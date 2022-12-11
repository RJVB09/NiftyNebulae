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
            path = path + ".unity3d";

            var assetBundle = AssetBundle.LoadFromFile(path);
            if (assetBundle == null)
            {
                Main.logError("Asset bundle failed to load. Try reinstalling correctly.");
            }
            else
            {
                Main.log("Asset bundle loaded.");
            }


            Shader[] assetbundleShaders = assetBundle.LoadAllAssets<Shader>();
            Main.log("Loading shaders from asset bundle:");
            foreach (Shader currentShader in assetbundleShaders)
            {
                shaders.Add(currentShader.name, currentShader);
                Main.log("- " + currentShader.name);
            }
            ComputeShader[] assetbundleComputeShaders = assetBundle.LoadAllAssets<ComputeShader>();
            Main.log("Loading compute shaders from asset bundle:");
            foreach (ComputeShader currentComputeShader in assetbundleComputeShaders)
            {
                computeShaders.Add(currentComputeShader.name, currentComputeShader);
                Main.log("- " + currentComputeShader.name);
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

            if (File.Exists(Path.Combine(KSPUtil.ApplicationRootPath, filePath)))
            {
                fileData = File.ReadAllBytes(Path.Combine(KSPUtil.ApplicationRootPath, filePath));
                tex = new Texture2D(2, 2);
                tex.LoadImage(fileData);
                Main.log("Loaded texture: " + Path.Combine(KSPUtil.ApplicationRootPath, filePath));
            }
            else
            {
                Main.logError("The path, " + Path.Combine(KSPUtil.ApplicationRootPath, filePath) + " does not lead to a texture.");
            }
            return tex;
        }
    }
}
