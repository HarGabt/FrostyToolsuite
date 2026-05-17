using System;
using System.Windows;

namespace Frosty.Core
{
    /// <summary>
    /// Manages loading and swapping WPF ResourceDictionary language files at runtime.
    /// Each application (ModManager, Editor) keeps its own language dictionary identified
    /// by the assembly name that hosts the Localization\Strings.*.xaml resources.
    /// </summary>
    public static class LocalizationManager
    {
        private static ResourceDictionary s_currentDictionary;

        /// <summary>Gets the currently active BCP-47 locale tag, e.g. "en-US" or "de-DE".</summary>
        public static string CurrentLanguage { get; private set; } = "en-US";

        /// <summary>
        /// Loads the language ResourceDictionary for <paramref name="locale"/> from
        /// <paramref name="assemblyName"/> and makes it active in
        /// <see cref="Application.Current"/> resources.
        /// Falls back to "en-US" if the requested locale file is not found.
        /// </summary>
        /// <param name="assemblyName">
        /// Short assembly name that contains the
        /// <c>Localization/Strings.{locale}.xaml</c> files, e.g. "FrostyModManager".
        /// </param>
        /// <param name="locale">BCP-47 locale tag, e.g. "en-US" or "de-DE".</param>
        public static void SetLanguage(string assemblyName, string locale)
        {
            string uri = $"pack://application:,,,/{assemblyName};component/Localization/Strings.{locale}.xaml";

            ResourceDictionary dict;
            try
            {
                dict = new ResourceDictionary { Source = new Uri(uri, UriKind.Absolute) };
            }
            catch
            {
                // Requested locale not available — fall back to English.
                if (!string.Equals(locale, "en-US", StringComparison.OrdinalIgnoreCase))
                    SetLanguage(assemblyName, "en-US");
                return;
            }

            var merged = Application.Current.Resources.MergedDictionaries;

            if (s_currentDictionary != null)
                merged.Remove(s_currentDictionary);

            s_currentDictionary = dict;
            merged.Add(dict);
            CurrentLanguage = locale;
        }
    }
}
