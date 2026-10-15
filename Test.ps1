$ErrorActionPreference = 'Stop'
Set-Location -LiteralPath $PSScriptRoot
& "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:exe /out:Tests.exe /r:System.Web.Extensions.dll Tests.cs Engine.cs
if ($LASTEXITCODE -ne 0) { throw 'Test build failed' }
& "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:exe /main:InterfaceTests /out:InterfaceTests.exe /resource:Assets/UpdatePublicKey.xml,UpdatePublicKey.xml /resource:Assets/Download.ico,Download.ico /resource:Assets/AsanDownload.png,AsanDownload.png /resource:Assets/Hamtavar.png,Hamtavar.png /resource:Assets/Kalameh-Regular.ttf,Kalameh-Regular.ttf /resource:Assets/Kalameh-Bold.ttf,Kalameh-Bold.ttf /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Web.Extensions.dll /r:Microsoft.CSharp.dll InterfaceTests.cs App.cs HistoryStore.cs Engine.cs Glass.cs Ads.cs Updates.cs Reports.cs Integration.cs DownloadPrompt.cs
if ($LASTEXITCODE -ne 0) { throw 'Interface test build failed' }
$testServer = Start-Process node -ArgumentList 'TestServer.js' -WindowStyle Hidden -PassThru
try {
    Start-Sleep -Milliseconds 800
    & .\Tests.exe
    if ($LASTEXITCODE -ne 0) { throw 'Tests failed' }
    & .\InterfaceTests.exe
    if ($LASTEXITCODE -ne 0) { throw 'Interface tests failed' }
} finally { Stop-Process -Id $testServer.Id -ErrorAction SilentlyContinue }
