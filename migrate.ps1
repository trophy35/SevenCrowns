param(
  # Path to Assets folder (default: ./Assets)
  [string]$AssetsPath = (Join-Path (Get-Location) "Assets"),
  # Dry run (no file changes)
  [switch]$WhatIf
)

########################################################################
# Helpers
########################################################################
function New-Dir {
  param([string]$Path)
  if (-not (Test-Path -LiteralPath $Path)) {
    if ($WhatIf) { Write-Host "[DRY] MKDIR $Path"; return }
    New-Item -ItemType Directory -Path $Path | Out-Null
    Write-Host "[+] Dir  $Path"
  } else {
    Write-Host "[-] Skip $Path (exists)"
  }
}

function Backup-File {
  param([string]$FilePath, [string]$BackupRoot)
  if (-not (Test-Path -LiteralPath $FilePath)) { return }
  $rel = Resolve-Path -LiteralPath $FilePath | Split-Path -NoQualifier
  $dst = Join-Path $BackupRoot $rel
  $dstDir = Split-Path $dst -Parent
  New-Dir -Path $dstDir
  if ($WhatIf) { Write-Host "[DRY] BACKUP $FilePath -> $dst"; return }
  Copy-Item -LiteralPath $FilePath -Destination $dst -Force
  Write-Host "[~] Backup $FilePath"
}

function Remove-File {
  param([string]$FilePath)
  if (-not (Test-Path -LiteralPath $FilePath)) { return }
  if ($WhatIf) { Write-Host "[DRY] DEL   $FilePath"; return }
  Remove-Item -LiteralPath $FilePath -Force
  Write-Host "[-] Del  $FilePath"
}

function Write-AsmdefJson {
  param(
    [string]$Path,
    [string]$Name,
    [string[]]$References = @(),
    [string[]]$IncludePlatforms = @(),
    [string[]]$ExcludePlatforms = @(),
    [bool]$AllowUnsafeCode = $false,
    [bool]$OverrideReferences = $false,
    [string[]]$PrecompiledReferences = @(),
    [bool]$AutoReferenced = $true,
    [string[]]$DefineConstraints = @(),
    [bool]$NoEngineReferences = $false,
    [string]$RootNamespace = "",
    [hashtable[]]$VersionDefines = @(),
    [string[]]$OptionalUnityReferences = @()
  )

  $obj = [ordered]@{
    name = $Name
    rootNamespace = $RootNamespace
    references = $References
    includePlatforms = $IncludePlatforms
    excludePlatforms = $ExcludePlatforms
    allowUnsafeCode = $AllowUnsafeCode
    overrideReferences = $OverrideReferences
    precompiledReferences = $PrecompiledReferences
    autoReferenced = $AutoReferenced
    defineConstraints = $DefineConstraints
    noEngineReferences = $NoEngineReferences
    versionDefines = $VersionDefines
    optionalUnityReferences = $OptionalUnityReferences
  }

  $json = ($obj | ConvertTo-Json -Depth 10)
  $dir = Split-Path $Path -Parent
  New-Dir -Path $dir
  if ($WhatIf) { Write-Host "[DRY] WRITE $Path`n$json"; return }
  $json | Out-File -FilePath $Path -Encoding UTF8 -NoNewline
  Write-Host "[+] Asm  $Path"
}

########################################################################
# Backup & remove existing granular asmdefs from the previous script
########################################################################
$backupRoot = Join-Path (Split-Path $AssetsPath -Parent) ("AsmdefBackup_" + (Get-Date -Format "yyyyMMdd_HHmmss"))
New-Dir -Path $backupRoot

# Patterns created by the previous script (adjust as needed)
$removePatterns = @(
  "_Project.Shared.Core.Runtime",
  "_Project.Shared.Core.Editor",
  "_Project.Shared.Core.Tests",
  "_Project.Shared.Utilities.Runtime",
  "_Project.Shared.Utilities.Editor",
  "_Project.Shared.Utilities.Tests",
  "_Project.Systems.SceneFlow.Runtime",
  "_Project.Systems.SceneFlow.Editor",
  "_Project.Systems.SceneFlow.Tests",
  "_Project.Systems.SaveSystem.Runtime",
  "_Project.Systems.SaveSystem.Editor",
  "_Project.Systems.SaveSystem.Tests",
  "_Project.Systems.AudioSystem.Runtime",
  "_Project.Systems.AudioSystem.Editor",
  "_Project.Systems.AudioSystem.Tests",
  "_Project.Systems.LocalizationSystem.Runtime",
  "_Project.Systems.LocalizationSystem.Editor",
  "_Project.Systems.LocalizationSystem.Tests",
  "_Project.Gameplay.Map.Runtime",
  "_Project.Gameplay.Map.Editor",
  "_Project.Gameplay.Map.Tests",
  "_Project.Gameplay.Battle.Runtime",
  "_Project.Gameplay.Battle.Editor",
  "_Project.Gameplay.Battle.Tests",
  "_Project.UI.Screens.MainMenu",
  "_Project.UI.Screens.MapHUD",
  "_Project.UI.Widgets"
)

# Find every *.asmdef under Assets and remove those matching names above
$allAsmdefs = Get-ChildItem -LiteralPath $AssetsPath -Recurse -Filter "*.asmdef"
foreach ($asm in $allAsmdefs) {
  try {
    $content = Get-Content -LiteralPath $asm.FullName -Raw
    $nameMatch = ($content | ConvertFrom-Json).name
  } catch {
    continue
  }

  if ($removePatterns -contains $nameMatch) {
    Backup-File -FilePath $asm.FullName -BackupRoot $backupRoot
    Remove-File -FilePath $asm.FullName
  }
}

########################################################################
# Create target folders for the new, cleaner assemblies
########################################################################
$rootNS = "SevenCrowns"
$targets = @{
  "Core"     = Join-Path $AssetsPath "_Project/Scripts/Core"
  "Config"   = Join-Path $AssetsPath "_Project/Scripts/Config"
  "Systems"  = Join-Path $AssetsPath "_Project/Scripts/Systems"
  "UI"       = Join-Path $AssetsPath "_Project/Scripts/UI"
  "Boot"     = Join-Path $AssetsPath "_Project/Scripts/Boot"
  "Gameplay" = Join-Path $AssetsPath "_Project/Scripts/Gameplay"
  "Editor"   = Join-Path $AssetsPath "_Project/Scripts/Editor"
  "TestsEdit"= Join-Path $AssetsPath "Tests/EditMode"
  "TestsPlay"= Join-Path $AssetsPath "Tests/PlayMode"
}
$targets.GetEnumerator() | ForEach-Object { New-Dir -Path $_.Value }

########################################################################
# Create new asmdefs (clean, modular, SOLID-friendly)
########################################################################

# 1) SevenCrowns.Core (no dependencies)
Write-AsmdefJson -Path (Join-Path $targets.Core "SevenCrowns.Core.asmdef") `
  -Name "SevenCrowns.Core" `
  -RootNamespace "$rootNS.Core"

# 2) SevenCrowns.Config (depends on Core)
Write-AsmdefJson -Path (Join-Path $targets.Config "SevenCrowns.Config.asmdef") `
  -Name "SevenCrowns.Config" `
  -References @("SevenCrowns.Core") `
  -RootNamespace "$rootNS.Config"

# 3) SevenCrowns.Systems (depends on Core) — add Addressables define if you plan to use it here
Write-AsmdefJson -Path (Join-Path $targets.Systems "SevenCrowns.Systems.asmdef") `
  -Name "SevenCrowns.Systems" `
  -References @("SevenCrowns.Core") `
  -RootNamespace "$rootNS.Systems" `
  -VersionDefines @(@{
    name = "Addressables"; define = "ADDRESSABLES"; expression = "com.unity.addressables"
  })

# 4) SevenCrowns.UI (depends on Core, optional TMP)
Write-AsmdefJson -Path (Join-Path $targets.UI "SevenCrowns.UI.asmdef") `
  -Name "SevenCrowns.UI" `
  -References @("SevenCrowns.Core","Unity.TextMeshPro") `
  -RootNamespace "$rootNS.UI"

# 5) SevenCrowns.Boot (depends on Core, Systems, UI, Config + Addressables/ResourceManager)
Write-AsmdefJson -Path (Join-Path $targets.Boot "SevenCrowns.Boot.asmdef") `
  -Name "SevenCrowns.Boot" `
  -References @(
      "SevenCrowns.Core",
      "SevenCrowns.Systems",
      "SevenCrowns.UI",
      "SevenCrowns.Config",
      "Unity.Addressables",
      "Unity.ResourceManager"
  ) `
  -RootNamespace "$rootNS.Boot" `
  -VersionDefines @(@{
    name = "Addressables"; define = "ADDRESSABLES"; expression = "com.unity.addressables"
  })

# 6) SevenCrowns.Gameplay (depends on Core, Systems, Config) — NO dependency on UI/Boot
Write-AsmdefJson -Path (Join-Path $targets.Gameplay "SevenCrowns.Gameplay.asmdef") `
  -Name "SevenCrowns.Gameplay" `
  -References @("SevenCrowns.Core","SevenCrowns.Systems","SevenCrowns.Config") `
  -RootNamespace "$rootNS.Gameplay"

# 7) SevenCrowns.Editor (Editor-only; depends on the runtime assemblies you need)
Write-AsmdefJson -Path (Join-Path $targets.Editor "SevenCrowns.Editor.asmdef") `
  -Name "SevenCrowns.Editor" `
  -IncludePlatforms @("Editor") `
  -References @("SevenCrowns.Core","SevenCrowns.Systems","SevenCrowns.UI","SevenCrowns.Config","SevenCrowns.Gameplay") `
  -RootNamespace "$rootNS.Editor"

# 8) Tests — EditMode
Write-AsmdefJson -Path (Join-Path $targets.TestsEdit "SevenCrowns.Tests.EditMode.asmdef") `
  -Name "SevenCrowns.Tests.EditMode" `
  -OptionalUnityReferences @("TestAssemblies") `
  -References @("SevenCrowns.Core","SevenCrowns.Systems","SevenCrowns.Gameplay") `
  -RootNamespace "$rootNS.Tests.EditMode"

# 9) Tests — PlayMode
Write-AsmdefJson -Path (Join-Path $targets.TestsPlay "SevenCrowns.Tests.PlayMode.asmdef") `
  -Name "SevenCrowns.Tests.PlayMode" `
  -OptionalUnityReferences @("TestAssemblies") `
  -References @("SevenCrowns.Core","SevenCrowns.Systems","SevenCrowns.Gameplay") `
  -RootNamespace "$rootNS.Tests.PlayMode"

Write-Host "`nDone. Return to Unity and let it recompile. If errors show up, check missing packages (TMP, Addressables)."
