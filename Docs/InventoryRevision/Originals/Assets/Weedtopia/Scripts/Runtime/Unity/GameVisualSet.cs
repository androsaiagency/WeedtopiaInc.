using System;
using UnityEngine;
using Weedtopia.Buildings.Presentation;

namespace Weedtopia.Unity
{
    [Serializable] public sealed class DevelopmentVisual { public string family; public GameObject ruin, active; }
    [CreateAssetMenu(menuName="Weedtopia/Game/Development Visuals")]
    public sealed class GameVisualSet : ScriptableObject, IBuildingVisualProvider
    {
        public DevelopmentVisual[] visuals;
        public bool TryGetVisual(string buildingId,int level,out GameObject prefab)
        {
            prefab=null;if(level<0||level>5000)return false;
            foreach(var v in visuals)if(v.family==buildingId){prefab=level==0?v.ruin:v.active;return prefab!=null;}
            return false;
        }
    }
}
