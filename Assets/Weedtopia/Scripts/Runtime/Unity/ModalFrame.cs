using UnityEngine;
using UnityEngine.UI;
using Weedtopia.Domain;

namespace Weedtopia.Unity
{
    /// <summary>Authored header and viewport isolate legacy window content from the close action and frame corners.</summary>
    public sealed class ModalFrame : MonoBehaviour
    {
        public RectTransform body, viewport;
        public Text heading;
        public Text headingSource;
        public Vector2 authoredSize;
        public Vector2 authoredPivot;
        public bool fillAvailable;
        public void Fit(RectTransform safe,float pixels)
        {
            var rect=(RectTransform)transform;float margin=Mathf.Max(24,12/pixels),inset=Mathf.Max(32,18/pixels),header=Mathf.Max(120,72/pixels);
            if(headingSource!=null)
            {
                // An archived heading source is inactive and cannot receive LocalizedLabel.OnEnable/Changed.
                var localization=headingSource.GetComponent<LocalizedLabel>();
                if(localization!=null&&GameText.Current!=null)headingSource.text=GameText.Get(localization.Key);
                heading.text=headingSource.text;
            }
            var confirmation=GetComponent<PremiumConfirmationLayout>();
            rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);rect.localScale=Vector3.one;rect.anchoredPosition=Vector2.zero;
            float width=Mathf.Min(confirmation==null?1500:Mathf.Max(1500,700/pixels),safe.rect.width-2*margin);
            bool shop=GetComponent<ShopUi>()!=null;
            if(shop)width=safe.rect.width-2*margin;
            if((GetComponent<WheelPanelLayout>()!=null||GetComponent<ShopUi>()!=null)&&safe.rect.height*pixels<600&&safe.rect.width/safe.rect.height>1.25f)width=safe.rect.width-2*margin;
            float naturalHeight=authoredSize.y*Mathf.Min(1,(width-2*inset)/authoredSize.x)+header+inset;
            rect.sizeDelta=new Vector2(width,shop?safe.rect.height-2*margin:Mathf.Min(fillAvailable?1120:naturalHeight,safe.rect.height-2*margin));
            float font=Mathf.Max(30,20/pixels),close=confirmation==null?Mathf.Max(76,48/pixels):0;
            heading.fontSize=Mathf.RoundToInt(font);float textWidth=rect.rect.width-2*inset-close,titleHeight=Mathf.Max(font*1.5f,DialoguePanel.PreferredHeight(heading,textWidth));
            header=Mathf.Max(header,titleHeight+2*inset);DialoguePanel.Place(heading.rectTransform,new Vector2(.5f,1),new Vector2(-close*.5f,-inset-titleHeight*.5f),new Vector2(textWidth,titleHeight));
            if(confirmation!=null)
                rect.sizeDelta=new Vector2(width,Mathf.Min(safe.rect.height-2*margin,Mathf.Max(rect.sizeDelta.y,confirmation.PreferredHeight(width-2*inset,pixels)+header+inset)));
            viewport.anchorMin=Vector2.zero;viewport.anchorMax=Vector2.one;viewport.offsetMin=new Vector2(inset,inset);viewport.offsetMax=new Vector2(-inset,-header);
            if(confirmation!=null){confirmation.Fit(pixels);return;}
            if(GetComponent<ModulePanelLayout>()!=null||GetComponent<ShopPanelLayout>()!=null)return;
            float scale=Mathf.Min(viewport.rect.width/authoredSize.x,viewport.rect.height/authoredSize.y);
            body.anchorMin=body.anchorMax=new Vector2(.5f,.5f);body.pivot=authoredPivot;body.sizeDelta=authoredSize;body.localScale=Vector3.one*Mathf.Min(1,scale);
            body.anchoredPosition=new Vector2((authoredPivot.x-.5f)*authoredSize.x,(authoredPivot.y-.5f)*authoredSize.y)*body.localScale.x;
        }
    }
}
