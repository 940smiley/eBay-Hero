# Live Mode

Live paths are `D:\INVENTORY_PHOTO_OPS` and `F:\Inventory`.

Live mode is disabled by default. Use `--allow-live` for app/CLI commands or `.\scripts\launch.ps1 -Live`. Live migration requires `.\scripts\migrate.ps1 -Apply -AllowLive`.

Live actions should be preceded by a dry run and database backup. Publishing to eBay remains disabled until credentials, OAuth consent, capability checks, listing validation, and final confirmation are complete.
