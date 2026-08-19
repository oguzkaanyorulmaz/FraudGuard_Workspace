# FraudGuard

Gerçek zamanlı fraud tespit ve karar sistemi. Kurallar koda gömülü değildir — string
ifade olarak veritabanında saklanır, çalışma anında derlenir ve mikrosaniyeler içinde
çalıştırılır. Yeni bir kural eklemek için derleme veya yeniden başlatma gerekmez.

.NET 10 · Clean Architecture · React · SQL Server · Redis · Docker

---

## Neden dinamik kural motoru

Klasik yaklaşımda her fraud kuralı ayrı bir `.cs` dosyasıdır: kural eklemek kod yazmak,
derlemek ve yeniden dağıtmak demektir. Fraud kuralları ise sık değişir — eşik
güncellemesi, yeni bir tipoloji, bir kuralın geçici olarak kapatılması.

FraudGuard'da kural bir veritabanı satırıdır:

```csharp
input.BinAltindaOnayliIslemAdedi >= 5 && input.Amount >= 50000
```

İfade `DynamicExpresso` ile Expression Tree'ye çevrilip derlenir; oluşan delegate kural
kodu bazında önbelleğe alınır. Maliyet yalnızca ilk derlemededir.

**Ölçüm:** 12 kuralın tam değerlendirmesi **0,0006 ms**.

---

## Karar modeli

Motor **ilk eşleşmede durmaz**. Tüm aktif kuralları çalıştırır, puanları biriktirir ve
kademeli karar üretir.

```
geçmiş yükle → sayaçları zenginleştir → tüm kuralları çalıştır
   → kombinasyon bonusu → güven indirimi → karar
```

| Skor | Karar | Sonuç |
|---|---|---|
| 0 – 39 | `NORMAL` | İşlem onaylanır |
| 40 – 69 | `IZLE` | İşlem geçer, analist paneline alarm düşer |
| 70 – 89 | `EK_DOGRULAMA` | 3D Secure / OTP zorunlu |
| 90+ | `RET_BLOKE` | İşlem reddedilir, kart bloke edilir |

Nihai skor: `(kural puanları + kombinasyon bonusu) − güven indirimi`

**Kombinasyon bonusu** — tek başına orta seviyede kalan sinyaller birlikte görülünce ek
puan alır. Kart testi ardından yüksek tutarlı çekim, ayrı ayrı zayıf; birlikte güçlü.

**Güven skoru** — yerleşik ve temiz geçmişe sahip hedeflerde skoru düşürür, yanlış
pozitifleri azaltır. Deterministik yaptırım kuralları (`IsCritical`) bu indirimden
**muaftır**: whitelist bir yaptırım sinyalini bastıramaz.

Örnek — üç kural birikip eşiği aşıyor:

```
AMOUNT_OVER_1M(30P) + WALLET_CASHOUT(40P) + RECEIVER_BALANCE_ANOMALY(30P)
→ skor 80 → EK_DOGRULAMA
```

---

## Kural yazmak

Arayüzden (Kural Yönetimi sekmesi) veya API ile. İfade **kaydedilmeden önce derlenir**;
geçersizse kural oluşturulmaz.

```bash
curl -X POST http://localhost:5217/api/RuleManagement/validate-expression \
  -H "Content-Type: application/json" -H "Authorization: Bearer $TOKEN" \
  -d '{"expression":"input.AyniKartIslemAdedi >= 3"}'
```

Kullanılabilir alanlar iki kümededir:

**`input.X`** — sistemin hesapladığı alanlar. İşlem verisi, geçmişten türeyen sayaçlar
(hız, hacim, çeşitlilik, iade), zaman göstergeleri, işyeri master verisi ve BIN
tablosundan gelen kart bilgileri. Her zaman doludur.

**`input.Auth.X`** — çağıranın gönderdiği ham yetkilendirme mesajı alanları (PIN varlığı,
temassız, 3D Secure, e-ticaret, giriş yöntemi…). Tümü nullable'dır ve bu kasıtlıdır:

```csharp
input.Auth.PinExist == false          // doğru
!input.Auth.PinExist                  // derlenmez
```

Fraud değerlendirmesinde *"bilinmiyor"* ile *"hayır"* aynı şey değildir. Nullable tipler
bu ayrımı derleme anında zorunlu kılar — alan gönderilmediğinde kural sessizce yanlış
pozitif üretemez.

Ayrıntı: [docs/KURAL_YAZMA_KILAVUZU.md](docs/KURAL_YAZMA_KILAVUZU.md)

---

## Mimari

```
FraudGuard.Domain          ← sıfır NuGet bağımlılığı
  Entities, DomainObjects, Interfaces
  Services/RuleEngine      DynamicRuleEngine, CombinationEngine,
                           TrustScoreService, RiskScoringService,
                           TransactionInputEnricher

FraudGuard.Application     → Domain
  Orkestrasyon: FraudEvaluationService, TransactionService
  DTO, mapping, validation

FraudGuard.Infrastructure  → Domain
  EF Core, repositories, seed
  DynamicExpressoRuleCompiler, CachedReferenceDataProvider
  Redis / MemoryCache sağlayıcıları

FraudGuard.API             → Application + Infrastructure
  Controllers, SignalR hub, JWT middleware
```

Bağımlılık oku baştan sona içeri bakar. Domain katmanı hiçbir pakete bağlı değildir;
ifade derleyicisi `IRuleExpressionCompiler` soyutlamasının arkasındadır.

**Saf domain servisleri.** `TransactionInputEnricher`, `RiskScoringService`,
`TrustScoreService` ve `CombinationEngine` veriye erişmez — girdiyi hazır alır. Veri
toplama orkestratörün işidir. Bu sayede skorlama ve eşik davranışı bağımlılıksız test
edilebilir.

---

## Bileşenler

| Bileşen | Teknoloji |
|---|---|
| API | .NET 10, ASP.NET Core, JWT, SignalR, Swagger |
| Kural motoru | DynamicExpresso |
| Veri | EF Core, SQL Server |
| Önbellek | Redis (StackExchange) |
| Analist paneli | React, SignalR client |
| Masaüstü | Electron bridge |
| Simülatör | Node.js — işlem üretimi ve kural yönetimi arayüzü |

---

## Çalıştırma

```bash
docker compose up -d --build
```

| Servis | Adres |
|---|---|
| Analist paneli | http://localhost:3000 |
| API / Swagger | http://localhost:5217 · `/swagger` |
| İşlem simülatörü | http://localhost:4000 |

Örnek kullanıcılar: `admin` / `admin123` · `karar` / `karar123` · `analist` / `analist123`

Veritabanı ilk açılışta kurulur ve örnek verilerle doldurulur. Şema değişiklikleri
açılışta uygulanır; kural kataloğu, referans verisi ve işyerleri **yalnızca eksik
kayıtlar eklenerek** uzlaştırılır — mevcut veriye dokunulmaz.

---

## API

| Uç | Ne yapar |
|---|---|
| `POST /api/transactions/process` | Kart işlemi — satış, iade, para yatırma, borç ödeme |
| `POST /api/transactions/transfer` | IBAN transferi |
| `GET /api/RuleManagement/active-rules` | Aktif kural kataloğu |
| `GET /api/RuleManagement/available-fields` | İfadelerde kullanılabilen alanlar |
| `POST /api/RuleManagement/validate-expression` | İfadeyi kaydetmeden doğrula |
| `POST /api/RuleManagement/rules` | Kural oluştur |
| `PATCH /api/RuleManagement/rules/{id}/status` | Aktif / pasif |
| `GET /api/FraudManagement/unresolved-logs` | Bekleyen alarmlar |
| `POST /api/FraudManagement/resolve-log` | Alarmı sonuçlandır |

İşlem yanıtı yalnızca kararı değil, **ona nasıl ulaşıldığını** da döner: tetiklenen
kurallar ve puanları, uygulanan kombinasyon bonusları, güven faktörleri ve
değerlendirilemeyen kurallar.

```json
{
  "decision": "IZLE",
  "riskScore": 45,
  "rawRuleScore": 65,
  "totalTrustDiscount": 20,
  "triggeredRules": [
    { "ruleCode": "S1", "score": 35, "category": "Velocity" },
    { "ruleCode": "S2", "score": 30, "category": "Velocity" }
  ],
  "trustFactors": ["Kartta son 90 günde alarm yok (-20P)"],
  "ruleFailures": []
}
```

`ruleFailures` boş değilse bir kuralın tanımı bozuktur. Bozuk kural ödeme akışını
düşürmez — atlanır, loglanır ve yanıtta raporlanır. Sessizce kaybolmaz.

---

## Tasarım notları

**Bozuk kural ödemeyi durdurmaz, ama gizlenmez de.** Her kural kendi try/catch'inde
çalışır; hata hem loga hem API yanıtına düşer. İfade geçerliliği ayrıca yazma anında
denetlenir.

**Hesaplanmayan alanlar ilan edilir.** `TransactionInputEnricher.UnpopulatedFields`,
modelde tanımlı ama doldurulmayan alanları listeler. Böyle bir alanı kullanan kural
derlenir ve aktif görünür ama hiç tetiklenmez — yani sessizce ölüdür. Kural yazma
arayüzü bu listeyi okuyup yazarı önden uyarır.

**Migration yoktur.** Şema `EnsureCreated` ile kurulur, sonraki değişiklikler açılışta
koşullu `ALTER TABLE` ile uygulanır. Veritabanı sıfırlanmadan güncellenir.

---

## Yol haritası

- Batch istatistik katmanı — haftalık/aylık/sektör baseline gerektiren senaryolar
- Kural ifadesini güncelleyen API ucu
- Yetkilendirme mesajı alanlarının kalıcılaştırılması (geçmişe bakan sayaçlar için)
- Gerçek BIN tablosu entegrasyonu

Ayrıntılı durum ve mimari sözleşmeler: [docs/DEVIR_NOTU.md](docs/DEVIR_NOTU.md)

---

## Not

Bu bir öğrenme ve portföy projesidir. Örnek veriler, BIN kayıtları ve kart numaraları
tamamen üretilmiştir; gerçek bir kuruma ait veri içermez.
