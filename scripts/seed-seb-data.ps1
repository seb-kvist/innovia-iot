# Seed script for Sebastians Hub tenant and devices
# Run this after starting DeviceRegistry.Api (port 5101)

$baseUrl = "http://localhost:5101"
$tenantSlug = "sebastians-hub"
$tenantName = "Sebastians Hub"

Write-Host "Seeding Sebastians Hub data..."

# 1. Create tenant with specific ID to match InnoviaHubSeb
Write-Host "Creating tenant: $tenantName"
$expectedTenantId = "c5ba0b5e-04a2-402a-97dd-c61e7bb9adc0"

# Check if tenant already exists
try {
    $existingTenant = Invoke-RestMethod -Uri "$baseUrl/api/tenants/by-slug/$tenantSlug" -Method GET
    $tenantId = $existingTenant.id
    Write-Host "✅ Tenant already exists with ID: $tenantId"
} catch {
    # Create new tenant
    $tenantBody = @{
        name = $tenantName
        slug = $tenantSlug
    } | ConvertTo-Json

    try {
        $tenantResponse = Invoke-RestMethod -Uri "$baseUrl/api/tenants" -Method POST -Body $tenantBody -ContentType "application/json"
        $tenantId = $tenantResponse.id
        Write-Host "✅ Tenant created with ID: $tenantId"
        
        if ($tenantId -ne $expectedTenantId) {
            Write-Host "⚠️  WARNING: Tenant ID doesn't match InnoviaHubSeb config!"
            Write-Host "   Expected: $expectedTenantId"
            Write-Host "   Got:      $tenantId"
            Write-Host "   Update InnoviaHubSeb/Backend/appsettings.json with the new TenantId"
        }
    } catch {
        Write-Host "❌ Failed to create tenant: $($_.Exception.Message)"
        exit 1
    }
}

# 2. Create devices
$devices = @(
    @{ model = "Toshi-Maestro-Temp-333";    serial = "toshi001"; status = "active" },
    @{ model = "Toshi-Maestro-Temp-666";    serial = "toshi002"; status = "active" },
    @{ model = "Toshi-Maestro-Temp-999";    serial = "toshi003"; status = "active" },
    @{ model = "Toshi-Maestro-CO2-33";      serial = "toshi004"; status = "active" },
    @{ model = "Toshi-Maestro-CO2-66";      serial = "toshi005"; status = "active" },
    @{ model = "Toshi-Maestro-CO2-99";      serial = "toshi006"; status = "active" },
    @{ model = "Toshi-Maestro-Humidity-3";  serial = "toshi007"; status = "active" },
    @{ model = "Toshi-Maestro-Humidity-6";  serial = "toshi008"; status = "active" },
    @{ model = "Toshi-Maestro-Humidity-9";  serial = "toshi009"; status = "active" },
    @{ model = "Ihsot-Maestro-Motion-1337"; serial = "toshi010"; status = "active" }
)

Write-Host "Creating $($devices.Count) devices..."

foreach ($device in $devices) {
    $deviceBody = @{
        model = $device.model
        serial = $device.serial
        status = ($device.status ?? "active")
    } | ConvertTo-Json

    try {
        $deviceResponse = Invoke-RestMethod -Uri "$baseUrl/api/tenants/$tenantId/devices" -Method POST -Body $deviceBody -ContentType "application/json"
        Write-Host "✅ Device created: $($device.serial) ($($device.model))"
    } catch {
        Write-Host "❌ Failed to create device $($device.serial): $($_.Exception.Message)"
    }
}

Write-Host "Seeding complete!"
Write-Host "Tenant ID: $tenantId"
Write-Host "Tenant Slug: $tenantSlug"
Write-Host "Devices: $($devices.Count) created"
Write-Host ""
Write-Host "Next steps:"
Write-Host "1. Update InnoviaHubSeb Backend/appsettings.json:"
Write-Host "   Set InnoviaIot.TenantId = `"$tenantId`""
Write-Host "2. Start Edge.Simulator to begin publishing data"
