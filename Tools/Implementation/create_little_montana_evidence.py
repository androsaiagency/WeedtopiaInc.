"""Create the explicit Little Montana capture runner from the proven capture protocol.

The generated editor utility owns only isolated diagnostic saves and screenshots.
It never installs or repairs production objects automatically.
"""
import pathlib

ROOT = pathlib.Path(__file__).resolve().parents[2]
DIRECTORY = ROOT / 'Assets/Weedtopia/Scripts/Editor/GameSetup'
source = (DIRECTORY / 'SevenHillsEvidence.cs').read_text(encoding='utf-8')
protocol = source[:source.index('        private static void Prepare()')]
protocol = protocol.replace('SevenHillsEvidence', 'LittleMontanaEvidence').replace('Docs/SevenHills', 'Docs/LittleMontana')
protocol = protocol.replace('SevenTravelVisible', 'MontanaTravelVisible').replace('"seven_hills"', '"little_montana"')
protocol = protocol.replace('Seven Hills', 'Little Montana')
protocol = protocol.replace('using Weedtopia.Domain;', 'using Weedtopia.Domain;\nusing Weedtopia.Application;')
probe_changes = {
    '        public static void Begin()': '        private static bool MemoryProbe => SessionState.GetBool(Key+".memoryProbe",false);\n        public static void Begin() => BeginCapture(false);\n        public static void BeginMemoryProbe() => BeginCapture(true);\n        private static void BeginCapture(bool memoryProbe)',
    '            Directory.CreateDirectory(Folder+"/Screenshots");SessionState.SetBool(Key,true);': '            SessionState.SetBool(Key+".memoryProbe",memoryProbe);\n            Directory.CreateDirectory(Folder+(memoryProbe?"/MemoryProbeScreenshots":"/Screenshots"));SessionState.SetBool(Key,true);',
    'ProductionUiEvidence.Write(fingerprint,Folder+"/ui_overflow.json")': 'ProductionUiEvidence.Write(fingerprint,Folder+(MemoryProbe?"/ui_overflow_probe.json":"/ui_overflow.json"),MemoryProbe?new[]{"1920x1080"}:new[]{"1920x1080","1366x768","2560x1080","1080x1920"})',
    'File.WriteAllText(Folder+"/gameview_receipt.json",': 'File.WriteAllText(Folder+(MemoryProbe?"/memory_probe_receipt.json":"/gameview_receipt.json"),',
    'File.WriteAllText(Folder+"/gameview_error.txt",': 'File.WriteAllText(Folder+(MemoryProbe?"/memory_probe_error.txt":"/gameview_error.txt"),',
    '        {requested=Path.GetFullPath(Folder+"/Screenshots/"+name+".png");frames=0;settle=EditorApplication.timeSinceStartup+2;resizeDeadline=EditorApplication.timeSinceStartup+12;}': '''        {
            // Run the same isolated public fixture actions, rendering only the seven memory frames.
            if(MemoryProbe&&!name.StartsWith("18e_memories_",StringComparison.Ordinal)){index++;frames=0;return;}
            requested=Path.GetFullPath(Folder+(MemoryProbe?"/MemoryProbeScreenshots/":"/Screenshots/")+name+".png");frames=0;settle=EditorApplication.timeSinceStartup+2;resizeDeadline=EditorApplication.timeSinceStartup+12;
        }'''
}
for before, after in probe_changes.items():
    if before not in protocol:
        raise ValueError('Shared capture protocol differs: ' + before)
    protocol = protocol.replace(before, after)
body = r'''
        private static void Prepare()
        {
            root.Session.CompleteIntro();root.Ui.Close();FreeplayFixture();steps=new List<Func<bool>>();
            root.SetLanguage("de");
            Populate("burning_hills",3000);root.Session.Campaign.RefreshMilestones();
            Require(root.Session.Campaign.Accept("big_rock_city"),"Accept existing second-city fixture");
            Populate("big_rock_city",2000);root.Session.Campaign.RefreshMilestones();
            Require(root.Session.Campaign.Accept("seven_hills"),"Accept existing third-city fixture");
            Populate("burning_hills",5000);Populate("big_rock_city",5000);Populate("seven_hills",4999);
            root.Session.Campaign.RefreshMilestones();
            if(root.Session.Campaign.UnlockProgress("little_montana")!=14999||root.Session.Campaign.Offered("little_montana"))
                throw new InvalidOperationException("Expected the exact 14999 locked fixture");
            CitiesBottom("01_locked_14999");
            Shot("02_exact_15000_offer",()=>
            {
                Populate("seven_hills",5000);root.Session.Campaign.RefreshMilestones();
                if(root.Session.Campaign.UnlockProgress("little_montana")!=15000)throw new InvalidOperationException("Expected exactly 15000");
                root.Ui.ShowCities();MontanaTravelVisible().travel.onClick.Invoke();SetCitiesScroll(1);
                if(root.Ui.Campaign.InvitationCityId!="little_montana")throw new InvalidOperationException("Fourth authored row did not show invitation");
            });
            Shot("03_rocko_fourth_city_mandate",()=>
            {
                root.Ui.Close();root.Ui.SpeakToCharacter("rocko");SetCitiesScroll(1);
                if(root.Ui.Campaign.InvitationCityId!="little_montana")throw new InvalidOperationException("Rocko did not open fourth city mandate");
            });
            Shot("04_mandate_accepted",()=>
            {
                root.Ui.Campaign.accept.onClick.Invoke();
                if(!root.Session.Campaign.Accessible("little_montana"))throw new InvalidOperationException("UI did not accept Little Montana");
                root.Ui.Rewards.DismissAll();CitiesTop();
            });
            CitiesBottom("04b_fourth_city_travel");
            TravelShot("little_montana","05_arrival",()=>root.Ui.Close());
            Shot("05a_arrival_quest_focus",()=>
            {
                if(root.Session.Progression.Current?.id!="lm_arrival")throw new InvalidOperationException("Unexpected arrival quest");
                root.Ui.Experience.quests.Open("lm_arrival");root.Ui.Experience.quests.previewAction.onClick.Invoke();
                var townHall=root.Site("lm_town_hall").transform.position;
                if(Vector2.Distance(DistrictMap.ToPlan(root.CameraRig.TargetPosition),DistrictMap.ToPlan(townHall))>.01f)
                    throw new InvalidOperationException("Arrival did not focus Little Montana town hall");
            });
            Shot("05b_arrival_rocko_dialogue",()=>root.Ui.SpeakToCharacter("rocko"));
            Shot("05c_claim_arrival",()=>
            {
                root.Ui.Experience.quests.Open("lm_arrival");root.Ui.Experience.quests.previewAction.onClick.Invoke();
                if(root.Session.Progression.Current?.id!="lm_first_garden")throw new InvalidOperationException("UI did not claim arrival");
                root.Ui.Experience.quests.Open("lm_first_garden");
            });
            Shot("05d_first_garden_build",()=>
            {
                root.Ui.Experience.quests.previewAction.onClick.Invoke();root.Ui.Interaction.upgrade.onClick.Invoke();
                if(root.Session.State.Find("lm_farm_grow_01").buildingSeconds<=0)throw new InvalidOperationException("UI did not begin first garden");
            });
            Shot("05e_garden_simulated_and_claimed",()=>
            {
                root.Ui.Close();root.Session.Advance(root.Session.State.Find("lm_farm_grow_01").buildingSeconds);root.Refresh();
                root.Ui.Experience.quests.Open("lm_first_garden");root.Ui.Experience.quests.previewAction.onClick.Invoke();
                if(root.Session.Progression.Current?.id!="lm_first_harvest")throw new InvalidOperationException("Garden claim did not advance");
                root.Ui.Rewards.DismissAll();root.Ui.Experience.quests.Open("lm_first_harvest");
            });
            Shot("06_station_before_growth",()=>{root.Ui.Close();root.CameraRig.Focus(DistrictMap.ToWorld(new Vector2(110,-76)));root.CameraRig.SetDiagnosticZoom(26);});
            Shot("07_pine_meadows",()=>{GrownCityFixture();root.Ui.Close();root.CameraRig.EnterDistrict(Zone.Farm);root.CameraRig.SetDiagnosticZoom(43);});
            Shot("08_station_quarter",()=>{root.Ui.Close();root.CameraRig.EnterDistrict(Zone.City);root.CameraRig.SetDiagnosticZoom(38);});
            Shot("09_eagle_slope",()=>{root.Ui.Close();root.CameraRig.EnterDistrict(Zone.Settlement);root.CameraRig.SetDiagnosticZoom(40);});
            Shot("10_old_mill_yard",()=>{root.Ui.Close();root.CameraRig.EnterDistrict(Zone.Industry);root.CameraRig.SetDiagnosticZoom(38);});
            Shot("11_mara_first_chapter",()=>root.Ui.ShowStories("lm_mara"));
            Shot("11b_mara_accepted",()=>root.Ui.Stories.accept.onClick.Invoke());
            Shot("11c_mara_saved_memory",()=>
            {
                var q=root.Session.Stories.ActiveDefinition("lm_mara");
                if(q==null)throw new InvalidOperationException("Mara was not accepted through the UI");
                CompleteActive("lm_mara",q);root.Ui.Rewards.DismissAll();root.Ui.Close();root.StoryWorld.Focus("lm_mara");root.CameraRig.SetDiagnosticZoom(17);
            });
            Shot("12_noah_third_chapter_choice",()=>{Complete("lm_noah");Complete("lm_noah");root.Ui.ShowStories("lm_noah");});
            Shot("12b_noah_teach_choice_accepted",()=>
            {
                root.Ui.Stories.alternate.onClick.Invoke();
                if(root.Session.Stories.Active("lm_noah")?.choiceId!="teach")throw new InvalidOperationException("UI alternate did not choose teach");
            });
            Shot("12c_noah_choice_memory",()=>
            {
                var q=root.Session.Stories.ActiveDefinition("lm_noah");if(q==null)throw new InvalidOperationException("No active Noah choice");
                CompleteActive("lm_noah",q);root.Ui.Rewards.DismissAll();root.Ui.Close();root.StoryWorld.Focus("lm_noah");root.CameraRig.SetDiagnosticZoom(17);
            });
            foreach(var resolution in new[]{new Vector2Int(1920,1080),new Vector2Int(1366,768),new Vector2Int(2560,1080),new Vector2Int(1080,1920)})
            {
                Shot("13_campaign_"+resolution.x+"x"+resolution.y,()=>{SetSize(resolution.x,resolution.y);root.Session.State.uiScale=resolution.x==1366?1.15f:1;CitiesTop();});
                CitiesBottom("13b_travel_"+resolution.x+"x"+resolution.y);
                Shot("14_residents_"+resolution.x+"x"+resolution.y,()=>{root.Ui.ShowStories();root.Ui.Stories.peopleTab.onClick.Invoke();});
                Shot("14b_resident_details_"+resolution.x+"x"+resolution.y,()=>root.Ui.ShowStories("lm_mara"));
                Shot("14c_projects_"+resolution.x+"x"+resolution.y,()=>{root.Ui.ShowStories();root.Ui.Stories.togetherTab.onClick.Invoke();});
            }
            foreach(string person in new[]{"lm_mara","lm_jon","lm_ellie","lm_noah","lm_ruth","lm_tessa","lm_ben"})
                Shot("15_city_"+person,()=>{SetSize(1920,1080);root.Session.State.uiScale=1;root.Ui.Close();root.StoryWorld.Focus(person);root.CameraRig.SetDiagnosticZoom(17);});
            foreach(string language in new[]{"de","en","es","fr","ru"})
            {
                Shot("16_campaign_"+language,()=>{root.SetLanguage(language);CitiesTop();});
                Shot("16b_story_"+language,()=>root.Ui.ShowStories("lm_noah"));
            }
            Shot("17_valley_trail_offered",()=>
            {
                root.SetLanguage("de");
                foreach(var person in root.Session.Stories.People)
                    while(root.Session.Stories.Person(person.id).completed.Count<person.chapters.Length)Complete(person.id);
                root.Ui.ShowStories("lm_joint_valley_trail");
            });
            Shot("17b_valley_trail_accepted",()=>root.Ui.Stories.accept.onClick.Invoke());
            Shot("17c_valley_trail_completed",()=>
            {
                var q=root.Session.Stories.ActiveDefinition("lm_joint_valley_trail");if(q==null)throw new InvalidOperationException("Trail project was not accepted");
                CompleteActive("lm_joint_valley_trail",q);root.Ui.ShowStories("lm_joint_valley_trail");
            });
            Shot("18_all_projects_completed",()=>
            {
                Complete("lm_joint_station_square");Complete("lm_joint_open_workyard");root.Refresh();root.Ui.ShowStories();root.Ui.Stories.togetherTab.onClick.Invoke();
            });
            Shot("18b_station_restored",()=>{root.Ui.Close();root.CameraRig.Focus(DistrictMap.ToWorld(new Vector2(110,-76)));root.CameraRig.SetDiagnosticZoom(26);});
            Shot("18c_noah_learning_workshop",()=>{root.Ui.Close();root.CameraRig.Focus(DistrictMap.ToWorld(new Vector2(110,-184)));root.CameraRig.SetDiagnosticZoom(23);});
            Shot("18d_eagle_trail_restored",()=>{root.Ui.Close();root.CameraRig.Focus(DistrictMap.ToWorld(new Vector2(-40,324)));root.CameraRig.SetDiagnosticZoom(23);});
            foreach(var person in new[]{"lm_mara","lm_jon","lm_ellie","lm_noah","lm_ruth","lm_tessa","lm_ben"})
                Shot("18e_memories_"+person,()=>
                {
                    SetSize(1920,1080);root.Ui.Close();
                    if(root.Session.Stories.Person(person).completed.Count!=4)throw new InvalidOperationException("Expected all four saved memories for "+person);
                    root.StoryWorld.Focus(person);
                    if(person=="lm_ruth")
                    {
                        // Her authored memories follow the narrow courtyard between mature houses.
                        // Frame that whole courtyard; this changes only the diagnostic camera.
                        root.CameraRig.Focus(DistrictMap.ToWorld(new Vector2(17,166)));root.CameraRig.SetDiagnosticZoom(28);
                    }
                    else root.CameraRig.SetDiagnosticZoom(17);
                });
            TravelShot("burning_hills","19_return_burning_hills",()=>{root.Ui.Close();root.Focus("town_hall");root.CameraRig.SetDiagnosticZoom(25);});
            TravelShot("big_rock_city","20_return_big_rock_city",()=>{root.Ui.Close();root.Focus("br_town_hall");root.CameraRig.SetDiagnosticZoom(25);});
            TravelShot("seven_hills","21_return_seven_hills",()=>{root.Ui.Close();root.Focus("sh_town_hall");root.CameraRig.SetDiagnosticZoom(25);});
            TravelShot("little_montana","22_return_little_montana_choice_preserved",()=>
            {
                var noah=root.Session.Stories.Person("lm_noah");
                if(noah.completed.Count!=4||noah.completed[2].choiceId!="teach")throw new InvalidOperationException("Saved Noah choice lost on travel");
                root.Ui.Close();root.CameraRig.Focus(DistrictMap.ToWorld(new Vector2(110,-184)));root.CameraRig.SetDiagnosticZoom(23);
            });
        }
        private static void Require(Failure failure,string action)
        {if(failure!=Failure.None)throw new InvalidOperationException(action+": "+failure);}
        private static void Populate(string city,int residents)
        {
            var game=root.Session;var plots=game.State.buildings.Where(b=>b.cityId==city&&(b.family=="settlement_plot"||b.family=="residential_house")).ToArray();
            foreach(var plot in plots){plot.level=0;game.State.workers.RemoveAll(w=>w.buildingId==plot.id);}
            var choices=new Dictionary<int,(int level,int workerLevel,int rank)>();
            for(int tier=1;tier<VisualTiers.Starts.Length;tier++)
                for(int level=1;level<=100;level++)for(int rank=1;rank<=10;rank++)
                {
                    int count=(int)Math.Floor(game.Definitions.Building("residential_house").baseResidents*(1+tier)*(1+game.Definitions.workerAttributeStep*level*rank));
                    if(!choices.ContainsKey(count))choices[count]=(VisualTiers.Starts[tier],level,rank);
                }
            var parts=new List<int>();int remaining=residents;
            while(remaining>=5000){parts.Add(5000);remaining-=5000;}
            if(remaining>0)
            {
                if(choices.ContainsKey(remaining))parts.Add(remaining);
                else{int first=choices.Keys.First(n=>n<remaining&&choices.ContainsKey(remaining-n));parts.Add(first);parts.Add(remaining-first);}
            }
            for(int index=0;index<parts.Count;index++)
            {
                var configuration=choices[parts[index]];var building=plots[index];building.family="residential_house";building.level=configuration.level;
                game.State.workers.Add(new WorkerState{id="lm_capture_"+building.id,role="caretaker",level=configuration.workerLevel,rank=configuration.rank,
                    buildingId=building.id,slot=0,attributes=new List<WorkerAttribute>{WorkerAttribute.Residents}});
            }
            if(game.Simulation.Residents(city)!=residents)throw new InvalidOperationException("Population fixture mismatch for "+city);
            game.State.revision++;
        }
        private static void CompleteActive(string id,StoryQuest q)
        {
            PrepareGoals(q);root.Refresh();root.Ui.ShowStories(id);root.Ui.Stories.claim.onClick.Invoke();
            if(root.Session.Stories.ActiveDefinition(id)!=null)throw new InvalidOperationException("UI did not claim active "+q.id);
        }
        private static void PrepareGoals(StoryQuest q)
        {
            // Explicit diagnostic growth fixtures for visual coverage, not a played economy proof.
            var state=root.Session.State;state.City.stats.deliveries+=1000;state.City.stats.delivered+=100000;state.City.stats.coffeeSold+=100000;
            state.City.stats.rawProduced+=100000;state.City.stats.refinedProduced+=100000;state.City.stats.butterProduced+=100000;state.City.stats.pastrySold+=100000;
            foreach(var goal in q.goals.Where(g=>g.kind==StoryGoalKind.NpcVisited))for(int i=0;i<goal.count;i++)root.Session.Stories.Visit(goal.target);
            foreach(var goal in q.goals.Where(g=>g.kind==StoryGoalKind.Level))
            {
                var building=state.buildings.First(b=>b.cityId==state.activeCity&&(b.id==goal.target||b.family==goal.target));building.level=Math.Max(goal.count,building.level);
            }
            state.revision++;
        }
    }
}
'''
def main():
    destination = DIRECTORY / 'LittleMontanaEvidence.cs'
    destination.write_text(protocol + body, encoding='utf-8')
    meta = destination.with_suffix('.cs.meta')
    if not meta.exists():
        meta.write_text('fileFormatVersion: 2\nguid: 9c0ead4933aa4c62a9176a015ac36401\n', encoding='utf-8')
    print(destination)


if __name__ == '__main__':
    main()
