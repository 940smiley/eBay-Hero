# Linux

Status: blocked.

The imported production UI is WPF targeting `net8.0-windows`, so it cannot ship as a Linux desktop app. The domain, persistence, file, OCR, export, and CLI projects target `net8.0` and are the reusable base for Linux.

Required next step: add an Avalonia or other Linux-compatible UI shell over the existing core services, then package as AppImage or `.deb`.

