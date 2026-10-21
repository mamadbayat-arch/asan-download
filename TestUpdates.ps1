$ErrorActionPreference='Stop'
Set-Location -LiteralPath $PSScriptRoot
& "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:exe /main:UpdateTests /out:UpdateTests.exe /resource:Assets/UpdatePublicKey.xml,UpdatePublicKey.xml /resource:Assets/Download.ico,Download.ico /resource:Assets/AsanDownload.png,AsanDownload.png /resource:Assets/Hamtavar.png,Hamtavar.png /resource:Assets/Kalameh-Regular.ttf,Kalameh-Regular.ttf /resource:Assets/Kalameh-Bold.ttf,Kalameh-Bold.ttf /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Web.Extensions.dll /r:Microsoft.CSharp.dll UpdateTests.cs App.cs HistoryStore.cs Engine.cs Glass.cs FdmWidgets.cs Lang.cs Ads.cs Updates.cs Reports.cs Integration.cs DownloadPrompt.cs
if($LASTEXITCODE -ne 0){throw 'Update test build failed'}
& ./UpdateTests.exe
if($LASTEXITCODE -ne 0){throw 'Update tests failed'}
