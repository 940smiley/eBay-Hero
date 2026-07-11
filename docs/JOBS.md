# Jobs

Jobs are persisted in SQLite with kind, status, progress, current file, payload, and error details. Job attempts record retries and results.

The Jobs tab displays recent jobs. Long-running orchestration is handled by scripts and the Control Center; in-app background retry scheduling is the next area to deepen.
