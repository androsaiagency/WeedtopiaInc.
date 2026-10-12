using System;
using UnityEngine;

namespace Weedtopia.Unity
{
    [Serializable] public sealed class VipMemoryVisual
    {
        public int chapter;
        public string choiceId;
        public GameObject target;
    }
    /// <summary>A fixed authored inhabitant. Population changes visibility; quests never remove the actor.</summary>
    public sealed class VipStoryActor : MonoBehaviour
    {
        public string vipId;
        public GameObject visibleRoot;
        public Transform artwork;
        public SpriteRenderer spriteRenderer, marker;
        public BoxCollider selectionBox;
        public VipMemoryVisual[] memories = Array.Empty<VipMemoryVisual>();
        private GameRoot root;
        private Vector3 rest;
        public bool Visible => visibleRoot != null && visibleRoot.activeSelf;
        public void Initialize(GameRoot value) { root = value; rest = artwork.localPosition; Refresh(); }
        public void Refresh()
        {
            bool visible = root != null && root.Session != null && root.Session.State.introSeen && root.Session.Stories.Unlocked(vipId);
            visibleRoot.SetActive(visible); selectionBox.enabled = visible;
            if (!visible) return;
            var stories = root.Session.Stories; var progress = stories.Person(vipId);
            marker.gameObject.SetActive(stories.Active(vipId) != null || stories.Offer(vipId) != null);
            marker.color = stories.Ready(vipId) ? new Color(.51f, .95f, .68f) : new Color(1, .79f, .37f);
            foreach (var visual in memories)
            {
                bool complete = progress.completed.Count > visual.chapter;
                bool chosen = complete && (string.IsNullOrEmpty(visual.choiceId) || progress.completed[visual.chapter].choiceId == visual.choiceId);
                visual.target.SetActive(chosen);
            }
        }
        public bool TryPick(Ray ray, out float distance)
        { distance = float.PositiveInfinity; return Visible && selectionBox.enabled && AuthoredBoxPicking.TryHit(selectionBox, ray, out distance); }
        private void LateUpdate()
        {
            if (root == null || !Visible) return;
            bool paused = root.Session.Paused;
            artwork.localPosition = rest + Vector3.up * (paused ? 0 : Mathf.Sin(Time.time * 1.5f + transform.position.x) * .035f);
            artwork.rotation = root.CameraRig.RenderCamera.transform.rotation;
            marker.transform.rotation = artwork.rotation;
            spriteRenderer.sortingOrder = WorldSort.At(transform.position) + 6;
            marker.sortingOrder = spriteRenderer.sortingOrder + 1;
        }
    }
}
