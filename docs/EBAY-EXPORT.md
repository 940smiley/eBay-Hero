# eBay Export

Exports write to:

```text
D:\INVENTORY_PHOTO_OPS\ebay-temp\yyyyMMdd_HHmmss
```

Generated files:

- `ebay_export_manifest.csv`
- `ebay_export_manifest.json`
- `README.txt`

Images are copied and ordered by item, then front/back/detail view priority.

Live eBay API integration is intentionally not implemented yet. The export service is isolated behind `IEbayExportService` so API support can be added later.

