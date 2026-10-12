using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Weedtopia.Unity
{
    /// <summary>Eight visible entries with a continuous list; item actions have a separate scrollable detail surface.</summary>
    public sealed class InventoryPanelLayout : MonoBehaviour
    {
        public RewardCollectionUi view;
        public ModalFrame frame;
        public ScrollRect grid, archive, detail;
        public Button back;
        private bool detailOpen;
        private float pixels;
        private readonly List<TileMetrics> tileMetrics=new List<TileMetrics>();
        public int VisibleCapacity { get; private set; }
        public void RevealDetails(){detailOpen=true;detail.verticalNormalizedPosition=1;Fit(pixels);}
        public void ResetList(){detailOpen=false;grid.verticalNormalizedPosition=archive.verticalNormalizedPosition=1;}
        private void Start(){back.onClick.AddListener(()=>{detailOpen=false;view.ClearSelection();Fit(pixels);});}
        public void Fit(float scale)
        {
            pixels=Mathf.Max(.01f,scale);float gap=Mathf.Max(12,8/pixels),font=Mathf.Max(22,14/pixels),action=Mathf.Max(68,44/pixels);
            float w=frame.viewport.rect.width,h=frame.viewport.rect.height;
            bool compact=h*pixels<420;if(compact){gap=6/pixels;action=40/pixels;}
            Stretch(frame.body);frame.body.anchoredPosition=Vector2.zero;
            foreach(var area in new[]{view.inventoryArea,view.archiveArea})Stretch((RectTransform)area.transform);
            view.description.gameObject.SetActive(false);
            Row(new[]{view.inventoryTab,view.achievementsTab,view.historyTab},frame.body,0,0,w,action,gap,font);
            view.balance.gameObject.SetActive(!compact);
            float balanceHeight=compact?0:Mathf.Max(font*1.6f,22/pixels);Label(view.balance,0,action+gap,w,balanceHeight,font);
            var wheel=GetComponent<ModulePanelLayout>().inventoryWheel;
            float footer=wheel!=null?action+gap:0;
            if(wheel!=null){Place((RectTransform)wheel.transform,0,h-action,w,action);DialoguePanel.FitCaption(wheel,font);}
            float top=action+gap+(compact?0:gap+balanceHeight);
            bool inventory=view.SelectedTab==0;
            bool wide=w*pixels>=620;
            float detailsWidth=wide?w*.37f:0,listWidth=wide?w-detailsWidth-gap:w;
            float listX=wide?-w*.5f+listWidth*.5f:0;
            if(inventory)
            {
                Row(view.categoryButtons,(RectTransform)view.inventoryArea.transform,listX,top,listWidth,action,gap,font*.92f);
                top+=action+gap;
            }
            float listHeight=Mathf.Max(1,h-top-footer);
            Place((RectTransform)grid.transform,listX,top,listWidth,listHeight);
            Place((RectTransform)archive.transform,0,top,w,listHeight);
            bool overlay=inventory&&!wide&&detailOpen&&view.details.SelectedItem!=null;
            grid.gameObject.SetActive(inventory&&!overlay);archive.gameObject.SetActive(!inventory);
            detail.gameObject.SetActive(inventory&&(wide||overlay));back.gameObject.SetActive(overlay);
            if(overlay){Place((RectTransform)back.transform,0,h-action,w,action);DialoguePanel.FitCaption(back,font);if(wheel!=null)wheel.gameObject.SetActive(false);}
            else if(wheel!=null)wheel.gameObject.SetActive(true);
            float detailX=wide?w*.5f-detailsWidth*.5f:0,detailW=wide?detailsWidth:w;
            Place((RectTransform)detail.transform,detailX,top,detailW,listHeight);
            int columns=listWidth*pixels>=360?4:2,rows=8/columns;
            float cell=(listWidth-gap*(columns-1)-10/pixels)/columns;
            float tileHeight=Mathf.Min(Mathf.Max(48/pixels,(listHeight-gap*(rows-1))/rows),148/pixels);
            var tiles=view.tiles.Where(t=>t.gameObject.activeSelf).ToArray();
            tileMetrics.Clear();
            foreach(var tile in tiles)
            {
                var metrics=MeasureTile(tile,cell,font);tileMetrics.Add(metrics);
                // Keep readable text and icon/badge space even when the viewport cannot show two full rows.
                tileHeight=Mathf.Max(tileHeight,metrics.height);
            }
            float contentHeight=Mathf.Max(listHeight,Mathf.Ceil(tiles.Length/(float)columns)*(tileHeight+gap)-gap);
            TopContent(grid.content,listWidth,contentHeight);
            for(int i=0;i<tiles.Length;i++)
            {
                var tile=tiles[i];var metrics=tileMetrics[i];
                Place((RectTransform)tile.transform,-listWidth*.5f+cell*.5f+i%columns*(cell+gap),i/columns*(tileHeight+gap),cell,tileHeight);
                LayoutTile(tile,cell,tileHeight,metrics);
            }
            VisibleCapacity=columns*rows;
            LayoutDetails(detailW,font,gap,action);
            LayoutArchive(w,listHeight,font,gap);
            foreach(var scroll in new[]{grid,archive,detail})if(scroll.verticalScrollbar!=null)
            {
                var bar=(RectTransform)scroll.verticalScrollbar.transform;bar.anchorMin=new Vector2(1,0);bar.anchorMax=Vector2.one;bar.offsetMin=new Vector2(-6/pixels,0);bar.offsetMax=Vector2.zero;
                // The shared sliced skin needs smaller corners on a six-display-unit handle.
                scroll.verticalScrollbar.handleRect.GetComponent<Image>().pixelsPerUnitMultiplier=Mathf.Max(16,4*pixels);
            }
            Label(view.emptyLabel,0,top+listHeight*.4f,w,80/pixels,font);
            frame.body.Find("TopRule")?.gameObject.SetActive(false);frame.body.Find("Divider")?.gameObject.SetActive(false);
        }
        private struct TileMetrics
        {
            public float height,titleWidth,titleHeight,iconSize,headerHeight,badgeWidth,badgeHeight;
            public bool badge,stackedHeader;
        }
        private TileMetrics MeasureTile(InventoryTileView tile,float width,float font)
        {
            float inset=8/pixels,headerWidth=Mathf.Max(1,width-2*inset),gap=6/pixels;
            tile.title.fontSize=Mathf.CeilToInt(font);tile.title.resizeTextForBestFit=false;
            tile.title.alignment=TextAnchor.UpperCenter;
            tile.title.horizontalOverflow=HorizontalWrapMode.Wrap;tile.title.verticalOverflow=VerticalWrapMode.Truncate;
            tile.quantity.fontSize=Mathf.RoundToInt(font*.9f);tile.quantity.resizeTextForBestFit=false;
            tile.quantity.alignment=TextAnchor.MiddleCenter;
            tile.quantity.horizontalOverflow=HorizontalWrapMode.Wrap;tile.quantity.verticalOverflow=VerticalWrapMode.Truncate;
            float badgeWidth=Mathf.Min(headerWidth,Mathf.Max(font*2.6f,
                tile.quantity.cachedTextGeneratorForLayout.GetPreferredWidth(tile.quantity.text,tile.quantity.GetGenerationSettings(Vector2.zero))/tile.quantity.pixelsPerUnit+12/pixels));
            float badgeHeight=Mathf.Max(font*1.4f,DialoguePanel.PreferredHeight(tile.quantity,Mathf.Max(1,badgeWidth-8/pixels)));
            float iconSize=Mathf.Min(headerWidth,Mathf.Min(width*.42f,32/pixels));
            bool badge=tile.quantityBadge.gameObject.activeSelf&&!string.IsNullOrEmpty(tile.quantity.text);
            bool stacked=badge&&headerWidth<iconSize+badgeWidth+gap;
            float headerHeight=stacked?badgeHeight+gap+iconSize:Mathf.Max(iconSize,badge?badgeHeight:0);
            float titleHeight=Mathf.Max(font*1.4f,DialoguePanel.PreferredHeight(tile.title,headerWidth));
            return new TileMetrics{height=6/pixels+headerHeight+gap+titleHeight+inset,
                titleWidth=headerWidth,titleHeight=titleHeight,iconSize=iconSize,headerHeight=headerHeight,
                badgeWidth=badgeWidth,badgeHeight=badgeHeight,badge=badge,stackedHeader=stacked};
        }
        private void LayoutTile(InventoryTileView tile,float width,float height,TileMetrics metrics)
        {
            float inset=8/pixels,top=6/pixels,gap=6/pixels;
            float badgeTop=top+(metrics.stackedHeader?0:(metrics.headerHeight-metrics.badgeHeight)*.5f);
            float iconTop=top+(metrics.stackedHeader?metrics.badgeHeight+gap:(metrics.headerHeight-metrics.iconSize)*.5f);
            float iconX=metrics.badge&&!metrics.stackedHeader?-width*.5f+inset+metrics.iconSize*.5f:0;
            Place((RectTransform)tile.icon.transform.parent,iconX,iconTop,metrics.iconSize,metrics.iconSize);Stretch(tile.icon.rectTransform);
            if(tile.Item?.kind==Weedtopia.Domain.RewardKind.Item&&tile.Item.key.StartsWith("build_"))
                tile.icon.rectTransform.anchoredPosition=new Vector2(metrics.iconSize*.15f,0);
            float badgeX=width*.5f-inset-metrics.badgeWidth*.5f;
            Place(tile.quantityBadge.rectTransform,badgeX,badgeTop,metrics.badgeWidth,metrics.badgeHeight);
            Place(tile.quantity.rectTransform,badgeX,badgeTop,metrics.badgeWidth,metrics.badgeHeight);
            Place(tile.title.rectTransform,0,top+metrics.headerHeight+gap,metrics.titleWidth,metrics.titleHeight);
            tile.tag.gameObject.SetActive(false);Stretch(tile.selection.rectTransform);
            Place(tile.accent.rectTransform,0,height-4/pixels,width-2*inset,2/pixels);
        }
        private void LayoutDetails(float w,float font,float gap,float action)
        {
            var d=view.details;float y=gap;
            foreach(string name in new[]{"IconHalo","Accent"})d.transform.Find(name)?.gameObject.SetActive(false);
            var r=(RectTransform)d.transform;TopContent(r,w,detail.viewport.rect.height);
            float icon=font*4;Place((RectTransform)d.icon.transform.parent,0,y,icon,icon);Stretch(d.icon.rectTransform);
            if(d.SelectedItem?.kind==Weedtopia.Domain.RewardKind.Item&&d.SelectedItem.key.StartsWith("build_"))d.icon.rectTransform.anchoredPosition=new Vector2(icon*.15f,0);
            y+=icon+gap;
            foreach(var label in new[]{d.title,d.metadata,d.description,d.effect,d.choiceHeading,d.choiceLabel})
            {
                if(string.IsNullOrEmpty(label.text))continue;
                label.fontSize=Mathf.RoundToInt(font);label.horizontalOverflow=HorizontalWrapMode.Wrap;label.verticalOverflow=VerticalWrapMode.Truncate;
                float height=Mathf.Max(font*1.4f,DialoguePanel.PreferredHeight(label,w-2*gap));Label(label,0,y,w-2*gap,height,font);y+=height+gap;
            }
            foreach(var row in new[]{new[]{d.previousChoice,d.nextChoice},new[]{d.action},new[]{d.split},new[]{d.cancel}})
            {
                var visible=row.Where(b=>b.gameObject.activeSelf).ToArray();if(visible.Length==0)continue;
                float height=action;foreach(var b in visible){DialoguePanel.FitCaption(b,font);height=Mathf.Max(height,DialoguePanel.PreferredHeight(b.GetComponentInChildren<Text>(),(w-2*gap)/visible.Length-32)+16/pixels);}
                Row(visible,r,0,y,w-2*gap,height,gap,font);y+=height+gap;
            }
            TopContent(r,w,Mathf.Max(y+gap,detail.viewport.rect.height));
        }
        private void LayoutArchive(float w,float h,float font,float gap)
        {
            var cards=view.cards.Where(c=>c.gameObject.activeSelf).ToArray();float top=0;
            foreach(var c in cards)
            {
                float icon=font*3;
                foreach(var label in new[]{c.title,c.detail,c.quantity})label.fontSize=Mathf.RoundToInt(font);
                float textW=w-icon-3*gap,x=icon*.5f,y=gap;
                foreach(var label in new[]{c.title,c.quantity,c.detail})
                {float height=DialoguePanel.PreferredHeight(label,textW);Label(label,x,y,textW,height,font);y+=height+gap;}
                float cardHeight=Mathf.Max(font*7,y);Place((RectTransform)c.transform,0,top,w,cardHeight);
                Place((RectTransform)c.icon.transform.parent,-w*.5f+icon*.5f+gap,gap,icon,icon);
                top+=cardHeight+gap;
            }
            TopContent(archive.content,w,Mathf.Max(h,top));
        }
        private static void Row(Button[] actions,RectTransform parent,float x,float top,float w,float h,float gap,float font)
        {
            float cell=(w-gap*(actions.Length-1))/actions.Length;
            for(int i=0;i<actions.Length;i++){Place((RectTransform)actions[i].transform,x-w*.5f+cell*.5f+i*(cell+gap),top,cell,h);DialoguePanel.FitCaption(actions[i],font);}
        }
        private static void Label(Text label,float x,float top,float w,float h,float font){Place(label.rectTransform,x,top,w,h);label.fontSize=Mathf.RoundToInt(font);}
        private static void TopContent(RectTransform r,float w,float h){r.anchorMin=r.anchorMax=new Vector2(.5f,1);r.pivot=new Vector2(.5f,1);r.localScale=Vector3.one;r.sizeDelta=new Vector2(w,h);r.anchoredPosition=new Vector2(0,r.anchoredPosition.y);}
        private static void Stretch(RectTransform r){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;r.localScale=Vector3.one;}
        private static void Place(RectTransform r,float x,float top,float w,float h)=>DialoguePanel.Place(r,new Vector2(.5f,1),new Vector2(x,-top-h*.5f),new Vector2(w,h));
    }
}
