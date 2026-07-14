import { Transaction } from '../entities/Transaction';

export interface ITransactionRepository {
    getPendingTransactions(): Promise<Transaction[]>;
    approveTransaction(id: string, reason: string, analystName?: string): Promise<boolean>;
    blockCard(id: string, reason: string, blockReasonId?: number, analystName?: string): Promise<boolean>;
}
