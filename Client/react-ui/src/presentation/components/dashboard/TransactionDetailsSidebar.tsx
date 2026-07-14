import React, { useState, useEffect } from 'react';
import type { Transaction } from '../../../domain/entities/Transaction';

interface Props {
    transaction: Transaction | null;
    isOpen: boolean;
    onClose: () => void;
}

export const TransactionDetailsSidebar: React.FC<Props> = ({ transaction, isOpen, onClose }) => {
    // C# API'den gelecek detay verilerini tutacağımız state
    const [detail, setDetail] = useState<any>(null);
    const [loading, setLoading] = useState<boolean>(false);

    useEffect(() => {
        if (isOpen && transaction) {
            setLoading(true);
            // Panel açıldığında gerçek log ID'si ile C#'a gidiyoruz
            fetch(`http://localhost:5217/api/FraudManagement/log-detail/${transaction.id}`)
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

    // YENİ: Skor hesaplamasını temiz bir şekilde yukarıda yapıyoruz
    const scoreValue = transaction.riskScore ?? 0;
    const isHighRisk = scoreValue >= 70;

    return (
        <>
            {/* Arka plan karartması */}
            <div className="fixed inset-0 bg-black/50 backdrop-blur-sm z-40" onClick={onClose}></div>

            {/* Yan Panel */}
            <div className="fixed inset-y-0 right-0 w-[400px] bg-gray-900 border-l border-gray-700 shadow-2xl z-50 transform transition-transform flex flex-col">
                <div className="flex justify-between items-center p-5 border-b border-gray-800">
                    <h2 className="text-lg font-bold text-white flex items-center gap-2">
                        🔍 Detaylı Analiz Paneli
                    </h2>
                    <button onClick={onClose} className="text-gray-400 hover:text-white font-bold text-xl">&times;</button>
                </div>

                <div className="p-6 flex-1 overflow-y-auto">
                    <div className="bg-gray-950 p-4 rounded-xl border border-gray-800 mb-6 text-center">
                        <div className="text-sm text-gray-500 mb-1">İşlem Tutarı</div>
                        <div className="text-3xl font-black text-white">
                            {detail ? `${detail.amount} ${detail.currency}` : transaction.money.getFormatted()}
                        </div>
                        {/* HATA VEREN KISIM DÜZELTİLDİ: Artık doğrudan isHighRisk ve scoreValue kullanılıyor */}
                        <div className={`mt-2 inline-block px-3 py-1 rounded-full text-xs font-bold border ${isHighRisk ? 'bg-red-500/10 text-red-400 border-red-500/30' : 'bg-orange-500/10 text-orange-400 border-orange-500/30'}`}>
                            Risk Skoru: {scoreValue} / 100
                        </div>
                    </div>

                    {loading ? (
                        <div className="flex justify-center items-center py-10">
                            <span className="text-gray-400 animate-pulse">Gerçek veriler SQL'den çekiliyor...</span>
                        </div>
                    ) : detail ? (
                        <div className="space-y-4">
                            <div className="bg-gray-800/30 p-3 rounded-lg border border-gray-800">
                                <span className="text-xs text-gray-500 block mb-1">Kart Numarası</span>
                                <span className="font-mono text-gray-200">{detail.maskedCardNumber}</span>
                            </div>
                            <div className="bg-gray-800/30 p-3 rounded-lg border border-gray-800">
                                <span className="text-xs text-gray-500 block mb-1">İşlem ID</span>
                                <span className="font-mono text-gray-400 text-sm">{detail.transactionId}</span>
                            </div>
                            <div className="bg-gray-800/30 p-3 rounded-lg border border-gray-800">
                                <span className="text-xs text-gray-500 block mb-1">Tetiklenen Kural</span>
                                <span className="text-red-400 font-semibold text-sm">{detail.ruleName}</span>
                                {/* SQL'DEN GELEN GERÇEK FRAUD GEREKÇESİ BURAYA BAĞLANDI */}
                                <p className="text-xs text-gray-400 mt-1">
                                    {detail.fraudReason || detail.suspicionReason}
                                </p>
                            </div>

                            {/* C# Backend'den Gelen Gerçek Müşteri İstihbaratı */}
                            <h3 className="text-gray-400 font-bold text-xs uppercase tracking-wider mt-6 mb-3 border-b border-gray-800 pb-2">Müşteri & Kart İstihbaratı</h3>
                            <div className="grid grid-cols-2 gap-3">
                                <div className="bg-gray-950 p-3 rounded border border-gray-800">
                                    <span className="text-[10px] text-gray-500 block">Müşteri Adı</span>
                                    <span className="text-xs font-semibold text-gray-300">{detail.customerFullName}</span>
                                </div>
                                <div className="bg-gray-950 p-3 rounded border border-gray-800">
                                    <span className="text-[10px] text-gray-500 block">TC Kimlik No</span>
                                    <span className="text-xs text-gray-300">{detail.identityNumber}</span>
                                </div>
                                <div className="bg-gray-950 p-3 rounded border border-gray-800 col-span-2">
                                    <span className="text-[10px] text-gray-500 block">Kart Limiti & Durumu</span>
                                    <span className="text-xs text-gray-300">{detail.cardLimit} TL - {detail.isCardBlocked ? 'Blokeli' : 'Aktif'}</span>
                                </div>
                            </div>

                            {/* C# Backend'den Gelen Gerçek Konum İstihbaratı */}
                            <h3 className="text-gray-400 font-bold text-xs uppercase tracking-wider mt-6 mb-3 border-b border-gray-800 pb-2">Konum İstihbaratı</h3>
                            <div className="bg-gray-950 p-3 rounded border border-gray-800">
                                <span className="text-[10px] text-gray-500 block">İşlem Lokasyonu</span>
                                <span className="text-xs text-gray-300">{detail.location}, {detail.country}</span>
                            </div>
                            <div className="bg-gray-950 p-3 rounded border border-gray-800">
                                <span className="text-[10px] text-gray-500 block">İşlem Tarihi</span>
                                <span className="text-xs text-gray-300">{new Date(detail.transactionDate).toLocaleString('tr-TR')}</span>
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