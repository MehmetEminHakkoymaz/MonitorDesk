using System.Globalization;
using MonitorDesk;
using MonitorDesk.Services;

internal static class LocalizationChecks
{
    internal static void Run(Action<bool, string> check)
    {
        var original = CultureInfo.CurrentUICulture;
        var originalLanguage = L.UiCulture;
        try
        {
            foreach (var locale in new[] { "tr-TR", "tr-CY" })
            {
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(locale);
                L.UseCulture(CultureInfo.CurrentUICulture);
                check(L.Get("Brightness") == "Parlaklık" && L.Get("Open") == "Aç", locale + " uses Turkish controls and tray menu");
                check(L.Format("Apply {0} to display {1}", L.Get("Contrast"), 3) == "Ekran 3 için Kontrast uygula", locale + " preserves and reorders placeholders");
                check(L.Message("Applying high light · Display 2 contrast…") == "Yüksek ışık uygulanıyor · Ekran 2 Kontrast…", locale + " translates service progress and invariant names");
                check(L.Message("Windows could not change the display mode (code -2).") == "Windows ekran modunu değiştiremedi (kod -2).", locale + " preserves native error codes");
                check(L.Message("1920 × 1080 · 120 Hz · Landscape (flipped)") == "1920 × 1080 · 120 Hz · Ters yatay", locale + " translates mode description without changing timing");
                check(L.Message("OMEN 25i: vendor error 42") == "OMEN 25i: vendor error 42", locale + " preserves unknown vendor details");
                check(L.ProfileSummary(new ProfileApplyResult([], null, null)) == "0 uygulandı · 0 zaten ayarlı · 0 atlandı · 0 kontrol edilmeli", locale + " localizes profile summary without changing status identifiers");
                check(new TrExtension { Key = "Identify" }.ProvideValue(null!) as string == "Tanımla", locale + " resolves XAML translation");
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
                check(L.Get("Display mode") == "Ekran modu", locale + " keeps selected language when callback culture differs");
            }
            foreach (var locale in new[] { "en-US", "de-DE" })
            {
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(locale);
                L.UseCulture(CultureInfo.CurrentUICulture);
                check(L.Get("Brightness") == "Brightness" && L.Get("Exit") == "Exit", locale + " uses English fallback");
                check(L.Message("Windows could not change the display mode (code -2).") == "Windows could not change the display mode (code -2).", locale + " preserves English errors");
            }
        }
        finally { CultureInfo.CurrentUICulture = original; L.UseCulture(originalLanguage); }
    }
}
