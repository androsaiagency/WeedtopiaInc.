using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Weedtopia.Domain;
using Weedtopia.Unity;

namespace Weedtopia.Editor.GameSetup
{
    public static class LuckyWheelEvidence
    {
        public static List<Action> Prepare(GameRoot root,ManualClock clock,Action<string> capture,Action<int,int> size)
        {
            var steps=new List<Action>();root.Session.CompleteIntro();root.Ui.Close();
            void Shot(string name,Action action){steps.Add(()=>{action();root.Refresh();root.Ui.Notify("");capture(name);});}
            Shot("00_wheel_entry_orders",()=>root.Ui.ShowJournal());
            Shot("01_wheel_free_1920",()=>root.Ui.ShowLuckyWheel());
            Shot("02_wheel_item_inspection",()=>root.Ui.LuckyWheel.slots.OrderByDescending(s=>Array.Find(root.Definitions.items,d=>d.id==s.Prize.id).rarity).First().button.onClick.Invoke());
            Shot("03_wheel_win_celebration",()=>root.Ui.LuckyWheel.Spin());
            Shot("04_wheel_remaining_nine",()=>{root.Ui.Rewards.DismissAll();root.Refresh();});
            Shot("05_wheel_paid_confirmation",()=>root.Ui.LuckyWheel.Spin());
            Shot("06_wheel_inventory_reward",()=>{root.Ui.ShowCollection();root.Ui.Collection.tiles.First(t=>t.gameObject.activeSelf).button.onClick.Invoke();});
            foreach(var res in new[]{new Vector2Int(1366,768),new Vector2Int(2560,1080),new Vector2Int(1080,1920)})
                Shot("07_wheel_"+res.x+"x"+res.y,()=>{size(res.x,res.y);root.Session.State.uiScale=res.x==1366?1.15f:1;root.Ui.ShowLuckyWheel();});
            Shot("08_wheel_midnight_refresh",()=>{size(1920,1080);root.Session.State.uiScale=1;clock.UtcNow=clock.UtcNow.Date.AddDays(1);root.Ui.ShowLuckyWheel();});
            // Only this disposable capture save receives diagnostic currency to show the exhausted state.
            Shot("09_wheel_exhausted",()=>
            {
                root.Session.State.grinders=10000;
                while(root.Session.Wheel.Remaining.Length>0){root.Session.Wheel.Offer(out var offer);root.Session.SpinWheel(offer.Token,out _);}
                root.Refresh();
            });
            return steps;
        }
    }
}
