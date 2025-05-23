# WaypointManager Script Documentation

## Overview
`WaypointManager` is a Unity MonoBehaviour for moving GameObjects along a series of waypoints. It supports looping, ping-pong, and stop-at-end movement modes, making it suitable for NPC patrols, moving platforms, or camera paths.

## Core Responsibilities
- Move a GameObject smoothly between a list of waypoints
- Support multiple movement modes (loop, ping-pong, stop at end)
- Handle rotation to face the next waypoint

## Main Features
- **Waypoint List:** Assign any number of waypoints (as `Transform`s) in the Inspector
- **Speed Controls:** Adjustable movement and rotation speed
- **Modes:**
  - **Loop:** Continuously cycles through waypoints
  - **PingPong:** Moves back and forth between waypoints
  - **StopAtEnd:** Stops at the last waypoint
- **Inspector Integration:** All settings are exposed for easy tuning

## How It Works
- On each `Update()`, the object moves toward the current waypoint and rotates to face it
- When close enough to a waypoint, `GetNextWaypoint()` determines the next target based on the selected mode
- Handles edge cases to avoid index out-of-bounds errors

## Example Usage
```csharp
// Attach to a GameObject (e.g., NPC, platform)
// Assign waypoints in the Inspector
// Set speed, rotationSpeed, and mode flags as needed
```

## Best Practices
- Place waypoints as empty GameObjects in the scene for clarity
- Avoid duplicate waypoints in the list
- Use only one mode (loop, pingPong, or stopAtEnd) at a time for predictable behavior
- Adjust speed and rotationSpeed for smooth movement

## Error Handling
- If no waypoints are assigned, the script does nothing (safe early return)
- Index bounds are checked in all movement modes
- Inspector-exposed fields make misconfiguration less likely

## Integration Points
- Can be triggered or paused by other scripts (e.g., for cutscenes)
- Works with any GameObject (NPCs, platforms, cameras, etc.)
- Combine with animation or event triggers at waypoints for advanced behaviors

## Example Inspector Setup
- Add `WaypointManager` to a GameObject
- Drag waypoint Transforms into the `waypoints` list
- Set `speed`, `rotationSpeed`, and select a movement mode

---
**See also:** Other animation scripts in this folder for advanced pathing or event-driven movement. 