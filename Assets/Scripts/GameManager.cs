using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameState CurrentState { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        SetState(GameState.AUTHENTICATION);
    }

    public void SetState(GameState newState)
    {
        GameState previousState = CurrentState;
        CurrentState = newState;

        Debug.Log($"[GameManager] Состояние изменено с {previousState} на {CurrentState}");

        switch (CurrentState)
        {
            case GameState.AUTHENTICATION:
                Time.timeScale = 1f;
                break;

            case GameState.MAIN_MENU:
                Time.timeScale = 1f;
                break;

            case GameState.DIALOGUE:
                Time.timeScale = 1f;
                break;

            case GameState.EXPLORATION:
                Time.timeScale = 1f;
                break;

            case GameState.PAUSE:
                Time.timeScale = 0f;
                break;
        }
    }
}
