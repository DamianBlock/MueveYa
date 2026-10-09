#!/usr/bin/env bash
set -euo pipefail

# Ajusta el nombre de la migración según convenga
MIGRATION_NAME=${1:-AutoMigration}
PROJECT_PATH="./AppFletesMueve.Api"
STARTUP_PROJECT_PATH="./AppFletesMueve.Api"

echo "Restaurando herramientas locales..."
dotnet tool restore

echo "Creando migración: $MIGRATION_NAME"
dotnet ef migrations add "$MIGRATION_NAME" --project "$PROJECT_PATH" --startup-project "$STARTUP_PROJECT_PATH"

echo "Aplicando migraciones a la base de datos"
dotnet ef database update --project "$PROJECT_PATH" --startup-project "$STARTUP_PROJECT_PATH"

echo "Listo. Revisa los archivos en $PROJECT_PATH/Migrations y haz commit." 
