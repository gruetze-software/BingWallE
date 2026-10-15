# BingWallE 1.2.2

- Prevent initialization of the ONNX Runtime telemetry uploader. A macOS crash report from 1.2.1 showed a native abort in its HTTP callback during process shutdown.
- Set the process-wide telemetry opt-out before loading ONNX Runtime, including the native Unix environment. Also disable telemetry events through the runtime API before creating an inference session.
- Local image recognition remains available. No model or keyword settings are changed.
- Add a cross-platform smoke check that verifies the native opt-out, performs real inference, disposes the session and exits normally in three separate processes.

The exact crash on macOS 27.0.1 could not be reproduced on the Windows development host; the fix avoids the telemetry initialization path identified in the report.
