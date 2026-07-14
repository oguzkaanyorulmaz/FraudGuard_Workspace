import React from 'react';
import type { Transaction } from '../../../domain/entities/Transaction';

interface Props {
    transaction: Transaction;
    isSelected: boolean;
    isHistoryView?: boolean;
    historyAction?: 'APPROVED' | 'BLOCKED';
    onToggle: (id: string) => void;
    onApprove: (id: string) => void;
    onBlock: (id: string) => void;
    onViewDetails: (txn: Transaction) => void;
}

export const TransactionRow: React.FC<Props> = ({
    transaction, isSelected, isHistoryView, historyAction, onToggle, onApprove, onBlock, onViewDetails
}) => {
    // KRİTİK DEĞİŞİKLİK: riskScore artık bir fonksiyon değil, doğrudan bir sayı (number).
    // Eğer null gelirse varsayılan olarak 0 atıyoruz.
    const scoreValue = transaction.riskScore ?? 0;
    const isHighRisk = scoreValue >= 70;

    return (
        <tr className={`hover:bg-gray-850/40 transition group border-b border-gray-800/60 ${isSelected ? 'bg-blue-900/10' : ''}`}>
            <td className="p-4 text-center">
                {!isHistoryView && (
                    <input type="checkbox" checked={isSelected} onChange={() => onToggle(transaction.id)} className="w-4 h-4 rounded accent-blue-600 cursor-pointer" />
                )}
            </td>
            <td className="p-4">
                <div className="flex items-center gap-2">
                    <span className={`text-xs font-bold w-6 ${isHighRisk ? 'text-red-500' : 'text-orange-400'}`}>{scoreValue}</span>
                    <div className="w-full bg-gray-800 h-2 rounded-full overflow-hidden">
                        <div className={`h-full rounded-full ${isHighRisk ? 'bg-red-500' : 'bg-orange-500'}`} style={{ width: `${scoreValue}%` }}></div>
                    </div>
                </div>
            </td>
            <td className="p-4 font-mono text-gray-500">#{transaction.id}</td>
            <td className="p-4 font-mono">{transaction.maskedCard}</td>
            {/* Not: Eğer money nesnesi de frontend'de değiştiyse burası transaction.amount şeklinde güncellenebilir */}
            <td className="p-4 font-black text-white">{transaction.money ? transaction.money.getFormatted() : '₺0.00'}</td>
            <td className="p-4">
                <div className="flex flex-col">
                    <span className={`text-[11px] px-2 py-0.5 rounded font-bold w-max border mb-1 ${isHighRisk ? 'bg-red-500/10 text-red-400 border-red-500/20' : 'bg-orange-500/10 text-orange-400 border-orange-500/20'}`}>
                        {transaction.ruleName || 'SİSTEM UYARISI'}
                    </span>
                    <span className="text-gray-300 text-xs">↳ {transaction.suspicionReason || 'Sistem tarafından riskli bulunarak incelemeye alındı.'}</span>
                </div>
            </td>
            <td className="p-4 flex flex-col gap-2 w-48">
                {isHistoryView ? (
                    <div className={`text-center font-bold text-xs p-2 rounded border ${historyAction === 'APPROVED' ? 'bg-emerald-900/30 text-emerald-400 border-emerald-500/30' : 'bg-red-900/30 text-red-400 border-red-500/30'}`}>
                        {historyAction === 'APPROVED' ? '✅ ONAYLANDI' : '🚫 BLOKELENDİ'}
                    </div>
                ) : (
                    <div className="flex gap-1">
                        <button onClick={() => onApprove(transaction.id)} className="flex-1 bg-emerald-600/20 hover:bg-emerald-600 text-emerald-400 hover:text-white px-2 py-1.5 rounded text-xs font-bold border border-emerald-500/30 transition">✔️ İzin</button>
                        <button onClick={() => onBlock(transaction.id)} className="flex-1 bg-red-600/20 hover:bg-red-600 text-red-400 hover:text-white px-2 py-1.5 rounded text-xs font-bold border border-red-500/30 transition">🚫 Bloke</button>
                    </div>
                )}
                <button onClick={() => onViewDetails(transaction)} className="w-full bg-gray-800 hover:bg-blue-600/30 hover:text-blue-400 text-gray-300 px-2 py-1.5 rounded text-xs font-bold border border-gray-700 transition">
                    🔍 İşlem Detayları
                </button>
            </td>
        </tr>
    );
};