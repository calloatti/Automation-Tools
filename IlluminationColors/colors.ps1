# === Timberborn Illumination Colors Script (FINAL FIXED VERSION) ===
# Generates blueprints (White + 24 new colors only) + colors.txt in perfect C# Color32 format

$colors = [ordered]@{
    "DefaultAmber"    = @(0.85, 0.49, 0.13)
    "BotBlue"         = @(0.16, 0.59, 0.80)
    "AutomatorBlue"   = @(0.52, 0.74, 0.80)
    "StandardGreen"   = @(0.12, 0.80, 0.12)
    "StandardOrange"  = @(0.80, 0.20, 0.00)
    "StandardRed"     = @(0.90, 0.00, 0.00)
    "Dimmed"          = @(0.07, 0.09, 0.10)
    "White"           = @(1.00, 1.00, 1.00)

    "NeonRose"        = @(1.00, 0.08, 0.53)
    "HotPink"         = @(1.00, 0.04, 0.73)
    "Fuchsia"         = @(1.00, 0.00, 1.00)
    "ElectricPurple"  = @(0.82, 0.00, 1.00)
    "Violet"          = @(0.59, 0.00, 1.00)
    "Indigo"          = @(0.31, 0.00, 1.00)
    "RoyalBlue"       = @(0.10, 0.25, 1.00)
    "ElectricBlue"    = @(0.00, 0.49, 1.00)
    "Azure"           = @(0.00, 0.71, 1.00)
    "Cyan"            = @(0.00, 1.00, 1.00)
    "Turquoise"       = @(0.00, 1.00, 0.75)
    "Aqua"            = @(0.10, 1.00, 0.57)
    "MintGreen"       = @(0.25, 1.00, 0.41)
    "SpringGreen"     = @(0.02, 1.00, 0.33)
    "VibrantLime"     = @(0.45, 1.00, 0.00)
    "Chartreuse"      = @(0.67, 1.00, 0.00)
    "LemonYellow"     = @(1.00, 1.00, 0.12)
    "GoldenYellow"    = @(1.00, 0.82, 0.00)
    "Coral"           = @(1.00, 0.55, 0.14)
    "SalmonPink"      = @(1.00, 0.41, 0.31)
    "Rose"            = @(1.00, 0.27, 0.49)
    "Lavender"        = @(0.92, 0.45, 1.00)
    "Magenta"         = @(1.00, 0.18, 0.80)
    "Raspberry"       = @(1.00, 0.14, 0.39)
}

$displayNames = [ordered]@{
    "DefaultAmber"    = "Default (Amber)"
    "BotBlue"         = "Bot Blue"
    "AutomatorBlue"   = "Automator Blue"
    "StandardGreen"   = "Standard Green"
    "StandardOrange"  = "Standard Orange"
    "StandardRed"     = "Standard Red"
    "Dimmed"          = "Dimmed / Off"
    "White"           = "Pure White"
    "NeonRose"        = "Neon Rose"
    "HotPink"         = "Hot Pink"
    "Fuchsia"         = "Fuchsia"
    "ElectricPurple"  = "Electric Purple"
    "Violet"          = "Violet"
    "Indigo"          = "Indigo"
    "RoyalBlue"       = "Royal Blue"
    "ElectricBlue"    = "Electric Blue"
    "Azure"           = "Azure"
    "Cyan"            = "Cyan"
    "Turquoise"       = "Turquoise"
    "Aqua"            = "Aqua"
    "MintGreen"       = "Mint Green"
    "SpringGreen"     = "Spring Green"
    "VibrantLime"     = "Vibrant Lime"
    "Chartreuse"      = "Chartreuse"
    "LemonYellow"     = "Lemon Yellow"
    "GoldenYellow"    = "Golden Yellow"
    "Coral"           = "Coral"
    "SalmonPink"      = "Salmon Pink"
    "Rose"            = "Rose"
    "Lavender"        = "Lavender"
    "Magenta"         = "Magenta"
    "Raspberry"       = "Raspberry"
}

function Build-Blueprint($id, $r, $g, $b, $order) {
    $json = @{
        "IlluminationColorSpec" = @{
            "Id" = "Calloatti.$id"
            "Color" = @{ 
                "r" = [math]::Round($r, 4)
                "g" = [math]::Round($g, 4)
                "b" = [math]::Round($b, 4)
                "a" = 1.0 
            }
        }
        "IlluminationPresetSpec" = @{
            "Order" = $order
        }
    }
    return $json | ConvertTo-Json -Depth 10
}

# Cleanup old blueprints
Get-ChildItem "Calloatti.IlluminationColor.*.blueprint.json" -ErrorAction SilentlyContinue | Remove-Item

# Generate blueprints (White + 24 new colors only)
Write-Host "Generating blueprints..." -ForegroundColor Cyan
$order = 10000
$keysToProcess = $colors.Keys | Select-Object -Skip 7

foreach ($name in $keysToProcess) {
    $rgb = $colors[$name]
    $filename = "Calloatti.IlluminationColor.$name.blueprint.json"
    (Build-Blueprint $name $rgb[0] $rgb[1] $rgb[2] $order) | Out-File $filename -Encoding utf8
    $order += 10
}

# Build colors.txt in EXACT format you wanted
Write-Host "Building colors.txt..." -ForegroundColor Cyan

$txtContent = @()
foreach ($key in $colors.Keys) {
    $rgb     = $colors[$key]
    $display = $displayNames[$key]
    $r = [math]::Round($rgb[0] * 255)
    $g = [math]::Round($rgb[1] * 255)
    $b = [math]::Round($rgb[2] * 255)
    $line = '    {{ new Color32({0}, {1}, {2}, 255), "{3}" }},' -f $r, $g, $b, $display
    $txtContent += $line
}

# Print the full list (so you can copy even if anything goes wrong)
Write-Host "`n=== FULL colors.txt CONTENT (ready to paste) ===" -ForegroundColor Yellow
$txtContent | ForEach-Object { Write-Host $_ }

# Write the file
$txtContent | Set-Content "colors.txt" -Encoding utf8 -Force
Write-Host "`n✅ colors.txt created successfully!" -ForegroundColor Green
Write-Host "✅ All 32 colors are now in your file exactly as you wanted!" -ForegroundColor Green

Write-Host "`nAll done! Open colors.txt and copy straight into your mod code 🦫💡" -ForegroundColor Magenta