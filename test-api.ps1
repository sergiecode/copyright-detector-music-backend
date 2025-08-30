# Simple API Test Script for Copyright Detector Music Backend
Write-Host "🧪 Testing Copyright Detector Music Backend API" -ForegroundColor Green

# Test Health Endpoint
Write-Host "`n📍 Testing Health Endpoint..." -ForegroundColor Yellow
try {
    $healthResponse = Invoke-WebRequest -Uri "http://localhost:5070/health" -Method GET -UseBasicParsing
    Write-Host "✅ Health Endpoint Status: $($healthResponse.StatusCode)" -ForegroundColor Green
    Write-Host "📋 Health Response: $($healthResponse.Content)" -ForegroundColor Cyan
} catch {
    Write-Host "❌ Health Endpoint Failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Test Status Endpoint
Write-Host "`n📊 Testing Status Endpoint..." -ForegroundColor Yellow
try {
    $statusResponse = Invoke-WebRequest -Uri "http://localhost:5070/api/search/status" -Method GET -UseBasicParsing
    Write-Host "✅ Status Endpoint Status: $($statusResponse.StatusCode)" -ForegroundColor Green
    Write-Host "📋 Status Response: $($statusResponse.Content)" -ForegroundColor Cyan
} catch {
    Write-Host "❌ Status Endpoint Failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Test Swagger/OpenAPI
Write-Host "`n📖 Testing Swagger UI..." -ForegroundColor Yellow
try {
    $swaggerResponse = Invoke-WebRequest -Uri "http://localhost:5070/" -Method GET -UseBasicParsing
    if ($swaggerResponse.StatusCode -eq 200) {
        Write-Host "✅ Swagger UI is accessible" -ForegroundColor Green
    }
} catch {
    Write-Host "❌ Swagger UI Failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n🎉 API Testing Complete!" -ForegroundColor Green
Write-Host "🔗 Visit http://localhost:5070/ for Swagger UI" -ForegroundColor Cyan
