
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

using KSP.UI.Screens;

#if false

namespace KerbalLaunchFailure
{
    [KSPAddon(KSPAddon.Startup.AllGameScenes, true)]
    class ShipCostModule : MonoBehaviour
    {
        public RDTechTree rdTechTree = new RDTechTree();
        void Start()
        {
            Debug.Log("ShipCostModule.Start");
                       
        RDTechTree.OnTechTreeSpawn.Add(new EventData<RDTechTree>.OnEvent(onTechTreeSpawn));
            RDTechTree.OnTechTreeDespawn.Add(new EventData<RDTechTree>.OnEvent(onTechTreeDespawn));
            
        }


    private void onTechTreeSpawn(RDTechTree rdTechTree)
    {
        Debug.Log("onTechTreeSpawn");
    }
    private void onTechTreeDespawn(RDTechTree rdTechTree)
    {
        Debug.Log("onTechTreeDespawn");
    }
}
}
#endif