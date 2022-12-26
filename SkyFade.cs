using System;
using UnityEngine;

namespace NiftyNebulae
{
    [KSPAddon(KSPAddon.Startup.FlightAndKSC, false)]
    public class SkyFade : MonoBehaviour
    {
        //Just nicked the GalaxyCubeControl.cs code and changed some stuff.

        public float min = 0;
        public float max = 1;
        public float atmosFadeLimit = 0.6f;
        public float glareFadeLimit = 0.6f;
        public float daytimeFadeLimit = 0.9f;
        public float airPressureFade = 1.1f;
        public float glareFadeLerpRate = 0.05f;
        public Sun sunRef;
        public Camera tgt;
        private double sunSrfAngle;
        private double sunCamAngle;
        private bool lineOfSightToSun;
        private float atmosFade;
        private float dayTimeFade;
        private float glareFade;
        public float totalFade;
        private int layerMask;
        RaycastHit lineOfSightToSunHit;
        public static SkyFade Instance;

        void Awake() => Instance = this;
        

        private void Start()
        {
            sunRef = GalaxyCubeControl.Instance.sunRef;
            tgt = GalaxyCubeControl.Instance.tgt;

            atmosFadeLimit = GalaxyCubeControl.Instance.atmosFadeLimit;
            glareFadeLimit = GalaxyCubeControl.Instance.glareFadeLimit;
            daytimeFadeLimit = GalaxyCubeControl.Instance.daytimeFadeLimit;
            airPressureFade = GalaxyCubeControl.Instance.airPressureFade;
            glareFadeLerpRate = GalaxyCubeControl.Instance.glareFadeLerpRate;
            min = GalaxyCubeControl.Instance.minGalaxyColor.r;
            max = GalaxyCubeControl.Instance.maxGalaxyColor.r;
            layerMask = 1 << LayerMask.NameToLayer("Scaled Scenery");
        }

        private void Update()
        {
            atmosFadeLimit = GalaxyCubeControl.Instance.atmosFadeLimit;
            glareFadeLimit = GalaxyCubeControl.Instance.glareFadeLimit;
            daytimeFadeLimit = GalaxyCubeControl.Instance.daytimeFadeLimit;
            airPressureFade = GalaxyCubeControl.Instance.airPressureFade;
            glareFadeLerpRate = GalaxyCubeControl.Instance.glareFadeLerpRate;
            min = GalaxyCubeControl.Instance.minGalaxyColor.r;
            max = GalaxyCubeControl.Instance.maxGalaxyColor.r;

            lineOfSightToSun = LineOfSightToSun();
            if (lineOfSightToSun)
            {
                sunCamAngle = Math.Acos(Mathf.Clamp((float)Vector3d.Dot(-sunRef.sunDirection, (Vector3d)tgt.transform.forward),-1f,1f)) * (180.0 / Math.PI);
                double num = tgt.fieldOfView * 0.5;
                float t;
                t = Mathf.Clamp01((float)((sunCamAngle + 10.0 - num) * 0.05));
                glareFade = Mathf.Lerp(glareFade, Mathf.Lerp(glareFadeLimit, 0.0f, t), glareFadeLerpRate);
            }
            else
            {
                sunCamAngle = 0.0;
                glareFade = Mathf.Lerp(glareFade, 0.0f, glareFadeLerpRate);
            }
            Vector3d localSpace = ScaledSpace.ScaledToLocalSpace(tgt.transform.position);
            CelestialBody mainBody = FlightGlobals.getMainBody(localSpace);
            double num1 = FlightGlobals.getStaticPressure(mainBody.GetAltitude(localSpace), mainBody) * 0.0098692326671601278;
            if (num1 > 0.0)
            {
                sunSrfAngle = Math.Acos(Vector3d.Dot(-sunRef.sunDirection, FlightGlobals.getUpAxis(localSpace))) * Mathf.Rad2Deg;
                sunSrfAngle = UtilMath.Clamp01((sunSrfAngle - 75.0) * 0.025);
                dayTimeFade = (float)num1 * Mathf.Lerp(daytimeFadeLimit, 0.0f, (float)sunSrfAngle);
                atmosFade = Mathf.Lerp(0.0f, atmosFadeLimit, (float)num1 * airPressureFade);
            }
            else
            {
                dayTimeFade = 0.0f;
                atmosFade = 0.0f;
            }
            totalFade = atmosFade + dayTimeFade + glareFade;  //Somehow glareFade goes NaN when the sun is focused FIXED
            //Main.Log("TotalFade: " + totalFade + ", sunRef.sunDirection: " + sunRef.sunDirection + ", sunCamAngle: " + sunCamAngle + ", glareFade: " + glareFade);
        }

        private bool LineOfSightToSun()
        {
            Vector3 direction = ScaledSun.Instance.transform.position - ScaledCamera.Instance.transform.position;
            return Physics.Raycast(new Ray(ScaledCamera.Instance.transform.position, direction), out lineOfSightToSunHit, direction.magnitude, layerMask);
        }
    }
}
