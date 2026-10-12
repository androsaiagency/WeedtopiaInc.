using UnityEngine;
using UnityEngine.UI;
using Weedtopia.Application;

namespace Weedtopia.Unity
{
    public sealed class RewardPreviewView : MonoBehaviour
    {
        public Image[] icons;
        public Text[] labels;
        public Text heading, empty;
        private GameRoot root;
        private string lastKey;
        public void Initialize(GameRoot game){root=game;heading.text=root.Definitions.Text("reward.preview");}
        public void Refresh()
        {
            if(root?.Session==null)return;
            var session=root.Session;
            string key=root.Localization.Language+":"+session.State.activeCity+":"+session.Progression.Current?.id+":"+session.CityProject.Current?.id+":"+session.State.City.unlocked;
            if(key==lastKey)return;lastKey=key;
            var list=RewardCatalog.Preview(session);
            empty.gameObject.SetActive(list.Count==0);empty.text=root.Definitions.Text(session.Progression.Current!=null?"reward.preview_no_items":"reward.preview_empty");
            for(int i=0;i<icons.Length;i++)
            {
                icons[i].transform.parent.gameObject.SetActive(i<list.Count);
                if(i>=list.Count)continue;
                icons[i].sprite=root.Presentation.art.Get(RewardText.Icon(list[i], root.Definitions));
                labels[i].text=(RewardText.Quantity(list[i])+" "+RewardText.Name(root.Definitions,list[i],true)).Trim();
            }
        }
    }
}
