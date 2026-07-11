# KnowledgeBase Discovery

`D:\KnowledgeBase` is treated as read-only unless a specific write is authorized by the user.

Use:

```powershell
.\scripts\discover-knowledgebase.ps1 -DryRun
```

The discovery script reports file metadata and likely-relevant filenames only. It does not execute unknown scripts, print full secrets, commit credentials, or send secret values to any external service.

To import reviewed secret values from a local JSON file:

```powershell
.\scripts\import-secrets.ps1 -SourceJson .\path\to\reviewed-secrets.json
.\scripts\import-secrets.ps1 -SourceJson .\path\to\reviewed-secrets.json -Apply
```

The import script defaults to dry run, shows names but not values, backs up existing local secret configuration, preserves unrelated settings, and adds local secret paths to `.gitignore`.
