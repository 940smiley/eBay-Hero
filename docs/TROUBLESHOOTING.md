# Troubleshooting

## Tesseract Not Found

Set the Tesseract path in configuration or install it at:

```text
E:\Apps\tesseract-ocr\tesseract.exe
```

## Database Issues

Run:

```powershell
dotnet run --project .\tools\InventoryPhotoOps.Cli -- database check
dotnet run --project .\tools\InventoryPhotoOps.Cli -- database backup
```

## Logs

Logs are written under:

```text
D:\INVENTORY_PHOTO_OPS\logs
```

