$ErrorActionPreference='Stop'
$taskTarget=[IO.Path]::GetFullPath($PSScriptRoot)
$taskAllowed=[IO.Path]::GetFullPath((Join-Path $env:LOCALAPPDATA 'Programs/AsanDownloadNext'))
if($taskTarget -ne $taskAllowed){throw 'Invalid uninstall directory'}
if(@(Get-Process -Name AsanDownload -ErrorAction SilentlyContinue|Where-Object {$_.Path -eq (Join-Path $taskTarget 'AsanDownload.exe')}).Count){throw 'Exit Asan Download 2 from its tray menu first.'}
foreach($taskPlace in @([Environment]::GetFolderPath('DesktopDirectory'),[Environment]::GetFolderPath('Programs'))){Remove-Item -LiteralPath (Join-Path $taskPlace 'آسان دانلود ۲.lnk') -ErrorAction SilentlyContinue}
Remove-Item -LiteralPath 'HKCU:/Software/Microsoft/Windows/CurrentVersion/Uninstall/AsanDownloadNext' -ErrorAction SilentlyContinue
foreach($taskBrowser in @('Google/Chrome','Microsoft/Edge')){Remove-Item -LiteralPath ('HKCU:/Software/'+$taskBrowser+'/NativeMessagingHosts/ir.hamtavar.asandownload2') -ErrorAction SilentlyContinue}
$taskRun='HKCU:/Software/Microsoft/Windows/CurrentVersion/Run'
if(Test-Path -LiteralPath $taskRun){$taskValues=Get-ItemProperty -LiteralPath $taskRun;foreach($taskProperty in $taskValues.PSObject.Properties){if($taskProperty.Value -is [string] -and $taskProperty.Value.IndexOf((Join-Path $taskTarget 'AsanDownload.exe'),[StringComparison]::OrdinalIgnoreCase) -ge 0){Remove-ItemProperty -LiteralPath $taskRun -Name $taskProperty.Name}}}
Remove-Item -LiteralPath $taskTarget -Recurse -Force
# Downloaded files, legacy history and the AsanDownload profile are retained.
