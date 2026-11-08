using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using Debug = UnityEngine.Debug;


public class AuthManager : MonoBehaviour
{
    
    public static AuthManager Instance { get; private set; }

  
    public UserData CurrentUser { get; private set; }

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

  
    public bool Register(string username, string email, string password)
    {
        
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            Debug.LogError("[Регистрация] Ошибка: Все поля (Логин, Email, Пароль) обязательны для заполнения!");
            return false;
        }

    
        if (password.Length < 6)
        {
            Debug.LogError("[Регистрация] Ошибка: Пароль должен быть не менее 6 символов!");
            return false;
        }

       
        if (!email.Contains("@"))
        {
            Debug.LogError("[Регистрация] Ошибка: Некорректный формат адреса электронной почты!");
            return false;
        }

      
        string userFolderPath = Path.Combine(Application.persistentDataPath, "Saves", username);
        string filePath = Path.Combine(userFolderPath, "user_profile.json");

       
        if (File.Exists(filePath))
        {
            Debug.LogError("[Регистрация] Ошибка: Пользователь с таким логином уже зарегистрирован!");
            return false;
        }

      
        string encryptedPassword = HashPassword(password);

        UserData newPlayer = new UserData
        {
            username = username,
            email = email, 
            passwordHash = encryptedPassword,
            gamesPlayed = 0,
            lastLoginDate = DateTime.Now.ToString("dd.MM.yyyy HH:mm")
        };

      
        try
        {
            Directory.CreateDirectory(userFolderPath); 
            string jsonText = JsonUtility.ToJson(newPlayer, true); 
            File.WriteAllText(filePath, jsonText); 

            Debug.Log($"[Регистрация] Успех! Создан профиль для пользователя {username}. Путь: {filePath}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Регистрация] Критическая ошибка при записи файла профиля: {ex.Message}");
            return false;
        }
    }

    
    public bool Login(string username, string password)
    {
        
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            Debug.LogError("[Вход] Ошибка: Поля логина и пароля не могут быть пустыми!");
            return false;
        }

       
        string filePath = Path.Combine(Application.persistentDataPath, "Saves", username, "user_profile.json");

        if (!File.Exists(filePath))
        {
            Debug.LogError("[Вход] Ошибка: Пользователь с таким логином не найден!");
            return false;
        }

        try
        {
           
            string jsonText = File.ReadAllText(filePath);
            UserData loadedUser = JsonUtility.FromJson<UserData>(jsonText);

            
            string enteredPasswordHash = HashPassword(password);

            if (loadedUser.passwordHash == enteredPasswordHash)
            {
                
                CurrentUser = loadedUser;
                CurrentUser.lastLoginDate = DateTime.Now.ToString("dd.MM.yyyy HH:mm");

             
                File.WriteAllText(filePath, JsonUtility.ToJson(CurrentUser, true));

                Debug.Log($"[Вход] Авторизация успешна! Добро пожаловать в игру, {CurrentUser.username}!");
                return true;
            }
            else
            {
                Debug.LogError("[Вход] Ошибка: Введен неверный пароль!");
                return false;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Вход] Критическая ошибка при чтении файла профиля: {ex.Message}");
            return false;
        }
    }

  
    private string HashPassword(string password)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                builder.Append(bytes[i].ToString("x2"));
            }
            return builder.ToString(); 
        }
    }
}
