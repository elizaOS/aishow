// Global state
let currentManifest = null;
let selectedSegmentIndex = -1;
let folderFiles = new Map(); // Store file references for media loading

// DOM elements
const folderInput = document.getElementById('folderInput');
const folderStatus = document.getElementById('folderStatus');
const manifestInfo = document.getElementById('manifestInfo');
const segmentsContainer = document.getElementById('segmentsContainer');
const segmentsGrid = document.getElementById('segmentsGrid');
const processingControls = document.getElementById('processingControls');
const logsContainer = document.getElementById('logsContainer');
const logContent = document.getElementById('logContent');

// Initialize the application
document.addEventListener('DOMContentLoaded', function() {
    initializeEventListeners();
    addLog('info', 'Hedra Manifest Explorer initialized successfully');
    addLog('info', 'Click "Load Episode Folder" and select an episode folder (e.g., S1E56) to get started.');
});

function initializeEventListeners() {
    // Folder selection
    folderInput.addEventListener('change', handleFolderSelection);
    
    // Clear logs button
    document.getElementById('clearLogsBtn').addEventListener('click', () => {
        logContent.innerHTML = '';
        addLog('info', 'Logs cleared');
    });
    
    // Processing buttons (placeholder functionality)
    document.getElementById('processAllBtn').addEventListener('click', () => {
        addLog('warning', 'Process All Segments - Feature coming soon!');
        updateProcessingStatus('Ready to implement batch processing...');
    });
    
    document.getElementById('processSingleBtn').addEventListener('click', () => {
        if (selectedSegmentIndex >= 0) {
            addLog('warning', `Process Single Segment ${selectedSegmentIndex + 1} - Feature coming soon!`);
            updateProcessingStatus(`Ready to process segment ${selectedSegmentIndex + 1}...`);
        } else {
            addLog('error', 'No segment selected for processing');
        }
    });
    
    document.getElementById('stopProcessingBtn').addEventListener('click', () => {
        addLog('info', 'Stop Processing - Feature coming soon!');
        updateProcessingStatus('Processing stopped');
    });
}

function handleFolderSelection(event) {
    const files = Array.from(event.target.files);
    addLog('info', `📁 Selected episode folder with ${files.length} files`);
    
    // Store files in our map for easy access
    folderFiles.clear();
    files.forEach(file => {
        folderFiles.set(file.webkitRelativePath, file);
        // Also store by just the filename for easier matching
        folderFiles.set(file.name, file);
    });
    
    // Debug: Show all loaded files
    addLog('info', '=== LOADED FILES DEBUG ===');
    let audioCount = 0, imageCount = 0;
    for (const [path, file] of folderFiles) {
        if (file.name.includes('.mp3') || file.name.includes('.wav')) {
            addLog('info', `🎵 AUDIO: ${path}`);
            audioCount++;
        } else if (file.name.includes('.jpg') || file.name.includes('.png')) {
            addLog('info', `🖼️ IMAGE: ${path}`);
            imageCount++;
        }
    }
    addLog('success', `=== SUMMARY: ${audioCount} audio files, ${imageCount} image files ===`);
    
    // Update status
    folderStatus.textContent = `Episode loaded: ${files.length} files (${audioCount} audio, ${imageCount} images)`;
    folderStatus.style.color = '#4caf50';
    
    // Look for hedra_manifest.json automatically
    const manifestFile = files.find(file => 
        file.name === 'hedra_manifest.json' && 
        file.webkitRelativePath.includes('hedra_processing')
    );
    
    if (manifestFile) {
        addLog('success', '✅ Found hedra_manifest.json in episode folder, loading automatically...');
        loadManifestFromFile(manifestFile);
    } else {
        addLog('warning', '⚠️ No hedra_manifest.json found in hedra_processing folder');
        addLog('info', '💡 Make sure the episode folder contains a hedra_processing/hedra_manifest.json file');
    }
}

function loadManifestFromFile(file) {
    const reader = new FileReader();
    reader.onload = function(e) {
        try {
            const manifestData = JSON.parse(e.target.result);
            currentManifest = manifestData;
            
            addLog('success', `✅ Manifest loaded successfully with ${manifestData.segmentsToProcess?.length || 0} segments`);
            addLog('success', `✅ Media files available: ${folderFiles.size} files loaded`);
            
            displayManifestInfo();
            displaySegments();
            showProcessingControls();
            
        } catch (error) {
            addLog('error', `❌ Error parsing manifest: ${error.message}`);
        }
    };
    reader.readAsText(file);
}

function displayManifestInfo() {
    if (!currentManifest) return;
    
    const totalSegments = currentManifest.segmentsToProcess?.length || 0;
    
    // Try to extract episode info from file paths
    let episodeInfo = 'Unknown';
    if (totalSegments > 0) {
        const firstSegment = currentManifest.segmentsToProcess[0];
        const audioPath = firstSegment.voiceUrl || '';
        const match = audioPath.match(/S1E(\d+)/);
        if (match) {
            episodeInfo = `Season 1, Episode ${match[1]}`;
        }
    }
    
    document.getElementById('totalSegments').textContent = totalSegments;
    document.getElementById('episodeInfo').textContent = episodeInfo;
    
    manifestInfo.style.display = 'block';
    manifestInfo.classList.add('fade-in');
}

function displaySegments() {
    if (!currentManifest || !currentManifest.segmentsToProcess) return;
    
    addLog('info', `🎭 Creating ${currentManifest.segmentsToProcess.length} segment cards...`);
    
    segmentsGrid.innerHTML = '';
    
    currentManifest.segmentsToProcess.forEach((segment, index) => {
        const segmentCard = createSegmentCard(segment, index);
        segmentsGrid.appendChild(segmentCard);
    });
    
    segmentsContainer.style.display = 'block';
    segmentsContainer.classList.add('fade-in');
    
    addLog('success', '✅ All segment cards created');
}

function createSegmentCard(segment, index) {
    const card = document.createElement('div');
    card.className = 'segment-card';
    card.dataset.index = index;
    
    // Extract filename for display
    const audioFileName = extractFileName(segment.voiceUrl);
    const imageFileName = extractFileName(segment.avatarImage);
    
    card.innerHTML = `
        <div class="segment-header">
            <span class="segment-number">Segment ${index + 1}</span>
            <span class="segment-status status-ready">Ready</span>
        </div>
        
        <div class="segment-text">${segment.text}</div>
        
        <div class="segment-media">
            <div class="media-item">
                <div class="media-preview" id="image-preview-${index}">
                    <div style="display: flex; align-items: center; justify-content: center; height: 100%; color: #999;">
                        🖼️ Image
                    </div>
                </div>
                <div class="media-label">${imageFileName}</div>
            </div>
            <div class="media-item">
                <div class="media-preview" id="audio-preview-${index}">
                    <div style="display: flex; align-items: center; justify-content: center; height: 100%; color: #999;">
                        🎵 Audio
                    </div>
                </div>
                <div class="media-label">${audioFileName}</div>
            </div>
        </div>
        
        <div class="segment-details">
            <div class="detail-item">
                <span>Aspect Ratio:</span>
                <span>${segment.aspectRatio}</span>
            </div>
            <div class="detail-item">
                <span>Resolution:</span>
                <span>${segment.resolution}</span>
            </div>
            <div class="detail-item">
                <span>Audio Source:</span>
                <span>${segment.audioSource}</span>
            </div>
            <div class="detail-item">
                <span>AI Model:</span>
                <span>${segment.ai_model_id_override ? 'Custom' : 'Default'}</span>
            </div>
        </div>
    `;
    
    // Add click handler for selection
    card.addEventListener('click', () => selectSegment(index));
    
    // Load media previews after a small delay to ensure DOM elements exist
    setTimeout(() => {
        loadMediaPreviews(segment, index);
    }, 100);
    
    return card;
}

function loadMediaPreviews(segment, index) {
    addLog('info', `=== 🎬 LOADING MEDIA FOR SEGMENT ${index + 1} ===`);
    
    // Check if we have any files loaded at all
    if (folderFiles.size === 0) {
        addLog('warning', `⚠️ No media files loaded - skipping preview for segment ${index + 1}`);
        addLog('info', '💡 Load an episode folder first to see media previews');
        return;
    }
    
    // Load image preview
    if (segment.avatarImage) {
        addLog('info', `🔍 Looking for image: ${segment.avatarImage}`);
        const imageFile = findFileInFolder(segment.avatarImage);
        
        if (imageFile) {
            addLog('success', `✅ Found image file: ${imageFile.name}`);
            
            // Wait a bit more and try multiple times to find the element
            let attempts = 0;
            const maxAttempts = 5;
            
            const tryLoadImage = () => {
                const imagePreview = document.getElementById(`image-preview-${index}`);
                if (imagePreview) {
                    addLog('success', `✅ Found image preview element for segment ${index + 1}`);
                    const img = document.createElement('img');
                    img.className = 'media-preview';
                    img.src = URL.createObjectURL(imageFile);
                    img.alt = 'Segment image';
                    img.onload = () => {
                        addLog('success', `✅ Image loaded successfully for segment ${index + 1}`);
                    };
                    img.onerror = (e) => {
                        addLog('error', `❌ Failed to load image for segment ${index + 1}: ${e}`);
                    };
                    imagePreview.innerHTML = '';
                    imagePreview.appendChild(img);
                } else {
                    attempts++;
                    if (attempts < maxAttempts) {
                        addLog('warning', `⚠️ Image preview element not found for segment ${index + 1}, retrying... (${attempts}/${maxAttempts})`);
                        setTimeout(tryLoadImage, 200);
                    } else {
                        addLog('error', `❌ Could not find image preview element for segment ${index + 1} after ${maxAttempts} attempts`);
                    }
                }
            };
            
            tryLoadImage();
        } else {
            addLog('error', `❌ Image file not found for: ${segment.avatarImage}`);
        }
    }
    
    // Load audio preview
    if (segment.voiceUrl) {
        addLog('info', `🔍 Looking for audio: ${segment.voiceUrl}`);
        const audioFile = findFileInFolder(segment.voiceUrl);
        
        if (audioFile) {
            addLog('success', `✅ Found audio file: ${audioFile.name}`);
            
            // Wait a bit more and try multiple times to find the element
            let attempts = 0;
            const maxAttempts = 5;
            
            const tryLoadAudio = () => {
                const audioPreview = document.getElementById(`audio-preview-${index}`);
                if (audioPreview) {
                    addLog('success', `✅ Found audio preview element for segment ${index + 1}`);
                    const audio = document.createElement('audio');
                    audio.controls = true;
                    audio.style.width = '100%';
                    audio.style.height = '40px';
                    audio.src = URL.createObjectURL(audioFile);
                    audio.onloadeddata = () => {
                        addLog('success', `✅ Audio loaded successfully for segment ${index + 1}`);
                    };
                    audio.onerror = (e) => {
                        addLog('error', `❌ Failed to load audio for segment ${index + 1}: ${e}`);
                    };
                    
                    const container = document.createElement('div');
                    container.style.display = 'flex';
                    container.style.flexDirection = 'column';
                    container.style.alignItems = 'center';
                    container.style.justifyContent = 'center';
                    container.style.height = '100%';
                    container.style.padding = '5px';
                    
                    container.appendChild(audio);
                    audioPreview.innerHTML = '';
                    audioPreview.appendChild(container);
                } else {
                    attempts++;
                    if (attempts < maxAttempts) {
                        addLog('warning', `⚠️ Audio preview element not found for segment ${index + 1}, retrying... (${attempts}/${maxAttempts})`);
                        setTimeout(tryLoadAudio, 200);
                    } else {
                        addLog('error', `❌ Could not find audio preview element for segment ${index + 1} after ${maxAttempts} attempts`);
                    }
                }
            };
            
            tryLoadAudio();
        } else {
            addLog('error', `❌ Audio file not found for: ${segment.voiceUrl}`);
        }
    }
}

function findFileInFolder(manifestPath) {
    if (!manifestPath) {
        addLog('error', '❌ manifestPath is null or empty');
        return null;
    }
    
    // Check if we have any files loaded
    if (folderFiles.size === 0) {
        addLog('warning', '⚠️ No files loaded in folderFiles map - load episode folder first');
        return null;
    }
    
    // Extract just the filename from the manifest path
    const fileName = extractFileName(manifestPath);
    addLog('info', `🔍 Searching for file: "${fileName}" from manifest path: "${manifestPath}"`);
    
    // Strategy 1: Direct filename match
    if (folderFiles.has(fileName)) {
        addLog('success', `✅ Found by direct filename match: ${fileName}`);
        return folderFiles.get(fileName);
    }
    
    // Strategy 2: Look through all files for exact filename match
    for (const [path, file] of folderFiles) {
        if (file.name === fileName) {
            addLog('success', `✅ Found by file.name match: ${file.name} at ${path}`);
            return file;
        }
    }
    
    // Strategy 3: Look for files that end with the filename
    for (const [path, file] of folderFiles) {
        if (path.endsWith(fileName)) {
            addLog('success', `✅ Found by path ending: ${path}`);
            return file;
        }
    }
    
    // Strategy 4: Look for files that contain the filename
    for (const [path, file] of folderFiles) {
        if (path.includes(fileName)) {
            addLog('success', `✅ Found by path containing: ${path}`);
            return file;
        }
    }
    
    // Strategy 5: Extract relative path and try to match
    // Convert "Assets/Resources/Episodes/S1E56/screenshots/S1E56_1_1.jpg" 
    // to "screenshots/S1E56_1_1.jpg"
    const pathParts = manifestPath.split('/');
    const episodeIndex = pathParts.findIndex(part => part.startsWith('S1E'));
    if (episodeIndex >= 0 && episodeIndex < pathParts.length - 1) {
        const relativePath = pathParts.slice(episodeIndex + 1).join('/');
        addLog('info', `🔍 Trying relative path: "${relativePath}"`);
        
        for (const [path, file] of folderFiles) {
            if (path.includes(relativePath) || path.endsWith(relativePath)) {
                addLog('success', `✅ Found by relative path: ${path}`);
                return file;
            }
        }
    }
    
    // Strategy 6: Look for files in the expected subfolder (screenshots or audio)
    const subfolderMatch = manifestPath.match(/(screenshots|audio)\/([^\/]+)$/);
    if (subfolderMatch) {
        const [, subfolder, filename] = subfolderMatch;
        addLog('info', `🔍 Looking in subfolder "${subfolder}" for "${filename}"`);
        
        for (const [path, file] of folderFiles) {
            if (path.includes(subfolder) && (path.includes(filename) || file.name === filename)) {
                addLog('success', `✅ Found in subfolder: ${path}`);
                return file;
            }
        }
    }
    
    // Strategy 7: Fuzzy match - look for any file with similar name (without extension)
    const baseFileName = fileName.replace(/\.[^/.]+$/, ""); // Remove extension
    addLog('info', `🔍 Trying fuzzy match for base name: "${baseFileName}"`);
    for (const [path, file] of folderFiles) {
        const fileBaseName = file.name.replace(/\.[^/.]+$/, "");
        if (fileBaseName === baseFileName) {
            addLog('warning', `⚠️ Found by fuzzy match: ${file.name} at ${path}`);
            return file;
        }
    }
    
    // Debug: Show what files we DO have (only if we have files)
    addLog('error', `❌ Could not find file: "${fileName}" from "${manifestPath}"`);
    if (folderFiles.size > 0) {
        addLog('info', '📋 Available files (first 10):');
        let count = 0;
        for (const [path, file] of folderFiles) {
            if (count < 10) { // Limit to first 10 files to avoid spam
                addLog('info', `  📄 ${file.name} (${path})`);
            }
            count++;
        }
        if (count > 10) {
            addLog('info', `  ... and ${count - 10} more files`);
        }
    } else {
        addLog('info', '📋 No files available - load episode folder to load media files');
    }
    
    return null;
}

function extractFileName(path) {
    if (!path) return 'Unknown';
    const parts = path.split('/');
    return parts[parts.length - 1] || 'Unknown';
}

function selectSegment(index) {
    // Remove previous selection
    document.querySelectorAll('.segment-card').forEach(card => {
        card.classList.remove('selected');
    });
    
    // Add selection to clicked card
    const selectedCard = document.querySelector(`[data-index="${index}"]`);
    if (selectedCard) {
        selectedCard.classList.add('selected');
        selectedSegmentIndex = index;
        
        // Enable single segment processing button
        document.getElementById('processSingleBtn').disabled = false;
        
        addLog('info', `🎯 Selected segment ${index + 1} for processing`);
        updateProcessingStatus(`Segment ${index + 1} selected and ready for processing`);
    }
}

function showProcessingControls() {
    if (currentManifest && currentManifest.segmentsToProcess?.length > 0) {
        // Enable process all button
        document.getElementById('processAllBtn').disabled = false;
        
        processingControls.style.display = 'block';
        processingControls.classList.add('fade-in');
    }
}

function updateProcessingStatus(message) {
    document.getElementById('processingStatus').textContent = message;
}

function addLog(type, message) {
    const timestamp = new Date().toLocaleTimeString();
    const logEntry = document.createElement('p');
    logEntry.className = `log-entry ${type}`;
    logEntry.textContent = `[${timestamp}] ${message}`;
    
    logContent.appendChild(logEntry);
    logContent.scrollTop = logContent.scrollHeight;
}

// Utility functions for future integration with HedraEpisodeProcessor

/**
 * Get the current manifest data
 * @returns {Object|null} The loaded manifest object
 */
function getCurrentManifest() {
    return currentManifest;
}

/**
 * Get the currently selected segment
 * @returns {Object|null} The selected segment object
 */
function getSelectedSegment() {
    if (selectedSegmentIndex >= 0 && currentManifest?.segmentsToProcess) {
        return currentManifest.segmentsToProcess[selectedSegmentIndex];
    }
    return null;
}

/**
 * Update segment status (for future processing integration)
 * @param {number} index - Segment index
 * @param {string} status - Status: 'ready', 'processing', 'complete', 'error'
 */
function updateSegmentStatus(index, status) {
    const card = document.querySelector(`[data-index="${index}"]`);
    if (card) {
        const statusElement = card.querySelector('.segment-status');
        statusElement.className = `segment-status status-${status}`;
        
        const statusText = {
            'ready': 'Ready',
            'processing': 'Processing',
            'complete': 'Complete',
            'error': 'Error'
        };
        
        statusElement.textContent = statusText[status] || status;
    }
}

/**
 * Simulate processing for demonstration (remove when integrating with real processor)
 */
function simulateProcessing(segmentIndex) {
    updateSegmentStatus(segmentIndex, 'processing');
    addLog('info', `Starting processing for segment ${segmentIndex + 1}`);
    
    // Simulate processing time
    setTimeout(() => {
        updateSegmentStatus(segmentIndex, 'complete');
        addLog('success', `Segment ${segmentIndex + 1} processing completed`);
    }, 3000);
}

// Export functions for potential external use
window.HedraExplorer = {
    getCurrentManifest,
    getSelectedSegment,
    updateSegmentStatus,
    addLog,
    updateProcessingStatus
}; 