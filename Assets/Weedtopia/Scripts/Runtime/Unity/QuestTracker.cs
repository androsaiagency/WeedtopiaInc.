using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Weedtopia.Application;
using Weedtopia.Domain;

namespace Weedtopia.Unity
{
    /// <summary>Active quest buttons and a readable scrollable checklist, backed by existing progression services.</summary>
    public sealed class QuestTracker : MonoBehaviour
    {
        public RectTransform dock;
        public Text dockHeading, heading, note, nextStep;
        public ScrollRect dockScroll, detailScroll;
        public Button rowPrefab, claim, journal;
        public Button shortcutPrefab, previewAction, previewDetails;
        public RectTransform preview;
        public Text previewHeading, previewBody;
        private GameRoot root;
        private GuidanceQuest[] active=Array.Empty<GuidanceQuest>();
        private readonly List<Button> dockRows=new List<Button>(),steps=new List<Button>();
        private string selected;
        public GuidanceQuest Selected => active.FirstOrDefault(q=>q.id==selected);
        public IReadOnlyList<Button> Buttons => dockRows;
        public void Initialize(GameRoot value)
        {
            root=value;dock.gameObject.SetActive(false);claim.onClick.AddListener(Claim);journal.onClick.AddListener(()=>root.Ui.ShowJournal());
            previewDetails.onClick.AddListener(()=>OpenDetails(selected));
            previewAction.onClick.AddListener(()=>{if(Selected?.ready==true)Claim();else if(Selected!=null)Follow(Array.FindIndex(Selected.steps,s=>!s.done));});
        }
        public void Open(string id)
        {selected=id;root.Ui.ShowPanel("questPreview");Refresh();}
        public void OpenDetails(string id)
        {selected=id;root.Ui.ShowPanel("questDetails");Refresh();detailScroll.verticalNormalizedPosition=1;}
        public void Refresh()
        {
            if(root.Session==null)return;
            active=QuestGuidance.Active(root.Session);
            dock.gameObject.SetActive(root.Session.State.introSeen&&!root.Ui.LegacyModalOpen&&!root.Ui.Interaction.BuildingOpen&&!root.Ui.Interaction.PickerOpen);
            dockHeading.text=GameText.Format("quest.active_count",active.Length);
            for(int i=0;i<dockRows.Count;i++)dockRows[i].gameObject.SetActive(i<active.Length);
            for(int i=0;i<active.Length;i++)
            {
                int index=i;var row=Row(dockRows,i,dockScroll.content,()=>Open(active[index].id),shortcutPrefab);var q=active[i];
                row.GetComponentInChildren<Text>().text=q.ready?"✓":(i+1).ToString();
            }
            if(!root.Ui.Element<RectTransform>("questDetails").gameObject.activeSelf&&!preview.gameObject.activeSelf)return;
            var current=Selected;
            if(current==null){current=active.FirstOrDefault();selected=current?.id;}
            heading.text=current?.title??GameText.Get("presentation.questtracker.alle_aufgaben_erledigt.2f996e3f");note.text=current?.note??GameText.Get("presentation.questtracker.entdecke_deine_stadt_und_sprich_mit_ihre.8f1520bc");
            nextStep.text=current==null?"":current.ready?GameText.Get("presentation.questtracker.alle_schritte_erledigt_hole_deine_belohn.e16d2c08"):GameText.Get("presentation.questtracker.n_chster_schritt.990a8951")+current.steps.FirstOrDefault(s=>!s.done)?.text;
            previewHeading.text=heading.text;
            previewBody.text=current==null?GameText.Get("presentation.questtracker.entdecke_deine_stadt_und_sprich_mit_ihre.8f1520bc"):current.ready?GameText.Get("presentation.questtracker.alle_schritte_sind_erledigt_du_kannst_je.9458d9b4"):GameText.Get("presentation.questtracker.als_n_chstes.167e1c75")+current.steps.First(s=>!s.done).text;
            previewAction.interactable=current!=null;
            previewAction.GetComponentInChildren<Text>().text=current?.ready==true?GameText.Get("presentation.questtracker.belohnung_abholen.2fd3b6df"):GameText.Get("presentation.questtracker.zum_ziel.222c6d4d");
            for(int i=0;i<steps.Count;i++)steps[i].gameObject.SetActive(current!=null&&i<current.steps.Length);
            if(current!=null)for(int i=0;i<current.steps.Length;i++)
            {
                int index=i;var row=Row(steps,i,detailScroll.content,()=>Follow(index));var step=current.steps[i];
                var label=row.GetComponentInChildren<Text>();label.text=(step.done?"✓ ":"• ")+step.text;label.color=step.done?new Color(.62f,.91f,.68f):new Color(.98f,.94f,.84f);
                row.interactable=step.focus!=null||step.person!=null;
            }
            claim.interactable=current!=null&&current.ready;
            claim.GetComponentInChildren<Text>().text=GameText.Get("presentation.questtracker.belohnung_abholen.2fd3b6df");
        }
        private Button Row(List<Button> pool,int index,Transform parent,Action action,Button prefab=null)
        {
            while(pool.Count<=index){var row=Instantiate(prefab==null?rowPrefab:prefab,parent,false);row.onClick.AddListener(()=>action());pool.Add(row);}
            pool[index].gameObject.SetActive(true);return pool[index];
        }
        public void Follow(int index)
        {
            var current=Selected;if(current==null||index<0||index>=current.steps.Length)return;var step=current.steps[index];
            if(step.person!=null){root.Ui.ShowStories(step.person);return;}
            if(current.kind==GuidanceKind.Project&&root.Session.CityProject.Current==null){root.Ui.ShowJournal();return;}
            if(step.focus==null){root.Ui.ShowJournal();return;}
            root.Ui.Close();root.Focus(step.focus);root.Ui.Interaction.OpenBuilding(step.focus);
        }
        private void Claim()
        {
            var q=Selected;if(q==null||!q.ready)return;
            if(q.kind==GuidanceKind.Story)root.StoryCommand(()=>root.Session.Stories.Claim(q.source,root.Session.Stories.ActiveDefinition(q.source).id));
            else if(q.kind==GuidanceKind.Vip)root.Command(()=>root.Session.Progression.ClaimVip(q.source));
            else root.Command(()=>q.kind==GuidanceKind.Project?root.Session.CityProject.Claim():root.Session.Progression.Claim());
            Refresh();
            if(preview.gameObject.activeSelf&&Selected==null)root.Ui.Close();
        }
        public void Fit(RectTransform safe,float pixels)
        {
            float margin=Mathf.Max(24,12/pixels),font=Mathf.Max(25,18/pixels),header=Mathf.Max(110,96/pixels);
            if(safe.rect.width<1320)header+=Mathf.Max(100,60/pixels);
            dock.anchorMin=dock.anchorMax=dock.pivot=new Vector2(0,1);dock.anchoredPosition=new Vector2(margin,-header);
            float unit=Mathf.Max(104,56/pixels),gap=Mathf.Max(12,8/pixels);
            int count=dockRows.Count(r=>r.gameObject.activeSelf);
            dock.sizeDelta=new Vector2(Mathf.Min(safe.rect.width-2*margin,Mathf.Min(4,count)*(unit+gap)),unit+8);
            dockHeading.gameObject.SetActive(false);
            var view=(RectTransform)dockScroll.transform;view.anchorMin=Vector2.zero;view.anchorMax=Vector2.one;view.offsetMin=Vector2.zero;view.offsetMax=Vector2.zero;
            dockScroll.content.sizeDelta=new Vector2(dockScroll.content.sizeDelta.x,unit);
            foreach(var row in dockRows)
            {
                var element=row.GetComponent<LayoutElement>();element.minWidth=element.preferredWidth=unit;element.minHeight=element.preferredHeight=unit;
                var caption=row.GetComponentInChildren<Text>();caption.fontSize=Mathf.RoundToInt(font*.85f);
                DialoguePanel.Place(caption.rectTransform,new Vector2(.5f,.5f),new Vector2(0,-unit*.28f),new Vector2(unit*.5f,font*1.2f));
                var icon=row.transform.Find("QuestIcon").GetComponent<RectTransform>();DialoguePanel.Place(icon,new Vector2(.5f,.5f),new Vector2(0,unit*.09f),Vector2.one*unit*.50f);
            }
            if(preview.gameObject.activeSelf)FitPreview(safe,pixels,font);
            var panel=root.Ui.Element<RectTransform>("questDetails");if(!panel.gameObject.activeSelf)return;
            float width=panel.rect.width,inset=Mathf.Max(36,18/pixels),button=Mathf.Max(70,48/pixels);
            heading.fontSize=Mathf.RoundToInt(font*1.2f);note.fontSize=Mathf.RoundToInt(font*.87f);nextStep.fontSize=Mathf.RoundToInt(font);
            float y=inset;
            foreach(var label in new[]{heading,note,nextStep})
            {
                float available=width-2*inset-(label==heading?button:0);
                float height=Mathf.Max(font*1.5f,label.cachedTextGeneratorForLayout.GetPreferredHeight(label.text,label.GetGenerationSettings(new Vector2(available,0)))/label.pixelsPerUnit+8);
                DialoguePanel.Place(label.rectTransform,new Vector2(.5f,1),new Vector2(label==heading?-button*.5f:0,-y-height*.5f),new Vector2(available,height));y+=height+font*.4f;
            }
            var details=(RectTransform)detailScroll.transform;details.anchorMin=Vector2.zero;details.anchorMax=Vector2.one;details.offsetMin=new Vector2(inset,inset+button+16);details.offsetMax=new Vector2(-inset,-y);
            FitRows(steps,width-2*inset,font);
            float actionWidth=(width-2*inset-20)*.5f;
            DialoguePanel.Place((RectTransform)claim.transform,new Vector2(.5f,0),new Vector2(-actionWidth*.5f-10,inset+button*.5f),new Vector2(actionWidth,button));
            DialoguePanel.Place((RectTransform)journal.transform,new Vector2(.5f,0),new Vector2(actionWidth*.5f+10,inset+button*.5f),new Vector2(actionWidth,button));
            foreach(var b in new[]{claim,journal})DialoguePanel.FitCaption(b,font*.85f);
        }
        private void FitPreview(RectTransform safe,float pixels,float font)
        {
            float inset=Mathf.Max(48,24/pixels),button=Mathf.Max(78,48/pixels),gap=Mathf.Max(20,10/pixels);
            float width=Mathf.Min(950,safe.rect.width-2*Mathf.Max(24,12/pixels));
            bool stacked=width*pixels<460;
            previewHeading.fontSize=Mathf.RoundToInt(font*1.12f);previewBody.fontSize=Mathf.RoundToInt(font);
            float title=DialoguePanel.PreferredHeight(previewHeading,width-2*inset-button);
            float body=DialoguePanel.PreferredHeight(previewBody,width-2*inset);
            float height=3*inset+title+body+(stacked?2*button+gap:button);
            preview.anchorMin=preview.anchorMax=preview.pivot=new Vector2(.5f,.5f);preview.anchoredPosition=Vector2.zero;preview.localScale=Vector3.one;preview.sizeDelta=new Vector2(width,height);
            DialoguePanel.Place(previewHeading.rectTransform,new Vector2(.5f,1),new Vector2(-button*.5f,-inset-title*.5f),new Vector2(width-2*inset-button,title));
            DialoguePanel.Place(previewBody.rectTransform,new Vector2(.5f,1),new Vector2(0,-inset-title-gap-body*.5f),new Vector2(width-2*inset,body));
            float actionWidth=stacked?width-2*inset:(width-2*inset-gap)*.5f;
            DialoguePanel.Place((RectTransform)previewAction.transform,new Vector2(.5f,0),new Vector2(stacked?0:-actionWidth*.5f-gap*.5f,inset+button*.5f+(stacked?button+gap:0)),new Vector2(actionWidth,button));
            DialoguePanel.Place((RectTransform)previewDetails.transform,new Vector2(.5f,0),new Vector2(stacked?0:actionWidth*.5f+gap*.5f,inset+button*.5f),new Vector2(actionWidth,button));
            DialoguePanel.FitCaption(previewAction,font*.85f);DialoguePanel.FitCaption(previewDetails,font*.85f);
            root.Ui.Experience.FitClose(preview,pixels);
        }
        private static void FitRows(IEnumerable<Button> rows,float width,float font)
        {
            foreach(var row in rows)
            {
                var label=row.GetComponentInChildren<Text>();label.fontSize=Mathf.RoundToInt(font);
                var rect=label.rectTransform;rect.offsetMin=new Vector2(20,12);rect.offsetMax=new Vector2(-20,-12);
                float height=label.cachedTextGeneratorForLayout.GetPreferredHeight(label.text,label.GetGenerationSettings(new Vector2(Mathf.Max(40,width-40),0)))/label.pixelsPerUnit;
                row.GetComponent<LayoutElement>().preferredHeight=Mathf.Max(font*2.4f,height+30);
            }
        }
    }
}
