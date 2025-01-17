class ShmotimeGenerator extends ShmotimeEventEmitter {
    constructor({ useMemory = 2, creativeWriting = false, archiveMode = true, plotTwist = false } = {}) {
        super();
        this.initialPrompt = null;
        this.showPrompts = new Map();
        this.retryCount = 0;
        this.currentShowData = null;
        this.useMemory = useMemory;
        this.creativeWriting = creativeWriting;
        this.archiveMode = archiveMode;
        this.plotTwist = plotTwist;
        this.episodeHistory = new Map();
        this.conversationHistory = new Map();
        this.MAX_MEMORY_EPISODES = 24;
        this.showArchives = new Map();
        this.originalPrompts = new Map(); // Store original prompts before processing
    }

    async fetchExternalData(url) {
        try {
            console.log('Fetching JSON info from:', url);

            // Add cache buster to URL
            const urlObj = new URL(url);
            urlObj.searchParams.append('cb', Date.now().toString());
            
            const response = await fetch(urlObj.toString());
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }
            const data = await response.json();
            console.log(data);
            return JSON.stringify(data);
        } catch (error) {
            console.error('Error fetching external data:', error);
            throw new Error(`Failed to fetch external data: ${error.message}`);
        }
    }

    async processShortcodes(prompt) {
        const shortcodeRegex = /\[externalData src='([^"]+)'\]/g;
        let processedPrompt = prompt;
        const matches = [...prompt.matchAll(shortcodeRegex)];

        for (const match of matches) {
            const [fullMatch, url] = match;
            try {
                const externalData = await this.fetchExternalData(url);
                processedPrompt = processedPrompt.replace(fullMatch, externalData);
            } catch (error) {
                processedPrompt = processedPrompt.replace(
                    fullMatch, 
                    `[Error loading external data: ${error.message}]`
                );
            }
        }

        return processedPrompt;
    }

    getRetryDelay() {
        return Math.min(1000 * Math.pow(2, this.retryCount), 32000);
    }

    addEpisodeToHistory(showId, episode) {
        if (!this.episodeHistory.has(showId)) {
            this.episodeHistory.set(showId, []);
        }
        const history = this.episodeHistory.get(showId);
        history.push(episode);
        
        if (history.length > this.MAX_MEMORY_EPISODES) {
            history.shift();
        }
    }

    addConversationMessage(showId, message) {
        // Only store conversation history if we're using full memory mode
        if (this.useMemory === 2) {
            if (!this.conversationHistory.has(showId)) {
                this.conversationHistory.set(showId, []);
            }
            const history = this.conversationHistory.get(showId);
            history.push(message);
            
            // Keep conversation history in sync with MAX_MEMORY_EPISODES
            if (history.length > this.MAX_MEMORY_EPISODES * 2) { // *2 because each episode has user+assistant messages
                history.splice(0, 2); // Remove oldest user+assistant pair
            }
        }
    }

    getEpisodeHistory(showId) {
        return this.episodeHistory.get(showId) || [];
    }

    initializeShowArchive(showData) {
        const showId = showData.showConfig.id;
        if (!this.showArchives.has(showId)) {
            if (showData.episodes) {
                this.showArchives.set(showId, {
                    config: {
                        ...showData.showConfig,
                        prompts: {
                            ...showData.showConfig.prompts,
                            episode: this.originalPrompts.get(showId) || showData.showConfig.prompts.episode
                        }
                    },
                    episodes: JSON.parse(JSON.stringify(showData.episodes))
                });
            } else {
                this.showArchives.set(showId, {
                    config: {
                        ...showData.showConfig,
                        prompts: {
                            ...showData.showConfig.prompts,
                            episode: this.originalPrompts.get(showId) || showData.showConfig.prompts.episode
                        }
                    },
                    episodes: []
                });
            }
        } else if (showData.episodes) {
            const archive = this.showArchives.get(showId);
            archive.episodes = JSON.parse(JSON.stringify(showData.episodes));
            archive.config.prompts.episode = this.originalPrompts.get(showId) || archive.config.prompts.episode;
        }
    }

    addEpisodeToArchive(showId, episode) {
        const archive = this.showArchives.get(showId);
        if (archive) {
            const existingIndex = archive.episodes.findIndex(ep => ep.id === episode.id);
            if (existingIndex >= 0) {
                archive.episodes[existingIndex] = episode;
            } else {
                archive.episodes.push(episode);
            }
            archive.episodes.sort((a, b) => a.id - b.id);
        }
    }

    downloadShowArchive(showId) {
        const archive = this.showArchives.get(showId);
        if (!archive) return;

        // Create a modified version of the archive with the original prompt
        const archiveWithOriginalPrompt = {
            ...archive,
            config: {
                ...archive.config,
                prompts: {
                    ...archive.config.prompts,
                    episode: this.originalPrompts.get(showId) || archive.config.prompts.episode
                }
            }
        };

        const timestamp = new Date().toISOString().replace(/[:.]/g, '-');
        const fileName = `${showId}-archive-${timestamp}.json`;
        
        const blob = new Blob(
            [JSON.stringify(archiveWithOriginalPrompt, null, 2)], 
            { type: 'application/json' }
        );
        
        const link = document.createElement('a');
        link.href = URL.createObjectURL(blob);
        link.download = fileName;
        
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        URL.revokeObjectURL(link.href);
    }

    async generateEpisode(showData) {
        this.initializeShowArchive(showData);
        this.currentShowData = showData;
        const config = showData.showConfig;
        const pilot = config.pilot;
        const showId = config.id;

        this.shmotime.emit('generator:starting', config);

        // Store the original prompt before processing
        const originalPrompt = config.prompts.episode;
        this.originalPrompts.set(showId, originalPrompt);

        // Process the episode prompt for external data
        try {
            console.log("processing shortcodes...");
            const processedPrompt = await this.processShortcodes(originalPrompt);
            config.prompts.episode = processedPrompt;
        } catch (error) {
            console.warn('Error processing external data:', error);
            // Continue with original prompt if external data processing fails
        }

        let plotSummary = null;

        if (typeof window.showPlotGenerator === 'function') {
            plotSummary = await showPlotGenerator(showId, config.name);
            if (plotSummary === '') {
                plotSummary = null;
            } else {
                this.shmotime.emit('generator:plotTwisted', plotSummary);
            }
        }

        if (this.creativeWriting) {
            try {
                plotSummary = await this.generatePlotSummary(showData, plotSummary).catch(console.warn);
                if (plotSummary) {
                    this.shmotime.emit('generator:plotGenerated', plotSummary);
                }
            } catch (error) {
                console.warn('Plot generation failed:', error);
            }
        }

        let messages = [];
        messages.push({
            role: "user",
            content: `${config.prompts.episode}\nI will send CONFIG that includes a PILOT EXAMPLE (for you to see how to name attributes in episodes) next.`
        });

        messages.push({
            role: "user",
            content: `CONFIG: ${JSON.stringify(config)}`
        });

        // Rest of the method remains unchanged...
        if (this.useMemory === 2) {
            console.log(showData);
            if (showData.episodes && showData.episodes.length > 0) {
                showData.episodes.forEach((episode, index) => {
                    if (index > 0) {
                        messages.push({
                            role: "user",
                            content: "Generate the next episode in the series."
                        });
                    }
                    messages.push({
                        role: "assistant",
                        content: JSON.stringify(episode)
                    });
                });
            } else {
                const history = this.conversationHistory.get(showId) || [];
                messages.push(...history);
            }
        } else if (this.useMemory === 1) {
            const episodes = showData.episodes || this.getEpisodeHistory(showId);
            if (episodes.length > 0) {
                messages.push({
                    role: "user",
                    content: `PREVIOUS EPISODES: ${episodes.map(ep => ep.summary).join('\n\nEPISODE: ')}`
                });
            }
        } else if (this.useMemory === 0) {
            const episodes = showData.episodes || this.getEpisodeHistory(showId);
            if (episodes.length > 0) {
                const lastEpisode = episodes[episodes.length - 1];
                messages.push({
                    role: "user",
                    content: `PREVIOUS EPISODE: ${lastEpisode.summary}`
                });
            }
        }

        messages.push({
            role: "user",
            content: plotSummary 
                ? `Generate the next episode in the series using this guidance: ${plotSummary}\n`
                : "Generate the next episode in the series."
        });

        return new Promise(async (resolve, reject) => {
            try {
                console.log('Generating', showId, messages);
                await this.makeGenerationRequest(messages, showId, resolve, reject);
            } catch (error) {
                reject(error);
            }
        });
    }

    async generatePlotSummary(showData, plotTwist = null) {
        const config = showData.showConfig;
        const pilot = config.pilot;
        const showId = config.id;
        const episodePrompt = config.prompts.episode;

        const plotMessages = [];

        // Initial expert writer prompt
        plotMessages.push({
            role: "user",
            content: `You are an expert television show writer with decades of experience crafting engaging and original episode plots. Create a one-paragraph plot summary that provides clear direction while leaving room for creative scene development. Focus on the core conflict, character dynamics, and story arc.${
                plotTwist ? `\n\nPLOT TWIST: ${plotTwist}` : ''
            }\n\nI will send CONFIG and PILOT next.`
        });

        // Show config and pilot
        plotMessages.push({
            role: "user",
            content: `CONFIG: ${JSON.stringify(config)}`
        });

        plotMessages.push({
            role: "user",
            content: `PILOT: ${JSON.stringify(pilot)}`
        });

        // Add episode history for context if available
        const episodes = this.getEpisodeHistory(showId);
        if (episodes.length > 0) {
            plotMessages.push({
                role: "user",
                content: `PREVIOUS EPISODES: ${episodes.map(ep => ep.summary).join('\n\nEPISODE: ')}\n\nBased on this history, generate a fresh plot that builds on previous events while avoiding repetition.`
            });
        }

        try {
            const response = await fetch(window.claudeApiUrl, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    model: "claude-3-5-sonnet-20241022",
                    messages: plotMessages,
                    max_tokens: 1024
                })
            });

            const data = await response.json();
            
            if (!response.ok || data.type === 'error') {
                throw new Error(data.error?.message || 'Plot generation failed');
            }

            if (data.content && data.content[0]?.type === 'text') {
                return data.content[0].text;
            }

            throw new Error('Invalid plot summary response format');
        } catch (error) {
            throw new Error('Failed to generate plot summary: ' + error.message);
        }
    }

    downloadEpisodeJson(showId, episode) {
        const timestamp = new Date().toISOString().replace(/[:.]/g, '-');
        const fileName = `${showId}-episode-${timestamp}.json`;
        
        const blob = new Blob(
            [JSON.stringify(episode, null, 2)], 
            { type: 'application/json' }
        );
        
        const link = document.createElement('a');
        link.href = URL.createObjectURL(blob);
        link.download = fileName;
        
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        URL.revokeObjectURL(link.href);
    }

    async makeGenerationRequest(messages, showId, resolve, reject) {
        try {
            console.log('sending generation request...');
            const response = await fetch(window.claudeApiUrl, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    model: "claude-3-5-sonnet-20241022",
                    messages: messages,
                    max_tokens: 4096
                })
            });
            console.log('fetched.');
            
            const data = await response.json();

            if (data.type === 'error' && data.error?.type === 'overloaded_error') {
                const delay = this.getRetryDelay();
                this.retryCount++;
                this.shmotime.emit('generator:retrying', { delay });
                setTimeout(() => this.makeGenerationRequest(messages, showId, resolve, reject), delay);
                return;
            }

            if (!response.ok || data.type === 'error') {
                throw new Error(data.error?.message || 'Generation failed');
            }

            if (data.content && data.content[0]?.type === 'text') {
                try {
                    const episode = JSON.parse(data.content[0].text);

                    // Only store in conversation history if using full memory mode
                    this.addConversationMessage(showId, {
                        role: "assistant",
                        content: data.content[0].text
                    });

                    this.retryCount = 0;
                    this.lastEpisode = episode;
                    
                    this.addEpisodeToHistory(showId, episode);
                    this.addEpisodeToArchive(showId, episode);

                    if (this.archiveMode) {
                        this.downloadShowArchive(showId);
                    }

                    this.shmotime.emit('generator:episodeGenerated', episode);
                    resolve(episode);
                } catch (error) {
                    console.error('Parse error:', error, 'Response:', data.content[0].text);
                    reject(new Error('Failed to parse episode JSON: ' + error.message));
                }
            } else {
                reject(new Error('Invalid response format'));
            }
        } catch (error) {
            reject(new Error('Failed to generate episode: ' + error.message));
        }
    }

    clearMemory() {
        this.showPrompts.clear();
        this.episodeHistory.clear();
        this.conversationHistory.clear();
        this.showArchives.clear();
    }

    addEventListeners() {
        this.shmotime.on('system:initialize', () => {
            this.emit('generator:initialized');
        });
    }
}