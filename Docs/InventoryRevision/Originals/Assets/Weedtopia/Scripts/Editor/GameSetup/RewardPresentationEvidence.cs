using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Weedtopia.Application;
using Weedtopia.Domain;
using Weedtopia.Unity;

namespace Weedtopia.Editor.GameSetup
{
    public static class RewardPresentationEvidence
    {
        public static List<Action> Prepare(GameRoot root, ManualClock clock, Action<string> capture, Action<int,int> size)
        {
            var steps=new List<Action>();
            void Shot(string name,Action action)=>steps.Add(()=>{action();root.Refresh();root.Ui.Notify("");capture(name);});
            void Drive(Func<bool> done, bool project=false)
            {
                root.Ui.Rewards.DismissAll();
                for(int i=0;i<CityProjectDriver.MaxSimulationSeconds&&!done();i++)
                {
                    if(project)CityProjectDriver.Act(root.Session,"neighborhood");else TutorialDriver.Act(root.Session,clock.UtcNow,false);
                    clock.UtcNow=clock.UtcNow.AddSeconds(1);root.Session.Advance(1);
                }
                if(!done())throw new InvalidOperationException("Reward evidence driver stalled");
            }
            root.Session.CompleteIntro();root.Ui.Close();
            Shot("01_quest_preview_1920",()=>
            {
                Drive(()=>root.Session.Progression.Current?.id=="city_rebuild"&&root.Session.Progression.Ready);
                root.Ui.ShowJournal();
            });
            Shot("02_quest_reward_1920",()=>
            {
                if(root.Command(root.Session.Progression.Claim)!=Failure.None)throw new InvalidOperationException("Expected successful quest claim");
            });
            Shot("03_quest_reward_1366",()=>{size(1366,768);root.Session.State.uiScale=1.15f;});
            Shot("04_quest_reward_ultrawide",()=>{size(2560,1080);root.Session.State.uiScale=1;});
            Shot("05_collection_1920",()=>{size(1920,1080);root.Ui.Rewards.DismissAll();root.Ui.ShowCollection();});
            Shot("06_history_1920",()=>root.Ui.Collection.SelectTab(2));
            Shot("07_festival_finale_1920",()=>
            {
                root.Ui.Close();Drive(()=>root.Session.State.City.freeplay);
                Drive(()=>root.Session.CityProject.Completed,true);
                var receipt=root.Session.State.rewardHistory.Last(r=>r.source==RewardSource.CityProject);
                root.Session.Save();root.Ui.Rewards.EnqueueAfter(receipt.sequence-1);
            });
            Shot("08_achievements_1920",()=>{root.Ui.Rewards.DismissAll();root.Ui.ShowCollection();root.Ui.Collection.SelectTab(1);});
            Shot("09_collection_portrait",()=>{size(1080,1920);root.Ui.Collection.SelectTab(0);});
            Shot("10_team_collection_entry",()=>{size(1920,1080);root.Ui.ShowWorkers();});
            return steps;
        }
    }
}
