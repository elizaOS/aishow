class ShmotimeEventEmitter {
    constructor() {
        this.events = {};
    }

    on(event, listener, scope) {
        if (!this.events[event]) {
            this.events[event] = [];
        }
        this.events[event].push({ listener, scope, once: false });
    }

    once(event, listener, scope) {
        if (!this.events[event]) {
            this.events[event] = [];
        }
        this.events[event].push({ listener, scope, once: true });
    }

    off(event, listener) {
        if (!event) {
            // Remove all listeners for all events
            this.events = {};
            return;
        }
        
        if (!this.events[event]) return;
        
        if (!listener) {
            // Remove all listeners for this event
            delete this.events[event];
            return;
        }
        
        this.events[event] = this.events[event].filter(l => l.listener !== listener);
    }
    
    has(eventName) {
        return !!(this.events[eventName] && this.events[eventName].length > 0);
    }

    emit(event, ...args) {
            // If this instance is the Shmotime class itself and verbose is enabled
            if( event.indexOf('error') === event.length-5 ) {
                if (args.length > 0) {
                    console.error(`[${this.constructor.name}]`, args[0].error);
                } else {
                    console.error(`[${this.constructor.name}]`, `Event: ${event}`);
                }
            }
            else if( window.shmotime.verbose === 5 ) {
                if( event.indexOf('manager:') === 0 ) {
                    if (args.length > 0) {
                        console.warn(`[${this.constructor.name}]`, `Event: ${event}`, ...args);
                    } else {
                        console.warn(`[${this.constructor.name}]`, `Event: ${event}`);
                    }
                }
            }
            else if (
                (this.constructor.name === 'Shmotime' && ((event !== 'system:stateChange' && this.verbose > 0) || (event === 'system:stateChange' && this.verbose > 1))) ||
                (this.shmotime && this.shmotime.verbose > 2)
            ) {
                if (args.length > 0) {
                    console.warn(`[${this.constructor.name}]`, `Event: ${event}`, ...args);
                } else {
                    console.warn(`[${this.constructor.name}]`, `Event: ${event}`);
                }
            }
            if (!this.events[event]) return;
            
            const listeners = this.events[event].slice(); // Create a copy to avoid issues while removing listeners
            
            listeners.forEach(l => {
                l.listener.apply(l.scope, args);
                
                if (l.once) {
                    // Remove the listener if it was registered with 'once'
                    this.off(event, l.listener);
                }
            });
        }
    }