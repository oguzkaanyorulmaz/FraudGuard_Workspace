import React from 'react';

export const Header: React.FC = () => {
    return (
        <div className="flex justify-between items-center mb-6 border-b border-gray-800 pb-4">
            <div>
                <h1 className="text-2xl font-black tracking-wider flex items-center gap-2 text-yellow">
                    🛡️ FRAUDGUARD <span className="text-xs bg-blue-600/20 text-blue-400 font-semibold px-2.5 py-1 rounded-full border border-blue-500/30">13.07.2026</span>
                </h1>
            </div>
            <div className="flex items-center gap-4">
                <div className="flex items-center gap-2 bg-red-950/40 border border-red-500/30 px-3 py-1.5 rounded-lg text-xs font-semibold text-red-400">
                    <span className="w-2 h-2 rounded-full bg-red-500 animate-pulse"></span> CANLI AKIŞ AKTİF
                </div>
                <div className="flex items-center gap-2 bg-gray-900 border border-gray-800 px-3 py-1.5 rounded-lg text-xs">
                    <span className="text-gray-400">👤 Analist:</span> <span className="font-medium text-blue-400">Oğuz Kaan</span>
                </div>
            </div>
        </div>
    );
};