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
            // Şifreyi temizleyelim
            setPassword('');
        } else {
            setError('Kayıt işlemi başarısız. Kullanıcı adı zaten alınmış olabilir.');
        }
        setLoading(false);
    };

    return (
        <div className="min-h-screen bg-[#F4F5F7] flex items-center justify-center">
            <div className="bg-white rounded-2xl shadow-xl border border-[#E4E7EB] p-8 w-full max-w-md">
                {/* Logo / Başlık */}
                <div className="text-center mb-8">
                    <div className="inline-flex items-center gap-2 mb-4">
                        <span className="bg-[#FFC72C] text-[#111] font-black italic text-xl px-2.5 py-0.5 rounded shadow-sm">FG</span>
                        <span className="text-xl font-bold tracking-tight text-[#111]">FraudGuard</span>
                    </div>
                    <p className="text-sm text-[#718096]">
                        {isRegisterMode ? 'Yeni Yetkili Hesabı Oluşturun' : 'Fraud Yönetim Paneline Giriş Yapın'}
                    </p>
                </div>

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

                {/* Giriş Modu */}
                {!isRegisterMode ? (
                    <form onSubmit={handleLoginSubmit} className="space-y-5">
                        <div>
                            <label className="block text-[10px] font-bold text-[#718096] uppercase tracking-wider mb-2">
                                Kullanıcı Adı
                            </label>
                            <input
                                type="text"
                                value={username}
                                onChange={(e) => setUsername(e.target.value)}
                                className="w-full px-4 py-3 border border-[#E4E7EB] rounded-xl text-sm font-semibold text-[#111] focus:outline-none focus:ring-2 focus:ring-[#FFC72C]/30 focus:border-[#FFC72C] transition-all"
                                placeholder="Kullanıcı Adı"
                                required
                            />
                        </div>
                        <div>
                            <label className="block text-[10px] font-bold text-[#718096] uppercase tracking-wider mb-2">
                                Şifre
                            </label>
                            <input
                                type="password"
                                value={password}
                                onChange={(e) => setPassword(e.target.value)}
                                className="w-full px-4 py-3 border border-[#E4E7EB] rounded-xl text-sm font-semibold text-[#111] focus:outline-none focus:ring-2 focus:ring-[#FFC72C]/30 focus:border-[#FFC72C] transition-all"
                                placeholder="••••••••"
                                required
                            />
                        </div>
                        <button
                            type="submit"
                            disabled={loading}
                            className="w-full bg-[#FFC72C] hover:bg-[#E5B224] text-[#111] font-bold py-3 rounded-xl transition-all text-sm disabled:opacity-50 cursor-pointer"
                        >
                            {loading ? 'Giriş yapılıyor...' : 'Giriş Yap'}
                        </button>
                        <p className="text-center text-xs text-[#718096] mt-4">
                            Hesabınız yok mu?{' '}
                            <button
                                type="button"
                                onClick={() => {
                                    setIsRegisterMode(true);
                                    setError('');
                                    setSuccessMessage('');
                                }}
                                className="text-blue-600 font-bold hover:underline"
                            >
                                Kayıt Olun
                            </button>
                        </p>
                    </form>
                ) : (
                    // Kayıt Modu
                    <form onSubmit={handleRegisterSubmit} className="space-y-4">
                        <div>
                            <label className="block text-[10px] font-bold text-[#718096] uppercase tracking-wider mb-2">
                                Kullanıcı Adı
                            </label>
                            <input
                                type="text"
                                value={username}
                                onChange={(e) => setUsername(e.target.value)}
                                className="w-full px-4 py-3 border border-[#E4E7EB] rounded-xl text-sm font-semibold text-[#111] focus:outline-none focus:ring-2 focus:ring-[#FFC72C]/30 focus:border-[#FFC72C] transition-all"
                                placeholder="Kullanıcı adı girin"
                                required
                            />
                        </div>
                        <div>
                            <label className="block text-[10px] font-bold text-[#718096] uppercase tracking-wider mb-2">
                                E-Posta
                            </label>
                            <input
                                type="email"
                                value={email}
                                onChange={(e) => setEmail(e.target.value)}
                                className="w-full px-4 py-3 border border-[#E4E7EB] rounded-xl text-sm font-semibold text-[#111] focus:outline-none focus:ring-2 focus:ring-[#FFC72C]/30 focus:border-[#FFC72C] transition-all"
                                placeholder="ornek@mail.com"
                                required
                            />
                        </div>
                        <div>
                            <label className="block text-[10px] font-bold text-[#718096] uppercase tracking-wider mb-2">
                                Şifre
                            </label>
                            <input
                                type="password"
                                value={password}
                                onChange={(e) => setPassword(e.target.value)}
                                className="w-full px-4 py-3 border border-[#E4E7EB] rounded-xl text-sm font-semibold text-[#111] focus:outline-none focus:ring-2 focus:ring-[#FFC72C]/30 focus:border-[#FFC72C] transition-all"
                                placeholder="••••••••"
                                required
                            />
                        </div>
                        <div>
                            <label className="block text-[10px] font-bold text-[#718096] uppercase tracking-wider mb-2">
                                Yetki Rolü
                            </label>
                            <select
                                value={role}
                                onChange={(e) => setRole(Number(e.target.value))}
                                className="w-full px-4 py-3 border border-[#E4E7EB] bg-white rounded-xl text-sm font-semibold text-[#111] focus:outline-none focus:ring-2 focus:ring-[#FFC72C]/30 focus:border-[#FFC72C] transition-all"
                            >
                                <option value={1}>Admin (Rol 1)</option>
                                <option value={2}>Karar Mekanizması (Rol 2)</option>
                                <option value={3}>Analist (Rol 3)</option>
                            </select>
                        </div>
                        <button
                            type="submit"
                            disabled={loading}
                            className="w-full bg-[#FFC72C] hover:bg-[#E5B224] text-[#111] font-bold py-3 rounded-xl transition-all text-sm disabled:opacity-50 cursor-pointer"
                        >
                            {loading ? 'Kayıt yapılıyor...' : 'Kayıt Ol'}
                        </button>
                        <p className="text-center text-xs text-[#718096] mt-4">
                            Zaten hesabınız var mı?{' '}
                            <button
                                type="button"
                                onClick={() => {
                                    setIsRegisterMode(false);
                                    setError('');
                                    setSuccessMessage('');
                                }}
                                className="text-blue-600 font-bold hover:underline"
                            >
                                Giriş Yapın
                            </button>
                        </p>
                    </form>
                )}


            </div>
        </div>
    );
};
