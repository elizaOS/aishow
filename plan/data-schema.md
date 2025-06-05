# Episode Data Schema for AI News Automation

## Overview
This document outlines key data structures for YouTube publishing automation, building on existing ShowRunner episode formats. The ShowRunner system already handles AI news integration - this focuses on the additional metadata needed for automated publishing.

## Existing Data Structures (Current)

### ShowRunner Episode Format
*From `Assets/Resources/config-example/show-config.json`*

```json
{
  "config": {
    "id": "aipodcast",
    "name": "AI Podcast", 
    "description": "A tech news broadcast about the work being done on ai16z's GitHub repo.",
    "creator": "ElizaOS Daily Update",
    "actors": {
      "marc": {
        "name": "Marc",
        "gender": "male",
        "description": "AI Marc AIndreessen...",
        "voice": "Microsoft Guy Online (Natural) - English (United States)"
      }
      // ... other actors
    },
    "locations": {
      "podcast_desk": { /* location config */ },
      "stonks": { /* market analysis location */ },
      "splitscreen": { /* interview layout */ }
    }
  },
  "episodes": [
    {
      "id": "S1E_SPECIAL",
      "name": "Episode Title",
      "premise": "Episode premise",
      "summary": "Episode summary", 
      "scenes": [
        {
          "location": "podcast_desk",
          "description": "Scene description",
          "in": "fade",
          "out": "cut",
          "cast": {
            "anchor_seat": "marc",
            "coanchor_seat": "eliza"
          },
          "dialogue": [
            {
              "actor": "marc",
              "line": "Dialogue text",
              "action": "normal"
            }
          ]
        }
      ]
    }
  ]
}
```

### HedraEpisodeProcessor Data
*From `Assets/Scripts/Tools/HedraEpisodeProcessor.cs`*

```csharp
[System.Serializable]
public class JsonEpisode
{
    public string id;
    public string name;
    public string premise;
    public string summary;
    public List<JsonScene> scenes;
}

[System.Serializable] 
public class JsonScene
{
    public string location;
    public string description;
    public List<JsonDialogueEntry> dialogue;
}

[System.Serializable]
public class JsonDialogueEntry
{
    public string actor;
    public string line;
    public string action;
}
```

## Extended Data Schema for Automation

### Publishing Metadata Extension
*Additional data needed for automation (beyond existing ShowRunner episode format)*

The core episode data already exists in ShowRunner. For automation, we need:

```json
{
  "publishing_metadata": {
    // YouTube Upload Requirements
    "title": "Generated episode title",
    "description": "Generated description with highlights and links",
    "tags": "Relevant,Tags,For,Discovery",
    "category_id": "22",
    "privacy_status": "public",
    "thumbnail_file": "path/to/thumbnail.jpg",
    
    // Playlist Assignment
    "playlist_assignments": ["daily_episodes", "relevant_category"],
    
    // Cross-Platform Messages
    "discord_message": "Generated Discord announcement",
    "twitter_message": "Generated Twitter post",
    
    // Status Tracking
    "upload_status": {
      "youtube_uploaded": false,
      "discord_posted": false, 
      "twitter_posted": false,
      "playlists_updated": false
    },
    
    // Results (populated after upload)
    "results": {
      "youtube_video_id": null,
      "youtube_url": null,
      "upload_timestamp": null
    }
  }
}
```

### Template System for Content Types

```json
{
  "metadata_templates": {
    "daily_episode": {
      "title_format": "AI News Daily: {top_topic} & {secondary_topic}",
      "description_template": "Recorded: {date}\n\n🤖 Today's Episode Highlights:\n\n{timestamped_segments}\n\n🔗 Sources & Links:\n{source_links}\n\n#AINews #ElizaOS #AIAgents #Blockchain #Web3",
      "discord_title": "🤖 Daily AI News - {date}",
      "twitter_format": "🚀 New AI News episode is live! Today we cover:\n\n{highlights}\n\nWatch: {url}\n\n#AI #ElizaOS #Crypto"
    },
    "market_focus": {
      "title_format": "🚀 {market_highlights} | AI News Market Analysis",
      "description_template": "Recorded: {date}\n\n📈 MARKET ANALYSIS SPECIAL\n\n{market_data}\n\nWith @DegenSpartan and Pepo\n\n{source_links}",
      "discord_title": "📈 Market Analysis Special - {date}",
      "twitter_format": "📈 MARKET ANALYSIS ALERT! 🔥\n\n@DegenSpartan and Pepo break down:\n{market_points}\n\nWatch: {url}\n\n#Crypto #DeFi #Trading"
    },
    "interview_special": {
      "title_format": "🎙️ {guest_name}: {interview_topic}",
      "description_template": "Recorded: {date}\n\n🎙️ SPECIAL INTERVIEW\n\nGuest: {guest_name}\nTopic: {interview_topic}\n\n{interview_highlights}\n\n{source_links}",
      "discord_title": "🎙️ Ecosystem Interview - {guest_name}",
      "twitter_format": "🎙️ New interview with {guest_name}!\n\nDiving deep into {topic}\n\nWatch: {url}\n\n#ElizaOS #Interview #Web3"
    }
  }
}
```

## Integration Points

### Event System Integration
*From `wiki/core/EpisodeCompletionNotifier.md`*

```csharp
// Episode completion triggers metadata generation
public class EpisodeCompletionNotifier : MonoBehaviour
{
    public UnityEvent<EpisodeMetadata> OnEpisodeMetadataReady;
    public UnityEvent<PublishingResults> OnPublishingComplete;
}
```

### AI News Data Integration  
*From [ai-news pipeline](https://github.com/m3-org/ai-news)*

```json
{
  "daily_data": {
    "date": "2025-06-15",
    "categories": {
      "github_updates": [...],
      "discord_highlights": [...],
      "market_data": [...],
      "ecosystem_news": [...]
    },
    "generated_at": "2025-06-15T08:00:00Z"
  }
}
```

## Usage in Automation Pipeline

1. **Episode Generation**: ShowRunner creates episode JSON using existing format
2. **Completion Detection**: EpisodeCompletionNotifier triggers metadata generation
3. **Content Analysis**: LLM analyzes episode content and AI news data
4. **Metadata Generation**: Template system creates YouTube/social media content
5. **Publishing**: Automated upload and cross-platform distribution
6. **Status Tracking**: Update processing status throughout pipeline

This schema bridges the existing ShowRunner system with the new automation requirements while maintaining compatibility with current episode generation workflows. 