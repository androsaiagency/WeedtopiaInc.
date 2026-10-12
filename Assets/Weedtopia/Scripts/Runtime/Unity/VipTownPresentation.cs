using System;
using System.Linq;
using UnityEngine;

namespace Weedtopia.Unity
{
    public sealed class VipTownPresentation : MonoBehaviour
    {
        public VipStoryActor[] actors = Array.Empty<VipStoryActor>();
        private GameRoot root;
        public void Initialize(GameRoot value) { root = value; foreach (var actor in actors) actor.Initialize(value); }
        public void Refresh() { foreach (var actor in actors) actor.Refresh(); }
        public VipStoryActor Actor(string id) => actors.FirstOrDefault(a => a.vipId == id);
        public bool TrySelect(Ray ray)
        {
            VipStoryActor picked = null; int front = int.MinValue;
            foreach (var actor in actors)
                if (actor.Visible && actor.spriteRenderer.sortingOrder > front && actor.TryPick(ray, out _))
                { picked = actor; front = actor.spriteRenderer.sortingOrder; }
            if (picked == null) return false;
            root.Ui.ShowStories(picked.vipId); return true;
        }
        public void Focus(string id)
        {
            var actor = Actor(id);
            if (actor == null || !actor.Visible) return;
            var definition = root.Session.Stories.People.Single(p => p.id == id);
            root.CameraRig.EnterDistrict(root.Definitions.instances.Single(i => i.id == definition.focus).zone);
            root.CameraRig.Focus(actor.transform.position);
        }
    }
}
