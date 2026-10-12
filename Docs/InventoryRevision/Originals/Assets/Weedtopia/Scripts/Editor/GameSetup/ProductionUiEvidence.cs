using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Weedtopia.Unity;

namespace Weedtopia.Editor.GameSetup
{
    [Serializable] public sealed class UiOverflowIssue {public string screen,objectPath,reason;public int width,height;}
    [Serializable] public sealed class UiOverflowReceipt {public string utc,fingerprint;public bool passed;public string[] resolutions;public UiOverflowIssue[] issues;}
    public static class ProductionUiEvidence
    {
        private static readonly List<UiOverflowIssue> issues=new List<UiOverflowIssue>();
        private static readonly HashSet<string> resolutions=new HashSet<string>();
        private static string PathOf(Transform tr)=>tr.parent==null?tr.name:PathOf(tr.parent)+"/"+tr.name;
        public static void Inspect(GameUi ui,string screen)
        {
            if(ui==null||!ui.gameObject.activeInHierarchy)return;Canvas.ForceUpdateCanvases();resolutions.Add(Screen.width+"x"+Screen.height);
            void Issue(Transform tr,string reason)=>issues.Add(new UiOverflowIssue{screen=screen,objectPath=PathOf(tr),reason=reason,width=Screen.width,height=Screen.height});
            foreach(var text in ui.GetComponentsInChildren<Text>().Where(x=>x.enabled&&!string.IsNullOrWhiteSpace(x.text)))
            {
                var size=text.rectTransform.rect.size;float preferred=text.cachedTextGeneratorForLayout.GetPreferredHeight(text.text,text.GetGenerationSettings(size))/text.pixelsPerUnit;
                if(preferred>size.y+2)Issue(text.transform,"Text requires "+preferred.ToString("F1")+"px, rect height "+size.y.ToString("F1"));
                if(text.text.StartsWith("[speaker.")||text.text.Contains("[ui.")||text.text.Contains("Entwicklungsvisual"))Issue(text.transform,"Unresolved content or debug label: "+text.text);
            }
            var corners=new Vector3[4];var safe=Screen.safeArea;
            foreach(var selectable in ui.GetComponentsInChildren<Selectable>().Where(x=>x.enabled))
            {
                if(selectable.GetComponentInParent<ScrollRect>()!=null)continue;
                selectable.GetComponent<RectTransform>().GetWorldCorners(corners);
                if(corners.Min(x=>x.x)<safe.xMin-2||corners.Max(x=>x.x)>safe.xMax+2||corners.Min(x=>x.y)<safe.yMin-2||corners.Max(x=>x.y)>safe.yMax+2)Issue(selectable.transform,"Action outside actual safe area");
            }
        }
        public static void Write(string fingerprint=null)
        {
            bool complete=new[]{"1920x1080","1366x768","2560x1080"}.All(x=>resolutions.Contains(x));
            var result=new UiOverflowReceipt{utc=DateTime.UtcNow.ToString("O"),passed=issues.Count==0&&complete,fingerprint=fingerprint??ProductionVisualPreflight.Fingerprint(),resolutions=resolutions.ToArray(),issues=issues.ToArray()};
            File.WriteAllText("Docs/ProductionFinish/Evidence/ui_overflow.json",JsonUtility.ToJson(result,true));
        }
    }
}
