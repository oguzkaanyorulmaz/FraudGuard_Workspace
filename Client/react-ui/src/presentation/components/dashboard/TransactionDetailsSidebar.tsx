import React, { useState, useEffect } from 'react';
import type { Transaction } from '../../../domain/entities/Transaction';
import { useAuth } from '../../contexts/AuthContext';

interface Props {
    transaction: Transaction | null;
    isOpen: boolean;
    onClose: () => void;
}

export const TransactionDetailsSidebar: React.FC<Props> = ({ transaction, isOpen, onClose }) => {
    console.log("TransactionDetailsSidebar rendered, isOpen:", isOpen, "txnId:", transaction?.id);

    const [detail, setDetail] = useState<any>(null);
    const [loading, setLoading] = useState<boolean>(false);
    const [historyTab, setHistoryTab] = useState<'all' | 'suspicious' | 'sent' | 'received'>('all');
    const { user } = useAuth();

    const [expandedTxId, setExpandedTxId] = useState<number | null>(null);

    const [shouldRender, setShouldRender] = useState(isOpen);
    const [animate, setAnimate] = useState(false);
    const [localTxn, setLocalTxn] = useState<Transaction | null>(null);

    useEffect(() => {
        if (isOpen) {
            setShouldRender(true);
            setExpandedTxId(null);
            if (transaction) setLocalTxn(transaction);
            const timer = setTimeout(() => {
                setAnimate(true);
            }, 50);
            return () => clearTimeout(timer);
        } else {
            setAnimate(false);
            const timer = setTimeout(() => {
                setShouldRender(false);
            }, 300);
            return () => clearTimeout(timer);
        }
    }, [isOpen, transaction]);

    useEffect(() => {
        if (isOpen && localTxn) {
            setLoading(true);
            setHistoryTab('all');
            const token = user?.token || '';

            fetch(`http://localhost:5217/api/FraudManagement/log-detail/${localTxn.id}`, {
                headers: {
                    'Authorization': `Bearer ${token}`
                }
            })
                .then(res => res.json())
                .then(json => {
                    if (json.isSuccess) {
                        setDetail(json.data);
                    }
                })
                .catch(err => console.error("Detaylar çekilemedi:", err))
                .finally(() => setLoading(false));
        } else if (!isOpen) {
            setDetail(null);
        }
    }, [isOpen, localTxn]);

    if (!shouldRender || !localTxn) return null;

    const scoreValue = localTxn.riskScore ?? 0;
    const isHighRisk = scoreValue >= 70;

    const formatCardNumber = (num: string) => {
        if (!num) return '';
        const clean = num.replace(/[\s-]/g, '');
        if (clean.length === 16) {
            return `${clean.slice(0, 4)} ${clean.slice(4, 8)} ${clean.slice(8, 12)} ${clean.slice(12, 16)}`;
        }
        return num;
    };

    const formatIBAN = (iban: string) => {
        if (!iban) return '';
        const clean = iban.replace(/\s+/g, '').toUpperCase();
        if (clean.length === 26) {
            return `${clean.slice(0, 4)} ${clean.slice(4, 8)} ${clean.slice(8, 12)} ${clean.slice(12, 16)} ${clean.slice(16, 20)} ${clean.slice(20, 24)} ${clean.slice(24, 26)}`;
        }
        return iban;
    };

    const formatPhoneNumber = (phone: string) => {
        if (!phone) return 'Kayıt Yok';
        let clean = phone.replace(/\D/g, '');
        if (clean.length === 11 && clean.startsWith('0')) {
            clean = clean.slice(1);
        } else if (clean.length === 12 && clean.startsWith('90')) {
            clean = clean.slice(2);
        } else if (clean.length === 13 && clean.startsWith('0090')) {
            clean = clean.slice(4);
        }

        if (clean.length === 10) {
            return `+90 ${clean.slice(0, 3)} ${clean.slice(3, 6)} ${clean.slice(6, 8)} ${clean.slice(8, 10)}`;
        }
        return phone;
    };

    const isSafeAction = detail && (
        detail.adminAction === 'MarkAsSafe' ||
        detail.adminAction === 'APPROVE' ||
        detail.adminAction === 'Approve' ||
        detail.adminAction === 'APPROVED'
    );

    return (
        <>
            {/* Arka plan karartması */}
            <div
                className={`fixed inset-0 bg-black/40 z-40 transition-opacity duration-300 ${animate ? 'opacity-100' : 'opacity-0 pointer-events-none'
                    }`}
                onClick={onClose}
            ></div>

            {/* Yan Panel */}
            <div
                className={`fixed inset-y-0 right-0 w-full md:w-[800px] bg-white border-l border-[#E4E7EB] shadow-2xl z-50 transition-transform duration-300 ease-out flex flex-col ${animate ? 'translate-x-0' : 'translate-x-full'
                    }`}
            >
                <div className="flex justify-between items-center p-3 md:p-5 border-b border-[#E4E7EB]">
                    <h2 className="text-base md:text-lg font-bold text-[#111] flex items-center gap-2">
                        🔍 <span className="hidden md:inline">Detaylı Analiz Paneli</span><span className="md:hidden">Analiz</span>
                    </h2>
                    <button onClick={onClose} className="text-[#718096] hover:text-[#111] font-bold text-xl cursor-pointer">&times;</button>
                </div>

                <div className="p-3 md:p-6 flex-1 overflow-y-auto">

                    {/* Şüphe Sebebi ve Tetiklenen Kural (Daha Belirgin Tasarım) */}
                    <div className="bg-amber-50 border-2 border-amber-300 p-5 rounded-xl text-xs text-amber-950 mb-6 flex flex-col gap-2.5 shadow-sm">
                        <div className="font-bold flex items-center gap-1.5 text-amber-800 uppercase tracking-wider text-xs border-b border-amber-200 pb-1.5">
                            ⚠️ Şüphe Sebebi ve Tetiklenen Kural
                        </div>
                        <div className="font-bold text-slate-800 text-sm">
                            Kural Adı: <span className="font-semibold text-slate-900">{localTxn.ruleName}</span>
                        </div>
                        {localTxn.ruleCode && (
                            <div className="font-bold text-slate-800 text-xs">
                                Kural Kodu: <span className="font-mono text-slate-700 bg-amber-100 px-1.5 py-0.5 rounded">{localTxn.ruleCode}</span>
                            </div>
                        )}
                        <div className="font-bold text-slate-800 text-sm">
                            Açıklama: <span className="font-normal text-slate-700">{localTxn.suspicionReason}</span>
                        </div>
                    </div>

                    {/* Çözümlenmiş işlem ise Analiz Kararı ve Analist bilgisi (Daha Belirgin Tasarım) */}
                    {detail && detail.adminNote && (
                        <div className={`border-2 p-5 rounded-xl text-xs mb-6 flex flex-col gap-2.5 shadow-sm ${isSafeAction ? 'bg-emerald-50 border-emerald-300 text-emerald-950' : 'bg-red-50 border-red-300 text-red-950'}`}>
                            <div className={`font-bold flex items-center gap-1.5 uppercase tracking-wider text-xs border-b pb-1.5 ${isSafeAction ? 'text-emerald-800 border-emerald-200' : 'text-red-800 border-red-200'}`}>
                                📋 Analiz Kararı
                            </div>
                            <div className="font-bold text-slate-800 text-sm">
                                Alınan Aksiyon: <span className={`font-bold px-2 py-0.5 rounded-full ${isSafeAction ? 'text-emerald-700 bg-emerald-100/50' : 'text-red-700 bg-red-100/50'}`}>{isSafeAction ? 'Güvenli İşlem' : 'Blokeli İşlem'}</span>
                            </div>
                            <div className="font-bold text-slate-800 text-sm">
                                Gerekçe: <span className="font-normal text-slate-700">{detail.adminNote}</span>
                            </div>
                            {detail.resolvedByAdmin && (
                                <div className="font-bold text-slate-800 text-sm">
                                    Aksiyonu Alan Analist: <span className="font-normal text-slate-700">{detail.resolvedByAdmin}</span>
                                </div>
                            )}
                        </div>
                    )}

                    {/* İşlem Tutarı */}
                    <div className="bg-[#F8F9FA] p-5 rounded-xl border border-[#E4E7EB] mb-6 text-center shadow-sm">
                        <div className="text-xs text-[#718096] font-bold uppercase tracking-wider mb-1">İşlem Tutarı</div>
                        <div className="text-3xl font-black text-[#111]">
                            {detail ? `${detail.amount} ${detail.currency}` : localTxn.money.getFormatted()}
                        </div>
                        {detail?.transactionTypeName && (
                            <div className="text-sm font-bold text-slate-500 mt-1 uppercase tracking-wider">
                                ⚙️ {detail.transactionTypeName}
                            </div>
                        )}
                        <div className={`mt-3 inline-block px-3.5 py-1.5 rounded-full text-sm font-bold border ${isHighRisk ? 'bg-red-500/10 text-red-600 border-red-500/20' : 'bg-amber-500/10 text-amber-600 border-amber-500/20'}`}>
                            Risk Skoru: {scoreValue} / 100
                        </div>
                    </div>

                    {loading ? (
                        <div className="flex justify-center items-center py-10">
                            <span className="text-gray-400 animate-pulse">Gerçek veriler SQL'den çekiliyor...</span>
                        </div>
                    ) : detail ? (
                        (() => {
                            const isTransfer = detail.paymentTypeCode === 'BankTransfer' || detail.paymentTypeCode === 'EFT';
                            return (
                                <div className="space-y-4">
                                    {/* Kart/Hesap & İşlem ID Bilgileri */}
                                    {isTransfer ? (
                                        <div className="grid grid-cols-2 gap-3">
                                            <div className="bg-white p-3.5 rounded-lg border border-[#E4E7EB] col-span-2">
                                                <span className="text-xs text-[#718096] font-bold uppercase tracking-wider block mb-1">Gönderen IBAN</span>
                                                <span className="font-mono text-[#111] font-semibold text-sm">{formatIBAN(detail.senderIBAN)}</span>
                                            </div>
                                            <div className="bg-white p-3.5 rounded-lg border border-[#E4E7EB] col-span-2">
                                                <span className="text-xs text-[#718096] font-bold uppercase tracking-wider block mb-1">Alıcı IBAN</span>
                                                <span className="font-mono text-[#111] font-semibold text-sm">{formatIBAN(detail.receiverIBAN)}</span>
                                            </div>
                                            <div className="bg-white p-3.5 rounded-lg border border-[#E4E7EB]">
                                                <span className="text-xs text-[#718096] font-bold uppercase tracking-wider block mb-1">Alıcı Adı Soyadı</span>
                                                <span className="font-bold text-slate-800 text-sm">{detail.receiverName || 'Bilinmiyor'}</span>
                                            </div>
                                            <div className="bg-white p-3.5 rounded-lg border border-[#E4E7EB]">
                                                <span className="text-xs text-[#718096] font-bold uppercase tracking-wider block mb-1">İşlem ID</span>
                                                <span className="font-mono text-[#111] font-semibold text-sm">#{detail.transactionId}</span>
                                            </div>
                                            {detail.description && (
                                                <div className="bg-white p-3.5 rounded-lg border border-[#E4E7EB] col-span-2">
                                                    <span className="text-xs text-[#718096] font-bold uppercase tracking-wider block mb-1">Transfer Açıklaması</span>
                                                    <span className="text-sm text-slate-700 italic">"{detail.description}"</span>
                                                </div>
                                            )}
                                        </div>
                                    ) : (
                                        <div className="grid grid-cols-2 gap-3">
                                            <div className="bg-white p-3.5 rounded-lg border border-[#E4E7EB]">
                                                <span className="text-xs text-[#718096] font-bold uppercase tracking-wider block mb-1">Kart Numarası</span>
                                                <span className="font-mono text-[#111] font-semibold text-sm">{formatCardNumber(detail.maskedCardNumber)}</span>
                                            </div>
                                            <div className="bg-white p-3.5 rounded-lg border border-[#E4E7EB]">
                                                <span className="text-xs text-[#718096] font-bold uppercase tracking-wider block mb-1">İşlem ID</span>
                                                <span className="font-mono text-[#111] font-semibold text-sm">#{detail.transactionId}</span>
                                            </div>
                                        </div>
                                    )}

                                    {/* Müşteri & Hesap/Kart İstihbaratı */}
                                    <h3 className="text-[#111] font-bold text-sm uppercase tracking-wider mt-6 mb-3 border-b border-[#E4E7EB] pb-2">
                                        {isTransfer ? 'Müşteri & Hesap İstihbaratı' : 'Müşteri & Kart İstihbaratı'}
                                    </h3>
                                    <div className="grid grid-cols-2 gap-3">
                                        <div className="bg-white p-3.5 rounded-lg border border-[#E4E7EB]">
                                            <span className="text-xs text-[#718096] font-bold uppercase tracking-wider block mb-1">Müşteri Adı</span>
                                            <span className="text-sm font-semibold text-[#1A1D20]">{detail.customerFullName}</span>
                                        </div>
                                        <div className="bg-white p-3.5 rounded-lg border border-[#E4E7EB]">
                                            <span className="text-xs text-[#718096] font-bold uppercase tracking-wider block mb-1">Telefon Numarası</span>
                                            <span className="text-sm font-semibold text-[#1A1D20]">{formatPhoneNumber(detail.phoneNumber)}</span>
                                        </div>
                                        {!isTransfer && detail.cardLimit > 0 ? (
                                            <>
                                                <div className="bg-white p-3.5 rounded-lg border border-[#E4E7EB]">
                                                    <span className="text-xs text-[#718096] font-bold uppercase tracking-wider block mb-1">Kart Limiti</span>
                                                    <span className="text-sm font-bold text-[#1A1D20]">{detail.cardLimit.toLocaleString('tr-TR')} TL</span>
                                                </div>
                                                <div className="bg-white p-3.5 rounded-lg border border-[#E4E7EB]">
                                                    <span className="text-xs text-[#718096] font-bold uppercase tracking-wider block mb-1">Kalan Limit</span>
                                                    <span className="text-sm font-bold text-[#1A1D20]">{detail.availableLimit.toLocaleString('tr-TR')} TL</span>
                                                </div>
                                                <div className="bg-white p-3.5 rounded-lg border border-[#E4E7EB] col-span-2">
                                                    <span className="text-xs text-[#718096] font-bold uppercase tracking-wider block mb-1">Kart Durumu</span>
                                                    <span className="text-sm font-bold text-[#1A1D20]">
                                                        {detail.isCardBlocked ? '⚠️ Blokeli' : (detail.isCardSuspicious ? '⚠️ Şüpheli' : '✅ Aktif')}
                                                    </span>
                                                </div>
                                            </>
                                        ) : (
                                            <>
                                                <div className="bg-white p-3.5 rounded-lg border border-[#E4E7EB]">
                                                    <span className="text-xs text-[#718096] font-bold uppercase tracking-wider block mb-1">Hesap Bakiyesi</span>
                                                    <span className="text-sm font-bold text-[#1A1D20]">{detail.availableLimit.toLocaleString('tr-TR')} TL</span>
                                                </div>
                                                <div className="bg-white p-3.5 rounded-lg border border-[#E4E7EB]">
                                                    <span className="text-xs text-[#718096] font-bold uppercase tracking-wider block mb-1">Hesap Durumu</span>
                                                    <span className="text-sm font-bold text-[#1A1D20]">
                                                        {detail.isCardBlocked ? '⚠️ Blokeli' : (detail.isCardSuspicious ? '⚠️ Şüpheli' : '✅ Aktif')}
                                                    </span>
                                                </div>
                                            </>
                                        )}
                                    </div>

                                    {/* Konum İstihbaratı */}
                                    <h3 className="text-[#111] font-bold text-sm uppercase tracking-wider mt-6 mb-3 border-b border-[#E4E7EB] pb-2">Konum İstihbaratı</h3>
                                    <div className="grid grid-cols-2 gap-3">
                                        <div className="bg-white p-3.5 rounded-lg border border-[#E4E7EB]">
                                            <span className="text-xs text-[#718096] font-bold uppercase tracking-wider block mb-1">İşlem Lokasyonu</span>
                                            <span className="text-sm font-semibold text-[#1A1D20]">{detail.location}, {detail.country}</span>
                                        </div>
                                        <div className="bg-white p-3.5 rounded-lg border border-[#E4E7EB]">
                                            <span className="text-xs text-[#718096] font-bold uppercase tracking-wider block mb-1">İşlem Tarihi</span>
                                            <span className="text-sm font-semibold text-[#1A1D20]">{new Date(detail.transactionDate).toLocaleDateString('tr-TR')} {new Date(detail.transactionDate).toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit' })}</span>
                                        </div>
                                    </div>

                                    {/* KARTIN/HESABIN YAPTIĞI SON 10 İŞLEM LİSTESİ */}
                                    <div className="mt-6 border-b border-[#E4E7EB] pb-2 flex flex-col gap-2">
                                        <h3 className="text-[#111] font-bold text-sm uppercase tracking-wider">
                                            {isTransfer ? 'Hesap / Transfer Geçmişi' : 'Kart Geçmişi'}
                                        </h3>
                                        <div className="flex gap-2 flex-wrap">
                                            <button
                                                type="button"
                                                onClick={() => { setHistoryTab('all'); setExpandedTxId(null); }}
                                                className={`px-3 py-1.5 rounded text-xs font-bold border transition-all cursor-pointer ${historyTab === 'all' ? 'bg-[#FFC72C] text-[#111] border-[#FFC72C]' : 'bg-white text-slate-500 border-slate-200 hover:text-[#111] hover:border-[#FFC72C]'}`}
                                            >
                                                📋 Tüm İşlemler (Son 10)
                                            </button>
                                            <button
                                                type="button"
                                                onClick={() => { setHistoryTab('suspicious'); setExpandedTxId(null); }}
                                                className={`px-3 py-1.5 rounded text-xs font-bold border transition-all cursor-pointer ${historyTab === 'suspicious' ? 'bg-[#FFC72C] text-[#111] border-[#FFC72C]' : 'bg-white text-slate-500 border-slate-200 hover:text-[#111] hover:border-[#FFC72C]'}`}
                                            >
                                                ⚠️ Şüpheli İşlemler (Son 10)
                                            </button>
                                            {detail.paymentTypeCode !== 'CreditCard' && (
                                                <button
                                                    type="button"
                                                    onClick={() => { setHistoryTab('sent'); setExpandedTxId(null); }}
                                                    className={`px-3 py-1.5 rounded text-xs font-bold border transition-all cursor-pointer ${historyTab === 'sent' ? 'bg-blue-600 text-white border-blue-600' : 'bg-white text-slate-500 border-slate-200 hover:text-blue-600 hover:border-blue-600'}`}
                                                >
                                                    📤 Gönderilen (Son 10)
                                                </button>
                                            )}
                                            {detail.paymentTypeCode !== 'CreditCard' && (
                                                <button
                                                    type="button"
                                                    onClick={() => { setHistoryTab('received'); setExpandedTxId(null); }}
                                                    className={`px-3 py-1.5 rounded text-xs font-bold border transition-all cursor-pointer ${historyTab === 'received' ? 'bg-emerald-600 text-white border-emerald-600' : 'bg-white text-slate-500 border-slate-200 hover:text-emerald-600 hover:border-emerald-600'}`}
                                                >
                                                    📥 Alınan (Son 10)
                                                </button>
                                            )}
                                        </div>
                                    </div>
                                    <div className="space-y-3 mt-3">
                                        {(() => {
                                            const txList = historyTab === 'sent'
                                                ? (detail.recentSentTransfers || [])
                                                : historyTab === 'received'
                                                    ? (detail.recentReceivedTransfers || [])
                                                    : historyTab === 'suspicious'
                                                        ? (detail.recentSuspiciousTransactions || [])
                                                        : (detail.recentTransactions || []);
                                            return txList.length > 0 ? (
                                                txList.map((tx: any, idx: number) => {
                                                    const isExpanded = expandedTxId === idx;
                                                    const isTxTransfer = tx.paymentTypeCode === 'BankTransfer' || tx.paymentTypeCode === 'EFT';
                                                    const metaInfo = isTxTransfer
                                                        ? `${tx.senderIBAN ? '...' + tx.senderIBAN.slice(-4) : ''} → ${tx.receiverName || (tx.receiverIBAN ? '...' + tx.receiverIBAN.slice(-4) : 'Alıcı')}`
                                                        : (detail.maskedCardNumber && detail.maskedCardNumber.length >= 4 ? `•••• ${detail.maskedCardNumber.slice(-4)}` : 'Kart');

                                                    return (
                                                        <div
                                                            key={idx}
                                                            onClick={() => setExpandedTxId(isExpanded ? null : idx)}
                                                            className="bg-[#F8F9FA] p-3.5 rounded-xl border border-[#E4E7EB] hover:border-[#FFC72C] transition-all flex flex-col gap-1.5 cursor-pointer shadow-sm"
                                                        >
                                                            {/* Header Bar */}
                                                            <div className="flex justify-between items-center">
                                                                <div className="flex items-center gap-3">
                                                                    {/* Badge */}
                                                                    {(() => {
                                                                        const code = tx.paymentTypeCode;
                                                                        if (code === 'CreditCard') {
                                                                            return (
                                                                                <span className="bg-[#FFC72C] text-[#111] px-2.5 py-1 rounded-lg text-xs font-bold uppercase tracking-wider shadow-sm">
                                                                                    Kredi Kartı
                                                                                </span>
                                                                            );
                                                                        }
                                                                        if (code === 'DebitCard') {
                                                                            return (
                                                                                <span className="bg-slate-700 text-white px-2.5 py-1 rounded-lg text-xs font-bold uppercase tracking-wider shadow-sm">
                                                                                    Banka Kartı
                                                                                </span>
                                                                            );
                                                                        }
                                                                        if (code === 'BankTransfer' || code === 'EFT') {
                                                                            const isOutgoing = tx.senderIBAN === detail.senderIBAN;
                                                                            return (
                                                                                <span className={`px-2.5 py-1 rounded-lg text-xs font-bold uppercase tracking-wider shadow-sm text-white ${isOutgoing ? 'bg-blue-600' : 'bg-emerald-600'}`}>
                                                                                    {isOutgoing ? 'Gönderilen' : 'Alınan'}
                                                                                </span>
                                                                            );
                                                                        }
                                                                        return (
                                                                            <span className="bg-[#FFC72C] text-[#111] px-2.5 py-1 rounded-lg text-xs font-bold uppercase tracking-wider shadow-sm">
                                                                                Kart
                                                                            </span>
                                                                        );
                                                                    })()}
                                                                    <div className="flex flex-col">
                                                                        <span className="text-sm font-black text-black leading-tight">
                                                                            {tx.amount.toLocaleString('tr-TR', { minimumFractionDigits: 2 })} {tx.currency}
                                                                        </span>
                                                                        <span className="text-xs text-slate-500 font-mono">
                                                                            {metaInfo}
                                                                        </span>
                                                                    </div>
                                                                </div>
                                                                <div className="flex items-center gap-2">
                                                                    {/* Status Badge */}
                                                                    <span className={`px-2.5 py-0.5 rounded-full border text-xs font-bold inline-flex items-center gap-1 ${tx.status === 'Approved'
                                                                            ? 'bg-emerald-500/10 text-emerald-600 border-emerald-500/20'
                                                                            : tx.status === 'Suspicious'
                                                                                ? 'bg-amber-500/10 text-amber-600 border-amber-500/20'
                                                                                : 'bg-red-500/10 text-red-600 border-red-500/20'
                                                                        }`}>
                                                                        {tx.status}
                                                                        {tx.status === 'Approved' && tx.fraudSuspicionReason && (
                                                                            <span
                                                                                className="text-amber-500 text-xs"
                                                                                title={`Daha önce şüpheli olarak işaretlendi. Gerekçe: ${tx.fraudSuspicionReason}`}
                                                                            >
                                                                                ⚠️
                                                                            </span>
                                                                        )}
                                                                    </span>
                                                                    <span className="text-xs text-slate-400 font-medium">
                                                                        {new Date(tx.transactionDate).toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit' })}
                                                                    </span>
                                                                    <svg
                                                                        className={`w-3.5 h-3.5 text-slate-400 transition-transform duration-200 ${isExpanded ? 'rotate-180' : ''}`}
                                                                        fill="none"
                                                                        stroke="currentColor"
                                                                        viewBox="0 0 24 24"
                                                                    >
                                                                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2.5} d="M19 9l-7 7-7-7" />
                                                                    </svg>
                                                                </div>
                                                            </div>

                                                            {/* Detay Açılma Alanı (Simulator Stilinde Grid) */}
                                                            {isExpanded && (
                                                                <div
                                                                    className="mt-3 pt-3 border-t border-dashed border-slate-200 text-xs text-[#1A1D20] space-y-3"
                                                                    onClick={(e) => e.stopPropagation()}
                                                                >
                                                                    <div className="grid grid-cols-2 gap-3 bg-slate-50/50 p-1.5 rounded-xl">
                                                                        {isTxTransfer ? (
                                                                            <>
                                                                                <div className="bg-white p-3 rounded-xl border border-[#E4E7EB] shadow-sm col-span-2">
                                                                                    <span className="text-xs text-[#718096] font-bold uppercase tracking-wider block mb-1">Gönderen IBAN</span>
                                                                                    <span className="font-mono text-slate-800 text-sm">{formatIBAN(tx.senderIBAN)}</span>
                                                                                </div>
                                                                                <div className="bg-white p-3 rounded-xl border border-[#E4E7EB] shadow-sm col-span-2">
                                                                                    <span className="text-xs text-[#718096] font-bold uppercase tracking-wider block mb-1">Alıcı IBAN</span>
                                                                                    <span className="font-mono text-slate-800 text-sm">{formatIBAN(tx.receiverIBAN)}</span>
                                                                                </div>
                                                                                <div className="bg-white p-3 rounded-xl border border-[#E4E7EB] shadow-sm">
                                                                                    <span className="text-xs text-[#718096] font-bold uppercase tracking-wider block mb-1">Alıcı Adı Soyadı</span>
                                                                                    <span className="font-bold text-slate-800 text-sm">{tx.receiverName || 'Bilinmiyor'}</span>
                                                                                </div>
                                                                                <div className="bg-white p-3 rounded-xl border border-[#E4E7EB] shadow-sm">
                                                                                    <span className="text-xs text-[#718096] font-bold uppercase tracking-wider block mb-1">İşlem Tipi</span>
                                                                                    <span className="font-semibold text-slate-800 text-sm">{tx.transactionTypeName}</span>
                                                                                </div>
                                                                                {tx.description && (
                                                                                    <div className="bg-white p-3 rounded-xl border border-[#E4E7EB] shadow-sm col-span-2">
                                                                                        <span className="text-xs text-[#718096] font-bold uppercase tracking-wider block mb-1">Açıklama</span>
                                                                                        <span className="text-slate-700 text-sm italic">"{tx.description}"</span>
                                                                                    </div>
                                                                                )}
                                                                            </>
                                                                        ) : (
                                                                            <>
                                                                                <div className="bg-white p-3 rounded-xl border border-[#E4E7EB] shadow-sm">
                                                                                    <span className="text-xs text-[#718096] font-bold uppercase tracking-wider block mb-1">Kart No</span>
                                                                                    <span className="font-semibold text-slate-800 text-sm">{formatCardNumber(detail.maskedCardNumber)}</span>
                                                                                </div>
                                                                                <div className="bg-white p-3 rounded-xl border border-[#E4E7EB] shadow-sm">
                                                                                    <span className="text-xs text-[#718096] font-bold uppercase tracking-wider block mb-1">İşlem Tipi</span>
                                                                                    <span className="font-semibold text-slate-800 text-sm">{tx.transactionTypeName}</span>
                                                                                </div>
                                                                                <div className="bg-white p-3 rounded-xl border border-[#E4E7EB] shadow-sm">
                                                                                    <span className="text-xs text-[#718096] font-bold uppercase tracking-wider block mb-1">Kategori</span>
                                                                                    <span className="font-semibold text-slate-800 text-sm">{tx.merchantCategory || 'Bilinmiyor'}</span>
                                                                                </div>
                                                                            </>
                                                                        )}

                                                                        {/* Lokasyon */}
                                                                        <div className="bg-white p-3 rounded-xl border border-[#E4E7EB] shadow-sm">
                                                                            <span className="text-xs text-[#718096] font-bold uppercase tracking-wider block mb-1">Lokasyon</span>
                                                                            <span className="font-semibold text-slate-800 text-sm">{tx.location}, {tx.country}</span>
                                                                        </div>

                                                                        {/* Tarih & Saat */}
                                                                        <div className="bg-white p-3 rounded-xl border border-[#E4E7EB] shadow-sm">
                                                                            <span className="text-xs text-[#718096] font-bold uppercase tracking-wider block mb-1">İşlem Zamanı</span>
                                                                            <span className="font-semibold text-slate-800 text-xs">
                                                                                {new Date(tx.transactionDate).toLocaleDateString('tr-TR')} {new Date(tx.transactionDate).toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit' })}
                                                                            </span>
                                                                        </div>

                                                                        {/* Durum */}
                                                                        <div className="bg-white p-3 rounded-xl border border-[#E4E7EB] shadow-sm">
                                                                            <span className="text-xs text-[#718096] font-bold uppercase tracking-wider block mb-1">İşlem Durumu</span>
                                                                            <span className={`font-bold text-sm ${tx.status === 'Approved' ? 'text-emerald-600' :
                                                                                    tx.status === 'Suspicious' ? 'text-amber-500' : 'text-red-500'
                                                                                }`}>{tx.status === 'Approved' ? 'Güvenli (Approved)' : tx.status === 'Suspicious' ? 'Şüpheli (Suspicious)' : 'Reddedildi (Declined)'}</span>
                                                                        </div>

                                                                        {/* Red Sebebi (Varsa) */}
                                                                        {tx.declineReason && (
                                                                            <div className="bg-white p-3 rounded-xl border border-[#E4E7EB] shadow-sm col-span-2">
                                                                                <span className="text-xs text-[#718096] font-bold uppercase tracking-wider block mb-1">Red Sebebi</span>
                                                                                <span className="font-bold text-red-600 text-sm">{tx.declineReason}</span>
                                                                            </div>
                                                                        )}

                                                                        {/* Şüphe Sebebi (Varsa) */}
                                                                        {tx.fraudSuspicionReason && (
                                                                            <div className="bg-white p-3 rounded-xl border border-[#E4E7EB] shadow-sm col-span-2">
                                                                                <span className="text-xs text-[#718096] font-bold uppercase tracking-wider block mb-1">Şüphe Sebebi</span>
                                                                                <span className="font-bold text-amber-600 text-sm">{tx.fraudSuspicionReason}</span>
                                                                            </div>
                                                                        )}

                                                                        {/* Analist Notu (Varsa) */}
                                                                        {tx.adminNote && (
                                                                            <div className="bg-white p-3 rounded-xl border border-[#E4E7EB] shadow-sm col-span-2">
                                                                                <span className="text-xs text-[#718096] font-bold uppercase tracking-wider block mb-1">Analist Gerekçe Notu</span>
                                                                                <span className="font-semibold text-slate-700 text-sm">{tx.adminNote}</span>
                                                                            </div>
                                                                        )}

                                                                        {/* Analist Adı (Varsa) */}
                                                                        {tx.resolvedByAdmin && (
                                                                            <div className="bg-white p-3 rounded-xl border border-[#E4E7EB] shadow-sm col-span-2">
                                                                                <span className="text-xs text-[#718096] font-bold uppercase tracking-wider block mb-1">Aksiyonu Alan Analist</span>
                                                                                <span className="font-semibold text-slate-800 text-sm">{tx.resolvedByAdmin}</span>
                                                                            </div>
                                                                        )}
                                                                    </div>
                                                                </div>
                                                            )}
                                                        </div>
                                                    );
                                                })
                                            ) : (
                                                <div className="text-center text-xs text-slate-400 py-4">
                                                    {historyTab === 'sent'
                                                        ? 'Bu hesaptan gönderilen herhangi bir transfer kaydı bulunmamaktadır.'
                                                        : historyTab === 'received'
                                                            ? 'Bu hesaba gelen herhangi bir transfer kaydı bulunmamaktadır.'
                                                            : historyTab === 'suspicious'
                                                                ? 'Bu karta/hesaba ait şüpheli bir geçmiş işlem bulunmamaktadır.'
                                                                : 'Bu karta/hesaba ait geçmiş işlem kaydı bulunmamaktadır.'}
                                                </div>
                                            );
                                        })()}
                                    </div>
                                </div>
                            );
                        })()
                    ) : (
                        <div className="text-center text-gray-500 py-10">Veri bulunamadı.</div>
                    )}
                </div>
            </div>
        </>
    );
};