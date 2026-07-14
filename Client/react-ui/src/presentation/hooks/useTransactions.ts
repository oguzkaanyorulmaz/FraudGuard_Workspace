import { useState, useEffect, useCallback } from 'react';
import type { Transaction } from '../../domain/entities/Transaction';
import { ApiTransactionRepo } from '../../infrastructure/repositories/ApiTransactionRepo';
import { GetPendingTransactions } from '../../application/use-cases/GetPendingTransactions';
import { ApproveTransaction } from '../../application/use-cases/ApproveTransaction';
import { BlockCard } from '../../application/use-cases/BlockCard';
import { ProcessBulkAction } from '../../application/use-cases/ProcessBulkAction';
import * as signalR from '@microsoft/signalr';
export type HistoryLog = {
    transaction: Transaction;
    action: 'APPROVED' | 'BLOCKED';
    reason: string;
    timestamp: Date;
};

const repository = new ApiTransactionRepo();
const getPendingUseCase = new GetPendingTransactions(repository);
const approveUseCase = new ApproveTransaction(repository);
const blockUseCase = new BlockCard(repository);
const processBulkUseCase = new ProcessBulkAction(repository);

export function useTransactions() {
    const [transactions, setTransactions] = useState<Transaction[]>([]);
    const [history, setHistory] = useState<HistoryLog[]>([]);
    const [loading, setLoading] = useState<boolean>(true);

    const fetchTransactions = useCallback(async () => {
        // Arka planda sessiz yenileme yaparken ekranda "Yükleniyor" titremesi olmasın diye
        // sadece liste tamamen boşsa loading'i true yapabiliriz.
        setLoading(prev => transactions.length === 0 ? true : prev);
        
        try {
            const data = await getPendingUseCase.execute();
            setTransactions(data);

            const historyData = await repository.getHistoricalTransactions();
            
            const formattedHistory: HistoryLog[] = historyData.map(h => ({
                transaction: h.transaction,
                action: h.action,
                reason: h.transaction.suspicionReason || "Admin Aksiyonu",
                timestamp: new Date(h.transaction.date)
            }));
            
            setHistory(formattedHistory);
        } catch (error) {
            console.error("Veriler çekilirken hata:", error);
        } finally {
            setLoading(false);
        }
    }, [transactions.length]);

    // --- SİGNALR BAĞLANTISI VE DİNLEME MANTIĞI BURADA ---
    useEffect(() => {
        // 1. Sayfa ilk yüklendiğinde verileri çek
        fetchTransactions();

        // 2. SignalR Bağlantısını Hazırla
        const connection = new signalR.HubConnectionBuilder()
            .withUrl("http://localhost:5217/fraudHub") // C# Backend'indeki Hub URL'si
            .withAutomaticReconnect() // Bağlantı anlık koparsa kendi kendine tekrar dener
            .build();

        // 3. Bağlantıyı Başlat ve Dinlemeye Geç
        connection.start()
            .then(() => {
                console.log("🟢 SignalR Bağlantısı Başarılı!");

                // Backend'den gelen 'RefreshLogs' sinyalini dinle
                connection.on("RefreshLogs", () => {
                    console.log("⚡ Backend'den yeni veri sinyali geldi, liste güncelleniyor...");
                    fetchTransactions();
                });
            })
            .catch(err => console.error("🔴 SignalR Bağlantı Hatası:", err));

        // 4. Temizlik (Cleanup): Bileşen ekrandan kalkarsa bağlantıyı kapat
        return () => {
            connection.stop().then(() => console.log("SignalR Bağlantısı Kapatıldı."));
        };
    }, [fetchTransactions]);
    // ----------------------------------------------------

    const addToHistory = (id: string, action: 'APPROVED' | 'BLOCKED', reason: string) => {
        const txn = transactions.find(t => t.id === id);
        if (txn) {
            setHistory(prev => [{ transaction: txn, action, reason, timestamp: new Date() }, ...prev]);
        }
    };

    const handleApprove = async (id: string, reason: string) => {
        addToHistory(id, 'APPROVED', reason);
        setTransactions(prev => prev.filter(t => t.id !== id));

        await approveUseCase.execute(id, reason);
        await fetchTransactions(); 
    };

    const handleBlock = async (id: string, reason: string, blockReasonId?: number) => {
        addToHistory(id, 'BLOCKED', reason);
        setTransactions(prev => prev.filter(t => t.id !== id));

        await blockUseCase.execute(id, reason, blockReasonId);
        await fetchTransactions();
    };

    const handleBulkBlock = async (ids: string[], reason: string) => {
        ids.forEach(id => addToHistory(id, 'BLOCKED', reason));
        setTransactions(prev => prev.filter(t => !ids.includes(t.id)));

        await processBulkUseCase.execute(ids, reason);
        await fetchTransactions();
    };

    return { transactions, history, loading, handleApprove, handleBlock, handleBulkBlock };
}