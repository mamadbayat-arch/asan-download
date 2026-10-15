param([string]$Version='2.0.1',[switch]$SkipTests)
$ErrorActionPreference='Stop'
$taskProject=$PSScriptRoot
$taskSource=Join-Path $taskProject 'gopeed'
Push-Location -LiteralPath $taskSource
try {
 foreach($taskTool in @('go','flutter')){if(!(Get-Command $taskTool -ErrorAction SilentlyContinue)){throw "$taskTool is required. See GOPEED-FORK.md."}}
 $env:CGO_ENABLED='1'
 if(!$SkipTests){& go test ./cmd/asanhost; if($LASTEXITCODE -ne 0){throw 'Browser host tests failed'}}
 & go build -tags nosqlite -ldflags "-w -s -X github.com/GopeedLab/gopeed/pkg/base.Version=$Version" -buildmode=c-shared -o ui/flutter/windows/libgopeed.dll ./bind/desktop
 if($LASTEXITCODE -ne 0){throw 'Go engine build failed'}
 New-Item -ItemType Directory -Path ui/flutter/assets/exec -Force|Out-Null
 & go build -ldflags '-w -s' -o ui/flutter/assets/exec/host.exe ./cmd/asanhost
 if($LASTEXITCODE -ne 0){throw 'Browser host build failed'}
 & go build -ldflags '-w -s' -o ui/flutter/assets/exec/updater.exe ./cmd/updater
 if($LASTEXITCODE -ne 0){throw 'Updater build failed'}
 Push-Location -LiteralPath ui/flutter
 try {
  & flutter pub get;if($LASTEXITCODE -ne 0){throw 'Flutter dependencies failed'}
  if(!$SkipTests){& flutter test;if($LASTEXITCODE -ne 0){throw 'Flutter tests failed'}}
  & flutter build windows --release --build-name=$Version --build-number=1 --dart-define=UPDATE_CHANNEL=windowsPortable
  if($LASTEXITCODE -ne 0){throw 'Windows build failed'}
  $taskBundle=Join-Path $PWD 'build/windows/x64/runner/Release'
  $taskGcc=Get-Command gcc -ErrorAction SilentlyContinue
  $taskRuntimeDirs=@('C:/Program Files/Git/mingw64/bin','C:/mingw64/bin')
  if($taskGcc){$taskRuntimeDirs=@((Split-Path $taskGcc.Source))+$taskRuntimeDirs}
  foreach($taskDll in @('libstdc++-6.dll','libgcc_s_seh-1.dll','libwinpthread-1.dll')){
   foreach($taskRuntimeDir in $taskRuntimeDirs){$taskCandidate=Join-Path $taskRuntimeDir $taskDll;if(Test-Path -LiteralPath $taskCandidate){Copy-Item -LiteralPath $taskCandidate -Destination $taskBundle -Force;break}}
  }
  foreach($taskDll in @('msvcp140.dll','vcruntime140.dll','vcruntime140_1.dll')){$taskCandidate=Join-Path $env:WINDIR "System32/$taskDll";if(Test-Path -LiteralPath $taskCandidate){Copy-Item -LiteralPath $taskCandidate -Destination $taskBundle -Force}}
  Copy-Item -LiteralPath (Join-Path $taskSource 'LICENSE'),(Join-Path $taskSource 'ASAN-NOTICE.md') -Destination $taskBundle
  Copy-Item -LiteralPath (Join-Path $taskSource 'packaging/Install.cmd'),(Join-Path $taskSource 'packaging/Install.ps1'),(Join-Path $taskSource 'packaging/Uninstall.ps1') -Destination $taskBundle
  Copy-Item -LiteralPath (Join-Path $taskSource 'browser-extension') -Destination (Join-Path $taskBundle 'BrowserExtension') -Recurse -Force
  $taskOut=Join-Path $taskProject 'GopeedRelease';New-Item -ItemType Directory -Path $taskOut -Force|Out-Null
  Compress-Archive -Path (Join-Path $taskBundle '*') -DestinationPath (Join-Path $taskOut "AsanDownload-Gopeed-$Version-Windows.zip") -Force
  Write-Output "Windows bundle ready: $taskOut"
 } finally {Pop-Location}
} finally {Pop-Location}
