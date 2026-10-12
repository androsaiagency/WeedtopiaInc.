from pathlib import Path

root = Path(__file__).resolve().parents[2]
p = root / 'Tools/Implementation/Run-UnityValidation.ps1'
s = p.read_text(encoding='utf-8-sig')
if "'UiRefinement'" not in s:
    s = s.replace("[ValidateSet('UiPolish'", "[ValidateSet('UiRefinement','UiRefinementCapture','UiPolish'")
    s = s.replace("@('UiPolishCapture',", "@('UiRefinementCapture','UiPolishCapture',")
    s = s.replace("        'UiPolish' {", "        'UiRefinement' {'Weedtopia.Editor.GameSetup.UiRefinementAuthoring.Apply'}\n        'UiRefinementCapture' {'Weedtopia.Editor.GameSetup.UiRefinementEvidence.Begin'}\n        'UiPolish' {")
    p.write_text(s, encoding='utf-8')
p = root / 'Assets/Weedtopia/Scripts/Editor/GameSetup/UiExperienceAuthoring.cs'
s = p.read_text(encoding='utf-8-sig').replace('PlayerProfile.Avatars','DialogueCharacters.Speakers')
p.write_text(s,encoding='utf-8')
p = root / 'Assets/Weedtopia/Scripts/Tests/EditMode/UiExperienceDomainTests.cs'
s = p.read_text(encoding='utf-8-sig')
for a,b in [('"nia"','"player_female_03"'),('"mila"','"player_female_01"'),('"gardener"','"player_female_04"'),('Is.EqualTo("rocko")','Is.EqualTo(PlayerProfile.DefaultAvatar)'),('\\"rocko\\"','\\"player_male_01\\"')]:
    s=s.replace(a,b)
p.write_text(s,encoding='utf-8')
p = root / 'Assets/Weedtopia/Scripts/Tests/PlayMode/UiExperienceTests.cs'
s=p.read_text(encoding='utf-8-sig').replace('Is.EqualTo("mila"));Assert.That(View.profileFeedback','Is.EqualTo("player_male_02"));Assert.That(View.profileFeedback').replace('Is.EqualTo("mila"));LogAssert','Is.EqualTo("player_male_02"));LogAssert')
s=s.replace('var caption=expected.GetComponentInChildren<Text>(true);Assert.That(caption.enabled,Is.True);Assert.That(caption.text,Is.EqualTo("X"));','var visual=expected.GetComponent<CloseButtonVisual>();Assert.That(visual,Is.Not.Null);Assert.That(visual.caption.enabled,Is.False);Assert.That(visual.icon.enabled,Is.True);Assert.That(visual.icon.sprite,Is.EqualTo(root.Presentation.art.Get("close")));')
p.write_text(s,encoding='utf-8')
