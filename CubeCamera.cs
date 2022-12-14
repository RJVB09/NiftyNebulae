using UnityEngine;

namespace NiftyNebulae
{
    public class CubeCamera : MonoBehaviour
    {
        public RenderTexture[] cubeMap = new RenderTexture[6]; //X+,Y+,Z+,X-,Y-,Z-
        public Material skyBox;
        public Camera[] cubeCameras = new Camera[6]; //X+,Y+,Z+,X-,Y-,Z-
        public int skyBoxResolution = 512;
        public string[] names = new string[6] 
        { 
            "Xpos", 
            "Ypos", 
            "Zpos", 
            "Xneg", 
            "Yneg", 
            "Zneg" 
        };
        public string[] shaderInputs = new string[6]
        {
            "_LeftTex",
            "_UpTex",
            "_FrontTex",
            "_RightTex",
            "_DownTex",
            "_BackTex"
        };
        public Quaternion[] orientations = new Quaternion[6] 
        { 
            Quaternion.Euler(0,90,0),
            Quaternion.Euler(-90,0,0),
            Quaternion.Euler(0,0,0),
            Quaternion.Euler(0,-90,0),
            Quaternion.Euler(90,0,0),
            Quaternion.Euler(0,180,0)
        };

        void Awake()
        {
            skyBox.shader = Shader.Find("Skybox/6 Sided");
            for (int i = 0; i < 6; i++)
            {
                cubeCameras[i] = new GameObject(names[i]).AddComponent<Camera>();
                cubeCameras[i].transform.SetParent(transform);
                cubeCameras[i].transform.position = Vector3.zero;
                cubeCameras[i].transform.rotation = orientations[i];

                cubeMap[i] = new RenderTexture(skyBoxResolution, skyBoxResolution, 1);
                cubeCameras[i].targetTexture = cubeMap[i];
            }
        }

        public void RenderCameras()
        {
            for (int i = 0; i < 6; i++)
            {
                cubeCameras[i].Render();
                skyBox.SetTexture(shaderInputs[i], cubeMap[i]);
            }
        }
    }
}