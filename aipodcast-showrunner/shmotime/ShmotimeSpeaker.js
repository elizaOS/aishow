class ShmotimeSpeaker extends ShmotimeEventEmitter {
    constructor({ voiceSystem = 'elevenlabs', saveAudioCache = false } = {}) {
        super();
        this.voiceSystem = voiceSystem;

        this.saveAudioCacheMode = saveAudioCache; // works, if you set it to TRUE.
        
        // Browser TTS setup
        this.synth = window.speechSynthesis;
        this.voices = [];
        this.voicesByName = new Map();
        
        // ElevenLabs-specific setup (only used when voiceSystem === 'elevenlabs')
        this.elevenLabsVoices = null;
        this.actorVoiceMappings = new Map();
        this.showVoiceMappings = new Map(); // Add initialization
        this.currentAudio = null;

        // Add new blob promise cache
        this.blobPromiseCache = new Map();

        // Add new properties for caching
        this.audioCache = new Map(); // In-memory cache for current episode
        this.currentEpisodeId = null;
        this.currentShowId = null;
        this.zip = new JSZip();
        
        // Speech configuration
        this.defaultRate = 1.0;
        this.defaultPitch = 1.0;
        this.defaultVolume = 0.8;
        
        // Action modifiers for speech
        this.actionModifiers = {
            normal: { rate: 1.0, pitch: 1.0 },
            excited: { rate: 1.2, pitch: 1.2 },
            angry: { rate: 1.1, pitch: 0.8 },
            sad: { rate: 0.9, pitch: 0.9 },
            whisper: { rate: 0.9, pitch: 1.1, volume: 0.4 },
            yell: { rate: 1.1, pitch: 1.1, volume: 1.0 },
            fight: { rate: 1.3, pitch: 1.2 },
            nervous: { rate: 1.1, pitch: 1.2 }
        };

        this.assetsBasePath = ''; // This can be configured if needed
    }

    async initializeVoiceSystem() {
        try {
            if (this.voiceSystem === 'browser') {
                await this.preloadBrowserVoices();
            } else {
                await this.preloadElevenLabsVoices();
                await this.loadVoiceMappings(); // Only load mappings for elevenlabs
            }
            
            this.isReady = true;
            this.initialize();
        } catch (error) {
            console.error('Failed to initialize voice system:', error);
            throw error;
        }
    }

    async preloadBrowserVoices() {
        return new Promise((resolve, reject) => {
            let attempts = 0;
            const maxAttempts = 5;
            const attemptInterval = 1000;

            const tryLoadVoices = () => {
                attempts++;
                const voices = this.synth.getVoices();
                
                if (voices.length > 0) {
                    this.voices = voices;
                    // Create a map of voices by both name and voiceURI for easier lookup
                    this.voicesByName = new Map(
                        this.voices.flatMap(voice => [
                            [voice.name, voice],
                            [voice.voiceURI, voice]
                        ])
                    );
                    console.log('[ShmotimeSpeaker] Loaded browser voices:', 
                        this.voices.map(v => ({name: v.name, uri: v.voiceURI})));
                    resolve();
                } else if (attempts < maxAttempts) {
                    setTimeout(tryLoadVoices, attemptInterval);
                } else {
                    reject(new Error('Failed to load TTS voices after multiple attempts'));
                }
            };

            if (this.synth.getVoices().length > 0) {
                tryLoadVoices();
            } else {
                this.synth.onvoiceschanged = () => {
                    this.synth.onvoiceschanged = null;
                    tryLoadVoices();
                };
            }
        });
    }

    getBrowserVoiceForActor(actorConfig) {
        if (!actorConfig) {
            console.warn('[ShmotimeSpeaker] No actor config provided, using fallback voice');
            return this.getFallbackVoice();
        }

        const requestedVoice = actorConfig.voice;
        if (!requestedVoice) {
            console.warn('[ShmotimeSpeaker] No voice specified for actor, using fallback voice');
            return this.getFallbackVoice();
        }

        // Try to find the voice by name or URI
        const voice = this.voicesByName.get(requestedVoice);
        
        if (voice) {
            console.log('[ShmotimeSpeaker] Found requested voice for actor:', voice.name);
            return voice;
        }

        console.warn('[ShmotimeSpeaker] Requested voice not found:', requestedVoice);
        return this.getFallbackVoice();
    }

    getFallbackVoice() {
        console.log('[ShmotimeSpeaker] Getting fallback voice');
        
        // Try to find an English voice first
        const fallbackVoice = this.voices.find(v => 
            v.lang.startsWith('en') && 
            v.localService
        );

        if (fallbackVoice) {
            console.log('[ShmotimeSpeaker] Found English fallback voice:', fallbackVoice.name);
            return fallbackVoice;
        }

        // If no English voice found, use the first available voice
        if (this.voices.length > 0) {
            console.log('[ShmotimeSpeaker] Using first available voice:', this.voices[0].name);
            return this.voices[0];
        }

        throw new Error('No voices available for text-to-speech');
    }

    getCastingPrompt() {
        return `You are a voice casting expert. I will provide you with a list of available voices and their characteristics, along with a list of show characters that need voices assigned to them. Some characters may already have voices assigned - these are provided for context but should not be reassigned.

Key considerations:
- Match voice gender to character gender when possible
- Consider character personality and background
- Consider character age and maturity level
- Match accent/dialect if specified in character description
- Ensure each voice is only used once across all characters
- Consider existing voice assignments when making new assignments to maintain cast harmony
- For best consistency, consider character relationships and interactions

Respond only with a JSON object of {actorId: voiceId} mappings. Nothing else.`;
    }



    getCacheKey(showId, episodeId, sceneIndex, dialogueIndex) {
        return `${showId}_${episodeId}_${sceneIndex + 1}_${dialogueIndex + 1}`;
    }

    // Helper method to find scene and dialogue indices
    findIndices(dialogue, scene, episode) {
        const sceneIndex = episode.scenes.findIndex(s => s === scene);

        const dialogueIndex = scene.dialogue.findIndex(d => 
            d.actor === dialogue.actor && 
            d.line === dialogue.line && 
            d.action === dialogue.action
        );

        return { sceneIndex, dialogueIndex };
    }

    async ensureEpisodeCached(showConfig, episode) {
        // First ensure all voices are mapped
        console.log('[ShmotimeSpeaker] Ensuring voice mappings for all actors...');
        await this.ensureVoiceMappingsForShow(showConfig.actors);

        // Verify all actors have voice mappings before proceeding
        const unmappedActors = [];
        for (const scene of episode.scenes) {
            for (const dialogue of scene.dialogue) {
                if (!this.actorVoiceMappings.has(dialogue.actor)) {
                    unmappedActors.push(dialogue.actor);
                }
            }
        }

        if (unmappedActors.length > 0) {
            console.log('[ShmotimeSpeaker] Found unmapped actors, forcing mapping:', unmappedActors);

            if( !this.elevenLabsVoices ) {
                await this.fetchElevenLabsVoices();
            }

            // If any unmapped actors found, force another mapping attempt
            await this.batchCastVoices(showConfig.actors);
            
            // Verify again
            const stillUnmapped = unmappedActors.filter(actor => !this.actorVoiceMappings.has(actor));
            if (stillUnmapped.length > 0) {
                throw new Error(`Failed to map voices for actors: ${stillUnmapped.join(', ')}`);
            }
        }

        console.log('[ShmotimeSpeaker] Current voice mappings:', 
            Object.fromEntries(this.actorVoiceMappings));

        // Now cache all dialogue
        for (const scene of episode.scenes) {
            const sceneIndex = episode.scenes.findIndex(s => s === scene);
            
            for (const dialogue of scene.dialogue) {
                const { dialogueIndex } = this.findIndices(dialogue, scene, episode);
                const key = this.getCacheKey(showConfig.id, episode.id, sceneIndex, dialogueIndex);

                if (!this.blobPromiseCache.has(key)) {
                    console.log(`[ShmotimeSpeaker] Caching dialogue: ${key}`, {
                        actor: dialogue.actor,
                        line: dialogue.line,
                        sceneIndex,
                        dialogueIndex
                    });

                    const voiceId = this.actorVoiceMappings.get(dialogue.actor);
                    if (!voiceId) {
                        // This should never happen now due to our checks above
                        throw new Error(`No voice mapping found for actor: ${dialogue.actor}`);
                    }

                    this.blobPromiseCache.set(
                        key,
                        this.generateAndCacheElevenLabsAudio(
                            dialogue.line,
                            voiceId,
                            showConfig.id,
                            episode.id,
                            sceneIndex,
                            dialogueIndex
                        )
                    );
                }
            }
        }

        // Log cache status
        console.log('[ShmotimeSpeaker] Blob promise cache status:', 
            Array.from(this.blobPromiseCache.keys()));
    }

    // New method to play from blob promise cache
    async playBlobCachedAudio(showId, episodeId, sceneIndex, dialogueIndex) {
        const cacheKey = this.getCacheKey(showId, episodeId, sceneIndex, dialogueIndex);
        
        const blobPromise = this.blobPromiseCache.get(cacheKey);
        if (!blobPromise) {
            throw new Error(`No cached blob promise found for key: ${cacheKey}`);
        }

        console.log(`[ShmotimeSpeaker] Playing cached audio: ${cacheKey}`);

        // Wait for the promise to resolve and play the audio
        try {
            const audioBlob = await blobPromise;
            return this.playAudioBlob(audioBlob);
        } catch (error) {
            console.error(`[ShmotimeSpeaker] Error playing cached audio ${cacheKey}:`, error);
            throw error;
        }
    }

    // Modified speakDialogue method
    async speakDialogue(dialogue, actorConfig, showConfig, episode, scene) {
        console.log('[ShmotimeSpeaker] Speaking dialogue:', {
            actor: dialogue.actor,
            line: dialogue.line,
            voiceSystem: this.voiceSystem
        });

        if (!this.isReady) {
            throw new Error('TTS not initialized');
        }

        if (this.voiceSystem === 'elevenlabs') {
            try {
                // Ensure all dialogue lines in the episode are being cached
                await this.ensureEpisodeCached(showConfig, episode);

                // Find indices using consistent helper method
                const { sceneIndex, dialogueIndex } = this.findIndices(dialogue, scene, episode);

                // Play from cache (will wait for promise to resolve if still generating)
                return await this.playBlobCachedAudio(
                    showConfig.id,
                    episode.id,
                    sceneIndex,
                    dialogueIndex
                );
            } catch (error) {
                console.error('[ShmotimeSpeaker] Error in speakDialogue:', error);
                throw error;
            }
        } else {
            // Browser TTS implementation
            if (this.synth.speaking) {
                this.synth.cancel();
            }

            const modifiers = this.getActionModifiers(dialogue.action);
            const utterance = new SpeechSynthesisUtterance(dialogue.line);
            utterance.voice = this.getBrowserVoiceForActor(actorConfig);
            utterance.rate = modifiers.rate * this.defaultRate;
            utterance.pitch = modifiers.pitch * this.defaultPitch;
            utterance.volume = (modifiers.volume || this.defaultVolume);

            return new Promise((resolve, reject) => {
                utterance.onend = resolve;
                utterance.onerror = reject;
                this.synth.speak(utterance);
            });
        }
    }

    async start() {
        this.addEventListeners();
        if (this.voiceSystem === 'browser') {
            await this.preloadBrowserVoices();
        }
        await this.loadVoiceMappings();
        this.isReady = true;
        this.initialize();
    }

    async getVoiceForActor(actorId, actorConfig, allShowActors) {
        console.log('[ShmotimeSpeaker] Getting voice for actor:', { actorId, actorConfig });
        
        // Check if we need to load voices first
        if (!this.elevenLabsVoices) {
            console.log('[ShmotimeSpeaker] No voices loaded yet, fetching...');
            await this.fetchElevenLabsVoices();
        }

        // Check if we already have a mapping for this actor
        if (this.actorVoiceMappings.has(actorId)) {
            console.log('[ShmotimeSpeaker] Found existing voice mapping for actor');
            return this.actorVoiceMappings.get(actorId);
        }

        // If we hit an unmapped actor, do batch casting for all actors
        console.log('[ShmotimeSpeaker] No mapping found, performing batch casting for all show actors');
        await this.batchCastVoices(allShowActors);
        
        // Now the mapping should exist
        if (this.actorVoiceMappings.has(actorId)) {
            return this.actorVoiceMappings.get(actorId);
        }

        throw new Error(`Failed to get voice mapping for actor: ${actorId}`);
    }

    async batchCastVoices(allShowActors) {
        // Create a simplified version of available voices
        const simplifiedVoices = this.elevenLabsVoices.map(voice => ({
            voice_id: voice.voice_id,
            name: voice.name,
            labels: voice.labels
        }));

        // Prepare actors data with existing voice assignments
        const actorsWithAssignmentInfo = Object.entries(allShowActors).map(([id, config]) => ({
            id,
            ...config,
            existing_voice: this.actorVoiceMappings.has(id) ? 
                this.actorVoiceMappings.get(id) : null
        }));

        console.log('[ShmotimeSpeaker] Requesting batch voice casting:', {
            totalActors: actorsWithAssignmentInfo.length,
            existingMappings: Array.from(this.actorVoiceMappings.entries())
        });

        const response = await fetch(`${window.apiBaseUrl}/api/cast-voices`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                voices: simplifiedVoices,
                actors: actorsWithAssignmentInfo,
                castingPrompt: this.getCastingPrompt()
            })
        });

        if (!response.ok) {
            throw new Error('Failed to batch cast voices');
        }

        const data = await response.json();
        const mappingText = data.content[0].text;
        const newMapping = JSON.parse(mappingText);

        // Only apply new mappings for previously unmapped actors
        for (const [actorId, voiceId] of Object.entries(newMapping)) {
            if (!this.actorVoiceMappings.has(actorId)) {
                console.log(`[ShmotimeSpeaker] Assigning new voice ${voiceId} to unmapped actor ${actorId}`);
                this.actorVoiceMappings.set(actorId, voiceId);
            } else {
                console.log(`[ShmotimeSpeaker] Preserving existing voice for actor ${actorId}`);
            }
        }

        // Save the updated mappings
        this.saveVoiceMappings();
    }

    async generateVoiceMapping(actors, retryCount = 0) {
        const maxRetries = 5;
        const baseDelay = 2000;

        try {
            // Log which actors need voices
            const existingActors = new Set(Object.keys(this.actorVoiceMappings));
            const newActors = Object.keys(actors).filter(id => !existingActors.has(id));
            
            console.log('[ShmotimeSpeaker] Actors needing voice assignment:', newActors);
            console.log('[ShmotimeSpeaker] Generating voice mapping for all actors:', actors);

            const castingPrompt = this.getCastingPrompt();

            const simplifiedVoices = this.elevenLabsVoices.map(voice => ({
                voice_id: voice.voice_id,
                name: voice.name,
                labels: voice.labels
            }));

            console.log('[ShmotimeSpeaker] Requesting voices for all uncasted show actors');
            
            const response = await fetch(`${window.apiBaseUrl}/api/cast-voices`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    voices: simplifiedVoices,
                    actors: actors,
                    castingPrompt
                })
            });

            const data = await response.json();
            console.log('[ShmotimeSpeaker] Received mapping data:', data);

            // Handle overloaded error with exponential backoff
            if (data.type === 'error' && data.error?.type === 'overloaded_error') {
                if (retryCount >= maxRetries) {
                    throw new Error('Maximum retry attempts reached');
                }

                const delay = baseDelay * Math.pow(2, retryCount);
                console.log(`[ShmotimeSpeaker] Service overloaded, retrying in ${delay/1000} seconds (attempt ${retryCount + 1}/${maxRetries})`);
                
                await new Promise(resolve => setTimeout(resolve, delay));
                return this.generateVoiceMapping(actors, retryCount + 1);
            }

            if (!response.ok) {
                throw new Error('Failed to generate mapping');
            }

            const mappingText = data.content[0].text;
            console.log('[ShmotimeSpeaker] Parsing mapping text:', mappingText);
            
            return JSON.parse(mappingText);
        } catch (error) {
            if (error.message === 'Maximum retry attempts reached') {
                throw error;
            }
            
            if (retryCount < maxRetries) {
                const delay = baseDelay * Math.pow(2, retryCount);
                console.log(`[ShmotimeSpeaker] Error occurred, retrying in ${delay/1000} seconds (attempt ${retryCount + 1}/${maxRetries}):`, error);
                
                await new Promise(resolve => setTimeout(resolve, delay));
                return this.generateVoiceMapping(actors, retryCount + 1);
            }
            
            console.error('[ShmotimeSpeaker] Failed to generate voice mapping:', error);
            throw error;
        }
    }

    async getVoiceMappingForShow(showId, actors) {
        console.log('[ShmotimeSpeaker] Getting voice mapping for show:', showId);
        
        // Check if we need to load voices first
        if (!this.elevenLabsVoices) {
            console.log('[ShmotimeSpeaker] No voices loaded yet, fetching...');
            await this.fetchElevenLabsVoices();
        }

        // Check if we already have mapping for this show
        if (this.showVoiceMappings.has(showId)) {
            console.log('[ShmotimeSpeaker] Found existing mapping for show');
            return this.showVoiceMappings.get(showId);
        }

        console.log('[ShmotimeSpeaker] No existing mapping found, generating new one');
        // Generate new mapping
        const newMapping = await this.generateVoiceMapping(actors);
        console.log('[ShmotimeSpeaker] Generated new mapping:', newMapping);

        // Save to both mapping stores
        this.showVoiceMappings.set(showId, newMapping);
        
        // Update actor mappings with new assignments
        for (const [actorId, voiceId] of Object.entries(newMapping)) {
            if (!this.actorVoiceMappings.has(actorId)) {
                this.actorVoiceMappings.set(actorId, voiceId);
            }
        }
        
        // Save both mappings to localStorage
        this.saveVoiceMappings();
        
        return newMapping;
    }

    async ensureVoiceMappingsForShow(showActors) {
        // Check if any actors need voice mapping
        const needsMapping = Object.keys(showActors).some(actorId => 
            !this.actorVoiceMappings.has(actorId)
        );

        if (!needsMapping) {
            console.log('[ShmotimeSpeaker] All show actors already have voice mappings');
            return;
        }

        // Get mappings for ALL actors in the show
        const mapping = await this.getVoiceMappingForShow('show', showActors);

        // Only apply new mappings to actors that didn't have one before
        for (const [actorId, voiceId] of Object.entries(mapping)) {
            if (!this.actorVoiceMappings.has(actorId)) {
                console.log(`[ShmotimeSpeaker] Assigning new voice ${voiceId} to unmapped actor ${actorId}`);
                this.actorVoiceMappings.set(actorId, voiceId);
            } else {
                console.log(`[ShmotimeSpeaker] Preserving existing voice for actor ${actorId}`);
            }
        }

        // Make sure both mapping stores are saved
        this.saveVoiceMappings();

        // Log final mapping state
        console.log('[ShmotimeSpeaker] Final voice mappings:', 
            Object.fromEntries(this.actorVoiceMappings));
    }

    async assignVoiceToActor(actorId, actorConfig) {
        console.log('[ShmotimeSpeaker] Assigning voice to actor:', { actorId, actorConfig });

        // Create a simplified version of available voices
        const simplifiedVoices = this.elevenLabsVoices.map(voice => ({
            voice_id: voice.voice_id,
            name: voice.name,
            labels: voice.labels
        }));

        // Filter out voices that are already assigned to other actors
        const usedVoices = new Set(this.actorVoiceMappings.values());
        const availableVoices = simplifiedVoices.filter(voice => 
            !usedVoices.has(voice.voice_id)
        );

        if (availableVoices.length === 0) {
            console.warn('[ShmotimeSpeaker] No available voices, will reuse existing voices');
            // If no available voices, use all voices
            availableVoices.push(...simplifiedVoices);
        }

        const response = await fetch(`${window.apiBaseUrl}/api/cast-voices`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                voices: availableVoices,
                actors: [{ id: actorId, ...actorConfig }],
                castingPrompt: this.getCastingPrompt()
            })
        });

        if (!response.ok) {
            throw new Error('Failed to assign voice to actor');
        }

        const data = await response.json();
        const mapping = JSON.parse(data.content[0].text);
        
        return mapping[actorId];
    }

    async fetchElevenLabsVoices() {
        console.log('[ShmotimeSpeaker] Fetching ElevenLabs voices...');
        const response = await fetch(`${window.apiBaseUrl}/api/elevenlabs/voices`);
        if (!response.ok) {
            console.error('[ShmotimeSpeaker] Failed to fetch voices:', response.status, response.statusText);
            throw new Error('Failed to fetch ElevenLabs voices');
        }
        const data = await response.json();
        console.log('[ShmotimeSpeaker] Received voices:', data.voices);
        this.elevenLabsVoices = data.voices;
    }

    async getVoiceMappingForShow(showId, actors) {
        console.log('[ShmotimeSpeaker] Getting voice mapping for show:', showId);
        
        // Check if we need to load voices first
        if (!this.elevenLabsVoices) {
            console.log('[ShmotimeSpeaker] No voices loaded yet, fetching...');
            await this.fetchElevenLabsVoices();
        }

        // Check if we already have mapping for this show
        if (this.showVoiceMappings.has(showId)) {
            console.log('[ShmotimeSpeaker] Found existing mapping for show');
            return this.showVoiceMappings.get(showId);
        }

        console.log('[ShmotimeSpeaker] No existing mapping found, generating new one');
        // Generate new mapping
        const mapping = await this.generateVoiceMapping(actors);
        console.log('[ShmotimeSpeaker] Generated new mapping:', mapping);
        this.showVoiceMappings.set(showId, mapping);
        
        // Save updated mappings to localStorage
        this.saveVoiceMappings();
        
        return mapping;
    }

    async loadVoiceMappings() {
        try {
            // Load actor voice mappings
            const storedActorMappings = localStorage.getItem('actorVoiceMappings');
            if (storedActorMappings) {
                const mappingsObj = JSON.parse(storedActorMappings);
                this.actorVoiceMappings = new Map(Object.entries(mappingsObj));
            } else {
                this.actorVoiceMappings = new Map();
            }

            // Load show voice mappings
            const storedShowMappings = localStorage.getItem('showVoiceMappings');
            if (storedShowMappings) {
                const showMappingsObj = JSON.parse(storedShowMappings);
                this.showVoiceMappings = new Map(Object.entries(showMappingsObj));
            } else {
                this.showVoiceMappings = new Map();
            }
        } catch (error) {
            console.error('[ShmotimeSpeaker] Error loading voice mappings from localStorage:', error);
            this.actorVoiceMappings = new Map();
            this.showVoiceMappings = new Map();
        }
    }

    // Modified saveVoiceMappings to also save show mappings
    saveVoiceMappings() {
        try {
            // Save actor mappings
            const actorMappingsObj = Object.fromEntries(this.actorVoiceMappings);
            localStorage.setItem('actorVoiceMappings', JSON.stringify(actorMappingsObj));

            // Save show mappings
            const showMappingsObj = Object.fromEntries(this.showVoiceMappings);
            localStorage.setItem('showVoiceMappings', JSON.stringify(showMappingsObj));
        } catch (error) {
            console.error('[ShmotimeSpeaker] Error saving voice mappings to localStorage:', error);
        }
    }

    async playElevenLabsAudio(text, voiceId) {
        console.log('OBSOLETE?');
        console.log('[ShmotimeSpeaker] Playing audio with voice ID:', voiceId);
        
        // Stop any currently playing audio
        if (this.currentAudio) {
            console.log('[ShmotimeSpeaker] Stopping current audio');
            this.currentAudio.pause();
            this.currentAudio = null;
        }

        console.log('[ShmotimeSpeaker] Requesting speech generation...');
        const response = await fetch(`${window.apiBaseUrl}/api/elevenlabs/speak`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                text,
                voice_id: voiceId
            })
        });

        if (!response.ok) {
            console.error('[ShmotimeSpeaker] Speech generation failed:', response.status, response.statusText);
            throw new Error('Failed to generate speech audio');
        }

        // Create blob from audio response
        const audioBlob = await response.blob();
        const audioUrl = URL.createObjectURL(audioBlob);
        console.log('[ShmotimeSpeaker] Created audio URL:', audioUrl);

        // Create and play audio element
        return new Promise((resolve, reject) => {
            const audio = new Audio(audioUrl);
            this.currentAudio = audio;

            audio.onended = () => {
                console.log('[ShmotimeSpeaker] Audio playback complete');
                URL.revokeObjectURL(audioUrl);
                this.currentAudio = null;
                resolve();
            };

            audio.onerror = (error) => {
                console.error('[ShmotimeSpeaker] Audio playback error:', error);
                URL.revokeObjectURL(audioUrl);
                this.currentAudio = null;
                reject(error);
            };

            console.log('[ShmotimeSpeaker] Starting audio playback...');
            audio.play().catch(error => {
                console.error('[ShmotimeSpeaker] Failed to start playback:', error);
                reject(error);
            });
        });
    }

    getActionModifiers(action) {
        return this.actionModifiers[action?.toLowerCase()] || this.actionModifiers.normal;
    }

    // New method to play cached audio file
    async playCachedAudio(audioPath) {
        try {
            // Try to fetch the audio file
            const response = await fetch(audioPath);
            if (!response.ok) {
                throw new Error('Audio file not found');
            }
            const audioBlob = await response.blob();
            return this.playAudioBlob(audioBlob);
        } catch (error) {
            throw new Error('Failed to play cached audio');
        }
    }

    // New method to play audio blob
    async playAudioBlob(audioBlob) {
        // Stop any currently playing audio
        if (this.currentAudio) {
            this.currentAudio.pause();
            this.currentAudio = null;
        }

        const audioUrl = URL.createObjectURL(audioBlob);

        return new Promise((resolve, reject) => {
            const audio = new Audio(audioUrl);
            this.currentAudio = audio;

            audio.onended = () => {
                URL.revokeObjectURL(audioUrl);
                this.currentAudio = null;
                resolve();
            };

            audio.onerror = (error) => {
                URL.revokeObjectURL(audioUrl);
                this.currentAudio = null;
                reject(error);
            };

            audio.play().catch(reject);
        });
    }

    formatTextForElevenLabs(text) {
        // Replace *actions* with [actions]
        const formattedText = text.replace(/\*(.+?)\*/g, (_, action) => {
            const trimmedAction = action.trim();
            return `[${trimmedAction}]`;
        });
        return formattedText;
    }

    async tryLoadPreGeneratedAudio(showId, episodeId, sceneIndex, dialogueIndex) {
        const fileName = `${episodeId}_${sceneIndex + 1}_${dialogueIndex + 1}.mp3`;
        const audioPath = `${this.assetsBasePath}assets_${showId}/audio/${fileName}`;

        try {
            console.log(`[ShmotimeSpeaker] Attempting to load pre-generated audio: ${audioPath}`);
            const response = await fetch(audioPath);
            
            if (!response.ok) {
                console.log(`[ShmotimeSpeaker] Pre-generated audio not found: ${audioPath}`);
                return null;
            }

            const audioBlob = await response.blob();
            console.log(`[ShmotimeSpeaker] Successfully loaded pre-generated audio: ${audioPath}`);
            return audioBlob;
        } catch (error) {
            console.log(`[ShmotimeSpeaker] Error loading pre-generated audio: ${audioPath}`, error);
            return null;
        }
    }

    // Modify the generateAndCacheElevenLabsAudio method to first try pre-generated audio
    async generateAndCacheElevenLabsAudio(text_raw, voiceId, showId, episodeId, sceneIndex, dialogueIndex, retryCount = 0) {
        // First try to load pre-generated audio
        const preGeneratedAudio = await this.tryLoadPreGeneratedAudio(showId, episodeId, sceneIndex, dialogueIndex);
        
        if (preGeneratedAudio) {
            // If we found pre-generated audio, cache it and return it
            const regularCacheKey = `${episodeId}_${sceneIndex + 1}_${dialogueIndex + 1}`;
            this.audioCache.set(regularCacheKey, preGeneratedAudio);
            this.currentEpisodeId = episodeId;
            this.currentShowId = showId;
            
            const cacheKey = this.getCacheKey(showId, episodeId, sceneIndex, dialogueIndex);
            console.log(`[ShmotimeSpeaker] Using pre-generated audio for ${cacheKey}`);
            return preGeneratedAudio;
        }

        // If no pre-generated audio found, fall back to ElevenLabs generation
        console.log(`[ShmotimeSpeaker] No pre-generated audio found, falling back to ElevenLabs generation`);
        
        const MAX_RETRIES = 5;
        const BASE_DELAY = 2000;
        
        try {
            console.log('[ShmotimeSpeaker] Generating audio:', {
                text_raw,
                voiceId,
                showId,
                episodeId,
                sceneIndex,
                dialogueIndex,
                retryAttempt: retryCount
            });

            const text = this.formatTextForElevenLabs(text_raw);
            const response = await fetch(`${window.apiBaseUrl}/api/elevenlabs/speak`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    text,
                    voice_id: voiceId
                })
            });

            if (!response.ok) {
                const errorText = await response.text().catch(e => 'No error details available');
                console.error(`[ShmotimeSpeaker] Request failed with status ${response.status}:`, {
                    status: response.status,
                    statusText: response.statusText,
                    errorDetails: errorText
                });

                if (retryCount < MAX_RETRIES) {
                    const delay = BASE_DELAY * Math.pow(2, retryCount);
                    console.log(`[ShmotimeSpeaker] Retrying in ${delay/1000} seconds (attempt ${retryCount + 1}/${MAX_RETRIES})`);
                    
                    await new Promise(resolve => setTimeout(resolve, delay));
                    
                    return this.generateAndCacheElevenLabsAudio(
                        text_raw,
                        voiceId,
                        showId,
                        episodeId,
                        sceneIndex,
                        dialogueIndex,
                        retryCount + 1
                    );
                }

                throw new Error(`Failed to generate speech audio after ${MAX_RETRIES} retries: ${response.status} ${response.statusText} - ${errorText}`);
            }

            const audioBlob = await response.blob();
            
            const regularCacheKey = `${episodeId}_${sceneIndex + 1}_${dialogueIndex + 1}`;
            this.audioCache.set(regularCacheKey, audioBlob);
            this.currentEpisodeId = episodeId;
            this.currentShowId = showId;

            const cacheKey = this.getCacheKey(showId, episodeId, sceneIndex, dialogueIndex);
            console.log(`[ShmotimeSpeaker] Successfully generated audio for ${cacheKey}`);
            return audioBlob;
        } catch (error) {
            if (retryCount < MAX_RETRIES) {
                const delay = BASE_DELAY * Math.pow(2, retryCount);
                console.error('[ShmotimeSpeaker] Error generating audio (will retry):', error);
                console.log(`[ShmotimeSpeaker] Retrying in ${delay/1000} seconds (attempt ${retryCount + 1}/${MAX_RETRIES})`);
                
                await new Promise(resolve => setTimeout(resolve, delay));
                
                return this.generateAndCacheElevenLabsAudio(
                    text_raw,
                    voiceId,
                    showId,
                    episodeId,
                    sceneIndex,
                    dialogueIndex,
                    retryCount + 1
                );
            }

            console.error('[ShmotimeSpeaker] Error generating audio (no more retries):', error);
            throw error;
        }
    }

    // New method to handle episode completion and ZIP creation
    async handleEpisodeComplete() {
        if (this.audioCache.size === 0) {
            console.log('[ShmotimeSpeaker] No audio files to zip');
            return;
        }

        try {
            const zip = new JSZip();
            const audioFolder = zip.folder(`assets_${this.currentShowId}/audio`);

            // Add all cached audio files to the ZIP
            for (const [key, audioBlob] of this.audioCache.entries()) {
                const fileName = `${key}.mp3`;
                audioFolder.file(fileName, audioBlob);
            }

            // Generate and download the ZIP file
            const zipBlob = await zip.generateAsync({ type: 'blob' });
            const zipUrl = URL.createObjectURL(zipBlob);
            
            // Create and trigger download
            const downloadLink = document.createElement('a');
            downloadLink.href = zipUrl;
            downloadLink.download = `audio_${this.currentShowId}_${this.currentEpisodeId}.zip`;
            document.body.appendChild(downloadLink);
            downloadLink.click();
            document.body.removeChild(downloadLink);
            
            // Cleanup
            URL.revokeObjectURL(zipUrl);
            this.audioCache.clear();
            this.currentEpisodeId = null;
            this.currentShowId = null;

        } catch (error) {
            console.error('[ShmotimeSpeaker] Failed to create ZIP file:', error);
        }
    }

    addEventListeners() {
        this.shmotime.on('system:initialize', this.initialize.bind(this));

        this.shmotime.on('manager:speak', async ({showConfig, dialogue, episode, scene}) => {
            try {
                const actorConfig = showConfig.actors[dialogue.actor];
                
                if (!actorConfig) {
                    console.warn(`No actor config found for: ${dialogue.actor}`);
                }

                // Use direct reference comparison instead of property matching
                const sceneIndex = episode.scenes.findIndex(s => s === scene);
                
                const dialogueIndex = scene.dialogue.findIndex(d => 
                    d.actor === dialogue.actor && 
                    d.line === dialogue.line && 
                    d.action === dialogue.action
                );

                const speechIdentifier = `Show:${showConfig.id} Episode:${episode.id} Scene:${sceneIndex + 1} Dialogue:${dialogueIndex + 1}`;
                
                this.shmotime.emit('speaker:speakStart', {
                    actor: dialogue.actor,
                    actorConfig,
                    dialogue,
                    episode,
                    scene,
                    speechIdentifier
                });

                await this.speakDialogue(dialogue, actorConfig, showConfig, episode, scene);

                this.shmotime.emit('speaker:speakEnd', {
                    actor: dialogue.actor,
                    actorConfig,
                    dialogue,
                    episode,
                    scene,
                    speechIdentifier
                });

                this.shmotime.emit('system:speakComplete', { speechIdentifier });
            } catch (error) {
                console.error('Speech failed:', error);
                this.shmotime.emit('speaker:error', {
                    error,
                    dialogue,
                    episode,
                    scene
                });
                this.shmotime.emit('system:speakComplete');
            }
        });



        const saveAudioCacheMode = this.saveAudioCacheMode;
        if( saveAudioCacheMode ) {
            let isInFinalScene = false;
            let totalDialogueInScene = 0;
            let currentDialogueCount = 0;

            this.shmotime.on('manager:prepareScene', (sceneInfo) => {
                const { sceneIndex, totalScenes } = sceneInfo.sequenceInfo;
                isInFinalScene = (sceneIndex === totalScenes - 1);
                
                if (isInFinalScene) {
                    totalDialogueInScene = sceneInfo.scene.dialogue.length;
                    currentDialogueCount = 0;
                }
            });

            this.shmotime.on('system:speakComplete', () => {
                if (isInFinalScene) {
                    currentDialogueCount++;
                    if (currentDialogueCount === totalDialogueInScene) {
                        // This is fired after the final line of dialogue in the final scene has finished speaking
                        console.log('Episode completed!');
                        this.handleEpisodeComplete();
                    }
                }
            });
        }



        this.shmotime.on('manager:pauseRequest', () => {
            if (this.voiceSystem === 'browser' && this.synth.speaking) {
                this.synth.cancel();
            } else if (this.voiceSystem === 'elevenlabs') {
                this.stopCurrentAudio();
            }
        });

        this.shmotime.on('manager:stationCleared', () => {
            if (this.voiceSystem === 'browser' && this.synth.speaking) {
                this.synth.cancel();
            } else if (this.voiceSystem === 'elevenlabs') {
                this.stopCurrentAudio();
            }
        });
    }

    initialize() {
        if (this.isReady) {
            if (this.voiceSystem === 'browser') {
                this.shmotime.emit('speaker:ready', {
                    availableVoices: this.voices.map(voice => ({
                        name: voice.name,
                        lang: voice.lang,
                        isLocal: voice.localService
                    }))
                });
            } else {
                this.shmotime.emit('speaker:ready', {
                    availableVoices: this.elevenLabsVoices
                });
            }
        }
    }

    stopCurrentAudio() {
        if (this.currentAudio) {
            this.currentAudio.pause();
            this.currentAudio = null;
        }
    }

    destroy() {
        this.emit('destroy');
        if (this.voiceSystem === 'browser' && this.synth.speaking) {
            this.synth.cancel();
        } else if (this.voiceSystem === 'elevenlabs') {
            this.stopCurrentAudio();
        }
        this.removeEventListeners();
        this.isReady = false;
        this.voices = [];
        this.voicesByName.clear();
        this.elevenLabsVoices = [];
        this.currentAudio = null;
    }
}