using Weedtopia.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace Weedtopia.Unity
{
    /// <summary>Serialized accessible controls for the authored one-axis district camera.</summary>
    public sealed class DistrictNavigator : MonoBehaviour
    {
        public IsometricCameraRig rig;
        public Scrollbar position;
        public Text caption;
        public Button previous, next, overview;
        [SerializeField] private string cityLabel="Burning Hills";
        private void Awake()
        {
            position.onValueChanged.AddListener(rig.ScrollTo);
            previous.onClick.AddListener(() => rig.StepDistrict(-1));
            next.onClick.AddListener(() => rig.StepDistrict(1));
            overview.onClick.AddListener(rig.ShowOverview);
        }
        private void LateUpdate()
        {
            var range = rig.CurrentDistrict;
            caption.text = range == null ? cityLabel+GameText.Get("presentation.districtnavigator.bersicht.eeb2d828") : (string.IsNullOrEmpty(range.labelKey)?range.label:GameText.Get(range.labelKey));
            position.interactable = range != null;
            previous.interactable = range != null && rig.DistrictFraction > .001f;
            next.interactable = range != null && rig.DistrictFraction < .999f;
            position.SetValueWithoutNotify(rig.DistrictFraction);
            // Arrow graphics follow the sketch's actual expansion direction.
            float angle = range == null ? 0 : Mathf.Atan2(range.direction.y, range.direction.x) * Mathf.Rad2Deg + 90;
            next.transform.Find("Arrow").localRotation = Quaternion.Euler(0,0,angle);
            previous.transform.Find("Arrow").localRotation = Quaternion.Euler(0,0,angle+180);
        }
    }
}
