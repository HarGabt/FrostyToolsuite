using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;

namespace Frosty.Core
{
    /// <summary>
    /// Manages loading and swapping WPF ResourceDictionary language files at runtime.
    /// For now each application (ModManager, Editor) keeps its own language dictionary identified
    /// by the assembly name that hosts the Localization\Strings.*.xaml resources.
    /// </summary>
    public static class LocalizationManager
    {
        private static ResourceDictionary s_currentDictionary;
        private static readonly Dictionary<string, string[]> s_availableLocales = new Dictionary<string, string[]>();

        /// <summary>Gets the currently active BCP-47 locale tag, e.g. "en-US" or "ru-RU".</summary>
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
        /// <param name="locale">BCP-47 locale tag, e.g. "en-US" or "ru-RU".</param>
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
                // If requested locale not available, fall back to English
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

            // Keep .NET culture in sync with the chosen language.
            // So dates, numbers and anything reading CultureInfo.Current follow the language too.
            try
            {
                CultureInfo culture = CultureInfo.GetCultureInfo(locale);
                CultureInfo.DefaultThreadCurrentUICulture = culture;
                Thread.CurrentThread.CurrentUICulture = culture;

                // DANGEROUS: This WILL cause issue when parsing or writing
                // culture-sensitive data (e.g. dates, numbers) in some language (Known: Turkish).
                // Could be fixed by specifying CultureInfo.InvariantCulture
                // https://github.com/HarGabt/FrostyToolsuite/pull/51
                CultureInfo.DefaultThreadCurrentCulture = culture;
                Thread.CurrentThread.CurrentCulture = culture;
            }
            catch (CultureNotFoundException)
            {
                CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
                CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
                Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
                Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
            }

            // Apply flow direction per-language. A language can marks itself RTL by adding this to its Strings.xaml file.
            // <sys:Boolean x:Key="IsRightToLeft">true</sys:Boolean>
            // https://learn.microsoft.com/dotnet/desktop/wpf/advanced/bidirectional-features-in-wpf-overview
            bool rightToLeft = dict["IsRightToLeft"] is bool isRtl && isRtl;
            Application.Current.Resources["FlowDirection"] =
                rightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        }

        /// <summary>
        /// Picks the best available locale by detecting user's Windows language.
        /// Exact tag match first (e.g. "en-US"), then the same language (e.g. "en-GB" -> "en-US"), otherwise "en-US".
        /// Use this for first run default.
        /// </summary>
        public static string GetBestLocale(CultureInfo culture = null)
        {
            culture = culture ?? CultureInfo.CurrentUICulture;
            string[] available = { "en-US", "ru-RU" };

            foreach (string locale in available)
            {
                if (string.Equals(locale, culture.Name, StringComparison.OrdinalIgnoreCase))
                    return locale;
            }
            foreach (string locale in available)
            {
                int dash = locale.IndexOf('-');
                string firstPart = dash > 0 ? locale.Substring(0, dash) : locale;
                if (string.Equals(firstPart, culture.TwoLetterISOLanguageName, StringComparison.OrdinalIgnoreCase))
                    return locale;
            }
            return "en-US";
        }
    }
}
