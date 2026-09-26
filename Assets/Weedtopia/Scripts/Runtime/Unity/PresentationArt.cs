using System;
using System.Collections.Generic;
using UnityEngine;

namespace Weedtopia.Unity
{
    [Serializable] public sealed class NamedSprite { public string id; public Sprite sprite; }
    /// <summary>Authored art references. No Resources lookup or generated fallback.</summary>
    public sealed class PresentationArt : ScriptableObject
    {
        public NamedSprite[] sprites;
        public Font bodyFont, headingFont;
        private Dictionary<string, Sprite> index;
        private void OnEnable(){index=null;}
        public void ClearCache(){index=null;}
        public static string IconForRole(string role)
        {
            switch(role)
            {
                case "grow":return "raw";case "processing":return "refined";case "farm_manager":return "cargo";case "farm_driver":return "farm";
                case "city_manager":return "city";case "city_driver":return "truck";case "coffee":return "money";case "caretaker":return "settlement";
                case "ranger":return "laurel";case "butter":return "butter";case "market":return "grinder";case "baker":return "pastry";
                default:throw new InvalidOperationException("Missing role icon: "+role);
            }
        }
        public Sprite Get(string id)
        {
            if(index==null)
            {
                index=new Dictionary<string, Sprite>(StringComparer.Ordinal);
                foreach(var entry in sprites) index.Add(entry.id,entry.sprite);
            }
            if(!index.TryGetValue(id,out var sprite)||sprite==null) throw new InvalidOperationException("Missing presentation sprite: "+id);
            return sprite;
        }
        public static string CharacterForRole(string role)
        {
            switch(role)
            {
                case "grow": return "mila";
                case "processing": return "jaro";
                case "farm_manager": case "city_manager": return "nia";
                case "farm_driver": case "city_driver": return "driver";
                case "coffee": case "market": return "city_host";
                case "caretaker": case "ranger": return "gardener";
                case "butter": case "baker": return "baker";
                default: throw new InvalidOperationException("Unmapped worker portrait role: "+role);
            }
        }
    }
}
