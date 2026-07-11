# Development

Requirements:

- Windows 10 or later
- .NET 8 runtime
- .NET SDK 8 or newer
- Tesseract for OCR integration testing

Commands:

```powershell
dotnet restore InventoryPhotoOps.sln
dotnet build InventoryPhotoOps.sln -c Release
dotnet test InventoryPhotoOps.sln -c Release
dotnet tool restore
```

EF migration generation:

```powershell
dotnet tool run dotnet-ef migrations add <Name> --project .\src\InventoryPhotoOps.Infrastructure --startup-project .\tools\InventoryPhotoOps.Cli --output-dir Data\Migrations
```

