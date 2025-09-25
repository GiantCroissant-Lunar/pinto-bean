param(
    [string]$AssemblyPath
)

Add-Type -AssemblyName System.Runtime
$asm = [Reflection.Assembly]::LoadFrom($AssemblyPath)
$serviceCollectionType = [Microsoft.Extensions.DependencyInjection.ServiceCollection]
$services = [Activator]::CreateInstance($serviceCollectionType)
$methods = $serviceCollectionType.GetMethods([Reflection.BindingFlags]::Public -bor [Reflection.BindingFlags]::Static)
$extensions = $methods | Where-Object { $_.Name -eq 'AddMessagePipe' }
if ($extensions.Count -eq 0) {
    Write-Error 'AddMessagePipe not found'
    exit 1
}
$extension = $extensions[0]
$extension.Invoke($null, @($services)) | Out-Null
$services | ForEach-Object { $_.ServiceType.FullName }
