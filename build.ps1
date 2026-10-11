param(
    [string]$Dotnet = 'D:\Codex\tools\dotnet\dotnet.exe',
    [string]$AndroidSdk = 'D:\Codex\tools\android-sdk',
    [string]$JavaSdk = 'D:\Codex\tools\jdk',
    [string]$Output = (Join-Path $PSScriptRoot '..\Luma-Android'),
    [string]$SigningDirectory = (Join-Path $PSScriptRoot '..\Luma-Signing')
)
$ErrorActionPreference = 'Stop'
$env:DOTNET_CLI_HOME = 'D:\Codex\tools\dotnet-home'
$env:NUGET_PACKAGES = 'D:\Codex\tools\nuget'
$env:TEMP = 'D:\Codex\tools\temp'
$env:TMP = $env:TEMP
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
New-Item -ItemType Directory -Force -Path $Output,$SigningDirectory,$env:TEMP | Out-Null
$signingFile = Join-Path $SigningDirectory 'signing.local.json'
$keyStore = [System.IO.Path]::GetFullPath((Join-Path $SigningDirectory 'luma-release.keystore'))
if (-not (Test-Path -LiteralPath $signingFile)) {
    if (Test-Path -LiteralPath $keyStore) { throw 'Найден ключ без signing.local.json. Восстановите настройки подписи из резервной копии.' }
    $password = [Guid]::NewGuid().ToString('N') + [Guid]::NewGuid().ToString('N')
    $env:LUMA_SIGNPASS = $password
    & (Join-Path $JavaSdk 'bin\keytool.exe') -genkeypair -keystore $keyStore -alias luma -keyalg RSA -keysize 2048 -validity 10000 -dname 'CN=Luma' -storepass:env LUMA_SIGNPASS -keypass:env LUMA_SIGNPASS -noprompt
    if ($LASTEXITCODE -ne 0) { throw 'Не удалось создать ключ подписи.' }
    @{ Password = $password; Alias = 'luma' } | ConvertTo-Json | Set-Content -LiteralPath $signingFile -Encoding UTF8
}
$signing = Get-Content -LiteralPath $signingFile -Raw | ConvertFrom-Json
$env:LUMA_SIGNPASS = $signing.Password
try {
    & $Dotnet publish (Join-Path $PSScriptRoot 'Luma.csproj') -c Release "-p:AndroidSdkDirectory=$AndroidSdk" "-p:JavaSdkDirectory=$JavaSdk" -p:AndroidKeyStore=true "-p:AndroidSigningKeyStore=$keyStore" "-p:AndroidSigningKeyAlias=$($signing.Alias)" -p:AndroidSigningStorePass=env:LUMA_SIGNPASS -p:AndroidSigningKeyPass=env:LUMA_SIGNPASS
    if ($LASTEXITCODE -ne 0) { throw 'Не удалось собрать APK.' }
    $apk = Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'bin\Release\net8.0-android\publish') -Filter '*-Signed.apk' | Select-Object -First 1
    if (-not $apk) { throw 'Подписанный APK не найден.' }
    Copy-Item -LiteralPath $apk.FullName -Destination (Join-Path $Output 'Luma-1.0.0.apk') -Force
    Write-Output (Join-Path $Output 'Luma-1.0.0.apk')
} finally { Remove-Item Env:\LUMA_SIGNPASS -ErrorAction SilentlyContinue }
