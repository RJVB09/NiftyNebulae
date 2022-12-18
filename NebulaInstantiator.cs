using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static NiftyNebulae.Main;

namespace NiftyNebulae
{
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class NebulaInstantiator : MonoBehaviour
    {
        public List<NebulaCFG> nebulaCFGs;
        public List<Nebula> nebulaObjects;

        public static NebulaInstantiator instance;

        void Awake()
        {
            instance = this;
        }

        public Nebula Find(Predicate<Nebula> match)
        {
            foreach (Nebula nebula in nebulaObjects)
            {
                if (match(nebula))
                {
                    return nebula;
                }
            }
            Log("No nebula found with Find(), returning empty nebula class.",LogType.Warning);
            return new Nebula();
        }

        public void InstantiateAllNebulae()
        {
            foreach (NebulaCFG nebulaCFG in nebulaCFGs)
            {
                GameObject currentGameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Nebula currentNebula = currentGameObject.AddComponent<Nebula>();
                CelestialBody parentBody;
                try
                {
                    parentBody = FlightGlobals.Bodies.Find(a => a.name == nebulaCFG.parentName);
                }
                catch (ArgumentNullException e)
                {
                    if (nebulaCFG.name == "")
                        Log("Couldn't find parentBody for nebula, check your nebula config. Skipping node." + e.Message, LogType.Exception);
                    else
                        Log("Couldn't find parentBody for nebula named " + nebulaCFG.name + ", check your nebula config. Skipping node." + e.Message, LogType.Exception);

                    continue;
                }

                currentGameObject.name = nebulaCFG.name;
                currentGameObject.transform.SetParent(parentBody.scaledBody.transform,true);
                currentGameObject.transform.localPosition = Vector3.zero;
                currentGameObject.transform.localScale = Vector3.one * 2 * 1000 * nebulaCFG.nebulaRadius; //Planet radius in scaled local space is always 1000 units
                currentGameObject.layer = 9;//Atmosphere layer, makes it transparent to scatterer sunflares // parentBody.scaledBody.layer;
                currentNebula.settings = nebulaCFG;
                currentNebula.parentBody = parentBody;


                nebulaObjects.Add(currentNebula);
            }
        }
    }
}
