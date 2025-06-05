# Cross-Platform Distribution System

## Issue Summary  
Implement automated posting to Discord and Twitter after successful YouTube upload to maximize episode reach and engagement.

## Problem Statement
Currently, episode announcements are manual across platforms, leading to inconsistent promotion and delayed community notifications. Cross-platform posting should happen automatically after upload.

## Acceptance Criteria
- [ ] Auto-post to Discord via webhook after YouTube upload
- [ ] Auto-post to Twitter with episode link and summary
- [ ] Include episode thumbnail and key highlights

## Technical Requirements

### Discord Integration
- Use webhook for immediate posting
- Include rich embed with episode details
- Support custom messaging per episode type

```json
{
  "discord": {
    "webhook_url": "DISCORD_WEBHOOK_URL",
    "embeds": [{
      "title": "🤖 New AI News Episode!",
      "description": "Today's episode covers ElizaOS updates, market analysis with Spartan & Pepo, and ecosystem news.",
      "url": "https://youtube.com/watch?v=VIDEO_ID",
      "thumbnail": {"url": "THUMBNAIL_URL"},
      "fields": [
        {"name": "Duration", "value": "12:34", "inline": true},
        {"name": "Topics", "value": "AI Agents, Crypto Markets, GitHub Updates", "inline": true}
      ],
      "footer": {"text": "ElizaOS AI News"}
    }]
  }
}
```

### Twitter Integration  
- Use Twitter API v2 for posting
- Support media uploads (thumbnails) 
- Generate appropriate messaging based on episode content

### Message Templates
Support different templates for content types:

```json
{
  "templates": {
    "daily_episode": {
      "discord_title": "New AI News Episode - {date}",
      "twitter_text": "New AI News episode: {highlights}\n\nWatch: {url}"
    },
    "market_special": {
      "discord_title": "Market Analysis with Spartan & Pepo",
      "twitter_text": "Market analysis: {market_points}\n\nWatch: {url}"
    },
    "interview": {
      "discord_title": "Interview: {guest}",
      "twitter_text": "New interview with {guest} about {topic}\n\nWatch: {url}"
    }
  }
}
```

## Implementation Approach

### Posting Workflow
1. YouTube upload completes successfully
2. Generate platform-specific messages from episode content
3. Post to Discord via webhook
4. Post to Twitter with video link
5. Track and log posting results

*Implementation details can vary based on your preferred approach.*

### Error Handling
- Network timeouts and retries (3 attempts)
- Platform API rate limits
- Authentication failures
- Media upload failures
- Graceful degradation (continue if one platform fails)

## Dependencies
- YouTube upload system completion
- Discord webhook setup
- Twitter API v2 credentials
- Episode metadata generation system

## Success Metrics
- 100% episode announcements posted automatically
- Cross-platform posting within 5 minutes of upload
- Zero manual posting interventions
- Consistent messaging across platforms

## Configuration
Store platform settings in environment variables:

```bash
DISCORD_WEBHOOK_URL=https://discord.com/api/webhooks/...

```

## Nice to Have
- Platform-specific content optimization
- Engagement tracking and analytics
- Support for additional platforms (LinkedIn, Reddit)
- A/B testing for message templates
- Community reaction monitoring 