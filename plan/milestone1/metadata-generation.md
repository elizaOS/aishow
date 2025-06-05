# Episode Metadata Generation System

## Issue Summary
Implement automated generation of YouTube metadata (titles, descriptions, tags, thumbnails) from episode content and [ai-news data](https://github.com/m3-org/ai-news) to eliminate manual metadata creation.

## Problem Statement
Currently, episode metadata is manually created, leading to inconsistent formatting, missing information, and publishing delays. Metadata should be automatically generated from episode content and news sources.

## Acceptance Criteria
- [ ] Generate YouTube titles from episode content
- [ ] Create detailed descriptions with timestamps and source links
- [ ] Auto-generate relevant tags based on episode topics
- [ ] Create or select appropriate thumbnails
- [ ] Support different metadata templates for content types
- [ ] Integrate with existing [ai-news JSON data](https://m3-org.github.io/ai-news/)

## Technical Requirements

### Data Sources Integration
Pull metadata from multiple sources:
- **ShowRunner episode JSON**: Scene content, dialogue, actors
- **AI News Daily JSON**: `https://elizaos.github.io/knowledge/ai-news/elizaos/json/daily.json`
- **Episode completion events**: Duration, generated timestamp
- **Show config**: Actor descriptions, show information

### Metadata Schema
Generate structured metadata matching YouTube upload requirements:

```json
{
  "episode_metadata": {
    "video_file": "recordings/ai-news-2025-06-15.mp4",
    "title": "AI News Daily: ElizaOS Updates & Market Analysis",
    "description": "Recorded: 2025-06-15\n\nToday's episode covers ElizaOS development updates, market analysis, and ecosystem news.\n\n[Generated description with key highlights and timestamps]\n\nSources: [Links to relevant content]",
    "tags": "AI News,ElizaOS,AI Agents,Development,Technology",
    "category_id": "22",
    "privacy_status": "public", 
    "thumbnail_file": "media/thumbnails/ai-news-2025-06-15.jpg",
    "duration": "12:34",
    "generated_at": "2025-06-15T10:30:00Z"
  }
}
```

### Title Generation
Generate titles based on episode content and type:

- **Daily episodes**: Include date and main topics
- **Market focus**: Highlight market analysis content  
- **Interview episodes**: Feature guest name and topic
- **Development updates**: Emphasize technical content

*Title format and style can be customized to match your brand.*

### Description Generation
Generate descriptions that include:
- Episode date and key highlights
- Relevant links and sources
- Appropriate formatting for readability

### Tag Generation
Create relevant tags based on episode content and topics covered.

### Thumbnail Management
Handle thumbnail selection or generation based on your existing thumbnail workflow.

## Implementation Details

### Content Analysis Pipeline
1. Parse episode JSON for dialogue and topics
2. Extract key themes using LLM analysis
3. Identify main speakers and segments
4. Pull additional context from ai-news data
5. Generate metadata using templates
6. Validate and format for YouTube API

### LLM Integration for Analysis
Use OpenRouter/Claude for:
- Content summarization
- Topic extraction  
- Title generation
- Description formatting
- Tag suggestion

### Sample Script Integration
Build on existing metadata structure (referenced in your comment):

```javascript
// Sample episode analysis
const episodeAnalysis = {
  mainTopics: ["ElizaOS v2.0", "Market Rally", "Community Growth"],
  speakers: ["marc", "eliza", "shaw", "sparty", "pepo"],
  segments: [
    {type: "intro", duration: "0:45", topics: ["daily_overview"]},
    {type: "market", duration: "2:35", topics: ["solana", "crypto", "trading"]},
    {type: "development", duration: "3:20", topics: ["github", "releases", "community"]},
    {type: "ecosystem", duration: "4:15", topics: ["partners", "dao", "interviews"]}
  ],
  sources: ["github.com/elizaOS/eliza", "ai-news daily data"]
};
```

## Dependencies
- Episode JSON completion events from ShowRunner
- Access to [ai-news daily JSON data](https://github.com/m3-org/ai-news)
- LLM API access for content analysis (OpenRouter)
- YouTube upload system (to receive the metadata)

## Success Metrics
- 100% episodes have complete, accurate metadata
- Zero manual metadata editing required
- Consistent formatting across all episodes
- Improved discoverability through better tags/descriptions

## Error Handling
- Missing or incomplete episode data
- AI-news data unavailable
- LLM API failures (fallback to template-based generation)
- Invalid metadata format detection

## Nice to Have
- A/B testing for title effectiveness
- Sentiment analysis for description tone
- Automatic highlight detection and timestamping
- SEO optimization for YouTube algorithm
- Multi-language metadata for international versions 