using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;
using Application = UnityEngine.Application;

public class MainMenuUI : MonoBehaviour
{
    [Header("Профиль игрока")]
    public TMP_Text welcomeText;

    [Header("Кнопки для блокировки")]
    public Button newGameButton;
    public Button loadGameButton;

    private void Start()
    {
        if (AuthManager.Instance != null && AuthManager.Instance.CurrentUser != null)
        {
            string username = AuthManager.Instance.CurrentUser.username;
            welcomeText.color = Color.white;
            welcomeText.text = $"Вы вошли как: {username}";

            if (newGameButton != null) newGameButton.interactable = true;
            if (loadGameButton != null) loadGameButton.interactable = true;
        }
        else
        {
            welcomeText.color = Color.red;
            welcomeText.text = "ДОСТУП ЗАБЛОКИРОВАН: Авторизуйтесь!";

            if (newGameButton != null) newGameButton.interactable = false;
            if (loadGameButton != null) loadGameButton.interactable = false;
        }
    }

    public void OnNewGamePressed()
    {
        Debug.Log("[Главное меню] Запуск новой игры...");
        GameManager.Instance.SetState(GameState.DIALOGUE);
        SceneManager.LoadScene("GameplayScene");
    }

    public void OnLoadGamePressed()
    {
        Debug.Log("[Главное меню] Загрузка последнего сохранения...");
    }

    public void OnSettingsPressed()
    {
        Debug.Log("[Главное меню] Открытие панели настроек...");
    }

    public void OnExitPressed()
    {
        Debug.Log("[Главное меню] Закрытие приложения...");
        Application.Quit();
    }
}
