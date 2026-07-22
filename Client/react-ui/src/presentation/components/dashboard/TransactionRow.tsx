import React from 'react';
import type { Transaction } from '../../../domain/entities/Transaction';
import { useAuth } from '../../contexts/AuthContext';

interface Props {
    transaction: Transaction;
    isSelected: boolean;
    isHistoryView?: boolean;
    historyAction?: 'APPROVED' | 'BLOCKED';
    onToggle: (id: string) => void;
    onApprove: (id: string) => void;
    onBlock: (id: string) => void;
    onViewDetails: (txn: Transaction) => void;
    isExiting?: boolean;
}

export const TransactionRow: React.FC<Props> = ({
    transaction, isSelected, isHistoryView, historyAction, onToggle, onApprove, onBlock, onViewDetails,
    isExiting = false
}) => {
    const { user } = useAuth();
    const isAnalyst = user?.role === 3;
    const scoreValue = transaction.riskScore ?? 0;
    const isHighRisk = scoreValue >= 70;

    const animationClass = isExiting ? 'row-exit' : 'row-enter';

    return (
        <tr className={`hover:bg-[#F8F9FA] transition-all duration-150 border-b border-[#E4E7EB] ${isSelected ? 'bg-[#FFC72C]/5' : ''} ${animationClass}`}>
            <td className="p-4 text-center">
                {!isHistoryView && (
                    <div className="custom-checkbox-cont justify-center">
                        <input 
                            type="checkbox" 
                            checked={isSelected} 
                            onChange={() => !isAnalyst && onToggle(transaction.id)} 
                            disabled={isAnalyst}
                            className={`custom-checkbox ${isAnalyst ? 'cursor-not-allowed opacity-50' : ''}`} 
                        />
                    </div>
                )}
            </td>
            <td className="p-4">
                <div className="flex items-center gap-2">
                    <span className={`text-xs font-bold w-6 ${isHighRisk ? 'text-red-500' : 'text-orange-400'}`}>{scoreValue}</span>
                    <div className="w-full bg-gray-200 h-2 rounded-full overflow-hidden">
                        <div className={`h-full rounded-full ${isHighRisk ? 'bg-red-500' : 'bg-orange-500'}`} style={{ width: `${scoreValue}%` }}></div>
                    </div>
                </div>
            </td>
            <td className="p-4 font-mono text-gray-600">#{transaction.transactionId}</td>
            <td className="p-4 font-mono text-black font-semibold">{transaction.maskedCard}</td>
            <td className="p-4 font-black text-black">{transaction.money ? transaction.money.getFormatted() : '₺0.00'}</td>
            <td className="p-4 text-xs text-slate-600 font-medium">
                {new Date(transaction.date).toLocaleString('tr-TR')}
            </td>
            <td className="p-4 whitespace-normal break-words max-w-md">
                <div className="flex flex-col">
                    <span className={`text-[11px] px-2 py-0.5 rounded font-bold w-max border mb-1 ${isHighRisk ? 'bg-red-50 text-red-700 border-red-100' : 'bg-amber-50 text-amber-800 border-amber-100'}`}>
                        {transaction.ruleName || 'SİSTEM UYARISI'}
                    </span>
                    <span className="text-slate-800 text-xs">↳ {transaction.suspicionReason || 'Sistem tarafından riskli bulunarak incelemeye alındı.'}</span>
                </div>
            </td>
            <td className="p-4 flex flex-col gap-2 w-48">
                {isHistoryView ? (
                    <div className={`text-center font-bold text-xs p-2 rounded border ${historyAction === 'APPROVED' ? 'bg-emerald-50 text-emerald-700 border-emerald-200' : 'bg-red-50 text-red-700 border-red-200'}`}>
                        {historyAction === 'APPROVED' ? '✅ ONAYLANDI' : '🚫 BLOKELENDİ'}
                    </div>
                ) : (
                    <div className="flex gap-1.5">
                        <button 
                            onClick={() => !isAnalyst && onApprove(transaction.id)} 
                            disabled={isAnalyst}
                            className={`relative overflow-hidden w-[90px] h-9 rounded-lg transition-all duration-500 flex items-center border-none outline-none select-none ${
                                isAnalyst 
                                    ? 'bg-slate-200 text-slate-400 cursor-not-allowed border border-slate-300' 
                                    : 'bg-[#FDBB30] text-[#111] cursor-pointer group'
                            }`}
                        >
                            {!isAnalyst && (
                                <span className="absolute left-0 top-0 h-full w-8 flex items-center justify-center transition-all duration-500 bg-[#FDBB30] group-hover:w-full group-active:scale-90 text-sm">
                                    ✔️
                                </span>
                            )}
                            <span className={`font-bold text-xs transition-all duration-500 ${
                                isAnalyst ? 'pl-6' : 'pl-9 group-hover:opacity-0 group-hover:translate-x-4'
                            }`}>
                                İzin
                            </span>
                        </button>
                        <button 
                            onClick={() => !isAnalyst && onBlock(transaction.id)} 
                            disabled={isAnalyst}
                            className={`relative overflow-hidden w-[90px] h-9 rounded-lg transition-all duration-500 flex items-center border-none outline-none select-none ${
                                isAnalyst 
                                    ? 'bg-slate-200 text-slate-400 cursor-not-allowed border border-slate-300' 
                                    : 'bg-red-600 text-white cursor-pointer group'
                            }`}
                        >
                            {!isAnalyst && (
                                <span className="absolute left-0 top-0 h-full w-8 flex items-center justify-center transition-all duration-500 bg-red-600 group-hover:w-full group-active:scale-90 text-sm">
                                    🚫
                                </span>
                            )}
                            <span className={`font-bold text-xs transition-all duration-500 ${
                                isAnalyst ? 'pl-5' : 'pl-9 group-hover:opacity-0 group-hover:translate-x-4'
                            }`}>
                                Bloke
                            </span>
                        </button>
                    </div>
                )}
                <button 
                    onClick={() => onViewDetails(transaction)} 
                    className="relative overflow-hidden w-full h-9 rounded-lg transition-all duration-500 flex items-center border border-[#C5CBD3] bg-white text-slate-700 outline-none select-none cursor-pointer group"
                >
                    <span className="absolute left-0 top-0 h-full w-8 flex items-center justify-center transition-all duration-500 bg-white group-hover:w-full group-active:scale-90 text-sm">
                        🔍
                    </span>
                    <span className="pl-9 font-semibold text-xs transition-all duration-500 group-hover:opacity-0 group-hover:translate-x-4">
                        İşlem Detayları
                    </span>
                </button>
            </td>
        </tr>
    );
};