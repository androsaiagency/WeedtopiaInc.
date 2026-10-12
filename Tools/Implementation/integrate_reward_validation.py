from pathlib import Path
root=Path(__file__).resolve().parents[2]
p=root/'Assets/Weedtopia/Scripts/Editor/GameSetup/InteractionVisualEvidence.cs';s=p.read_text(encoding='utf-8-sig')
s=s.replace('public static void BeginMenuLayout()', 'public static void BeginRewards()\n        {BeginAt("Docs/RewardPresentation");}\n        public static void BeginMenuLayout()',1)
s=s.replace('private static void PrepareSteps()\n        {','private static void PrepareSteps()\n        {\n            if(EvidenceRoot=="Docs/RewardPresentation"){steps=RewardPresentationEvidence.Prepare(root,clock,Capture,SetSize);return;}',1)
p.write_text(s,encoding='utf-8')
p=root/'Tools/Implementation/Run-UnityValidation.ps1';s=p.read_text(encoding='utf-8-sig')
s=s.replace("'ConstructionFences')][string]", "'ConstructionFences','Rewards','RewardsCapture')][string]",1)
s=s.replace("'ConveyorCargoCapture','ConstructionCapture'))", "'ConveyorCargoCapture','ConstructionCapture','RewardsCapture'))")
s=s.replace("'ConstructionFences' {'", "'Rewards' {'Weedtopia.Editor.GameSetup.RewardPresentationAuthoring.Apply'}\n        'RewardsCapture' {'Weedtopia.Editor.GameSetup.InteractionVisualEvidence.BeginRewards'}\n        'ConstructionFences' {'",1)
p.write_text(s,encoding='utf-8')
print('Explicit authoring and Game View reward validation modes registered.')
