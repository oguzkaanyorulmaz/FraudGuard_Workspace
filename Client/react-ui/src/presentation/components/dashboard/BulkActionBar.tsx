import React from 'react';

interface Props {
    selectedCount: number;
    onBulkBlock: () => void;
    onClear: () => void;
    onBulkApprove: () => void;
}

export const BulkActionBar: React.FC<Props> = ({ selectedCount, onBulkBlock, onClear, onBulkApprove }) => {
    if (selectedCount === 0) return null;

    return (
        <div className="bg-amber-50 border border-amber-200/80 p-3.5 rounded-xl mt-4 flex justify-between items-center transition-all shadow-sm">
            <div className="flex items-center gap-3">
                <span className="bg-[#FFC72C] text-[#111] w-7 h-7 rounded-full flex items-center justify-center text-xs font-black shadow-sm">
                    {selectedCount}
                </span>
                <span className="text-[#1A1D20] text-sm font-semibold">şüpheli işlem seçildi</span>
            </div>

            <div className="flex gap-3">
                <button
                    onClick={onClear}
                    title="Seçilenleri temizlemek için tıklayın"
                    className="bg-[#1A1D20] hover:bg-[#2D3136] text-white px-4 py-1.5 rounded-lg text-xs font-bold shadow-sm transition cursor-pointer"
                >
                    ✖ Seçilenleri Temizle
                </button>

                <button
                    onClick={onBulkApprove}
                    className="bg-emerald-600 hover:bg-emerald-500 text-white px-4 py-1.5 rounded-lg text-xs font-bold shadow-sm transition flex items-center gap-2 border border-emerald-400/20 cursor-pointer"
                >
                    ✅ Seçilenlere İzin Ver
                </button>


                <button
                    onClick={onBulkBlock}
                    className="bg-red-600 hover:bg-red-500 text-white px-4 py-1.5 rounded-lg text-xs font-bold shadow-sm transition flex items-center gap-2 border border-red-400/20 cursor-pointer"
                >
                    🚫 Seçilenleri Bloke Et
                </button>
            </div>
        </div>
    );
};