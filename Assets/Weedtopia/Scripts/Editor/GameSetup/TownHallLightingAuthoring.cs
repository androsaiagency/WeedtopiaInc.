using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Weedtopia.Editor.BuildingArtPipeline;
using Weedtopia.Unity;
using Object = UnityEngine.Object;

namespace Weedtopia.Editor.GameSetup
{
    /// <summary>Local material fill for the town hall in the painted city; no scene lighting changes.</summary>
    public static class TownHallLightingAuthoring
    {
        public const string ReportFolder="Docs/ArtProduction/Buildings/town_hall/T_FIXED/Meshy_Highpoly_2026-09-28/CityLighting";
        public const float Fill=.65f;
        private static readonly int[] Targets={50000,20000,12000};
        private static string MaterialFolder=>Path.GetDirectoryName(TownHall3DIntegration.ModelPath).Replace('\\','/')+"/Materials";

        private static void ConfigureFill(Material material,float fill)
        {
            // Texture-coloured fill restores painted brightness while the Lit pass retains normals,
            // highlights and directional shading. URP 17 requires an emissive GI flag to keep
            // the keyword during import. These non-static renderers do not contribute baked GI.
            material.SetTexture("_EmissionMap",material.GetTexture("_BaseMap"));
            material.SetColor("_EmissionColor",new Color(fill,fill,fill,1));
            material.EnableKeyword("_EMISSION");
            material.globalIlluminationFlags=MaterialGlobalIlluminationFlags.BakedEmissive;
        }

        [MenuItem("Weedtopia/Art Pipeline/3D Town Hall/Apply City Brightness")]
        public static void Apply()
        {
            BurningHillsLayoutInstaller.RequireSafeEditor();
            BuildingIntakeService.EnsureAssetFolder(MaterialFolder);
            var prefab=PrefabUtility.LoadPrefabContents(TownHall3DIntegration.ModelPath);
            try
            {
                var levels=prefab.GetComponent<LODGroup>().GetLODs();
                if(levels.Length!=Targets.Length)throw new InvalidDataException("Expected three approved LODs.");
                for(int i=0;i<Targets.Length;i++)
                {
                    string path=MaterialFolder+"/TownHall_"+Targets[i]+"_City.mat";
                    var material=AssetDatabase.LoadAssetAtPath<Material>(path);
                    if(material==null)
                    {
                        var source=AssetDatabase.LoadAssetAtPath<Material>(TownHall3DReview.Root+"/Materials/TownHall_"+Targets[i]+"_2K.mat");
                        material=new Material(source){name="TownHall_"+Targets[i]+"_City"};
                        AssetDatabase.CreateAsset(material,path);
                    }
                    ConfigureFill(material,Fill);EditorUtility.SetDirty(material);
                    foreach(var renderer in levels[i].renderers)
                    {
                        if((GameObjectUtility.GetStaticEditorFlags(renderer.gameObject)&StaticEditorFlags.ContributeGI)!=0)
                            throw new InvalidOperationException("City fill is for non-GI-contributing renderers only.");
                        renderer.sharedMaterial=material;
                        PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
                    }
                    foreach(var property in new[]{"_BaseMap","_BumpMap","_MetallicGlossMap"})
                    {
                        var texture=material.GetTexture(property);
                        if(texture==null||texture.width!=2048||texture.height!=2048)throw new InvalidDataException("Missing approved 2K texture.");
                    }
                }
                PrefabUtility.SaveAsPrefabAsset(prefab,TownHall3DIntegration.ModelPath);
            }
            finally{PrefabUtility.UnloadPrefabContents(prefab);}
            AssetDatabase.SaveAssets();Directory.CreateDirectory(ReportFolder);
            File.WriteAllText(ReportFolder+"/material_receipt.json",JsonUtility.ToJson(new Receipt{utc=DateTime.UtcNow.ToString("O"),fill=Fill,materials=Targets.Length},true));
        }
        [Serializable] private sealed class Receipt
        {
            public string utc;
            public float fill;
            public int materials;
            public bool sceneLightingChanged=false,textureFilesChanged=false;
        }

        public static void PrepareEvidence(GameRoot root,Action<string,Action> shot,bool compare)
        {
            var site=root.Site("town_hall");var lod=site.GetComponentInChildren<LODGroup>();
            var life=Object.FindFirstObjectByType<LivingTownPresentation>();
            if(life!=null){life.diagnosticFreeze=true;life.PresentAt(12);}
            void View(float zoom)
            {
                root.Ui.Close();root.Ui.Interaction.CloseBuilding();root.CameraRig.ShowOverview();
                root.CameraRig.SetDiagnosticZoom(zoom);root.CameraRig.Focus(site.transform.position);
            }
            if(compare)
            {
                // Owned transient copies ensure comparison strengths never modify serialized assets.
                var materials=lod.GetLODs().SelectMany(l=>l.renderers).Select(r=>r.material).ToArray();
                foreach(float strength in new[]{0f,.4f,.65f,.9f})
                {
                    string label=Mathf.RoundToInt(strength*100).ToString("D2");
                    shot("fill_"+label+"_context",()=>{foreach(var material in materials)ConfigureFill(material,strength);View(48);});
                    shot("fill_"+label+"_close",()=>View(22));
                }
            }
            else
            {
                foreach(var renderer in lod.GetLODs().SelectMany(l=>l.renderers))
                {
                    var material=renderer.sharedMaterial;
                    if(!material.IsKeywordEnabled("_EMISSION")||Mathf.Abs(material.GetColor("_EmissionColor").r-Fill)>.001f)
                        throw new InvalidDataException("Saved city fill did not survive material import: "+material.name);
                }
                shot("01_city_context",()=>View(48));
                shot("02_town_hall_close",()=>View(22));
                shot("03_selected",()=>root.Ui.Interaction.OpenBuilding("town_hall"));
                shot("04_lod1",()=>{View(22);lod.ForceLOD(1);});
                shot("05_lod2",()=>lod.ForceLOD(2));
                shot("06_auto_lod",()=>{lod.ForceLOD(-1);View(32);});
            }
        }
    }
}
