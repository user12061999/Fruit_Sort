namespace HAVIGAME {

    public enum LogLevel : byte {
        Debug,
        Info,
        Warning,
        Error,
    }

    public static class Log {
        private const string logInfoFormat = "<color=#00FF00>{0}</color>";
        private const string logWarningFormat = "<color=#FFFF00>{0}</color>";
        private const string logErrorFormat = "<color=#FF0000>{0}</color>";
        private static LogLevel logLevel = LogLevel.Debug;
        private static bool debugEnabled = true;
        private static bool infoEnabled = true;
        private static bool warningEnabled = true;
        private static bool errorEnabled = true;

        public static LogLevel LogLevel => logLevel;
        public static bool DebugEnabled => debugEnabled;
        public static bool InfoEnabled => infoEnabled;
        public static bool WarningEnabled => warningEnabled;
        public static bool ErrorEnabled => errorEnabled;

        public static void SetLogLevel(LogLevel logLevel) {
            Log.logLevel = logLevel;

            switch (logLevel) {
                case LogLevel.Debug:
                    debugEnabled = true;
                    infoEnabled = true;
                    warningEnabled = true;
                    errorEnabled = true;
                    break;
                case LogLevel.Info:
                    debugEnabled = false;
                    infoEnabled = true;
                    warningEnabled = true;
                    errorEnabled = true;
                    break;
                case LogLevel.Warning:
                    debugEnabled = false;
                    infoEnabled = false;
                    warningEnabled = true;
                    errorEnabled = true;
                    break;
                case LogLevel.Error:
                    debugEnabled = false;
                    infoEnabled = false;
                    warningEnabled = false;
                    errorEnabled = true;
                    break;
                default:
                    break;
            }
        }

        public static void Debug(string messsge) {
            if (DebugEnabled) LogMessage(LogLevel.Debug, messsge);
        }

        public static void Debug<T>(string format, T arg) {
            if (DebugEnabled) LogMessage(LogLevel.Debug, Utility.Text.Format(format, arg));
        }

        public static void Debug<T1, T2>(string format, T1 arg1, T2 arg2) {
            if (DebugEnabled) LogMessage(LogLevel.Debug, Utility.Text.Format(format, arg1, arg2));
        }

        public static void Debug<T1, T2, T3>(string format, T1 arg1, T2 arg2, T3 arg3) {
            if (DebugEnabled) LogMessage(LogLevel.Debug, Utility.Text.Format(format, arg1, arg2, arg3));
        }


        public static void Info(string messsge) {
            if (InfoEnabled) LogMessage(LogLevel.Info, messsge);
        }

        public static void Info<T>(string format, T arg) {
            if (InfoEnabled) LogMessage(LogLevel.Info, Utility.Text.Format(format, arg));
        }

        public static void Info<T1, T2>(string format, T1 arg1, T2 arg2) {
            if (InfoEnabled) LogMessage(LogLevel.Info, Utility.Text.Format(format, arg1, arg2));
        }

        public static void Info<T1, T2, T3>(string format, T1 arg1, T2 arg2, T3 arg3) {
            if (InfoEnabled) LogMessage(LogLevel.Info, Utility.Text.Format(format, arg1, arg2, arg3));
        }


        public static void Warning(string messsge) {
            if (WarningEnabled) LogMessage(LogLevel.Warning, messsge);
        }

        public static void Warning<T>(string format, T arg) {
            if (WarningEnabled) LogMessage(LogLevel.Warning, Utility.Text.Format(format, arg));
        }

        public static void Warning<T1, T2>(string format, T1 arg1, T2 arg2) {
            if (WarningEnabled) LogMessage(LogLevel.Warning, Utility.Text.Format(format, arg1, arg2));
        }

        public static void Warning<T1, T2, T3>(string format, T1 arg1, T2 arg2, T3 arg3) {
            if (WarningEnabled) LogMessage(LogLevel.Warning, Utility.Text.Format(format, arg1, arg2, arg3));
        }

        public static void Error(string messsge) {
            if (ErrorEnabled) LogMessage(LogLevel.Error, messsge);
        }

        public static void Error<T>(string format, T arg) {
            if (ErrorEnabled) LogMessage(LogLevel.Error, Utility.Text.Format(format, arg));
        }

        public static void Error<T1, T2>(string format, T1 arg1, T2 arg2) {
            if (ErrorEnabled) LogMessage(LogLevel.Error, Utility.Text.Format(format, arg1, arg2));
        }

        public static void Error<T1, T2, T3>(string format, T1 arg1, T2 arg2, T3 arg3) {
            if (ErrorEnabled) LogMessage(LogLevel.Error, Utility.Text.Format(format, arg1, arg2, arg3));
        }

        private static void LogMessage(LogLevel level, string message) {
            switch (level) {
                case LogLevel.Debug:
                    UnityEngine.Debug.Log(message);
                    break;

                case LogLevel.Info:
                    UnityEngine.Debug.Log(Utility.Text.Format(logInfoFormat, message));
                    break;

                case LogLevel.Warning:
                    UnityEngine.Debug.LogWarning(Utility.Text.Format(logWarningFormat, message));
                    break;

                case LogLevel.Error:
                    UnityEngine.Debug.LogError(Utility.Text.Format(logErrorFormat, message));
                    break;
            }
        }
    }
}
