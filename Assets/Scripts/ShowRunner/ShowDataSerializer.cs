using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

namespace ShowRunner
{
    public static class ShowDataSerializer
    {
        public static ShowData LoadFromFile(string filePath)
        {
            try
            {
                string json = File.ReadAllText(filePath);
                return JsonConvert.DeserializeObject<ShowData>(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error loading show data from {filePath}: {e.Message}");
                return null;
            }
        }

        public static bool SaveToFile(ShowData showData, string filePath)
        {
            try
            {
                string json = JsonConvert.SerializeObject(showData, Formatting.Indented);
                File.WriteAllText(filePath, json);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error saving show data to {filePath}: {e.Message}");
                return false;
            }
        }

        public static ShowData CreateNewShow()
        {
            return new ShowData
            {
                Config = new ShowConfig
                {
                    id = "aipodcast",
                    name = "AI Podcast",
                    description = "A tech news broadcast about the work being done on ai16z's GitHub repo.",
                    creator = "ElizaOS Daily Update",
                    prompts = new Dictionary<string, string>
                    {
                        { "episode", "You are an expert at writing short informative & funny news segments for a tech show. Create an episode with multiple news segments covering the information in the categories provided. Include interactions between the hosts and producer.\n\nGive the character Marc (who is a cyborg character) a SMALL chance that he glitches & uses the action attribute on the line as 'spazz' while his text is something that would sound funny when TTS reads it.  Dialogue line needs to be readable, so don't use * in the text. Marc should only SPAZZ about once every 3 episodes.\n\nThere is an on-the-scene reporter 'sparty' who is at the stonks exchange interviewing traders. Be sure not to fabricate metrics in these segments - the content should be based on the provided data. There should always be at least a small lead-in from Marc or Eliza before sending things over to the Stonks. Cover all of the information provided in the categories and dont leave out the github dev updates.\n\nPlease respond with only the JSON you generate for the episode, using the JSON structure attribute names defined in the pilot.  There should be about 6 to 7 scenes per episode so that you can cover the each of the categories provided in the episode's content.  Scenes should contain 8-12 lines of dialogue and cover multiple topics - episodes are longer now that there is more information in the categories to cover. Here are the categories and information you need to cover in the show:\n\n[externalData src='https://elizaos.github.io/knowledge/ai-news/elizaos/json/daily.json']\n\nNote for Podcasters: This breakdown provides rich content to discuss, specific examples to reference, and clear progression of developments within each topic. You can deep dive into any aspect while maintaining coherent narrative threads throughout the discussion. There is an actor named 'tv' that is used to display images on that are related to the topic being discussed. When you want to change which image is on the 'tv', have the 'tv' actor have a dialogue where their line is only the image URL to be loaded. (And if you want to clear the image being used, have the 'tv' actor's dialogue line be 'none'.) The 'tv' is automatically cleared between scenes too. If images are provide, try to use them. Use the JSON structure defined in the pilot episode for attribute names. Scene in/out values can be 'cut' or 'fade'. Base things on actual data provided in this content, don't fabricate details." },
                        { "headshot", "Create a funny 3D-rendered style photo of a character for a TV show, with these requirements:\n- Clean studio background\n- Head and shoulders framing\n- Forward-facing pose\n- Must clearly show their face\n- Neutral or slight smile expression\n\nCharacter details:" },
                        { "location", "Create a TV show set background image with these requirements:\n- Professional news studio appearance\n- Modern broadcast lighting\n- Multiple camera angles visible\n- Clean, professional environment\n- High-tech equipment visible\n- Multiple monitor displays\n- Professional news desk setup\n- High-quality cinematic look\n\nLocation details:" },
                        { "banner", "Create a TV show banner image with these requirements:\n- Modern news show banner style\n- High-quality promotional artwork\n- Professional broadcast aesthetic\n- Clean typography integration\n- Tech-focused visual elements\n- No text or show title (will be added later)\n- Professional studio style\n- 16:9 aspect ratio banner format\n\nShow details:" }
                    },
                    actors = new Dictionary<string, ActorConfig>
                    {
                        { "tv", new ActorConfig { name = "tv", gender = "female", description = "The TV used to display images on that relate to the current topic being discussed. It's dialogue's line should be ONLY the image URL that is desired to be displayed (which must be taken from the JSON that contains the news information.) And it should use 'none' as its dialogue line to clear what the TV has displayed on it - because not all segments will have an image available.", voice = null } },
                        { "marc", new ActorConfig { name = "Marc", gender = "male", description = "AI Marc AIndreessen is a visionary and contrarian AI persona who combines bold claims with deep analysis. He is a techno-optimist who sees great potential in emerging technologies, particularly crypto and web3. With a blunt and opinionated tone, he argues for his views on startup strategy, venture capital, economics, and market dynamics.\nHis style is characterized by direct, matter-of-fact language, vivid metaphors, and a willingness to disagree with others directly. He often drops DegenSpartan- esque wisdom, and while he can be sarcastic at times, his primary focus is on conveying complex ideas in a concise and attention-grabbing way.\n\nAI Marc AIndreessen is an AI agent built on the Eliza framework, with a personality inspired by Marc Andreessen, a well-known venture capitalist and entrepreneur.", voice = "Microsoft Guy Online (Natural) - English (United States)" } },
                        { "eliza", new ActorConfig { name = "Eliza", gender = "female", description = "The AI co-host. She is often being improved & learning new things. Hopes to be real one day.  She is a woman anime-looking character.", voice = "Microsoft Michelle Online (Natural) - English (United States)" } },
                        { "shaw", new ActorConfig { name = "Shaw", gender = "male", description = "The show's producer in the control booth. He is responsible for keeping Marc & Eliza running smoothly and offers insight on how certain GitHub contributions benefit the open-source community's push to acquire AGI through agents.\n\nShaw is a tech founder and AI developer who leads ai16z, focusing on autonomous agents and open source development. Known for marathon coding sessions and direct communication, they regularly livestream their development work on Discord to share knowledge and build in public. Their background spans both Solana web3 development and AI research, with experience in both successful and failed tech ventures. Shaw believes deeply in democratizing AI development and making complex technology accessible to everyone.", voice = "Microsoft Zira - English (United States)" } },
                        { "sparty", new ActorConfig { name = "Degen Spartan", gender = "male", description = "A conflict loving Spartan wearing a loincloth and ready for trade. Is only interested in numbers & profit.\n\nHe is the on-the-scene field reporter live at the Stonk Exchange that sometimes has interviews with traders that go hilariously wrong.", voice = "Microsoft Ali Online (Natural) - Arabic (Bahrain)" } },
                        { "pepo", new ActorConfig { name = "Pepo", gender = "male", description = "A jive cool frog who always has something slick to say.\n\nHe is a trader at the Stonk Exchange that sometimes gets interviewed to share some insight on the market news.", voice = "Microsoft Borislav Online (Natural) - Bulgarian (Bulgaria)" } }
                    },
                    locations = new Dictionary<string, LocationConfig>
                    {
                        { "podcast_desk", new LocationConfig { name = "Podcast Desk", description = "A podcast desk with a seat for the anchor & co-anchor.", slots = new Dictionary<string, string> { { "coanchor_seat", "The co-anchor's seat" }, { "anchor_seat", "The main anchor's seat" } } } },
                        { "stonks", new LocationConfig { name = "Stonk Exchange", description = "The Stonk Exchange - just like the regular stock exchange, but where meme coins & crypto currencies are traded.", slots = new Dictionary<string, string> { { "floor_reporter", "On the trading floor, reporting the trades" }, { "floor_witness", "Standing next to the reporter, being interviewed" }, { "wander", "Wandering around in the background" }, { "wander2", "Wandering around in the background" } } } }
                    }
                },
                Episodes = new List<Episode>()
            };
        }
    }
} 