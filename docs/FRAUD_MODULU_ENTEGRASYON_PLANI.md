# FRAUD MODÜLÜ (Excel) → FraudGuard Entegrasyon Analizi ve Planı

> Durum: **Taslak / karar bekliyor.** Bu doküman hazırlanırken hiçbir kod değişikliği yapılmadı.
> Kaynaklar: `FRAUD MODÜLÜ (1) (2).xlsx` (8 sayfa) ve mevcut `FraudGuard_Workspace` kod tabanı.
> Tarih: 17.08.2026

---

## 1. Yönetici Özeti

Excel dosyası, **60 senaryoluk kümülatif skorlama tabanlı bir POS/üye işyeri (acquiring) fraud motorunun** iş
tanımıdır. Mevcut FraudGuard ise **25 kurallı, ilk eşleşmede duran, kart/müşteri (issuer) tarafı** bir
sistemdir. İki model arasındaki fark kural sayısı değil; **hedef aktör** ve **karar mekanizmasıdır**.

| | Mevcut FraudGuard | Excel FRAUD MODÜLÜ |
|---|---|---|
| Bakış açısı | Issuer (kart hamili / müşteri) | Acquirer (üye işyeri / POS) |
| Ana aktör | Kart, Müşteri, IBAN | **İşyeri (merchantId)** + Kart |
| Kural sayısı | 25 | 60 senaryo + 5 kombinasyon |
| Karar | İlk eşleşen kural → `Suspicious` | Tüm senaryolar → **puan birikimi** → kademeli aksiyon |
| Skor | Sunum katmanında türetiliyor, kalıcı değil | Kalıcı, decay'li, tekrar tavanlı, güven skoru ile eşiklenen |
| Aksiyon | Approved / Declined / Suspicious | NORMAL / İZLE / EK_DOĞRULAMA / RET |
| Zaman penceresi | Karta ait son 24 saat | 1 dk – 30 gün + haftalık/aylık baseline'lar |

**Sonuç:** Bu bir "kural ekleme" işi değil, **motor değişimi + yeni veri modeli** işidir. Senaryoların
yaklaşık %70'i mevcut veri modelinde karşılığı olmayan alanlara (merchantId, MCC, BIN, POS tahsis tarihi,
hata kodu, temassız/offline/pinsiz bayrakları) bağımlıdır. Bu alanlar olmadan senaryolar yazılamaz.

---

## 2. Mevcut Sistem Envanteri

### 2.1 Mimari
- **Backend** (.NET, Clean Architecture): `FraudGuard.API` / `.Application` / `.Domain` / `.Infrastructure`
- **Veri**: EF Core + SQL Server (Docker). **Migration yok** — `Program.cs` içinde `EnsureCreated()` /
  `CreateTables()` ile şema kuruluyor, tüm seed verisi `FraudGuardDbContext.OnModelCreating` içinde `HasData`.
- **Cache**: `ICacheProvider` soyutlaması; DI'da `MemoryCacheProvider` singleton kayıtlı
  (`RedisCacheProvider` sınıfı mevcut ama bağlı değil), docker-compose'da Redis servisi ayakta.
- **Realtime**: SignalR `FraudHub`
- **Client**: React (clean arch: domain/application/infrastructure/presentation) + Electron bridge
- **TransactionSimulator**: Node.js, işlem üretici

### 2.2 Fraud motoru (bugün nasıl çalışıyor)
`Backend/FraudGuard.Domain/Services/FraudEvaluationService.cs`

1. Karta/IBAN'a ait **son 24 saatlik** işlem listesi çekilir (5 dk cache).
2. DB'deki aktif kural kodları okunur.
3. `IEnumerable<IFraudRule>` üzerinde döngü → **ilk `IsSuspicious == true` dönende `return`.**
4. `TransactionService` sonucu `Suspicious` yapar, tek bir `EFraudLog` kaydı açar.

Kural sözleşmesi tek satır:
```csharp
Task<(bool IsSuspicious, string? Reason)> EvaluateAsync(ProcessTransactionInput input, List<ITransaction> history);
```

### 2.3 Skorlama (bugün)
`FraudManagementAppService.CalculateRiskScore()` — kural kodu → sabit ağırlık, sonra kanal ve MCC çarpanı.
**Sunum anında hesaplanıyor, DB'ye yazılmıyor, biriktirilmiyor, yaşlandırılmıyor.** Excel'in istediği
skorlama ile ortak yanı sadece "0-100 bir sayı" olması.

### 2.4 Veri modeli (özet)
- `ECustomer`, `ECreditCard`, `EDebitCard`, `EBankAccountBeneficiary`
- `ECreditCardTransaction`, `EDebitCardTransaction`, `ETransferTransaction` (ortak arayüz `ITransaction`)
- `EFraudRule` (RuleId, RuleCode, RuleName, Description, IsActive), `EFraudLog`, `EBlockReason`,
  `EChannelType`, `ETransactionType`, `EUser`

İşlem üzerindeki "işyeri" bilgisi yalnızca `MerchantCategory` adında **serbest metin** bir alandır.
**Sistemde `Merchant` diye bir varlık yoktur.**

---

## 3. Excel'in İçeriği (sayfa sayfa)

| Sayfa | Satır | İçerik | Entegrasyondaki rolü |
|---|---|---|---|
| **MB REHBER _Zorunlu** | 25 madde | TCMB/BDDK zorunlu izleme göstergeleri | Regülasyon gerekçesi; senaryoların çoğunun kaynağı |
| **SENARYOLAR** | 60 senaryo | Kod, ad, koşul, Input, Expression, Output, **Puan**, **Seviye**, **Hedef** | Motorun kural kataloğu |
| **INPUTLAR İÇİN MESAJ DESENİ** | 108 alan | `islem / kart / hamil / isyeri / sayaclar / bayraklar_liste / senaryoGecmisi` gruplarında kanonik alan sözlüğü | **Hedef veri sözleşmesi** — en kritik sayfa |
| **RAPORLAR** | – | Boş ("detay iletilebilir") | Kapsam dışı, sonra istenmeli |
| **RISK HESAPLAMA** | – | Skor depolama, kademeli eşik, decay, tekrar tavanı | Skorlama motorunun spesifikasyonu |
| **KOMBINASYONLAR** | 5 kombinasyon | S kodları + bonus puan + fraud tipi | Korelasyon katmanı |
| **GUVEN SKORU** | – | İşyeri/kart güven faktörleri → RET eşiğini 90'dan 135'e kadar yükseltir | Yanlış pozitif azaltıcı |
| **KARAR KAYDI** | 12 alan | Denetlenebilir karar kaydı şeması | Yeni tablo: audit/decision log |

### 3.1 Skorlama kuralları (RISK HESAPLAMA sayfası)
- **Depolama:** `kartRiskSkoru` (Hedef=Kart senaryoları) ve `isyeriRiskSkoru` (Hedef=İşyeri senaryoları) ayrı ayrı birikir.
- **Kademeli eşik:** `0-39 Normal` · `40-69 İzle` · `70-89 Ek doğrulama (3D/belge)` · `90+ RET`
- **Decay:** Her puan **kendi senaryosunun penceresi kadar** yaşar (S1 → 1 saat, S2 → 24 saat, S38 → 30 gün).
  Skor = `now()` itibarıyla penceresi dolmamış puanların toplamı. Veri saklama: 30 gün.
- **Tekrar tavanı:** Çok Güçlü → sınırsız, Güçlü → 3, Orta → 3, Zayıf → 2.
- **RET sonrası:** hedef `riskliMi = true` işaretlenir, **skor sıfırlanmaz**, işaret manuel kaldırılır.

### 3.2 Güven skoru
İşyeri faktörleri (>6 ay kayıtlı 15, 90 gün temiz 20, istikrarlı ciro 15, vergi mükellefi 10, whitelist 40) ve
kart faktörleri (90 günde çok işlem 15, chargeback yok 20, 3D geçmişi 15) birikir; toplam güven RET eşiğini
belirler: `0-19 → 90`, `20-39 → 105`, `40-59 → 120`, `60+ → 135`. **Tavan 135.**
Kesin/yaptırım senaryoları (OFAC vb.) güvenden muaf → anında RET.

### 3.3 Kombinasyonlar
| Kombinasyon | Senaryolar | Hedef | Bonus |
|---|---|---|---|
| Kart Testi + Cashout | S23 + S37 | Kart | 20 |
| Kimlik Uyuşmazlığı | S35 + S36 | Kart | 15 |
| Bust-out | S10/21/22 + S16/17 | İşyeri | 25 |
| Kart Yayılması | S2 + S38 | Kart | 15 |
| Gece + Yüksek Tutar | S18 + S14/20 | İşyeri | 10 |

Kural: iki senaryo da aktif (penceresi dolmamış) olmalı, aynı hedefe puan yazmış olmalı, bonus bir kez eklenir.

---

## 4. Boşluk Analizi

### 4.1 Veri modeli boşlukları (en büyük blokaj)

| Gerekli alan grubu | Excel alanları | Mevcut durum |
|---|---|---|
| **İşyeri master** | `merchantId`, `mccKodu`, `isyeriLokasyon`, `posTahsisTarihi`, `firmaKayitTarihi`, `sonBasariliIslemTarihi`, `hedefIsYeriMi`, `vergiMukellefiMi`, `pfAltiMi`, `yurtDisiKartYasakMi`, `yetkiliDogumTarihi` | ❌ **Merchant varlığı hiç yok** |
| **Kart metadata** | `binNo`, `kartUlke`, `kartSemasi`, `yurtDisiMi` | ❌ Yok |
| **Hamil bilgisi (işlem anında)** | `hamilTelefon`, `hamilEposta` | ⚠️ `ECustomer` üzerinde var, **işlem kaydında yok** (S35/S36 işlem bazlı farklılık arıyor) |
| **İşlem bayrakları** | `mobilFlag`, `offlineMi`, `temassizMi`, `pinsizIslemMi`, `sifresizIslemMi`, `fizikselIslemMi`, `crossBorderMi`, `hataKodu`, `bolge`, `posTipi` | ❌ Yok |
| **Referans listeleri** | `riskliBinMi`, `riskliUlkeMi`, `yasakliBinMi` (OFAC), `expediaBinMi`, `durdurulanSemaMi`, `durdurulanUlkeMi`, whitelist | ❌ Yok |
| **Sayaçlar (~60 adet)** | `farkliKartSayisi`, `son30GunMaxIslemTutari`, `sektorGunlukCiro`, `sonOtuzGeceOrtTutari`, `oncekiHaftaRetOrani` … | ❌ Yok — mevcut motor sadece "karta ait son 24 saatlik liste" veriyor |
| **Senaryo geçmişi** | `oncekiSenaryoKodu`, `oncekiSenaryoZamani`, `girdigiSenaryoKodlari` | ❌ Yok (S59/S60 için şart) |

### 4.2 Motor boşlukları
1. **İlk eşleşmede duruyor** → tüm senaryoların çalışması ve puan biriktirmesi gerekiyor.
2. **Puan kalıcılığı yok** → hedef bazlı (kart/işyeri) skor tablosu + per-entry expiry gerekiyor.
3. **Decay / tekrar tavanı / kombinasyon bonusu / güven skoru / dinamik eşik** → hiçbiri yok.
4. **Kesin senaryo (anında RET) ayrımı yok** → skora bakılmadan kesen bir yol gerekiyor.
5. **Sadece gerçek zamanlı** → S5, S9, S16, S17, S26, S27, S40 gibi haftalık/aylık/sektörel senaryolar
   **batch/scheduled** hesap ister; her işlemde 30 günlük tarama yapılamaz.
6. **Kural parametreleri kod içinde sabit** (`RuleThresholdConstants`, sınıf içi literal'ler) → Excel her senaryo
   için puan/seviye/hedef/pencere/eşik parametresi tanımlıyor; bunlar DB'den yönetilmeli.
7. **`EFraudRule` şeması yetersiz** → `Puan`, `Seviye`, `Hedef`, `Pencere`, `KesinSenaryoMu` alanları yok.

### 4.3 Altyapı boşlukları
- **Migration yok** (`EnsureCreated`). Yeni tablo/kolon eklemek mevcut DB volume'ünü kullanılamaz hale getirir.
  **Entegrasyona başlamadan önce EF Core Migrations'a geçilmeli.**
- **Redis bağlı değil** (`MemoryCacheProvider` singleton kayıtlı). Kayan pencere sayaçları için Redis
  (sorted set / TTL'li key) neredeyse zorunlu.
- Simülatör işyeri trafiği üretmiyor → senaryoların test edilebilmesi için merchant'lı üretim gerekli.
- UI'da kural listesi hardcoded (`Client/react-ui/src/core/types/fraud.enums.ts`) → 60 senaryo için katalog
  API'den gelmeli.

---

## 5. Senaryo Fizibilite Triyajı

60 senaryo, **hangi altyapıyı beklediğine** göre gruplandı. Uygulama sırası bu gruplara göre olmalı.

### Grup A — Gerçek zamanlı sayaç + işyeri kimliği yeterli (14 senaryo)
`S1, S2, S6, S7, S12, S14, S18, S20, S23, S29, S30, S34, S37, S38`
İhtiyaç: `merchantId` + kayan pencere sayaçları (1dk–30gün). En hızlı kazanç bu grupta.

### Grup B — İşyeri master verisi gerekli (11 senaryo)
`S4, S8, S10, S21, S22, S25, S28, S44, S48, S54, S55`
İhtiyaç: `posTahsisTarihi`, `firmaKayitTarihi`, `mccKodu`, `vergiMukellefiMi`, `isyeriLokasyon`, `yetkiliDogumTarihi`.

### Grup C — Kart/BIN referans verisi gerekli (10 senaryo)
`S15, S41, S42, S43, S45, S46, S47, S50, S56, S57`
İhtiyaç: BIN tablosu (ülke, şema, banka), OFAC/riskli BIN listeleri, ülke/şema durdurma listeleri.
Bunlar **veri temini** işidir; liste sağlanmadan yazılamaz.

### Grup D — Batch istatistik / baseline gerekli (16 senaryo)
`S3, S5, S9, S11, S13, S16, S17, S19, S24, S26, S27, S31, S32, S33, S39, S40`
İhtiyaç: gecelik hesaplanan `MerchantDailyStats` / `MerchantNightStats` / `SectorStats` tabloları,
`hataKodu` ve `bolge` alanları, `hedefIsYeriMi` bayrağı.

### Grup E — İşlem bazlı bayrak/hamil verisi gerekli (7 senaryo)
`S35, S36, S49, S51, S52, S53, S58`
İhtiyaç: `hamilTelefon`, `hamilEposta`, `mobilFlag`, `offlineMi`, `temassizMi`, `sifresizIslemMi`, ilk kullanım takibi.

### Grup F — Motor özelliği, senaryo değil (2)
`S59` (senaryolar arası süre koşulu), `S60` (belirli senaryolara girmiş olma koşulu).
Bunlar kombinasyon motorunun yapı taşları — kural olarak değil, **skorlama motorunun içinde** kodlanmalı.

---

## 6. Excel'de Netleştirilmesi Gereken Noktalar

Bunlar iş tarafına sorulmadan implementasyona geçilmemeli:

1. **S1 ile S7 birebir aynı.** İkisi de "1 saatte 3 farklı kart veya aynı kartla 3 işlem", ikisi de 30 puan,
   ikisi de İşyeri hedefli. Aynı işlemde ikisi de tetiklenirse **60 puan** yazar. Tek senaryoya indirilmeli
   ya da farkı tanımlanmalı (S1 sadece Link/Manuel POS, S7 tüm işyerleri → kapsam farkı belirtilmeli).
2. **S2 ile S38 örtüşüyor.** S2: 24 saatte ≥2 işyeri (25p), S38: 30 günde ≥2 işyeri (20p). S2 tetiklenen her
   durumda S38 de tetiklenir → 45p + "Kart Yayılması" kombinasyon bonusu 15p = 60p. Kart neredeyse otomatik
   olarak "Ek doğrulama" bandına giriyor. Eşikler gözden geçirilmeli.
3. **S4'ün expression'ı hatalı:** `mccKodu != 9399 || 9311 || 8062 && toplamSatisTutar >= 50000`.
   Doğrusu: `mccKodu NOT IN (9399, 9311, 8062) AND toplamSatisTutar >= 50000`.
4. **S12'nin hedefi belirsiz** — Excel'in kendisi de not düşmüş: *"işyeri veya kart olduğu belirtilmemiş"*.
5. **RET / puan çelişkisi:** S50, S51, S54, S55, S58 satırlarında `Output: islemSonucu = RET` yazıyor ama
   seviye "Çok Güçlü" değil ve sonlu puan verilmiş (30/40/40/15/15). Bunlar **kesin RET mi, puanlı senaryo mu?**
6. **S58 (kartın ilk kez kullanımı) RET olarak uygulanamaz** — sistemi ilk kez gören her kart reddedilir.
   İzleme/puan olarak konumlandırılmalı.
7. **S55 (belirli illerdeki işyerlerine RET)** — lokasyon bazlı toptan ret, ayrımcılık ve regülasyon
   riski taşır. Hukuk/uyum onayı olmadan **RET olarak uygulanmamalı**; izleme + düşük puan önerilir.
   *(İl listesi için kaynak Excel'in SENARYOLAR sayfası, S55 satırı.)*
8. **Tekrar tavanı ile decay ilişkisi:** "Güçlü → max 3 tekrar" tavanı pencere içinde mi, gün başına mı,
   yoksa ömür boyu mu? Pencere bazlı varsayacağız, teyit gerekli.
9. **"Puan: TAM"** ifadesi sayısal değil; motor için `KesinSenaryoMu = true` bayrağına çevrilecek.
10. **Kart + İşyeri hedefli tek senaryo var (S43)** — puan iki hedefe de mi yazılacak, yoksa yalnız RET mi?
11. **RAPORLAR sayfası boş** — rapor gereksinimleri ayrıca istenmeli.

---

## 7. Önerilen Hedef Mimari

Mevcut Clean Architecture korunur; fraud motoru **yanına** yeni bir skorlama katmanı eklenir.

```
ProcessTransaction
      │
      ▼
[1] Kanonik olay kurulumu ──── FraudEvaluationContext
      │   (islem + kart + hamil + isyeri + bayraklar)
      ▼
[2] Kesin senaryo kontrolü ─── OFAC/BIN/şema/ülke listeleri → eşleşirse ANINDA RET (skora bakma)
      │
      ▼
[3] Sayaç yükleme ──────────── Redis kayan pencere + MerchantStats (batch)
      │
      ▼
[4] TÜM senaryolar çalışır ─── IScenario[] → tetiklenen senaryo listesi (kısa devre yok)
      │
      ▼
[5] Skorlama ──────────────── puan yaz (decay'li) → tekrar tavanı uygula
      │                       → kombinasyon bonusu → güven skoru → dinamik eşik
      ▼
[6] Karar ─────────────────── NORMAL / İZLE / EK_DOĞRULAMA / RET
      │
      ▼
[7] KararKaydı (audit) ────── DecisionRecord + FraudLog + SignalR
```

### Yeni domain yapıları (öneri)
| Ad | Amaç |
|---|---|
| `EMerchant` | İşyeri master (MCC, POS tahsis tarihi, lokasyon, vergi mükellefi, whitelist, hedefIsYeriMi …) |
| `EMerchantDailyStats` / `EMerchantNightStats` | Batch baseline'lar (günlük ciro, gece ort., ret oranı, haftalık) |
| `ESectorStats` | MCC bazlı sektör ortalaması (S26/S27) |
| `EScenarioDefinition` | 60 senaryonun parametreleri: kod, ad, puan, seviye, hedef, pencere, kesinSenaryoMu, aktif |
| `EScenarioHit` | Tetiklenen senaryo kaydı: hedefTipi, hedefKimlik, senaryoKodu, puan, zaman, **expiresAt** (decay motoru) |
| `ERiskDecision` | KARAR KAYDI şeması: kararId, skor, güven, uygulananEşik, tetiklenenSenaryolar, kombinasyonBonusları |
| `ETrustFactor` | Güven skoru faktörleri (işyeri/kart) |
| `EBinReference` / `EReferenceList` | BIN → ülke/şema/banka; riskli/yasaklı liste yönetimi |

### Yeni arayüz sözleşmesi (öneri)
Mevcut `IFraudRule` **kaldırılmaz**, yanına genişletilmiş sözleşme konur; eski 25 kural adapter ile devam eder:
```csharp
public interface IScenario
{
    string ScenarioCode { get; }          // "S1" ...
    ScenarioTarget Target { get; }        // Kart | Isyeri | Ikisi
    Task<ScenarioResult> EvaluateAsync(FraudEvaluationContext ctx);
}
// ScenarioResult: Triggered, TriggeredCondition, Score (definition'dan), Details
```

---

## 8. Faz Planı

### Faz 0 — Hazırlık ve kararlar *(kod yok)*
- Bölüm 6'daki 11 sorunun iş tarafında netleşmesi
- Kapsam kararı: 60 senaryonun tamamı mı, öncelikli alt küme mi
- BIN / OFAC / MCC / whitelist referans verilerinin temin edilebilirliği
- **Çıktı:** onaylı senaryo kataloğu (puan/seviye/hedef/pencere tablosu)

### Faz 1 — Altyapı borcunun kapatılması
1. **EF Core Migrations'a geçiş** (`EnsureCreated` → `Migrate`) — bundan sonraki her şema değişikliğinin ön koşulu
2. `RedisCacheProvider`'ın DI'ya bağlanması ve sayaç API'sinin (`IncrementInWindow`, `DistinctCountInWindow`) eklenmesi
3. **Çıktı:** şema evrimi ve kayan pencere sayacı mümkün hale gelir

### Faz 2 — Veri modeli ve kanonik olay şeması
1. `EMerchant` + işlem tablolarına `MerchantId` FK
2. `ProcessTransactionInput` / işlem entity'lerine bayrak alanları (`mobilFlag`, `offlineMi`, `temassizMi`,
   `pinsizIslemMi`, `sifresizIslemMi`, `fizikselIslemMi`, `crossBorderMi`, `hataKodu`, `bolge`, `posTipi`)
3. Kart metadata (`binNo`, `kartUlke`, `kartSemasi`, `yurtDisiMi`) + işlem bazlı `hamilTelefon` / `hamilEposta`
4. `FraudEvaluationContext` (INPUTLAR sayfasındaki 108 alanın birebir karşılığı)
5. Simülatör ve seed verisinin işyerili hale getirilmesi
6. **Çıktı:** Excel'in "mesaj deseni" ile kod tabanı birebir örtüşür

### Faz 3 — Skorlama motoru *(Excel'in kalbi)*
1. `EScenarioDefinition` + 60 senaryonun DB'ye seed'lenmesi (kod değil, veri)
2. `EScenarioHit` + decay (per-entry `ExpiresAt`) + tekrar tavanı
3. Kombinasyon bonusları (S59/S60 mantığı burada)
4. Güven skoru ve dinamik RET eşiği (90/105/120/135)
5. Kesin senaryo kısa devresi
6. `ERiskDecision` karar kaydı
7. `FraudEvaluationService`'in "ilk eşleşme" davranışından "tümünü çalıştır + biriktir"e taşınması
   *(mevcut 25 kural geriye dönük uyumlu kalacak şekilde)*
8. **Çıktı:** motor hazır, senaryo eklemek artık sadece sınıf yazmak

### Faz 4 — Batch istatistik katmanı
`MerchantDailyStats` / `NightStats` / `SectorStats` üreten zamanlanmış iş (BackgroundService veya Hangfire).
Grup D senaryolarının ön koşulu.

### Faz 5 — Senaryoların dalgalar halinde yazımı
- Dalga 1: **Grup A** (14) — sayaç yeterli
- Dalga 2: **Grup B** (11) — işyeri master hazır olunca
- Dalga 3: **Grup E** (7) — bayraklar hazır olunca
- Dalga 4: **Grup D** (16) — batch istatistik hazır olunca
- Dalga 5: **Grup C** (10) — referans listeleri temin edilince
- Her dalga: senaryo sınıfı + birim test + simülatörde tetikleme senaryosu

### Faz 6 — Yönetim ve sunum
- Senaryo kataloğu API'si + UI (aktif/pasif, puan/eşik düzenleme) — `fraud.enums.ts` hardcode'unun kaldırılması
- Analist ekranında **skor kırılımı** (hangi senaryolar, kaç puan, ne zaman düşecek)
- Karar kaydı görüntüleme, işyeri risk paneli
- RAPORLAR sayfası tanımlandığında raporlama

---

## 9. Karar Bekleyen Ana Sorular

1. **Kapsam:** FraudGuard acquiring (üye işyeri) tarafına da mı açılacak, yoksa senaryolar mevcut issuer
   modeline mi uyarlanacak? *(Bu, planın tamamını belirleyen tek soru.)*
2. **Hedef:** 60 senaryonun tamamı mı, yoksa Grup A+B ile başlayan bir MVP mi?
3. **Referans verileri:** BIN tablosu, OFAC listesi, riskli BIN/ülke listeleri temin edilebilecek mi?
4. **Skorlama:** Mevcut `CalculateRiskScore` (0-100 türetilmiş) tamamen Excel modeliyle mi değişecek,
   yoksa iki skor bir süre yan yana mı yaşayacak?
5. **DB:** Mevcut seed/veri korunacak mı, yoksa migration'a geçerken sıfırdan mı kurulacak?
6. **Simülatör:** İşyeri trafiği üretecek şekilde genişletilecek mi? (Senaryoların test edilebilirliği buna bağlı.)

---

## 10. Riskler

| Risk | Etki | Önlem |
|---|---|---|
| Migration'sız şema değişikliği | Mevcut DB kullanılamaz hale gelir | Faz 1'i atlamamak |
| Örtüşen senaryolar (S1/S7, S2/S38) | Yanlış pozitif patlaması, herkes RET yer | Faz 0'da eşik kalibrasyonu + shadow mode |
| Tüm senaryoların her işlemde çalışması | Gecikme artışı | Redis sayaçları + batch baseline + senaryo bazlı erken çıkış |
| S55 gibi lokasyon bazlı toptan ret | Uyum/hukuk riski | RET yerine izleme; hukuk onayı |
| 60 senaryonun tek seferde devreye alınması | Kontrolsüz üretim davranışı | **Shadow mode**: skor hesapla, aksiyon alma, ölç, sonra aç |
| Referans listelerinin gelmemesi | Grup C (10 senaryo) yazılamaz | Faz 0'da netleştir, planı buna göre daralt |
