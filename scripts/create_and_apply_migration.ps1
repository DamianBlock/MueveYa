param(
	[string]$MigrationName = "AutoMigration"
)

$projectPath = "./AppFletesMueve.Api"
$startupProject = "./AppFletesMueve.Api"

Write-Host "Restaurando herramientas locales..."
dotnet tool restore

Write-Host "Creando migración: $MigrationName"
dotnet ef migrations add $MigrationName --project $projectPath --startup-project $startupProject

Write-Host "Aplicando migraciones a la base de datos"
dotnet ef database update --project $projectPath --startup-project $startupProject

Write-Host "Listo. Revisa los archivos en $projectPath\Migrations y haz commit."
