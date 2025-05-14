using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using ShowRunner;
using System.IO;

namespace ShowRunner.Editor
{
    public class ShowFormatEditor : EditorWindow
    {
        private ShowData showData;
        private Vector2 scrollPosition;
        private string currentFilePath;
        private bool showConfig = true;
        private bool showEpisodes = true;
        private Dictionary<int, bool> showEpisodeFoldouts = new Dictionary<int, bool>();
        private Dictionary<int, bool> showSceneFoldouts = new Dictionary<int, bool>();

        [MenuItem("Window/ShowRunner/Show Format Editor")]
        public static void ShowWindow()
        {
            GetWindow<ShowFormatEditor>("Show Format Editor");
        }

        private void OnGUI()
        {
            if (showData == null)
            {
                showData = ShowDataSerializer.CreateNewShow();
            }

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            DrawFileOperations();
            EditorGUILayout.Space();

            showConfig = EditorGUILayout.Foldout(showConfig, "Show Configuration", true);
            if (showConfig)
            {
                DrawConfigSection();
            }
            EditorGUILayout.Space();

            showEpisodes = EditorGUILayout.Foldout(showEpisodes, "Episodes", true);
            if (showEpisodes)
            {
                DrawEpisodesSection();
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawFileOperations()
        {
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Load Show"))
            {
                string path = EditorUtility.OpenFilePanel("Load Show", "", "json");
                if (!string.IsNullOrEmpty(path))
                {
                    var loadedData = ShowDataSerializer.LoadFromFile(path);
                    if (loadedData != null)
                    {
                        showData = loadedData;
                        currentFilePath = null;
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Error", "Failed to load show data from file.", "OK");
                    }
                }
            }

            if (GUILayout.Button("Save As..."))
            {
                string path = EditorUtility.SaveFilePanel("Save Show As", "", "show.json", "json");
                if (!string.IsNullOrEmpty(path))
                {
                    if (ShowDataSerializer.SaveToFile(showData, path))
                    {
                        currentFilePath = path;
                        EditorUtility.DisplayDialog("Success", "Show data saved successfully!", "OK");
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Error", "Failed to save show data to file.", "OK");
                    }
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawConfigSection()
        {
            if (showData.Config == null)
            {
                showData.Config = new ShowConfig
                {
                    id = System.Guid.NewGuid().ToString(),
                    name = "New Show",
                    description = "",
                    creator = "",
                    prompts = new Dictionary<string, string>(),
                    actors = new Dictionary<string, ActorConfig>(),
                    locations = new Dictionary<string, LocationConfig>()
                };
            }

            EditorGUI.indentLevel++;
            
            showData.Config.name = EditorGUILayout.TextField(new GUIContent("Show Name", "The display name of the show."), showData.Config.name);
            showData.Config.description = EditorGUILayout.TextField(new GUIContent("Description", "A short description or summary of the show."), showData.Config.description);
            showData.Config.creator = EditorGUILayout.TextField(new GUIContent("Creator", "The creator or author of the show."), showData.Config.creator);

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("Prompts are reusable text snippets or instructions for the show (e.g., intro, outro, catchphrases).", MessageType.Info);
            EditorGUILayout.LabelField("Prompts", EditorStyles.boldLabel);
            DrawDictionaryVertical(showData.Config.prompts, "Prompt Key", "Prompt Value");

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("Actors are the characters or voices in your show. Each actor has a name, description, and voice.", MessageType.Info);
            EditorGUILayout.LabelField("Actors", EditorStyles.boldLabel);
            DrawActorsVertical();

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("Locations are the places or settings where scenes occur. Each location can have named slots (areas or props).", MessageType.Info);
            EditorGUILayout.LabelField("Locations", EditorStyles.boldLabel);
            DrawLocationsVertical();

            EditorGUI.indentLevel--;
        }

        private void DrawEpisodesSection()
        {
            EditorGUI.indentLevel++;

            if (GUILayout.Button("Add Episode"))
            {
                showData.Episodes.Add(new Episode
                {
                    id = System.Guid.NewGuid().ToString(),
                    name = "New Episode",
                    premise = "",
                    summary = "",
                    scenes = new List<Scene>()
                });
            }

            for (int i = 0; i < showData.Episodes.Count; i++)
            {
                var episode = showData.Episodes[i];
                if (!showEpisodeFoldouts.ContainsKey(i))
                {
                    showEpisodeFoldouts[i] = false;
                }

                showEpisodeFoldouts[i] = EditorGUILayout.Foldout(showEpisodeFoldouts[i], episode.name, true);
                if (showEpisodeFoldouts[i])
                {
                    EditorGUI.indentLevel++;
                    episode.name = EditorGUILayout.TextField("Name", episode.name);
                    episode.premise = EditorGUILayout.TextField("Premise", episode.premise);
                    episode.summary = EditorGUILayout.TextField("Summary", episode.summary);

                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Scenes", EditorStyles.boldLabel);

                    if (GUILayout.Button("Add Scene"))
                    {
                        episode.scenes.Add(new Scene
                        {
                            location = "",
                            description = "",
                            inTime = "",
                            outTime = "",
                            cast = new Dictionary<string, string>(),
                            dialogue = new List<Dialogue>()
                        });
                    }

                    for (int j = 0; j < episode.scenes.Count; j++)
                    {
                        var scene = episode.scenes[j];
                        if (!showSceneFoldouts.ContainsKey(j))
                        {
                            showSceneFoldouts[j] = false;
                        }

                        showSceneFoldouts[j] = EditorGUILayout.Foldout(showSceneFoldouts[j], $"Scene {j + 1}", true);
                        if (showSceneFoldouts[j])
                        {
                            EditorGUI.indentLevel++;
                            scene.location = EditorGUILayout.TextField("Location", scene.location);
                            scene.description = EditorGUILayout.TextField("Description", scene.description);
                            scene.inTime = EditorGUILayout.TextField("In", scene.inTime);
                            scene.outTime = EditorGUILayout.TextField("Out", scene.outTime);

                            EditorGUILayout.Space();
                            EditorGUILayout.LabelField("Cast", EditorStyles.boldLabel);
                            DrawDictionary(scene.cast);

                            EditorGUILayout.Space();
                            EditorGUILayout.LabelField("Dialogue", EditorStyles.boldLabel);
                            DrawDialogue(scene.dialogue);

                            EditorGUI.indentLevel--;
                        }
                    }

                    EditorGUI.indentLevel--;
                }
            }

            EditorGUI.indentLevel--;
        }

        private void DrawDictionary(Dictionary<string, string> dict)
        {
            if (dict == null)
            {
                dict = new Dictionary<string, string>();
            }

            EditorGUI.indentLevel++;

            List<string> keysToRemove = new List<string>();
            foreach (var kvp in dict)
            {
                EditorGUILayout.BeginHorizontal();
                string newKey = EditorGUILayout.TextField(kvp.Key);
                string newValue = EditorGUILayout.TextField(kvp.Value);

                if (newKey != kvp.Key || newValue != kvp.Value)
                {
                    keysToRemove.Add(kvp.Key);
                    dict[newKey] = newValue;
                }

                if (GUILayout.Button("X", GUILayout.Width(20)))
                {
                    keysToRemove.Add(kvp.Key);
                }
                EditorGUILayout.EndHorizontal();
            }

            foreach (var key in keysToRemove)
            {
                dict.Remove(key);
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("New Key:", GUILayout.Width(60));
            string newKeyField = EditorGUILayout.TextField("");
            EditorGUILayout.LabelField("Value:", GUILayout.Width(40));
            string newValueField = EditorGUILayout.TextField("");

            if (GUILayout.Button("Add", GUILayout.Width(60)) && !string.IsNullOrEmpty(newKeyField))
            {
                dict[newKeyField] = newValueField;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUI.indentLevel--;
        }

        private void DrawDictionaryVertical(Dictionary<string, string> dict, string keyLabel, string valueLabel)
        {
            if (dict == null)
                dict = new Dictionary<string, string>();
            EditorGUI.indentLevel++;
            List<string> keysToRemove = new List<string>();
            foreach (var kvp in dict)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                string newKey = EditorGUILayout.TextField(new GUIContent(keyLabel, "The unique key for this entry."), kvp.Key);
                string newValue = EditorGUILayout.TextField(new GUIContent(valueLabel, "The value or text for this entry."), kvp.Value);
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Remove", GUILayout.Width(70)))
                {
                    keysToRemove.Add(kvp.Key);
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                if (newKey != kvp.Key || newValue != kvp.Value)
                {
                    keysToRemove.Add(kvp.Key);
                    dict[newKey] = newValue;
                }
            }
            foreach (var key in keysToRemove)
                dict.Remove(key);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            string newKeyField = EditorGUILayout.TextField(new GUIContent(keyLabel, "The unique key for this entry."), "");
            string newValueField = EditorGUILayout.TextField(new GUIContent(valueLabel, "The value or text for this entry."), "");
            if (GUILayout.Button("Add New Entry"))
            {
                if (!string.IsNullOrEmpty(newKeyField))
                    dict[newKeyField] = newValueField;
            }
            EditorGUILayout.EndVertical();
            EditorGUI.indentLevel--;
        }

        private void DrawActorsVertical()
        {
            if (showData.Config.actors == null)
                showData.Config.actors = new Dictionary<string, ActorConfig>();
            EditorGUI.indentLevel++;
            List<string> keysToRemove = new List<string>();
            foreach (var kvp in showData.Config.actors)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                string newKey = EditorGUILayout.TextField(new GUIContent("Actor ID", "A unique identifier for this actor (e.g., 'host', 'guest1')."), kvp.Key);
                var actor = kvp.Value;
                actor.name = EditorGUILayout.TextField(new GUIContent("Name", "The display name of the actor."), actor.name);
                actor.description = EditorGUILayout.TextField(new GUIContent("Description", "A short description of the actor's role or personality."), actor.description);
                actor.voice = EditorGUILayout.TextField(new GUIContent("Voice", "The voice or TTS profile for this actor."), actor.voice);
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Remove", GUILayout.Width(70)))
                {
                    keysToRemove.Add(kvp.Key);
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                if (newKey != kvp.Key)
                {
                    keysToRemove.Add(kvp.Key);
                    showData.Config.actors[newKey] = actor;
                }
            }
            foreach (var key in keysToRemove)
                showData.Config.actors.Remove(key);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            string newKeyField = EditorGUILayout.TextField(new GUIContent("New Actor ID", "A unique identifier for the new actor."), "");
            if (GUILayout.Button("Add New Actor"))
            {
                if (!string.IsNullOrEmpty(newKeyField))
                    showData.Config.actors[newKeyField] = new ActorConfig { name = "", description = "", voice = "" };
            }
            EditorGUILayout.EndVertical();
            EditorGUI.indentLevel--;
        }

        private void DrawLocationsVertical()
        {
            if (showData.Config.locations == null)
                showData.Config.locations = new Dictionary<string, LocationConfig>();
            EditorGUI.indentLevel++;
            List<string> keysToRemove = new List<string>();
            foreach (var kvp in showData.Config.locations)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                string newKey = EditorGUILayout.TextField(new GUIContent("Location ID", "A unique identifier for this location (e.g., 'studio', 'lab')."), kvp.Key);
                var location = kvp.Value;
                location.name = EditorGUILayout.TextField(new GUIContent("Name", "The display name of the location."), location.name);
                location.description = EditorGUILayout.TextField(new GUIContent("Description", "A short description of the location."), location.description);
                EditorGUILayout.LabelField("Slots", EditorStyles.boldLabel);
                DrawDictionaryVertical(location.slots, "Slot Key", "Slot Value");
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Remove", GUILayout.Width(70)))
                {
                    keysToRemove.Add(kvp.Key);
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                if (newKey != kvp.Key)
                {
                    keysToRemove.Add(kvp.Key);
                    showData.Config.locations[newKey] = location;
                }
            }
            foreach (var key in keysToRemove)
                showData.Config.locations.Remove(key);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            string newKeyField = EditorGUILayout.TextField(new GUIContent("New Location ID", "A unique identifier for the new location."), "");
            if (GUILayout.Button("Add New Location"))
            {
                if (!string.IsNullOrEmpty(newKeyField))
                    showData.Config.locations[newKeyField] = new LocationConfig { name = "", description = "", slots = new Dictionary<string, string>() };
            }
            EditorGUILayout.EndVertical();
            EditorGUI.indentLevel--;
        }

        private void DrawDialogue(List<Dialogue> dialogue)
        {
            EditorGUI.indentLevel++;

            for (int i = 0; i < dialogue.Count; i++)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                var line = dialogue[i];
                line.actor = EditorGUILayout.TextField("Actor", line.actor);
                line.line = EditorGUILayout.TextField("Line", line.line);
                line.action = EditorGUILayout.TextField("Action", line.action);

                if (GUILayout.Button("Remove Line"))
                {
                    dialogue.RemoveAt(i);
                    i--;
                }
                EditorGUILayout.EndVertical();
            }

            if (GUILayout.Button("Add Dialogue Line"))
            {
                dialogue.Add(new Dialogue
                {
                    actor = "",
                    line = "",
                    action = ""
                });
            }

            EditorGUI.indentLevel--;
        }
    }
} 