# UTOP Solution Scaffold — builds the structure locked in UTOP-solution-structure-v2.md
# Run from the repo root, on the branch you intend to scaffold implementation on.
# Requires: .NET 10 SDK, Node.js (for frontend), run in PowerShell.

$ErrorActionPreference = "Stop"

Write-Host "== UTOP scaffold starting ==" -ForegroundColor Cyan

# ---------- Solution ----------
dotnet new sln -n UTOP

New-Item -ItemType Directory -Force -Path "src", "tests" | Out-Null

# ---------- Shared Kernel ----------
$sharedPath = "src/UTOP.Shared"
dotnet new classlib -n UTOP.Shared -o $sharedPath
Remove-Item "$sharedPath/Class1.cs" -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path `
    "$sharedPath/Domain/Events", `
    "$sharedPath/Domain/Exceptions", `
    "$sharedPath/Domain/ValueObjects", `
    "$sharedPath/Time", `
    "$sharedPath/Infrastructure/Logging", `
    "$sharedPath/Infrastructure/Messaging", `
    "$sharedPath/Infrastructure/Security" | Out-Null
dotnet sln add "$sharedPath/UTOP.Shared.csproj"

# ---------- Full DDD contexts ----------
$fullContexts = @(
    "Booking", "Accommodation", "ResourceAllocation", "Pilgrimage",
    "GroupManagement", "CostSplitting", "Notifications", "KnowledgeBase",
    "AIRecommendation", "Identity", "Localization"
)

foreach ($ctx in $fullContexts) {
    $p = "src/UTOP.$ctx"
    dotnet new classlib -n "UTOP.$ctx" -o $p
    Remove-Item "$p/Class1.cs" -ErrorAction SilentlyContinue
    New-Item -ItemType Directory -Force -Path `
        "$p/Domain/Aggregates", "$p/Domain/Entities", "$p/Domain/ValueObjects", `
        "$p/Domain/Events", "$p/Domain/Services", "$p/Domain/Repositories", `
        "$p/Application/Commands", "$p/Application/Queries", "$p/Application/Handlers", `
        "$p/Application/Sagas", "$p/Application/Interfaces", `
        "$p/Infrastructure/Persistence/Migrations", "$p/Infrastructure/ExternalServices/Stubs", `
        "$p/Infrastructure/ExternalServices/Adapters", `
        "$p/API/Controllers", "$p/API/Mapping" | Out-Null
    dotnet sln add "$p/UTOP.$ctx.csproj"

    $testP = "tests/UTOP.$ctx.Tests"
    dotnet new xunit -n "UTOP.$ctx.Tests" -o $testP
    dotnet add "$testP/UTOP.$ctx.Tests.csproj" reference "$p/UTOP.$ctx.csproj"
    dotnet sln add "$testP/UTOP.$ctx.Tests.csproj"
}

# ---------- TravelCategory (lightweight) ----------
$tc = "src/UTOP.TravelCategory"
dotnet new classlib -n UTOP.TravelCategory -o $tc
Remove-Item "$tc/Class1.cs" -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path `
    "$tc/Domain/ValueObjects", "$tc/Domain/Services", `
    "$tc/Application/Queries", "$tc/Application/Handlers", `
    "$tc/Infrastructure/Persistence/Migrations", `
    "$tc/API/Controllers" | Out-Null
dotnet sln add "$tc/UTOP.TravelCategory.csproj"

$tcTest = "tests/UTOP.TravelCategory.Tests"
dotnet new xunit -n UTOP.TravelCategory.Tests -o $tcTest
dotnet add "$tcTest/UTOP.TravelCategory.Tests.csproj" reference "$tc/UTOP.TravelCategory.csproj"
dotnet sln add "$tcTest/UTOP.TravelCategory.Tests.csproj"

# ---------- Analytics (projection-host) ----------
$an = "src/UTOP.Analytics"
dotnet new classlib -n UTOP.Analytics -o $an
Remove-Item "$an/Class1.cs" -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path `
    "$an/Consumers", `
    "$an/Projections/ReadModels", "$an/Projections/ProjectionHandlers", "$an/Projections/Rebuild", `
    "$an/Infrastructure/Persistence", `
    "$an/API/Controllers" | Out-Null
dotnet sln add "$an/UTOP.Analytics.csproj"

$anTest = "tests/UTOP.Analytics.Tests"
dotnet new xunit -n UTOP.Analytics.Tests -o $anTest
dotnet add "$anTest/UTOP.Analytics.Tests.csproj" reference "$an/UTOP.Analytics.csproj"
dotnet sln add "$anTest/UTOP.Analytics.Tests.csproj"

# ---------- API host ----------
$api = "src/UTOP.API"
dotnet new webapi -n UTOP.API -o $api
New-Item -ItemType Directory -Force -Path "$api/Middleware" | Out-Null
dotnet sln add "$api/UTOP.API.csproj"

foreach ($ctx in $fullContexts + @("TravelCategory", "Analytics")) {
    dotnet add "$api/UTOP.API.csproj" reference "src/UTOP.$ctx/UTOP.$ctx.csproj"
}
dotnet add "$api/UTOP.API.csproj" reference "$sharedPath/UTOP.Shared.csproj"

foreach ($ctx in $fullContexts + @("TravelCategory", "Analytics")) {
    dotnet add "src/UTOP.$ctx/UTOP.$ctx.csproj" reference "$sharedPath/UTOP.Shared.csproj"
}

# ---------- Shared test project ----------
$sharedTest = "tests/UTOP.Shared.Tests"
dotnet new xunit -n UTOP.Shared.Tests -o $sharedTest
dotnet add "$sharedTest/UTOP.Shared.Tests.csproj" reference "$sharedPath/UTOP.Shared.csproj"
dotnet sln add "$sharedTest/UTOP.Shared.Tests.csproj"

# ---------- Integration test project ----------
$integTest = "tests/UTOP.Integration.Tests"
dotnet new xunit -n UTOP.Integration.Tests -o $integTest
dotnet add "$integTest/UTOP.Integration.Tests.csproj" reference "$api/UTOP.API.csproj"
dotnet sln add "$integTest/UTOP.Integration.Tests.csproj"

Write-Host "== Backend scaffold complete ==" -ForegroundColor Green

# ---------- Frontend ----------
Write-Host "== Scaffolding frontend (requires Node.js) ==" -ForegroundColor Cyan

npm create vite@latest frontend -- --template react-ts

$feFeatures = @(
    "booking", "accommodation", "resource-allocation", "pilgrimage",
    "group-management", "cost-splitting", "notifications", "knowledge-base",
    "analytics", "ai-recommendation", "identity", "localization", "travel-category"
)

New-Item -ItemType Directory -Force -Path `
    "frontend/src/app/layouts", "frontend/src/app/routes" | Out-Null

foreach ($f in $feFeatures) {
    New-Item -ItemType Directory -Force -Path `
        "frontend/src/features/$f/components", `
        "frontend/src/features/$f/pages", `
        "frontend/src/features/$f/services" | Out-Null
}

New-Item -ItemType Directory -Force -Path `
    "frontend/src/shared/components", "frontend/src/shared/hooks", `
    "frontend/src/shared/services", "frontend/src/shared/i18n", `
    "frontend/src/shared/auth", "frontend/src/shared/types" | Out-Null

Write-Host "== Frontend scaffold complete ==" -ForegroundColor Green
Write-Host "== UTOP scaffold finished. Open UTOP.sln in VS Code. ==" -ForegroundColor Cyan
