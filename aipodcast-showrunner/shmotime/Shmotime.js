/* Hey Claude - this example document for the Shmotime class is to show you the structure & explain the improvements we need. */
/* Please have a comprehensive approach when structuring this - our main class. */
class Shmotime extends ShmotimeEventEmitter {
    constructor() {
        super();

        /* Managing the state is important. We need to emit a "system:stateChanged" event whenever it changes that includes the current & previous states. */
        this.state = 'uninitialized';   // uninitialized | preload | preloaded | running
        this.station = null;
    }

    /* This is the method that the page calls on us when it's time to begin loading things. */
    start({/* constructor attributes here, such as a main config, and instances of components */ config, components} = {}) {
        this.verbose = 4;   /* this option should be passed in with the config intead of being hardcoded. */
        // 0: no output
        // 1: +system events (excluding system:stateChange)
        // 2: +system:stateChange system events
        // 3: +all events
        // 4: +other extra debug output
        // 5: manager events only

        if (this.verbose > 0) {
            console.debug(`[${this.constructor.name}]`, `Starting v0.0.0`);
        }

        /* Now handle ALL components we are given & execute their start function... */
        if (components) {
            for (const [componentName, componentInstance] of Object.entries(components)) {
                componentInstance.shmotime = this;

                if (typeof componentInstance.start === 'function') {
                    if (this.verbose > 3) {
                        console.debug(`[${this.constructor.name}]`, `Component Started: ${componentInstance.constructor.name}`);
                    }
                    componentInstance.start();
                }

                this[componentName] = componentInstance;
            }
        }

        this.UTIL_OnDOMContentLoaded(()=> {    /* This UTIL function will check document.readyState === "loading" & either attach a DOMContentLoaded listener or immediately execute the callback depending on the state. */            
            if (components) {   /* We need to be the first ones to add our listener so the UI component will be able to do preDOMContentLoaded stuff. */
                for (const [componentName, componentInstance] of Object.entries(components)) {
                    if (typeof componentInstance.preDOMContentLoaded === 'function') {
                        componentInstance.preDOMContentLoaded();
                    }
                }
            }
        });

        /* some other stuff goes here (usually) */

        /* create & attach event listeners w/ bookeeping for us to clean-up in our destroy handler */
        this.addEventListeners();  /* Note that only event listeners that do **not** require existing DOM objects should init inside of addEventListeners. */

        /* We are going to use our own UTIL_OnDOMContentLoaded method to inject any CSS or DOM elements we may need. */
        this.UTIL_OnDOMContentLoaded(()=> {    /* This UTIL function will check document.readyState === "loading" & either attach a DOMContentLoaded listener or immediately execute the callback depending on the state. */
            /* Then, if we have UI elements, add our DOM stuff. */
            this.injectCSS();   /* CSS & DOM always need Shmotime prefixes on IDs and classnames to avoid conflicts. Bookeep to clean-up later. */
            this.injectDOM();   /* Inside of injectDOM is where any DOM-dependent event listeners get setup for bookeeping for clean-up later. */

            /* We're ready to preload. */
            this.preload();
        });
    }

    UTIL_OnDOMContentLoaded(callback, scope = null) {
        function scopedCallback() {
            // Remove the event listener after it executes
            document.removeEventListener("DOMContentLoaded", scopedCallback);

            // Call the callback with the correct scope
            callback.call(scope);
        }

        if (document.readyState === "loading") {
            // DOM is still loading, wait for it to complete
            document.addEventListener("DOMContentLoaded", scopedCallback);
        } else {
            // DOM is already ready, execute the callback immediately
            callback.call(scope);
        }
    }


    initialize() {
        /* Anything we want to initialize for ourselves, we do first. */
        this.setState('running', 'starting-up');

        /* And then we emit the event for components to use. */
        this.emit('system:initialize');
    }

    preload() {
        /* If we end up having anything to preload, we'll do it in this preload method. Wait on our main class stuff first. */

        /* Then, handle the components. */
        /* We emit a "system:preload" event, and any components that care to will respond by calling our "componentPreloading" method w/ a Promise that they will resolve when they are done preloading. */
        this.setState('preload', 'starting-up');

        // Create an array to store component preload promises
        const preloadPromises = [];
        
        // Create a method for components to register their preload promises
        this.componentPreloading = (promise) => {
            if (promise instanceof Promise) {
                preloadPromises.push(promise);
            }
        };

        this.emit('system:preload');
        
        // Wait for all promises to resolve
        Promise.all(preloadPromises)
            .then(() => {
                /* We're done preloading. */
                this.setState('preloaded', 'starting-up');
                this.emit('system:preloaded');

                /* We'll just auto-init after preloading completes, for now. */
                this.initialize();
            })
            .catch(error => {
                this.emit('system:error', { 
                    phase: 'preload',
                    error: error
                });
            });
    }

    setState(nextState, reason) {
        /* First, we'd do any special internal handling (because we are the main class.) */
        const oldState = this.state;

        this.state = nextState;

        /* And then we'd fire out the event for components to know. */
        this.emit('system:stateChange', {currentState: this.state, previousState: oldState, reason});
    }

    destroy() {
        // Emit the 'destroy' event
        this.emit('destroy');

        /* Clear all pending timeouts or intervals we created during our lifetime. */
        /* Remove all event listeners we created during our lifetime. */
        this.removeEventListeners()
        /* Clean-up everything that we created during our lifetime. */

        /* If we had UI elements, remove any DOM elements (including CSS style) that we added to the page. */
    }

    injectCSS() {
        const cssString = `
            /* CSS goes in here */
        `;

        const style = document.createElement('style');
        style.textContent = cssString;
        document.head.appendChild(style);
    }

    injectDOM() {
        /* nothing to inject here yet. */
    }

    showLoaded(showConfig, episodes) {
        //this.emit('system:showLoaded', {showConfig, episodes});
        console.log(showConfig);
        this.emit('system:showLoaded', {showConfig, episodes});
    }

    addEventListeners() {
        /* This is sometimes called before DOMContentLoaded. So only handle things that don't need the DOM in here. */
    }

    removeEventListeners() {
        /* This has its own method to keep things organized. */
    }
}

window.shmotime = new Shmotime();