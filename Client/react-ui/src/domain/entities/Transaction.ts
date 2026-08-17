import { Money } from '../value-objects/Money';

export interface TransactionProps {
    id: string;
    transactionId: string;
    maskedCard: string;
    money: Money;
    ruleName: string;
    suspicionReason: string;
    riskScore: number;
    location: string;
    date: string;
    fraudReason?: string;
    ruleCode?: string;
    paymentType?: string;

}

export const RULE_NAMES: Record<string, string> = {
    VELOCITY: 'Hız / Sıklık',
    IMPOSSIBLE_TRAVEL: 'İmkansız Seyahat',
    ANOMALOUS_TIME: 'Gece Yüksek Tutar',
    CARD_TESTING: 'Kart Yoklama',
    BRUTE_FORCE: 'Ardışık Red',
    CROSS_BORDER: 'Sınır Ötesi İlk İşlem',
    HIGH_RISK_MCC: 'Yüksek Riskli MCC',
    MAX_OUT: 'Limit Boşaltma',
    CURRENCY_MISMATCH: 'Para Birimi Sapması',
    CONSECUTIVE_REFUNDS: 'Ardışık İade',
    SMURFING: 'Smurfing (Dilimleme)',
    WALLET_CASHOUT: 'Wallet Cash-Out',
    MULTI_SOURCE_FUNDING: 'Çoklu Fonlama',
    CROSS_BORDER_TRANSFER: 'Sınır Ötesi Transfer',
    ACCOUNT_DRAIN: 'Hesap Boşaltma',
    NEW_BENEFICIARY_TRANSFER: 'Yeni Alıcı Transferi',
    SUSPICIOUS_DESCRIPTION: 'Şüpheli Açıklama',
    HIGH_RISK_RECEIVER: 'Şüpheli Alıcı/Katır',
    MULTI_SENDER_TO_SINGLE_RECEIVER: 'Tek Alıcıya Çoklu Gönderim',
    RECEIVER_BALANCE_ANOMALY: 'Katır Hesap Anormalliği',
    HIGH_VALUE_REFUND_VOID: 'Yüksek Tutarlı İade',
    DEPOSIT_AND_RUN: 'Yatır ve Kaç',
    DEPOSIT_LIMIT_AVOIDANCE: 'Yapılandırılmış Aklama',
    ANOMALOUS_DEPOSIT_TIME: 'Gece Nakit Yatırma',
    RAPID_TXN_VELOCITY_2MIN: '2 Dk Hızlı İşlem Sıklığı',
    HOURLY_SAME_CARD_VELOCITY: '1 Saatte Aynı Kartla Yoğun İşlem',
    INTENSE_FAILED_ATTEMPTS: 'Yoğun Başarısız Denemeler',
    NIGHT_HIGH_VALUE: 'Gece Yüksek Tutar',
    PROBING_THEN_HIGH_VALUE: 'Küçük Denemeler Sonrası Yüksek Tutar',
    DAILY_LIMIT_EXCEEDED: 'Günlük Hacim/Adet Aşımı',
    REFUND_EXCEEDS_SALES: 'İade Satışı Aştı',
    CONSECUTIVE_REFUNDS_2HOURS: '2 Saatte Yoğun İade',
    AVERAGE_MULTIPLIER_SURGE: 'Ortalamanın 4 Katı Tutar',
    MULTI_COUNTRY_ACTIVITY: 'Çoklu Ülke Aktivitesi',
    MERCHANT_MULTI_CARD_VELOCITY: 'İşyerinde Çoklu Kart',
    NEW_MERCHANT_HIGH_TURNOVER: 'Yeni İşyeri Yüksek Ciro',
    // Geriye dönük uyumluluk
    S1: '2 Dk Hızlı İşlem Sıklığı',
    S2: '1 Saatte Aynı Kartla Yoğun İşlem',
    S3: 'Yoğun Başarısız Denemeler',
    S4: 'Gece Yüksek Tutar',
    S5: 'Küçük Denemeler Sonrası Yüksek Tutar',
    S6: 'Günlük Hacim/Adet Aşımı',
    S7: 'İade Satışı Aştı',
    S8: '2 Saatte Yoğun İade',
    S9: 'Ortalamanın 4 Katı Tutar',
    S10: 'Çoklu Ülke Aktivitesi',
    S11: 'İşyerinde Çoklu Kart',
    S12: 'Yeni İşyeri Yüksek Ciro'
};

export class Transaction {
    private readonly props: TransactionProps;

    constructor(props: TransactionProps) {
        this.props = props;
    }

    get id(): string { return this.props.id; }
    get maskedCard(): string { return this.props.maskedCard; }
    get money(): Money { return this.props.money; }
    get transactionId(): string { return this.props.transactionId; }
    get ruleName(): string { return this.props.ruleName; }
    get suspicionReason(): string { return this.props.suspicionReason; }
    get riskScore(): number {
        const text = this.props.suspicionReason || this.props.fraudReason;
        if (text) {
            const match = text.match(/Skor\s+(\d+)/i);
            if (match && match[1]) {
                return Math.min(100, parseInt(match[1], 10));
            }
        }
        return Math.min(100, this.props.riskScore || 0);
    }

    get triggeredRules(): { code: string; name: string; score?: number }[] {
        const text = this.props.suspicionReason || this.props.fraudReason || '';
        const match = text.match(/—\s*([^|]+)/);
        if (match && match[1]) {
            const rawRules = match[1].split(',').map(r => r.trim()).filter(Boolean);
            if (rawRules.length > 0) {
                return rawRules.map(raw => {
                    const scoreMatch = raw.match(/^([A-Za-z0-9_]+)\((\d+)P\)$/);
                    if (scoreMatch) {
                        const code = scoreMatch[1];
                        const score = parseInt(scoreMatch[2], 10);
                        return {
                            code,
                            name: RULE_NAMES[code] || code,
                            score
                        };
                    }
                    return {
                        code: raw,
                        name: RULE_NAMES[raw] || raw
                    };
                });
            }
        }
        return [{ code: this.props.ruleCode || 'DEFAULT', name: this.props.ruleName || 'Şüpheli İşlem' }];
    }

    get location(): string { return this.props.location; }
    get date(): string { return this.props.date; }

    get fraudReason(): string | undefined { return this.props.fraudReason; }
    get ruleCode(): string | undefined { return this.props.ruleCode; }
    get paymentType(): string | undefined { return this.props.paymentType; }
}