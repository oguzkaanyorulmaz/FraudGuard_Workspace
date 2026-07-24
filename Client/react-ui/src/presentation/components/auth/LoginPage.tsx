import React, { useState } from 'react';
import { useAuth } from '../../contexts/AuthContext';

export const LoginPage: React.FC = () => {
    const { login, register } = useAuth();
    const [isRegisterMode, setIsRegisterMode] = useState(false);
    const [username, setUsername] = useState('');
    const [password, setPassword] = useState('');
    const [email, setEmail] = useState('');
    const [role, setRole] = useState<number>(3); // Default: Analist (3)
    const [error, setError] = useState('');
    const [successMessage, setSuccessMessage] = useState('');
    const [loading, setLoading] = useState(false);

    const handleLoginSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setError('');
        setSuccessMessage('');
        setLoading(true);

        const success = await login(username, password);
        if (!success) {
            setError('Kullanıcı adı veya şifre hatalı.');
        }
        setLoading(false);
    };

    const handleRegisterSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setError('');
        setSuccessMessage('');
        setLoading(true);

        const success = await register(username, email, password, role);
        if (success) {
            setSuccessMessage('Kayıt başarılı! Şimdi giriş yapabilirsiniz.');
            setIsRegisterMode(false);
            setPassword('');
        } else {
            setError('Kayıt işlemi başarısız. Kullanıcı adı zaten alınmış olabilir.');
        }
        setLoading(false);
    };

    return (
        <div className="min-h-screen bg-[#F4F5F7] flex flex-col justify-between font-sans">
            {/* Üst Bar (Header) */}
            <header className="bg-white border-b border-[#E4E7EB] py-4 px-6 md:px-12 flex justify-between items-center shadow-sm">
                <div className="flex items-center gap-2">
                    {/* FG Logo */}
                    <span className="bg-[#FDBB30] text-[#111] font-black italic text-lg px-2.5 py-0.5 rounded shadow-sm">FG</span>
                    <span className="text-lg font-bold tracking-tight text-[#111]">FraudGuard</span>
                </div>
                <div className="text-xs text-[#718096] font-bold flex items-center gap-1.5 select-none">

                </div>
            </header>

            {/* Orta Kısım (Main Form) */}
            <main className="flex-1 flex flex-col items-center justify-center p-6 md:p-12">
                <div className="w-full max-w-md">
                    <h1 className="text-center text-2xl font-bold text-[#555] mb-6 tracking-wide select-none">
                        Hoş Geldiniz
                    </h1>

                    <div className="bg-white rounded-2xl shadow-xl border border-[#E4E7EB] overflow-hidden">
                        {/* Bireysel/Ticari Yerine Giriş Yap / Kayıt Ol Sekmeleri */}
                        <div className="flex border-b border-[#E4E7EB] bg-[#FAFBFD]">
                            <button
                                type="button"
                                onClick={() => {
                                    setIsRegisterMode(false);
                                    setError('');
                                    setSuccessMessage('');
                                }}
                                className={`flex-1 py-4 text-center text-xs md:text-sm font-extrabold transition-all border-b-2 cursor-pointer ${!isRegisterMode
                                    ? 'border-[#FDBB30] text-[#111] bg-white'
                                    : 'border-transparent text-[#718096] hover:text-[#111]'
                                    }`}
                            >
                                GİRİŞ YAP
                            </button>
                            <button
                                type="button"
                                onClick={() => {
                                    setIsRegisterMode(true);
                                    setError('');
                                    setSuccessMessage('');
                                }}
                                className={`flex-1 py-4 text-center text-xs md:text-sm font-extrabold transition-all border-b-2 cursor-pointer ${isRegisterMode
                                    ? 'border-[#FDBB30] text-[#111] bg-white'
                                    : 'border-transparent text-[#718096] hover:text-[#111]'
                                    }`}
                            >
                                KAYIT OL
                            </button>
                        </div>

                        {/* Form Gövdesi */}
                        <div className="p-8">
                            {/* Hata Mesajı */}
                            {error && (
                                <div className="bg-red-50 border border-red-200 text-red-700 text-xs font-semibold px-4 py-3 rounded-xl mb-6">
                                    {error}
                                </div>
                            )}

                            {/* Başarı Mesajı */}
                            {successMessage && (
                                <div className="bg-emerald-50 border border-emerald-200 text-emerald-700 text-xs font-semibold px-4 py-3 rounded-xl mb-6">
                                    {successMessage}
                                </div>
                            )}

                            {!isRegisterMode ? (
                                /* GİRİŞ FORMU */
                                <form onSubmit={handleLoginSubmit} className="space-y-6">
                                    <div>
                                        <label className="block text-xs font-bold text-[#718096] uppercase tracking-wider mb-2">
                                            Kullanıcı Adı
                                        </label>
                                        <div className="relative">
                                            <input
                                                type="text"
                                                value={username}
                                                onChange={(e) => setUsername(e.target.value)}
                                                className="w-full pl-4 pr-10 py-3.5 border border-[#E4E7EB] rounded-xl text-sm font-semibold text-[#111] focus:outline-none focus:ring-2 focus:ring-[#FDBB30]/30 focus:border-[#FDBB30] transition-all bg-white"
                                                placeholder="Kullanıcı adınızı giriniz"
                                                required
                                            />
                                            <span className="absolute right-3.5 top-1/2 -translate-y-1/2 text-gray-400 text-sm select-none">👤</span>
                                        </div>
                                    </div>
                                    <div>
                                        <div className="flex justify-between items-center mb-2">
                                            <label className="block text-xs font-bold text-[#718096] uppercase tracking-wider">
                                                Şifreniz
                                            </label>
                                        </div>
                                        <div className="relative">
                                            <input
                                                type="password"
                                                value={password}
                                                onChange={(e) => setPassword(e.target.value)}
                                                className="w-full pl-4 pr-10 py-3.5 border border-[#E4E7EB] rounded-xl text-sm font-semibold text-[#111] focus:outline-none focus:ring-2 focus:ring-[#FDBB30]/30 focus:border-[#FDBB30] transition-all bg-white"
                                                placeholder="Şifrenizi giriniz"
                                                required
                                            />
                                            <span className="absolute right-3.5 top-1/2 -translate-y-1/2 text-gray-400 text-sm select-none">🔑</span>
                                        </div>
                                    </div>
                                    <button
                                        type="submit"
                                        disabled={loading}
                                        className="w-full bg-[#111] hover:bg-[#222] text-white font-bold py-3.5 rounded-xl transition-all text-sm tracking-wider uppercase cursor-pointer shadow-md disabled:opacity-50"
                                    >
                                        {loading ? 'GİRİŞ YAPILIYOR...' : 'GİRİŞ YAP'}
                                    </button>
                                </form>
                            ) : (
                                /* KAYIT FORMU */
                                <form onSubmit={handleRegisterSubmit} className="space-y-5">
                                    <div>
                                        <label className="block text-xs font-bold text-[#718096] uppercase tracking-wider mb-2">
                                            Kullanıcı Adı
                                        </label>
                                        <input
                                            type="text"
                                            value={username}
                                            onChange={(e) => setUsername(e.target.value)}
                                            className="w-full px-4 py-3.5 border border-[#E4E7EB] rounded-xl text-sm font-semibold text-[#111] focus:outline-none focus:ring-2 focus:ring-[#FDBB30]/30 focus:border-[#FDBB30] transition-all bg-white"
                                            placeholder="Kullanıcı adı girin"
                                            required
                                        />
                                    </div>
                                    <div>
                                        <label className="block text-xs font-bold text-[#718096] uppercase tracking-wider mb-2">
                                            E-Posta
                                        </label>
                                        <input
                                            type="email"
                                            value={email}
                                            onChange={(e) => setEmail(e.target.value)}
                                            className="w-full px-4 py-3.5 border border-[#E4E7EB] rounded-xl text-sm font-semibold text-[#111] focus:outline-none focus:ring-2 focus:ring-[#FDBB30]/30 focus:border-[#FDBB30] transition-all bg-white"
                                            placeholder="ornek@mail.com"
                                            required
                                        />
                                    </div>
                                    <div>
                                        <label className="block text-xs font-bold text-[#718096] uppercase tracking-wider mb-2">
                                            Şifre
                                        </label>
                                        <input
                                            type="password"
                                            value={password}
                                            onChange={(e) => setPassword(e.target.value)}
                                            className="w-full px-4 py-3.5 border border-[#E4E7EB] rounded-xl text-sm font-semibold text-[#111] focus:outline-none focus:ring-2 focus:ring-[#FDBB30]/30 focus:border-[#FDBB30] transition-all bg-white"
                                            placeholder="••••••••"
                                            required
                                        />
                                    </div>
                                    <div>
                                        <label className="block text-xs font-bold text-[#718096] uppercase tracking-wider mb-2">
                                            Yetki Rolü
                                        </label>
                                        <select
                                            value={role}
                                            onChange={(e) => setRole(Number(e.target.value))}
                                            className="w-full px-4 py-3.5 border border-[#E4E7EB] bg-white rounded-xl text-sm font-semibold text-[#111] focus:outline-none focus:ring-2 focus:ring-[#FDBB30]/30 focus:border-[#FDBB30] transition-all"
                                        >
                                            <option value={3}>Analist (Rol 3)</option>
                                            <option value={2}>Karar Mekanizması (Rol 2)</option>
                                            <option value={1}>Sistem Yöneticisi / Admin (Rol 1)</option>
                                        </select>
                                    </div>
                                    <button
                                        type="submit"
                                        disabled={loading}
                                        className="w-full bg-[#111] hover:bg-[#222] text-white font-bold py-3.5 rounded-xl transition-all text-sm tracking-wider uppercase cursor-pointer shadow-md disabled:opacity-50"
                                    >
                                        {loading ? 'KAYIT YAPILIYOR...' : 'KAYIT OL'}
                                    </button>
                                </form>
                            )}
                        </div>
                    </div>
                </div>
            </main>

            {/* Alt Kısım (Güvenlik / Proje Tanıtım Footerı) */}
            <footer className="bg-[#FFF9E6] border-t border-[#FEEEC3] py-8 px-6 md:px-12 flex justify-center shadow-inner">
                <div className="w-full max-w-4xl bg-white/70 border border-[#FEEEC3] p-6 rounded-2xl flex flex-col md:flex-row gap-6 items-center md:items-start">
                    {/* Sarı Güvenlik İkonu */}
                    <div className="bg-[#FDBB30]/10 border border-[#FDBB30]/20 p-4 rounded-full text-2xl flex items-center justify-center select-none shadow-sm">
                        🛡️
                    </div>
                    <div className="flex-1 space-y-3">
                        <h3 className="text-sm font-bold text-[#8F6A0F] uppercase tracking-wide">
                            Güvenlik & Proje Bilgilendirmesi
                        </h3>
                        <p className="text-xs text-[#7A6128] leading-relaxed font-medium">
                            Merhaba! Ben Oğuz Kaan Yorulmaz. Konya Teknik Üniversitesi Yazılım Mühendisliği bölümü öğrencisiyim. 
                            Şu an <strong>VakıfBank</strong> bünyesinde, değerli mentorum <strong>Sıla Şirin İĞDE'nin</strong> rehberliğinde stajımı yapmaktayım. 
                            Modern web teknolojileri, temiz kod mimarisi (Clean Architecture) ve yazılım pratikleri üzerine yoğunlaşarak kendimi geliştiriyorum.
                        </p>
                        <p className="text-xs text-[#7A6128] leading-relaxed font-medium">
                            İncelemekte olduğunuz <strong>FraudGuard</strong> platformu; staj dönemimde Sıla Şirin İĞDE'nin mentorluğunda, <strong>.NET 8 (C#)</strong>, 
                            <strong> Redis</strong> ve <strong>SQL Server</strong> veritabanı altyapıları ile 
                            <strong> React (Vite + TypeScript)</strong> frontend kütüphanesi kullanılarak Clean Architecture ve Domain-Driven Design (DDD) 
                            prensiplerine uygun olarak geliştirdiğim Fraud Tespit ve Yönetim (Fraud Management & Detection) projemdir.
                        </p>
                    </div>
                </div>
            </footer>
        </div>
    );
};
