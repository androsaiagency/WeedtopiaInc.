using System;
using System.Linq;
using UnityEngine;
using Weedtopia.Domain;
using Weedtopia.Unity;
using Object=UnityEngine.Object;

namespace Weedtopia.Editor.GameSetup
{
    /// <summary>Diagnostic poses of existing scene instances; never saved to the production scene.</summary>
    public static class DirectionalMotionEvidence
    {
        public static void Prepare(GameRoot root,Action<string,Action> shot)
        {
            var life=Object.FindFirstObjectByType<LivingTownPresentation>();life.diagnosticFreeze=true;
            var people=new[]{"resident_a","resident_b","resident_c","cyclist"}.Select(key=>life.actors.First(a=>a.animation.animationSet.name==key)).ToArray();
            var van=GameObject.Find("Delivery_0").GetComponent<DeliveryView>();var vehicleMotion=van.GetComponent<DirectionalSpriteAnimator>();
            var flights=new[]{"swallow","butterfly"}.Select(key=>life.actors.First(a=>a.animation.animationSet.name==key)).ToArray();
            var allVehicles=Object.FindObjectsByType<DeliveryView>(FindObjectsSortMode.None);
            void View(Vector2 point,float zoom){root.Ui.Close();root.Ui.Interaction.CloseBuilding();root.CameraRig.ShowOverview();root.CameraRig.SetDiagnosticZoom(zoom);root.CameraRig.Focus(DistrictMap.ToWorld(point));}
            for(int d=0;d<8;d++)for(int f=0;f<6;f++)
            {
                int direction=d,frame=f;
                shot($"direction_{d}_frame_{f}",()=>
                {
                    foreach(var a in life.actors)a.anchor.gameObject.SetActive(false);
                    foreach(var v in allVehicles){v.enabled=false;v.transform.Find("Body").gameObject.SetActive(false);}
                    View(new Vector2(292,58),22);
                    var planHeading=new Vector2(Mathf.Cos(direction*Mathf.PI/4),Mathf.Sin(direction*Mathf.PI/4));
                    Vector3 heading=DistrictMap.ToWorld(planHeading);
                    for(int i=0;i<people.Length;i++)
                    {
                        var actor=people[i];actor.anchor.gameObject.SetActive(true);
                        actor.anchor.position=DistrictMap.ToWorld(new Vector2(267+i*12,58)+planHeading*(frame/6f*actor.animation.animationSet.distancePerCycle));
                        actor.animation.Present(heading,(frame+.1f)/6*actor.animation.animationSet.distancePerCycle,true,root.CameraRig.RenderCamera);
                        actor.artwork.sortingOrder=WorldSort.At(actor.anchor.position)+3;
                    }
                    van.transform.Find("Body").gameObject.SetActive(true);van.transform.Find("Body/Cargo").gameObject.SetActive(false);
                    van.transform.position=DistrictMap.ToWorld(new Vector2(320,58)+planHeading*(frame/6f*vehicleMotion.animationSet.distancePerCycle));
                    vehicleMotion.Present(heading,(frame+.1f)/6*vehicleMotion.animationSet.distancePerCycle,true,root.CameraRig.RenderCamera);
                    vehicleMotion.artwork.sortingOrder=WorldSort.At(van.transform.position)+4;
                    for(int i=0;i<flights.Length;i++)
                    {
                        var a=flights[i];a.anchor.gameObject.SetActive(true);a.anchor.position=DistrictMap.ToWorld(new Vector2(283+i*15,75),3);
                        a.animation.Present(heading,(frame+.1f)/6*a.animation.animationSet.distancePerCycle,true,root.CameraRig.RenderCamera);
                    }
                });
            }
            void Restore(){foreach(var v in allVehicles)v.enabled=true;life.PresentAt(12);root.Session.State.deliveries.Clear();}
            shot("city_street",()=>{Restore();View(new Vector2(45,-63),36);});
            shot("north_south_walk",()=>{life.PresentAt(40);View(new Vector2(89,55),28);});
            var cityRoute=root.Definitions.routes.First(r=>r.from=="city_warehouse"&&r.to=="coffee_01"&&r.vehicle);
            for(int f=0;f<12;f++)
            {
                int frame=f;
                shot("city_van_"+f.ToString("D2"),()=>
                {
                    Restore();root.Session.State.tickRemainder=0;
                    root.Session.State.deliveries.Add(new DeliveryState {route=cityRoute.id,workerId=root.Session.State.workers.First(w=>w.role=="city_driver").id,cargo=6,totalSeconds=100,remainingSeconds=90-frame*3});
                    root.Refresh();life.PresentAt(12+frame*.15f);View(DistrictMap.ToPlan(root.Presentation.Route(cityRoute.id).Position(.25f)),30);
                });
            }
            var bakeryRoute=root.Definitions.routes.First(r=>r.pickupByRecipient);
            foreach(bool fetching in new[]{true,false})
                shot(fetching?"baker_outbound":"baker_return",()=>
                {
                    Restore();root.Session.State.tickRemainder=0;
                    root.Session.State.deliveries.Add(new DeliveryState {route=bakeryRoute.id,workerId=root.Session.State.workers.First(w=>w.role=="baker").id,cargo=fetching?0:8,totalSeconds=100,remainingSeconds=55,fetching=fetching});
                    root.Refresh();View(DistrictMap.ToPlan(root.Presentation.Route(bakeryRoute.id).Position(.5f)),27);
                });
            shot("final_overview",()=>{Restore();root.Refresh();root.CameraRig.ShowOverview();});
        }
    }
}
