import React from 'react';

interface Props {
    selectedCount: number;
    onBulkBlock: () => void;
    onClear: () => void;
}

export const BulkActionBar: React.FC<Props> = ({ selectedCount, onBulkBlock, onClear }) => {
    if (selectedCount === 0) return null;

    return (
        <div className="bg-blue-900/30 border border-blue-500/40 p-3 rounded-lg mt-4 flex justify-between items-center transition-all animate-pulse-slow">
            <div className="flex items-center gap-3">
                <span className="bg-blue-600 text-white w-7 h-7 rounded-full flex items-center justify-center text-xs font-black shadow-lg">
                    {selectedCount}
                </span>
                <span className="text-blue-100 text-sm font-medium">şüpheli işlem seçildi</span>
            </div>

            <div className="flex gap-3">
                <button
                    onClick={onClear}
                    className="px-3 py-1.5 text-xs font-bold text-blue-300 hover:text-white transition"
                >
                    ✖ Seçimi Temizle
                </button>
                <button
                    onClick={onBulkBlock}
                    className="bg-red-600 hover:bg-red-500 text-white px-4 py-1.5 rounded text-xs font-bold shadow-lg transition flex items-center gap-2 border border-red-400/50"
                >
                    🚫 Seçilenleri Bloke Et
                </button>
            </div>
        </div>
    );
};