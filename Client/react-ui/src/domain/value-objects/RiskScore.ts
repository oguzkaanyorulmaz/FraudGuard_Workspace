/**
 * Kümülatif risk skoru.
 *
 * Eşikler backend'deki RiskScoringConstants ile birebir aynıdır; biri değişirse
 * diğeri de değişmelidir. Skorun üst sınırı YOKTUR: motor tetiklenen tüm kuralların
 * puanını toplar, kombinasyon bonusu ekler ve yalnızca tabanı 0'a sabitler.
 * Bu yüzden 100'ün üzerinde skor normaldir ve hata değildir.
 */

/** Backend: Backend/FraudGuard.Domain/Common/Constants/RiskScoringConstants.cs */
export const RISK_THRESHOLDS = {
    /** 40+ : analist paneline alarm düşer */
    IZLE: 40,
    /** 70+ : 3D Secure / OTP zorunlu */
    EK_DOGRULAMA: 70,
    /** 90+ : işlem reddedilir, hedef bloke edilir */
    RET_BLOKE: 90
} as const;

/** Backend'deki RiskDecisionEnum'un istemci karşılığı. */
export type RiskTier = 'NORMAL' | 'IZLE' | 'EK_DOGRULAMA' | 'RET_BLOKE';

const TIER_LABELS: Record<RiskTier, string> = {
    NORMAL: 'Normal',
    IZLE: 'İzle',
    EK_DOGRULAMA: 'Ek Doğrulama',
    RET_BLOKE: 'Ret / Bloke'
};

export class RiskScore {
    private readonly value: number;

    constructor(score: number) {
        // Skor 0 ile 100 arasında sınırlandırılır
        this.value = Number.isFinite(score) && score > 0 ? Math.min(100, Math.round(score)) : 0;
    }

    getValue(): number {
        return this.value;
    }

    getTier(): RiskTier {
        if (this.value >= RISK_THRESHOLDS.RET_BLOKE) return 'RET_BLOKE';
        if (this.value >= RISK_THRESHOLDS.EK_DOGRULAMA) return 'EK_DOGRULAMA';
        if (this.value >= RISK_THRESHOLDS.IZLE) return 'IZLE';
        return 'NORMAL';
    }

    getLabel(): string {
        return TIER_LABELS[this.getTier()];
    }

    /**
     * İlerleme çubuğunun genişliği (%). Skorun kendisi 100'ü aşabildiği için
     * çubuğun taşmaması adına kırpılır — gösterilen sayı kırpılmaz.
     */
    getBarPercent(): number {
        return Math.min(this.value, 100);
    }

    /** Skor 100'ü aştı mı — çubuğun dolu olduğunu ayrıca belirtmek için. */
    isOverflowing(): boolean {
        return this.value > 100;
    }

    isCritical(): boolean {
        return this.value >= RISK_THRESHOLDS.RET_BLOKE;
    }

    isHighRisk(): boolean {
        return this.value >= RISK_THRESHOLDS.EK_DOGRULAMA;
    }

    isMediumRisk(): boolean {
        return this.value >= RISK_THRESHOLDS.IZLE && this.value < RISK_THRESHOLDS.EK_DOGRULAMA;
    }
}
