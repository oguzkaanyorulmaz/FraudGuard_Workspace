import React from 'react';
import type { Transaction } from '../../../domain/entities/Transaction';
import { TransactionRow } from './TransactionRow';

interface Props {
    transactions: { transaction: Transaction; historyAction?: 'APPROVED' | 'BLOCKED' }[];
    loading: boolean;
    selectedIds: string[];
    onToggleSelection: (id: string) => void;
    onSelectAll: (ids: string[]) => void;
    onApprove: (id: string) => void;
    onBlock: (id: string) => void;
    onViewDetails: (txn: Transaction) => void;
    sortFields: { field: string; direction: 'asc' | 'desc' }[];
    onSort: (field: string) => void;
    exitingIds: string[];
    emptyMessage: string;
}

export const TransactionList: React.FC<Props> = ({
    transactions, loading, selectedIds, onToggleSelection, onSelectAll, onApprove, onBlock, onViewDetails,
    sortFields, onSort, exitingIds, emptyMessage
}) => {
    const pendingIds = transactions.filter(t => !t.historyAction).map(t => t.transaction.id);
    const isAllSelected = pendingIds.length > 0 && pendingIds.every(id => selectedIds.includes(id));

    const handleSelectAll = () => {
        if (isAllSelected) onSelectAll([]);
        else onSelectAll(pendingIds);
    };

    const renderSortHeader = (label: string, field: string, widthClass?: string) => {
        const activeSort = sortFields.find(s => s.field === field);
        return (
            <th
                className={`p-4 cursor-pointer hover:bg-slate-200 select-none transition-all ${widthClass || ''}`}
                onClick={() => onSort(field)}
            >
                <div className="flex items-center gap-1.5">
                    <span>{label}</span>
                    <span className="text-[10px] text-gray-400 font-bold">
                        {activeSort ? (activeSort.direction === 'asc' ? '▲' : '▼') : '↕'}
                    </span>
                </div>
            </th>
        );
    };

    if (loading) return <div className="bg-white border border-[#E4E7EB] rounded-xl p-16 text-center shadow-sm"><div className="text-slate-400 font-bold animate-pulse">Veriler Yükleniyor...</div></div>;
    if (transactions.length === 0) return <div className="bg-white border border-[#E4E7EB] rounded-xl p-16 text-center shadow-sm"><div className="text-slate-500 font-bold text-base"> {emptyMessage}</div></div>;

    return (
        <div className="bg-white rounded-xl border border-[#E4E7EB] overflow-hidden shadow-sm">
            <table className="w-full text-left text-sm whitespace-nowrap">
                <thead className="bg-[#F8F9FA] text-[#718096] font-bold border-b border-[#E4E7EB] uppercase text-[11px] tracking-wider">
                    <tr>
                        <th className="p-4 w-12 text-center">
                            {pendingIds.length > 0 && (
                                <div className="custom-checkbox-cont justify-center">
                                    <input
                                        type="checkbox"
                                        checked={isAllSelected}
                                        onChange={handleSelectAll}
                                        className="custom-checkbox"
                                    />
                                </div>
                            )}
                        </th>
                        {renderSortHeader("Risk Skoru", "riskScore", "w-28")}
                        {renderSortHeader("İşlem ID", "transactionId", "w-24")}
                        <th className="p-4 w-44">Maskeli Kart</th>
                        <th className="p-4 w-48 select-none">
                            <div className="flex items-center gap-1.5">
                                <span
                                    className="cursor-pointer hover:underline flex items-center gap-0.5"
                                    onClick={() => onSort('amount')}
                                >
                                    Tutar
                                    <span className="text-[10px] text-gray-400 font-bold">
                                        {(() => {
                                            const s = sortFields.find(x => x.field === 'amount');
                                            return s ? (s.direction === 'asc' ? '▲' : '▼') : '↕';
                                        })()}
                                    </span>
                                </span>
                                <span className="text-gray-300">/</span>
                                <span
                                    className="cursor-pointer hover:underline flex items-center gap-0.5"
                                    onClick={() => onSort('currency')}
                                >
                                    Para Birimi
                                    <span className="text-[10px] text-gray-400 font-bold">
                                        {(() => {
                                            const s = sortFields.find(x => x.field === 'currency');
                                            return s ? (s.direction === 'asc' ? '▲' : '▼') : '↕';
                                        })()}
                                    </span>
                                </span>
                            </div>
                        </th>
                        {renderSortHeader("Tarih", "date", "w-40")}
                        <th className="p-4 min-w-[250px] max-w-md">Şüphe Sebebi ve Kural</th>
                        {renderSortHeader("Aksiyonlar", "action", "text-center w-48")}
                    </tr>
                </thead>
                <tbody className="divide-y divide-gray-800/60">
                    {transactions.map(item => (
                        <TransactionRow
                            key={item.transaction.id}
                            transaction={item.transaction}
                            isSelected={selectedIds.includes(item.transaction.id)}
                            isHistoryView={!!item.historyAction}
                            historyAction={item.historyAction}
                            onToggle={onToggleSelection}
                            onApprove={onApprove}
                            onBlock={onBlock}
                            onViewDetails={onViewDetails}
                            isExiting={exitingIds.includes(item.transaction.id)}
                        />
                    ))}
                </tbody>
            </table>
        </div>
    );
};