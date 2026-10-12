from pathlib import Path

root=Path(__file__).resolve().parents[2]
source=root/'Assets/Weedtopia/Scripts/Editor/GameSetup/UiPolishEvidence.cs'
target=source.with_name('UiRefinementEvidence.cs')
s=source.read_text(encoding='utf-8-sig').replace('UiPolishEvidence','UiRefinementEvidence').replace('PolishCaptureReceipt','RefinementCaptureReceipt').replace('UiPolishCapture','UiRefinementCapture').replace('Docs/UiPolishRevision','Docs/UiRefinementRevision')
start=s.index('        private static void Prepare()')
end=s.index('        private static void Inspect(',start)
prepare='''        private static void Populate()
        {
            World();var state=root.Session.State;state.grinders=10000;state.cards=3;
            InventoryItems.Grant(state,root.Definitions.items.Select(i=>new ItemStack{id=i.id,count=2}).ToArray(),new List<RewardItem>());
            for(int i=0;i<30;i++)root.Session.Workers.Grant("grow");state.revision++;root.Refresh();
        }
        private static void Prepare()
        {
            foreach(var size in new[]{(1920,1080,1f),(1280,720,1f),(844,390,1f),(390,844,1f),(2340,1080,2.75f)})
            {
                steps.Add((size.Item1,size.Item2,size.Item3,"worker_picker",()=>{World();root.Session.State.Find("farm_grow_01").level=1;root.Session.Workers.Grant("grow");root.Ui.Interaction.OpenBuilding("farm_grow_01");root.Ui.Interaction.OpenPicker(0);root.Refresh();}));
                steps.Add((size.Item1,size.Item2,size.Item3,"settings",()=>root.Ui.Experience.ShowSettings()));
                foreach(int avatarIndex in new[]{0,5,9})
                    steps.Add((size.Item1,size.Item2,size.Item3,"profile_"+avatarIndex,()=>{root.ChangeProfile("Kaya",PlayerProfile.Avatars[avatarIndex]);root.Ui.Experience.ShowSettings();root.Ui.Experience.SelectSettingsTab(true);}));
                steps.Add((size.Item1,size.Item2,size.Item3,"inventory_top",()=>{Populate();root.Ui.ShowCollection();root.Ui.Collection.GetComponent<InventoryPanelLayout>().ResetList();}));
                steps.Add((size.Item1,size.Item2,size.Item3,"inventory_bottom",()=>root.Ui.Collection.GetComponent<InventoryPanelLayout>().grid.verticalNormalizedPosition=0));
                steps.Add((size.Item1,size.Item2,size.Item3,"inventory_detail",()=>{var v=root.Ui.Collection;v.SelectCategory(2);v.Inspect(v.tiles.First(t=>t.gameObject.activeSelf&&t.Item.key=="build_24h").Item);}));
                steps.Add((size.Item1,size.Item2,size.Item3,"inventory_split",()=>root.Ui.Collection.details.split.onClick.Invoke()));
                steps.Add((size.Item1,size.Item2,size.Item3,"wheel_free",()=>{World();root.Session.State.grinders=10000;root.Ui.ShowLuckyWheel();}));
                foreach(int prize in new[]{0,3,6,9})
                    steps.Add((size.Item1,size.Item2,size.Item3,"wheel_prize_"+prize,()=>root.Ui.LuckyWheel.Inspect(root.Session.Wheel.Remaining[prize])));
                steps.Add((size.Item1,size.Item2,size.Item3,"wheel_paid",()=>{root.Ui.LuckyWheel.Spin();root.Ui.Close();root.Ui.ShowLuckyWheel();root.Ui.LuckyWheel.Spin();}));
            }
            for(int index=0;index<10;index++)
            {int avatarIndex=index;steps.Add((1920,1080,1f,"avatar_"+index,()=>{root.ChangeProfile("Kaya",PlayerProfile.Avatars[avatarIndex]);root.Ui.Experience.ShowSettings();root.Ui.Experience.SelectSettingsTab(true);}));}
            foreach(string id in new[]{"workers","journal","settings","collection","luckyWheel","vipStories","cityCampaign","questPreview","questDetails","offline","newGameConfirm","townhall"})
                steps.Add((1920,1080,1f,"close_"+id,()=>root.Ui.ShowPanel(id)));
        }
        private static bool Inside(RectTransform child,RectTransform parent)
        {
            var corners=new Vector3[4];child.GetWorldCorners(corners);var r=parent.rect;
            return corners.All(c=>{var p=parent.InverseTransformPoint(c);return p.x>=r.xMin-2&&p.x<=r.xMax+2&&p.y>=r.yMin-2&&p.y<=r.yMax+2;});
        }
'''
s=s[:start]+prepare+s[end:]
insert='''            foreach(var close in root.Ui.GetComponentsInChildren<CloseButtonVisual>())
            {if(close.icon.sprite!=root.Presentation.art.Get("close")||!close.icon.enabled||close.caption!=null&&close.caption.enabled)issues.Add(screen+": inconsistent close icon");}
            if(screen.EndsWith("inventory_top"))
            {
                var v=root.Ui.Collection;var layout=v.GetComponent<InventoryPanelLayout>();
                int visible=v.tiles.Count(t=>t.gameObject.activeSelf&&Inside((RectTransform)t.transform,layout.grid.viewport));
                if(visible<8)issues.Add(screen+": only "+visible+" fully visible inventory tiles");
                if(v.previousButton.gameObject.activeSelf||v.nextButton.gameObject.activeSelf||v.pageLabel.gameObject.activeSelf)issues.Add(screen+": pagination still visible");
            }
            if(root.Ui.LuckyWheel.gameObject.activeInHierarchy)
            {
                var v=root.Ui.LuckyWheel;var layout=v.GetComponent<WheelPanelLayout>();
                if(!Inside(layout.stage,layout.frame.viewport))issues.Add(screen+": wheel outside viewport");
                foreach(var label in new[]{v.remaining,v.countdown,v.chance,v.balance,v.status,v.itemName,v.description,v.costHeading,v.costBody}.Where(t=>t.gameObject.activeInHierarchy))
                {
                    if(!Inside(label.rectTransform,layout.frame.viewport))issues.Add(screen+": wheel label outside viewport "+label.name);
                    float preferred=DialoguePanel.PreferredHeight(label,label.rectTransform.rect.width);
                    if(preferred>label.rectTransform.rect.height+2)issues.Add(screen+": wheel text overflow "+label.name);
                }
                foreach(var action in new[]{v.spin,v.cancel,v.inventory}.Where(b=>b.gameObject.activeInHierarchy))if(!Inside((RectTransform)action.transform,layout.frame.viewport))issues.Add(screen+": wheel action outside viewport "+action.name);
            }
'''
needle='            var experience=root.Ui.Experience;'
s=s.replace(needle,insert+needle)
target.write_text(s,encoding='utf-8')
source=root/'Tools/Implementation/verify_ui_polish_preservation.py'
target=source.with_name('verify_ui_refinement_preservation.py')
target.write_text(source.read_text().replace("REPORT=ROOT/'Docs/UiPolishRevision'","REPORT=ROOT/'Docs/UiRefinementRevision'"),encoding='utf-8')
