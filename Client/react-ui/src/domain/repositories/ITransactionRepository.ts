import { Transaction } from '../entities/Transaction';

export interface ITransactionRepository {
    getPendingTransactions(): Promise<Transaction[]>;
    approveTransaction(id: string, reason: string): Promise<boolean>;
    blockCard(id: string, reason: string, blockReasonId?: number): Promise<boolean>;
}