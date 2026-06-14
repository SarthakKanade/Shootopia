using UnityEngine;
using VelocityWars.Core;

public enum GameState { Menu, Playing, Paused, GameOver, Victory }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Events")]
    [SerializeField] private GameEvent _onGameStart;
    [SerializeField] private GameEvent _onGameOver;
    [SerializeField] private GameEvent _onVictory;
    [SerializeField] private GameEvent _onLifeLost;
    [SerializeField] private FloatGameEvent _onEnemyCountChanged;

    [Header("Settings")]
    [SerializeField] private int _maxLives = 3;

    private GameState _currentState;
    private int _remainingEnemies;
    private int _currentLives;

    public GameState CurrentState => _currentState;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
        _currentLives = _maxLives;
    }

    private void Start()
    {
        SetState(GameState.Playing);
    }

    public void SetState(GameState newState)
    {
        _currentState = newState;
        
        switch (_currentState)
        {
            case GameState.Playing:
                _onGameStart?.Raise();
                break;
            case GameState.GameOver:
                _onGameOver?.Raise();
                break;
            case GameState.Victory:
                _onVictory?.Raise();
                break;
        }
    }

    public void RegisterEnemy()
    {
        _remainingEnemies++;
        Debug.Log($"[GameManager] Enemy Registered. Total: {_remainingEnemies}");
        _onEnemyCountChanged?.Raise(_remainingEnemies);
    }

    public void EnemyKilled()
    {
        _remainingEnemies = Mathf.Max(0, _remainingEnemies - 1);
        Debug.Log($"[GameManager] Enemy Killed. Remaining: {_remainingEnemies}");
        _onEnemyCountChanged?.Raise(_remainingEnemies);
        
        if (_remainingEnemies <= 0)
        {
            SetState(GameState.Victory);
        }
        }

        public void LoseLife()
        {
        _currentLives--;
        Debug.Log($"[GameManager] Life Lost. Remaining: {_currentLives}");
        _onLifeLost?.Raise();
        
        if (_currentLives <= 0)
        {
            SetState(GameState.GameOver);
        }
        else
        {
            // Placeholder for respawn logic
            Debug.Log("[GameManager] Respawn logic would go here.");
            // For now, if we are not handling respawn, the game might just get stuck in a weird state 
            // but architectural requirement is to have this method.
        }
        }

        public void RestartLevelButton()
{
        int currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex;
        UnityEngine.SceneManagement.SceneManager.LoadScene(currentScene);
    }

    public void QuitButton()
    {
        Debug.LogWarning("Does not work in the Unity Editor!  You silly goose!");
        Application.Quit();
    }
}
