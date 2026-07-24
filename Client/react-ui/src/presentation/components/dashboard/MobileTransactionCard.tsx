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

const formatIdentifier = (val: string) => {
    if (!val) return '';
    const clean = val.replace(/[\s-]/g, '');
    if (clean.toUpperCase().startsWith('TR') || clean.length === 26) {
        const upper = clean.toUpperCase();
        if (upper.length === 26) {
            return `${upper.slice(0, 4)} •••• ${upper.slice(22, 26)}`;
        }
        return upper;
    }
    if (clean.length === 16) {
        return `${clean.slice(0, 4)} •••• •••• ${clean.slice(12, 16)}`;
    }
    return val;
};

export const MobileTransactionCard: React.FC<Props> = ({
    transaction, isSelected, isHistoryView, historyAction, onToggle, onApprove, onBlock, onViewDetails,
    isExiting = false
}) => {
    const { user } = useAuth();
    const isAnalyst = user?.role === 3;
    const scoreValue = transaction.riskScore ?? 0;
    const isHighRisk = scoreValue >= 70;
    const animationClass = isExiting ? 'row-exit' : 'row-enter';

    return (
        <div className={`mobile-txn-card ${animationClass} ${isSelected ? 'border-[#FFC72C] bg-[#FFC72C]/5' : ''}`}>
            {/* Kart Başlığı: Checkbox + Risk Skoru + Durum */}
            <div className="mobile-txn-card-header">
                <div className="mobile-txn-card-header-left">
                    {!isHistoryView && (
                        <div className="custom-checkbox-cont">
                            <input
                                type="checkbox"
                                checked={isSelected}
                                onChange={() => !isAnalyst && onToggle(transaction.id)}
                                disabled={isAnalyst}
                                className={`custom-checkbox ${isAnalyst ? 'cursor-not-allowed opacity-50' : ''}`}
                            />
                        </div>
                    )}
                    <div className="flex items-center gap-1.5">
                        <span className={`text-xs font-bold ${isHighRisk ? 'text-red-500' : 'text-orange-400'}`}>
                            {scoreValue}
                        </span>
                        <div className="w-16 bg-gray-200 h-1.5 rounded-full overflow-hidden">
                            <div
                                className={`h-full rounded-full ${isHighRisk ? 'bg-red-500' : 'bg-orange-500'}`}
                                style={{ width: `${scoreValue}%` }}
                            />
                        </div>
                    </div>
                </div>
                <span className="text-[10px] font-mono text-gray-500">#{transaction.transactionId}</span>
            </div>

            {/* Kart Gövdesi: Bilgi Alanları */}
            <div className="mobile-txn-card-body">
                <div className="mobile-txn-card-field">
                    <span className="mobile-txn-card-label">Kart / IBAN</span>
                    <span className="mobile-txn-card-value font-mono text-xs">{formatIdentifier(transaction.maskedCard)}</span>
                </div>
                <div className="mobile-txn-card-field">
                    <span className="mobile-txn-card-label">Tutar</span>
                    <span className="mobile-txn-card-value font-black">{transaction.money ? transaction.money.getFormatted() : '₺0.00'}</span>
                </div>
                <div className="mobile-txn-card-field">
                    <span className="mobile-txn-card-label">Tarih</span>
                    <span className="mobile-txn-card-value text-xs text-slate-600">{new Date(transaction.date).toLocaleString('tr-TR')}</span>
                </div>
                <div className="mobile-txn-card-field">
                    <span className="mobile-txn-card-label">Kural</span>
                    <span className={`text-[10px] px-1.5 py-0.5 rounded font-bold w-max border ${isHighRisk ? 'bg-red-50 text-red-700 border-red-100' : 'bg-amber-50 text-amber-800 border-amber-100'}`}>
                        {transaction.ruleName || 'SİSTEM'}
                    </span>
                </div>
                <div className="mobile-txn-card-field full-width">
                    <span className="mobile-txn-card-label">Şüphe Sebebi</span>
                    <span className="mobile-txn-card-value text-xs text-slate-700">
                        ↳ {transaction.suspicionReason || 'Sistem tarafından riskli bulunarak incelemeye alındı.'}
                    </span>
                </div>
            </div>

            {/* Kart Aksiyonları */}
            <div className="mobile-txn-card-actions">
                {isHistoryView ? (
                    <div className={`w-full text-center font-bold text-xs p-2 rounded-lg border ${historyAction === 'APPROVED' ? 'bg-emerald-50 text-emerald-700 border-emerald-200' : 'bg-red-50 text-red-700 border-red-200'}`}>
                        {historyAction === 'APPROVED' ? '✅ ONAYLANDI' : '🚫 BLOKELENDİ'}
                    </div>
                ) : (
                    <>
                        <button
                            onClick={() => !isAnalyst && onApprove(transaction.id)}
                            disabled={isAnalyst}
                            className={`flex-1 h-9 rounded-lg text-xs font-bold transition-all ${
                                isAnalyst
                                    ? 'bg-slate-200 text-slate-400 cursor-not-allowed'
                                    : 'bg-[#FDBB30] text-[#111] cursor-pointer hover:bg-[#E5A520]'
                            }`}
                        >
                            ✔️ İzin
                        </button>
                        <button
                            onClick={() => !isAnalyst && onBlock(transaction.id)}
                            disabled={isAnalyst}
                            className={`flex-1 h-9 rounded-lg text-xs font-bold transition-all ${
                                isAnalyst
                                    ? 'bg-slate-200 text-slate-400 cursor-not-allowed'
                                    : 'bg-red-600 text-white cursor-pointer hover:bg-red-700'
                            }`}
                        >
                            🚫 Bloke
                        </button>
                    </>
                )}
                <button
                    onClick={() => onViewDetails(transaction)}
                    className="flex-1 h-9 rounded-lg text-xs font-semibold border border-[#C5CBD3] bg-white text-slate-700 cursor-pointer hover:bg-slate-50 transition-all"
                >
                    🔍 Detay
                </button>
            </div>
        </div>
    );
};
