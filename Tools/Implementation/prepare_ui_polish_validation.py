from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
runner=ROOT/'Tools/Implementation/Run-UnityValidation.ps1'
text=runner.read_text(encoding='utf-8-sig')
text=text.replace("[ValidateSet('UiExperience'", "[ValidateSet('UiPolish','UiPolishCapture','UiExperience'")
text=text.replace("@('UiExperienceCapture'", "@('UiPolishCapture','UiExperienceCapture'")
runner.write_text(text,encoding='utf-8')

source=ROOT/'Assets/Weedtopia/Scripts/Editor/GameSetup/UiExperienceEvidence.cs'
target=source.with_name('UiPolishEvidence.cs')
if not target.exists():
    code=source.read_text(encoding='utf-8-sig').replace('UiExperienceEvidence','UiPolishEvidence').replace('Weedtopia.UiExperienceCapture','Weedtopia.UiPolishCapture').replace('ExperienceCaptureReceipt','PolishCaptureReceipt').replace('Docs/UiExperienceRevision','Docs/UiPolishRevision')
    code=code.replace('"coffeeshop_checklist",()=>root.Ui.Experience.quests.Open("project")','"coffeeshop_checklist",()=>root.Ui.Experience.quests.OpenDetails("project")')
    code=code.replace('screenshots.Count==50','screenshots.Count==steps.Count')
    code=code.replace('if(action.GetComponentInParent<ScrollRect>()!=null)continue;','if(action.GetComponentInParent<ScrollRect>()!=null||action.name=="DialogueTap")continue;')
    target.write_text(code,encoding='utf-8')
print('Prepared explicit authoring and capture runner')
