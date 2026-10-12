using UnityEngine;
using UnityEngine.UI;

namespace Weedtopia.Unity
{
    /// <summary>Responsive authored roster and detail column. On narrow screens the roster sits above the details.</summary>
    public sealed class TeamPanelLayout : MonoBehaviour
    {
        public ModalFrame frame;
        public RectTransform roster, cardsCounter, reveal;
        public ScrollRect detailsScroll;
        public RectTransform detailsContent;
        public Dropdown filter;
        public Button inventory, spin, remove, improve, rank;
        public Text hint, detail;
        public void Fit(GameRoot root,float pixels)
        {
            var body=frame.body;body.localScale=Vector3.one;body.anchorMin=body.anchorMax=body.pivot=new Vector2(.5f,.5f);body.anchoredPosition=Vector2.zero;body.sizeDelta=frame.viewport.rect.size;
            float w=body.rect.width,h=body.rect.height,button=Mathf.Max(78,48/pixels),font=Mathf.Max(25,16/pixels),gap=Mathf.Max(20,10/pixels);
            bool wide=w*pixels>=540;
            Place((RectTransform)filter.transform,-w*.18f,-button*.5f,w*.61f,button);Place((RectTransform)inventory.transform,w*.32f,-button*.5f,w*.35f,button);DialoguePanel.FitCaption(inventory,font);
            foreach(var label in filter.GetComponentsInChildren<Text>(true))label.fontSize=Mathf.RoundToInt(font);
            float top=button+gap,leftWidth=wide?(w-gap)*.42f:w,listHeight=wide?h-top:Mathf.Min(h*.36f,260/pixels);
            Place(roster,wide?-(w-leftWidth)*.5f:0,-top-listHeight*.5f,leftWidth,listHeight);
            float rightWidth=wide?w-leftWidth-gap:w,rightTop=wide?top:top+listHeight+gap;
            Place((RectTransform)detailsScroll.transform,wide?(w-rightWidth)*.5f:0,-rightTop-(h-rightTop)*.5f,rightWidth,h-rightTop);
            foreach(var row in roster.GetComponentsInChildren<Button>())
            {
                var label=row.GetComponentInChildren<Text>();label.fontSize=Mathf.RoundToInt(font);
                float iconSize=Mathf.Max(76,44/pixels),inset=Mathf.Max(14,8/pixels),width=leftWidth-iconSize-3*inset;
                float height=Mathf.Max(110,DialoguePanel.PreferredHeight(label,width)+2*inset);
                var element=row.GetComponent<LayoutElement>();if(element!=null)element.preferredHeight=height;
                label.rectTransform.anchorMin=Vector2.zero;label.rectTransform.anchorMax=Vector2.one;label.rectTransform.offsetMin=new Vector2(iconSize+2*inset,inset);label.rectTransform.offsetMax=new Vector2(-inset,-inset);
                var icon=row.transform.Find("RowIcon")?.GetComponent<RectTransform>();if(icon!=null)DialoguePanel.Place(icon,new Vector2(0,.5f),new Vector2(iconSize*.5f+inset,0),new Vector2(iconSize,height-2*inset));
            }
            float y=gap;
            Place(cardsCounter,0,-y-button*.4f,Mathf.Min(rightWidth,400),button*.8f);y+=button*.8f+gap;
            foreach(var text in cardsCounter.GetComponentsInChildren<Text>())text.fontSize=Mathf.RoundToInt(text.name=="Value"?font*1.2f:font*.75f);
            Put(spin,rightWidth,button,font,ref y,gap);
            hint.fontSize=Mathf.RoundToInt(font*.9f);float hintHeight=DialoguePanel.PreferredHeight(hint,rightWidth-20);Place(hint.rectTransform,0,-y-hintHeight*.5f,rightWidth-20,hintHeight);y+=hintHeight+gap;
            float revealHeight=Mathf.Max(220,130/pixels);Place(reveal,0,-y-revealHeight*.5f,rightWidth-20,revealHeight);y+=revealHeight+gap;
            var presentation=root.Presentation;float portraitWidth=revealHeight*.65f;
            DialoguePanel.Place(presentation.workerPortrait.rectTransform,new Vector2(0,.5f),new Vector2(portraitWidth*.5f,0),new Vector2(portraitWidth,revealHeight-16));
            DialoguePanel.Place(presentation.workerName.rectTransform,new Vector2(.5f,.5f),new Vector2(portraitWidth*.35f,0),new Vector2(rightWidth-portraitWidth-32,revealHeight-24));presentation.workerName.fontSize=Mathf.RoundToInt(font);
            presentation.workerRarity.rectTransform.anchorMin=Vector2.zero;presentation.workerRarity.rectTransform.anchorMax=Vector2.one;presentation.workerRarity.rectTransform.offsetMin=presentation.workerRarity.rectTransform.offsetMax=Vector2.zero;
            DialoguePanel.Place(presentation.workerRoleIcon.rectTransform,new Vector2(1,1),new Vector2(-font,-font),Vector2.one*font*1.2f);
            detail.fontSize=Mathf.RoundToInt(font);float detailHeight=DialoguePanel.PreferredHeight(detail,rightWidth-20);Place(detail.rectTransform,0,-y-detailHeight*.5f,rightWidth-20,detailHeight);y+=detailHeight+gap;
            foreach(var action in new[]{remove,improve,rank})Put(action,rightWidth-20,button,font,ref y,gap);
            detailsContent.sizeDelta=new Vector2(0,y+gap);
        }
        private static void Put(Button action,float width,float height,float font,ref float y,float gap)
        {Place((RectTransform)action.transform,0,-y-height*.5f,width,height);DialoguePanel.FitCaption(action,font);y+=height+gap;}
        private static void Place(RectTransform rect,float x,float y,float w,float h)=>DialoguePanel.Place(rect,new Vector2(.5f,1),new Vector2(x,y),new Vector2(w,h));
    }
}
