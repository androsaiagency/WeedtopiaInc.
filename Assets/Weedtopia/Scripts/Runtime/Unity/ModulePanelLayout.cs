using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Weedtopia.Unity
{
    /// <summary>Reflows existing authored module controls into a readable, vertically scrollable window.</summary>
    public sealed class ModulePanelLayout : MonoBehaviour
    {
        public ModalFrame frame;
        public ScrollRect scroll;
        public RectTransform wheelStage;
        public Button inventoryWheel;
        private float width, font, gap, button, y;
        public void Fit(float pixels)
        {
            var storyLayout=GetComponent<VipStoryPanelLayout>();if(storyLayout!=null){storyLayout.Fit(pixels);return;}
            var inventoryLayout=GetComponent<InventoryPanelLayout>();if(inventoryLayout!=null){inventoryLayout.Fit(pixels);return;}
            var wheelLayout=GetComponent<WheelPanelLayout>();if(wheelLayout!=null){wheelLayout.Fit(pixels);return;}
            var body=frame.body;body.localScale=Vector3.one;body.anchorMin=new Vector2(0,1);body.anchorMax=Vector2.one;body.pivot=new Vector2(.5f,1);
            width=frame.viewport.rect.width;font=Mathf.Max(25,16/pixels);gap=Mathf.Max(18,10/pixels);button=Mathf.Max(78,48/pixels);y=gap;
            // Never reset scroll position during fitting. Unity clamps it when the current content becomes shorter.
            body.sizeDelta=new Vector2(0,body.sizeDelta.y);
            var inventory=GetComponent<RewardCollectionUi>();var stories=GetComponent<VipStoriesUi>();var cities=GetComponent<CityCampaignUi>();var wheel=GetComponent<LuckyWheelUi>();
            if(inventory!=null)Inventory(inventory);else if(stories!=null)Stories(stories);else if(cities!=null)Cities(cities);else if(wheel!=null)Wheel(wheel);
            body.sizeDelta=new Vector2(0,Mathf.Max(y+gap,frame.viewport.rect.height));
        }
        private void Inventory(RewardCollectionUi v)
        {
            Hide("TopRule","Divider");Actions(v.inventoryTab,v.achievementsTab,v.historyTab);Paragraph(v.balance);Paragraph(v.description);
            foreach(var area in new[]{v.inventoryArea,v.archiveArea}){var r=(RectTransform)area.transform;r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,1);r.anchoredPosition=Vector2.zero;r.sizeDelta=new Vector2(width,frame.body.sizeDelta.y);}
            if(v.inventoryArea.activeSelf)
            {
                int columns=width/font>=32?4:2;float cell=(width-gap*(columns-1))/columns;
                for(int i=0;i<v.categoryButtons.Length;i++){int row=i/columns;Action(v.categoryButtons[i],-width*.5f+cell*.5f+i%columns*(cell+gap),y+row*(button+gap),cell,button);}
                y+=Mathf.Ceil(v.categoryButtons.Length/(float)columns)*(button+gap);
                var tiles=v.tiles.Where(t=>t.gameObject.activeSelf).ToArray();float height=font*9;
                for(int i=0;i<tiles.Length;i++)
                {
                    var t=tiles[i];Put((RectTransform)t.transform,-width*.5f+cell*.5f+i%columns*(cell+gap),y+i/columns*(height+gap),cell,height);
                    t.title.fontSize=Mathf.RoundToInt(font);t.tag.fontSize=Mathf.RoundToInt(font*.8f);t.quantity.fontSize=Mathf.RoundToInt(font*.9f);
                    DialoguePanel.Place(t.title.rectTransform,new Vector2(.5f,0),new Vector2(0,font*2.7f),new Vector2(cell-16,font*3.2f));
                    DialoguePanel.Place(t.tag.rectTransform,new Vector2(.5f,0),new Vector2(0,font*.7f),new Vector2(cell-16,font*1.2f));
                    float icon=Mathf.Min(cell*.65f,font*4);Put((RectTransform)t.icon.transform.parent,0,font*.5f,icon,icon);
                    t.icon.rectTransform.anchorMin=Vector2.zero;t.icon.rectTransform.anchorMax=Vector2.one;t.icon.rectTransform.offsetMin=t.icon.rectTransform.offsetMax=Vector2.zero;
                    t.selection.rectTransform.anchorMin=Vector2.zero;t.selection.rectTransform.anchorMax=Vector2.one;t.selection.rectTransform.offsetMin=t.selection.rectTransform.offsetMax=Vector2.zero;
                    DialoguePanel.Place(t.accent.rectTransform,new Vector2(.5f,1),new Vector2(0,-6),new Vector2(cell-2*gap,4));
                    DialoguePanel.Place(t.quantityBadge.rectTransform,new Vector2(1,1),new Vector2(-font,-font*.6f),new Vector2(font*2,font*1.2f));
                    t.quantity.rectTransform.anchorMin=t.quantity.rectTransform.anchorMax=new Vector2(1,1);t.quantity.rectTransform.anchoredPosition=new Vector2(-font,-font*.6f);t.quantity.rectTransform.sizeDelta=new Vector2(font*2,font*1.2f);
                }
                y+=Mathf.Ceil(tiles.Length/(float)columns)*(height+gap);Paragraph(v.emptyLabel);Actions(v.previousButton,v.nextButton);Paragraph(v.pageLabel);
                float top=y;var d=v.details;var r=(RectTransform)d.transform;Put(r,0,top,width,1);y=gap;
                foreach(var decoration in new[]{"IconHalo","Accent"}){var child=r.Find(decoration);if(child!=null)child.gameObject.SetActive(false);}
                float iconSize=font*4;Put((RectTransform)d.icon.transform.parent,0,y,iconSize,iconSize);y+=iconSize+gap;
                Paragraph(d.title);Paragraph(d.metadata);Paragraph(d.description);Paragraph(d.effect);Paragraph(d.choiceHeading);Paragraph(d.choiceLabel);
                Actions(d.previousChoice,d.nextChoice);Actions(d.action);Actions(d.split,d.cancel);float detailHeight=y+gap;Put(r,0,top,width,detailHeight);y=top+detailHeight+gap;
            }
            else
            {
                foreach(var card in v.cards.Where(c=>c.gameObject.activeSelf))
                {
                    float height=font*7;Put((RectTransform)card.transform,0,y,width,height);
                    float icon=font*3;var iconRect=card.icon.transform.parent.GetComponent<RectMask2D>()!=null?(RectTransform)card.icon.transform.parent:card.icon.rectTransform;
                    DialoguePanel.Place(iconRect,new Vector2(0,.5f),new Vector2(icon*.5f+gap,0),new Vector2(icon,icon));
                    float textWidth=width-icon-3*gap;foreach(var text in new[]{card.quantity,card.title,card.detail})text.fontSize=Mathf.RoundToInt(font);
                    DialoguePanel.Place(card.quantity.rectTransform,new Vector2(1,1),new Vector2(-textWidth*.5f-gap,-font),new Vector2(textWidth,font*1.5f));
                    DialoguePanel.Place(card.title.rectTransform,new Vector2(1,.5f),new Vector2(-textWidth*.5f-gap,0),new Vector2(textWidth,font*3));
                    DialoguePanel.Place(card.detail.rectTransform,new Vector2(.5f,0),new Vector2(0,font*.9f),new Vector2(width-20,font*1.5f));y+=height+gap;
                }
                Paragraph(v.emptyLabel);Actions(v.previousButton,v.nextButton);Paragraph(v.pageLabel);
            }
            Actions(inventoryWheel);
        }
        private void Stories(VipStoriesUi v)
        {
            Actions(v.peopleTab,v.togetherTab);Paragraph(v.milestone);
            float rosterHeight=font*11;Put((RectTransform)v.rosterScroll.transform,0,y,width,rosterHeight);StretchViewport(v.rosterScroll);y+=rosterHeight+gap;
            foreach(var row in v.rows)
            {
                float h=font*5.5f;var layout=row.GetComponent<LayoutElement>();if(layout!=null)layout.preferredHeight=h;
                DialoguePanel.Place(row.accent.rectTransform,new Vector2(0,.5f),new Vector2(2,0),new Vector2(4,h-gap));
                float icon=font*4;DialoguePanel.Place((RectTransform)row.portrait.transform.parent,new Vector2(0,.5f),new Vector2(icon*.5f+gap,0),new Vector2(icon,h-gap));
                float textWidth=width-icon-4*gap;row.caption.fontSize=Mathf.RoundToInt(font);row.status.fontSize=Mathf.RoundToInt(font*.85f);
                DialoguePanel.Place(row.caption.rectTransform,new Vector2(1,.5f),new Vector2(-textWidth*.5f-gap,font*.8f),new Vector2(textWidth,font*2.5f));
                DialoguePanel.Place(row.status.rectTransform,new Vector2(1,.5f),new Vector2(-textWidth*.5f-gap,-font*1.4f),new Vector2(textWidth,font*1.8f));
            }
            Paragraph(v.nameLabel);Paragraph(v.role);Paragraph(v.biography);Paragraph(v.greeting);
            Put(v.portrait.rectTransform,0,y,font*5,font*7);y+=font*7+gap;
            float detailHeight=Mathf.Max(font*14,frame.viewport.rect.height*.5f);Put((RectTransform)v.detailScroll.transform,0,y,width,detailHeight);StretchViewport(v.detailScroll);y+=detailHeight+gap;
            foreach(var text in v.detailScroll.content.GetComponentsInChildren<Text>(true))text.fontSize=Mathf.RoundToInt(font);
            foreach(var action in v.goals){DialoguePanel.FitCaption(action,font);var layout=action.GetComponent<LayoutElement>();if(layout!=null)layout.preferredHeight=Mathf.Max(button,DialoguePanel.PreferredHeight(action.GetComponentInChildren<Text>(),width-40)+24);}
            Actions(v.accept,v.alternate);Actions(v.claim);Actions(v.speak,v.focus);Actions(v.remember);Paragraph(v.hint);
        }
        private void Cities(CityCampaignUi v)
        {
            Hide("TopRule","Divider");Paragraph(v.invitationTitle);Paragraph(v.invitation);Paragraph(v.bridge);Paragraph(v.status);Actions(v.accept);Paragraph(v.shared);
            if(v.HasAuthoredTravelRows){foreach(var row in v.travelRows){if(row==null)continue;Paragraph(row.progress);Actions(row.travel);}}
            else{Paragraph(v.burningProgress);Actions(v.burningTravel);Paragraph(v.rockProgress);Actions(v.rockTravel);}
            float size=font*5;Put(v.rockoPortrait.rectTransform,0,y,size,size);y+=size+gap;
        }
        private void Wheel(LuckyWheelUi v)
        {
            Hide("TopRule","DetailsSurface","ItemAccent","Subtitle","ResetHeading","ResetHint");Paragraph(v.remaining);Paragraph(v.countdown);Actions(v.spin,v.cancel);Paragraph(v.status);
            float diameter=Mathf.Min(width,font*28);Put(wheelStage,0,y,730,730);wheelStage.localScale=Vector3.one*(diameter/730);wheelStage.anchoredPosition=new Vector2(0,-y-diameter*.5f);y+=diameter+gap;
            Paragraph(v.chance);float icon=font*4;Put((RectTransform)v.itemIcon.transform.parent,0,y,icon,icon);y+=icon+gap;
            Paragraph(v.itemName);Paragraph(v.description);Paragraph(v.costHeading);Paragraph(v.costBody);Paragraph(v.balance);Actions(v.inventory);
        }
        private void Paragraph(Text text)
        {
            if(text==null||!text.gameObject.activeSelf||string.IsNullOrEmpty(text.text))return;
            text.fontSize=Mathf.RoundToInt(font);text.horizontalOverflow=HorizontalWrapMode.Wrap;text.verticalOverflow=VerticalWrapMode.Truncate;
            float height=Mathf.Max(font*1.5f,DialoguePanel.PreferredHeight(text,width-20));Put(text.rectTransform,0,y,width-20,height);y+=height+gap;
        }
        private void Actions(params Button[] actions)
        {
            var visible=actions.Where(a=>a!=null&&a.gameObject.activeSelf).ToArray();if(visible.Length==0)return;
            float cell=(width-gap*(visible.Length-1))/visible.Length,height=button;
            foreach(var a in visible){DialoguePanel.FitCaption(a,font);height=Mathf.Max(height,DialoguePanel.PreferredHeight(a.GetComponentInChildren<Text>(),cell-48)+24);}
            for(int i=0;i<visible.Length;i++)Action(visible[i],-width*.5f+cell*.5f+i*(cell+gap),y,cell,height);
            y+=height+gap;
        }
        private void Action(Button action,float x,float top,float w,float h){Put((RectTransform)action.transform,x,top,w,h);DialoguePanel.FitCaption(action,font);}
        private static void Put(RectTransform rect,float x,float top,float w,float h)=>DialoguePanel.Place(rect,new Vector2(.5f,1),new Vector2(x,-top-h*.5f),new Vector2(w,h));
        private static void StretchViewport(ScrollRect view)
        {view.viewport.anchorMin=Vector2.zero;view.viewport.anchorMax=Vector2.one;view.viewport.offsetMin=view.viewport.offsetMax=Vector2.zero;}
        private void Hide(params string[] names){foreach(string name in names){var child=frame.body.Find(name);if(child!=null)child.gameObject.SetActive(false);}}
    }
}
