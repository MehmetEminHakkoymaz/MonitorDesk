# MonitorDesk

[English](README.md) | **Türkçe**

Her ekranı kendine göre ayarla. MonitorDesk, ekran bilgilerini görüntülemek ve desteklenen monitörlerin donanımsal parlaklık ve kontrast ayarlarını değiştirmek için geliştirilmiş hafif bir Windows masaüstü uygulamasıdır.

**Geçici proje adı · v0.8.0 (geliştirme) · Yalnızca Windows**

## Kurulum

Kurulum paketleri [GitHub Releases](https://github.com/MehmetEminHakkoymaz/MonitorDesk/releases) sayfasına dosya olarak eklenebilir. Yayınlandığında **MonitorDesk-Setup-0.7.0-win-x64.exe** dosyasını indirip çalıştır. GitHub’ın kaynak kod ZIP dosyası kurulum paketi değildir.

Kurulum, MonitorDesk’i Windows kullanıcı hesabına yükler; masaüstü ve Başlat menüsü kısayollarını oluşturur ve Windows Ayarları’na kaldırma kaydı ekler. .NET Windows Desktop çalışma ortamı uygulamayla birlikte gelir; hedef bilgisayarda ayrıca .NET kurulması veya internet bağlantısı gerekmez. Yönetici yetkisi istenmez. Paket, x64 uyumlu Windows 10 (19041 ve üzeri) ve Windows 11 içindir; monitör sürücüsü içermez. Daha yeni kurulum dosyası mevcut kurulumu günceller.

## Özellikler

- Bağlı ekranları, geçerli çözünürlüklerini, tazeleme hızlarını ve birincil ekran durumunu gösterir.
- Ekranları geçici numara etiketleriyle tanımlar.
- Harici monitörlerin parlaklık ve kontrast değerlerini Windows DDC/CI API’leriyle okur ve değiştirir.
- Uyumlu dahili panellerde Windows WMI üzerinden parlaklık ayarı sunar.
- Kullanılamayan kontrolleri açıkça belirtir; sahte değer veya yazılımsal karartma kullanmaz.
- Ekran yapılandırması değiştiğinde bilgileri yeniler.
- Açık/koyu tema, monitör başına DPI farkındalığı ve klavyeyle erişilebilir kontroller sunar.
- Değişiklikleri açık bir uygulama adımıyla gönderir; kaydırıcıyı hareket ettirmek tek başına monitör ayarını değiştirmez.
- Desteklenen çözünürlük ve tazeleme hızlarını 15 saniyelik onay süresiyle önizler.
- Yatay, dikey, ters yatay ve ters dikey yönleri önizler.
- Genişletilmiş ekranları sürükleyerek konumlandırmayı, kenar hizalamayı ve süre sonunda geri dönmeyi destekler.
- Gece, normal ve yüksek ışık parlaklık/kontrast profillerini desteklenen tüm monitörlere uygular.

Ana pencere, yerleşim düzenleyicisini ve kompakt ekran kartlarını bir araya getirir. Kartlar pencere genişliğine göre bir, iki veya üç sütuna yerleşir; açılır menüler açık/koyu temaya uyum sağlar. Windows’taki ekran düzeni değişmediyse yerleşim taslağı yenileme sırasında korunur; yapılandırma değişirse güncel düzen yüklenir.

Pencere büyütüldüğünde çalışma alanı ortalanır ve genişliği 1320 pikselle sınırlanır. Işık profilleri geniş pencerede ekran yerleşiminin yanında, dar pencerede altında görünür. Gece, normal ve yüksek ışık için doluluğu artan üç güneş simgesi kullanılır; profil değerleri araç ipuçlarında bulunur. Profil alanında yalnızca işlem sırasında ilerleme ve sonuç mesajları gösterilir. Kaydırıcılar ve kaydırma çubukları seçilen temaya uyum sağlar.

### Işık profilleri

**Eye comfort**, bağlı ekranlarda tıklamaları engellemeyen sıcak renkli bir katmanı açıp kapatır. Kaydırıcıyla yoğunluğu ayarlanır; modu kapatmak veya MonitorDesk’i kapatmak katmanı kaldırır. Windows Night light’tan bağımsızdır; gamma kalibrasyonunu, monitörün renk sıcaklığını, parlaklığını veya kontrastını değiştirmez. Başlangıçta kapalıdır ve sonraki oturuma kaydedilmez. Katman, Windows Night light’ın renk dönüşümünü birebir uygulamak yerine sıcak bir renk karıştırır; özel tam ekran oyunlarında veya Windows güvenli masaüstünde görünmeyebilir ve ekran görüntülerine dahil olabilir. Fiziksel kullanım, farklı DPI değerleri ve tam ekran davranışı kullanıcı tarafından test edilmelidir.

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
dotnet publish src/MonitorDesk -c Release --no-restore -o artifacts/MonitorDesk
```

`artifacts/MonitorDesk/MonitorDesk.exe` dosyasını çalıştır. Bu derleme framework’e bağımlıdır: hedef bilgisayarda .NET 10 Windows Desktop Runtime bulunmalıdır. Yönetici yetkisi istenmez.

### Kurulum EXE’sini oluşturma

Paketi hazırlayan bilgisayarda .NET 10 SDK, Inno Setup 6.3+ veya 7 ve Microsoft çalışma ortamı paketlerini indirmek için internet gerekir. Derleyiciyi bir defa kur:

```powershell
winget install --id JRSoftware.InnoSetup -e -s winget -i
```

Ardından depo kökündeki **Build-Installer.cmd** dosyasını çalıştır veya şu komutu kullan:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Build-Installer.ps1
```

Betik mevcut kontrolleri çalıştırır, .NET’i içeren Windows x64 uygulamasını oluşturur, salt okunur açılış kontrolü yapar ve `artifacts/installers/MonitorDesk-Setup-0.7.0-win-x64.exe` dosyasını yanında SHA-256 doğrulama dosyasıyla üretir. Yalnızca kurulum paketi oluşturma işlemi, çalışma ortamını indirmek için `installer/NuGet.Config` kullanır; normal geliştirmedeki paket kaynakları değişmez. Derleyici farklı konumdaysa `-CompilerPath "C:\path\to\ISCC.exe"` parametresini kullan.

Yayınlamadan önce .NET kurulu olmayan bir Windows hesabında kurulumu, masaüstü kısayolunu, mevcut kurulumu güncellemeyi ve kaldırmayı test et. EXE ve `.sha256` dosyasını uygulama sürümüyle eşleşen bir GitHub Release’e ekle. Yerel derleme betiği kendiliğinden release oluşturmaz veya dosya yüklemez. Paket içindeki .NET’i güncellemek için güncel SDK ile yeniden paket oluşturulmalıdır. İlk kurulum paketi dijital olarak imzalanmamıştır; Windows bilinmeyen yayıncı uyarısı gösterebilir.

Kullanıcı, otomatik kontroller ve paketlenmiş uygulamanın salt okunur açılış kontrolü dahil kurulum EXE’sini Inno Setup 6.7.3 ile başarıyla oluşturdu. Üretilen SHA-256 doğrulama dosyası kontrol edildi. Kurulum, güncelleme ve kaldırma testleri henüz yapılmadı.

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
MonitorDesk.exe --probe C:\path\displays.json
MonitorDesk.exe --snapshot C:\path\window.png
```

## Yol haritası

- Onaylanan ekran modlarını Windows oturumları arasında kalıcı olarak saklama.
- Özelleştirilebilir profiller ve zamanlanmış ışık değişiklikleri.
- Sistem tepsisi kontrolleri ve klavye kısayolları.
- Arayüzün yerelleştirilmesi ve Türkçe çevirisi.
- Nihai ürün adı ve dağıtılabilir kurulum programı.

Bu README [İngilizce](README.md) ve Türkçe olarak sunulur. Dil değiştirmek için her iki dosyanın başındaki bağlantıları kullanabilirsin. Bu bağlantılar dokümantasyonun dilini değiştirir; uygulama arayüzü şu anda İngilizcedir. Doküman güncellenirken iki README sürümü de eş zamanlı güncellenmelidir. Kod yorumları ve GitHub etkinlikleri İngilizcedir.

### Aralıklı kontrast okuma hataları (v0.1.2)

Yerel üç monitörlü sistemde Ekran 2, arka arkaya yapılan 8 kontrast okumasının 6’sında I2C aktarım hatası (`0xC0262582`) döndürdü. Parlaklık okumasından sonra 150 ms beklendiğinde 8 okumanın tamamı başarılı oldu. Bu sonuç, zamanlamaya duyarlı bir iletişim sorununu destekler; belirli bir kablo, sürücü veya ürün yazılımı arızasını kanıtlamaz.

Donanım okumaları artık aralıklı yapılır ve geçici aktarım hatalarında en fazla üç okuma denemesi gerçekleştirilir. Tüm denemeler başarısız olursa daha önce başarıyla okunan değerler görünür kalır; ancak kontroller devre dışı bırakılır ve değerlerin güncel olmadığı açıkça belirtilir. Önbellekteki hiçbir değer yeni bir okuma gibi gösterilmez. Ekranlar kaybolduğunda ilgili önbellek kayıtları silinir. Bu yaklaşım dayanıklılığı artırır; sürücü veya donanım güvenilirliğini garanti etmez.

Salt okunur tanılama komutları (monitör ayarlarını değiştirmez):

```powershell
dotnet run --project tests/MonitorDesk.Checks -c Release -- --diagnose
dotnet run --project tests/MonitorDesk.Checks -c Release -- --diagnose --paced
dotnet run --project tests/MonitorDesk.Checks -c Release -- --service-diagnose
```
