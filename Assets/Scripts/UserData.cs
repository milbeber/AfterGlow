using System;

[Serializable]
public class UserData
{
    public string username;       // Логин игрока
    public string email;          // Электронная почта игрока
    public string passwordHash;   // Зашифрованный пароль
    public int gamesPlayed;       // Сколько игр сыграно (для статистики)
    public string lastLoginDate;  // Дата последнего входа
}
