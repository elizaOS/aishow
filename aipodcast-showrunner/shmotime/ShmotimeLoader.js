class ShmotimeLoader extends ShmotimeEventEmitter {
    constructor({} = {}) {
        super();
        this.container = null;
        this.loadedData = {
            showConfig: null,
            episodes: []
        };
    }

    injectCSS() {
        const cssString = `
            .ShmotimeLoader-container {
                display: flex;
                gap: 20px;
                margin-bottom: 20px;
            }
            
            .ShmotimeLoader-dropZone {
                flex: 1;
                border: 2px dashed #ccc;
                border-radius: 8px;
                padding: 20px;
                text-align: center;
                transition: background-color 0.3s, border-color 0.3s;
                cursor: pointer;
            }
            
            .ShmotimeLoader-dropZone.dragOver {
                background-color: rgba(0, 150, 255, 0.1);
                border-color: #0096ff;
            }
            
            .ShmotimeLoader-dropZone.error {
                background-color: rgba(255, 0, 0, 0.1);
                border-color: #ff0000;
            }
        `;

        const style = document.createElement('style');
        style.textContent = cssString;
        document.head.appendChild(style);
        this.styleElement = style;
    }

    showManualUI() {
        this.injectCSS();
        this.injectDOM();
    }

    hideManualUI() {
        if (this.styleElement) {
            this.styleElement.remove();
            this.styleElement = null;
        }
        if (this.container) {
            this.container.remove();
            this.container = null;
        }
    }

    injectDOM() {
        this.container = document.createElement('div');
        this.container.className = 'ShmotimeLoader-container';

        const dropZone = document.createElement('div');
        dropZone.className = 'ShmotimeLoader-dropZone';
        dropZone.textContent = 'Drop show JSON file here';
        
        this.container.appendChild(dropZone);
        document.body.insertBefore(this.container, document.body.firstChild);
        
        dropZone.addEventListener('dragover', this.preventDefault.bind(this));
        dropZone.addEventListener('drop', this.handleDrop.bind(this));
        dropZone.addEventListener('click', this.handleClick.bind(this));
    }

    preventDefault(e) {
        e.preventDefault();
        e.stopPropagation();
    }

    async handleDrop(e) {
        this.preventDefault(e);
        const files = Array.from(e.dataTransfer.files);
        if (files.length > 0) {
            await this.handleFile(files[0]);
        }
    }

    handleClick() {
        const input = document.createElement('input');
        input.type = 'file';
        input.accept = '.json';
        input.addEventListener('change', async (e) => {
            const files = Array.from(e.target.files);
            if (files.length > 0) {
                await this.handleFile(files[0]);
            }
        });
        input.click();
    }

    async readFile(file) {
        return new Promise((resolve, reject) => {
            const reader = new FileReader();
            reader.onload = (e) => resolve(e.target.result);
            reader.onerror = (e) => reject(new Error('Failed to read file'));
            reader.readAsText(file);
        });
    }

    async loadShowFromJSON(jsonUrl) {
        try {
            const response = await fetch(jsonUrl);
            const showData = await response.json();
            this.emit('loader:showLoaded', showData);
            return showData;
        } catch (error) {
            this.emit('loader:error', { error });
            return null;
        }
    }

    async handleFile(file) {
        try {
            const content = await this.readFile(file);
            const showData = JSON.parse(content);
            this.emit('loader:showLoaded', showData);
        } catch (error) {
            console.error('Error loading show:', error);
            this.emit('loader:error', { error });
        }
    }

    loadShowFromJSONManual() {
        this.shmotime.UTIL_OnDOMContentLoaded(() => {
            this.showManualUI();
        }, this);
    }

    addEventListeners() {
        this.shmotime.on('system:initialize', this.initialize.bind(this));
    }

    removeEventListeners() {}
}