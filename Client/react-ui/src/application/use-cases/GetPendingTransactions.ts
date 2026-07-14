import type { ITransactionRepository } from '../../domain/repositories/ITransactionRepository';
import { Transaction } from '../../domain/entities/Transaction';

export class GetPendingTransactions {
    private readonly repository: ITransactionRepository;

    constructor(repository: ITransactionRepository) {
        this.repository = repository;
    }

    async execute(): Promise<Transaction[]> {
        // Burada ileride loglama veya analistin bu işlemi görme yetkisi var mı diye kontroller yapılabilir.
        return await this.repository.getPendingTransactions();
    }
}