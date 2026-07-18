# Development

Requirements:

- Windows 10 or later
- .NET 8 runtime
- .NET SDK 8 or newer
- Tesseract for OCR integration testing

Commands:

```powershell
dotnet restore eBayHero.sln
dotnet build eBayHero.sln -c Release
dotnet test eBayHero.sln -c Release
dotnet tool restore
```

EF migration generation:

```powershell
dotnet tool run dotnet-ef migrations add <Name> --project .\src\eBayHero.Infrastructure --startup-project .\tools\eBayHero.Cli --output-dir Data\Migrations
```


