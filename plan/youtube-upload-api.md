# YouTube Data API v3 Integration

## Issue Summary
Implement automated video uploads to YouTube using the YouTube Data API v3 to eliminate the 2-week delay in publishing episodes.

## Problem Statement
Currently, episodes are manually uploaded to YouTube, causing significant delays (last episode is 2 weeks old). This breaks the daily news cycle and reduces community engagement.

## Acceptance Criteria
- [ ] Integrate YouTube Data API v3 for video uploads
- [ ] Support video file upload with metadata
- [ ] Handle API rate limits and error conditions
- [ ] Return video URL and metadata after successful upload
- [ ] Log upload status and any errors for debugging

## Technical Requirements

### Video Upload Requirements
Support the standard YouTube upload parameters:

- `video_file`: Path to the video file
- `title`: Generated episode title
- `description`: Generated episode description  
- `tags`: Relevant tags for discovery
- `category_id`: YouTube category (typically "22" for People & Blogs)
- `privacy_status`: "public", "private", or "unlisted"
- `thumbnail_file`: Custom thumbnail image (optional)

### API Integration Points
- Upload endpoint: `POST https://www.googleapis.com/upload/youtube/v3/videos`
- Metadata update: `PUT https://www.googleapis.com/youtube/v3/videos`
- Authentication: OAuth2 or Service Account

### Error Handling
- Network timeouts and retries
- API quota exceeded (rate limiting)
- Invalid video format/size
- Authentication failures
- Graceful degradation if upload fails

## Implementation Notes
- Use [YouTube Data API v3 Upload Guide](https://developers.google.com/youtube/v3/guides/uploading_a_video) 
- Integrate with existing ShowRunner episode completion events
- Store upload results for playlist management
- Consider chunked uploads for large video files
- Implementation approach can vary based on your tech stack preferences

## Dependencies
- YouTube Data API v3 credentials setup
- Integration with episode metadata generation system
- Video file location/naming convention

## Success Metrics
- Episodes uploaded within 1 hour of generation
- Zero manual upload interventions needed
- 100% upload success rate (with retries)

## Nice to Have
- Upload progress tracking
- Thumbnail auto-generation if not provided
- Video quality/format optimization before upload 