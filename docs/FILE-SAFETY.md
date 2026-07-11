# File Safety

Safety rules:

- Development defaults refuse live writes without explicit live access.
- Desktop development launches use `%LOCALAPPDATA%\InventoryPhotoOps-dev`; pass `--allow-live` or set `IPO_ALLOW_LIVE=1` to use the configured live root intentionally.
- eBay export copies files only.
- Import planning detects duplicate hashes and destination collisions.
- No file is silently overwritten.
- Tests use temporary directories.
- Clean scripts refuse paths outside the repository.

Next-phase work: full rollback execution for multi-file moves/renames, Recycle Bin delete integration, and in-app undo history.
