## AI Podcast Show Runner
![showrunnershot](https://hackmd.io/_uploads/SykKFhT8kl.jpg)

This is the static HTML local web app that loads a show, generates a new episode (or loads an existing episode), and plays back the show while firing the needed events to drive other components.  It also provides an ElevenLabs speaker component for TTS.

## Usage
![showrunnershot2](https://hackmd.io/_uploads/ByLDt2TIkl.jpg)

This app must be ran on something like a loopback server for CORS-compatibility. Prepros was the loopback server that was used during development.

The current version of the AI Podcast show's news aggregator data is automatically pulled into the prompt when a new episode is being generated.

The current news aggregator updates once-per-day at 5PM Eastern.

To run an episode, the HUMAN loads the Unity frontend application and gets it ready to run a simulation, and then runs this show runner.

They record what happens in the Unity app into a local video file, and then manually post that file to YouTube.

After an episode is generated it is saved into the show config's JSON that gets downloaded to provide consistent episode history. You need to copy the new show config JSON it downloads & replace the old "show-config.json" file with the new one before generating the next episode the next day - this way it gets the correct episode history (which helps it not repeat the same jokes each episode, etc.).

ElevenLabs TTS that was generated for that episode also gets downloaded in a ZIP after an episode concludes. If you'd like those cached versions to be used in replays you must unzip it "here" next to the show_runner.html file.

This implementation of a show runner currently communicates w/ the Unity frontend through a Firebase Realtime Database with a shared event stream.

## Firebase Realtime Database
The use of a Firebase RTD for the interface between the locally ran web app & the locally ran Unity application is a temporary solution - and thus does not have security implemented onto it.  **It is recommended you implement a LOCAL interface to avoid the need for a Firebase RTD at all. But if you use the Firebase RTD implementation, you muse keep your Firebase config & database URL a secret.**

You must setup your Firebase RTD w/ no security rules.  And put your Firebase config that you export from your dashboard, and the URL to your database, into the appropriate fields of the config.js file in this repo.  You must also use the SAME database URL in your Unity frontend setup.

## Event Stream
The event stream is a JSON structure that the Unity frontend polls about once a second (or however fast network conditions allow.)
The Unity frontend reads from the "inputs" attribute, and writes to the "outputs" attribute.
An example of an event stream is in EXAMPLE_EVENT_STREAM_JSON_FROM_FETCH.json.

### Structure
```
{
  "eventStream": {
    "inputs": {
      UNIQUE_INPUT_KEY_NAME: {
        "type": EVENT_NAME,
        "timestamp": EVENT_TIMESTAMP,
        "data": {...}
      }
    },
    "outputs": {
        UNIQUE_OUTPUT_KEY_NAME: {
            "type": EVENT_NAME
        }
    }
  }
}
```

### Input Examples
#### showLoaded
```
{
  "actors": {
    "eliza": true,
    "marc": true,
    "vitalik": true
  },
  "id": "aipodcast",
  "locations": {
    "podcast_desk": true
  }
}
```
#### speak
```
{
  "action": "enthusiastic",
  "actor": "marc",
  "line": "Welcome back to AI Tech Today! I'm Marc, alongside my co-host Eliza."
}
```

### Output Example
#### prepareSceneComplete
```
{
    // No data is included with this event.
}
```

## Config
There is a config.js file that you must define sensitive information in such as your Firebase config and insecure AI endpoint URLs.
This file is not present in this repo - you must create it yourself and place it in the folder next to show_runner.html.

The config.js file must have this structure:
```
window.firebaseConfig = {...};
window.apiBaseUrl = '';
window.claudeApiUrl = '';
window.firebaseUrl = '';
```

The apiBaseUrl and claudeApiUrl require you setup an endpoint w/ AI integration. For example code on how to setup such endpoints for the show runner, see the documentation here: https://hackmd.io/@smsithlord/aipodcast

## License
This show runner (show_runner.html, aipodcast-streamer.js, aipodcast-runner.js) were created for the AI Podcast project & are distributed under that project's license.

The contents of the "thirdparty" and "shmotime" folders are licensed under the MIT licenses included in their folders.