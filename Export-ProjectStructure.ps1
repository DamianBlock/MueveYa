<#
.SINOPSIS
  Exporta el árbol de carpetas y el contenido de los archivos relevantes
  de la solución AppFletesMueve a un único .txt, para compartir el contexto
  completo del proyecto.

.USO
  1) Copiá este archivo en la carpeta raíz de la solución (donde está AppFletesMueve.slnx).
  2) Abrí PowerShell ahí y ejecutá:
       .\Export-ProjectStructure.ps1
  3) Se genera "ProjectStructure_export.txt" en la misma carpeta. Compartí ese archivo.
#>

param(
    [string]$RootPath = ".",
    [string]$OutputFile = "ProjectStructure_export.txt"
)

# Carpetas a excluir completamente
$excludeDirs = @('bin', 'obj', '.git', '.vs', 'TestResults', 'packages', '.idea')

# Extensiones cuyo CONTENIDO queremos volcar
$includeExtensions = @('.cs', '.xaml', '.csproj', '.slnx', '.sln', '.json', '.http', '.config', '.editorconfig')

# Archivos generados que no aportan y son muy pesados/ruidosos
$excludeFilePatterns = @('*.Designer.cs', '*.AssemblyInfo.cs', '*.g.cs', '*.g.i.cs', '*AssemblyAttributes.cs')

function Test-ExcludedPath {
    param([string]$Path)
    foreach ($dir in $excludeDirs) {
        if ($Path -match "[\\/]$([regex]::Escape($dir))[\\/]" -or $Path -match "[\\/]$([regex]::Escape($dir))$") {
            return $true
        }
    }
    return $false
}

function Test-ExcludedFile {
    param([string]$FileName)
    foreach ($pattern in $excludeFilePatterns) {
        if ($FileName -like $pattern) { return $true }
    }
    return $false
}

$rootFull = (Resolve-Path $RootPath).Path
$sw = New-Object System.IO.StreamWriter($OutputFile, $false, [System.Text.Encoding]::UTF8)

Write-Host "Generando arbol de carpetas..."
$sw.WriteLine("=== ARBOL DE CARPETAS ===")
$sw.WriteLine("Raiz: $rootFull")
$sw.WriteLine("")

Get-ChildItem -Path $rootFull -Recurse -Force |
    Where-Object { -not (Test-ExcludedPath $_.FullName) } |
    Sort-Object FullName |
    ForEach-Object {
        $relative = $_.FullName.Substring($rootFull.Length).TrimStart('\', '/')
        $depth = ($relative -split '[\\/]').Count - 1
        $indent = "  " * $depth
        $marker = if ($_.PSIsContainer) { "[DIR] " } else { "" }
        $sw.WriteLine("$indent$marker$($_.Name)")
    }

$sw.WriteLine("")
$sw.WriteLine("=== CONTENIDO DE ARCHIVOS ===")

Get-ChildItem -Path $rootFull -Recurse -Force -File |
    Where-Object {
        (-not (Test-ExcludedPath $_.FullName)) -and
        ($includeExtensions -contains $_.Extension.ToLower()) -and
        (-not (Test-ExcludedFile $_.Name))
    } |
    Sort-Object FullName |
    ForEach-Object {
        $relative = $_.FullName.Substring($rootFull.Length).TrimStart('\', '/')
        Write-Host "Incluyendo: $relative"
        $sw.WriteLine("")
        $sw.WriteLine("----- ARCHIVO: $relative -----")
        try {
            $content = Get-Content -Path $_.FullName -Raw -ErrorAction Stop
            $sw.WriteLine($content)
        }
        catch {
            $sw.WriteLine("[No se pudo leer el archivo: $($_.Exception.Message)]")
        }
    }

$sw.Close()
Write-Host ""
Write-Host "Listo. Archivo generado: $OutputFile"