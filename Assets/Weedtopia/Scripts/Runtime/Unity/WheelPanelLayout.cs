using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Weedtopia.Unity
{
    /// <summary>A complete wheel and its controls stay inside one viewport at every supported aspect ratio.</summary>
    public sealed class WheelPanelLayout : MonoBehaviour
    {
        public ModalFrame frame;
        public LuckyWheelUi view;
        public RectTransform stage;
        private float font,gap,w,h;
        public void Fit(float pixels)
        {
            pixels=Mathf.Max(.01f,pixels);w=frame.viewport.rect.width;h=frame.viewport.rect.height;
            bool wide=w*pixels>=620||w/h>=1.25f;
            font=wide?Mathf.Max(24,14/pixels):14/pixels;
            bool compact=h*pixels<420;gap=compact?2/pixels:Mathf.Max(12,8/pixels);
            var body=frame.body;body.anchorMin=Vector2.zero;body.anchorMax=Vector2.one;body.offsetMin=body.offsetMax=Vector2.zero;body.localScale=Vector3.one;
            foreach(string name in new[]{"TopRule","DetailsSurface","ItemAccent","Subtitle","ResetHeading","ResetHint"})body.Find(name)?.gameObject.SetActive(false);
            view.description.text=view.description.text.Replace("\n\n","\n");
            view.costBody.text=view.costBody.text.Replace("\n", " ");
            float line=font*1.6f+8,action=Mathf.Max(72,44/pixels);
            if(wide&&!compact)
            {
                // Large UI scale can exhaust logical height even on a physically tall display.
                // Keep all instructions and controls; decorative fields give way first.
                float normalRight=w*.41f-gap;
                float required=font*3+gap+new[]{view.itemName,view.description,view.status,view.costHeading,view.costBody,view.balance}.Sum(label=>Measure(label,normalRight))+2*action+gap;
                if(required>h){compact=true;gap=2/pixels;}
            }
            float left=wide?w*(compact?.50f:.59f):w,right=wide?w-left-gap:w,x=wide?-w*.5f+left*.5f:0;
            Label(view.remaining,x,0,left,line);Label(view.countdown,x,line,left,line);
            float wheelTop=line*2+gap;
            if(wide)
            {
                float diameter=Mathf.Min(left-gap,h-wheelTop-line-gap);
                Wheel(x,wheelTop,diameter);
                Label(view.chance,x,h-line,left,line);
                float rightX=w*.5f-right*.5f,y=0;
                Item(rightX,right,ref y,!compact);view.costHeading.gameObject.SetActive(!compact);
                Text(view.status,rightX,right,ref y);if(!compact)Text(view.costHeading,rightX,right,ref y);Text(view.costBody,rightX,right,ref y);Text(view.balance,rightX,right,ref y);
                float reserved=2*action+gap;
                float top=Mathf.Max(y,h-reserved);
                Action(view.spin,rightX,top,right,action);top+=action+gap;
                if(view.cancel.gameObject.activeSelf)
                {float cell=(right-gap)*.5f;Action(view.cancel,rightX-cell*.5f-gap*.5f,top,cell,action);Action(view.inventory,rightX+cell*.5f+gap*.5f,top,cell,action);}
                else Action(view.inventory,rightX,top,right,action);
            }
            else
            {
                // Measure text and actions first. The wheel receives the actual space that remains.
                float detailsHeight=Measure(view.itemName,w)+Measure(view.description,w)+Measure(view.status,w)+Measure(view.costBody,w)+Measure(view.balance,w)+line;
                float actions=2*action+gap;
                float diameter=Mathf.Min(w-gap,h-wheelTop-detailsHeight-actions-3*gap);
                Wheel(0,wheelTop,Mathf.Max(1,diameter));
                float y=wheelTop+diameter+gap;Label(view.chance,0,y,w,line);y+=line+gap;
                Item(0,w,ref y,false);Text(view.status,0,w,ref y);view.costHeading.gameObject.SetActive(false);
                Text(view.costBody,0,w,ref y);Text(view.balance,0,w,ref y);
                Action(view.spin,0,h-actions,w,action);
                if(view.cancel.gameObject.activeSelf)
                {float cell=(w-gap)*.5f;Action(view.cancel,-cell*.5f-gap*.5f,h-action,cell,action);Action(view.inventory,cell*.5f+gap*.5f,h-action,cell,action);}
                else Action(view.inventory,0,h-action,w,action);
            }
        }
        private void Item(float x,float width,ref float y,bool icon)
        {
            view.costHeading.gameObject.SetActive(true);
            var iconRect=(RectTransform)view.itemIcon.transform.parent;iconRect.gameObject.SetActive(icon);
            if(icon)
            {
                float size=font*3;Place(iconRect,x,y,size,size);
                var image=view.itemIcon.rectTransform;image.anchorMin=Vector2.zero;image.anchorMax=Vector2.one;image.offsetMin=image.offsetMax=Vector2.zero;
                if(view.itemIcon.sprite.name.IndexOf("time",System.StringComparison.OrdinalIgnoreCase)>=0)image.anchoredPosition=new Vector2(size*.15f,0);
                y+=size+gap;
            }
            Text(view.itemName,x,width,ref y);Text(view.description,x,width,ref y);
        }
        private float Measure(Text label,float width)
        {
            if(string.IsNullOrEmpty(label.text))return 0;
            label.fontSize=Mathf.RoundToInt(font);label.horizontalOverflow=HorizontalWrapMode.Wrap;label.verticalOverflow=VerticalWrapMode.Truncate;
            return Mathf.Max(font*1.4f,DialoguePanel.PreferredHeight(label,width))+gap;
        }
        private void Text(Text label,float x,float width,ref float y){float height=Measure(label,width);if(height==0)return;Label(label,x,y,width,height-gap);y+=height;}
        private void Label(Text label,float x,float top,float width,float height){label.fontSize=Mathf.RoundToInt(font);Place(label.rectTransform,x,top,width,height);}
        private void Action(Button action,float x,float top,float width,float height){DialoguePanel.FitCaption(action,font);Place((RectTransform)action.transform,x,top,width,height);}
        private void Wheel(float x,float top,float diameter){Place(stage,x,top,730,730);stage.localScale=Vector3.one*(diameter/730);stage.anchoredPosition=new Vector2(x,-top-diameter*.5f);}
        private static void Place(RectTransform r,float x,float top,float w,float h)=>DialoguePanel.Place(r,new Vector2(.5f,1),new Vector2(x,-top-h*.5f),new Vector2(w,h));
    }
}
