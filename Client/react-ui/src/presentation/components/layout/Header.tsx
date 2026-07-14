import React from 'react';

export const Header: React.FC = () => {
    return (
        <div className="flex justify-between items-center mb-8 border-b border-[#E4E7EB] pb-5">
            <div className="flex items-center gap-3">
                <div className="flex items-center gap-2 font-sans">
                    <span className="bg-[#FFC72C] text-[#111] font-black italic text-xl px-2.5 py-0.5 rounded shadow-sm">FG</span>
                    <span className="text-xl font-bold tracking-tight text-[#111]">FraudGuard</span>
                </div>
            </div>
            <div className="flex items-center gap-3">
                {/* 🔴 Canlı Akış Aktif uyarısı kırmızı yapıldı */}
                <div className="flex items-center gap-2 bg-red-50 border border-red-200 px-3 py-1.5 rounded-lg text-[11px] font-bold text-red-600 shadow-sm">
                    <span className="w-1.5 h-1.5 rounded-full bg-red-600 animate-pulse"></span> CANLI AKIŞ AKTİF
                </div>
                <div className="flex items-center gap-2 bg-[#111111] text-white px-4 py-1.5 rounded-lg text-xs font-semibold hover:bg-black transition cursor-pointer">
                    <span>👤 Analist: Oğuz Kaan</span>
                </div>
            </div>
        </div>
    );
};
