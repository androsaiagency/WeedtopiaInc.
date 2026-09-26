using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Weedtopia.Domain;

namespace Weedtopia.Unity
{
    /// <summary>Pure presentation adapter. All fixed views and artwork are serialized by explicit authoring.</summary>
    public sealed class ProductionPresentation : MonoBehaviour
    {
        public PresentationArt art;
        public CharacterActor[] characters;
        public PresentationRoute[] routes;
        public Text[] currencyValues;
        public Image introPortrait, questPortrait, workerPortrait, workerRoleIcon;
        public Image workerRarity;
        public Text workerName;
        public RectTransform workerReveal;
        public GameObject[] contextualActions;
        public RectTransform detailPanel;
        public AudioSource ambience;
        public AudioSource eventAudio;
        public AudioClip upgradeSound,deliverySound;
        public bool releaseRequested;
        public bool explicitBuildingApproval;
        private GameRoot root;
        private float revealAt=-10;
        private int previousDeliveries=-1,previousLevels=-1;
        public void Initialize(GameRoot value)
        {
            root=value;
            if(releaseRequested&&!explicitBuildingApproval)throw new InvalidOperationException("Production startup blocked: explicit building art approval is missing.");
            foreach(var actor in characters)actor.Initialize(root);
        }
        public void Refresh()
        {
            if(root.Session==null)return;
            var s=root.Session.State;
            int levels=s.buildings.Sum(x=>x.level);
            if(eventAudio!=null)
            {
                if(previousLevels>=0&&levels>previousLevels&&upgradeSound!=null)eventAudio.PlayOneShot(upgradeSound,.7f);
                else if(previousDeliveries>=0&&s.City.stats.deliveries>previousDeliveries&&deliverySound!=null)eventAudio.PlayOneShot(deliverySound,.45f);
            }
            previousLevels=levels;previousDeliveries=s.City.stats.deliveries;
            currencyValues[0].text=s.money.ToString();currencyValues[1].text=s.grinders.ToString();currencyValues[2].text=s.cards.ToString();currencyValues[3].text=root.Session.Simulation.Residents(s.activeCity).ToString();
            var q=root.Session.Progression.Current;
            string speaker=(q?.speaker??"rocko").ToLowerInvariant();
            if(!new[]{"rocko","mila","jaro","nia"}.Contains(speaker))speaker="rocko";
            questPortrait.sprite=art.Get("portrait_"+speaker);
            var b=s.Find(root.SelectedId);var kind=root.Definitions.Building(b.family).kind;
            bool[] visible={kind!=BuildingKind.TownHall&&kind!=BuildingKind.Plot,b.buildingSeconds>0,kind==BuildingKind.Grow,kind==BuildingKind.Processing||kind==BuildingKind.Butter||kind==BuildingKind.Shop||kind==BuildingKind.Bakery,true,true,kind==BuildingKind.Plot,kind==BuildingKind.Plot};
            float y=-343;
            for(int i=0;i<contextualActions.Length;i++)
            {
                contextualActions[i].SetActive(visible[i]);
                // Keep hidden controls at their authored tested coordinates.
                if(visible[i]){contextualActions[i].GetComponent<RectTransform>().anchoredPosition=new Vector2(0,y);y-=52;}
            }
            detailPanel.sizeDelta=new Vector2(detailPanel.sizeDelta.x,Mathf.Max(420,-y+8));
            foreach(var route in routes)
            {
                var def=root.Definitions.routes.First(x=>x.id==route.routeId);
                var source=s.Find(def.from);
                route.Present(!root.Session.Paused&&source.level>0&&root.Session.Workers.Automated(source)&&string.IsNullOrEmpty(source.bottleneck),source.level>0&&source.Total>0,.1f);
            }
        }
        public void PaintWorker(WorkerState worker,bool reveal=false)
        {
            if(worker==null){workerPortrait.gameObject.SetActive(false);workerRoleIcon.gameObject.SetActive(false);workerName.text="Dein Team";return;}
            workerPortrait.gameObject.SetActive(true);workerRoleIcon.gameObject.SetActive(true);
            workerPortrait.sprite=art.Get("portrait_"+PresentationArt.CharacterForRole(worker.role));
            workerRoleIcon.sprite=art.Get(PresentationArt.IconForRole(worker.role));
            Color[] colors={new Color(.7f,.69f,.54f),new Color(.38f,.73f,.6f),new Color(.35f,.65f,.92f),new Color(.73f,.43f,.92f),new Color(1,.72f,.26f)};
            workerRarity.color=colors[Mathf.Clamp(worker.rarity,0,4)];
            workerName.text=root.Definitions.Text(root.Definitions.Role(worker.role).nameKey).Replace("-Manager","\nManager");
            if(reveal)revealAt=Time.unscaledTime;
        }
        public void RevealWorker(){revealAt=Time.unscaledTime;}
        private void Update()
        {
            if(workerReveal!=null){float t=Mathf.Clamp01((Time.unscaledTime-revealAt)/.55f);workerReveal.localScale=Vector3.one*(1+.08f*Mathf.Sin(t*Mathf.PI));}
        }
        public void DecorateRow(Button row,string icon,Color? tint=null)
        {
            var image=row.transform.Find("RowIcon")?.GetComponent<Image>();
            if(image!=null){image.sprite=art.Get(icon);image.color=tint??Color.white;}
        }
        public PresentationRoute Route(string id)=>routes.FirstOrDefault(x=>x.routeId==id);
    }
}
