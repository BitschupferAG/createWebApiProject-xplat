# Installer für createWebApiProject-xplat (Windows).
#   irm https://raw.githubusercontent.com/BitschupferAG/createWebApiProject-xplat/main/install.ps1 | iex
# Kann gefahrlos mehrfach ausgeführt werden; ein zweiter Lauf aktualisiert.
$ErrorActionPreference = 'Stop'
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

$repo       = 'BitschupferAG/createWebApiProject-xplat'
$installDir = Join-Path $env:USERPROFILE '.createWebApiProject-xplat'
$dotnetDir  = Join-Path $env:USERPROFILE '.dotnet'
$toolsDir   = Join-Path $dotnetDir 'tools'
$env:Path   = "$toolsDir;$env:Path"

function Get-SdkMajor {
  try { [int]((& dotnet --version 2>$null) -split '\.')[0] } catch { 0 }
}
function Info($msg) { Write-Host $msg -ForegroundColor Cyan }

# 1. .NET 10 SDK
$newPaths = @($installDir, $toolsDir)
if ((Get-SdkMajor) -lt 10) {
  Info "Installiere .NET 10 SDK nach $dotnetDir ..."
  $installer = (Invoke-WebRequest 'https://dot.net/v1/dotnet-install.ps1' -UseBasicParsing).Content
  & ([scriptblock]::Create($installer)) -Channel 10.0 -InstallDir $dotnetDir
  $env:Path = "$dotnetDir;$env:Path"
  $env:DOTNET_ROOT = $dotnetDir
  [Environment]::SetEnvironmentVariable('DOTNET_ROOT', $dotnetDir, 'User')
  $newPaths += $dotnetDir
  if ((Get-SdkMajor) -lt 10) { throw '.NET 10 SDK konnte nicht installiert werden.' }
} else {
  Info ".NET SDK $(& dotnet --version) ist bereits installiert."
}

# 2. dotnet-ef 10
if ((& dotnet tool list -g) -match '^dotnet-ef\s') {
  Info 'Aktualisiere dotnet-ef ...'
  & dotnet tool update -g dotnet-ef --version '10.*'
} else {
  Info 'Installiere dotnet-ef ...'
  & dotnet tool install -g dotnet-ef --version '10.*'
}
if ($LASTEXITCODE -ne 0) { throw 'dotnet-ef konnte nicht installiert werden.' }

# 3. Tool herunterladen
# Neueste Release-Version per Redirect ermitteln (statt GitHub-API: kein Rate-Limit bei vielen Schülern hinter einer IP)
$req = [Net.WebRequest]::Create("https://github.com/$repo/releases/latest")
$req.AllowAutoRedirect = $false
$resp = $req.GetResponse()
$tag = ($resp.Headers['Location'] -split '/')[-1]
$resp.Close()
if ($tag -notlike 'v*') { throw 'Keine Release-Version auf GitHub gefunden.' }

Info "Lade createWebApiProject-xplat $tag nach $installDir ..."
$tmp = Join-Path ([IO.Path]::GetTempPath()) ([Guid]::NewGuid())
New-Item -ItemType Directory $tmp | Out-Null
try {
  $zip = Join-Path $tmp 'tool.zip'
  Invoke-WebRequest "https://github.com/$repo/archive/refs/tags/$tag.zip" -OutFile $zip -UseBasicParsing
  Expand-Archive $zip (Join-Path $tmp 'src')
  $extracted = Get-ChildItem (Join-Path $tmp 'src') -Directory | Select-Object -First 1
  if (Test-Path $installDir) { Remove-Item $installDir -Recurse -Force }
  Move-Item $extracted.FullName $installDir
  Set-Content (Join-Path $installDir 'VERSION') $tag
} finally {
  Remove-Item $tmp -Recurse -Force -ErrorAction SilentlyContinue
}

# 4. PATH (Benutzer) ergänzen, ohne Duplikate
$userPath = @(([Environment]::GetEnvironmentVariable('Path', 'User') -split ';') | Where-Object { $_ })
foreach ($p in $newPaths) {
  if ($userPath -notcontains $p) { $userPath += $p }
}
[Environment]::SetEnvironmentVariable('Path', ($userPath -join ';'), 'User')

Write-Host "`nFertig! " -ForegroundColor Green -NoNewline
Write-Host "Öffne ein neues Terminal und probiere:`n  createWebApi-xplat MyBackend ."
