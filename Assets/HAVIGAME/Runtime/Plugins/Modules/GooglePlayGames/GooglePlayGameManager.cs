using System.Collections;
using System.Collections.Generic;

#if GOOGLE_PLAY_GAMES
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using HAVIGAME.Services.Auth;
using UnityEngine;
#endif

namespace HAVIGAME.Plugins.GooglePlayGame {
    public static class GooglePlayGameManager {
        public const string DEFINE_SYMBOL = "GOOGLE_PLAY_GAMES";

        public static InitializeEvent initializeEvent = new InitializeEvent();

        private static string authorizationCode;

        public static bool IsInitialized => initializeEvent.IsInitialized;
        public static string GetAuthorizationCode => authorizationCode;

        public static void Initialize() {
#if GOOGLE_PLAY_GAMES
            if (initializeEvent.IsRunning) {
                Log.Warning("[GooglePlayGames] GooglePlayGames is running with initialize state {0}.", IsInitialized);
                return;
            }

            PlayGamesPlatform.Activate();

            Log.Info("[GooglePlayGames] Initialize completed.");
            initializeEvent.Invoke(true);
#endif
        }

#if GOOGLE_PLAY_GAMES
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void RegisterModule() {
            GameManager.RegisterModule<Initializer>();
        }

        public class Initializer : ModuleInitializer {

            public override int Order => SERVICE;
            public override InitializeEvent InitializeEvent => GooglePlayGameManager.initializeEvent;

            public override void Initialize() {
                GooglePlayGameManager.Initialize();
            }
        }
#endif
    }
}
