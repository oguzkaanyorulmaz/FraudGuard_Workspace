-- Fraud motorunun skorunu işlem kayıtlarına taşıyan kolonlar.
--
-- Neden gerekli: uygulama şemayı EnsureCreated / CreateTables ile kuruyor.
-- Bu yol yalnızca tablo HİÇ yoksa çalışır; var olan tabloya kolon eklemez.
-- Dolayısıyla mevcut bir veritabanında bu script elle çalıştırılmalıdır,
-- yoksa "Invalid column name 'RiskScore'" hatası alınır.
--
-- Alternatif: veritabanını sıfırlamak (docker compose down -v) — tüm veri gider.
--
-- Çalıştırma:
--   docker exec -i fraudguard-db /opt/mssql-tools18/bin/sqlcmd \
--     -S localhost -U sa -P 'FraudGuard2026_!' -C -d FraudGuard \
--     -i docs/migration-risk-score.sql

SET NOCOUNT ON;

-- CreditCardTransactions -----------------------------------------------------
IF COL_LENGTH('dbo.CreditCardTransactions', 'RiskScore') IS NULL
    ALTER TABLE dbo.CreditCardTransactions
        ADD RiskScore INT NOT NULL CONSTRAINT DF_CCTx_RiskScore DEFAULT 0;

IF COL_LENGTH('dbo.CreditCardTransactions', 'RiskDecision') IS NULL
    ALTER TABLE dbo.CreditCardTransactions
        ADD RiskDecision INT NOT NULL CONSTRAINT DF_CCTx_RiskDecision DEFAULT 0;

-- DebitCardTransactions ------------------------------------------------------
IF COL_LENGTH('dbo.DebitCardTransactions', 'RiskScore') IS NULL
    ALTER TABLE dbo.DebitCardTransactions
        ADD RiskScore INT NOT NULL CONSTRAINT DF_DCTx_RiskScore DEFAULT 0;

IF COL_LENGTH('dbo.DebitCardTransactions', 'RiskDecision') IS NULL
    ALTER TABLE dbo.DebitCardTransactions
        ADD RiskDecision INT NOT NULL CONSTRAINT DF_DCTx_RiskDecision DEFAULT 0;

-- TransferTransactions -------------------------------------------------------
IF COL_LENGTH('dbo.TransferTransactions', 'RiskScore') IS NULL
    ALTER TABLE dbo.TransferTransactions
        ADD RiskScore INT NOT NULL CONSTRAINT DF_TrTx_RiskScore DEFAULT 0;

IF COL_LENGTH('dbo.TransferTransactions', 'RiskDecision') IS NULL
    ALTER TABLE dbo.TransferTransactions
        ADD RiskDecision INT NOT NULL CONSTRAINT DF_TrTx_RiskDecision DEFAULT 0;

-- Not: RiskDecision değerleri RiskDecisionEnum ile eşleşir —
-- 0=Normal, 1=Izle, 2=EkDogrulama, 3=RetBloke.
--
-- Mevcut (script öncesi) kayıtlar 0 / Normal olarak kalır. Bu kayıtların gerçek
-- skoru hiç saklanmamıştı; panelde 0 görünmeleri beklenen davranıştır.
-- Skorun metinsel izi FraudReason kolonunda duruyor ("[Izle] Skor 65 — ...").

PRINT 'RiskScore / RiskDecision kolonları hazır.';
