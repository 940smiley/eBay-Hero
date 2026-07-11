# Dependency Paths

Path configuration lives in `config\paths.json`.

Defaults:

- Repository: `D:\WORK\Projects\ACTIVE\InventoryPhotoOps`
- Operations root: `D:\INVENTORY_PHOTO_OPS`
- Inventory source: `F:\Inventory`
- Shared tools: `E:\DevTools`
- Shared apps: `E:\Apps`
- Shared caches: `E:\DevCaches`
- Project tools: `E:\InventoryPhotoOps-Tools`

Use `scripts\configure-paths.ps1 -Apply` for directories. User-level environment variables require `-SetUserEnvironment`; machine-wide variables are not changed.
