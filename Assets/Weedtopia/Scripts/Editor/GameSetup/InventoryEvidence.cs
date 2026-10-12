using System;
using System.Collections.Generic;
using System.Linq;
using Weedtopia.Application;
using Weedtopia.Domain;
using Weedtopia.Unity;

namespace Weedtopia.Editor.GameSetup
{
    public static class InventoryEvidence
    {
        public static List<Action> Prepare(GameRoot root,ManualClock clock,Action<string> capture,Action<int,int> size)
        {
            root.Session.CompleteIntro();root.Ui.Close();
            // Diagnostic save only. All rewards are earned through the normal tutorial use cases.
            for(int i=0;i<TutorialDriver.MaxSimulationSeconds&&!root.Session.State.City.freeplay;i++)
            {TutorialDriver.Act(root.Session,clock.UtcNow,false);clock.UtcNow=clock.UtcNow.AddSeconds(1);root.Session.Advance(1);}
            if(!root.Session.State.City.freeplay)throw new InvalidOperationException("Inventory capture tutorial stalled");
            root.Session.Save();var ui=root.Ui.Collection;
            void Inspect(string id){ui.Inspect(RewardCatalog.Inventory(root.Session.State).First(i=>i.key==id));root.Refresh();}
            // Start a real optional build, using the earned balance. No fabricated construction timer.
            var target=root.Session.State.buildings.First(b=>b.family=="cultivation_plot"&&b.level==0);
            if(root.Session.Upgrades.BuyWithMoney(target.id)!=Failure.None)throw new InvalidOperationException("Capture construction failed");
            return new List<Action>
            {
                ()=>{size(1920,1080);root.Ui.ShowCollection();Inspect("build_1m");capture("01_inventory_all_1920");},
                ()=>{ui.SelectCategory(2);Inspect("build_24h");capture("02_boosters_1920");},
                ()=>{Inspect("build_1m");ui.details.action.onClick.Invoke();root.Refresh();capture("03_speedup_confirmation_1920");},
                ()=>{ui.details.action.onClick.Invoke();root.Refresh();capture("04_speedup_applied_1920");},
                ()=>{Inspect("build_24h");ui.details.split.onClick.Invoke();root.Refresh();capture("04b_split_confirmation_1920");},
                ()=>{ui.details.cancel.onClick.Invoke();ui.SelectCategory(1);Inspect("specialist_contract");capture("05_specialist_contract_1920");},
                ()=>{ui.Inspect(RewardCatalog.Inventory(root.Session.State).First(i=>i.kind==RewardKind.Worker));root.Refresh();capture("06_worker_detail_1920");},
                ()=>{ui.SelectCategory(3);Inspect("cash_crate");capture("07_supplies_1920");},
                ()=>{size(1366,768);root.Session.State.uiScale=1.15f;ui.SelectCategory(2);Inspect("build_5h");capture("08_inventory_1366");},
                ()=>{size(2560,1080);root.Session.State.uiScale=1;root.Refresh();capture("09_inventory_ultrawide");},
                ()=>{size(1080,1920);root.Refresh();capture("10_inventory_portrait");},
                ()=>{size(1920,1080);ui.SelectCategory(1);Inspect("team_pack");ui.details.action.onClick.Invoke();ui.details.action.onClick.Invoke();root.Refresh();capture("11_pack_contents_1920");},
                ()=>{root.Ui.Rewards.DismissAll();root.Ui.ShowCollection();ui.SelectTab(2);root.Refresh();capture("12_inventory_history_1920");},
            };
        }
    }
}
