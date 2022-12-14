using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace NiftyNebulae
{
    [DefaultExecutionOrder(545)] //Before the scaledSpace camera
    public class UniverseSpace : MonoBehaviour
    {
        public const float scaleFactor = 6000;
        public CubeCamera cubeCamera;
        public Camera scaledCamera;
        public Skybox scaledCameraSkybox;

        void Awake()
        {
            cubeCamera = new GameObject("CubeCamera").AddComponent<CubeCamera>();
            cubeCamera.gameObject.transform.SetParent(transform);
            scaledCameraSkybox = scaledCamera.gameObject.AddComponent<Skybox>();
        }

        void Update()
        {
            cubeCamera.RenderCameras();
            scaledCameraSkybox.material = cubeCamera.skyBox;
        }
    }
}
