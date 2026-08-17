# Fraud Kuralı Yazma Kılavuzu

Bu belge, FraudGuard'a yeni bir fraud kuralı eklemek için gereken her şeyi içerir.
Kural eklemek için **kod yazmak gerekmez** — kural bir veritabanı satırıdır.

---

## 1. Kural nedir

Bir kural, işlem verisi üzerinde çalışan boolean bir ifadedir. `true` dönerse kural tetiklenir
ve puanı hedefin risk skoruna eklenir.

```
input.AyniKartIslemAdedi >= 3
```

Motor, aktif kuralların **tamamını** çalıştırır ve puanları toplar. İlk eşleşmede durmaz.
Toplam skor, kararı belirler:

| Skor | Karar | Ne olur |
|---|---|---|
| 0 – 39 | `NORMAL` | İşlem onaylanır |
| 40 – 69 | `IZLE` | İşlem geçer, analist paneline alarm düşer |
| 70 – 89 | `EK_DOGRULAMA` | 3D Secure / OTP zorunlu |
| 90+ | `RET_BLOKE` | İşlem reddedilir, kart bloke edilir |

Nihai skor: `(kural puanları + kombinasyon bonusu) − güven indirimi`, en az 0.

---

## 2. Kural ekleme — üç yol

### A. API ile (önerilen)

Kaydetmeden önce ifadeyi doğrula:

```bash
curl -X POST http://localhost:5217/api/RuleManagement/validate-expression \
  -H "Content-Type: application/json" \
  -d '{"expression":"input.AyniKartIslemAdedi >= 3"}'
```

Geçerliyse kuralı oluştur:

```bash
curl -X POST http://localhost:5217/api/RuleManagement/rules \
  -H "Content-Type: application/json" \
  -d '{
    "ruleCode": "S13",
    "ruleName": "Kural adı",
    "description": "Ne yaptığı",
    "expression": "input.AyniKartIslemAdedi >= 3",
    "score": 30,
    "target": "Card",
    "category": "Velocity",
    "isActive": true
  }'
```

Kural **bir sonraki işlemden itibaren** aktiftir. Motor kural listesini önbelleğe almaz,
yeniden başlatma gerekmez.

Geçersiz ifade kaydedilmez — API `400` ve derleyici hatasını döner.

### B. Doğrudan SQL ile

```sql
INSERT INTO FraudRules (RuleCode, RuleName, Description, Expression, Score, Target, Category, IsActive)
VALUES ('S13', 'Kural adı', 'Açıklama', 'input.AyniKartIslemAdedi >= 3', 30, 1, 1, 1);
```

`RuleId` identity kolonudur, elle verme. `Target`: 1=Card, 2=Merchant.
`Category`: 1=Velocity, 2=Amount, 3=Time, 4=Identity, 5=Location.

> Bu yol ifadeyi doğrulamaz. Yanlış yazarsan kural sessizce atlanır — API yolunu tercih et.

### C. Seed'e ekleyerek (kalıcı)

`Backend/FraudGuard.Infrastructure/Persistence/SeedData/FraudRuleSeedData.cs`:

```csharp
Dynamic(38, "S13", "Kural adı",
    "input.AyniKartIslemAdedi >= 3",
    "Açıklama",
    30, RuleCategoryEnum.Velocity),
```

Proje `EnsureCreated()` kullandığı için bu ancak veritabanı sıfırdan kurulduğunda uygulanır:

```bash
docker compose down -v && docker compose up -d --build
```

Kalıcı kural seti için doğru yer burasıdır. Günlük deneme için A yolunu kullan.

---

## 3. İfadede kullanılabilecek alanlar

Canlı liste:

```bash
curl http://localhost:5217/api/RuleManagement/available-fields
```

### İşlem alanları
| Alan | Tip |
|---|---|
| `Amount` | decimal |
| `Currency` | string |
| `MerchantCategory` | string |
| `Country` | string |
| `Location` | string |
| `ChannelTypeId` | int (1=POS, 2=SanalPOS, 3=ATM, 4=Mobil, 5=Web) |
| `TransactionType` | enum (Sale, Refund, Deposit, CardPayment) |
| `PaymentType` | enum (CreditCard, DebitCard, BankTransfer, EFT) |

### Zaman alanları
| Alan | Tip | Açıklama |
|---|---|---|
| `IslemSaati` | int | 0–23 |
| `HaftaSonuMu` | bool | Cumartesi/Pazar |
| `GeceIslemiMi` | bool | 22:00–06:00 |
| `IslemZamani` | DateTime | |

### Sayaçlar (karta ait son 24 saatten hesaplanır)
| Alan | Tip | Açıklama |
|---|---|---|
| `AyniKartIslemAdedi` | int | Son 1 saatteki işlem adedi (bu işlem dahil) |
| `IkiDakikadaYapilanIslemAdedi` | int | Son 2 dakika (bu işlem dahil) |
| `BasarisizIslemSayisi` | int | Son 24 saatte onaylanmayan işlem |
| `SonGunIslemSayisi` | int | Son 24 saatteki toplam adet |
| `SonGunIslemHacmi` | decimal | Son 24 saatteki toplam tutar |
| `ToplamSatisTutar` | decimal | Son 24 saatteki onaylı satış |
| `ToplamIadeTutari` | decimal | Son 24 saatteki iade |
| `IkiSaatlikIadeIslemSayisi` | int | Son 2 saatteki iade adedi |
| `BinAltindaOnayliIslemAdedi` | int | Son 1 saatte 1.000 TL altı onaylı işlem |
| `AyniKartAyniTutarAdedi` | int | Aynı tutarlı işlem adedi (bu işlem dahil) |
| `FarkliKategoriSayisi` | int | Farklı işyeri kategorisi |
| `FarkliUlkeSayisi` | int | Farklı ülke |
| `GeceIslemAdedi` | int | Gece penceresindeki işlem |
| `EnYuksekIslemTutari` | decimal | Son 24 saatteki maksimum |
| `OrtalamaIslemTutari` | decimal | Son 24 saatteki ortalama (geçmiş yoksa 0) |
| `SonIslemdenGecenDakika` | int | Geçmiş yoksa `int.MaxValue` |

### ⚠️ Dolmayan alanlar
Aşağıdakiler modelde tanımlıdır ama çalışma anında **boştur**. Bunları kullanan kural hiç tetiklenmez:

`MerchantId` · `MccKodu` · `FarkliKartSayisi` · `FarkliIsyeriSayisi` · `PosTahsisTarihi`

Bunlar işyeri (merchant) bazlı sayaçlardır; sistemde `Merchant` varlığı oluşturulana kadar doldurulamaz.

---

## 4. İfade sözdizimi

C# alt kümesi. Desteklenenler:

```csharp
input.Amount >= 50000                              // karşılaştırma
input.GeceIslemiMi && input.Amount >= 10000        // ve
input.FarkliUlkeSayisi >= 3 || input.Amount > 1e6  // veya
input.MerchantCategory == "Kuyumcu"                // string eşitlik
input.Amount > 4 * input.OrtalamaIslemTutari       // aritmetik
input.IslemSaati >= 22 || input.IslemSaati < 6     // saat aralığı
!input.HaftaSonuMu                                 // değilleme
```

Kurallar:
- Parametre adı **her zaman `input`**
- İfade **bool** dönmeli
- String değerler **çift tırnak** içinde
- Yalnızca `input` üzerindeki alanlara erişilebilir

### Sık yapılan hata: kaybolan tırnaklar

Terminalden SQL çalıştırırken çift tırnaklar kabuk tarafından yutulabilir:

```
input.MerchantCategory == TestKategori     ← BOZUK, tırnaklar gitmiş
```

Bu ifade derlenemez, kural sessizce atlanır. SQL tarafında `CHAR(34)` kullan ya da API yolunu tercih et.

---

## 5. Kuralı test etme

Bir işlem gönder ve yanıta bak:

```bash
curl -X POST http://localhost:5217/api/transactions/process \
  -H "Content-Type: application/json" \
  -d '{
    "cardNumber":"5520000000000018","expiryDate":"12/28","cvv":"101",
    "amount":75000,"currency":"TRY","transactionType":1,"paymentType":1,
    "channelTypeId":2,"location":"Istanbul","country":"Turkiye",
    "merchantCategory":"Elektronik"
  }'
```

Yanıtta göreceklerin:

```json
{
  "decision": "IZLE",
  "riskScore": 45,
  "rawRuleScore": 65,
  "totalBonusScore": 0,
  "totalTrustDiscount": 20,
  "triggeredRules": [
    { "ruleCode": "S1", "score": 35, "category": "Velocity" },
    { "ruleCode": "S2", "score": 30, "category": "Velocity" }
  ],
  "trustFactors": ["Kartta son 90 günde alarm yok (-20P)"],
  "ruleFailures": []
}
```

**`ruleFailures` doluysa** bir kuralın ifadesi bozuktur. Aynı hata backend logunda da görünür:

```bash
docker logs fraudguard-backend --tail 50 | grep "KURAL DEĞERLENDİRİLEMEDİ"
```

### Kuralın tetiklenmemesi

Sırayla kontrol et:
1. `IsActive = 1` mi?
2. İfade doğrulamadan geçiyor mu? → `validate-expression`
3. Kullandığın alan gerçekten doluyor mu? → `available-fields`, `isPopulated` alanına bak
4. Yanıtta `ruleFailures` var mı?
5. İade kuralı mı yazdın? İade kuralları yalnızca `transactionType=2` işlemlerde çalışır

---

## 6. Puanlama tavsiyesi

Puanlar toplandığı için tekil ağırlıkları RET eşiğinin (90) belirgin altında tut.
Tek bir kuralın tek başına işlemi reddetmesi genelde istenmez.

| Sinyalin gücü | Önerilen puan |
|---|---|
| Zayıf ipucu | 5 – 20 |
| Orta | 20 – 35 |
| Güçlü | 35 – 50 |

İki kural aynı olguyu ölçüyorsa ikisi birden tetiklenir ve **çift puan** yazar.
Örnek: `S1` (2 dakikada 3 işlem) ve `S2` (1 saatte 3 işlem) üç işlemlik bir seride birlikte
tetiklenip 65 puan yazar. Yeni kural yazarken mevcut kataloğu kontrol et:

```bash
curl http://localhost:5217/api/RuleManagement/all-rules
```

---

## 7. Kombinasyon bonusu

Birlikte tetiklenen kurallara ek puan verilebilir. `RuleCombinations` tablosuna satır ekle:

```sql
INSERT INTO RuleCombinations (CombinationName, RuleCodes, Target, BonusScore, FraudType, IsActive)
VALUES ('Kart Testi + Cashout', 'S3,S5', 1, 20, 'Kart test edildi, sonra vuruldu.', 1);
```

`RuleCodes` içindeki **tüm** kodlar aynı işlemde tetiklenirse bonus bir kez eklenir.

---

## 8. Bilinen kısıtlar

- **Güven indirimi kesin kuralları da düşürür.** Whitelist'teki bir kart 40 puan indirim aldığı için
  çok yüksek puanlı bir kural bile eşiğin altında kalabilir. `IsCritical` benzeri bir muafiyet alanı yok.
- **Puan yaşlanması (decay) yok.** Puanlar yalnızca o işlem için hesaplanır, zaman içinde birikmez.
- **İşyeri skoru hesaplanmaz.** `Target = Merchant` kuralları yazılabilir ama sayaçları dolmadığı için
  pratikte tetiklenmezler.
