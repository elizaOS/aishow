# Scene Transition UI Setup Guide

## Overview
This guide explains how to set up the scene transition UI that creates a smooth wipe effect between scenes.

## Required Components
1. Canvas (Screen Space - Overlay)
2. RawImage with Mask
3. Animator Controller
4. SceneTransitionManager script

## Setup Steps

### 1. Create the Canvas
1. Create a new Canvas (Right-click in Hierarchy > UI > Canvas)
2. Set Render Mode to "Screen Space - Overlay"
3. Set Sort Order to a high number (e.g., 100) to ensure it's always on top

### 2. Create the Transition Panel
1. Create a new Panel under the Canvas (Right-click Canvas > UI > Panel)
2. Add a RawImage component to the Panel
3. Add a Mask component to the Panel
4. Set the Panel's RectTransform to fill the entire screen
5. Set the RawImage's color to your desired transition color (e.g., black)

### 3. Create the Animator Controller
1. Create a new Animator Controller (Right-click in Project > Create > Animator Controller)
2. Create two states: "Idle" and "Transition"
3. Add a bool parameter called "Transition"
4. Create transitions between states:
   - Idle -> Transition: When "Transition" is true
   - Transition -> Idle: When "Transition" is false
5. In the Transition state, animate the mask's position/scale to create the wipe effect
   - Start with the mask covering the entire screen
   - Animate to reveal the screen (or vice versa, depending on your desired effect)

### 4. Set Up the SceneTransitionManager
1. Create an empty GameObject in your scene
2. Add the SceneTransitionManager script to it
3. Assign the references in the Inspector:
   - Transition Panel: Drag the Panel with the RawImage
   - Transition Animator: Drag the Animator component from the Panel
   - Transition Trigger Param: Set to "Transition" (or your custom parameter name)
4. Adjust the timing settings:
   - Start Transition At Audio Progress: 0.8 (80% through the last line)
   - Transition Duration: 1.0 (or your desired duration)

### 5. Connect to ShowRunner
1. Find your ShowRunner GameObject
2. Drag the SceneTransitionManager GameObject to the "Scene Transition Manager" field

## Animation Tips
- Use a simple mask shape (rectangle or circle) for the wipe effect
- Consider using easing functions for smoother transitions
- Test the timing with different audio lengths to ensure smooth transitions
- The transition should complete before the next scene starts loading

## Troubleshooting
- If the transition appears behind other UI elements, increase the Canvas's Sort Order
- If the transition is too fast/slow, adjust the Transition Duration
- If the transition starts too early/late, adjust the Start Transition At Audio Progress
- Ensure the Mask component is properly configured to show/hide the RawImage 