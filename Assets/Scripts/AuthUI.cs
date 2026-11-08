using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

public class AuthUI : MonoBehaviour
{
    [Header("Окна интерфейса")]
    public GameObject loginWindow;
    public GameObject registerWindow;

    [Header("Поля ввода окна Входа")]
    public TMP_InputField loginUsernameInput;
    public TMP_InputField loginPasswordInput;
    public TMP_Text loginErrorText;

    [Header("Поля ввода окна Регистрации")]
    public TMP_InputField regUsernameInput;
    public TMP_InputField regEmailInput;
    public TMP_InputField regPasswordInput;
    public TMP_Text regErrorText;

    private void Start()
    {
        GameManager.Instance.SetState(GameState.AUTHENTICATION);
        ShowLoginWindow();
    }

    public void ShowLoginWindow()
    {
        loginWindow.SetActive(true);
        registerWindow.SetActive(false);

        loginErrorText.text = "";
        regErrorText.text = "";

        loginUsernameInput.text = "";
        loginPasswordInput.text = "";
    }

    public void ShowRegisterWindow()
    {
        loginWindow.SetActive(false);
        registerWindow.SetActive(true);

        loginErrorText.text = "";
        regErrorText.text = "";

        regUsernameInput.text = "";
        regEmailInput.text = "";
        regPasswordInput.text = "";
    }

    public void OnLoginButtonPressed()
    {
        loginErrorText.text = "";

        if (string.IsNullOrEmpty(loginUsernameInput.text) || string.IsNullOrEmpty(loginPasswordInput.text))
        {
            loginErrorText.color = Color.red;
            loginErrorText.text = "Ошибка: Заполните все поля!";
            return;
        }

        bool success = AuthManager.Instance.Login(loginUsernameInput.text, loginPasswordInput.text);

        if (success)
        {
            loginErrorText.color = Color.green;
            loginErrorText.text = "Успешный вход! Переход в игру...";
            GameManager.Instance.SetState(GameState.MAIN_MENU);
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }
        else
        {
            loginErrorText.color = Color.red;
            loginErrorText.text = "Ошибка: Неверный логин или пароль!";
        }
    }

    public void OnRegisterButtonPressed()
    {
        regErrorText.text = "";

        if (string.IsNullOrEmpty(regUsernameInput.text) || string.IsNullOrEmpty(regEmailInput.text) || string.IsNullOrEmpty(regPasswordInput.text))
        {
            regErrorText.color = Color.red;
            regErrorText.text = "Ошибка: Все поля обязательны!";
            return;
        }

       
        if (!regEmailInput.text.Contains("@"))
        {
            regErrorText.color = Color.red;
            regErrorText.text = "Ошибка: Некорректный формат почты (отсутствует '@')!";
            return;
        }

        bool success = AuthManager.Instance.Register(regUsernameInput.text, regEmailInput.text, regPasswordInput.text);

        if (success)
        {
            regErrorText.color = Color.green;
            regErrorText.text = "Регистрация успешна! Перейдите к входу.";

            regUsernameInput.text = "";
            regEmailInput.text = "";
            regPasswordInput.text = "";
        }
        else
        {
            regErrorText.color = Color.red;
            regErrorText.text = "Ошибка регистрации! Проверьте данные.";
        }
    }
}
