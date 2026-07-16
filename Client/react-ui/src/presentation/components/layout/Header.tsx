import React from 'react';
import { useAuth } from '../../contexts/AuthContext';

export const Header: React.FC = () => {
    const { user, logout } = useAuth();

    return (
        <div className="flex justify-between items-center mb-8 border-b border-[#E4E7EB] pb-5">
            <div className="flex items-center gap-3">
                <div className="flex items-center gap-2 font-sans">
                    <span className="bg-[#FFC72C] text-[#111] font-black italic text-xl px-2.5 py-0.5 rounded shadow-sm">FG</span>
                    <span className="text-xl font-bold tracking-tight text-[#111]">FraudGuard</span>
                </div>
            </div>
            <div className="flex items-center gap-3">
                {user && (
                    <>
                        <div className="flex items-center gap-2 bg-[#111111] text-white px-4 py-1.5 rounded-lg text-xs font-semibold">
                            <span>👤 Kullanıcı: {user.username}</span>
                        </div>
                        <button 
                            onClick={logout} 
                            className="bg-red-600 hover:bg-red-700 text-white px-4 py-1.5 rounded-lg text-xs font-semibold transition cursor-pointer"
                        >
                            Çıkış Yap
                        </button>
                    </>
                )}
            </div>
        </div>
    );
};
