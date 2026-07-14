import type { ITransactionRepository } from '../../domain/repositories/ITransactionRepository';

export class BlockCard {
    private readonly repository: ITransactionRepository;

    constructor(repository: ITransactionRepository) {
        this.repository = repository;
    }

    // DİKKAT: blockReasonId parametresi eklendi
    async execute(transactionId: string, reason: string, blockReasonId?: number): Promise<boolean> {
        if (!reason || reason.trim().length === 0) {
            throw new Error("Bloke işlemi için resmi bir gerekçe yazılması zorunludur.");
        }
        
        // Parametreyi Repo'ya iletiyoruz
        return await this.repository.blockCard(transactionId, reason, blockReasonId);
    }
}