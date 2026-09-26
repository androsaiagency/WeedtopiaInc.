using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Weedtopia.Unity;
using Weedtopia.Editor.BuildingArtPipeline;

namespace Weedtopia.Editor.GameSetup
{
    public static class ProductionArtImporter
    {
        public const string Root="Assets/Weedtopia/Production";
        public const string ArtPath=Root+"/PresentationArt.asset";
        [Serializable] private sealed class Slices { public Slice[] entries; }
        [Serializable] private sealed class Slice {public string file,name;public int x,y,width,height,border,ppu;public float pivotX,pivotY;}
        public static PresentationArt Import(bool refresh=false)
        {
            foreach(string folder in new[]{Root,Root+"/Prefabs",Root+"/Meshes",Root+"/Materials",Root+"/Fonts"})BuildingIntakeService.EnsureAssetFolder(folder);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var existing=AssetDatabase.LoadAssetAtPath<PresentationArt>(ArtPath);if(existing!=null&&!refresh)return existing;
            var slices=JsonUtility.FromJson<Slices>(File.ReadAllText(Root+"/art_slices.json"));
            foreach(var group in slices.entries.GroupBy(x=>x.file))
            {
                var importer=(TextureImporter)AssetImporter.GetAtPath(Root+"/Art/"+group.Key);
                importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Multiple;
                importer.alphaIsTransparency=true;importer.mipmapEnabled=group.Key=="Props.png"||group.Key=="Characters.png";
                importer.filterMode=FilterMode.Bilinear;importer.wrapMode=TextureWrapMode.Clamp;importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.CompressedHQ;importer.isReadable=false;
#pragma warning disable 618
                importer.spritesheet=group.Select(x=>new SpriteMetaData{name=x.name,rect=new Rect(x.x,x.y,x.width,x.height),alignment=9,pivot=new Vector2(x.pivotX,x.pivotY),border=new Vector4(x.border,x.border,x.border,x.border)}).ToArray();
#pragma warning restore 618
                importer.SaveAndReimport();
            }
            var terrain=(TextureImporter)AssetImporter.GetAtPath(Root+"/Art/Terrain.png");terrain.mipmapEnabled=true;terrain.wrapMode=TextureWrapMode.Repeat;terrain.filterMode=FilterMode.Trilinear;terrain.anisoLevel=4;terrain.textureCompression=TextureImporterCompression.CompressedHQ;terrain.maxTextureSize=2048;terrain.SaveAndReimport();
            var art=existing!=null?existing:ScriptableObject.CreateInstance<PresentationArt>();
            art.sprites=slices.entries.Select(x=>new NamedSprite{id=x.name,sprite=AssetDatabase.LoadAllAssetsAtPath(Root+"/Art/"+x.file).OfType<Sprite>().Single(s=>s.name==x.name)}).ToArray();
            art.bodyFont=AssetDatabase.LoadAssetAtPath<Font>(Root+"/Fonts/Lato-Regular.ttf");art.headingFont=AssetDatabase.LoadAssetAtPath<Font>(Root+"/Fonts/Lato-Bold.ttf");
            if(art.bodyFont==null||art.headingFont==null)throw new InvalidOperationException("Licensed Lato font missing");
            art.ClearCache();if(existing==null)AssetDatabase.CreateAsset(art,ArtPath);else EditorUtility.SetDirty(art);AssetDatabase.SaveAssets();return art;
        }
    }
}
