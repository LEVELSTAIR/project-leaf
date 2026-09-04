namespace Arborvale.Shared
{
    /// <summary>
    /// Service locator for online features. Null in demo/offline builds.
    /// Gameplay code (Assembly-CSharp) checks IsAvailable before calling backend APIs.
    /// Arborvale.Online registers the real implementation on startup;
    /// nothing registers it in demo/offline builds, so it stays null.
    /// </summary>
    public static class OnlineServices
    {
        public static IOnlineServices Instance { get; private set; }

        public static bool IsAvailable => Instance != null && Instance.IsAvailable;

        public static void Register(IOnlineServices services)
        {
            Instance = services;
        }

        public static void Unregister()
        {
            Instance = null;
        }
    }
}
