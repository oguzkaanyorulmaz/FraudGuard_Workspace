import React from 'react';
import type { Transaction } from '../../../domain/entities/Transaction';
import { RiskScore } from '../../../domain/value-objects/RiskScore';
import { useAuth } from '../../contexts/AuthContext';
import { theme } from '../../styles/theme';

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

export const MobileTransactionCard: React.FC<Props> = React.memo(({
    transaction, isSelected, isHistoryView, historyAction, onToggle, onApprove, onBlock, onViewDetails
}) => {
    const { user } = useAuth();
    const isAnalyst = user?.role === 3;
    const risk = new RiskScore(transaction.riskScore ?? 0);
    const scoreValue = risk.getValue();
    const riskStyle = theme.riskTier[risk.getTier()];
    const animationClass = 'row-enter';

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
                        <span className={`text-xs font-bold ${riskStyle.text}`} title={risk.getLabel()}>
                            {scoreValue}
                        </span>
                        <div className="w-16 bg-gray-200 h-1.5 rounded-full overflow-hidden">
                            <div
                                className={`h-full rounded-full ${riskStyle.bar}`}
                                style={{ width: `${risk.getBarPercent()}%` }}
                            />
                        </div>
                    </div>
                </div>
                <span className="text-[10px] font-mono text-gray-500">#{transaction.transactionId}</span>
            </div>

            {/* Kart Gövdesi: Bilgi Alanları */}
            <div className="mt-3 space-y-3">
                {/* Satır 1: Kart/IBAN (sol) ve Tutar (sağ) */}
                <div className="flex justify-between items-baseline gap-2">
                    <div className="flex flex-col">
                        <span className="text-[9px] font-bold text-[#718096] uppercase tracking-wider mb-0.5">KART / IBAN</span>
                        <span className="font-mono text-xs md:text-sm font-bold text-[#111]">{formatIdentifier(transaction.maskedCard)}</span>
                    </div>
                    <div className="flex flex-col items-end">
                        <span className="text-[9px] font-bold text-[#718096] uppercase tracking-wider mb-0.5">TUTAR</span>
                        <span className="text-xs md:text-sm font-black text-black">{transaction.money ? transaction.money.getFormatted() : '₺0.00'}</span>
                    </div>
                </div>

                {/* Satır 2: Tarih (sol) ve Kural (sağ) */}
                <div className="flex justify-between items-center gap-2 pt-2 border-t border-gray-100">
                    <div className="flex flex-col">
                        <span className="text-[9px] font-bold text-[#718096] uppercase tracking-wider mb-0.5">TARİH</span>
                        <span className="text-[11px] text-slate-700 font-semibold">{new Date(transaction.date).toLocaleString('tr-TR')}</span>
                    </div>
                    <div className="flex flex-col items-end">
                        <span className="text-[9px] font-bold text-[#718096] uppercase tracking-wider mb-0.5">KURALLAR</span>
                        <div className="flex flex-wrap justify-end gap-1 max-w-[180px]">
                            {transaction.triggeredRules.map((r, idx) => (
                                <span key={idx} className={`text-[10px] px-1.5 py-0.5 rounded font-bold border ${riskStyle.badge}`} title={r.code}>
                                    {r.name} {r.score !== undefined && <span className="opacity-80 text-[9px]">({r.score}P)</span>}
                                </span>
                            ))}
                        </div>
                    </div>
                </div>

                {/* Satır 3: Şüphe Sebebi (tam genişlik) */}
                <div className="bg-slate-50 border-l-2 border-amber-400 p-2.5 rounded-r-lg text-xs text-slate-700 leading-relaxed">
                    <span className="font-bold text-[#8F6A0F] mr-1">Sebep:</span> {transaction.suspicionReason || 'Sistem tarafından riskli bulunarak incelemeye alındı.'}
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
                    className={`${isHistoryView ? 'w-12 flex-shrink-0' : 'flex-1'} h-9 rounded-lg text-xs font-semibold border border-[#C5CBD3] bg-white text-slate-700 flex items-center justify-center cursor-pointer hover:bg-slate-50 transition-all`}
                >
                    {isHistoryView ? '🔍' : '🔍 Detay'}
                </button>
            </div>
        </div>
    );
});
