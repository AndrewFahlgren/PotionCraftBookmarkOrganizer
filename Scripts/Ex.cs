using PotionCraftBookmarkOrganizer.Scripts.Storage;
using System;
using System.Collections.Generic;
using System.Text;

namespace PotionCraftBookmarkOrganizer.Scripts
{
    /// <summary>
    /// Exception Handler
    /// </summary>
    public static class Ex
    {
        /// <summary>
        /// Runs the given code inside of a try catch.
        /// </summary>
        /// <param name="action">the code to run.</param>
        /// <param name="errorAction">optional. Runs this code on error if provided.</param>
        /// <returns>true on error unless an errorAction is specified</returns>
        public static bool RunSafe(Func<bool> action, Func<bool> errorAction = null, bool logErrorToStorage = false)
        {
            try
            {
                return action();
            }
            catch (Exception ex)
            {
                LogException(ex, logErrorToStorage);
            }
            if (errorAction == null)
                return true;
            return errorAction();
        }


        /// <summary>
        /// Runs the given code inside of a try catch.
        /// </summary>
        /// <param name="action">the code to run.</param>
        /// <param name="errorAction">optional. Runs this code on error if provided.</param>
        /// <returns>true on error unless an errorAction is specified</returns>
        public static void RunSafe(Action action, Action errorAction = null, bool logErrorToStorage = false)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                LogException(ex, logErrorToStorage);
                if (errorAction != null) errorAction();
            }
        }

        public static void LogException(Exception ex, bool logErrorToStorage = false)
        {
            var exceptionText = GetExceptionText(ex);
            Plugin.PluginLogger.LogError(exceptionText);
            if (logErrorToStorage)
            {
                StaticStorage.ErrorLog.Add(exceptionText);
            }
        }

        public static string GetExceptionText(Exception ex)
        {
            return $"{DateTime.UtcNow}: {ex.GetType()}: {ex.Message}\r\n{ex.StackTrace}\r\n{ex.InnerException?.Message}";
        }
    }
}
