using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class Texture3DCompute : MonoBehaviour
{
    public ComputeShader computeShader;
    Texture2D outputImage;
    RenderTexture rt;
    public string outputPath = "/Images";
    public string outputFileName = "texture_";
    public int size = 3; //resolution of the 2d texture in 2^3n and resolution of the 3d result in 2^2n 

    int Texture2DSize;
    int Texture3DSize;

    void Start()
    {
        Texture2DSize = (int)Mathf.Pow(2f, 3f * (float)size);
        Texture3DSize = (int)Mathf.Pow(2f, 2f * (float)size);

        outputImage = new Texture2D(Texture2DSize, Texture2DSize, TextureFormat.ARGB32, false);
        OnGPU();
        SaveImageOverWrite();
    }

    void OnGPU()
    {

        if (rt != null)
            DestroyImmediate(rt);
        rt = new RenderTexture(Texture2DSize, Texture2DSize, 0);
        rt.enableRandomWrite = true;
        rt.Create();

        computeShader.SetTexture(0, "Result", rt);
        computeShader.SetInt("_Texture2DSize", Texture2DSize);
        computeShader.SetInt("_Texture3DSize", Texture3DSize);
        computeShader.Dispatch(0, Mathf.CeilToInt(Texture2DSize / 32f), Mathf.CeilToInt(Texture2DSize / 32f), 1);

    }

    void SaveImage()
    {
        var old_rt = RenderTexture.active;
        RenderTexture.active = rt;

        outputImage.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        outputImage.Apply();
        outputImage.alphaIsTransparency = true;
        outputImage.wrapMode = TextureWrapMode.Clamp;
        outputImage.filterMode = FilterMode.Point;

        RenderTexture.active = old_rt;

        byte[] pngEncoded = outputImage.EncodeToPNG();
        if (!Directory.Exists(Application.dataPath + outputPath + "/"))
            Directory.CreateDirectory(Application.dataPath + outputPath + "/");

        int imageIndex = 0;

        while (File.Exists(Application.dataPath + outputPath + "/" + outputFileName + imageIndex + ".png"))
            imageIndex++;

        File.WriteAllBytes(Application.dataPath + outputPath + "/" + outputFileName + imageIndex + ".png", pngEncoded);
    }

    void SaveImageOverWrite()
    {
        var old_rt = RenderTexture.active;
        RenderTexture.active = rt;

        outputImage.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        outputImage.Apply();
        outputImage.alphaIsTransparency = true;
        outputImage.wrapMode = TextureWrapMode.Clamp;
        outputImage.filterMode = FilterMode.Point;

        RenderTexture.active = old_rt;

        byte[] pngEncoded = outputImage.EncodeToPNG();
        if (!Directory.Exists(Application.dataPath + outputPath + "/"))
            Directory.CreateDirectory(Application.dataPath + outputPath + "/");

        File.WriteAllBytes(Application.dataPath + outputPath + "/" + outputFileName + ".png", pngEncoded);
    }
}
