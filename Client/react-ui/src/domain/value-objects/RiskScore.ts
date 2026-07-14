export class RiskScore {
    private readonly value: number;

    constructor(score: number) {
        if (score < 0 || score > 100) {
            throw new Error('Risk skoru 0 ile 100 arasında olmak zorundadır.');
        }
        this.value = score;
    }

    getValue(): number {
        return this.value;
    }

    isHighRisk(): boolean {
        return this.value >= 75;
    }

    isMediumRisk(): boolean {
        return this.value >= 40 && this.value < 75;
    }
}