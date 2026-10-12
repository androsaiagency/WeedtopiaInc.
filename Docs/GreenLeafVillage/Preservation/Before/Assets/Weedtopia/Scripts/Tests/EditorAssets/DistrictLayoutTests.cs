using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Weedtopia.Content;
using Weedtopia.Domain;
using Weedtopia.Unity;
using Weedtopia.Editor.GameSetup;

namespace Weedtopia.Tests.Assets
{
    public sealed class DistrictLayoutTests
    {
        [Test] public void AuthoredSelectionRespectsRotatedScaledBoxesAndRejectsMisses()
        {
            var obj=new GameObject("Selection regression",typeof(BoxCollider));
            try
            {
                var box=obj.GetComponent<BoxCollider>();box.center=new Vector3(0,1.5f,0);box.size=new Vector3(1.4f,3.2f,1.4f);
                obj.transform.SetPositionAndRotation(new Vector3(160,0,-178),Quaternion.Euler(0,33,0));obj.transform.localScale=new Vector3(1.45f,2,.7f);
                Vector3 center=obj.transform.TransformPoint(box.center),origin=center+new Vector3(-550,550,-550);
                Assert.That(AuthoredBoxPicking.TryHit(box,new Ray(origin,center-origin),out var distance),Is.True);Assert.That(distance,Is.GreaterThan(900));
                Assert.That(AuthoredBoxPicking.TryHit(box,new Ray(origin+Vector3.right*40,center-origin),out _),Is.False);
                box.enabled=false;Assert.That(AuthoredBoxPicking.TryHit(box,new Ray(origin,center-origin),out _),Is.False);
            }
            finally{UnityEngine.Object.DestroyImmediate(obj);}
        }
        [Test] public void DistrictsExpandAlongTheSketchAndClampAtBothEnds()
        {
            var map=AssetDatabase.LoadAssetAtPath<DistrictMap>(BurningHillsLayoutInstaller.MapPath);
            var directions=new[]{Vector2.right,Vector2.down,Vector2.up,Vector2.left};
            foreach(Zone zone in Enum.GetValues(typeof(Zone)))
            {
                var r=map.Range(zone);Assert.That(r.direction,Is.EqualTo(directions[(int)zone]));
                Assert.That(Vector2.Dot((r.end-r.start).normalized,r.direction),Is.GreaterThan(.999f));
                Assert.That(r.Clamp(r.start-r.direction*1000),Is.EqualTo(r.start));
                Assert.That(Vector2.Distance(r.Clamp(r.end+r.direction*1000),r.end),Is.LessThan(.001));
                Assert.That(Vector2.Distance(DistrictMap.ToPlan(DistrictMap.ToWorld(r.end)),r.end),Is.LessThan(.001));
            }
        }
        [Test] public void EveryExpandedWorkplaceHasARealSupplyRouteAndFarmNeedsTransport()
        {
            var d=AssetDatabase.LoadAssetAtPath<GameContent>(GameScaffold.ContentPath).Load();
            d.instances=d.instances.Where(x=>x.cityId=="burning_hills").ToArray();
            var localIds=d.instances.Select(x=>x.id).ToArray();d.routes=d.routes.Where(x=>localIds.Contains(x.from)&&localIds.Contains(x.to)).ToArray();
            var plan=JsonUtility.FromJson<BurningHillsLayoutInstaller.LayoutPlan>(File.ReadAllText(BurningHillsLayoutInstaller.Folder+"/BurningHillsLayout.json"));
            Assert.That(d.instances.Count(x=>x.family=="cultivation_plot"),Is.EqualTo(8));
            Assert.That(d.instances.Count(x=>x.family=="coffee_shop"),Is.EqualTo(12));
            Assert.That(d.instances.Count(x=>x.family=="settlement_plot"),Is.EqualTo(18));
            Assert.That(d.instances.Count(x=>x.family=="bakery"),Is.EqualTo(12));
            foreach(var shop in d.instances.Where(x=>x.family=="coffee_shop"||x.family=="butter_factory"))
                Assert.That(d.routes.Any(x=>x.from=="city_warehouse"&&x.to==shop.id&&x.vehicle),Is.True,shop.id);
            foreach(var bakery in d.instances.Where(x=>x.family=="bakery"))
                Assert.That(d.routes.Any(x=>x.from=="butter_factory"&&x.to==bakery.id&&x.vehicle&&x.pickupByRecipient),Is.True,bakery.id);
            foreach(var r in d.routes.Where(x=>x.vehicle))
            {
                var p=plan.routes.Single(x=>x.id==r.id);float length=Enumerable.Range(1,p.points.Length-1).Sum(i=>Vector2.Distance(p.points[i-1],p.points[i]));
                Assert.That(r.travelSeconds,Is.GreaterThanOrEqualTo(Mathf.CeilToInt(length/14)),r.id);
            }
            Assert.That(plan.routes.Single(x=>x.id=="farm_city").length,Is.GreaterThan(180));
        }
    }
}
