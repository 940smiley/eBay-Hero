# Agent Notes

- Treat this repository as the canonical product repository.
- Treat `D:\WORK` source repositories as read-only reference sources.
- Do not import secrets, runtime databases, logs, generated thumbnails, or local config.
- Use `EA_BUILD_ROOT` and `NUGET_PACKAGES` when building on machines with limited `C:` space.
- Run `scripts\Discover-SourceProjects.ps1` before any future source import decisions.
- Keep live eBay publication disabled unless the user explicitly authorizes a specific action.

