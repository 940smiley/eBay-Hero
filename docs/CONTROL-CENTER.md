# Control Center

Launch:

```powershell
.\eBay-Hero-ControlCenter.bat
```

The GUI reads `scripts\scripts.manifest.json`, groups actions by category, and runs selected actions without freezing the window. It shows the command, output, errors, exit code, logs, and failure-bundle path.

Primary actions include first run, doctor, restore, build, tests, JSON migration dry-run/apply, CardOps import dry-run/apply, demo/live launch, portable package, installer package, release verification, KnowledgeBase discovery, and storage audit.

The compatibility launcher remains available:

```powershell
.\eBayHero-ControlCenter.bat
```

