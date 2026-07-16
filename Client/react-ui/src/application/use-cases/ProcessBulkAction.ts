import type { ITransactionRepository } from '../../domain/repositories/ITransactionRepository';

export class ProcessBulkAction {
    private readonly repository: ITransactionRepository;

    constructor(repository: ITransactionRepository) {
        this.repository = repository;
    }

    async execute(transactionIds: string[], reason: string, blockReasonId?: number, analystName?: string): Promise<boolean> {
        if (transactionIds.length === 0) {
            throw new Error("Toplu işlem için en az bir kayıt seçilmelidir.");
        }
        if (!reason || reason.trim().length === 0) {
            throw new Error("Toplu bloke işlemi için gerekçe zorunludur.");
        }

        // Gerçek bir API senaryosunda bu işlem backend'e tek bir dizi (array) olarak yollanır.
        // Şimdilik her bir ID için repository'i tetikliyoruz.
        for (const id of transactionIds) {
            await this.repository.blockCard(id, reason, blockReasonId, analystName);
        }

        return true;
    }
}