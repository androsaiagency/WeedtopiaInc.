from pathlib import Path
import shutil
ROOT=Path(__file__).resolve().parents[2]
def edit(path, old, new):
    p=ROOT/path
    s=p.read_text(encoding='utf-8-sig')
    assert old in s, (path,old[:80])
    p.write_text(s.replace(old,new,1),encoding='utf-8')
base='Assets/Weedtopia/Scripts/Runtime/'
edit(base+'Domain/GameState.cs','public long revision, nextRecoveryWork;', 'public long revision, nextRecoveryWork;\n        public long rewardSequence;\n        public List<RewardReceipt> rewardHistory = new List<RewardReceipt>();')
edit(base+'Application/ProgressionService.cs','foreach (string role in q.workerRewards) simulation.Workers.Grant(role);','var rewards = RewardHistory.Currency(q.moneyReward, q.cardsReward);\n            foreach (string role in q.workerRewards) rewards.Add(RewardItem.ForWorker(simulation.Workers.Grant(role)));\n            if (q.unlock > state.City.unlocked) rewards.Add(RewardItem.Of(RewardKind.District, 1, q.unlock.ToString()));')
edit(base+'Application/ProgressionService.cs','if (state.City.questIndex == defs.quests.Length) state.City.freeplay = true;', 'if (state.City.questIndex == defs.quests.Length) { state.City.freeplay = true; rewards.Add(RewardItem.Of(RewardKind.Title, 1, "founder")); }\n            RewardHistory.Record(state, RewardSource.Quest, q.id, q.titleKey, rewards);')
edit(base+'Application/ProgressionService.cs','state.City.stats.vipCompleted++; state.revision++; return Failure.None;', 'state.City.stats.vipCompleted++;\n            RewardHistory.Record(state, RewardSource.Vip, q.id, "reward.source.vip", RewardHistory.Currency(q.reward));\n            state.revision++; return Failure.None;')
edit(base+'Application/CityProjectService.cs','foreach (string role in step.workerRewards) simulation.Workers.Grant(role);','var rewards = RewardHistory.Currency(step.moneyReward, step.cardsReward);\n            foreach (string role in step.workerRewards) rewards.Add(RewardItem.ForWorker(simulation.Workers.Grant(role)));')
edit(base+'Application/CityProjectService.cs','BeginStep(); state.revision++; return Failure.None;\n        }\n    }', 'if (Completed)\n            {\n                rewards.Add(RewardItem.Of(RewardKind.Title, 1, "festival"));\n                rewards.Add(RewardItem.Of(RewardKind.SalesBonus, new Amount(Plan.salesBonus * 100), Plan.bonusFamily));\n            }\n            RewardHistory.Record(state, RewardSource.CityProject, step.id, step.titleKey, rewards);\n            BeginStep(); state.revision++; return Failure.None;\n        }\n    }')
edit(base+'Domain/WorkerService.cs','worker = Grant(role, rarity); state.City.stats.spins++;', 'worker = Grant(role, rarity); state.City.stats.spins++;\n            RewardHistory.Record(state, RewardSource.Recruitment, worker.id, "reward.source.recruitment", new[] { RewardItem.ForWorker(worker) });')
edit(base+'Application/DailyService.cs','bool changed = false;', 'bool changed = false;\n            int cardsBefore = state.cards, grindersBefore = state.grinders;\n            var vipBefore = state.vip.Select(v => v.id).ToArray();')
edit(base+'Application/DailyService.cs','if (changed) state.revision++;', 'if (changed)\n            {\n                var rewards = RewardHistory.Currency(0, state.cards - cardsBefore, state.grinders - grindersBefore);\n                int orders = state.vip.Count(v => !vipBefore.Contains(v.id));\n                if (orders > 0) rewards.Add(RewardItem.Of(RewardKind.VipOrders, orders));\n                RewardHistory.Record(state, RewardSource.Daily, day.ToString(System.Globalization.CultureInfo.InvariantCulture), "reward.source.daily", rewards);\n                state.revision++;\n            }')
art=ROOT/'Assets/Weedtopia/Production/Rewards/Art'
art.mkdir(parents=True,exist_ok=True)
shutil.copy2(r'C:\Users\Mein-PC\.codex\generated_images\01a0f070-2516-7cf0-9271-78fb3f2d8961\exec-327cb3bb-0e8d-47a4-8a32-1c1706fc3591.png', art/'RewardChest.png')
print('Reward ledger and award sources integrated; chest copied.')
