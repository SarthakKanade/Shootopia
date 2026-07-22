using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

namespace Shootopia
{
    /// <summary>
    /// Live scoreboard overlay active during gameplay when Tab key is held.
    /// Lists all connected players with their name, kills, and deaths.
    /// Groups players by team in TDM mode, or ranks by kills in FFA mode.
    /// </summary>
    public class ScoreboardController : MonoBehaviour
    {
        private bool _showScoreboard = false;

        private GUIStyle _overlayBgStyle;
        private GUIStyle _windowStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _teamHeaderBlueStyle;
        private GUIStyle _teamHeaderRedStyle;
        private GUIStyle _columnHeaderStyle;
        private GUIStyle _rowStyleNormal;
        private GUIStyle _rowStyleLocal;
        private GUIStyle _subTextStyle;

        private void Update()
        {
            // Listen for Tab key hold
            bool tabHeld = false;
            if (Keyboard.current != null)
            {
                tabHeld = Keyboard.current.tabKey.isPressed;
            }
            else
            {
                tabHeld = Input.GetKey(KeyCode.Tab);
            }

            _showScoreboard = tabHeld;
        }

        private void InitStyles()
        {
            if (_overlayBgStyle != null) return;

            // Semi-transparent dark backdrop
            _overlayBgStyle = new GUIStyle(GUI.skin.box);
            Texture2D bgTex = new Texture2D(1, 1);
            bgTex.SetPixel(0, 0, new Color(0.02f, 0.02f, 0.05f, 0.75f));
            bgTex.Apply();
            _overlayBgStyle.normal.background = bgTex;

            // Scoreboard inner window
            _windowStyle = new GUIStyle(GUI.skin.box);
            Texture2D winTex = new Texture2D(1, 1);
            winTex.SetPixel(0, 0, new Color(0.08f, 0.09f, 0.12f, 0.92f));
            winTex.Apply();
            _windowStyle.normal.background = winTex;
            _windowStyle.padding = new RectOffset(20, 20, 20, 20);

            // Title Style
            _titleStyle = new GUIStyle(GUI.skin.label);
            _titleStyle.alignment = TextAnchor.MiddleCenter;
            _titleStyle.fontSize = 26;
            _titleStyle.fontStyle = FontStyle.Bold;
            _titleStyle.normal.textColor = Color.white;

            // Team Header Styles
            _teamHeaderBlueStyle = new GUIStyle(GUI.skin.label);
            _teamHeaderBlueStyle.fontSize = 18;
            _teamHeaderBlueStyle.fontStyle = FontStyle.Bold;
            _teamHeaderBlueStyle.normal.textColor = new Color(0.3f, 0.65f, 1f); // Blue

            _teamHeaderRedStyle = new GUIStyle(GUI.skin.label);
            _teamHeaderRedStyle.fontSize = 18;
            _teamHeaderRedStyle.fontStyle = FontStyle.Bold;
            _teamHeaderRedStyle.normal.textColor = new Color(1f, 0.35f, 0.35f); // Red

            // Column Header Style
            _columnHeaderStyle = new GUIStyle(GUI.skin.label);
            _columnHeaderStyle.fontSize = 13;
            _columnHeaderStyle.fontStyle = FontStyle.Bold;
            _columnHeaderStyle.normal.textColor = new Color(0.7f, 0.7f, 0.7f);

            // Row Styles
            _rowStyleNormal = new GUIStyle(GUI.skin.label);
            _rowStyleNormal.fontSize = 14;
            _rowStyleNormal.normal.textColor = new Color(0.9f, 0.9f, 0.9f);

            _rowStyleLocal = new GUIStyle(GUI.skin.label);
            _rowStyleLocal.fontSize = 14;
            _rowStyleLocal.fontStyle = FontStyle.Bold;
            _rowStyleLocal.normal.textColor = new Color(1f, 0.85f, 0.2f); // Yellow highlight for local player

            // SubText / Hint
            _subTextStyle = new GUIStyle(GUI.skin.label);
            _subTextStyle.alignment = TextAnchor.MiddleCenter;
            _subTextStyle.fontSize = 11;
            _subTextStyle.normal.textColor = new Color(0.5f, 0.5f, 0.5f);
        }

        private void OnGUI()
        {
            if (!_showScoreboard) return;

            InitStyles();

            // Fullscreen backdrop (non-blocking)
            GUI.Box(new Rect(0, 0, Screen.width, Screen.height), GUIContent.none, _overlayBgStyle);

            // Centered Overlay Rect
            float width = 620f;
            float height = 460f;
            float x = (Screen.width - width) / 2f;
            float y = (Screen.height - height) / 2f;

            GUILayout.BeginArea(new Rect(x, y, width, height), _windowStyle);

            var gameMode = GameModeBase.Instance;
            bool isTDM = gameMode is TDMMode;

            string modeTitle = isTDM ? "TEAM DEATHMATCH SCOREBOARD" : "FREE-FOR-ALL SCOREBOARD";
            GUILayout.Label(modeTitle, _titleStyle);
            GUILayout.Space(10f);

            ulong localClientId = NetworkManager.Singleton != null ? NetworkManager.Singleton.LocalClientId : 0;

            if (isTDM && gameMode is TDMMode tdm)
            {
                RenderTDMScoreboard(tdm, localClientId);
            }
            else
            {
                RenderFFAScoreboard(gameMode, localClientId);
            }

            GUILayout.FlexibleSpace();
            GUILayout.Label("Release [Tab] to close", _subTextStyle);
            GUILayout.EndArea();
        }

        private void RenderTDMScoreboard(TDMMode tdm, ulong localClientId)
        {
            // Group players into Blue (0) and Red (1)
            var bluePlayers = new List<ulong>();
            var redPlayers = new List<ulong>();
            var unassignedPlayers = new List<ulong>();

            if (NetworkManager.Singleton != null)
            {
                foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
                {
                    ulong id = client.ClientId;
                    int team = tdm.GetPlayerTeam(id);
                    if (team == 0) bluePlayers.Add(id);
                    else if (team == 1) redPlayers.Add(id);
                    else unassignedPlayers.Add(id);
                }
            }

            // Team Blue Section
            GUILayout.Label($"BLUE TEAM — Kills: {tdm.ScoreBlue}", _teamHeaderBlueStyle);
            DrawTableHeader();
            foreach (var id in bluePlayers)
            {
                DrawPlayerRow(id, gameMode: tdm, isLocal: id == localClientId);
            }

            GUILayout.Space(15f);

            // Team Red Section
            GUILayout.Label($"RED TEAM — Kills: {tdm.ScoreRed}", _teamHeaderRedStyle);
            DrawTableHeader();
            foreach (var id in redPlayers)
            {
                DrawPlayerRow(id, gameMode: tdm, isLocal: id == localClientId);
            }

            if (unassignedPlayers.Count > 0)
            {
                GUILayout.Space(10f);
                GUILayout.Label("UNASSIGNED", _columnHeaderStyle);
                foreach (var id in unassignedPlayers)
                {
                    DrawPlayerRow(id, gameMode: tdm, isLocal: id == localClientId);
                }
            }
        }

        private void RenderFFAScoreboard(GameModeBase gameMode, ulong localClientId)
        {
            var players = new List<ulong>();
            if (NetworkManager.Singleton != null)
            {
                foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
                {
                    players.Add(client.ClientId);
                }
            }

            // Sort players by kills descending
            players.Sort((a, b) =>
            {
                int killsA = gameMode != null ? gameMode.GetPlayerKills(a) : 0;
                int killsB = gameMode != null ? gameMode.GetPlayerKills(b) : 0;
                return killsB.CompareTo(killsA);
            });

            DrawTableHeader();
            foreach (var id in players)
            {
                DrawPlayerRow(id, gameMode: gameMode, isLocal: id == localClientId);
            }
        }

        private void DrawTableHeader()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("PLAYER NAME", _columnHeaderStyle, GUILayout.Width(280f));
            GUILayout.Label("KILLS", _columnHeaderStyle, GUILayout.Width(90f));
            GUILayout.Label("DEATHS", _columnHeaderStyle, GUILayout.Width(90f));
            GUILayout.Label("K/D", _columnHeaderStyle, GUILayout.Width(80f));
            GUILayout.EndHorizontal();

            // Divider
            GUILayout.Box("", GUILayout.Height(1f), GUILayout.ExpandWidth(true));
            GUILayout.Space(3f);
        }

        private void DrawPlayerRow(ulong clientId, GameModeBase gameMode, bool isLocal)
        {
            string name = PlayerNameRegistry.GetName(clientId);
            if (string.IsNullOrEmpty(name)) name = $"Player {clientId}";
            if (isLocal) name += " (YOU)";

            int kills = gameMode != null ? gameMode.GetPlayerKills(clientId) : 0;
            int deaths = gameMode != null ? gameMode.GetPlayerDeaths(clientId) : 0;
            float kd = deaths > 0 ? (float)kills / deaths : kills;
            string kdStr = deaths > 0 ? kd.ToString("0.0") : (kills > 0 ? kills.ToString("0.0") : "0.0");

            GUIStyle style = isLocal ? _rowStyleLocal : _rowStyleNormal;

            GUILayout.BeginHorizontal();
            GUILayout.Label(name, style, GUILayout.Width(280f));
            GUILayout.Label(kills.ToString(), style, GUILayout.Width(90f));
            GUILayout.Label(deaths.ToString(), style, GUILayout.Width(90f));
            GUILayout.Label(kdStr, style, GUILayout.Width(80f));
            GUILayout.EndHorizontal();
        }
    }
}
