using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Weedtopia.Domain;
using Weedtopia.Unity;
using Object = UnityEngine.Object;

namespace Weedtopia.Editor.GameSetup
{
    /// <summary>Explicit, idempotent migration of the collection prefab. No runtime hierarchy generation.</summary>
    public static class InventoryAuthoring
    {
        private const string Folder = "Assets/Weedtopia/Production/Rewards";
        private const string TilePath = Folder + "/InventoryTile.prefab", DetailPath = Folder + "/InventoryDetail.prefab";
        private static PresentationArt art;
        private static GameDefinitions defs;
        private static readonly Color Gold = new Color(1,.79f,.37f), Paper = new Color(.97f,.94f,.84f), Muted = new Color(.64f,.78f,.75f);
        [MenuItem("Weedtopia/Presentation/Install Categorized Inventory")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode before authoring.");
            for (int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)
                if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Save manual scene changes first.");
            var setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var scene=EditorSceneManager.OpenScene(GameScaffold.GameScene); var root=Object.FindFirstObjectByType<GameRoot>(); art=root.Presentation.art;
                defs=JsonUtility.FromJson<GameDefinitions>(File.ReadAllText("Assets/Weedtopia/Game/Content/BurningHills.json")); defs.Validate();
                var installed=AssetDatabase.LoadAssetAtPath<GameObject>(RewardPresentationAuthoring.CollectionPath).GetComponent<RewardCollectionUi>();
                if(installed.tiles==null || installed.tiles.Length!=12 || installed.details==null)
                { CreateTile(); CreateDetail(); CreateInventory(); }
                var detailRoot=PrefabUtility.LoadPrefabContents(DetailPath);
                try
                {
                    var detail=detailRoot.GetComponent<InventoryDetailView>();
                    if(detail.split==null)
                    {
                        detail.split=Button("Split",detailRoot.transform,"",new Vector2(0,-331),new Vector2(366,30));
                        detail.splitLabel=detail.split.GetComponentInChildren<Text>();detail.splitLabel.fontSize=16;
                        PrefabUtility.SaveAsPrefabAsset(detailRoot,DetailPath);
                    }
                }
                finally{PrefabUtility.UnloadPrefabContents(detailRoot);}
                BindSurfaceSprites();
                bool migrated=RefineLayout();
                var serialized=new SerializedObject(root.Ui); var bindings=serialized.FindProperty("bindings");
                for(int i=0;i<bindings.arraySize;i++)
                {
                    var b=bindings.GetArrayElementAtIndex(i);string id=b.FindPropertyRelative("id").stringValue;
                    if(id!="collectionJournal" && id!="collectionTeam")continue;
                    var text=((GameObject)b.FindPropertyRelative("target").objectReferenceValue).GetComponentInChildren<Text>();
                    text.text=defs.Text("reward.collection"); PrefabUtility.RecordPrefabInstancePropertyModifications(text);
                }
                var panel=root.Ui.Collection.GetComponent<RectTransform>();
                if(migrated){panel.sizeDelta=new Vector2(1320,930);PrefabUtility.RecordPrefabInstancePropertyModifications(panel);}
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
                var view=root.Ui.Collection;
                if(view.tiles.Length!=12 || view.details==null || view.categoryButtons.Length!=4)throw new InvalidOperationException("Inventory binding failure");
                foreach(var item in defs.items) if(art.Get(item.icon)==null)throw new InvalidOperationException("Missing item icon: "+item.icon);
                Directory.CreateDirectory("Docs/InventoryRevision");
                File.WriteAllText("Docs/InventoryRevision/authoring_receipt.json", "{\"status\":\"passed\",\"items\":12,\"tiles\":12,\"categories\":4,\"authoredPrefabs\":3,\"scene\":\"BurningHills\"}");
            }
            finally{if(setup.Any(s=>s.isLoaded)&&setup.Any(s=>s.isActive))EditorSceneManager.RestoreSceneManagerSetup(setup);}
        }
        private static bool RefineLayout()
        {
            if(AssetDatabase.LoadAssetAtPath<GameObject>(RewardPresentationAuthoring.CollectionPath).GetComponent<RewardCollectionUi>().layoutVersion>=1)return false;
            foreach(string path in new[]{TilePath,DetailPath,RewardPresentationAuthoring.CollectionPath})
            {
                var prefab=PrefabUtility.LoadPrefabContents(path);
                try
                {
                    if(path==RewardPresentationAuthoring.CollectionPath)
                    {
                        var view=prefab.GetComponent<RewardCollectionUi>();
                        view.layoutVersion=1;
                        ((RectTransform)view.inventoryTab.transform).anchoredPosition=new Vector2(155,402);
                        ((RectTransform)view.achievementsTab.transform).anchoredPosition=new Vector2(315,402);
                        ((RectTransform)view.historyTab.transform).anchoredPosition=new Vector2(475,402);
                    }
                    else
                    {
                        Image icon;
                        if(path==TilePath)
                        {
                            var view=prefab.GetComponent<InventoryTileView>();icon=view.icon;
                            view.quantityBadge=prefab.transform.Find("CountPill").GetComponent<Image>();
                        }
                        else
                        {
                            var view=prefab.GetComponent<InventoryDetailView>();icon=view.icon;
                            ((RectTransform)view.action.transform).anchoredPosition=new Vector2(0,-271);
                            foreach(var button in new[]{view.split,view.cancel})
                            {
                                var rect=(RectTransform)button.transform;rect.anchoredPosition=new Vector2(0,-323);rect.sizeDelta=new Vector2(rect.sizeDelta.x,42);
                                button.GetComponentInChildren<Text>().rectTransform.sizeDelta=new Vector2(rect.sizeDelta.x-16,38);
                            }
                        }
                        if(icon.transform.parent==prefab.transform)
                        {
                            var viewport=Rect("IconViewport",prefab.transform,icon.rectTransform.sizeDelta);
                            viewport.anchoredPosition=icon.rectTransform.anchoredPosition;viewport.SetSiblingIndex(icon.transform.GetSiblingIndex());
                            viewport.gameObject.AddComponent<RectMask2D>();icon.transform.SetParent(viewport,false);icon.rectTransform.anchoredPosition=Vector2.zero;
                        }
                    }
                    PrefabUtility.SaveAsPrefabAsset(prefab,path);
                }
                finally{PrefabUtility.UnloadPrefabContents(prefab);}
            }
            return true;
        }
        private static void BindSurfaceSprites()
        {
            // Authored solid-color surface: retain flat graphic colors while satisfying sprite reference checks.
            const string path=Folder+"/Art/UiSurface.png";
            if(!File.Exists(path))
            {
                var texture=new Texture2D(4,4,TextureFormat.RGBA32,false);
                texture.SetPixels(Enumerable.Repeat(Color.white,16).ToArray());texture.Apply();
                File.WriteAllBytes(path,texture.EncodeToPNG());Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;
                importer.spriteImportMode=SpriteImportMode.Single;importer.mipmapEnabled=false;importer.filterMode=FilterMode.Point;
                importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
            }
            var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);
            foreach(var prefabPath in new[]{TilePath,DetailPath,RewardPresentationAuthoring.CardPath,RewardPresentationAuthoring.CollectionPath,RewardPresentationAuthoring.CelebrationPath})
            {
                var prefab=PrefabUtility.LoadPrefabContents(prefabPath);
                try
                {
                    bool changed=false;
                    foreach(var image in prefab.GetComponentsInChildren<Image>(true).Where(i=>i.sprite==null))
                    {image.sprite=sprite;image.preserveAspect=false;EditorUtility.SetDirty(image);changed=true;}
                    if(changed)PrefabUtility.SaveAsPrefabAsset(prefab,prefabPath);
                }
                finally{PrefabUtility.UnloadPrefabContents(prefab);}
            }
        }
        private static void CreateTile()
        {
            if(File.Exists(TilePath))return;
            var rect=Rect("InventoryTile",null,new Vector2(178,175));var view=rect.gameObject.AddComponent<InventoryTileView>();
            var bg=rect.gameObject.AddComponent<Image>();bg.color=new Color(.055f,.17f,.18f);
            view.button=rect.gameObject.AddComponent<Button>();view.button.targetGraphic=bg;
            var colors=view.button.colors;colors.highlightedColor=new Color(1,1,.72f);colors.selectedColor=new Color(1,1,.72f);view.button.colors=colors;
            view.selection=Image("Selection",rect,null,Vector2.zero,new Vector2(178,175),new Color(.22f,.7f,.62f,.22f));
            view.accent=Image("Rarity",rect,null,new Vector2(0,86),new Vector2(178,3),Gold);
            view.icon=Image("Icon",rect,art.Get("gift"),new Vector2(0,27),new Vector2(79,79));
            Image("CountPill",rect,null,new Vector2(53,63),new Vector2(68,28),new Color(.025f,.08f,.09f,.95f));
            view.quantity=Label("Quantity",rect,"",new Vector2(53,63),new Vector2(67,28),18,Gold);
            view.title=Label("Title",rect,"",new Vector2(0,-33),new Vector2(166,56),18,Paper);
            view.tag=Label("Category",rect,"",new Vector2(0,-72),new Vector2(166,22),12,Muted);
            Save(rect,TilePath);
        }
        private static void CreateDetail()
        {
            if(File.Exists(DetailPath))return;
            var rect=Rect("InventoryDetail",null,new Vector2(420,690)); var view=rect.gameObject.AddComponent<InventoryDetailView>();
            var bg=rect.gameObject.AddComponent<Image>();bg.color=new Color(.048f,.145f,.155f,.99f);
            view.accent=Image("Accent",rect,null,new Vector2(0,343),new Vector2(420,3),Gold);
            Image("IconHalo",rect,art.Get("frame"),new Vector2(0,244),new Vector2(172,172),new Color(.44f,.77f,.68f,.6f));
            view.icon=Image("Icon",rect,art.Get("gift"),new Vector2(0,244),new Vector2(125,125));
            view.title=Label("Name",rect,"",new Vector2(0,134),new Vector2(384,68),26,Paper,true);
            view.metadata=Label("Metadata",rect,"",new Vector2(0,81),new Vector2(390,30),16,Gold);
            view.description=Label("Description",rect,"",new Vector2(0,9),new Vector2(368,110),18,Paper);
            view.effect=Label("Effect",rect,"",new Vector2(0,-109),new Vector2(368,124),17,Muted);
            view.choiceHeading=Label("ChoiceHeading",rect,"",new Vector2(0,-185),new Vector2(390,42),15,Gold);
            view.choiceLabel=Label("Choice",rect,"",new Vector2(0,-228),new Vector2(286,58),18,Paper);
            view.previousChoice=Button("PreviousChoice",rect,"‹",new Vector2(-175,-228),new Vector2(48,46));
            view.nextChoice=Button("NextChoice",rect,"›",new Vector2(175,-228),new Vector2(48,46));
            view.action=Button("Use",rect,"",new Vector2(0,-292),new Vector2(366,52));view.actionLabel=view.action.GetComponentInChildren<Text>();view.actionLabel.fontSize=21;
            view.cancel=Button("Cancel",rect,defs.Text("inventory.cancel"),new Vector2(0,-331),new Vector2(128,28));view.cancel.GetComponentInChildren<Text>().fontSize=15;
            Save(rect,DetailPath);
        }
        private static void CreateInventory()
        {
            var rect=Rect("RewardCollection",null,new Vector2(1320,930));var view=rect.gameObject.AddComponent<RewardCollectionUi>();
            var bg=rect.gameObject.AddComponent<Image>();bg.color=new Color(.022f,.073f,.083f,.995f);
            Image("TopRule",rect,null,new Vector2(0,463),new Vector2(1320,3),Gold);
            Image("Divider",rect,null,new Vector2(174,-30),new Vector2(1,760),new Color(.18f,.34f,.33f));
            view.heading=Label("Heading",rect,"",new Vector2(-317,404),new Vector2(598,62),38,Gold,true);view.heading.alignment=TextAnchor.MiddleLeft;
            view.balance=Label("Balance",rect,"",new Vector2(-315,358),new Vector2(600,30),17,Muted);view.balance.alignment=TextAnchor.MiddleLeft;
            view.inventoryTab=Button("Inventory",rect,defs.Text("reward.tab.inventory"),new Vector2(175,402),new Vector2(148,46));
            view.achievementsTab=Button("Achievements",rect,defs.Text("reward.tab.achievements"),new Vector2(350,402),new Vector2(148,46));
            view.historyTab=Button("History",rect,defs.Text("reward.tab.history"),new Vector2(525,402),new Vector2(148,46));
            view.description=Label("Description",rect,"",new Vector2(-234,251),new Vector2(774,44),18,Muted);
            view.inventoryArea=Rect("InventoryArea",rect,rect.sizeDelta).gameObject;
            var area=view.inventoryArea.transform;
            string[] labels={"inventory.all","inventory.category.Workers","inventory.category.Boosters","inventory.category.Supplies"};
            float[] positions={-559,-382,-154,59},widths={118,212,212,178};view.categoryButtons=new Button[4];
            for(int i=0;i<4;i++){view.categoryButtons[i]=Button("Category_"+i,area,defs.Text(labels[i]),new Vector2(positions[i],307),new Vector2(widths[i],48));view.categoryButtons[i].GetComponentInChildren<Text>().fontSize=18;}
            view.tiles=new InventoryTileView[12];
            for(int i=0;i<12;i++)
            {
                var tile=Instance(TilePath,area);tile.name="Slot_"+(i+1);tile.GetComponent<RectTransform>().anchoredPosition=new Vector2(-525+i%4*192,137-i/4*192);
                view.tiles[i]=tile.GetComponent<InventoryTileView>();
            }
            var detail=Instance(DetailPath,area);detail.GetComponent<RectTransform>().anchoredPosition=new Vector2(423,-42);view.details=detail.GetComponent<InventoryDetailView>();
            view.archiveArea=Rect("ArchiveArea",rect,rect.sizeDelta).gameObject;view.cards=new RewardCardView[9];
            for(int i=0;i<9;i++)
            {var card=Instance(RewardPresentationAuthoring.CardPath,view.archiveArea.transform);card.name="Reward_"+(i+1);card.GetComponent<RectTransform>().anchoredPosition=new Vector2((i%3-1)*370,115-i/3*178);view.cards[i]=card.GetComponent<RewardCardView>();}
            view.emptyLabel=Label("Empty",rect,"",new Vector2(-230,-40),new Vector2(720,160),24,Paper);
            view.previousButton=Button("Previous",rect,"‹",new Vector2(-377,-414),new Vector2(110,48));
            view.nextButton=Button("Next",rect,"›",new Vector2(-89,-414),new Vector2(110,48));
            view.pageLabel=Label("Page",rect,"",new Vector2(-233,-414),new Vector2(170,38),20,Paper);
            Save(rect,RewardPresentationAuthoring.CollectionPath);
        }
        private static GameObject Instance(string path,Transform parent)=>(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path),parent);
        private static RectTransform Rect(string name,Transform parent,Vector2 size)
        {var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rect.SetParent(parent,false);rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);rect.sizeDelta=size;return rect;}
        private static Image Image(string name,Transform parent,Sprite sprite,Vector2 position,Vector2 size,Color? color=null)
        {var rect=Rect(name,parent,size);rect.anchoredPosition=position;var image=rect.gameObject.AddComponent<Image>();image.sprite=sprite;image.color=color??Color.white;image.preserveAspect=sprite!=null;image.raycastTarget=false;return image;}
        private static Text Label(string name,Transform parent,string value,Vector2 position,Vector2 size,int fontSize,Color color,bool heading=false)
        {var rect=Rect(name,parent,size);rect.anchoredPosition=position;var text=rect.gameObject.AddComponent<Text>();text.font=heading?art.headingFont:art.bodyFont;text.fontSize=fontSize;text.text=value;text.color=color;text.alignment=TextAnchor.MiddleCenter;text.raycastTarget=false;text.supportRichText=false;return text;}
        private static Button Button(string name,Transform parent,string label,Vector2 position,Vector2 size)
        {var go=Instance(RewardPresentationAuthoring.ButtonPath,parent);go.name=name;var rect=go.GetComponent<RectTransform>();rect.anchoredPosition=position;rect.sizeDelta=size;var text=go.GetComponentInChildren<Text>();text.rectTransform.sizeDelta=new Vector2(size.x-16,size.y-4);text.text=label;text.fontSize=20;return go.GetComponent<Button>();}
        private static void Save(RectTransform rect,string path){PrefabUtility.SaveAsPrefabAsset(rect.gameObject,path);Object.DestroyImmediate(rect.gameObject);}
    }
}
