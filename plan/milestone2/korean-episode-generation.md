# Korean Episode Generation System

## Issue Summary
Implement Korean language episode generation using translated scripts and Korean TTS voices to serve the Korean ElizaOS community.

## Problem Statement
The Korean ElizaOS community represents a significant portion of the global user base, but currently has no localized content. We need to generate Korean-language episodes using translated scripts and appropriate Korean TTS voices.

## Acceptance Criteria
- [ ] Generate Korean audio from translated episode scripts
- [ ] Use appropriate Korean TTS voices for each character
- [ ] Integrate with existing ShowRunner video generation system
- [ ] Maintain episode timing and pacing appropriate for Korean language
- [ ] Generate Korean episode videos with proper lip sync
- [ ] Output episodes with Korean metadata and descriptions

## Technical Requirements

### TTS Integration
- Support for high-quality Korean TTS voices
- Character voice mapping (assign specific Korean voices to Marc, Eliza, Shaw, etc.)
- Proper pronunciation of technical terms and names
- Natural speech rhythm and intonation for Korean

### Character Voice Mapping
Map existing characters to appropriate Korean voices:
- **Marc**: Male Korean voice with confident tone
- **Eliza**: Female Korean voice with friendly tone  
- **Shaw**: Male Korean voice with technical/professional tone
- **Spartan**: Male Korean voice with energetic tone
- **Pepo**: Male Korean voice with casual/cool tone

### Episode Generation Workflow
1. Receive translated Korean episode JSON
2. Generate Korean TTS audio for each dialogue line
3. Process through existing ShowRunner video generation
4. Generate Korean metadata (title, description, tags)
5. Output Korean episode video file

### Metadata Localization
- Translate episode titles to Korean
- Generate Korean descriptions with relevant context
- Use Korean tags for better discoverability
- Maintain consistent branding in Korean

## Implementation Approach

### TTS Service Options
Evaluate and integrate Korean TTS services:
- Cloud TTS providers with Korean language support
- On-premise TTS solutions if needed for consistency
- Voice quality and naturalness assessment

### ShowRunner Integration
- Leverage existing ShowRunner video generation pipeline
- Adapt timing calculations for Korean speech patterns
- Ensure lip sync compatibility with Korean audio
- Maintain visual consistency with English episodes

### Quality Assurance
- Test Korean pronunciation of technical terms
- Validate episode timing and pacing
- Ensure character voice consistency
- Review cultural appropriateness

## Dependencies
- Translation pipeline completion
- Korean TTS service access
- ShowRunner video generation system
- YouTube upload automation (from Milestone 1)

## Success Metrics
- Daily Korean episodes generated alongside English versions
- Korean audio quality comparable to English episodes
- Episode timing maintains good pacing
- Korean community engagement and feedback
- Zero technical issues with Korean character encoding

## Cultural Considerations
- Respect Korean communication styles and formality levels
- Adapt content context for Korean audience when appropriate
- Consider Korean social media and platform preferences
- Handle Korean-specific terminology correctly

## Technical Challenges
- Korean text encoding and display
- Speech timing differences between English and Korean
- Technical term pronunciation in Korean
- Character voice personality consistency across languages

## Nice to Have
- Multiple Korean voice options per character
- Regional Korean dialect support
- Korean community feedback integration
- Korean-specific visual elements or branding
- Integration with Korean social media platforms 