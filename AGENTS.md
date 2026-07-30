Include ..\AGENTS.md

# Automation Tools — Mod-Specific Agent Instructions

## Identity
- **Assembly:** `autotools`
- **Namespace:** `Calloatti.AutoTools`
- **Framework:** Harmony, Bindito DI
- **ModId:** `Calloatti.AutoTools`
- **Min Game Version:** 1.0.12.5 — uses `timberborn-decompiled-1.0.*`

## What This Mod Does
Adds automation batch control panel, auto-mapping tools, and move-connections tools for automation networks.

## Source Architecture (`Version-1.0/Source/`)

| File | Role |
|---|---|
| `ModStarter.cs` | Entry point — `IModStarter` |
| `ModConfigurator.cs` | DI configurator — registers services |
| `AutomationBatchControlConfigurator.cs` | Batch control panel configurator |
| `AutomationBatchControlTab.cs` | UI tab for batch operations |
| `MoveConnectionsConfigurator.cs` | Move connections tool configurator |
| `MoveConnectionsFragment.cs` | UI fragment for move connections |
| `MoveConnectionsTool.cs` | Tool logic for moving automation connections |
| `AutoMapService.cs` | Core auto-mapping service |
| `AutoMapService.Events.cs` | Event handlers for auto-map |
| `AutoMapService.Input.cs` | Input handling for auto-map |
| `AutoMapService.Visuals.cs` | Visual feedback for auto-map |
| `AutoMapHoverService.cs` | Hover detection for auto-map |
| `AutoMapInputService.cs` | Input processor for auto-map |
| `AutoMapStateListener.cs` | State change listener |
