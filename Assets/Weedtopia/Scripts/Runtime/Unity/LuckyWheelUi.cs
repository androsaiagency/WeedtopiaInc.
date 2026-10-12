using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Weedtopia.Application;
using Weedtopia.Domain;

namespace Weedtopia.Unity
{
    public sealed class LuckyWheelUi : MonoBehaviour
    {
        public RectTransform disc, rays;
        public LuckyWheelGraphic graphic;
        public LuckyWheelPrizeView[] slots;
        public Button spin, cancel, inventory;
        public Text spinLabel, countdown, remaining, chance, balance, status, itemName, description, costHeading, costBody;
        public Image itemIcon, itemAccent;
        private GameRoot root;
        private WheelOffer offer;
        private WheelSpinResult outcome,lastResult;
        private WheelPrize inspectedPrize;
        private float started, targetAngle;
        private long paintedDay=-1;
        private int paintedSpins=-1;
        private bool priorPaused;
        public bool Spinning { get; private set; }
        public bool Confirming => offer != null;
        public void Initialize(GameRoot game)
        {
            root=game;spin.onClick.AddListener(Spin);cancel.onClick.AddListener(CancelOffer);
            inventory.onClick.AddListener(()=>root.Ui.ShowCollection());
        }
        public void Open()
        {
            CancelOffer();lastResult=null;paintedDay=-1;paintedSpins=-1;status.text=T("wheel.prize_hint");Refresh();
        }
        public void RefreshLanguage()
        {
            if(root?.Session==null)return;
            // A language change keeps the domain offer and animation; only their visible captions are repainted.
            Refresh();
            if(!gameObject.activeInHierarchy)return;
            PaintBoardText();
            foreach(var slot in slots.Where(s=>s.gameObject.activeSelf&&s.Prize!=null))
            {
                slot.RefreshLanguage(root);
            }
            if(inspectedPrize!=null)PaintPrize(inspectedPrize);
            if(Spinning){status.text=T("wheel.spinning");spinLabel.text=T("wheel.spinning");}
            else if(offer!=null)status.text=string.Format(GameText.Current?.Culture ?? System.Globalization.CultureInfo.InvariantCulture,T("wheel.confirm"),offer.Grinders);
            else status.text=lastResult!=null?WonText(lastResult):root.Session.Wheel.Remaining.Length==0?T("wheel.empty_hint"):T("wheel.prize_hint");
        }
        public void Refresh()
        {
            if(root?.Session==null)return;
            try {root.Session.RefreshWheel();}catch(Exception ex) when(ex is IOException||ex is UnauthorizedAccessException){root.Ui.Notify(T("save.error"));return;}
            if(!gameObject.activeInHierarchy)return;
            var service=root.Session.Wheel;var board=root.Session.State.luckyWheel;
            if(board==null)return;
            var until=service.UntilReset;
            countdown.text=string.Format(GameText.Current?.Culture ?? System.Globalization.CultureInfo.InvariantCulture,T("wheel.countdown"),((int)until.TotalHours).ToString("00")+":"+until.Minutes.ToString("00")+":"+until.Seconds.ToString("00"));
            balance.text=string.Format(GameText.Current?.Culture ?? System.Globalization.CultureInfo.InvariantCulture,T("wheel.balance"),root.Session.State.grinders);
            if(Spinning)return;
            if(offer!=null && (board.day!=paintedDay || board.spins!=paintedSpins))CancelOffer();
            if(board.day!=paintedDay || board.spins!=paintedSpins)
            {
                PaintBoard();status.text=service.Remaining.Length==0?T("wheel.empty_hint"):T("wheel.prize_hint");
            }
            int cost=service.NextCost;
            bool enough=root.Session.State.grinders>=cost;
            spin.interactable=service.Remaining.Length>0&&enough;
            spinLabel.text=service.Remaining.Length==0?T("wheel.empty"):offer!=null?string.Format(GameText.Current?.Culture ?? System.Globalization.CultureInfo.InvariantCulture,T("wheel.confirm_button"),offer.Grinders):
                !enough?string.Format(GameText.Current?.Culture ?? System.Globalization.CultureInfo.InvariantCulture,T("wheel.insufficient"),cost):cost==0?T("wheel.free"):string.Format(GameText.Current?.Culture ?? System.Globalization.CultureInfo.InvariantCulture,T("wheel.paid"),cost);
            cancel.gameObject.SetActive(offer!=null);
        }
        private void PaintBoard()
        {
            var board=root.Session.State.luckyWheel;var prizes=root.Session.Wheel.Remaining;
            paintedDay=board.day;paintedSpins=board.spins;disc.localRotation=Quaternion.identity;lastResult=null;
            PaintBoardText();
            graphic.SetSectors(prizes.Select(p=>SectorColor(Array.Find(root.Definitions.items,d=>d.id==p.id).rarity)).ToArray());
            for(int i=0;i<slots.Length;i++)
            {
                slots[i].gameObject.SetActive(i<prizes.Length);
                if(i>=prizes.Length)continue;
                var prize=prizes[i];slots[i].Bind(root,prize,()=>Inspect(prize));
                float angle=(90-i*360f/prizes.Length)*Mathf.Deg2Rad;
                var rect=(RectTransform)slots[i].transform;
                rect.anchoredPosition=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*200;
                slots[i].button.interactable=true;
            }
            if(prizes.Length>0)Inspect(prizes[0]);
            else {itemName.text=T("wheel.empty");description.text=T("wheel.empty_hint");itemIcon.gameObject.SetActive(false);}
        }
        private void PaintBoardText()
        {
            var prizes=root.Session.Wheel.Remaining;
            remaining.text=string.Format(GameText.Current?.Culture ?? System.Globalization.CultureInfo.InvariantCulture,T("wheel.remaining"),prizes.Length);
            chance.text=prizes.Length==0?T("wheel.empty"):string.Format(GameText.Current?.Culture ?? System.Globalization.CultureInfo.InvariantCulture,T("wheel.chance"),(100f/prizes.Length).ToString("0.##",root.Localization.Culture));
        }
        private static Color SectorColor(int rarity)=>rarity==4?new Color(.59f,.36f,.10f):rarity==3?new Color(.34f,.19f,.47f):
            rarity==2?new Color(.13f,.32f,.44f):rarity==1?new Color(.10f,.35f,.27f):new Color(.11f,.28f,.29f);
        public void Inspect(WheelPrize prize)
        {
            if(Spinning)return;
            inspectedPrize=prize;PaintPrize(prize);
        }
        private void PaintPrize(WheelPrize prize)
        {
            var def=Array.Find(root.Definitions.items,i=>i.id==prize.id);
            itemName.text=T(def.nameKey);itemIcon.gameObject.SetActive(true);itemIcon.sprite=root.Presentation.art.Get(def.icon);
            itemIcon.rectTransform.anchoredPosition=def.icon=="time"?new Vector2(itemIcon.rectTransform.sizeDelta.x*.15f,0):Vector2.zero;
            itemAccent.color=InventoryText.Rarity(def.rarity);
            description.text=string.Format(GameText.Current?.Culture ?? System.Globalization.CultureInfo.InvariantCulture,T("wheel.details"),T("rarity."+def.rarity),T("inventory.category."+def.category),T(def.descriptionKey));
        }
        public void Spin()
        {
            if(Spinning || root.Session==null)return;
            try {root.Session.RefreshWheel();}catch(Exception ex) when(ex is IOException||ex is UnauthorizedAccessException){root.Ui.Notify(T("save.error"));return;}
            Refresh();
            if(offer==null)
            {
                var failure=root.Session.Wheel.Offer(out offer);
                if(failure!=Failure.None){root.Ui.Notify(T("failure."+failure));Refresh();return;}
                if(offer.Grinders>0){status.text=string.Format(GameText.Current?.Culture ?? System.Globalization.CultureInfo.InvariantCulture,T("wheel.confirm"),offer.Grinders);Refresh();return;}
            }
            var token=offer.Token;offer=null;
            // The save is complete before the first animation frame. Presentation cannot award a second item.
            Spinning=true;
            var result=root.SpinWheel(token,out outcome);
            if(result!=Failure.None){Spinning=false;Refresh();return;}
            priorPaused=root.Session.Paused;root.Session.Paused=true;
            spin.interactable=false;cancel.gameObject.SetActive(false);inventory.interactable=false;
            foreach(var slot in slots)slot.button.interactable=false;
            status.text=T("wheel.spinning");spinLabel.text=T("wheel.spinning");
            targetAngle=1800+outcome.Index*360f/slots.Count(s=>s.gameObject.activeSelf);
            started=Time.unscaledTime;
        }
        private void Update()
        {
            if(!Spinning)return;
            float t=Mathf.Clamp01((Time.unscaledTime-started)/4.2f);
            float angle=targetAngle*(1-Mathf.Pow(1-t,4));disc.localRotation=Quaternion.Euler(0,0,angle);
            foreach(var slot in slots)slot.content.localRotation=Quaternion.Euler(0,0,-angle);
            rays.localRotation=Quaternion.Euler(0,0,Time.unscaledTime*4);
            if(t>=1)CompleteSpin(true);
        }
        private void CompleteSpin(bool celebrate)
        {
            if(!Spinning)return;
            var result=outcome;outcome=null;Spinning=false;root.Session.Paused=priorPaused;
            foreach(var slot in slots)slot.content.localRotation=Quaternion.identity;
            inventory.interactable=true;paintedSpins=-1;Refresh();
            lastResult=result;status.text=WonText(result);
            Inspect(new WheelPrize{id=result.Prize.id,count=result.Prize.count});
            if(celebrate)root.Ui.Rewards?.EnqueueAfter(result.ReceiptSequence-1);
        }
        private string WonText(WheelSpinResult result)=>T("wheel.won")+" "+string.Format(GameText.Current?.Culture ?? System.Globalization.CultureInfo.InvariantCulture,T("wheel.secured"),result.Prize.count,
                T(Array.Find(root.Definitions.items,d=>d.id==result.Prize.id).shortNameKey).Replace("\n"," "));
        private void CancelOffer(){root?.Session?.Wheel.Cancel();offer=null;cancel.gameObject.SetActive(false);}
        private void OnDisable()
        {
            CancelOffer();
            // Closing the screen skips only the animation; the already saved reward remains in inventory/history.
            CompleteSpin(false);
        }
        private string T(string key)=>root.Definitions.Text(key);
    }
}
