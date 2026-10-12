using System;
using UnityEngine;
using UnityEngine.UI;

namespace Weedtopia.Unity
{
    [Serializable] public sealed class DialoguePortrait
    {
        public string speaker;
        public Sprite greeting, explaining, pleased;
        public bool mirrorToFaceRight;
    }
    /// <summary>Authored dialogue stage. Portraits and gestures are selected explicitly by the dialogue.</summary>
    public sealed class DialoguePanel : MonoBehaviour
    {
        public RectTransform box;
        public Image portrait;
        public Text speaker, body;
        public RectTransform nameGroup;
        public InputField nameInput;
        public Button next, skip;
        public Button tapScreen;
        public DialoguePortrait[] portraits;
        public string SpeakerId { get; private set; }
        public string Emotion { get; private set; }
        public void Present(string id, string emotion, string title, string text, bool askName=false)
        {
            var entry=Array.Find(portraits,p=>p.speaker==id);
            if(entry==null)throw new InvalidOperationException("Missing dialogue portrait: "+id);
            SpeakerId=id;Emotion=emotion;
            portrait.sprite=emotion=="pleased"?entry.pleased:emotion=="explaining"?entry.explaining:entry.greeting;
            portrait.rectTransform.localScale=new Vector3(entry.mirrorToFaceRight?-1:1,1,1);
            speaker.text=title;body.text=text;
            if(nameGroup!=null)nameGroup.gameObject.SetActive(askName);
            next.gameObject.SetActive(askName);skip.gameObject.SetActive(false);
        }
        public void Fit(RectTransform safe, float pixelsPerUnit)
        {
            var stage=(RectTransform)transform;
            stage.anchorMin=stage.anchorMax=new Vector2(.5f,0);stage.pivot=new Vector2(.5f,0);stage.localScale=Vector3.one;
            float margin=Mathf.Max(24,12/pixelsPerUnit),font=Mathf.Max(28,18/pixelsPerUnit),button=Mathf.Max(68,48/pixelsPerUnit);
            float width=Mathf.Min(1560,safe.rect.width-2*margin),height=safe.rect.height-2*margin;
            stage.anchoredPosition=new Vector2(0,margin);stage.sizeDelta=new Vector2(width,height);
            bool naming=nameGroup!=null&&nameGroup.gameObject.activeSelf;
            float inset=Mathf.Max(40,20/pixelsPerUnit),titleHeight=font*1.5f;
            body.fontSize=Mathf.RoundToInt(font);body.alignment=TextAnchor.UpperLeft;
            float speechHeight=PreferredHeight(body,width-2*inset);
            float actions=naming?2*button+font*1.5f+20:0;
            float boxHeight=Mathf.Min(height*.78f,Mathf.Max((naming?240:118)/pixelsPerUnit,2*inset+titleHeight+actions+speechHeight));
            box.anchorMin=box.anchorMax=new Vector2(.5f,0);box.pivot=new Vector2(.5f,0);box.anchoredPosition=Vector2.zero;box.sizeDelta=new Vector2(width,boxHeight);
            Place(speaker.rectTransform,new Vector2(.5f,1),new Vector2(0,-inset-titleHeight*.5f),new Vector2(width-2*inset,titleHeight));speaker.fontSize=Mathf.RoundToInt(font*1.05f);
            float bodyHeight=boxHeight-2*inset-titleHeight-actions;
            Place(body.rectTransform,new Vector2(.5f,1),new Vector2(0,-inset-titleHeight-bodyHeight*.5f),new Vector2(width-2*inset,bodyHeight));body.fontSize=Mathf.RoundToInt(font);body.alignment=TextAnchor.UpperLeft;
            float nextWidth=Mathf.Min(width-2*inset,480);
            Place((RectTransform)next.transform,new Vector2(.5f,0),new Vector2(width*.5f-nextWidth*.5f-inset,inset+button*.5f),new Vector2(nextWidth,button));Font(next,font);
            if(naming)
            {
                float inputHeight=button, groupHeight=inputHeight+font*1.5f;
                Place(nameGroup,new Vector2(.5f,0),new Vector2(0,inset+button+12+groupHeight*.5f),new Vector2(width-2*inset,groupHeight));
                var caption=nameGroup.Find("NameCaption").GetComponent<Text>();caption.fontSize=Mathf.RoundToInt(font*.85f);
                Place(caption.rectTransform,new Vector2(.5f,1),new Vector2(0,-font*.6f),new Vector2(nameGroup.sizeDelta.x,font*1.3f));
                Place((RectTransform)nameInput.transform,new Vector2(.5f,0),new Vector2(0,inputHeight*.5f),new Vector2(nameGroup.sizeDelta.x,inputHeight));
                foreach(var label in nameInput.GetComponentsInChildren<Text>(true))label.fontSize=Mathf.RoundToInt(font);
            }
            float portraitHeight=Mathf.Min(height-boxHeight+16,Mathf.Max(220,150/pixelsPerUnit));
            portrait.rectTransform.anchorMin=portrait.rectTransform.anchorMax=new Vector2(0,0);portrait.rectTransform.pivot=new Vector2(.5f,0);
            portrait.rectTransform.anchoredPosition=new Vector2(inset+portraitHeight*.43f,boxHeight-4);portrait.rectTransform.sizeDelta=new Vector2(portraitHeight*.86f,portraitHeight);portrait.preserveAspect=true;
            if(tapScreen!=null)
            {
                var canvas=GetComponentInParent<Canvas>();var target=(RectTransform)tapScreen.transform;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(stage,new Vector2(Screen.width*.5f,Screen.height*.5f),null,out var center);
                target.anchorMin=target.anchorMax=target.pivot=new Vector2(.5f,.5f);target.position=stage.TransformPoint(center);
                target.sizeDelta=new Vector2(Screen.width,Screen.height)/canvas.scaleFactor;target.SetAsFirstSibling();
            }
        }
        private static void Font(Button button,float size)=>FitCaption(button,size);
        public static float PreferredHeight(Text label,float width)=>label.cachedTextGeneratorForLayout.GetPreferredHeight(label.text,label.GetGenerationSettings(new Vector2(width,0)))/label.pixelsPerUnit+8;
        public static void FitCaption(Button button,float size)
        {
            var label=button.GetComponentInChildren<Text>(true);label.fontSize=Mathf.RoundToInt(size);label.enabled=true;label.gameObject.SetActive(true);
            label.rectTransform.anchorMin=Vector2.zero;label.rectTransform.anchorMax=Vector2.one;
            label.rectTransform.offsetMin=new Vector2(12,4);label.rectTransform.offsetMax=new Vector2(-12,-4);
        }
        public static void Place(RectTransform rect,Vector2 anchor,Vector2 position,Vector2 size)
        {rect.anchorMin=rect.anchorMax=anchor;rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=position;rect.sizeDelta=size;rect.localScale=Vector3.one;}
    }
}
