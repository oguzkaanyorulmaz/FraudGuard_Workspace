export const FraudRuleType = {
    VELOCITY: 'VELOCITY',
    IMPOSSIBLE_TRAVEL: 'IMPOSSIBLE_TRAVEL',
    ANOMALOUS_TIME: 'ANOMALOUS_TIME',
    CARD_TESTING: 'CARD_TESTING',
    BRUTE_FORCE: 'BRUTE_FORCE',
    CROSS_BORDER: 'CROSS_BORDER',
    HIGH_RISK_MCC: 'HIGH_RISK_MCC',
    MAX_OUT: 'MAX_OUT',
    CURRENCY_MISMATCH: 'CURRENCY_MISMATCH'
} as const;

export type FraudRuleType = typeof FraudRuleType[keyof typeof FraudRuleType];

export const TransactionStatus = {
    PENDING: 'PENDING',
    ALLOWED: 'ALLOWED',
    BLOCKED: 'BLOCKED'
} as const;

export type TransactionStatus = typeof TransactionStatus[keyof typeof TransactionStatus];