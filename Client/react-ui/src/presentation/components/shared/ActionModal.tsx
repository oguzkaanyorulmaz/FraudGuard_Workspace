import React, { useState } from 'react';

interface Props {
    isOpen: boolean;
    actionType: 'APPROVE' | 'BLOCK' | null;
    transactionId: string | null;
    // onConfirm'e seçilen blockReasonId eklendi (Opsiyonel numara dönecek)
    onConfirm: (id: string, reason: string, blockReasonId?: number) => void;
    onCancel: () => void;
}

export const ActionModal: React.FC<Props> = ({ isOpen, actionType, transactionId, onConfirm, onCancel }) => {
    const [reason, setReason] = useState('');
    const [error, setError] = useState('');
    
    // YENİ: Combobox state'i (Varsayılan 2: Fraud)
    const [selectedReasonId, setSelectedReasonId] = useState<number>(2);

    if (!isOpen || !transactionId) return null;

    const isBlock = actionType === 'BLOCK';

    const handleConfirm = () => {
        if (reason.trim().length < 10) {
            setError('Lütfen en az 10 karakterlik bir gerekçe girin.');
            return;
        }
        
        // Eğer işlem blokajsa ID'yi yolla, değilse undefined yolla
        onConfirm(transactionId, reason, isBlock ? selectedReasonId : undefined);
        
        setReason('');
        setError('');
        setSelectedReasonId(2); // Modalı sıfırla
    };

    const handleCancel = () => {
        setReason('');
        setError('');
        setSelectedReasonId(2);
        onCancel();
    };

    return (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-sm p-4">
            <div className="bg-gray-900 border border-gray-700 rounded-xl shadow-2xl w-full max-w-md overflow-hidden transform transition-all">

                {/* Modal Başlığı (Değişmedi) */}
                <div className={`p-4 border-b ${isBlock ? 'bg-red-950/30 border-red-900/50' : 'bg-emerald-950/30 border-emerald-900/50'}`}>
                    <h2 className={`text-lg font-bold flex items-center gap-2 ${isBlock ? 'text-red-400' : 'text-emerald-400'}`}>
                        {isBlock ? '🚫 İşlemi Bloke Et' : '✅ İşleme İzin Ver'}
                    </h2>
                    <p className="text-gray-400 text-xs mt-1">
                        İşlem ID: <span className="font-mono text-white">{transactionId}</span>
                    </p>
                </div>

                <div className="p-5">
                    {/* YENİ: Blokaj ise Combobox'ı göster */}
                    {isBlock && (
                        <div className="mb-4">
                            <label className="block text-sm font-medium text-gray-300 mb-2">
                                Blokaj Sebebi (Zorunlu)
                            </label>
                            <select
                                value={selectedReasonId}
                                onChange={(e) => setSelectedReasonId(Number(e.target.value))}
                                className="w-full bg-gray-950 border border-gray-700 rounded-lg p-3 text-sm text-gray-200 focus:outline-none focus:border-red-500"
                            >
                                <option value={1}>Çalıntı (Stolen)</option>
                                <option value={2}>Dolandırıcılık Şüphesi (Fraud)</option>
                                <option value={3}>Kayıp (Lost)</option>
                            </select>
                        </div>
                    )}

                    {/* Aksiyon Gerekçesi (Textarea) */}
                    <label className="block text-sm font-medium text-gray-300 mb-2">
                        Aksiyon Gerekçesi (Zorunlu)
                    </label>
                    <textarea
                        className={`w-full bg-gray-950 border ${error ? 'border-red-500' : 'border-gray-700'} rounded-lg p-3 text-sm text-gray-200 focus:outline-none focus:border-blue-500 resize-none`}
                        rows={4}
                        placeholder="Analiz sonucunuzu ve karar sebebinizi buraya yazın..."
                        value={reason}
                        onChange={(e) => {
                            setReason(e.target.value);
                            if (error) setError('');
                        }}
                    />
                    {error && <p className="text-red-500 text-xs mt-2 font-medium animate-pulse">{error}</p>}

                    <div className="bg-gray-800/50 rounded-lg p-3 mt-4 text-xs text-gray-400 border border-gray-800">
                        ℹ️ Girdiğiniz gerekçe FraudGuard veritabanına silinmez bir log olarak kaydedilecektir.
                    </div>
                </div>

                {/* Butonlar (Değişmedi) */}
                <div className="p-4 border-t border-gray-800 flex gap-3 justify-end bg-gray-900/50">
                    <button onClick={handleCancel} className="px-4 py-2 text-sm font-bold text-gray-400 hover:text-white transition">İptal</button>
                    <button onClick={handleConfirm} className={`px-4 py-2 rounded-lg text-sm font-bold shadow-lg transition ${isBlock ? 'bg-red-600 hover:bg-red-500 text-white' : 'bg-emerald-600 hover:bg-emerald-500 text-white'}`}>
                        {isBlock ? 'Blokajı Onayla' : 'İzni Onayla'}
                    </button>
                </div>
            </div>
        </div>
    );
};