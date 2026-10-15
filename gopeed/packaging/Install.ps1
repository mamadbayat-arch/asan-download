$ErrorActionPreference='Stop'
$taskSource=[IO.Path]::GetFullPath($PSScriptRoot)
if(!(Test-Path -LiteralPath (Join-Path $taskSource 'AsanDownload.exe'))){throw 'Extract the Windows bundle before running Install.cmd.'}
$taskTarget=[IO.Path]::GetFullPath((Join-Path $env:LOCALAPPDATA 'Programs/AsanDownloadNext'))
if($taskTarget -ne [IO.Path]::GetFullPath((Join-Path $env:LOCALAPPDATA 'Programs/AsanDownloadNext'))){throw 'Invalid installation directory'}
if(@(Get-Process -Name AsanDownload -ErrorAction SilentlyContinue|Where-Object {$_.Path -eq (Join-Path $taskTarget 'AsanDownload.exe')}).Count){throw 'Exit Asan Download 2 from its tray menu first.'}
New-Item -ItemType Directory -Path $taskTarget -Force|Out-Null
if($taskSource -ne $taskTarget){Get-ChildItem -LiteralPath $taskSource |ForEach-Object {Copy-Item -LiteralPath $_.FullName -Destination $taskTarget -Recurse -Force}}
$taskShell=New-Object -ComObject WScript.Shell
foreach($taskPlace in @([Environment]::GetFolderPath('DesktopDirectory'),[Environment]::GetFolderPath('Programs'))){$taskShortcut=$taskShell.CreateShortcut((Join-Path $taskPlace 'آسان دانلود ۲.lnk'));$taskShortcut.TargetPath=Join-Path $taskTarget 'AsanDownload.exe';$taskShortcut.WorkingDirectory=$taskTarget;$taskShortcut.IconLocation=$taskShortcut.TargetPath+',0';$taskShortcut.Save()}
$taskRegistry='HKCU:/Software/Microsoft/Windows/CurrentVersion/Uninstall/AsanDownloadNext';New-Item -Path $taskRegistry -Force|Out-Null
New-ItemProperty -LiteralPath $taskRegistry -Name DisplayName -Value 'آسان دانلود ۲' -Force|Out-Null
New-ItemProperty -LiteralPath $taskRegistry -Name DisplayVersion -Value '2.0.0' -Force|Out-Null
New-ItemProperty -LiteralPath $taskRegistry -Name Publisher -Value MehrNoor -Force|Out-Null
New-ItemProperty -LiteralPath $taskRegistry -Name UninstallString -Value ('powershell.exe -NoProfile -ExecutionPolicy Bypass -File "'+(Join-Path $taskTarget 'Uninstall.ps1')+'"') -Force|Out-Null
Start-Process -FilePath (Join-Path $taskTarget 'AsanDownload.exe') -WindowStyle Hidden
'Installed Asan Download 2. Select its BrowserExtension folder with Load unpacked to use the new browser bridge.'
