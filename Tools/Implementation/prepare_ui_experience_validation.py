from pathlib import Path
root=Path(__file__).resolve().parents[2]
file=root/'Tools/Implementation/Run-UnityValidation.ps1'
text=file.read_text(encoding='utf-8-sig')
if "'UiExperience','UiExperienceCapture','EditMode'" not in text:
    text=text.replace("[ValidateSet('EditMode'", "[ValidateSet('UiExperience','UiExperienceCapture','EditMode'",1)
text=text.replace("@('Capture','ProductionCapture'", "@('UiExperienceCapture','Capture','ProductionCapture'")
file.write_text(text,encoding='utf-8-sig')
print('UI experience installer and capture modes registered.')
