using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Markup;
using MonitorDesk.Services;

namespace MonitorDesk;

// Keep service status identifiers and hardware data invariant; translate at the UI boundary.
internal static class L
{
    private static CultureInfo uiCulture = CultureInfo.CurrentUICulture;
    internal static CultureInfo UiCulture => uiCulture;
    internal static void UseCulture(CultureInfo culture) => uiCulture = culture;
    internal static bool Turkish => uiCulture.TwoLetterISOLanguageName == "tr";
    private static readonly Dictionary<string, string> TurkishText = new(StringComparer.Ordinal)
    {
        ["New profile"] = "Yeni profil",
        ["+ New profile"] = "+ Yeni profil",
        ["Edit profile"] = "Profili düzenle",
        ["Profile name"] = "Profil adı",
        ["Save"] = "Kaydet",
        ["Cancel"] = "İptal",
        ["Applying {0}…"] = "{0} uygulanıyor…",
        ["{0} saved monitor(s) are disconnected and were skipped."] = "Kayıtlı {0} monitör bağlı değil ve atlandı.",
        ["Delete"] = "Sil",
        ["Enter a unique profile name."] = "Benzersiz bir profil adı gir.",
        ["Select at least one control."] = "En az bir ayar seç.",
        ["Not included in this profile."] = "Bu profile dahil değil.",
        ["Percentages of each monitor's range. Unchecked controls stay unchanged. Saving does not apply the profile."] = "Değerler her monitörün ayar aralığının yüzdesidir. Seçilmeyen ayarlar değişmez. Kaydetmek profili uygulamaz.",
        ["Unavailable controls are skipped when applying."] = "Uygulama sırasında kullanılamayan ayarlar atlanır.",
        ["Saved values for disconnected monitors are kept."] = "Bağlı olmayan monitörlerin kayıtlı değerleri korunur.",
        ["Could not load custom profiles: "] = "Özel profiller okunamadı: ",
        ["Could not save custom profiles: "] = "Özel profiller kaydedilemedi: ",
        ["Profile saving is disabled in diagnostics."] = "Tanılama sırasında profil kaydetme kapalıdır.",
        ["Preferences"] = "Ayarlar",
        ["Theme, Eye comfort state and strength, and window position are saved automatically."] = "Tema, göz konforunun açık/kapalı durumu ve yoğunluğu ile pencere konumu otomatik kaydedilir.",
        ["Start with Windows in the system tray"] = "Windows ile sistem tepsisinde başlat",
        ["Starts when you sign in. Open the window from the tray icon."] = "Oturum açtığında başlar. Pencereyi tepsi simgesinden açabilirsin.",
        ["Close"] = "Kapat",
        ["Could not load preferences; defaults are in use: "] = "Ayarlar okunamadı; varsayılanlar kullanılıyor: ",
        ["Could not save preferences: "] = "Ayarlar kaydedilemedi: ",
        ["Could not read Windows startup preference: "] = "Windows başlangıç ayarı okunamadı: ",
        ["Could not change Windows startup preference: "] = "Windows başlangıç ayarı değiştirilemedi: ",
        ["Ready · Brightness and contrast apply automatically. Display modes require Preview."] = "Hazır · Parlaklık ve kontrast otomatik uygulanır. Ekran modları için Önizle kullan.",
        ["Monitor values read back. Device limits may affect the applied value."] = "Monitör değerleri yeniden okundu. Donanım sınırları uygulanan değeri etkileyebilir.",
        ["Applies automatically while dragging. Intermediate values are combined; the final value is retained."] = "Sürüklerken otomatik uygulanır. Ara değerler birleştirilir; son seçilen değer korunur.",
        ["Your workspace, in balance."] = "Çalışma alanın dengede.",
        ["Identify"] = "Tanımla",
        ["Refresh"] = "Yenile",
        ["Theme"] = "Tema",
        ["Lighting profiles"] = "Işık profilleri",
        ["Display settings"] = "Ekran ayarları",
        ["Results by monitor"] = "Monitör sonuçları",
        ["Finding displays…"] = "Ekranlar aranıyor…",
        ["Reading displays…"] = "Ekranlar okunuyor…",
        ["Eye comfort · Off"] = "Göz konforu · Kapalı",
        ["Eye comfort · On"] = "Göz konforu · Açık",
        ["Warm filter strength"] = "Sıcak filtre yoğunluğu",
        ["Strength: 40 / 90"] = "Yoğunluk: 40 / 90",
        ["Toggle a warm, click-through color filter on all screens. Independent of Windows Night light; brightness and contrast stay unchanged."] = "Tüm ekranlarda sıcak renk filtresini aç veya kapat. Windows Gece ışığından bağımsızdır; parlaklık ve kontrastı değiştirmez.",
        ["Reading displays and supported hardware controls…"] = "Ekranlar ve desteklenen donanım ayarları okunuyor…",
        ["Some controls did not respond. Last-known values are marked and disabled; refresh to retry."] = "Bazı ayarlar yanıt vermedi. Son bilinen değerler işaretlendi ve devre dışı bırakıldı; yeniden denemek için yenile.",
        ["Ready · Changes apply only when you choose Apply or Preview."] = "Hazır · Değişiklikler yalnızca Uygula veya Önizle seçildiğinde uygulanır.",
        ["Display discovery failed"] = "Ekranlar bulunamadı",
        ["Showing previous display information"] = "Önceki ekran bilgileri gösteriliyor",
        ["Could not refresh displays: "] = "Ekranlar yenilenemedi: ",
        ["No connected monitors were found."] = "Bağlı monitör bulunamadı.",
        ["Read-back failed: "] = "Doğrulama okuması başarısız: ",
        ["Profile application cancelled; completed changes remain applied."] = "Profil uygulaması iptal edildi; tamamlanan değişiklikler korunuyor.",
        ["Could not apply profile: "] = "Profil uygulanamadı: ",
        ["No active displays were found. Connect a screen and refresh."] = "Etkin ekran bulunamadı. Bir ekran bağlayıp yenile.",
        ["· PRIMARY"] = "· ANA EKRAN",
        ["· EXTENDED"] = "· GENİŞLETİLMİŞ",
        ["Refresh rate unavailable"] = "Tazeleme hızı okunamadı",
        ["Display mode"] = "Ekran modu",
        ["No compatible display modes available."] = "Uyumlu ekran modu bulunamadı.",
        ["Preview"] = "Önizle",
        ["Orientation"] = "Yönlendirme",
        ["Rotate"] = "Döndür",
        ["Preview orientation using the current resolution and refresh rate."] = "Geçerli çözünürlük ve tazeleme hızıyla yönlendirmeyi önizle.",
        ["15-second undo protection · Session only"] = "15 saniyelik geri alma koruması · Yalnızca bu oturum",
        ["Connect a second extended display to arrange your workspace."] = "Ekranları yerleştirmek için genişletilmiş modda ikinci bir ekran bağla.",
        ["this screen layout"] = "bu ekran yerleşimi",
        ["Layout unavailable: "] = "Yerleşim kullanılamıyor: ",
        ["Keep for this session"] = "Bu oturum için koru",
        ["Revert"] = "Geri al",
        ["Confirm display mode"] = "Ekran modunu onayla",
        ["Could not preview display mode: "] = "Ekran modu önizlenemedi: ",
        ["Contrast"] = "Kontrast",
        ["Brightness"] = "Parlaklık",
        ["Value unavailable. Refresh to retry."] = "Değer okunamadı. Yeniden denemek için yenile.",
        ["Apply "] = "Uygula: ",
        ["Last-known value. Refresh to reconnect."] = "Son bilinen değer. Yeniden bağlanmak için yenile.",
        ["Monitor did not respond. Refresh to reconnect."] = "Monitör yanıt vermedi. Yeniden bağlanmak için yenile.",
        ["Monitor did not respond. Retrying automatically."] = "Monitör yanıt vermedi. Otomatik yeniden deneniyor.",
        ["Monitor connection restored. Controls are ready."] = "Monitör bağlantısı yeniden kuruldu. Ayarlar hazır.",
        ["Some controls did not respond. Retrying automatically; last-known values remain disabled."] = "Bazı ayarlar yanıt vermedi. Otomatik yeniden deneniyor; son bilinen değerler devre dışı kalır.",
        ["Could not apply warm filter: "] = "Sıcak filtre uygulanamadı: ",
        ["Monilivo quick controls"] = "Monilivo hızlı ayarlar",
        ["Refresh displays"] = "Ekranları yenile",
        ["Toggle warm eye comfort filter"] = "Göz konforu filtresini aç veya kapat",
        ["No displays available. Refresh to retry."] = "Ekran bulunamadı. Yeniden denemek için yenile.",
        ["On"] = "Açık",
        ["Off"] = "Kapalı",
        ["enabled"] = "açık",
        ["disabled"] = "kapalı",
        ["Drag screens to match your desk. Arrow keys to adjust; Shift for precision."] = "Ekranları masandaki düzene göre sürükle. Ok tuşlarıyla ayarla; hassas hareket için Shift kullan.",
        ["Preview layout"] = "Yerleşimi önizle",
        ["Arrange your screens"] = "Ekranlarını yerleştir",
        ["Reset draft"] = "Taslağı sıfırla",
        ["\nPrimary"] = "\nAna ekran",
        [", primary"] = ", ana ekran",
        ["Ready · Confirm within 15 seconds to keep for this session."] = "Hazır · Bu oturumda korumak için 15 saniye içinde onayla.",
        ["Drag to arrange. Preview to apply."] = "Yerleştirmek için sürükle. Uygulamak için önizle.",
        ["Open"] = "Aç",
        ["Exit"] = "Çıkış",
        ["Night"] = "Gece",
        ["Normal"] = "Normal",
        ["High light"] = "Yüksek ışık",
        ["Landscape"] = "Yatay",
        ["Portrait"] = "Dikey",
        ["Landscape (flipped)"] = "Ters yatay",
        ["Portrait (flipped)"] = "Ters dikey",
        ["Unknown orientation"] = "Bilinmeyen yönlendirme",
        ["Built-in brightness · Windows WMI"] = "Dahili parlaklık · Windows WMI",
        ["Hardware controls · DDC/CI"] = "Donanım ayarları · DDC/CI",
        ["Hardware controls unavailable. Check DDC/CI in the monitor menu and your cable or dock."] = "Donanım ayarları kullanılamıyor. Monitör menüsündeki DDC/CI ayarını, kabloyu veya bağlantı istasyonunu kontrol et.",
        ["Connect at least two extended displays to arrange them."] = "Yerleştirmek için genişletilmiş modda en az iki ekran bağla.",
        ["Display configuration changed. Close this window and refresh."] = "Ekran yapılandırması değişti. Bu pencereyi kapatıp yenile.",
        ["Display positions are outside the supported range."] = "Ekran konumları desteklenen aralığın dışında.",
        ["Screens overlap. Drag them apart until their edges meet."] = "Ekranlar üst üste biniyor. Kenarları birleşene kadar ayır.",
        ["Leave no gaps: each screen must share an edge with the connected layout."] = "Boşluk bırakma: her ekran bağlı yerleşimle bir kenarı paylaşmalı.",
        ["Windows did not provide a usable source mode."] = "Windows kullanılabilir bir kaynak modu sağlamadı.",
        ["Windows returned inconsistent display configuration data."] = "Windows tutarsız ekran yapılandırması döndürdü.",
        ["Arrangement requires extended displays. Switch mirrored displays to Extend in Windows Settings."] = "Yerleşim için genişletilmiş ekranlar gerekir. Windows Ayarlarında çoğaltılan ekranları Genişlet olarak değiştir.",
        ["Windows denied access to the interactive display session. Open Monilivo directly from your Windows desktop and try again."] = "Windows ekran oturumuna erişimi reddetti. Monilivo’yu Windows masaüstünden açıp yeniden dene.",
        ["Display configuration is changing. Refresh and try again."] = "Ekran yapılandırması değişiyor. Yenileyip yeniden dene.",
        ["Displays changed while arranging. Close the editor and refresh."] = "Yerleştirme sırasında ekranlar değişti. Düzenleyiciyi kapatıp yenile.",
        ["Layout must preserve the connected displays, their sizes and the primary display."] = "Yerleşim, bağlı ekranları, boyutlarını ve ana ekranı korumalı.",
        ["Connected displays changed. Restore the layout in Windows Display Settings."] = "Bağlı ekranlar değişti. Windows Ekran Ayarlarında yerleşimi geri yükle.",
        ["Windows did not restore the previous layout."] = "Windows önceki yerleşimi geri yükleyemedi.",
        ["Windows did not apply the requested layout."] = "Windows istenen yerleşimi uygulayamadı.",
        ["Display is unavailable. Refresh and try again."] = "Ekran kullanılamıyor. Yenileyip yeniden dene.",
        ["Display disconnected. Refresh and try again."] = "Ekranın bağlantısı kesildi. Yenileyip yeniden dene.",
        ["Display configuration changed. Refresh and try again."] = "Ekran yapılandırması değişti. Yenileyip yeniden dene.",
        ["Display settings changed elsewhere. Refresh and try again."] = "Ekran ayarları başka bir yerde değişti. Yenileyip yeniden dene.",
        ["This mode is no longer supported. Refresh and try again."] = "Bu mod artık desteklenmiyor. Yenileyip yeniden dene.",
        ["Original display is disconnected. Restore its mode in Windows Settings after reconnecting."] = "Önceki ekranın bağlantısı kesildi. Yeniden bağladıktan sonra Windows Ayarlarında modunu geri yükle.",
        ["Windows did not restore the previous mode. Open Windows Display Settings to recover."] = "Windows önceki modu geri yükleyemedi. Düzeltmek için Windows Ekran Ayarlarını aç.",
        ["Windows did not apply the requested mode."] = "Windows istenen modu uygulayamadı.",
        ["This mode requires a Windows restart and cannot be previewed."] = "Bu mod Windows’un yeniden başlatılmasını gerektirir ve önizlenemez.",
        ["Windows rejected this display mode."] = "Windows bu ekran modunu reddetti.",
        ["Display mode kept for this Windows session."] = "Ekran modu bu Windows oturumu için korundu.",
        ["Previous display mode restored."] = "Önceki ekran modu geri yüklendi.",
        ["Could not restore display mode: "] = "Ekran modu geri yüklenemedi: ",
        ["The display is no longer available. Refresh and try again."] = "Ekran artık kullanılamıyor. Yenileyip yeniden dene.",
        ["Windows could not enumerate displays."] = "Windows ekranları listeleyemedi.",
        ["Monitor did not respond; last-known value is not current."] = "Monitör yanıt vermedi; son bilinen değer güncel değil.",
        ["Control unavailable."] = "Ayar kullanılamıyor.",
        ["Command sent; waiting for read-back."] = "Komut gönderildi; doğrulama okuması bekleniyor.",
        ["Current value could not be read. Refresh to verify."] = "Geçerli değer okunamadı. Doğrulamak için yenile.",
        ["Applied"] = "Uygulandı",
        ["Already set"] = "Zaten ayarlı",
        ["Skipped"] = "Atlandı",
        ["Failed"] = "Başarısız",
        ["Different value"] = "Farklı değer",
        ["Unverified"] = "Doğrulanamadı",
        ["Monilivo is already running"] = "Monilivo zaten açık",
        ["Monilivo is already running in the system tray.\n\nClick its tray icon to open the quick controls, or double-click it to restore the main window."] = "Monilivo zaten açık ve sistem tepsisinde çalışıyor.\n\nSistem tepsisindeki Monilivo simgesinden paneli açabilir veya simgeye çift tıklayarak ana pencereye dönebilirsin.",
        ["Apply {0}: brightness {1}%, contrast {2}% on all supported monitors. Unavailable controls are skipped."] = "Tüm desteklenen monitörlere {0} uygula: parlaklık %{1}, kontrast %{2}. Kullanılamayan ayarlar atlanır.",
        ["Apply {0} lighting profile to all monitors"] = "Tüm monitörlere {0} ışık profilini uygula",
        ["Reading current controls before applying {0}…"] = "{0} uygulanmadan önce geçerli ayarlar okunuyor…",
        ["{0} connected display(s)"] = "{0} bağlı ekran",
        ["DISPLAY {0:00}  {1}"] = "EKRAN {0:00}  {1}",
        ["Display {0} resolution"] = "Ekran {0} çözünürlüğü",
        ["Display {0} refresh rate"] = "Ekran {0} tazeleme hızı",
        ["Display {0} orientation"] = "Ekran {0} yönlendirmesi",
        ["Testing {0}…"] = "{0} deneniyor…",
        ["Keep {0}?"] = "{0} korunsun mu?",
        ["Reverting in {0} seconds unless you keep this mode."] = "Bu modu korumazsan {0} saniye içinde geri alınacak.",
        ["Display {0} {1}"] = "Ekran {0} {1}",
        ["Last known: {0} / {1} · Not current"] = "Son bilinen: {0} / {1} · Güncel değil",
        ["Applying {0} to display {1}…"] = "Ekran {1} için {0} uygulanıyor…",
        ["{0} command sent. Display values have been read back."] = "{0} komutu gönderildi. Ekran değerleri yeniden okunarak doğrulandı.",
        ["Could not update {0}: {1} Refresh before trying again."] = "{0} güncellenemedi: {1} Yeniden denemeden önce yenile.",
        ["Hardware range: {0}–{1}"] = "Donanım aralığı: {0}–{1}",
        ["Apply {0} to display {1}"] = "Ekran {1} için {0} uygula",
        ["Apply display {0} {1}"] = "Ekran {0} {1} ayarını uygula",
        ["Strength: {0} / 90"] = "Yoğunluk: {0} / 90",
        ["{0}: brightness {1}%, contrast {2}%"] = "{0}: parlaklık %{1}, kontrast %{2}",
        ["Display {0} · {1}"] = "Ekran {0} · {1}",
        ["Eye comfort · {0} · {1} / 90"] = "Göz konforu · {0} · {1} / 90",
        ["Warm filter {0}. Selected strength: {1} / 90. Click to toggle."] = "Sıcak filtre {0}. Seçili yoğunluk: {1} / 90. Açmak veya kapatmak için tıkla.",
        ["Display {0}: {1} × {2}; position {3}, {4}"] = "Ekran {0}: {1} × {2}; konum {3}, {4}",
        ["Display {0}{1}. Use arrow keys to move."] = "Ekran {0}{1}. Taşımak için ok tuşlarını kullan.",
        ["{0} applied · {1} already set · {2} skipped · {3} need attention"] = "{0} uygulandı · {1} zaten ayarlı · {2} atlandı · {3} kontrol edilmeli",
    };
    internal static string Get(string english) => Turkish && TurkishText.TryGetValue(english, out var value) ? value : english;
    internal static string Format(string english, params object?[] args) => string.Format(CultureInfo.CurrentCulture, Get(english), args);
    internal static string ProfileSummary(ProfileApplyResult result) => Format("{0} applied · {1} already set · {2} skipped · {3} need attention",
        result.Controls.Count(c => c.Status == "Applied"), result.Controls.Count(c => c.Status == "Already set"),
        result.Controls.Count(c => c.Status == "Skipped"), result.Controls.Count(c => c.Status is "Failed" or "Different value" or "Unverified"));
    private static readonly Dictionary<string, string> MarkupKeys = new()
    {
        ["Preferences"] = "Preferences",
        ["Tagline"] = "Your workspace, in balance.",
        ["Identify"] = "Identify",
        ["Refresh"] = "Refresh",
        ["Theme"] = "Theme",
        ["LightingProfiles"] = "Lighting profiles",
        ["DisplaySettings"] = "Display settings",
        ["MonitorResults"] = "Results by monitor",
        ["FindingDisplays"] = "Finding displays…",
        ["ReadingDisplays"] = "Reading displays…",
        ["EyeComfortOff"] = "Eye comfort · Off",
        ["WarmStrength"] = "Warm filter strength",
        ["InitialStrength"] = "Strength: 40 / 90",
        ["WarmTooltip"] = "Toggle a warm, click-through color filter on all screens. Independent of Windows Night light; brightness and contrast stay unchanged.",
    };
    internal static string Markup(string key) => Get(MarkupKeys[key]);

    // Known service messages stay English internally for diagnostics and status comparisons.
    // Only these exact templates are translated; external exception details pass through.
    internal static string Message(string message)
    {
        if (!Turkish) return message;
        if (TurkishText.TryGetValue(message, out var exact)) return exact;
        foreach (var (pattern, template) in ServiceTemplates)
        {
            var match = Regex.Match(message, pattern, RegexOptions.CultureInvariant);
            if (match.Success) return string.Format(CultureInfo.CurrentCulture, template,
                match.Groups.Cast<Group>().Skip(1).Select(g => (object)Get(g.Value switch { "night" => "Night", "normal" => "Normal", "high light" => "High light", "brightness" => "Brightness", "contrast" => "Contrast", _ => g.Value })).ToArray());
        }
        foreach (var prefix in new[] { "Could not restore display mode: " })
            if (message.StartsWith(prefix, StringComparison.Ordinal)) return Get(prefix) + Message(message[prefix.Length..]);
        // A display-mode description contains a numeric resolution and an orientation label.
        foreach (var name in new[] { "Landscape (flipped)", "Portrait (flipped)", "Landscape", "Portrait", "Unknown orientation" })
            if (message.EndsWith(" · " + name, StringComparison.Ordinal)) return message[..^name.Length] + Get(name);
        return message;
    }
    private static readonly (string Pattern, string Template)[] ServiceTemplates =
    [
        (@"^Current value is (\d+)\.$", "Geçerli değer: {0}."),
        (@"^Verified value: (\d+)\.$", "Doğrulanan değer: {0}."),
        (@"^Requested (\d+); monitor reports (\d+)\. Device presets or supported brightness steps may limit the value\.$", "İstenen: {0}; monitörün bildirdiği: {1}. Monitör profilleri veya desteklenen parlaklık adımları değeri sınırlayabilir."),
        (@"^Applying (night|normal|high light) · Display (\d+) (brightness|contrast)…$", "{0} uygulanıyor · Ekran {1} {2}…"),
        (@"^Windows display layout operation failed \(code (-?\d+)\)\.$", "Windows ekran yerleşimi işlemi başarısız (kod {0})."),
        (@"^Windows could not change the display mode \(code (-?\d+)\)\.$", "Windows ekran modunu değiştiremedi (kod {0})."),
        (@"^Windows rejected the brightness change \(code (\d+)\)\.$", "Windows parlaklık değişikliğini reddetti (kod {0})."),
        (@"^Monitor did not accept the setting \(Windows error (0x[0-9A-F]+)\)\. Check DDC/CI, the monitor's picture mode and other monitor-control apps, then refresh and retry\.$", "Monitör ayarı kabul etmedi (Windows hatası {0}). DDC/CI, monitörün görüntü modu ve diğer monitör kontrol uygulamalarını kontrol et; ardından yenileyip yeniden dene.")
    ];
}

[MarkupExtensionReturnType(typeof(string))]
public sealed class TrExtension : MarkupExtension
{
    public string Key { get; set; } = "";
    public override object ProvideValue(IServiceProvider serviceProvider) => L.Markup(Key);
}
