# Monilivo

Önceki adı **MonitorDesk**. Mevcut ayarlar ve özel profiller uyumluluk için `%LOCALAPPDATA%\MonitorDesk` klasöründe korunur. Kurulum aynı güncelleme kimliğini kullanır; eski kısayolları ve başlangıç komutlarını `Monilivo.exe` için günceller. Kaynak klasörler ve GitHub deposu mevcut adlarını korur. Yerel geliştirmede **Run-Monilivo.cmd** dosyasını kullan; eski **Run-MonitorDesk.cmd** de yeni uygulamayı açmaya devam eder.

[English](README.md) | **Türkçe**

Her ekranı kendine göre ayarla. Monilivo, ekran bilgilerini görüntülemek ve desteklenen monitörlerin donanımsal parlaklık ve kontrast ayarlarını değiştirmek için geliştirilmiş hafif bir Windows masaüstü uygulamasıdır.

**Geçici proje adı · v0.17.0 (geliştirme) · Yalnızca Windows**

## Kurulum

Kurulum paketleri [GitHub Releases](https://github.com/MehmetEminHakkoymaz/MonitorDesk/releases) sayfasına dosya olarak eklenebilir. Yayınlanan son sürümdeki setup EXE dosyasını indirip çalıştır. Güncel yerel derleme **Monilivo-Setup-0.17.0-win-x64.exe** dosyasını oluşturur; bu dosya ayrıca sürüm eki olarak yüklenmelidir. GitHub’ın kaynak kod ZIP dosyası kurulum paketi değildir.

Kurulum, Monilivo’yu Windows kullanıcı hesabına yükler; masaüstü ve Başlat menüsü kısayollarını oluşturur ve Windows Ayarları’na kaldırma kaydı ekler. .NET Windows Desktop çalışma ortamı uygulamayla birlikte gelir; hedef bilgisayarda ayrıca .NET kurulması veya internet bağlantısı gerekmez. Yönetici yetkisi istenmez. Paket, x64 uyumlu Windows 10 (19041 ve üzeri) ve Windows 11 içindir; monitör sürücüsü içermez. Daha yeni kurulum dosyası mevcut kurulumu günceller.

## Özellikler

Uygulama dosyası, ana pencere ve sistem tepsisi artık monitör/ışık simgesini kullanır. Yeni kurulumlarda masaüstü/Başlat kısayolları ve kurulum EXE’si de bu simgeyi gösterir. Düzenlenebilir vektör kaynak `src/MonitorDesk/Assets/MonitorDesk.svg` dosyasındadır; dokuz boyutlu Windows simgesini `powershell.exe -NoProfile -ExecutionPolicy Bypass -File tools/Build-Icon.ps1` ile yeniden oluşturabilirsin. Sabitlenmiş kısayollar veya Windows simge önbelleği, kısayol yeniden oluşturulana kadar eski simgeyi gösterebilir.

- Bağlı ekranları, geçerli çözünürlüklerini, tazeleme hızlarını ve birincil ekran durumunu gösterir.
- Ekranları geçici numara etiketleriyle tanımlar.
- Harici monitörlerin parlaklık ve kontrast değerlerini Windows DDC/CI API’leriyle okur ve değiştirir.
- Uyumlu dahili panellerde Windows WMI üzerinden parlaklık ayarı sunar.
- Kullanılamayan kontrolleri açıkça belirtir; sahte değer veya yazılımsal karartma kullanmaz.
- Ekran yapılandırması değiştiğinde bilgileri yeniler.
- Açık/koyu tema, monitör başına DPI farkındalığı ve klavyeyle erişilebilir kontroller sunar.
- Ana pencere ve tepsi panelindeki kaydırıcılarla parlaklık ve kontrastı doğrudan ayarlar; sürükleme sırasında değişiklik otomatik uygulanır.
- Desteklenen çözünürlük ve tazeleme hızlarını 15 saniyelik onay süresiyle önizler.
- Yatay, dikey, ters yatay ve ters dikey yönleri önizler.
- Genişletilmiş ekranları sürükleyerek konumlandırmayı, kenar hizalamayı ve süre sonunda geri dönmeyi destekler.
- Gece, normal ve yüksek ışık parlaklık/kontrast profillerini desteklenen tüm monitörlere uygular.

Ana pencere, yerleşim düzenleyicisini ve kompakt ekran kartlarını bir araya getirir. Kartlar pencere genişliğine göre bir, iki veya üç sütuna yerleşir; açılır menüler açık/koyu temaya uyum sağlar. Windows’taki ekran düzeni değişmediyse yerleşim taslağı yenileme sırasında korunur; yapılandırma değişirse güncel düzen yüklenir.

Pencere büyütüldüğünde çalışma alanı ortalanır ve genişliği 1320 pikselle sınırlanır. Işık profilleri geniş pencerede ekran yerleşiminin yanında, dar pencerede altında görünür. Gece, normal ve yüksek ışık için doluluğu artan üç güneş simgesi kullanılır; profil değerleri araç ipuçlarında bulunur. Profil alanında yalnızca işlem sırasında ilerleme ve sonuç mesajları gösterilir. Kaydırıcılar ve kaydırma çubukları seçilen temaya uyum sağlar.

### Ayarlar ve Windows ile başlatma

Üst araç çubuğundan **Ayarlar** düğmesini aç. Tema, göz konforu yoğunluğu, pencerenin konumu, boyutu ve tam ekran durumu otomatik olarak `%LOCALAPPDATA%\MonitorDesk\settings.json` dosyasına kaydedilir ve sonraki açılışta geri yüklenir. Önceden kullanılan monitör artık bağlı değilse pencere bağlı bir ekranın görünür alanına alınır. Göz konforu başlangıçta kapalıdır; seçtiğin yoğunluk hatırlanır.

**Windows ile sistem tepsisinde başlat** seçeneğini açarsan Windows hesabında oturum açıldığında Monilivo, ana pencereyi göstermeden tepside başlar. Varsayılan olarak kapalıdır; aynı ekrandan kapatabilirsin. Bu seçenek mevcut uygulama dosyasını hesabının Windows başlangıç kayıtlarına ekler; dosya yolunun sabit kalması için kurulu uygulamadan etkinleştir. Ana pencereyi açmak için tepsi simgesine çift tıkla. Uygulama kaldırıldığında o kuruluma ait başlangıç kaydı temizlenir.

Geliştirme sürümünde 171 otomatik kontrol geçti: ayarların kaydedilip okunması, geçersiz veriler, bağlantısı kesilen ekranların koordinatları ve taklit edilmiş kayıt defteri üzerinden başlangıç komutları sınandı. Türkçe/İngilizce ayarlar ekranları ile gizli başlangıç ve tepsiden geri açma davranışı kontrol edildi. Gerçek Windows oturum açılışı, yeniden açılışta ayarların korunması ve farklı DPI değerlerinde konum geri yükleme manuel test edilmelidir. Tanılama çalıştırmaları kullanıcı ayarlarını kaydetmez ve otomatik başlangıcı etkinleştirmez.

### Sistem tepsisi

Uygulama açılışta Windows **görüntüleme dilini** kullanır: Türkçe dil ayarlarında Türkçe, diğer dillerde İngilizce açılır. Ana pencere, tepsi paneli ve menüsü, ışık profilleri, araç ipuçları, ekran önizleme pencereleri ve uygulamanın durum mesajları aynı dili kullanır. Monitör adları ve Windows/üreticiden gelen harici hata ayrıntıları aynen korunur. Windows görüntüleme dilini değiştirdikten sonra Monilivo’yu yeniden başlat; yalnızca bölgesel tarih/sayı biçimini değiştirmek arayüz dilini değiştirmez.

Her Windows kullanıcısı/oturumunda tek normal Monilivo örneği çalışır. Uygulama açıkken veya tepside gizliyken yeniden çalıştırmak, zaten açık olduğunu bildiren uyarıyı gösterir ve ikinci kopyayı kapatır. Gerçek çıkış kilidi bırakır; süreç sonlandırıldığında kalıcı kilit kalmaz. Tanılama, görüntü alma ve yaşam döngüsü kontrolleri bu başlangıç kilidinden bağımsızdır. Özelliği denemeden önce eski sürümü tepsiden Çıkış ile kapat; v0.10.2 öncesi sürümler bu kilidi kullanmaz.

Tepsi simgesine tek sol tık, kompakt hızlı ayar panelini açıp kapatır. Monitörler alt alta sıralanır; her birinde otomatik uygulanan tek satırlık parlaklık/kontrast kaydırıcıları bulunur. Durum satırının üzerinde tam mesaj araç ipucunda görünür. Monitörlerin altında üç ışık profili, onların altında göz simgeli **Eye comfort · On/Off** düğmesi yer alır. Panel, ana pencereyle aynı monitör servisini, işlem durumunu ve sıcak filtreyi kullanır. Dışarı tıklamak veya Escape paneli gizler. Genişliği 320 mantıksal piksel, maksimum yüksekliği 560 pikseldir; gerektiğinde kaydırılabilir. Tıklanan ekranda görev çubuğunun üstüne yerleşir; çift tık tam pencereyi açmaya devam eder. Küçültülen v0.10.1 sürümünün derlemesi ve masaüstü aç/gizle/geri aç/çıkış kontrolü geçti; üç monitörlü görünüm görsel olarak incelendi.

Ana pencereyi kapatmak Monilivo’yu Windows bildirim alanına gizler. Tepsi simgesine çift tıklayarak veya sağ tık menüsündeki **Aç** seçeneğiyle pencereyi geri getir. **Çıkış**, uygulamayı tamamen kapatır ve sıcak renk filtresini kaldırır. Pencere gizliyken filtre ve ekran değişikliği takibi çalışmaya devam eder. Küçült düğmesi normal küçültme davranışını korur; onaylanmamış ekran önizlemesi varsa gizlenmeden veya çıkmadan önce geri alınır. Windows oturumu kapatıldığında uygulama da kapanır. Tepsi simgesi oluşturulamazsa kapat düğmesi uygulamadan çıkar; erişilemez bir arka plan süreci bırakılmaz.

v0.9.0 için 121 otomatik/salt okunur kontrol ve masaüstünde kapatınca gizleme, geri açma ve gerçek süreç çıkışını doğrulayan kontrol geçti. Sağ tık menüsü, filtrenin tepside korunması ve Explorer yeniden başlatma davranışının manuel testi henüz yapılmadı.

### Otomatik kaydırıcılar

Daha önce okunabilen parlaklık/kontrast kontrolleri yanıt vermemeye başlarsa uygulama tepsideyken de otomatik yeniden denenir. Her seferinde yalnızca sorunlu bir monitörün güncel olmayan kontrolleri okunur: ilk deneme 5 saniye sonra, sonraki denemeler başarısız okuma tamamlandıktan 15, 30 ve 60 saniye sonra yapılır. Sağlıklı ve desteklenmeyen kontroller sürekli sorgulanmaz. Kaydırıcı kullanımı, ayar yazma, profil ve ekran önizlemesi sırasında denemeler ertelenir; toparlanma okuması sırasında komutların çakışmaması için ayar kontrolleri kısa süreliğine devre dışı bırakılır. Başarılı okumada güncel değerler ve kaydırıcılar geri gelir; o monitör için denemeler durur. Manuel Yenile kullanılmaya devam edilebilir. Takılan bir sürücü çağrısı toparlanmayı ve sonraki işlemleri geciktirebilir.

v0.14.0 geliştirme sürümünde donanıma yazma yapmadan 181 otomatik kontrol geçti. Artan bekleme aralıkları, toparlanma/bağlantı kesilmesi temizliği ve monitörlerin bağımsız deneme zamanları sınandı. OMEN monitörün gerçek toparlanma davranışı kullanıcı tarafından test edilmelidir.

İlk parlaklık/kontrast değişikliği donanım kuyruğuna bekletilmeden alınır. Sürükleme sırasında her monitör/ayar için en az 250 ms aralıklarla son istenen değer gönderilir; yeni hareketler gönderim zamanını ötelemek yerine bekleyen ara değeri değiştirir. Komutlar mevcut DDC toparlanma süreleriyle sırayla gönderilir; meşgul veya yavaş bir monitör bu aralığı uzatabilir. Kaydırıcı bırakıldığında son seçilen değer kuyrukta korunur. Ana pencere ve tepsi aynı kuyruğu kullanır. Yazma sırasında kaydırıcılar kullanılabilir; hareket durulduğunda yeniden okunan gerçek değer gösterilir, dolayısıyla donanımın desteklediği adımlar istenen değerden farklı olabilir. Hata durumunda bekleyen değişiklikler durdurulur ve ayrıntı gösterilir; kullanılamayan veya güncel olmayan kontroller kapalı kalır. Uygulamadan gerçek çıkış, gönderilmemiş değişiklikleri iptal eder. Çözünürlük, tazeleme hızı, yönlendirme ve yerleşim için önizleme onayı devam eder. Geliştirme sürümü donanıma yazmadan 155 otomatik kontrolü geçti; fiziksel kaydırıcı davranışı kullanıcı tarafından test edilmelidir.

### Işık profilleri

**+ Yeni profil** ile bir ad verip bağlı her monitör için farklı parlaklık ve kontrast yüzdeleri seçebilirsin. İşaretini kaldırdığın ayarlar değişmez. Kaydetmek profili uygulamadan saklar. Özel profiller ana pencerede ve tepsi panelinde üç hazır profilin altında düğme olarak görünür; düğmeye tıklayarak uygula. Ana penceredeki kalem düğmesinden düzenleyebilir, adını değiştirebilir veya silebilirsin. Düzenleme sırasında bağlı olmayan monitörlerin kayıtlı değerleri korunur; profile dahil olmayan monitörler değişmez. Eşleştirme ekran numarası yerine monitör cihaz kimliği ve fiziksel indeks üzerinden yapılır.

Profiller `%LOCALAPPDATA%\MonitorDesk\profiles.json` dosyasında saklanır ve sonraki açılışta geri gelir. Uygulamadan önce güncel destek bilgileri okunur, komutlar sırayla gönderilir ve değerler yeniden okunarak doğrulanır; desteklenmeyen veya güncel olmayan ayarlar atlanır. v0.15.0 geliştirme sürümünde 196 otomatik kontrol geçti; kaydetme, geçersiz veri, monitör başına farklı değerler, seçilmeyen ayarlar ve bağlı olmayan cihazlar sınandı. Gerçek monitörlerde profil uygulama kullanıcı tarafından test edilmelidir.

Eye comfort’ın seçili yoğunluğu, ana penceredeki kaydırıcının altında ve tepsi düğmesinde **değer / 90** olarak gösterilir; mod kapalıyken de görünür. Bu, filtrenin seçili yoğunluk seviyesidir; yüzde veya monitör parlaklığı değildir. Kaydırıcı değişince iki görünüm birlikte güncellenir.

**Eye comfort**, bağlı ekranlarda tıklamaları engellemeyen sıcak renkli bir katmanı açıp kapatır. Kaydırıcıyla yoğunluğu ayarlanır; modu kapatmak veya Monilivo’yu kapatmak katmanı kaldırır. Windows Night light’tan bağımsızdır; gamma kalibrasyonunu, monitörün renk sıcaklığını, parlaklığını veya kontrastını değiştirmez. Başlangıçta kapalıdır; seçili yoğunluk sonraki oturum için kaydedilir. Katman, Windows Night light’ın renk dönüşümünü birebir uygulamak yerine sıcak bir renk karıştırır; özel tam ekran oyunlarında veya Windows güvenli masaüstünde görünmeyebilir ve ekran görüntülerine dahil olabilir. Fiziksel kullanım, farklı DPI değerleri ve tam ekran davranışı kullanıcı tarafından test edilmelidir.

DDC okumalarında yeniden deneme aralıkları artırıldı. Parlaklık/kontrast yazmaları geçici iletişim hatalarında en fazla üç kez denenir; ardından başka komut veya doğrulama okumasından önce monitöre toparlanma süresi verilir. Desteklenmeyen komutlar tekrar denenmez, güncel olmayan değerler işaretlenir. Bu değişiklik OMEN 25i’de parlaklık yazmasından sonra kontrastın geçici kaybolması bildirimini ele alır; fiziksel monitörde düzeldiği henüz doğrulanmadı. Geliştirme sürümünde donanım ayarlarını değiştirmeden 121 otomatik/salt okunur kontrol geçti.

**Lighting profiles** alanındaki bir düğmeye basınca ilgili profil bağlı tüm monitörlere hemen uygulanır:

- **Night (Gece):** parlaklık %25, kontrast %60.
- **Normal:** parlaklık %55, kontrast %70.
- **High light (Yüksek ışık):** parlaklık %90, kontrast %75.

Yüzdeler her kontrolün desteklediği minimum ve maksimum aralığına göre hesaplanır. Algılanan parlaklık monitörler arasında değişebilir. Gece profili donanımsal parlaklığı ve kontrastı ayarlar; renk sıcaklığını değiştirmez veya Windows Gece Işığı'nı açmaz. Profiller yalnızca seçildiğinde uygulanır; zamanlama veya uygulama açılışında otomatik uygulama yoktur.

Uygulama önce güncel yetenekleri okur; kullanılamayan veya güncel olmayan kontrolleri atlar ve desteklenen ayarları sırayla gönderir. Bir hata diğer kontrolleri engellemez. Zaten hedef değerde olan kontrollere tekrar yazılmaz. İşlem sonunda değerler yeniden okunur; **Results by monitor** bölümünde başarılı değişiklikler, atlanan kontroller, hatalar, doğrulanamayan ve istenenden farklı okunan değerler gösterilir. Dahili paneller parlaklığı desteklenen seviyeye yuvarlayabilir. Profil kısmen uygulanabilir; tamamlanan değişiklikler korunur. Sonrasında başka profil seçebilir veya ayrı kaydırıcıları kullanabilirsin.

v0.7.0 için 123 otomatik/salt okunur kontrol geçti. Geniş, normal ve dar pencere ile açık tema WPF görünümleri incelendi. Profil toplu uygulama testlerinde yazma işlemleri taklit edildi; güncellenen arayüzün fiziksel monitörlerde kullanımı kullanıcı tarafından test edilmelidir.

## Derleme ve çalıştırma

Windows’a .NET 10 SDK’yı kurduktan sonra depo kökünde şu komutları çalıştır:

```powershell
dotnet restore tests/MonitorDesk.Checks --configfile NuGet.Config
dotnet build tests/MonitorDesk.Checks -c Release --no-restore
dotnet run --project tests/MonitorDesk.Checks -c Release --no-build
dotnet run --project src/MonitorDesk -c Release --no-build
```

Üçüncü taraf NuGet paketleri gerekmez. NuGet.Config, paket kaynaklarını bilinçli olarak temizler; framework başvurularını kurulu SDK sağlar.

Dağıtılabilir bir klasör oluşturmak için:

```powershell
dotnet publish src/MonitorDesk -c Release --no-restore -o artifacts/Monilivo
```

`artifacts/Monilivo/Monilivo.exe` dosyasını çalıştır. Bu derleme framework’e bağımlıdır: hedef bilgisayarda .NET 10 Windows Desktop Runtime bulunmalıdır. Yönetici yetkisi istenmez.

### Kurulum EXE’sini oluşturma

Paketi hazırlayan bilgisayarda .NET 10 SDK, Inno Setup 6.3+ veya 7 ve Microsoft çalışma ortamı paketlerini indirmek için internet gerekir. Derleyiciyi bir defa kur:

```powershell
winget install --id JRSoftware.InnoSetup -e -s winget -i
```

Ardından depo kökündeki **Build-Installer.cmd** dosyasını çalıştır veya şu komutu kullan:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Build-Installer.ps1
```

Betik mevcut kontrolleri çalıştırır, .NET’i içeren Windows x64 uygulamasını oluşturur, salt okunur açılış kontrolü yapar ve `artifacts/installers/Monilivo-Setup-0.17.0-win-x64.exe` dosyasını yanında SHA-256 doğrulama dosyasıyla üretir. Yalnızca kurulum paketi oluşturma işlemi, çalışma ortamını indirmek için `installer/NuGet.Config` kullanır; normal geliştirmedeki paket kaynakları değişmez. Derleyici farklı konumdaysa `-CompilerPath "C:\path\to\ISCC.exe"` parametresini kullan.

Yayınlamadan önce .NET kurulu olmayan bir Windows hesabında kurulumu, masaüstü kısayolunu, mevcut kurulumu güncellemeyi ve kaldırmayı test et. EXE ve `.sha256` dosyasını uygulama sürümüyle eşleşen bir GitHub Release’e ekle. Yerel derleme betiği kendiliğinden release oluşturmaz veya dosya yüklemez. Paket içindeki .NET’i güncellemek için güncel SDK ile yeniden paket oluşturulmalıdır. İlk kurulum paketi dijital olarak imzalanmamıştır; Windows bilinmeyen yayıncı uyarısı gösterebilir.

v0.11.0 kurulum EXE’si, 143 otomatik kontrol ve paketlenmiş uygulamanın salt okunur açılış kontrolü geçtikten sonra Inno Setup 6.7.3 ile oluşturuldu. Üretilen SHA-256 doğrulama dosyası kontrol edildi. Kurulum, güncelleme ve kaldırma testleri henüz yapılmadı; kaynak kodu push etmek yerel EXE’yi GitHub’a yüklemez.

## Donanım sınırlamaları

Monitörün kendi menüsünden DDC/CI özelliğini etkinleştir. Bazı bağlantı istasyonları, adaptörler, sürücüler, HDR modları ve monitör önayarları kontrolleri engelleyebilir veya sınırlayabilir. Bir değerin okunabilmesi, cihazın değişikliği kabul edeceğini garanti etmez. Apply işlemi komut hatalarını bildirir ve geçerli değerleri yeniden okur.

WMI parlaklığı yalnızca etkin bir sağlayıcı ekranın cihaz kimliğiyle eşleştiğinde kullanılır. Dahili panel parlaklığı desteklenen en yakın seviyeye ayarlanır. WMI üzerinden kontrast ayarı sunulmaz. WMI ve DDC/CI işlemleri arayüz iş parçacığının dışında, sırayla yürütülür. Takılan bir sürücü çağrısı sonraki işlemleri geciktirebilir; bu sürümde sürücü çağrısını zorla sonlandıran bir zaman aşımı yoktur.

Ekran numaraları uygulamaya özeldir; Windows Ayarları ile aynı olmaları garanti edilmez. Yinelenen ekran yapılandırmaları fiziksel ekranları gruplayabilir. Tazeleme hızı, EnumDisplaySettings tarafından bildirilen tam sayıdır; kesirli hızlar gösterilmez.

### Çözünürlük ve tazeleme hızı

Bir çözünürlük ve desteklenen tazeleme hızlarından birini seçip **Preview** düğmesine bas. Windows, seçilen modu uygulamadan önce doğrular. 15 saniye içinde **Keep for this session** ile onayla veya **Revert** / Escape ile önceki moda dön. Onay penceresini kapatmak da değişikliği geri alır. Onaylanan değişiklikler mevcut Windows oturumu için geçerlidir; kayıtlı Windows varsayılanları değiştirilmez.

Çözünürlük ve tazeleme hızı seçimleri geçerli renk derinliğini, ekran yönünü ve tarama türünü korur. Yinelenen ekranlar birlikte değişebilir. Başka bir yerden yapılan ekran değişikliklerinden sonra önizleme öncesinde yenileme gerekir. Geri dönüş sayacı arayüz iş parçacığından bağımsız çalışır; ancak uygulamanın sonlandırılması veya ekran sürücüsünün takılması durumunda kurtarma yapamaz. Ekranın bağlantısının kesilmesi veya sürücü hatası geri dönüşü engelleyebilir; hatalar durum alanında gösterilir. Gerekirse Windows Görüntü Ayarları’nı kullan.

Etkin ekran yönünü değiştirmek için **Orientation** ve **Rotate** seçeneklerini kullan. Bu işlem geçerli tazeleme hızını korur ve çeyrek dönüşlerde piksel genişliği ile yüksekliğini yer değiştirir; henüz uygulanmamış çözünürlük seçimlerini kullanmaz. Windows, değişiklik yapmadan önce dönüş desteğini sınar. Aynı 15 saniyelik onay akışı, geri alındığında veya onay verilmediğinde önceki yönü ve çözünürlüğü geri yükler. Onay yalnızca mevcut Windows oturumu için geçerlidir. Monitörü fiziksel olarak elle döndürmek gerekir. Kullanıcı, yön değiştirme işlevini kendi sisteminde denediğini ve sorun yaşamadığını bildirdi; kurtarma senaryoları ayrı ayrı kaydedilmedi.

v0.3.0 yerel doğrulamasında, üç ekrandaki modların salt okunur olarak listelenmesi dahil 83 kontrol geçti. Kullanıcı çözünürlük ve tazeleme hızı işlevlerini de kendi sisteminde denediğini ve sorun yaşamadığını bildirdi. Manuel kurtarma senaryoları ayrı ayrı kaydedilmedi.

### Ekran yerleşimi

Ana pencerenin üstündeki **Arrange your screens** alanında numaralı kutuları masandaki düzene göre sürükle. Yakın kenarlar birbirine hizalanır. Odaklanan kutuyu yön tuşlarıyla 10 piksel, Shift + yön tuşlarıyla 1 piksel hareket ettirebilirsin. **Reset draft**, Windows ayarlarını değiştirmeden başlangıçtaki taslağı geri yükler. Birincil ekran aynı kalır; kutusu taşındığında bile koordinatlar ona göre yeniden hesaplanır.

**Preview layout**, ekranların üst üste binmediği ve bağlantısız boşlukların kalmadığı değiştirilmiş düzenlerde etkinleşir. Ekranlar yalnızca köşeden değil, bir kenar boyunca temas etmelidir. Windows tüm konumları birlikte doğrular ve uygular; kaydedilen kaynak ve hedef modları korunur. Düzeni mevcut Windows oturumu için korumak üzere 15 saniye içinde onayla veya tüm eski konumları geri yüklemek için geri al. Kayıtlı Windows varsayılanları değiştirilmez. Yinelenen ekran düzenleri desteklenmez; önce Windows Ayarları’ndan Genişlet seçeneğine geç.

Düzenleme sırasında ekran bağlamak, bağlantısını kesmek veya ekran ayarlarını değiştirmek taslağı geçersiz kılar. Geri dönüş sırasında bağlantı değişmişse eski bir yapılandırma uygulanmak yerine hata bildirilir. Mod önizlemelerinde olduğu gibi, uygulamanın sonlandırılması veya sürücünün takılması otomatik geri dönüşü engelleyebilir; gerekirse Windows Görüntü Ayarları’nı kullan.

v0.4.0 yerel doğrulamasında, gerçek CCD ekran yerleşiminin okunması dahil 108 otomatik/salt okunur kontrol geçti. Değiştirilmemiş düzenin yerel API ile doğrulanması, ajan sandbox ortamında Windows hata kodu 5 alındığı için atlandı; etkileşimli masaüstü oturumuna erişim reddedildi. Kullanıcı daha sonra masaüstü uygulamasında ekran yerleşimini denedi ve konumlandırmanın düzgün çalıştığını doğruladı. Zaman aşımı, bağlantı kesilmesi ve kurtarma senaryoları ayrı ayrı kaydedilmedi.

Uygulama hesap gerektirmez, bir sunucuya bağlanmaz ve telemetri toplamaz. Tanılama çıktıları yerel ekran kimliklerini içerir; paylaşmadan önce gözden geçir.

## Doğrulama

Otomatik kontroller, yerel API yapılarının bellek düzenlerini ve desteklenmeyen ya da sınır dışındaki yazma işlemlerinin donanıma erişmeden reddedilmesini kapsar. Windows CI iş akışı yerelde hazırdır ancak GitHub’da henüz etkin değildir. Otomatik kontroller gerçek monitör davranışını sınayamaz.

İlk yerel salt okunur doğrulamada, 240 Hz ve 120 Hz hızlarında çalışan iki adet 1920×1080 harici monitör algılandı. İkisi de parlaklık bildirdi; yalnızca ilki kontrast bildirdi. Donanımsal yazma işlemleri ve dahili panel WMI yazma işlemleri bu doğrulamada denenmedi.

Sürüm yayımlamadan önce yapılacak manuel kontroller:

- Desteklenen her ekranda küçük bir parlaklık değişikliği uygula ve eski değeri geri yükle.
- Desteklenmeyen kontrast kontrolünün kullanılamadığını doğrula.
- Negatif koordinatlarda ve farklı DPI ölçeklerinde ekran tanımlamayı dene.
- Yenileme sırasında ekranın bağlantısını kesip yeniden bağla ve toparlanmayı doğrula.
- Açık/koyu temayı, klavyeyle gezinmeyi ve %125–200 ölçeklendirmeyi dene.
- Uyumlu bir dizüstü bilgisayar panelini ayrıca test et.

## Mimari

`Services/Native.cs`, Windows API’leriyle iletişim katmanını içerir. `Services/DisplayService.cs`, yeteneklerin okunmasını, fiziksel monitör tanıtıcılarının güvenli temizlenmesini ve yazma işlemlerinin sıraya alınmasını yönetir. `MainWindow`, kartları gerçek yeteneklere göre oluşturur. `App` ayrıca yerel, salt okunur tanılama modlarını destekler:

```powershell
Monilivo.exe --probe C:\path\displays.json
Monilivo.exe --snapshot C:\path\window.png
```

## Yol haritası

- Onaylanan ekran modlarını Windows oturumları arasında kalıcı olarak saklama.
- Zamanlanmış ışık değişiklikleri.
- Sistem tepsisi kontrolleri ve klavye kısayolları.
- Ek arayüz dilleri ve isteğe bağlı manuel dil seçimi.
- Nihai ürün adı ve dağıtılabilir kurulum programı.

Bu README [İngilizce](README.md) ve Türkçe olarak sunulur. Dil değiştirmek için her iki dosyanın başındaki bağlantıları kullanabilirsin. Bu bağlantılar dokümantasyonun dilini değiştirir; uygulama arayüzü bağımsız olarak Windows görüntüleme dilini kullanır (Türkçe veya İngilizce). Doküman güncellenirken iki README sürümü de eş zamanlı güncellenmelidir. Kod yorumları ve GitHub etkinlikleri İngilizcedir.

### Aralıklı kontrast okuma hataları (v0.1.2)

Yerel üç monitörlü sistemde Ekran 2, arka arkaya yapılan 8 kontrast okumasının 6’sında I2C aktarım hatası (`0xC0262582`) döndürdü. Parlaklık okumasından sonra 150 ms beklendiğinde 8 okumanın tamamı başarılı oldu. Bu sonuç, zamanlamaya duyarlı bir iletişim sorununu destekler; belirli bir kablo, sürücü veya ürün yazılımı arızasını kanıtlamaz.

Donanım okumaları artık aralıklı yapılır ve geçici aktarım hatalarında en fazla üç okuma denemesi gerçekleştirilir. Tüm denemeler başarısız olursa daha önce başarıyla okunan değerler görünür kalır; ancak kontroller devre dışı bırakılır ve değerlerin güncel olmadığı açıkça belirtilir. Önbellekteki hiçbir değer yeni bir okuma gibi gösterilmez. Ekranlar kaybolduğunda ilgili önbellek kayıtları silinir. Bu yaklaşım dayanıklılığı artırır; sürücü veya donanım güvenilirliğini garanti etmez.

Salt okunur tanılama komutları (monitör ayarlarını değiştirmez):

```powershell
dotnet run --project tests/MonitorDesk.Checks -c Release -- --diagnose
dotnet run --project tests/MonitorDesk.Checks -c Release -- --diagnose --paced
dotnet run --project tests/MonitorDesk.Checks -c Release -- --service-diagnose
```
