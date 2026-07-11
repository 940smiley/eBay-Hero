# Image Editor

The Image Editor tab performs nondestructive edits against working copies.

Implemented operations:

- Rotate right
- Crop center
- Rerun OCR against the selected photo
- Persist edit sessions and operation history in SQLite

The service supports command-based crop, rotate left/right, arbitrary rotation, straighten, reset, and save-derived-copy. Original overwrite is intentionally blocked by the service unless a future explicit overwrite workflow is added.
