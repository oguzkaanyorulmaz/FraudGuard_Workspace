import type { ITransactionRepository } from '../../domain/repositories/ITransactionRepository';
import { Transaction } from '../../domain/entities/Transaction';
import { Money } from '../../domain/value-objects/Money';

export class ApiTransactionRepo implements ITransactionRepository {
    private readonly fraudManagementUrl = 'http://localhost:5217/api/FraudManagement';

    private getToken(): string {
        try {
            const stored = localStorage.getItem('fraudguard_user');
            if (stored) {
                const parsed = JSON.parse(stored);
                return parsed.token || '';
            }
        } catch (e) {
            console.error("Token okuma hatası", e);
        }
        return '';
    }

    async getPendingTransactions(): Promise<Transaction[]> {
        const response = await fetch(`${this.fraudManagementUrl}/unresolved-logs`, {
            headers: {
                'Authorization': `Bearer ${this.getToken()}`
            }
        });
        if (!response.ok) throw new Error('API bağlantı hatası!');

        const jsonResponse = await response.json();


        const dataList = jsonResponse.data || jsonResponse;

        if (!Array.isArray(dataList)) {
            console.error("C# API'den beklenen liste formatı gelmedi. Gelen veri:", jsonResponse);
            return [];
        }

        return dataList.map((item: any) => new Transaction({
            id: item.logId ? item.logId.toString() : (item.id ? item.id.toString() : "0"),
            transactionId: item.transactionId ? item.transactionId.toString() : "0",
            maskedCard: item.cardNumber || item.maskedCardNumber || "Bilinmiyor",
            money: new Money(item.amount || 0, item.currency || "TRY"),

            riskScore: item.riskScore || 0,

            suspicionReason: item.reason || item.suspicionReason || "Belirtilmemiş",
            ruleName: item.ruleName || "Bilinmeyen Kural",
            location: item.location || "Bilinmiyor",
            date: item.transactionDate || item.logDate || item.date || item.createdAt || new Date().toISOString(),
            fraudReason: item.fraudReason || undefined,
            ruleCode: item.ruleCode || undefined,
            paymentType: item.paymentTypeCode === 'CreditCard' ? 'CREDIT_CARD' :
                         item.paymentTypeCode === 'DebitCard' ? 'DEBIT_CARD' :
                         (item.paymentTypeCode === 'BankTransfer' || item.paymentTypeCode === 'EFT') ? 'BANK_TRANSFER' : undefined
        }));
    }

    async approveTransaction(id: string, reason: string, analystName?: string): Promise<boolean> {
        const numericId = parseInt(id.replace(/\D/g, ''), 10);
        const response = await fetch(`${this.fraudManagementUrl}/resolve-log`, {
            method: 'POST',
            headers: { 
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${this.getToken()}`
            },
            body: JSON.stringify({
                logId: numericId,
                adminAction: "MarkAsSafe",
                adminNote: reason,
                resolvedByAdmin: analystName
            })
        });

        if (!response.ok) {
            const errorDetails = await response.text();
            console.error("C# Backend'den Dönen Hata Mesajı:", errorDetails);
        }

        return response.ok;
    }

    async blockCard(id: string, reason: string, blockReasonId?: number, analystName?: string): Promise<boolean> {
        const numericId = parseInt(id.replace(/\D/g, ''), 10);
        const response = await fetch(`${this.fraudManagementUrl}/resolve-log`, {
            method: 'POST',
            headers: { 
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${this.getToken()}`
            },
            body: JSON.stringify({
                logId: numericId,
                adminAction: "MarkAsFraud",
                adminNote: reason,
                blockReasonId: blockReasonId,
                resolvedByAdmin: analystName
            })
        });

        if (!response.ok) {
            const errorDetails = await response.text();
            console.error("C# Backend'den Dönen Hata Mesajı:", errorDetails);
        }

        return response.ok;
    }

    async processBulkAction(ids: string[], reason: string): Promise<void> {
        for (const id of ids) {
            await this.blockCard(id, reason);
        }
    }

    async getLogDetailById(logId: number): Promise<any> {
        const response = await fetch(`${this.fraudManagementUrl}/log-detail/${logId}`, {
            headers: {
                'Authorization': `Bearer ${this.getToken()}`
            }
        });
        if (!response.ok) throw new Error('Detaylar çekilirken hata oluştu!');

        const jsonResponse = await response.json();

        return jsonResponse.data || jsonResponse;
    }

    async getHistoricalTransactions(): Promise<{ transaction: Transaction, action: 'APPROVED' | 'BLOCKED' }[]> {
        const response = await fetch(`${this.fraudManagementUrl}/resolved-logs`, {
            headers: {
                'Authorization': `Bearer ${this.getToken()}`
            }
        });
        if (!response.ok) throw new Error('Geçmiş veriler çekilirken hata oluştu!');

        const jsonResponse = await response.json();
        const dataList = jsonResponse.data || jsonResponse;

        if (!Array.isArray(dataList)) return [];

        return dataList.map((item: any) => {
            const historyAction = item.adminAction === 'MarkAsSafe' ? 'APPROVED' : 'BLOCKED';

            return {
                transaction: new Transaction({
                    id: item.logId ? item.logId.toString() : (item.id ? item.id.toString() : "0"),
                    transactionId: item.transactionId ? item.transactionId.toString() : "0",
                    maskedCard: item.cardNumber || item.maskedCardNumber || "Bilinmiyor",
                    money: new Money(item.amount || 0, item.currency || "TRY"),
                    riskScore: item.riskScore || 0,
                    suspicionReason: item.reason || item.suspicionReason || "Belirtilmemiş",
                    ruleName: item.ruleName || "Bilinmeyen Kural",
                    location: item.location || "Bilinmiyor",
                    date: item.transactionDate || item.logDate || item.date || item.createdAt || new Date().toISOString(),
                    fraudReason: item.fraudReason || undefined,
                    ruleCode: item.ruleCode || undefined,
                    paymentType: item.paymentTypeCode === 'CreditCard' ? 'CREDIT_CARD' :
                                 item.paymentTypeCode === 'DebitCard' ? 'DEBIT_CARD' :
                                 (item.paymentTypeCode === 'BankTransfer' || item.paymentTypeCode === 'EFT') ? 'BANK_TRANSFER' : undefined
                }),
                action: historyAction
            };
        });
    }
}