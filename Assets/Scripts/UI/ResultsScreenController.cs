using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

namespace Shootopia
{
    public class ResultsScreenController : MonoBehaviour
    {
        private string _winnerInfo = "";
        private PlayerScoreEntry[] _scores = null;
        private bool _showResults = false;

        private GUIStyle _backgroundStyle;
        private GUIStyle _dialogStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _winnerStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _tableHeaderStyle;
        private GUIStyle _tableRowStyle;

        private void OnEnable()
        {
            GameModeBase.OnMatchEndedWithScoreboard += ShowResults;
        }

        private void OnDisable()
        {
            GameModeBase.OnMatchEndedWithScoreboard -= ShowResults;
        }

        private void ShowResults(string winnerInfo, PlayerScoreEntry[] scores)
        {
            _winnerInfo = winnerInfo;
            _scores = scores;
            _showResults = true;

            // Unlock cursor so players can click the Return to Lobby button
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void InitStyles()
        {
            if (_backgroundStyle != null) return;

            // Background style: dark overlay
            _backgroundStyle = new GUIStyle(GUI.skin.box);
            Texture2D darkTex = new Texture2D(1, 1);
            darkTex.SetPixel(0, 0, new Color(0.05f, 0.05f, 0.05f, 0.85f));
            darkTex.Apply();
            _backgroundStyle.normal.background = darkTex;

            // Dialog style: border box (using a slightly lighter dark color)
            _dialogStyle = new GUIStyle(GUI.skin.box);
            Texture2D dialogTex = new Texture2D(1, 1);
            dialogTex.SetPixel(0, 0, new Color(0.12f, 0.12f, 0.14f, 0.95f));
            dialogTex.Apply();
            _dialogStyle.normal.background = dialogTex;
            _dialogStyle.padding = new RectOffset(25, 25, 25, 25);

            // Title Style
            _titleStyle = new GUIStyle(GUI.skin.label);
            _titleStyle.alignment = TextAnchor.MiddleCenter;
            _titleStyle.fontSize = 32;
            _titleStyle.fontStyle = FontStyle.Bold;
            _titleStyle.normal.textColor = new Color(0.9f, 0.3f, 0.3f); // Coral/Red

            // Label Style
            _labelStyle = new GUIStyle(GUI.skin.label);
            _labelStyle.alignment = TextAnchor.MiddleCenter;
            _labelStyle.fontSize = 14;
            _labelStyle.normal.textColor = Color.gray;

            // Winner Style
            _winnerStyle = new GUIStyle(GUI.skin.label);
            _winnerStyle.alignment = TextAnchor.MiddleCenter;
            _winnerStyle.fontSize = 24;
            _winnerStyle.fontStyle = FontStyle.Bold;
            _winnerStyle.normal.textColor = new Color(0.95f, 0.8f, 0.2f); // Golden Yellow

            // Table Header Style
            _tableHeaderStyle = new GUIStyle(GUI.skin.label);
            _tableHeaderStyle.fontSize = 16;
            _tableHeaderStyle.fontStyle = FontStyle.Bold;
            _tableHeaderStyle.normal.textColor = Color.white;

            // Table Row Style
            _tableRowStyle = new GUIStyle(GUI.skin.label);
            _tableRowStyle.fontSize = 14;
            _tableRowStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f);

            // Button Style
            _buttonStyle = new GUIStyle(GUI.skin.button);
            _buttonStyle.fontSize = 16;
            _buttonStyle.fontStyle = FontStyle.Bold;
            _buttonStyle.normal.textColor = Color.white;
            _buttonStyle.hover.textColor = Color.green;
        }

        private void OnGUI()
        {
            InitStyles();

            // 1. Draw results screen scoreboard if match ended
            if (_showResults)
            {
                // Full screen overlay background
                GUI.Box(new Rect(0, 0, Screen.width, Screen.height), GUIContent.none, _backgroundStyle);

                // Center Dialog Box
                float boxWidth = 550f;
                float boxHeight = 400f;
                float boxX = (Screen.width - boxWidth) / 2f;
                float boxY = (Screen.height - boxHeight) / 2f;

                GUILayout.BeginArea(new Rect(boxX, boxY, boxWidth, boxHeight), _dialogStyle);

                GUILayout.Label("MATCH OVER", _titleStyle);
                GUILayout.Space(10f);

                GUILayout.Label("WINNER:", _labelStyle);
                GUILayout.Label(_winnerInfo, _winnerStyle);
                GUILayout.Space(25f);

                // Scoreboard Table header
                GUILayout.BeginHorizontal();
                GUILayout.Label("Player Name", _tableHeaderStyle, GUILayout.Width(250f));
                GUILayout.Label("Kills", _tableHeaderStyle, GUILayout.Width(100f));
                GUILayout.Label("Deaths", _tableHeaderStyle, GUILayout.Width(100f));
                GUILayout.EndHorizontal();

                // Separator line
                GUILayout.Box("", GUILayout.Height(2f), GUILayout.ExpandWidth(true));
                GUILayout.Space(5f);

                // Draw score entries
                if (_scores != null)
                {
                    foreach (var entry in _scores)
                    {
                        string playerName = PlayerNameRegistry.GetName(entry.ClientId);
                        if (string.IsNullOrEmpty(playerName)) playerName = $"Player {entry.ClientId}";

                        GUILayout.BeginHorizontal();
                        GUILayout.Label(playerName, _tableRowStyle, GUILayout.Width(250f));
                        GUILayout.Label(entry.Kills.ToString(), _tableRowStyle, GUILayout.Width(100f));
                        GUILayout.Label(entry.Deaths.ToString(), _tableRowStyle, GUILayout.Width(100f));
                        GUILayout.EndHorizontal();
                    }
                }

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Return to Lobby", _buttonStyle, GUILayout.Height(40f)))
                {
                    ReturnToLobby();
                }

                GUILayout.EndArea();
                return;
            }

            // 2. Draw pre-match countdown or match timer if game mode exists
            if (GameModeBase.Instance != null)
            {
                // Pre-match countdown
                int countdown = GameModeBase.Instance.PreMatchCountdown.Value;
                if (countdown > 0)
                {
                    GUI.Box(new Rect(0, 0, Screen.width, Screen.height), GUIContent.none, _backgroundStyle);
                    
                    float labelWidth = 600f;
                    float labelHeight = 150f;
                    float labelX = (Screen.width - labelWidth) / 2f;
                    float labelY = (Screen.height - labelHeight) / 2f;
                    
                    GUIStyle countdownStyle = new GUIStyle(_titleStyle);
                    countdownStyle.fontSize = 72;
                    countdownStyle.normal.textColor = Color.yellow;
                    
                    GUI.Label(new Rect(labelX, labelY, labelWidth, labelHeight), $"GET READY\n{countdown}", countdownStyle);
                }
                // Live match timer
                else if (GameModeBase.Instance.MatchActive.Value)
                {
                    float timeRemaining = GameModeBase.Instance.TimeRemaining;
                    int minutes = Mathf.FloorToInt(timeRemaining / 60f);
                    int seconds = Mathf.FloorToInt(timeRemaining % 60f);
                    string timerText = string.Format("{0:00}:{1:00}", minutes, seconds);

                    float timerWidth = 150f;
                    float timerHeight = 50f;
                    float timerX = (Screen.width - timerWidth) / 2f;
                    float timerY = 15f;

                    GUIStyle timerStyle = new GUIStyle(GUI.skin.box);
                    timerStyle.alignment = TextAnchor.MiddleCenter;
                    timerStyle.fontSize = 24;
                    timerStyle.fontStyle = FontStyle.Bold;
                    timerStyle.normal.textColor = Color.white;
                    Texture2D timerBg = new Texture2D(1, 1);
                    timerBg.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.5f));
                    timerBg.Apply();
                    timerStyle.normal.background = timerBg;

                    GUI.Box(new Rect(timerX, timerY, timerWidth, timerHeight), timerText, timerStyle);
                }
            }
        }

        private void ReturnToLobby()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.Shutdown();
            }

            if (SessionManager.Instance != null)
            {
                _ = SessionManager.Instance.LeaveSession();
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene("Main");
            }
        }
    }
}
