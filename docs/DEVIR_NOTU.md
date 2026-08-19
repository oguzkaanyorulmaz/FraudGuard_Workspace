# Devir Notu — Dinamik Kural Motoru

> Bu dosya, oturum bağlamı dolduğunda çalışmanın nereden devam edeceğini anlatır.
> Dal: `feat/dinamik-kural-motoru` · Son commit: `8d0d873`

---

## Sistemin bugünkü hâli

**Kural motoru %100 dinamik.** Kod tabanlı kural sınıfı kalmadı; her kural bir
veritabanı satırındaki string ifadedir, `DynamicExpresso` ile çalışma anında derlenir
ve `RuleCode` bazında önbelleğe alınır.

| | |
|---|---|
| Kural kataloğu | 81 kural — 66 aktif, 15 pasif |
| Pasif olanlar | Veri kaynağı olmayan senaryolar; `Expression = "false"`, açıklamada `[ENGEL: ...]` |
| Excel kapsamı | 60 senaryonun tamamı katalogda |
| Doğrulanan | 66 aktif kuralın 45'i canlı işlemle en az bir kez tetiklendi |
| `ruleFailures` | Tüm test turlarında 0 |
| Derleme | 0 hata, 0 uyarı |

**Karar akışı:** geçmiş yükle → sayaçları zenginleştir → tüm kuralları çalıştır →
kombinasyon bonusu → güven indirimi → 4 kademeli karar (`NORMAL` / `IZLE` /
`EK_DOGRULAMA` / `RET_BLOKE`).

---

## Mimari sözleşmeler — bozmayın

**Domain paket bağımsızdır.** `FraudGuard.Domain.csproj` sıfır NuGet referansı taşır.
`DynamicExpresso` yalnızca Infrastructure'da; Domain'de `IRuleExpressionCompiler`
soyutlaması var.

**`TransactionInputEnricher` saftır.** Repository'ye erişmez, `static`'tir. Veri
gerektiren her gösterge orkestratör (`FraudEvaluationService`) tarafından hazır
geçilir — merchant, alıcı bağlamı ve referans verisi böyle çalışır. Yeni bir
gösterge eklerken bu deseni koruyun.

**`UnpopulatedFields` sözleşmesi.** `TransactionInputEnricher.UnpopulatedFields`,
modelde tanımlı ama hesaplanmayan alanları listeler. Yeni sayaç eklerken ilgili
satırı **silin**; hesaplanmayan alan eklerken **ekleyin**. Arayüz bu listeyi okuyup
kural yazarını uyarır. Liste kaymışsa ölü kural sessizce oluşur.

**Uzlaştırma deseni.** Proje `EnsureCreated()` kullanır, migration yoktur. Seed
yalnızca boş veritabanına uygulanır. Bu yüzden `Program.cs` açılışta üç şeyi
uzlaştırır: kural kataloğu, referans verisi (BIN + listeler), işyerleri. Hepsi
**yalnızca eksik olanı ekler**, mevcut kayda dokunmaz. Yeni bir seed tablosu
eklerseniz aynı uzlaştırmayı yazın, yoksa veriniz mevcut kurulumlara hiç ulaşmaz.

**Kesin kurallar güven indiriminden muaftır.** `EFraudRule.IsCritical` işaretli
kuralların puanı indirimsiz eklenir. Deterministik yaptırım sinyalleri (yaptırım
BIN'i, bloke alıcı hesap) için kullanılır; sezgisel kurallara koymayın.

---

## Kural yazmak

Ayrıntı: [KURAL_YAZMA_KILAVUZU.md](KURAL_YAZMA_KILAVUZU.md)

Özet: arayüzden (`localhost:4000` → Kural Yönetimi) ya da API ile
(`POST /api/RuleManagement/rules`). İfade kaydedilmeden derlenir; geçersizse
kaydedilmez. Kullanılabilir alanlar: `GET /api/RuleManagement/available-fields`
(202 alan — 71 kök + 131 `Auth.*`).

`input.X` sistemin hesapladığı, her zaman dolu alanlar.
`input.Auth.X` çağıranın gönderdiği ham auth mesajı alanları; hepsi nullable,
`== true` / `== false` ile yazılır. Gönderilmezse `null` kalır ve kural tetiklenmez.

---

## Sıradaki işler

### 1. Kural ifadesini güncelleyecek API ucu yok
`PATCH .../rules/{id}/status` yalnızca aktif/pasif yapıyor. Bir kuralın ifadesini
değiştirmek için SQL gerekiyor. `PUT /api/RuleManagement/rules/{id}` eklenmeli —
ifade doğrulaması `CreateRuleAsync`'teki gibi olmalı.

### 2. Batch istatistik katmanı → 13 senaryo açar
Pasif duran 13 kural haftalık/aylık/sektör baseline'ı bekliyor:
`WEEKLY_ANOMALY_TRACKING`, `DECLINE_RATE_SURGE`, `FLAT_AMOUNT_RATIO`,
`REPEATED_FIXED_AMOUNT`, `DAILY_ANOMALY_TRACKING`, `SECTOR_TURNOVER_EXCESS`,
`SECTOR_COUNT_EXCESS`, `VOLUME_VS_WEEKLY_BASELINE`, `NIGHT_DEVIATION_TARGETED`,
`NIGHT_COUNT_INCREASE`, `NIGHT_AMOUNT_INCREASE`, `DAILY_AMOUNT_SURGE`,
`WEEKLY_COUNT_SURGE`.

Gerekli: gecelik çalışan `MerchantDailyStats` / `MerchantNightStats` / `SectorStats`
tabloları ve bunları üreten bir `BackgroundService`. Her işlemde 30 günlük tarama
yapılamaz.

### 3. Hamil iletişim bilgisi → 2 senaryo açar
`SAME_CARD_MULTIPLE_PHONES`, `SAME_CARD_MULTIPLE_EMAILS`. İşlem kaydında kart
hamiline ait telefon/e-posta yok; `ECustomer` üzerindeki değerler işlem bazında
değişmediği için "2 farklı telefon" ölçülemiyor.

### 4. Auth alanlarının kalıcılaştırılması → `S19` açar
`Auth.*` alanları yalnızca input'ta yaşıyor, işlem tablolarına yazılmıyor. Geçmişe
bakan sayaçlar (ör. "son 1 saatte 5 hatalı PIN") için ilgili alanların
kalıcılaştırılması gerekiyor. Yol: alanı entity'ye ekle → `Program.cs` şema
senkronizasyonuna `ALTER TABLE` satırı → enricher'da sayacı hesapla → kuralı yaz.

### 5. Gerçek BIN tablosu
`ReferenceDataSeed` içindeki 9 BIN **örnek veridir**. Üretim için kurumun BIN
dosyasıyla değiştirilmeli. `CachedReferenceDataProvider` 10 dakikalık TTL ile
süreç belleğinde tutuyor, büyük tabloyu kaldırır.

### 6. Kalan teknik borç
- Anemik model: yalnızca kart bloke etme davranışı entity'ye taşındı, gerisi veri torbası.
- `MULTI_SOURCE_FUNDING` "farklı kart" boyutunu ölçemiyor (yatırma işlemi kaynak kart
  taşımıyor); fonlama sıklığı üzerinden çalışıyor.
- Güven indirimi hâlâ toplam skoru düşürüyor; Excel'in "eşiği yükselt" modeli değil.
  Fark: whitelist'li bir hedefte kesin olmayan kurallar bastırılabiliyor.

---

## Test etme

`docs/` altında test betiği yok; oturum boyunca kullanılanlar geçici dizindeydi.
Canlı doğrulama yaparken şu üç tuzağa dikkat:

1. **Kart bloke olur.** `RET_BLOKE` kararı kartı bloke eder; sonraki çağrılar
   değerlendirmeye girmeden reddedilir. Her adımdan önce blokeyi kaldırın.
2. **SQL ile yapılan değişiklik önbelleği tazelemez.** Kart bilgisi `card_info_*`
   anahtarında. SQL ile bloke kaldırırsanız uygulama hâlâ blokeli görür —
   backend'i yeniden başlatın.
3. **İşlem geçmişi birikir.** Aynı kartla tekrar test edince hız kuralları
   tetiklenir. Temiz sonuç için kartın işlem kayıtlarını silin.

Bir kural tetiklenmiyorsa **önce `status` alanına bakın**: `Declined` ise işlem
fraud değerlendirmesine hiç girmemiştir.

---

## Ortam

```bash
docker compose up -d --build          # tümü
docker compose up -d --build backend  # yalnızca backend
```

| Servis | Adres |
|---|---|
| Analist paneli | http://localhost:3000 |
| API / Swagger | http://localhost:5217 · `/swagger` |
| Simülatör | http://localhost:4000 |
| SQL Server | `localhost,1433` — sa / `FraudGuard2026_!` |
| Redis | `localhost:6379` — anahtar öneki `fraudguard:` |

Giriş: `admin` / `admin123`

> Şema değişikliği `Program.cs` içindeki `ExecuteSqlRaw` bloğuyla uygulanır.
> Veritabanını sıfırlamak gerekmez; `docker compose down -v` yalnızca sıfırdan
> kurulum isteniyorsa kullanılır.
