import React from 'react';
import { useAuth } from '../../contexts/AuthContext';

interface Props {
    selectedCount: number;
    onBulkBlock: () => void;
    onClear: () => void;
    onBulkApprove: () => void;
}

export const BulkActionBar: React.FC<Props> = ({ selectedCount, onBulkBlock, onClear, onBulkApprove }) => {
    const { user } = useAuth();
    const isAnalyst = user?.role === 3;

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
                    className="relative overflow-hidden w-[170px] h-9 rounded-lg transition-all duration-500 flex items-center border-none outline-none select-none bg-[#1A1D20] text-white cursor-pointer group"
                >
                    <span className="absolute left-0 top-0 h-full w-8 flex items-center justify-center transition-all duration-500 bg-[#1A1D20] group-hover:w-full group-active:scale-90 text-sm">
                        ✖
                    </span>
                    <span className="pl-9 font-bold text-xs transition-all duration-500 group-hover:opacity-0 group-hover:translate-x-4">
                        Seçilenleri Temizle
                    </span>
                </button>

                <button
                    onClick={() => !isAnalyst && onBulkApprove()}
                    disabled={isAnalyst}
                    className={`relative overflow-hidden w-[170px] h-9 rounded-lg transition-all duration-500 flex items-center border-none outline-none select-none ${
                        isAnalyst 
                            ? 'bg-slate-200 text-slate-400 border border-slate-300 cursor-not-allowed' 
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
                        Seçilenlere İzin Ver
                    </span>
                </button>

                <button
                    onClick={() => !isAnalyst && onBulkBlock()}
                    disabled={isAnalyst}
                    className={`relative overflow-hidden w-[170px] h-9 rounded-lg transition-all duration-500 flex items-center border-none outline-none select-none ${
                        isAnalyst 
                            ? 'bg-slate-200 text-slate-400 border border-slate-300 cursor-not-allowed' 
                            : 'bg-red-600 text-white cursor-pointer group'
                    }`}
                >
                    {!isAnalyst && (
                        <span className="absolute left-0 top-0 h-full w-8 flex items-center justify-center transition-all duration-500 bg-red-600 group-hover:w-full group-active:scale-90 text-sm">
                            🚫
                        </span>
                    )}
                    <span className={`font-bold text-xs transition-all duration-500 ${
                        isAnalyst ? 'pl-6' : 'pl-9 group-hover:opacity-0 group-hover:translate-x-4'
                    }`}>
                        Seçilenleri Bloke Et
                    </span>
                </button>
            </div>
        </div>
    );
};