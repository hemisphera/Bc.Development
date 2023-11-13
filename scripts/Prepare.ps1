$ProjectRoot = Resolve-Path "$PSScriptRoot\.."
$ExternalAssembliesFolder = Join-Path $ProjectRoot "_external"

$FrameworkUiClientPath = "$ExternalAssembliesFolder/Microsoft.Dynamics.Framework.UI.Client.dll"
Invoke-WebRequest $env:FRAMEWORK_UI_CLIENT_URI -OutFile $FrameworkUiClientPath
Unblock-File $FrameworkUiClientPath