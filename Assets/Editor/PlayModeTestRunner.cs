using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using TMPro;

namespace Unity.AI.Assistant.PlayModeTest
{
    [InitializeOnLoad]
    internal static class PlayModeTestRunner
    {
        private const string StateKey = "PlayModeTest.State";
        private const string ResultKey = "PlayModeTest.Result";
        private const string ScriptPathKey = "PlayModeTest.ScriptPath";
        private const string SentinelLog = "PLAY_MODE_TEST_COMPLETE";

        private static readonly int WaitFrames = SessionState.GetInt("PlayModeTest.WaitFrames", 15);

        private static List<string> _capturedLogs = new List<string>();
        private const int MaxCapturedLogs = 50;

        static PlayModeTestRunner()
        {
            string state = SessionState.GetString(StateKey, "Idle");

            switch (state)
            {
                case "Idle":
                    break;

                case "WaitingForCompile":
                    Debug.Log("[PlayModeTest] Bootstrap compiled. Scheduling Play Mode entry.");
                    EditorApplication.delayCall += () =>
                    {
                        SessionState.SetString(StateKey, "EnteringPlayMode");
                        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
                        EditorApplication.isPlaying = true;
                    };
                    break;

                case "EnteringPlayMode":
                    EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
                    if (EditorApplication.isPlaying)
                    {
                        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
                        SessionState.SetString(StateKey, "InPlayMode");
                        EditorApplication.update += WaitFramesThenRun;
                    }
                    break;

                case "InPlayMode":
                    if (EditorApplication.isPlaying)
                    {
                        EditorApplication.update += WaitFramesThenRun;
                    }
                    break;

                case "Done":
                    Debug.Log(SentinelLog);
                    if (string.IsNullOrEmpty(SessionState.GetString("PlayModeTest.ScreenshotPath", "")))
                    {
                        EditorApplication.delayCall += SelfDestruct;
                    }
                    break;
            }
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.EnteredPlayMode)
            {
                EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
                SessionState.SetString(StateKey, "InPlayMode");
                EditorApplication.update += WaitFramesThenRun;
            }
        }

        private static int _frameCount = 0;
        private static bool _hasRun = false;

        private static void WaitFramesThenRun()
        {
            _frameCount++;
            if (_frameCount < WaitFrames) return;

            if (_hasRun) return;
            _hasRun = true;
            EditorApplication.update -= WaitFramesThenRun;

            Application.logMessageReceived += OnLogMessage;
            string resultJson;
            try
            {
                resultJson = RunTestLogic();
            }
            catch (System.Exception e)
            {
                Debug.LogError("[PlayModeTest] Test threw exception: " + e);
                resultJson = JsonUtility.ToJson(new TestResult
                {
                    success = false,
                    error = e.Message,
                    logs = _capturedLogs.ToArray()
                });
            }
            finally
            {
                Application.logMessageReceived -= OnLogMessage;
            }

            SessionState.SetString(ResultKey, resultJson);
            SessionState.SetString(StateKey, "Done");
            EditorApplication.isPlaying = false;
        }

        private static void SelfDestruct()
        {
            string scriptPath = SessionState.GetString(ScriptPathKey, "");
            if (!string.IsNullOrEmpty(scriptPath) && AssetDatabase.AssetPathExists(scriptPath))
            {
                AssetDatabase.DeleteAsset(scriptPath);
            }
            SessionState.EraseString(StateKey);
            SessionState.EraseString(ScriptPathKey);
        }

        private static void OnLogMessage(string message, string stackTrace, LogType type)
        {
            if (_capturedLogs.Count >= MaxCapturedLogs) return;
            if (type == LogType.Error || type == LogType.Exception ||
                message.Contains("[Test]") || message.Contains("TEST_RESULT"))
            {
                _capturedLogs.Add("[" + type + "] " + message);
            }
        }

        [System.Serializable]
        private class TestResult
        {
            public bool success;
            public string error;
            public int initialPlayerMoney;
            public string initialUIText;
            public int updatedPlayerMoney;
            public string updatedUIText;
            public string[] logs;
        }

        private static string RunTestLogic()
        {
            var uiGameSceneGo = GameObject.Find("UI_GameScene");
            if (uiGameSceneGo == null)
            {
                var uiSceneComponent = Object.FindAnyObjectByType<UI_GameScene>();
                if (uiSceneComponent != null)
                {
                    uiGameSceneGo = uiSceneComponent.gameObject;
                }
            }

            if (uiGameSceneGo == null)
            {
                return JsonUtility.ToJson(new TestResult
                {
                    success = false,
                    error = "UI_GameScene GameObject not found in scene."
                });
            }

            var textMoneyGo = GameObject.Find("Text_Money");
            var tmpText = textMoneyGo != null ? textMoneyGo.GetComponent<TextMeshProUGUI>() : uiGameSceneGo.GetComponentInChildren<TextMeshProUGUI>();
            if (tmpText == null)
            {
                return JsonUtility.ToJson(new TestResult
                {
                    success = false,
                    error = "TextMeshProUGUI component (Text_Money) not found."
                });
            }

            var player = PlayerController.Instance != null ? PlayerController.Instance : Object.FindAnyObjectByType<PlayerController>();
            if (player == null)
            {
                return JsonUtility.ToJson(new TestResult
                {
                    success = false,
                    error = "PlayerController not found."
                });
            }

            int initMoney = player.money;
            string initText = tmpText.text;
            Debug.Log("[Test] Initial Money: " + initMoney + ", Initial UI Text: " + initText);

            // Test money update (e.g., +500)
            player.UpdateMoney(500);
            int updatedMoney = player.money;
            string updatedText = tmpText.text;
            Debug.Log("[Test] Updated Money: " + updatedMoney + ", Updated UI Text: " + updatedText);

            bool isTextCorrect = updatedText == $"{updatedMoney:N0}";

            return JsonUtility.ToJson(new TestResult
            {
                success = isTextCorrect,
                error = isTextCorrect ? null : "UI text did not update to expected formatted money string.",
                initialPlayerMoney = initMoney,
                initialUIText = initText,
                updatedPlayerMoney = updatedMoney,
                updatedUIText = updatedText,
                logs = _capturedLogs.ToArray()
            });
        }
    }
}
