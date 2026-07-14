import React, { useState } from 'react';

interface Props {
    isOpen: boolean;
    actionType: 'APPROVE' | 'BLOCK' | null;
    transactionId: string | null;
    onConfirm: (id: string, reason: string, blockReasonId?: number, analystName?: string) => void;
    onCancel: () => void;
}

export const ActionModal: React.FC<Props> = ({ isOpen, actionType, transactionId, onConfirm, onCancel }) => {
    const [reason, setReason] = useState('');
    const [error, setError] = useState('');
    const [selectedReasonId, setSelectedReasonId] = useState<number>(2);

    const [analystName, setAnalystName] = useState('Oğuz Kaan');

    if (!isOpen || !transactionId) return null;
    const isBlock = actionType === 'BLOCK';

    const handleConfirm = () => {
        if (!analystName.trim()) {
            setError('Lütfen analist adını giriniz.');
            return;
        }
        if (reason.trim().length < 10) {
            setError('Lütfen en az 10 karakterlik bir gerekçe girin.');
            return;
        }

        onConfirm(transactionId, reason, isBlock ? selectedReasonId : undefined, analystName);

        setReason('');
        setError('');
        setSelectedReasonId(2);
    };

    const handleCancel = () => {
        setReason('');
        setError('');
        setSelectedReasonId(2);
        onCancel();
    };

    return (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-sm p-4">
            <div className="bg-white border border-[#E4E7EB] rounded-2xl shadow-2xl w-full max-w-md overflow-hidden transform transition-all text-[#1A1D20]">

                <div className={`p-5 border-b ${isBlock ? 'bg-red-50 border-red-100' : 'bg-emerald-50 border-emerald-100'}`}>
                    <h2 className={`text-lg font-bold flex items-center gap-2 ${isBlock ? 'text-red-700' : 'text-emerald-700'}`}>
                        {isBlock ? '🚫 İşlemi Bloke Et' : '✅ İşleme İzin Ver'}
                    </h2>
                    <p className="text-slate-500 text-xs mt-1">
                        İşlem ID: <span className="font-mono text-black font-semibold">#{transactionId}</span>
                    </p>
                </div>

                <div className="p-5 space-y-4">
                    <div>
                        <label className="block text-xs font-bold uppercase tracking-wider text-slate-500 mb-1">
                            Analist Adı Soyadı (Zorunlu)
                        </label>
                        <input
                            type="text"
                            className="w-full bg-white border border-[#C5CBD3] rounded-lg p-2.5 text-sm text-[#1A1D20] focus:outline-none focus:border-[#FFC72C]"
                            value={analystName}
                            onChange={(e) => setAnalystName(e.target.value)}
                            placeholder="Adınızı ve soyadınızı girin..."
                        />
                    </div>

                    {isBlock && (
                        <div>
                            <label className="block text-xs font-bold uppercase tracking-wider text-slate-500 mb-1">
                                Blokaj Sebebi (Zorunlu)
                            </label>
                            <select
                                value={selectedReasonId}
                                onChange={(e) => setSelectedReasonId(Number(e.target.value))}
                                className="w-full bg-white border border-[#C5CBD3] rounded-lg p-2.5 text-sm text-[#1A1D20] focus:outline-none focus:border-[#FFC72C]"
                            >
                                <option value={1}>Çalıntı (Stolen)</option>
                                <option value={2}>Dolandırıcılık Şüphesi (Fraud)</option>
                                <option value={3}>Kayıp (Lost)</option>
                            </select>
                        </div>
                    )}

                    <div>
                        <label className="block text-xs font-bold uppercase tracking-wider text-slate-500 mb-1">
                            Aksiyon Gerekçesi (Zorunlu)
                        </label>
                        <textarea
                            className={`w-full bg-white border ${error && reason.trim().length < 10 ? 'border-red-500' : 'border-[#C5CBD3]'} rounded-lg p-2.5 text-sm text-[#1A1D20] focus:outline-none focus:border-blue-500 resize-none`}
                            rows={3}
                            placeholder="Analiz sonucunuzu ve karar sebebinizi buraya yazın..."
                            value={reason}
                            onChange={(e) => {
                                setReason(e.target.value);
                                if (error) setError('');
                            }}
                        />
                        {error && <p className="text-red-500 text-xs mt-1 font-medium">{error}</p>}
                    </div>
                </div>

                <div className="p-4 border-t border-[#E4E7EB] flex gap-3 justify-end bg-slate-50">
                    <button onClick={handleCancel} className="px-4 py-2 text-sm font-bold text-slate-500 hover:text-slate-800 transition cursor-pointer">İptal</button>
                    <button onClick={handleConfirm} className={`px-5 py-2 rounded-lg text-sm font-bold shadow transition cursor-pointer ${isBlock ? 'bg-red-600 hover:bg-red-500 text-white' : 'bg-[#FFC72C] hover:bg-[#E5B224] text-[#111]'}`}>
                        {isBlock ? 'Blokajı Onayla' : 'İzni Onayla'}
                    </button>
                </div>
            </div>
        </div>
    );
};
