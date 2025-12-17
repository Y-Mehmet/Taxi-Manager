# Level Rename Script
# Bu script "a" suffix'li levelleri düzenli numaralara dönüştürür
# Örnek: Level_4, Level_4a, Level_5 → Level_4, Level_5, Level_6

$levelsPath = "d:\Github\Gemini_CLI_test\Spyke_Case\Assets\Resources\Levels"

# Tüm .asset dosyalarını al (meta dosyaları hariç)
$files = Get-ChildItem -Path $levelsPath -Filter "Level_*.asset" | Where-Object { $_.Name -notmatch "\.meta$" }

# Level numaralarını ve suffix'leri parse et
$levelData = @()
foreach ($file in $files) {
    if ($file.Name -match "^Level_(\d+)(a?)\.asset$") {
        $num = [int]$Matches[1]
        $suffix = $Matches[2]
        $levelData += [PSCustomObject]@{
            OriginalName = $file.Name
            Number = $num
            Suffix = $suffix
            SortKey = if ($suffix -eq "a") { $num + 0.5 } else { $num }
            FullPath = $file.FullName
        }
    }
}

# SortKey'e göre sırala
$sortedLevels = $levelData | Sort-Object SortKey

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "LEVEL YENIDEN ADLANDIRMA ONCAN IZLEMESİ" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Yeni numaraları hesapla
$newNumber = 1
$renameOperations = @()

foreach ($level in $sortedLevels) {
    $newName = "Level_$newNumber.asset"
    if ($level.OriginalName -ne $newName) {
        Write-Host "$($level.OriginalName) -> $newName" -ForegroundColor Yellow
        $renameOperations += [PSCustomObject]@{
            OldName = $level.OriginalName
            NewName = $newName
            OldPath = $level.FullPath
            NewPath = Join-Path $levelsPath $newName
            OldMetaPath = "$($level.FullPath).meta"
            NewMetaPath = (Join-Path $levelsPath $newName) + ".meta"
        }
    } else {
        Write-Host "$($level.OriginalName) -> $newName (değişiklik yok)" -ForegroundColor Green
    }
    $newNumber++
}

Write-Host ""
Write-Host "Toplam dosya sayısı: $($sortedLevels.Count)"
Write-Host "Değiştirilecek dosya sayısı: $($renameOperations.Count)"
Write-Host ""

# Onay iste
$confirm = Read-Host "Yeniden adlandırma işlemini başlatmak için 'EVET' yazın"

if ($confirm -ne "EVET") {
    Write-Host "İşlem iptal edildi." -ForegroundColor Red
    exit
}

# Önce tüm dosyaları geçici isimlere taşı (çakışmayı önlemek için)
Write-Host ""
Write-Host "Adım 1: Geçici isimlere taşınıyor..." -ForegroundColor Cyan

foreach ($op in $renameOperations) {
    $tempName = $op.OldPath + ".temp"
    $tempMetaName = $op.OldMetaPath + ".temp"
    
    if (Test-Path $op.OldPath) {
        Rename-Item -Path $op.OldPath -NewName ($op.OldName + ".temp") -Force
    }
    if (Test-Path $op.OldMetaPath) {
        Rename-Item -Path $op.OldMetaPath -NewName ($op.OldName + ".meta.temp") -Force
    }
}

Write-Host "Adım 2: Final isimlere taşınıyor..." -ForegroundColor Cyan

foreach ($op in $renameOperations) {
    $tempPath = $op.OldPath + ".temp"
    $tempMetaPath = $op.OldMetaPath + ".temp"
    
    if (Test-Path $tempPath) {
        Rename-Item -Path $tempPath -NewName $op.NewName -Force
        Write-Host "  $($op.OldName) -> $($op.NewName)" -ForegroundColor Green
    }
    if (Test-Path $tempMetaPath) {
        Rename-Item -Path $tempMetaPath -NewName ($op.NewName + ".meta") -Force
    }
}

Write-Host ""
Write-Host "==========================================" -ForegroundColor Green
Write-Host "İŞLEM TAMAMLANDI!" -ForegroundColor Green
Write-Host "==========================================" -ForegroundColor Green
Write-Host ""
Write-Host "ÖNEMLİ: Unity'yi açtığınızda Asset Database'i yeniden import etmeniz gerekebilir."
Write-Host "Unity'de: Assets -> Reimport All"
