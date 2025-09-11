<#
Adds a real gRPC service class and wires it into Program.cs for a given microservice.

Creates:
  src/Services/<Service>/PA.<Service>.Api/Services/<Service>GrpcService.cs

Updates Program.cs to:
  - enable HTTP/2 on port 8080 (for gRPC in containers)
  - AddGrpc + AddGrpcReflection registrations
  - MapGrpcReflectionService()
  - MapGrpcService<<Service>GrpcService>()

Optionally adds API reference to PA.Contracts.Grpc.

Examples:
  .\scripts\add-grpc-service.ps1
  .\scripts\add-grpc-service.ps1 -ServiceName Orders -ProtoNamespace orders.v1
#>

param(
  [string]$ServiceName = "Catalog",
  [string]$ProtoNamespace = "",
  [string]$RepoRoot = ".",
  [bool]  $AddContractsRef = $true
)

$ErrorActionPreference = 'Stop'

function Resolve-Paths {
  param([string]$ServiceName, [string]$RepoRoot)
  $apiDir        = Join-Path $RepoRoot "src/Services/$ServiceName/PA.$ServiceName.Api"
  $servicesDir   = Join-Path $apiDir  "Services"
  $programPath   = Join-Path $apiDir  "Program.cs"
  $contractsProj = Join-Path $RepoRoot "src/PA.Contracts.Grpc/PA.Contracts.Grpc.csproj"
  $grpcClassName = "$ServiceName" + "GrpcService"
  $serviceFile   = Join-Path $servicesDir "$grpcClassName.cs"
  [pscustomobject]@{
    ApiDir        = $apiDir
    ServicesDir   = $servicesDir
    ProgramPath   = $programPath
    ContractsProj = $contractsProj
    GrpcClass     = $grpcClassName
    ServiceFile   = $serviceFile
  }
}

# File-lock safe writes
function Test-FileLocked {
  param([string]$Path)
  try {
    $fs = [System.IO.File]::Open($Path, [System.IO.FileMode]::OpenOrCreate, [System.IO.FileAccess]::ReadWrite, [System.IO.FileShare]::None)
    $fs.Close()
    return $false
  } catch { return $true }
}
function Write-TextFileWithRetry {
  param([string]$Path, [string]$Content, [int]$Retries = 20, [int]$DelayMs = 300)
  $dir = Split-Path -Parent $Path
  if (-not (Test-Path $dir)) { New-Item -ItemType Directory -Force -Path $dir | Out-Null }
  for ($i=0; $i -le $Retries; $i++) {
    try {
      if (Test-Path $Path) {
        try { Set-ItemProperty -Path $Path -Name IsReadOnly -Value $false -ErrorAction SilentlyContinue } catch {}
        if (Test-FileLocked $Path) { throw "Locked" }
      }
      $tmp = [System.IO.Path]::GetTempFileName()
      Set-Content -LiteralPath $tmp -Value $Content -Encoding UTF8
      Move-Item -LiteralPath $tmp -Destination $Path -Force
      return
    } catch {
      if ($i -eq $Retries) { throw }
      Start-Sleep -Milliseconds $DelayMs
    }
  }
}

# Small helpers for safe text insertion
function Insert-After {
  param([string]$Text, [string]$Marker, [string]$Insert)
  $pos = $Text.IndexOf($Marker)
  if ($pos -ge 0) {
    $idx = $pos + $Marker.Length
    return $Text.Insert($idx, [Environment]::NewLine + $Insert + [Environment]::NewLine)
  }
  return $Text
}

# Defaults
if ([string]::IsNullOrWhiteSpace($ProtoNamespace)) { $ProtoNamespace = ($ServiceName.ToLower() + ".v1") }

# Guess common proto service names from your .proto
switch ($ServiceName.ToLower()) {
  'catalog'        { $protoServiceClassName = 'CatalogService' }
  'orders'         { $protoServiceClassName = 'OrderService' }
  'payments'       { $protoServiceClassName = 'PaymentService' }
  'shipping'       { $protoServiceClassName = 'ShippingService' }
  'customers'      { $protoServiceClassName = 'CustomerService' }
  'inventory'      { $protoServiceClassName = 'InventoryService' }
  'manufacturing'  { $protoServiceClassName = 'ManufacturingService' }
  default          { $protoServiceClassName = "$ServiceName`Service" } # backtick escapes in PS
}

$paths = Resolve-Paths -ServiceName $ServiceName -RepoRoot $RepoRoot
if (-not (Test-Path $paths.ApiDir)) { throw "API project directory not found: $($paths.ApiDir)" }
New-Item -ItemType Directory -Path $paths.ServicesDir -Force | Out-Null

# ---- Create <Service>GrpcService.cs ----
$serviceNamespace = "PA.$ServiceName.Api.Services"
$protoBase = ('{0}.{1}.{1}Base' -f $ProtoNamespace, $protoServiceClassName)

# Concrete example for Catalog; generic stub for others
$catalogBody = @'
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using catalog.v1; // generated from PA.Contracts.Grpc
using PA.Catalog.Data;

namespace PA.Catalog.Api.Services {
    public class CatalogGrpcService : CatalogService.CatalogServiceBase
    {
        private readonly CatalogDbContext _db;
        public CatalogGrpcService(CatalogDbContext db) => _db = db;

        public override async Task<GetProductResponse> GetProductBySku(GetProductBySkuRequest request, ServerCallContext context)
        {
            var entity = await _db.Set<PA.Catalog.Domain.Models.Product>()
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Sku == request.Sku, context.CancellationToken);

            var resp = new GetProductResponse();
            if (entity != null) {
                resp.Product = new Product {
                    Id = entity.Id,
                    Sku = entity.Sku,
                    Name = entity.Name,
                    Description = entity.Description ?? "",
                    IsManufactured = entity.IsManufactured,
                    Price = (double)entity.PriceRetail,
                    UnitOfMeasure = entity.UnitOfMeasure,
                    Active = entity.Active,
                    Flavor = entity.Flavor ?? "",
                    SizeLb = (double)(entity.SizeLb ?? 0)
                };
            }
            return resp;
        }
    }
}
'@

$genericBody = @'
using Grpc.Core;

namespace {0} {
    /// <summary>
    /// Replace the base class with your generated proto service base:
    /// e.g., {1}
    /// </summary>
    public class {2} : {1}
    {
        // Inject your DbContext or services as needed
        public {2}() { }
        // TODO: implement RPCs defined in your proto
    }
}
'@

if ($ServiceName.ToLower() -eq 'catalog') {
  Write-TextFileWithRetry -Path $paths.ServiceFile -Content $catalogBody
} else {
  $body = $genericBody -f $serviceNamespace, $protoBase, $paths.GrpcClass
  Write-TextFileWithRetry -Path $paths.ServiceFile -Content $body
}
Write-Host ("`u2713 Created {0}" -f $paths.ServiceFile) -ForegroundColor Green

# ---- Optionally add project reference to PA.Contracts.Grpc ----
if ($AddContractsRef) {
  if (Test-Path $paths.ContractsProj) {
    $apiCsproj = Get-ChildItem -LiteralPath $paths.ApiDir -Filter '*.csproj' | Select-Object -First 1
    if ($apiCsproj) {
      Write-Host "Adding reference to PA.Contracts.Grpc (if not already present)..." -ForegroundColor DarkCyan
      & dotnet add $apiCsproj.FullName reference $paths.ContractsProj | Out-Null
    }
  }
}

# ---- Update Program.cs ----
if (-not (Test-Path $paths.ProgramPath)) { throw "Program.cs not found: $($paths.ProgramPath)" }
$prog = Get-Content -LiteralPath $paths.ProgramPath -Raw
$nl = [Environment]::NewLine
$builderMarker = 'var builder = WebApplication.CreateBuilder(args);'
$appBuildMarker = 'var app = builder.Build();'

# Ensure Kestrel using
if ($prog -notmatch 'using\s+Microsoft\.AspNetCore\.Server\.Kestrel\.Core;') {
  $prog = 'using Microsoft.AspNetCore.Server.Kestrel.Core;' + $nl + $prog
}

# Ensure AddGrpc + AddGrpcReflection after builder marker
if ($prog.Contains($builderMarker) -and $prog -notmatch 'Services\.AddGrpc\(') {
  $afterBuilder = @'
builder.Services.AddGrpc();
builder.Services.AddGrpcReflection();
'@.Trim()
  $prog = Insert-After -Text $prog -Marker $builderMarker -Insert $afterBuilder
}

# Ensure Kestrel HTTP/2 block after builder marker
if ($prog -notmatch 'ConfigureKestrel\(' -and $prog.Contains($builderMarker)) {
  $kestrelBlock = @'
builder.WebHost.ConfigureKestrel(o =>
{
    o.ListenAnyIP(8080, lo => lo.Protocols = HttpProtocols.Http2);
});
'@.Trim()
  $prog = Insert-After -Text $prog -Marker $builderMarker -Insert $kestrelBlock
}

# Map reflection + service after app.Build
if ($prog.Contains($appBuildMarker)) {
  $reflectionLine = 'app.MapGrpcReflectionService();'
  $mapLine = ('app.MapGrpcService<{0}.{1}>();' -f $serviceNamespace, $paths.GrpcClass)

  if ($prog -notmatch [regex]::Escape($reflectionLine)) {
    $prog = Insert-After -Text $prog -Marker $appBuildMarker -Insert $reflectionLine
  }
  if ($prog -notmatch [regex]::Escape($mapLine)) {
    $prog = Insert-After -Text $prog -Marker $appBuildMarker -Insert $mapLine
  }
} else {
  Write-Warning "Could not find 'var app = builder.Build();' in Program.cs; skipping MapGrpc* insertions."
}

# Backup and write Program.cs
Copy-Item -LiteralPath $paths.ProgramPath -Destination ($paths.ProgramPath + '.bak') -Force
Write-TextFileWithRetry -Path $paths.ProgramPath -Content $prog
Write-Host ("`u2713 Updated {0} (HTTP/2 + gRPC mappings)" -f $paths.ProgramPath) -ForegroundColor Green

Write-Host "`nDone. Restore/build the solution:" -ForegroundColor Cyan
Write-Host "  dotnet restore PA.MES.sln"
Write-Host "  dotnet build   PA.MES.sln"
