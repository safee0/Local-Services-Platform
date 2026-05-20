using System;

namespace LocalServicesPlatform.Services
{
    public class UserSession
    {
        // Notification action so the UI re-renders instantly on login state changes
        public event Action? OnSessionChanged;

        public int UserId { get; private set; }
        public string FullName { get; private set; } = string.Empty;
        public string Email { get; private set; } = string.Empty;
        public string Role { get; private set; } = string.Empty;
        public bool IsLoggedIn { get; private set; }

        public void Login(int userId, string fullName, string email, string role)
        {
            UserId = userId;
            FullName = fullName;
            Email = email;
            Role = role;
            IsLoggedIn = true;

            NotifyStateChanged();
        }

        public void Logout()
        {
            UserId = 0;
            FullName = string.Empty;
            Email = string.Empty;
            Role = string.Empty;
            IsLoggedIn = false;

            NotifyStateChanged();
        }

        private void NotifyStateChanged() => OnSessionChanged?.Invoke();
    }
}