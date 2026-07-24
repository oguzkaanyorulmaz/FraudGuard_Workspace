import React, { useState } from 'react';
import { useAuth } from '../../contexts/AuthContext';

export const Header: React.FC = () => {
    const { user, logout } = useAuth();
    const [showConfirm, setShowConfirm] = useState(false);

    return (
        <>
            <div className="w-full bg-[#FDBB30] border-b border-[#E5A520]/80 shadow-md">
                <div className="w-full px-3 py-2.5 md:px-8 md:py-3.5 flex justify-between items-center">
                    <div className="flex items-center gap-2 md:gap-3">
                        <div className="flex items-center gap-1.5 md:gap-2.5 font-sans">
                            <span className="bg-[#111111] text-[#FDBB30] font-black italic text-lg md:text-xl px-2 md:px-2.5 py-0.5 rounded shadow-sm">FG</span>
                            <span className="text-base md:text-xl font-extrabold tracking-tight text-[#111111]">FraudGuard</span>
                        </div>
                    </div>
                    <div className="flex items-center gap-1.5 md:gap-3">
                        {user && (
                            <>
                                <div className="flex items-center gap-1 md:gap-2 bg-[#111111]/10 text-[#111111] px-2 md:px-4 py-1 md:py-1.5 rounded-lg text-[10px] md:text-xs font-bold border border-[#111111]/20">
                                    <span>👤 {user.username}</span>
                                </div>
                                <button 
                                    onClick={() => setShowConfirm(true)} 
                                    className="bg-[#111111] hover:bg-black text-white px-2.5 md:px-4 py-1 md:py-1.5 rounded-lg text-[10px] md:text-xs font-bold transition cursor-pointer shadow-sm whitespace-nowrap"
                                >
                                    Çıkış
                                </button>
                            </>
                        )}
                    </div>
                </div>
            </div>

            {/* Çıkış Yap Onay Modalı */}
            {showConfirm && (
                <div className="fixed inset-0 bg-black/50 backdrop-blur-sm flex justify-center items-center z-50">
                    <div className="bg-white rounded-2xl shadow-2xl p-6 w-96 max-w-[90%] transform transition-all border border-gray-100">
                        <div className="flex items-center gap-3 text-red-600 mb-4">
                            <span className="text-2xl">⚠️</span>
                            <h3 className="text-lg font-bold text-gray-900">Oturumu Kapat</h3>
                        </div>
                        <p className="text-sm text-gray-600 mb-6">
                            Hesabınızdan çıkış yapmak istediğinize emin misiniz?
                        </p>
                        <div className="flex justify-end gap-3 text-xs font-bold">
                            <button
                                onClick={() => setShowConfirm(false)}
                                className="px-4 py-2 rounded-xl text-gray-700 bg-gray-100 hover:bg-gray-200 transition cursor-pointer"
                            >
                                Vazgeç
                            </button>
                            <button
                                onClick={() => {
                                    setShowConfirm(false);
                                    logout();
                                }}
                                className="px-4 py-2 rounded-xl text-white bg-red-600 hover:bg-red-700 transition cursor-pointer"
                            >
                                Çıkış Yap
                            </button>
                        </div>
                    </div>
                </div>
            )}
        </>
    );
};
