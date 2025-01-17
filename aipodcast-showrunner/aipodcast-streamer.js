import { initializeApp } from "https://www.gstatic.com/firebasejs/11.0.2/firebase-app.js";
import { getDatabase, ref, push, onChildAdded, remove, off, goOffline, get, set } from "https://www.gstatic.com/firebasejs/11.0.2/firebase-database.js";

export class ShmotimeStreamer {
    constructor({ testMode = false } = {}) {
        this.shmotime = window.shmotime;
        if (!window.firebaseConfig) {
            throw new Error('Firebase config not found in window.firebaseConfig');
        }
        
        this.testMode = testMode;  // turning on TEST mode makes it NOT wait for the prepareSceneComplete event
        
        const app = initializeApp(window.firebaseConfig);
        this.db = getDatabase(app);
        this.inputsRef = ref(this.db, 'eventStream/inputs');
        this.outputsRef = ref(this.db, 'eventStream/outputs');

        // Start periodic purge
        this.purgeInterval = setInterval(() => this.purgeOldEvents(), 30000);
    }

    start() {
        const runner = this.shmotime.runner;
        
        // Bind handlers
        this.handlePrepareScene = this.pushInput.bind(this, 'prepareScene');
        this.handleSpeak = this.pushInput.bind(this, 'speak');
        this.handleEpisodeGenerating = this.pushInput.bind(this, 'episodeGenerating');
        this.handleEpisodeGenerated = this.pushInput.bind(this, 'episodeGenerated');
        this.handleShowLoaded = async (data) => {
            //await this.clearEventStreams();
            //this.pushInput('showLoaded', data);
            this.setInput('showLoaded', data);
        };
        
        // Listen to runner events that go to the log
        runner.on('prepareScene', async (data) => {
            this.handlePrepareScene(data);
            
            if (this.testMode) {
                await new Promise(resolve => setTimeout(resolve, 3000));
                await this.pushOutput('prepareSceneComplete');
            }
        });
        
        runner.on('speak', async (data) => {
            this.handleSpeak(data);
            
            if (this.testMode && runner.ttsMode === 'external') {
                await new Promise(resolve => setTimeout(resolve, 3000));
                await this.pushOutput('speakComplete');
            }
        });
        
        runner.on('episodeGenerating', this.handleEpisodeGenerating);
        runner.on('episodeGenerated', this.handleEpisodeGenerated);
        runner.on('showLoaded', this.handleShowLoaded);
        
        // Start listening to Firebase outputs
        this.setupOutputListener();
    }
    
    async clearEventStreams() {
        // Clear both inputs and outputs
        await remove(this.inputsRef);
        await remove(this.outputsRef);
        console.log('Cleared Firebase event streams');
    }

    async purgeOldEvents() {
        const sixSecondsAgo = Date.now() - 8000;
        
        // Get all input events
        const snapshot = await get(this.inputsRef);
        if (snapshot.exists()) {
            const events = snapshot.val();
            
            // Check each event
            Object.entries(events).forEach(([key, event]) => {
                if (event.timestamp < sixSecondsAgo) {
                    remove(ref(this.db, `eventStream/inputs/${key}`));
                }
            });
        }
    }
    
    setInput(type, data_in) {
        console.log('SETTING DATA', type, data_in);

        let data = data_in;
        if (type === 'showLoaded') {
            data = {
                id: data_in.showConfig.id,
                actors: {},
                locations: {}
            };
            for (let actorId in data_in.showConfig.actors) {
                data.actors[actorId] = true;
            }
            for (let locationId in data_in.showConfig.locations) {
                data.locations[locationId] = true;
            }
        }

        const timestamp = Date.now();
        const newKey = push(this.inputsRef).key;
        
        // Set the entire inputs node with just this one child
        return set(this.inputsRef, {
            [newKey]: {
                timestamp,
                type,
                data
            }
        });
    }

    pushInput(type, data_in) {
        console.log('PUSHING DATA', type, data_in);

        let data = data_in;
        if( type === 'episodeGenerated' ) {
            data = true;
        }
        else if( type === 'prepareScene' ) {
            data = {
                location: data_in.scene.location,
                in: data_in.scene.in,
                out: data_in.scene.out,
                cast: data_in.scene.cast
            };
        }
        else if( type === 'speak' ) {
            data = {
                actor: data_in.dialogue.actor,
                line: data_in.dialogue.line ?? 'Excelsior!',
                action: data_in.dialogue.action ?? 'normal'
            };
        }
        
        const timestamp = Date.now();
console.log(this.inputsRef, timestamp, type, data);
        push(this.inputsRef, {
            timestamp,
            type,
            data
        });
    }

    pushOutput(type) {
        const timestamp = Date.now();
        const outputEvent = {
            timestamp,
            type
        };
        console.log('Pushing output event:', outputEvent);
        return push(this.outputsRef, outputEvent);
    }
    
    setupOutputListener() {
        onChildAdded(this.outputsRef, (snapshot) => {
            const event = snapshot.val();
            const runner = this.shmotime.runner;
            
            if (event.type === 'prepareSceneComplete') {
                runner.emit('prepareSceneComplete');
            }
            
            if (event.type === 'speakComplete' && runner.ttsMode === 'external') {
                runner.emit('speakComplete');
            }
            
            remove(snapshot.ref);
        });
    }
    
    destroy() {
        const runner = this.shmotime.runner;
        
        // Clear interval
        if (this.purgeInterval) {
            clearInterval(this.purgeInterval);
        }
        
        // Remove runner event listeners
        runner.off('prepareScene', this.handlePrepareScene);
        runner.off('speak', this.handleSpeak);
        runner.off('episodeGenerating', this.handleEpisodeGenerating);
        runner.off('episodeGenerated', this.handleEpisodeGenerated);
        runner.off('showLoaded', this.handleShowLoaded);
        
        // Remove Firebase listeners
        off(this.outputsRef);
        
        // Close Firebase connection
        goOffline(this.db);
    }
}