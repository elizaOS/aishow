# ShowRunner Core Component

## Overview
The ShowRunner is the central controller class that orchestrates the entire show experience in the AISHOW system. It manages show playback, scene preparation, event processing, and coordinates between various subsystems.

## Key Responsibilities
- Loading and managing show data from JSON files
- Controlling episode playback and scene transitions
- Processing events and managing show state
- Coordinating audio playback and actor interactions
- Managing manual/auto playback modes
- Handling scene preparation and transitions

## Configuration
```csharp
[Header("Configuration")]
[SerializeField] private string episodesRootPath = "Episodes";
[SerializeField] private float dialogueDelay = 0.5f;
[SerializeField] private bool playAudioFromActors = true;
```

## Dependencies
- EventProcessor: Handles event processing
- ScenePreparationManager: Manages scene loading and preparation
- SceneTransitionManager: Handles scene transitions
- AudioSource: Default audio source for playback
- ShowRunnerUI: User interface integration

## Core Methods

### Show Loading and Management
- `DiscoverShowFiles()`: Finds all .json show files in Resources/Episodes
- `LoadShowData(string showFileNameToLoad)`: Loads show data from JSON
- `ProcessLoadedJson(string jsonContent, string sourceFileName)`: Processes loaded JSON data
- `ResetShowState()`: Resets the show to initial state

### Episode Control
- `SelectEpisode(int index)`: Selects and prepares an episode
- `NextStep()`: Advances to the next step in the show
- `GetCurrentEpisodeTitle()`: Returns current episode title
- `GetTotalEpisodeCount()`: Returns total number of episodes
- `GetEpisodeTitles()`: Returns list of episode titles

### Scene Management
- `PrepareScene(string sceneName)`: Prepares a scene for playback
- `OnScenePreparationComplete(string sceneName)`: Handles scene preparation completion
- `GetCurrentSceneLocation()`: Returns current scene location

### Audio Management
- `PreloadAudio()`: Preloads audio files
- `GetAudioSourceForActor(string actorName)`: Gets audio source for actor
- `PlayDialogueAudio(EventData speakEvent)`: Plays dialogue audio

### Playback Control
- `SetManualMode(bool manual)`: Sets manual/auto playback mode
- `PauseShow()`: Pauses show playback
- `ResumeShow()`: Resumes show playback

## Events
- `OnLastDialogueComplete`: Fired when episode completes
- `OnEpisodeSelectedForDisplay`: Fired when episode is selected
- `OnSceneChangedForDisplay`: Fired when scene changes

## State Management
- Tracks current episode, scene, and dialogue indices
- Manages playback state and scene preparation status
- Handles internal pause state for commercial breaks

## Best Practices
1. Always initialize required components in Awake()
2. Use manual mode for testing and debugging
3. Handle scene preparation asynchronously
4. Cache audio sources for better performance
5. Validate show data before playback

## Error Handling
- Validates required components on startup
- Handles missing scene preparation manager
- Manages audio source assignment fallbacks
- Provides graceful degradation for missing components

## Integration Points
- CommercialManager: For commercial break integration
- UXAnimationManager: For episode end animations
- ShowRunnerUI: For user interface control
- EventProcessor: For event handling
- ScenePreparationManager: For scene management 