import type { ITransactionRepository } from '../../domain/repositories/ITransactionRepository';

export class ApproveTransaction {
    private readonly repository: ITransactionRepository;

    constructor(repository: ITransactionRepository) {
        this.repository = repository;
    }

    async execute(transactionId: string, reason: string, analystName?: string): Promise<boolean> {
        if (!reason || reason.trim().length === 0) {
            throw new Error("Onay işlemi için gerekçe belirtilmesi zorunludur.");
        }
        return await this.repository.approveTransaction(transactionId, reason, analystName);
    }
}