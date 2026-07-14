import React from 'react';
import type { Transaction } from '../../../domain/entities/Transaction';
import { TransactionRow } from './TransactionRow';

interface Props {
    transactions: { transaction: Transaction; historyAction?: 'APPROVED' | 'BLOCKED' }[];
    loading: boolean;
    isHistoryView?: boolean;
    selectedIds: string[];
    onToggleSelection: (id: string) => void;
    onSelectAll: (ids: string[]) => void;
    onApprove: (id: string) => void;
    onBlock: (id: string) => void;
    onViewDetails: (txn: Transaction) => void;
}

export const TransactionList: React.FC<Props> = ({
    transactions, loading, isHistoryView, selectedIds, onToggleSelection, onSelectAll, onApprove, onBlock, onViewDetails
}) => {

    const allIds = transactions.map(t => t.transaction.id);
    const isAllSelected = transactions.length > 0 && selectedIds.length === transactions.length;

    const handleSelectAll = () => {
        if (isAllSelected) onSelectAll([]);
        else onSelectAll(allIds);
    };

    if (loading) return <div className="bg-gray-900 rounded-b-xl border border-gray-800 p-12 text-center shadow-2xl"><div className="text-gray-400 font-bold animate-pulse">Veriler Yükleniyor...</div></div>;
    if (transactions.length === 0) return <div className="bg-gray-900 rounded-b-xl border border-gray-800 p-12 text-center shadow-2xl"><div className="text-emerald-400 font-bold text-lg">🎉 Harika! Kayıt bulunmuyor.</div></div>;

    return (
        <div className="bg-white rounded-xl border border-[#E4E7EB] overflow-hidden shadow-sm">
            <table className="w-full text-left text-sm whitespace-nowrap">
                <thead className="bg-[#F8F9FA] text-[#718096] font-bold border-b border-[#E4E7EB] uppercase text-[11px] tracking-wider">
                    <tr>
                        <th className="p-4 w-12 text-center">
                            {!isHistoryView && <input type="checkbox" checked={isAllSelected} onChange={handleSelectAll} className="w-4 h-4 rounded accent-blue-600 cursor-pointer" />}
                        </th>
                        <th className="p-4 w-28">Risk Skoru</th>
                        <th className="p-4 w-24">İşlem ID</th>
                        <th className="p-4 w-44">Maskeli Kart</th>
                        <th className="p-4 w-32">Tutar</th>
                        <th className="p-4">Şüphe Sebebi ve Kural</th>
                        <th className="p-4 text-center">Aksiyonlar</th>
                    </tr>
                </thead>
                <tbody className="divide-y divide-gray-800/60">
                    {transactions.map(item => (
                        <TransactionRow
                            key={item.transaction.id}
                            transaction={item.transaction}
                            isSelected={selectedIds.includes(item.transaction.id)}
                            isHistoryView={isHistoryView}
                            historyAction={item.historyAction}
                            onToggle={onToggleSelection}
                            onApprove={onApprove}
                            onBlock={onBlock}
                            onViewDetails={onViewDetails}
                        />
                    ))}
                </tbody>
            </table>
        </div>
    );
};