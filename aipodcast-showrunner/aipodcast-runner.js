class ShmotimeRunner extends ShmotimeEventEmitter {
    constructor(generator) {
        super();
        this.generator = generator;
        this.isPlaying = false;
        this.currentShow = null;
        this.currentEpisode = null;
        this.currentSceneIndex = 0;
        this.currentDialogueIndex = 0;
        this.currentEpisodeIndex = 0;
        
        // Add UI elements
        this.setupUI();
        this.ttsMode = 'browser';
        
        // Bind handlers
        this.handlePrepareScene = this.logInput.bind(this, 'prepareScene');
        this.handleSpeak = this.logInput.bind(this, 'speak');
        this.handlePrepareSceneComplete = this.logOutput.bind(this, 'prepareSceneComplete');
        this.handleSpeakComplete = this.logEvent.bind(this);
        this.handleEpisodeGenerating = this.logInput.bind(this, 'episodeGenerating');
        this.handleEpisodeGenerated = this.logInput.bind(this, 'episodeGenerated');
        this.handleShowLoaded = this.logInput.bind(this, 'showLoaded');
        
        // Add event listeners
        this.on('prepareScene', this.handlePrepareScene);
        this.on('speak', this.handleSpeak);
        this.on('prepareSceneComplete', this.handlePrepareSceneComplete);
        this.on('speakComplete', this.handleSpeakComplete);
        this.on('episodeGenerating', this.handleEpisodeGenerating);
        this.on('episodeGenerated', this.handleEpisodeGenerated);
        this.on('showLoaded', this.handleShowLoaded);
    }

    setupUI() {
        const template = `
            <div style="background: #1a1a1a; color: #fff; padding: 20px; font-family: monospace;">
                <select style="display: none; background: #333; color: #fff; padding: 5px; margin-bottom: 10px;">
                    <option value="browser">Web Browser</option>
                    <option value="external">External</option>
                </select>
                
                <div class="input-log" style="background: #2a2a2a; padding: 10px; margin: 10px 0; height: 200px; overflow-y: auto;">
                    <h3>INPUTS</h3>
                </div>
                
                <div class="output-log" style="background: #2a2a2a; padding: 10px; margin: 10px 0; height: 200px; overflow-y: auto;">
                    <h3>OUTPUTS</h3>
                </div>
            </div>
        `;

        const container = document.createElement('div');
        container.innerHTML = template.trim();
        document.body.appendChild(container.firstElementChild);
        
        const select = document.querySelector('select');
        select.addEventListener('change', (e) => {
            this.ttsMode = e.target.value;
        });
        
        this.inputLog = document.querySelector('.input-log');
        this.outputLog = document.querySelector('.output-log');
    }

    async generateAndPlayEpisode() {
        if (!this.currentShow) {
            console.error('No show loaded');
            return;
        }

        this.emit('episodeGenerating', { showName: this.currentShow.config.name });
        
        const shmotime = this.shmotime;
        this.currentEpisode = await shmotime.generator.generateEpisode({
            showConfig: this.currentShow.config
        });
        
        this.currentSceneIndex = 0;
        this.currentDialogueIndex = 0;
        
        this.emit('episodeGenerated', {
            showName: this.currentShow.config.name,
            episodeTitle: `(${this.currentEpisode.id}) ${this.currentEpisode.name}`,
            numScenes: this.currentEpisode.scenes.length
        });
        
        if (this.isPlaying) {
            await this.playScene();
        }
    }

    /*async playScene() {
        if (!this.isPlaying || !this.currentEpisode) return;

        const scene = this.currentEpisode.scenes[this.currentSceneIndex];
        if (!scene) {
            await this.generateAndPlayEpisode();
            return;
        }

        const sceneInfo = {
            showConfig: this.currentShow.config,
            episode: this.currentEpisode,
            scene: {
                ...scene,
                title: this.currentShow.config.locations[scene.location].name
            }
        };

        this.emit('prepareScene', sceneInfo);
        
        if (this.has('prepareScene')) {
            await new Promise(resolve => {
                const handler = () => {
                    this.off('prepareSceneComplete', handler);
                    resolve();
                };
                this.on('prepareSceneComplete', handler);
            });
        }

        while (this.isPlaying && this.currentDialogueIndex < scene.dialogue.length) {
            const dialogueEntry = scene.dialogue[this.currentDialogueIndex];
            const actorConfig = this.currentShow.config.actors[dialogueEntry.actor];
            const enhancedDialogue = {
                ...dialogueEntry,
                actorConfig
            };

            this.emit('speak', {
                showConfig: this.currentShow.config,
                dialogue: enhancedDialogue,
                episode: this.currentEpisode,
                scene: scene
            });

            this.shmotime.emit('manager:speak', {
                showConfig: this.currentShow.config,
                dialogue: enhancedDialogue,
                episode: this.currentEpisode,
                scene: scene
            });

            // Wait for speak completion if we have listeners
            if (this.shmotime.has('manager:speak')) {
                await new Promise(resolve => {
                    const handler = () => {
                        this.shmotime.off('system:speakComplete', handler);
                        resolve();
                    };
                    this.shmotime.on('system:speakComplete', handler);
                });
            }

            this.currentDialogueIndex++;
        }

        if (this.isPlaying && this.currentDialogueIndex >= scene.dialogue.length) {
            this.currentSceneIndex++;
            this.currentDialogueIndex = 0;
            await this.playScene();
        }
    }*/


    async playScene() {
        if (!this.isPlaying || !this.currentEpisode) return;

        const scene = this.currentEpisode.scenes[this.currentSceneIndex];
        if (!scene) {
            await this.playNextEpisode();
            return;
        }

        // Create sequence info for tracking progress
        const sequenceInfo = {
            sceneIndex: this.currentSceneIndex,
            totalScenes: this.currentEpisode.scenes.length,
            dialogueIndex: this.currentDialogueIndex,
            totalDialogueInScene: scene.dialogue.length
        };

        const sceneInfo = {
            showConfig: this.currentShow.config,
            episode: this.currentEpisode,
            scene: {
                ...scene,
                title: this.currentShow.config.locations[scene.location].name
            },
            sequenceInfo  // Add sequence info to the scene info
        };

        // Emit prepare scene with sequence info
        this.emit('prepareScene', sceneInfo);
        this.shmotime.emit('manager:prepareScene', sceneInfo);  // Add this line
        
        if (this.has('prepareScene')) {
            await new Promise(resolve => {
                const handler = () => {
                    this.off('prepareSceneComplete', handler);
                    resolve();
                };
                this.on('prepareSceneComplete', handler);
            });
        }

        while (this.isPlaying && this.currentDialogueIndex < scene.dialogue.length) {
            const dialogueEntry = scene.dialogue[this.currentDialogueIndex];
            const actorConfig = this.currentShow.config.actors[dialogueEntry.actor];
            const enhancedDialogue = {
                ...dialogueEntry,
                actorConfig
            };

            // Add sequence info to speak events
            const speakInfo = {
                showConfig: this.currentShow.config,
                dialogue: enhancedDialogue,
                episode: this.currentEpisode,
                scene: scene,
                sequenceInfo: {
                    ...sequenceInfo,
                    dialogueIndex: this.currentDialogueIndex
                }
            };

            this.emit('speak', speakInfo);
            this.shmotime.emit('manager:speak', speakInfo);

            // Wait for speak completion if we have listeners
            if (this.shmotime.has('manager:speak')) {
                await new Promise(resolve => {
                    const handler = () => {
                        this.shmotime.off('system:speakComplete', handler);
                        resolve();
                    };
                    this.shmotime.on('system:speakComplete', handler);
                });
            }

            this.currentDialogueIndex++;
        }

        if (this.isPlaying && this.currentDialogueIndex >= scene.dialogue.length) {
            const isLastScene = this.currentSceneIndex === this.currentEpisode.scenes.length - 1;
            const isLastDialogue = this.currentDialogueIndex >= scene.dialogue.length;
            
            if (isLastScene && isLastDialogue) {
                // Emit episode complete event
                this.shmotime.emit('episode:complete', {
                    showConfig: this.currentShow.config,
                    episode: this.currentEpisode
                });
            }

            this.currentSceneIndex++;
            this.currentDialogueIndex = 0;
            await this.playScene();
        }
    }

    logInput(type, data) {
        const timestamp = new Date().toLocaleTimeString();
        const message = document.createElement('div');
        message.style.color = '#4CAF50';
        
        switch(type) {
            case 'prepareScene':
                message.textContent = `${timestamp} - PrepareScene: ${data.scene.title}`;
                break;
            case 'speak':
                message.textContent = `${timestamp} - Speak: ${data.dialogue.actorConfig.name}: "${data.dialogue.line}"`;
                break;
            case 'episodeGenerating':
                message.textContent = `${timestamp} - EpisodeGenerating: ${data.showName}`;
                break;
            case 'episodeGenerated':
                message.textContent = `${timestamp} - EpisodeGenerated: "${data.episodeTitle}" (${data.numScenes} scenes)`;
                break;
            case 'showLoaded':
                message.textContent = `${timestamp} - ShowLoaded: "${data.showName}" (${data.showId})`;
                break;
        }
        
        this.inputLog.appendChild(message);
        this.inputLog.scrollTop = this.inputLog.scrollHeight;
    }

    logOutput(type) {
        const timestamp = new Date().toLocaleTimeString();
        const message = document.createElement('div');
        message.style.color = '#2196F3';
        message.textContent = `${timestamp} - ${type}`;
        this.outputLog.appendChild(message);
        this.outputLog.scrollTop = this.outputLog.scrollHeight;
    }

    logEvent(event) {
        if (event === 'speakComplete') {
            if (this.ttsMode === 'external') {
                this.logInput('speakComplete', { type: 'speakComplete' });
            } else {
                this.logOutput('speakComplete');
            }
        }
    }

    async loadShow(showData) {
        console.log('Loading show with episodes:', showData.episodes);
        this.currentShow = showData;
        this.currentEpisodeIndex = 0;
        this.emit('showLoaded', { 
            showConfig: showData.config,
            showName: showData.config.name,
            showId: showData.config.id,
            numEpisodes: showData.episodes?.length || 0
        });
        
        if (this.isPlaying) {
            await this.playNextEpisode();
        }
    }

    async playNextEpisode() {
        if (!this.currentShow) {
            console.error('No show loaded');
            return;
        }

        console.log('Playing next episode. Current show:', this.currentShow);
        console.log('Episodes array:', this.currentShow.episodes);
        console.log('Current episode index:', this.currentEpisodeIndex);

        // Check if we have episodes to play
        if (this.currentShow.episodes && this.currentShow.episodes.length > 0) {
            console.log('Found existing episodes, using episode', this.currentEpisodeIndex);
            
            // Get next episode from array, looping back to start if needed
            this.currentEpisodeIndex = this.currentEpisodeIndex % this.currentShow.episodes.length;
            this.currentEpisode = this.currentShow.episodes[this.currentEpisodeIndex];
            this.currentEpisodeIndex++;
            
            this.currentSceneIndex = 0;
            this.currentDialogueIndex = 0;
            
            console.log('Selected episode:', this.currentEpisode);
            
            this.emit('episodeGenerated', {
                showName: this.currentShow.config.name,
                episodeTitle: `(${this.currentEpisode.id}) ${this.currentEpisode.name}`,
                numScenes: this.currentEpisode.scenes.length,
                source: 'existing'
            });
        } else {
            console.log('No existing episodes found, generating new episode');
            // Fall back to generating if no episodes exist
            this.emit('episodeGenerating', { showName: this.currentShow.config.name });
            
            const shmotime = this.shmotime;
            this.currentEpisode = await shmotime.generator.generateEpisode({
                showConfig: this.currentShow.config
            });
            
            this.currentSceneIndex = 0;
            this.currentDialogueIndex = 0;
            
            this.emit('episodeGenerated', {
                showName: this.currentShow.config.name,
                episodeTitle: `(${this.currentEpisode.id}) ${this.currentEpisode.name}`,
                numScenes: this.currentEpisode.scenes.length,
                source: 'generated'
            });
        }
        
        if (this.isPlaying) {
            await this.playScene();
        }
    }

    async play() {
        if (this.isPlaying) return;
        
        this.isPlaying = true;
        this.emit('playStateChanged', this.isPlaying);
        
        if (!this.currentEpisode) {
            await this.playNextEpisode();
        } else {
            await this.playScene();
        }
    }

    pause() {
        if (!this.isPlaying) return;
        
        this.isPlaying = false;
        this.emit('playStateChanged', this.isPlaying);
    }

    destroy() {
        this.isPlaying = false;
        this.currentShow = null;
        this.currentEpisode = null;
        
        this.off('prepareScene', this.handlePrepareScene);
        this.off('speak', this.handleSpeak);
        this.off('prepareSceneComplete', this.handlePrepareSceneComplete);
        this.off('speakComplete', this.handleSpeakComplete);
        this.off('episodeGenerating', this.handleEpisodeGenerating);
        this.off('episodeGenerated', this.handleEpisodeGenerated);
        this.off('showLoaded', this.handleShowLoaded);
        
        document.body.removeChild(document.querySelector('div'));
        
        this.removeAllListeners();
    }
}