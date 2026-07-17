import React, { useState, useEffect } from 'react';
import type { Transaction } from '../../../domain/entities/Transaction';
import { useAuth } from '../../contexts/AuthContext';

interface Props {
    transaction: Transaction | null;
    isOpen: boolean;
    onClose: () => void;
}

export const TransactionDetailsSidebar: React.FC<Props> = ({ transaction, isOpen, onClose }) => {
    // C# API'den gelecek detay verilerini tutacağımız state
    const [detail, setDetail] = useState<any>(null);
    const [loading, setLoading] = useState<boolean>(false);
    const { user } = useAuth();
    // const isAnalyst = user?.role === 3;

    useEffect(() => {
        if (isOpen && transaction) {
            setLoading(true);
            const token = user?.token || '';
            // Panel açıldığında gerçek log ID'si ile C#'a gidiyoruz
            fetch(`http://localhost:5217/api/FraudManagement/log-detail/${transaction.id}`, {
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
        } else {
            setDetail(null); // Panel kapanınca veriyi temizle
        }
    }, [isOpen, transaction]);

    if (!isOpen || !transaction) return null;

    // Skor hesaplaması
    const scoreValue = transaction.riskScore ?? 0;
    const isHighRisk = scoreValue >= 70;

    return (
        <>
            {/* Arka plan karartması */}
            <div className="fixed inset-0 bg-black/50 backdrop-blur-sm z-40" onClick={onClose}></div>

            {/* Yan Panel */}
            <div className="fixed inset-y-0 right-0 w-[420px] bg-white border-l border-[#E4E7EB] shadow-2xl z-50 transform transition-transform flex flex-col">
                <div className="flex justify-between items-center p-5 border-b border-[#E4E7EB]">
                    <h2 className="text-lg font-bold text-[#111] flex items-center gap-2">
                        🔍 Detaylı Analiz Paneli
                    </h2>
                    <button onClick={onClose} className="text-[#718096] hover:text-[#111] font-bold text-xl cursor-pointer">&times;</button>
                </div>

                <div className="p-6 flex-1 overflow-y-auto">
                    {/* İşlem Tutarı */}
                    <div className="bg-[#F8F9FA] p-5 rounded-xl border border-[#E4E7EB] mb-6 text-center">
                        <div className="text-[11px] text-[#718096] font-bold uppercase tracking-wider mb-1">İşlem Tutarı</div>
                        <div className="text-3xl font-black text-[#111]">
                            {detail ? `${detail.amount} ${detail.currency}` : transaction.money.getFormatted()}
                        </div>
                        {detail?.transactionTypeName && (
                            <div className="text-xs font-bold text-slate-500 mt-1 uppercase tracking-wider">
                                ⚙️ {detail.transactionTypeName}
                            </div>
                        )}
                        <div className={`mt-3 inline-block px-3 py-1 rounded-full text-xs font-bold border ${isHighRisk ? 'bg-red-500/10 text-red-600 border-red-500/20' : 'bg-amber-500/10 text-amber-600 border-amber-500/20'}`}>
                            Risk Skoru: {scoreValue} / 100
                        </div>
                    </div>

                    {/* Çözümlenmiş işlem ise Analiz Kararı ve Analist bilgisi */}
                    {detail && detail.adminNote && (
                        <div className="bg-emerald-50 border border-emerald-200/60 p-4 rounded-xl text-xs text-emerald-950 mb-6 flex flex-col gap-1.5 shadow-sm">
                            <div className="font-black flex items-center gap-1 text-emerald-800 uppercase tracking-wider text-[10px]">📋 Analiz Kararı</div>
                            <div className="font-semibold text-slate-800">Gerekçe: <span className="font-normal text-slate-700">{detail.adminNote}</span></div>
                            {detail.resolvedByAdmin && (
                                <div className="text-[10px] text-slate-500 font-bold italic mt-1 border-t border-emerald-100 pt-1.5">
                                    Aksiyonu Alan Analist: <span className="text-slate-700 font-semibold">{detail.resolvedByAdmin}</span>
                                </div>
                            )}
                        </div>
                    )}

                    {loading ? (
                        <div className="flex justify-center items-center py-10">
                            <span className="text-gray-400 animate-pulse">Gerçek veriler SQL'den çekiliyor...</span>
                        </div>
                    ) : detail ? (
                        <div className="space-y-4">
                            {/* Kart & İşlem ID Bilgileri */}
                            <div className="bg-white p-3.5 rounded-lg border border-[#E4E7EB]">
                                <span className="text-[10px] text-[#718096] font-bold uppercase tracking-wider block mb-1">Kart Numarası</span>
                                <span className="font-mono text-[#111] font-semibold">{detail.maskedCardNumber}</span>
                            </div>
                            <div className="bg-white p-3.5 rounded-lg border border-[#E4E7EB]">
                                <span className="text-[10px] text-[#718096] font-bold uppercase tracking-wider block mb-1">İşlem ID</span>
                                <span className="font-mono text-[#718096] text-sm">#{detail.transactionId}</span>
                            </div>

                            {/* Müşteri & Kart İstihbaratı */}
                            <h3 className="text-[#111] font-bold text-xs uppercase tracking-wider mt-6 mb-3 border-b border-[#E4E7EB] pb-2">Müşteri & Kart İstihbaratı</h3>
                            <div className="grid grid-cols-2 gap-3">
                                <div className="bg-white p-3.5 rounded-lg border border-[#E4E7EB]">
                                    <span className="text-[10px] text-[#718096] font-bold uppercase tracking-wider block mb-1">Müşteri Adı</span>
                                    <span className="text-xs font-semibold text-[#1A1D20]">{detail.customerFullName}</span>
                                </div>
                                <div className="bg-white p-3.5 rounded-lg border border-[#E4E7EB]">
                                    <span className="text-[10px] text-[#718096] font-bold uppercase tracking-wider block mb-1">Telefon Numarası</span>
                                    <span className="text-xs font-semibold text-[#1A1D20]">{detail.phoneNumber || 'Kayıt Yok'}</span>
                                </div>
                                <div className="bg-white p-3.5 rounded-lg border border-[#E4E7EB] col-span-2">
                                    {detail.cardLimit > 0 ? (
                                        <>
                                            <span className="text-[10px] text-[#718096] font-bold uppercase tracking-wider block mb-1">Kart Limiti, Kalan Limit & Durum</span>
                                            <span className="text-xs font-semibold text-[#1A1D20]">Limit: {detail.cardLimit.toLocaleString('tr-TR')} TL | Kalan Limit: {detail.availableLimit.toLocaleString('tr-TR')} TL | {detail.isCardBlocked ? '⚠️ Blokeli' : '✅ Aktif'}</span>
                                        </>
                                    ) : (
                                        <>
                                            <span className="text-[10px] text-[#718096] font-bold uppercase tracking-wider block mb-1">Hesap Bakiyesi & Durum</span>
                                            <span className="text-xs font-semibold text-[#1A1D20]">Bakiye: {detail.availableLimit.toLocaleString('tr-TR')} TL | {detail.isCardBlocked ? '⚠️ Blokeli' : '✅ Aktif'}</span>
                                        </>
                                    )}
                                </div>
                            </div>

                            {/* Konum İstihbaratı */}
                            <h3 className="text-[#111] font-bold text-xs uppercase tracking-wider mt-6 mb-3 border-b border-[#E4E7EB] pb-2">Konum İstihbaratı</h3>
                            <div className="grid grid-cols-2 gap-3">
                                <div className="bg-white p-3.5 rounded-lg border border-[#E4E7EB]">
                                    <span className="text-[10px] text-[#718096] font-bold uppercase tracking-wider block mb-1">İşlem Lokasyonu</span>
                                    <span className="text-xs font-semibold text-[#1A1D20]">{detail.location}, {detail.country}</span>
                                </div>
                                <div className="bg-white p-3.5 rounded-lg border border-[#E4E7EB]">
                                    <span className="text-[10px] text-[#718096] font-bold uppercase tracking-wider block mb-1">İşlem Tarihi</span>
                                    <span className="text-xs font-semibold text-[#1A1D20]">{new Date(detail.transactionDate).toLocaleDateString('tr-TR')}</span>
                                </div>
                            </div>

                            {/* KARTIN YAPTIĞI SON 10 İŞLEM LİSTESİ */}
                            <h3 className="text-[#111] font-bold text-xs uppercase tracking-wider mt-6 mb-3 border-b border-[#E4E7EB] pb-2">Kart Geçmişi (Son 10 İşlem)</h3>
                            <div className="space-y-3">
                                {detail.recentTransactions && detail.recentTransactions.length > 0 ? (
                                    detail.recentTransactions.map((tx: any, idx: number) => (
                                        <div key={idx} className="bg-[#F8F9FA] p-3.5 rounded-xl border border-[#E4E7EB] hover:border-[#FFC72C] transition-all flex flex-col gap-1.5">
                                            <div className="flex justify-between items-center">
                                                <span className="text-xs font-black text-black">
                                                    {tx.amount} {tx.currency} <span className="text-[10px] font-bold text-slate-500 bg-slate-100 px-1.5 py-0.5 rounded">({tx.transactionTypeName})</span>
                                                </span>
                                                <span className="text-[10px] bg-white text-slate-600 px-2.5 py-0.5 rounded-full border border-slate-200 font-bold">{tx.merchantCategory}</span>
                                            </div>
                                            <div className="flex justify-between items-center text-[11px] text-slate-500">
                                                <span>📍 {tx.location}, {tx.country}</span>
                                                <span>⏱️ {new Date(tx.transactionDate).toLocaleDateString('tr-TR')}</span>
                                            </div>
                                            {tx.fraudSuspicionReason && (
                                                <div className="mt-2 p-2 bg-amber-50 rounded border border-amber-200 text-[10px] text-amber-900 flex flex-col gap-1">
                                                    <div className="font-bold flex items-center gap-1">⚠️ Şüphe Sebebi: <span className="font-medium text-slate-700">{tx.fraudSuspicionReason}</span></div>
                                                    {tx.adminNote && <div className="mt-0.5 flex items-start gap-1 font-bold text-[#A67E00]">↳ Not: <span className="font-medium text-slate-700">{tx.adminNote}</span></div>}
                                                    {tx.resolvedByAdmin && <div className="text-[9px] text-slate-400 font-semibold italic mt-0.5 align-self-end">Onaylayan: {tx.resolvedByAdmin}</div>}
                                                </div>
                                            )}
                                            {tx.declineReason && (
                                                <div className={`mt-2 p-2 rounded border text-[10px] flex flex-col gap-1 ${tx.declineReason === 'Hatalı CVV'
                                                        ? 'bg-red-50 border-red-200 text-red-900'
                                                        : 'bg-slate-50 border-slate-200 text-slate-900'
                                                    }`}>
                                                    <div className="font-bold flex items-center gap-1">
                                                        ❌ Red Nedeni: <span className={`font-semibold ${tx.declineReason === 'Hatalı CVV' ? 'text-red-700 font-bold' : 'text-slate-700'}`}>{tx.declineReason}</span>
                                                    </div>
                                                </div>
                                            )}
                                        </div>
                                    ))
                                ) : (
                                    <div className="text-center text-xs text-slate-400 py-4">Bu kartın geçmiş işlem kaydı bulunmamaktadır.</div>
                                )}
                            </div>
                        </div>
                    ) : (
                        <div className="text-center text-gray-500 py-10">Veri bulunamadı.</div>
                    )}
                </div>
            </div>
        </>
    );
};