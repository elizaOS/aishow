# Hedra Manifest Explorer

A simple, intuitive HTML static explorer for Hedra processing manifests that can browse season folders and display episode content.

## Features

- 📁 **Folder Browsing**: Select entire season folders (like S1E56) to automatically load all assets
- 📄 **Manifest Loading**: Automatically detects and loads `hedra_manifest.json` files
- 🖼️ **Media Preview**: Displays screenshots and plays audio files directly in the browser
- 🎭 **Segment Management**: Visual cards for each segment with detailed information
- ⚙️ **Processing Interface**: Template buttons for future integration with HedraEpisodeProcessor
- 📝 **Logging System**: Real-time logs for tracking operations

## File Structure

```
Assets/Templates/HedraManifestExplorer/
├── index.html          # Main HTML interface
├── styles.css          # Modern, responsive CSS styling
├── script.js           # JavaScript functionality
└── README.md           # This documentation
```

## How to Use

### 1. Basic Setup
1. Copy the entire `HedraManifestExplorer` folder to your desired location
2. Open `index.html` in any modern web browser
3. No server required - works as a static HTML file

### 2. Loading Episode Data
1. Click **"📁 Browse Season Folder"**
2. Navigate to and select an episode folder (e.g., `Assets/Resources/Episodes/S1E56/`)
3. The explorer will automatically:
   - Load all files in the folder
   - Search for `hedra_manifest.json` in the `hedra_processing` subfolder
   - Display manifest information and segments

### 3. Exploring Segments
- Each segment appears as a visual card showing:
  - Segment number and status
  - Dialogue text (truncated)
  - Screenshot preview (if image file found)
  - Audio player (if audio file found)
  - Technical details (aspect ratio, resolution, etc.)
- Click any segment card to select it for processing

### 4. Processing Controls (Template)
- **🚀 Process All Segments**: Placeholder for batch processing
- **🎯 Process Selected Segment**: Placeholder for single segment processing
- **⏹️ Stop Processing**: Placeholder for stopping operations

## Expected Folder Structure

The explorer expects the following folder structure (matching your current setup):

```
Assets/Resources/Episodes/S1E56/
├── hedra_processing/
│   └── hedra_manifest.json     # Main manifest file
├── screenshots/
│   ├── S1E56_1_1.jpg
│   ├── S1E56_1_2.jpg
│   └── ...
├── audio/
│   ├── S1E56_1_1.mp3
│   ├── S1E56_1_2.mp3
│   └── ...
└── AI_Podcast_S1E56.json       # Source episode data
```

## Integration with HedraEpisodeProcessor

This template is designed to eventually integrate with the Unity `HedraEpisodeProcessor.cs`. The JavaScript provides several utility functions:

### Available Functions
```javascript
// Get current manifest data
HedraExplorer.getCurrentManifest()

// Get selected segment
HedraExplorer.getSelectedSegment()

// Update segment status
HedraExplorer.updateSegmentStatus(index, 'processing')

// Add log entries
HedraExplorer.addLog('info', 'Processing started...')

// Update processing status
HedraExplorer.updateProcessingStatus('Ready to process')
```

### Status Types
- `ready` - Segment is ready for processing
- `processing` - Currently being processed
- `complete` - Processing completed successfully
- `error` - Processing failed

## Customization

### Styling
Edit `styles.css` to customize:
- Color schemes and gradients
- Card layouts and spacing
- Button styles and animations
- Responsive breakpoints

### Functionality
Edit `script.js` to:
- Add new processing features
- Modify file detection logic
- Integrate with external APIs
- Add new UI components

### Layout
Edit `index.html` to:
- Add new sections or controls
- Modify the information displayed
- Change the overall layout structure

## Browser Compatibility

- ✅ Chrome 60+
- ✅ Firefox 55+
- ✅ Safari 12+
- ✅ Edge 79+

**Note**: Requires modern browser support for:
- File API with `webkitdirectory`
- CSS Grid and Flexbox
- ES6 JavaScript features

## Future Integration Plans

1. **Unity WebGL Build**: Export Unity HedraEpisodeProcessor as WebGL to run in browser
2. **API Integration**: Connect directly to Hedra APIs from the web interface
3. **Real-time Processing**: Live status updates and progress tracking
4. **Batch Operations**: Advanced batch processing with queue management
5. **Export Features**: Download processed videos and manifests

## Troubleshooting

### Common Issues

**Manifest not loading automatically:**
- Ensure the manifest file is named exactly `hedra_manifest.json`
- Check that it's in the `hedra_processing` subfolder
- Verify the JSON format is valid

**Media files not displaying:**
- Confirm file paths in manifest match actual file locations
- Check that image and audio files exist in the selected folder
- Ensure file extensions are supported (.jpg, .png, .mp3, .wav)

**Browser security restrictions:**
- Some browsers may block local file access
- Consider running from a local web server if needed
- Use browser developer tools to check for console errors

## Development Notes

This template follows clean, modular JavaScript patterns and uses modern CSS features for a professional appearance. The code is well-commented and structured for easy modification and extension.

The design prioritizes:
- **Simplicity**: Easy to understand and use
- **Modularity**: Components can be modified independently
- **Extensibility**: Ready for future feature additions
- **Responsiveness**: Works on desktop and mobile devices 