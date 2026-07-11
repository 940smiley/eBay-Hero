# Architecture

```mermaid
flowchart LR
  WPF["WPF App / MVVM"] --> Core["Core Models + Services"]
  WPF --> Infra["Infrastructure / EF Core SQLite"]
  WPF --> OCR["OCR Service"]
  WPF --> FS["File System Services"]
  WPF --> Export["eBay Export"]
  CLI["CLI + Migrator"] --> Infra
  CLI --> FS
  CLI --> OCR
  CLI --> Export
```

Boundaries:

- `Core`: domain models, enums, service contracts, pure domain helpers.
- `Infrastructure`: SQLite DbContext, EF migrations, JSON migration, database maintenance.
- `FileSystem`: scanning, hashing, file operation planning.
- `Ocr`: Tesseract invocation and adaptive learning.
- `Export`: copy-only eBay export.
- `App`: WPF views and MVVM composition.

