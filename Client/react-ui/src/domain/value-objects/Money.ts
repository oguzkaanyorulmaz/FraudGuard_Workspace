export class Money {
    private readonly amount: number;
    private readonly currency: string;

    constructor(amount: number, currency: string) {
        if (amount < 0) {
            throw new Error('İşlem tutarı negatif olamaz.');
        }
        this.amount = amount;
        this.currency = currency;
    }

    getAmount(): number {
        return this.amount;
    }

    getCurrency(): string {
        return this.currency;
    }

    getFormatted(): string {
        return `${this.amount.toLocaleString('tr-TR')} ${this.currency}`;
    }
}