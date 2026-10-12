using System;
using System.Collections.Generic;
using System.Linq;
using Weedtopia.Domain;

namespace Weedtopia.Application
{
    public enum GuidanceKind { Tutorial, Project, Story, Vip }
    public sealed class GuidanceStep
    {
        public string text, focus, person;
        public bool done;
    }
    public sealed class GuidanceQuest
    {
        public string id, title, note, source;
        public GuidanceKind kind;
        public bool ready;
        public GuidanceStep[] steps;
    }
    /// <summary>Read-only instructions derived from the same rules used to award quests. No parallel progress state.</summary>
    public static class QuestGuidance
    {
        public static string BuildingName(GameSession session, string id)
        {
            var building = session.State.buildings.FirstOrDefault(b=>b.id==id && b.cityId==session.State.activeCity);
            string family = building?.family ?? id;
            var definition = session.Definitions.buildings.FirstOrDefault(b=>b.id==family);
            if(definition==null)return id;
            string result = session.Definitions.Text(definition.nameKey);
            if(building!=null && int.TryParse(id.Substring(id.LastIndexOf('_')+1),out int number))result += " " + number;
            return result;
        }
        public static string Focus(GameSession session, string target, string fallback)
        {
            var matches=session.State.buildings.Where(b=>b.cityId==session.State.activeCity&&(b.id==target||b.family==target)).OrderByDescending(b=>b.level).ToArray();
            if(matches.Length>0)return matches[0].id;
            if(target=="park"||target=="residential_house")return session.State.buildings.FirstOrDefault(b=>b.cityId==session.State.activeCity&&b.family=="settlement_plot")?.id??fallback;
            return fallback;
        }
        private static GuidanceStep Step(string text, Amount current, Amount goal, string focus=null, string person=null)
            => new GuidanceStep{text=text+" · "+Amount.Min(current,goal)+" / "+goal,done=current>=goal,focus=focus,person=person};
        public static GuidanceQuest[] Active(GameSession session)
        {
            var result=new List<GuidanceQuest>();var defs=session.Definitions;
            var q=session.Progression.Current;
            if(q!=null)
            {
                var steps=q.conditions.Select(c=>
                {
                    string target=BuildingName(session,c.target),focus=Focus(session,c.target,q.focus),text;
                    switch(c.kind)
                    {
                        case ConditionKind.Talk: text=defs.Format("guidance.talk",defs.Text("speaker."+c.target).Split('·')[0].Trim());break;
                        case ConditionKind.Level: text=defs.Format("guidance.level",target,c.count);break;
                        case ConditionKind.Assigned: text=defs.Format("guidance.assigned",target,c.count);break;
                        case ConditionKind.Automated: text=defs.Format("guidance.automated",target);break;
                        case ConditionKind.Harvest: text=defs.Text("presentation.questguidance.am_anbauplatz_die_fertige_ernte_einsamme.187a3c90");break;
                        case ConditionKind.Delivered: text=defs.Text("presentation.questguidance.fahrer_einsetzen_und_eine_lieferung_absc.542da2b1");break;
                        case ConditionKind.Sold: text=defs.Text("presentation.questguidance.bl_ten_im_coffeeshop_verkaufen_lassen.7382a9cd");break;
                        case ConditionKind.Spin: text=defs.Text("presentation.questguidance.in_mein_team_eine_worker_karte_einsetzen.e06164a6");break;
                        case ConditionKind.TownHall: text=defs.Text("presentation.questguidance.rathaus_antippen_und_stadtverwaltung_ffn.3f1b766d");break;
                        case ConditionKind.Residents: text=defs.Text("presentation.questguidance.wohnh_user_versorgen_einwohner_gewinnen.b4f5920f");break;
                        case ConditionKind.Daily: text=(string.IsNullOrEmpty(c.target)?defs.Text("presentation.questguidance.in_auftr_ge_die_tagesboni_abholen.c7f08aa2"):target+defs.Text("presentation.questguidance.tagesbonus_abholen.b3c8f988"));break;
                        case ConditionKind.Butter: text=defs.Text("presentation.questguidance.butterfabrik_besetzen_und_butter_produzi.48f1710b");break;
                        case ConditionKind.PastrySold: text=defs.Text("presentation.questguidance.b_ckerei_besetzen_und_backwaren_verkaufe.ec5f3fb2");break;
                        default: text=defs.Text("presentation.questguidance.in_auftr_ge_einen_vip_auftrag_abschlie_e.ca679861");break;
                    }
                    return Step(text,session.Progression.Progress(c),c.count,focus);
                }).ToArray();
                result.Add(new GuidanceQuest{id=q.id,title=defs.Text(q.titleKey),kind=GuidanceKind.Tutorial,ready=session.Progression.Ready,steps=steps,note=defs.Text("presentation.questguidance.jeden_schritt_antippen_um_zum_ziel_zu_ge.7eaa5414")});
            }
            else if(session.CityProject.Available&&!session.CityProject.Completed)
            {
                var project=session.CityProject;
                result.Add(new GuidanceQuest{id="project",title=CityProjectText.Title(session),kind=GuidanceKind.Project,ready=project.Ready,
                    note=project.Current==null?defs.Text(project.Definition.invitationKey):defs.Text("presentation.questguidance.produktion_und_verk_ufe_z_hlen_seit_begi.dcddd8d1"),
                    steps=project.Current==null?new[]{new GuidanceStep{text=defs.Text("presentation.questguidance.in_auftr_ge_den_festplan_ausw_hlen.a9493d9c"),focus="town_hall"}}:
                    project.Current.goals.Select((g,i)=>ProjectStep(session,g,i)).ToArray()});
            }
            foreach(var person in session.Stories.People.Select(p=>p.id).Concat(session.Stories.Collaborations.Select(c=>c.id)))
            {
                var active=session.Stories.Active(person);var definition=session.Stories.ActiveDefinition(person);if(active==null||definition==null)continue;
                result.Add(new GuidanceQuest{id="story:"+person,source=person,title=defs.Text(definition.titleKey),kind=GuidanceKind.Story,ready=session.Stories.Ready(person),
                    note=defs.Text("presentation.questguidance.fortschritt_dieser_angenommenen_aufgabe_.702b35df"),steps=definition.goals.Select((g,i)=>
                    {
                        string name=g.kind==StoryGoalKind.NpcVisited?defs.Text(session.Stories.People.Single(p=>p.id==g.target).nameKey):g.kind==StoryGoalKind.Level?BuildingName(session,g.target):"";
                        Amount progress=session.Stories.Progress(person,i);
                        return new GuidanceStep{text=string.Format(Weedtopia.Domain.GameText.Current?.Culture ?? System.Globalization.CultureInfo.InvariantCulture,defs.Text(g.textKey),name,progress,active.targets[i]),done=progress>=active.targets[i],focus=g.kind==StoryGoalKind.Level?Focus(session,g.target,null):null,person=g.kind==StoryGoalKind.NpcVisited?g.target:null};
                    }).ToArray()});
            }
            foreach(var vip in session.State.vip.Where(v=>!v.completed&&v.cityId==session.State.activeCity))
            {
                Amount progress=session.Progression.VipValue(vip.kind,vip.cityId);
                result.Add(new GuidanceQuest{id="vip:"+vip.id,source=vip.id,title="VIP · "+(vip.kind==ConditionKind.Delivered?defs.Text("presentation.questguidance.waren_liefern.021cb988"):defs.Text("presentation.questguidance.ernte_produzieren.cdf66c58")),kind=GuidanceKind.Vip,ready=progress>=vip.target,
                    note=defs.Format("guidance.reward",vip.reward),steps=new[]{Step(vip.kind==ConditionKind.Delivered?defs.Text("presentation.questguidance.waren_mit_fahrern_ausliefern.fa68fa9b"):defs.Text("presentation.questguidance.bl_ten_an_den_anbaupl_tzen_produzieren.7847a8f9"),progress,vip.target)}});
            }
            return result.ToArray();
        }
        private static GuidanceStep ProjectStep(GameSession session, ProjectGoal g, int index)
        {
            string target=BuildingName(session,g.target),text;
            switch(g.kind)
            {
                case ProjectGoalKind.Level:text=session.Definitions.Format("guidance.level",target,g.count);break;
                case ProjectGoalKind.Automated:text=session.Definitions.Format("guidance.automated",target);break;
                case ProjectGoalKind.Residents:text=session.Definitions.Format("guidance.residents",g.count);break;
                case ProjectGoalKind.RefinedProduced:text=session.Definitions.Text("presentation.questguidance.seit_abschnittsbeginn_bl_ten_veredeln.d689d0cb");break;
                case ProjectGoalKind.CoffeeSold:text=session.Definitions.Text("presentation.questguidance.seit_abschnittsbeginn_bl_ten_im_coffeesh.b9ef8562");break;
                case ProjectGoalKind.ButterProduced:text=session.Definitions.Text("presentation.questguidance.seit_abschnittsbeginn_butter_produzieren.156b9ea1");break;
                case ProjectGoalKind.PastrySold:text=session.Definitions.Text("presentation.questguidance.seit_abschnittsbeginn_backwaren_verkaufe.653d4518");break;
                default:text=session.Definitions.Text("presentation.questguidance.seit_abschnittsbeginn_waren_ausliefern.3130e034");break;
            }
            return Step(text,session.CityProject.Progress(index),g.count,Focus(session,g.target,session.CityProject.Focus));
        }
    }
}
