using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class Texture3DLightingBakery : MonoBehaviour
{
    public ComputeShader computeShader;
    Texture2D outputImage;
    public Texture2D inputImage;
    public Texture2D lightingInfo; //r = emission (how much a pixel should be lit or unlit)    //b = transmittance (opacity of the pixel for lighting)
    RenderTexture rt;
    public string outputPath = "/Images";
    public string outputFileName = "texture_lit_";
    public float stepSize = 0.005f;
    public float density = 20f;
    public LightSource[] lightSources;

    int Texture2DSize;
    int Texture3DSize;

    [System.Serializable]
    public struct LightSource
    {
        public LightSource(Vector3 position, Vector4 color, float fallOffPow, float range)
        {
            this.positionX = position.x;
            this.positionY = position.y;
            this.positionZ = position.z;
            this.colorR = color.x;
            this.colorG = color.y;
            this.colorB = color.z;
            this.colorA = color.w;
            this.fallOffPow = fallOffPow; //how quickly the brightness should drop with distance (1-distanceBetween0andRange)^fallOffPow = brightness
            this.range = range; //range of the lightsource, note that 0.5 would take up almost the whole cube
        }

        public float positionX, positionY, positionZ;
        public float colorR, colorG, colorB, colorA;
        public float fallOffPow;
        public float range;
    }

    void Start()
    {
        Texture2DSize = inputImage.width;
        Texture3DSize = (int)Mathf.Pow(inputImage.width,2f/3f);

        outputImage = new Texture2D(Texture2DSize, Texture2DSize, TextureFormat.ARGB32, false);
        if (lightingInfo.width != inputImage.height || lightingInfo.height != inputImage.height)
            Debug.LogError("Lighting map should be the same size as the input image.");
        else
            OnGPU();


        SaveImageOverWrite();
    }

    void OnGPU()
    {

        if (rt != null)
            DestroyImmediate(rt);
        rt = new RenderTexture(inputImage.width,inputImage.height, 0);
        rt.enableRandomWrite = true;
        rt.Create();

        computeShader.SetTexture(0, "Result", rt);
        computeShader.SetTexture(0, "InputTexture", inputImage);
        computeShader.SetTexture(0, "LightingInfo", lightingInfo);

        ComputeBuffer lightSourceBuffer = new ComputeBuffer(lightSources.Length, sizeof(float) * 9);
        lightSourceBuffer.SetData(lightSources);

        computeShader.SetBuffer(0, "LightSourcesBuffer", lightSourceBuffer);
        computeShader.SetInt("LightSourcesBuffer_Length", lightSources.Length);
        computeShader.SetInt("_Texture2DSize", Texture2DSize);
        computeShader.SetInt("_Texture3DSize", Texture3DSize);
        computeShader.SetFloat("_StepSize", stepSize);
        computeShader.SetFloat("_Density", density);
        Debug.Log("t2d: " + Texture2DSize);
        Debug.Log("t3d: " + Texture3DSize);
        computeShader.Dispatch(0, Mathf.CeilToInt(Texture2DSize / 32f), Mathf.CeilToInt(Texture2DSize / 32f), 1);


        lightSourceBuffer.Dispose();
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
