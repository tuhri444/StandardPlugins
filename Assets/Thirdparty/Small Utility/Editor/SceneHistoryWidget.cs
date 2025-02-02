/*Alterante version compatible with older Unity versions that do not support C# 9*/

/*
MIT License

Copyright (c) 2016 Jesse Ringrose

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public class SceneHistoryWidget
{

    private const int MAX_ITEMS = 10;

    private static bool showOnRight;
    private static readonly List<HistoryItem> sceneHistory;
    private static string[] sceneHistoryNames;
    private static int currentSceneIndex;
    private static string currentScenePath;
    private static bool ignoreNextSceneChange;

    static SceneHistoryWidget()
    {
        EditorApplication.update += Update;
        SceneView.duringSceneGui += OnGUI;

        sceneHistory = new List<HistoryItem>(MAX_ITEMS);

        var currentScene = SceneManager.GetActiveScene();
        currentScenePath = currentScene.path;

        LoadHistory();

        if (sceneHistory.Count > currentSceneIndex && sceneHistory[currentSceneIndex].path == currentScenePath)
        {
            // history is still accurate, no need to refresh.
        }
        else
        {
            AddHistoryItem(currentScene);
        }

        showOnRight = EditorPrefs.GetBool(HistoryKey("showOnRight"));
    }

    private static void AddHistoryItem(Scene scene)
    {
        if (string.IsNullOrEmpty(scene.path))
        {
            return;
        }

        HistoryItem item = new HistoryItem(scene);

        sceneHistory.Remove(item);
        sceneHistory.Insert(0, item);

        while (sceneHistory.Count > MAX_ITEMS)
        {
            sceneHistory.RemoveAt(sceneHistory.Count - 1);
        }

        UpdateHistoryNames();

        SaveHistory();

        SetIndex(0);
    }

    private static void UpdateHistoryNames()
    {
        sceneHistoryNames = new string[sceneHistory.Count];
        for (int i = 0; i < sceneHistory.Count; i++)
        {
            sceneHistoryNames[i] = (i > 0 ? (-i).ToString() : "") + " " + sceneHistory[i].name;
        }
    }

    private static void Update()
    {
        var activeScene = SceneManager.GetActiveScene();
        if (!activeScene.path.Equals(currentScenePath, StringComparison.Ordinal))
        {
            currentScenePath = activeScene.path;

            // ignore scene changes that were triggered by the widget
            if (ignoreNextSceneChange)
            {
                ignoreNextSceneChange = false;
            }
            else
            {

                // if we're leaving an unsaved scene then remove it from history (we can't go back to it anyways)
                if (sceneHistory.Count > 0 && string.IsNullOrEmpty(sceneHistory[0].path))
                {
                    sceneHistory.RemoveAt(0);
                }

                AddHistoryItem(activeScene);

                SceneView.RepaintAll();
            }
        }

        HandleMouseInput();
    }

    private static void OnGUI(SceneView sceneView)
    {
        Handles.BeginGUI();

        int pickerWidth = 150;
        int buttonWidth = 16;
        int barHeight = 16;
        int barWidth = pickerWidth + buttonWidth + buttonWidth + 4;
        int currX = 0;

        if (showOnRight)
        {
            currX = (int)(Screen.width / EditorGUIUtility.pixelsPerPoint) - barWidth;
        }

        GUI.Box(new Rect(currX, 0, barWidth, barHeight), "", EditorStyles.toolbar);

        // back
        GUI.enabled = CanGoBack;
        if (GUI.Button(new Rect(currX, 0, buttonWidth, barHeight), "<", EditorStyles.toolbarButton))
        {
            LoadScene(currentSceneIndex + 1);
        }
        currX += buttonWidth;
        GUI.enabled = true;


        // dropdown
        int newSceneIndex = EditorGUI.Popup(new Rect(currX, 0, pickerWidth, barHeight), currentSceneIndex, sceneHistoryNames, EditorStyles.toolbarPopup);
        if (newSceneIndex != currentSceneIndex)
        {
            LoadScene(newSceneIndex);
        }
        currX += pickerWidth;


        // forward
        GUI.enabled = CanGoForwards;
        if (GUI.Button(new Rect(currX, 0, buttonWidth, barHeight), ">", EditorStyles.toolbarButton))
        {
            LoadScene(currentSceneIndex - 1);
        }
        currX += buttonWidth;
        GUI.enabled = true;

        Handles.EndGUI();

        HandleMouseInput();
    }

    private static bool CanGoBack => sceneHistory.Count > 1 && currentSceneIndex < sceneHistory.Count - 1;

    private static bool CanGoForwards => currentSceneIndex != 0;

    private static void HandleMouseInput()
    {
        if (Event.current != null && Event.current.rawType is EventType.ContextClick)
        {
            int button = Event.current.button;
            if (button == 3 && CanGoBack)
            {
                LoadScene(currentSceneIndex + 1);
                Event.current.Use();
            }
            else if (button == 4 && CanGoForwards)
            {
                LoadScene(currentSceneIndex - 1);
                Event.current.Use();
            }
        }
    }

    private static void LoadScene(int sceneIndex)
    {
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            ignoreNextSceneChange = true;
            SetIndex(sceneIndex);
            EditorSceneManager.OpenScene(sceneHistory[sceneIndex].path, OpenSceneMode.Single);
        }
    }

    private static void SetIndex(int index)
    {
        currentSceneIndex = index;
        EditorPrefs.SetInt(HistoryKey("index"), currentSceneIndex);
    }

    private static void SaveHistory()
    {
        for (int i = 0; i < sceneHistory.Count; i++)
        {
            EditorPrefs.SetString(HistoryKey(i + "_path"), sceneHistory[i].path);
            EditorPrefs.SetString(HistoryKey(i + "_name"), sceneHistory[i].name);
        }
    }

    private static void LoadHistory()
    {
        for (int i = 0; i < MAX_ITEMS; i++)
        {
            if (EditorPrefs.HasKey(HistoryKey(i + "_path")) && EditorPrefs.HasKey(HistoryKey(i + "_name")))
            {
                HistoryItem item = new HistoryItem();
                item.path = EditorPrefs.GetString(HistoryKey(i + "_path"));
                item.name = EditorPrefs.GetString(HistoryKey(i + "_name"));
                sceneHistory.Add(item);
            }
            else
            {
                break;
            }
        }

        if (EditorPrefs.HasKey(HistoryKey("index")))
        {
            currentSceneIndex = EditorPrefs.GetInt(HistoryKey("index"));
        }

        UpdateHistoryNames();
    }

    private static string HistoryKey(string key)
    {
        return "sceneHistory_" + PlayerSettings.productName + "_" + key;
    }

    [SettingsProvider]
    private static SettingsProvider SceneHistory_Prefs()
    {
        SettingsProvider sceneHistoryPrefs = new SettingsProvider("Preferences/Scene History", SettingsScope.User)
        {
            guiHandler = _ =>
            {
                bool newShowOnRight = EditorGUILayout.Toggle("Show On Right", showOnRight);

                if (showOnRight != newShowOnRight)
                {
                    showOnRight = newShowOnRight;
                    EditorPrefs.SetBool(HistoryKey("showOnRight"), showOnRight);
                }

                if (GUILayout.Button("Clear History"))
                {
                    for (int i = 0; i < MAX_ITEMS; i++)
                    {
                        EditorPrefs.DeleteKey(HistoryKey(i + "_path"));
                        EditorPrefs.DeleteKey(HistoryKey(i + "_name"));
                    }

                    while (sceneHistory.Count > 1)
                        sceneHistory.RemoveAt(1);

                    UpdateHistoryNames();
                }
            }
            
        };

        return sceneHistoryPrefs;
    }

    private struct HistoryItem
    {
        public string name;
        public string path;

        public HistoryItem(Scene scene)
        {
            name = scene.name;
            path = scene.path;
        }
    }
}