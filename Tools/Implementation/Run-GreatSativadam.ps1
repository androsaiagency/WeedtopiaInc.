[CmdletBinding()]
param(
    [ValidateSet('Install','Capture')][string]$Mode='Install',
    [string]$Label='',
    [string]$UnityEditor='D:\Unity Games\6000.3.9f1\Editor\Unity.exe'
)
$ErrorActionPreference='Stop'
function Get-CaptureSaveSnapshot([string]$PersistentRoot){
    foreach($name in @('burning_hills_v2.json','burning_hills_v2.json.bak','local_leaderboard.json','local_leaderboard.json.bak')){
        $path=Join-Path $PersistentRoot $name
        $exists=Test-Path -LiteralPath $path -PathType Leaf
        [PSCustomObject]@{name=$name;path=$path;exists=$exists;sha256=$(if($exists){(Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()}else{$null})}
    }
}
function Get-CaptureLanguageSnapshot([string]$RegistrySubKey){
    $values=[Collections.Generic.List[object]]::new()
    $key=[Microsoft.Win32.Registry]::CurrentUser.OpenSubKey($RegistrySubKey,$false)
    try{
        if($null-ne$key){
            foreach($name in @($key.GetValueNames()|Where-Object{$_.StartsWith('Weedtopia.Language',[StringComparison]::OrdinalIgnoreCase)}|Sort-Object)){
                $kind=$key.GetValueKind($name)
                $raw=$key.GetValue($name,$null,[Microsoft.Win32.RegistryValueOptions]::DoNotExpandEnvironmentNames)
                $encoding='string';$data=$raw
                switch($kind.ToString()){
                    'Binary' {$encoding='base64';$data=[Convert]::ToBase64String([byte[]]$raw)}
                    'None' {$encoding='base64';$data=[Convert]::ToBase64String([byte[]]$raw)}
                    'DWord' {$encoding='integer';$data=([int]$raw).ToString([Globalization.CultureInfo]::InvariantCulture)}
                    'QWord' {$encoding='integer';$data=([long]$raw).ToString([Globalization.CultureInfo]::InvariantCulture)}
                    'MultiString' {$encoding='strings';$data=[string[]]$raw}
                    'String' {$data=[string]$raw}
                    'ExpandString' {$data=[string]$raw}
                    default {throw ('Unsupported language registry value kind: '+$kind)}
                }
                $values.Add([PSCustomObject]@{name=$name;kind=$kind.ToString();encoding=$encoding;data=$data})
            }
        }
        [PSCustomObject]@{registrySubKey=$RegistrySubKey;registryKeyExisted=($null-ne$key);values=$values.ToArray()}
    }finally{if($null-ne$key){$key.Dispose()}}
}
function Restore-CaptureLanguageSnapshot($Snapshot){
    $removed=[Collections.Generic.List[string]]::new()
    $key=[Microsoft.Win32.Registry]::CurrentUser.OpenSubKey($Snapshot.registrySubKey,$true)
    if($null-eq$key-and$Snapshot.values.Count-gt0){$key=[Microsoft.Win32.Registry]::CurrentUser.CreateSubKey($Snapshot.registrySubKey)}
    try{
        if($null-ne$key){
            $originalNames=@($Snapshot.values|ForEach-Object{$_.name})
            foreach($name in @($key.GetValueNames()|Where-Object{$_.StartsWith('Weedtopia.Language',[StringComparison]::OrdinalIgnoreCase)})){
                if($name-notin$originalNames){$key.DeleteValue($name,$false);$removed.Add($name)}
            }
            foreach($value in $Snapshot.values){
                $kind=[Microsoft.Win32.RegistryValueKind]([Enum]::Parse([Microsoft.Win32.RegistryValueKind],$value.kind))
                $raw=$value.data
                switch($value.encoding){
                    'base64' {$raw=[Convert]::FromBase64String([string]$value.data)}
                    'integer' {if($kind-eq[Microsoft.Win32.RegistryValueKind]::DWord){$raw=[int]::Parse([string]$value.data,[Globalization.CultureInfo]::InvariantCulture)}else{$raw=[long]::Parse([string]$value.data,[Globalization.CultureInfo]::InvariantCulture)}}
                    'strings' {$raw=[string[]]$value.data}
                    'string' {$raw=[string]$value.data}
                    default {throw ('Unsupported language backup encoding: '+$value.encoding)}
                }
                $key.SetValue($value.name,$raw,$kind)
            }
            $key.Flush()
        }
    }finally{if($null-ne$key){$key.Dispose()}}
    return $removed.ToArray()
}
$projectRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$reportRoot=Join-Path $projectRoot 'Docs/GreatSativadam/Validation'
New-Item -ItemType Directory -Force -Path $reportRoot | Out-Null
if((Get-Process Unity -ErrorAction SilentlyContinue)-or(Test-Path -LiteralPath (Join-Path $projectRoot 'Temp/UnityLockfile'))){throw 'Existing Unity editor or lock. No second editor is allowed.'}
$runId=(Get-Date -Format 'yyyyMMdd_HHmmss')+'_'+$Mode+$(if($Label){'_'+$Label}else{''})
$log=Join-Path $reportRoot ($runId+'.log')
$method=if($Mode-eq'Install'){'Weedtopia.Editor.GameSetup.GreatSativadamAuthoring.Apply'}else{'Weedtopia.Editor.GameSetup.GreatSativadamEvidence.Begin'}
$arguments=@('-batchmode','-projectPath',('"'+$projectRoot+'"'),'-logFile',('"'+$log+'"'),'-executeMethod',$method)
if($Mode-eq'Install'){$arguments+=@('-nographics','-quit')}
$settings=@{}
foreach($relative in @('ProjectSettings/ProjectSettings.asset','ProjectSettings/UnityConnectSettings.asset','Assets/Settings/PC_RPAsset.asset','Assets/Settings/UniversalRenderPipelineGlobalSettings.asset')){
    $path=Join-Path $projectRoot $relative
    if(Test-Path -LiteralPath $path){$settings[$path]=[IO.File]::ReadAllBytes($path)}
}
$preservationPassed=$true
if($Mode-eq'Capture'){
    $playerSettings=Get-Content -LiteralPath (Join-Path $projectRoot 'ProjectSettings/ProjectSettings.asset') -Raw
    $companyMatch=[regex]::Match($playerSettings,'(?m)^\s*companyName:\s*(.+?)\s*$')
    $productMatch=[regex]::Match($playerSettings,'(?m)^\s*productName:\s*(.+?)\s*$')
    if(!$companyMatch.Success-or!$productMatch.Success){throw 'Cannot identify the actual Unity company/product for player-state preservation.'}
    $company=$companyMatch.Groups[1].Value.Trim();$product=$productMatch.Groups[1].Value.Trim()
    if($company-ne'DefaultCompany'-or$product-ne'Weedtopia'){throw 'Player-state preservation requires the verified DefaultCompany/Weedtopia project identity.'}
    $persistentRoot=Join-Path ([Environment]::GetFolderPath('UserProfile')) ('AppData/LocalLow/'+$company+'/'+$product)
    $registrySubKey='Software\Unity\UnityEditor\'+$company+'\'+$product
    $saveBefore=@(Get-CaptureSaveSnapshot $persistentRoot)
    $languageBefore=Get-CaptureLanguageSnapshot $registrySubKey
    $languageBackup=Join-Path $reportRoot ($runId+'_playerprefs_before.json')
    $backup=[ordered]@{utc=[DateTime]::UtcNow.ToString('o');run=$runId;company=$company;product=$product;languagePreferenceKey='Weedtopia.Language';registryPath=('HKCU\'+$registrySubKey);language=$languageBefore;saveFiles=$saveBefore}
    [IO.File]::WriteAllText($languageBackup,($backup|ConvertTo-Json -Depth 10),[Text.UTF8Encoding]::new($false))
}
$started=[DateTime]::UtcNow
$process=Start-Process -FilePath $UnityEditor -ArgumentList $arguments -WindowStyle Hidden -PassThru
Write-Output ('Owned Unity PID '+$process.Id+'; '+$runId)
try{$process.WaitForExit()}finally{
    try{foreach($path in $settings.Keys){[IO.File]::WriteAllBytes($path,$settings[$path])}}finally{
    if($Mode-eq'Capture'){
        $preservationErrors=[Collections.Generic.List[string]]::new()
        $removedLanguageValues=@();$saveAfter=@();$languageAfter=$null
        try{$removedLanguageValues=@(Restore-CaptureLanguageSnapshot $languageBefore)}catch{$preservationErrors.Add('Language restore: '+$_.Exception.Message)}
        try{$saveAfter=@(Get-CaptureSaveSnapshot $persistentRoot)}catch{$preservationErrors.Add('Save verification: '+$_.Exception.Message)}
        try{$languageAfter=Get-CaptureLanguageSnapshot $registrySubKey}catch{$preservationErrors.Add('Language verification: '+$_.Exception.Message)}
        $saveFilesUnchanged=$saveAfter.Count-eq$saveBefore.Count
        foreach($before in $saveBefore){
            $after=@($saveAfter|Where-Object{$_.path-eq$before.path})
            if($after.Count-ne1-or$after[0].exists-ne$before.exists-or$after[0].sha256-ne$before.sha256){$saveFilesUnchanged=$false;$preservationErrors.Add('Real player save changed: '+$before.path)}
        }
        $languageRestored=$null-ne$languageAfter-and(ConvertTo-Json -InputObject $languageBefore.values -Depth 8 -Compress)-eq(ConvertTo-Json -InputObject $languageAfter.values -Depth 8 -Compress)
        if(!$languageRestored){$preservationErrors.Add('Original language registry values were not restored exactly.')}
        $preservationPassed=$saveFilesUnchanged-and$languageRestored-and$preservationErrors.Count-eq0
        $preservationReceipt=Join-Path $projectRoot 'Docs/GreatSativadam/player_state_preservation.json'
        $preservation=[ordered]@{run=$runId;startedUtc=$started.ToString('o');endedUtc=[DateTime]::UtcNow.ToString('o');status=$(if($preservationPassed){'passed'}else{'failed'});persistentDataPath=$persistentRoot;saveFilesUnchanged=$saveFilesUnchanged;languageRestored=$languageRestored;languagePreferenceKey='Weedtopia.Language';registryPath=('HKCU\'+$registrySubKey);languageBackup=$languageBackup;saveFilesBefore=$saveBefore;saveFilesAfter=$saveAfter;languageBefore=$languageBefore;languageAfter=$languageAfter;newLanguageValuesRemoved=$removedLanguageValues;errors=$preservationErrors.ToArray()}
        [IO.File]::WriteAllText($preservationReceipt,($preservation|ConvertTo-Json -Depth 10),[Text.UTF8Encoding]::new($false))
    }
    }
}
$receipt=Join-Path $projectRoot $(if($Mode-eq'Install'){'Docs/GreatSativadam/authoring_receipt.json'}else{'Docs/GreatSativadam/gameview_receipt.json'})
$result=[ordered]@{run=$runId;mode=$Mode;startedUtc=$started.ToString('o');endedUtc=[DateTime]::UtcNow.ToString('o');exitCode=$process.ExitCode;log=$log;status='unverified'}
if($Mode-eq'Capture'){$result.playerStatePreservation=$preservationReceipt;$result.playerStatePreserved=$preservationPassed}
if((Test-Path -LiteralPath $receipt)-and(Get-Item -LiteralPath $receipt).LastWriteTimeUtc-ge$started){
    $data=Get-Content -LiteralPath $receipt -Raw|ConvertFrom-Json
    $valid=$process.ExitCode-eq0-and$data.status-eq'passed'
    if($Mode-eq'Capture'){
        $fresh=@($data.screenshots|Where-Object{!(Test-Path -LiteralPath $_)-or(Get-Item -LiteralPath $_).LastWriteTimeUtc-lt$started}).Count-eq0
        $layout=Join-Path $projectRoot 'Docs/GreatSativadam/ui_overflow.json'
        $ui=if(Test-Path -LiteralPath $layout){Get-Content -LiteralPath $layout -Raw|ConvertFrom-Json}else{$null}
        $valid=$valid-and$preservationPassed-and$fresh-and$data.screenshots.Count-eq89-and$data.savedStoryRoundtrip-and$data.housingInspectionMatchesSimulation-and$data.returnedToPreviousCity-and$data.allThirteenCitiesVisited-and$data.thirteenCityOfflineRoundtrip-and$data.joostChoiceVariantsVerified-and$data.architectureGroundAnchorsVerified-and$data.matrix.Count-eq60-and$ui.passed
    }
    $result.status=if($valid){'passed'}else{'failed'};$result.receipt=$receipt
}
$result|ConvertTo-Json -Depth 5|Set-Content -LiteralPath (Join-Path $reportRoot ($runId+'.json')) -Encoding utf8
$result|ConvertTo-Json -Depth 5
if($result.status-ne'passed'){exit 1}
