# Seed script for Sebastians Hub tenant and devices
# Run this after starting DeviceRegistry.Api (port 5101)

$baseUrl = "http://localhost:5101"
$tenantSlug = "sebastians-hub"
$tenantName = "Sebastians Hub"

Write-Host "🌱 Seeding Sebastians Hub data..." -ForegroundColor Green

# 1. Create tenant with specific ID to match InnoviaHubSeb
Write-Host "Creating tenant: $tenantName" -ForegroundColor Yellow
$expectedTenantId = "c5ba0b5e-04a2-402a-97dd-c61e7bb9adc0"

# Check if tenant already exists
try {
    $existingTenant = Invoke-RestMethod -Uri "$baseUrl/api/tenants/by-slug/$tenantSlug" -Method GET
    $tenantId = $existingTenant.id
    Write-Host "✅ Tenant already exists with ID: $tenantId" -ForegroundColor Green
} catch {
    # Create new tenant
    $tenantBody = @{
        name = $tenantName
        slug = $tenantSlug
    } | ConvertTo-Json

    try {
        $tenantResponse = Invoke-RestMethod -Uri "$baseUrl/api/tenants" -Method POST -Body $tenantBody -ContentType "application/json"
        $tenantId = $tenantResponse.id
        Write-Host "✅ Tenant created with ID: $tenantId" -ForegroundColor Green
        
        if ($tenantId -ne $expectedTenantId) {
            Write-Host "⚠️  WARNING: Tenant ID doesn't match InnoviaHubSeb config!" -ForegroundColor Yellow
            Write-Host "   Expected: $expectedTenantId" -ForegroundColor Yellow
            Write-Host "   Got:      $tenantId" -ForegroundColor Yellow
            Write-Host "   Update InnoviaHubSeb/Backend/appsettings.json with the new TenantId" -ForegroundColor Yellow
        }
    } catch {
        Write-Host "❌ Failed to create tenant: $($_.Exception.Message)" -ForegroundColor Red
        exit 1
    }
}

# 2. Create devices
$devices = @(
    @{ model = "Toshi-Maestro-Temp-333"; serial = "toshi001" },
    @{ model = "Toshi-Maestro-Temp-444"; serial = "toshi002" },
    @{ model = "Toshi-Maestro-Temp-555"; serial = "toshi003" },
    @{ model = "Toshi-Maestro-Temp-666"; serial = "toshi004" },
    @{ model = "Toshi-Maestro-Temp-777"; serial = "toshi005" },
    @{ model = "Toshi-Maestro-Temp-888"; serial = "toshi006" },
    @{ model = "Toshi-Maestro-Temp-999"; serial = "toshi007" },
    @{ model = "Toshi-Maestro-Temp-111"; serial = "toshi008" },
    @{ model = "Toshi-Maestro-Temp-222"; serial = "toshi009" },
    @{ model = "Toshi-Maestro-Temp-000"; serial = "toshi010" }
)

Write-Host "Creating $($devices.Count) devices..." -ForegroundColor Yellow

foreach ($device in $devices) {
    $deviceBody = @{
        model = $device.model
        serial = $device.serial
        status = "active"
    } | ConvertTo-Json

    try {
        $deviceResponse = Invoke-RestMethod -Uri "$baseUrl/api/tenants/$tenantId/devices" -Method POST -Body $deviceBody -ContentType "application/json"
        Write-Host "✅ Device created: $($device.serial) ($($device.model))" -ForegroundColor Green
    } catch {
        Write-Host "❌ Failed to create device $($device.serial): $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host "🎉 Seeding complete!" -ForegroundColor Green
Write-Host "Tenant ID: $tenantId" -ForegroundColor Cyan
Write-Host "Tenant Slug: $tenantSlug" -ForegroundColor Cyan
Write-Host "Devices: $($devices.Count) created" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "1. Update InnoviaHubSeb Backend/appsettings.json:" -ForegroundColor White
Write-Host "   Set InnoviaIot.TenantId = `"$tenantId`"" -ForegroundColor Gray
Write-Host "2. Start Edge.Simulator to begin publishing data" -ForegroundColor White
