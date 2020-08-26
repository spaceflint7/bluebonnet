
README for the Demo folder.

This is simple demo showing C# / F# code running in a Java environment.

The demo is split conceptually into demo logic, and platform wrapper.
The demo logic are in Points_CS and Points_FS, for a C# and F# examples,
respectively.

Platform-specific graphics APIs are not a part of the Bluebonnet Baselib,
so the demo defines a simple Hardware Abstraction Layer (HAL) interface
(in the HAL.cs file), and each platform-specific wrapper implements it.

The native .Net application uses Windows Forms; see WinForm.cs.
The desktop Java wrapper uses Swing; see JavaForm.cs.
The Android wrapper uses canvas API; see the Android project.

The project folders are a combination of each demo logic and platform:

WinForm_CS, WinForm_FS, JavaForm_CS, JavaForm_FS, Android_CS, Android_FS
