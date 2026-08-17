// src/styles/theme.ts

export const theme = {
    colors: {
        primary: "#FDBB30",          // VakıfBank Sarısı
        primaryHover: "#E5A520",
        black: "#111111",            // Siyah Buton ve Metin Rengi
        white: "#FFFFFF",
        bg: "#FFFFFF",               // Temiz Beyaz Arka Plan
        card: "#FFFFFF",             // Beyaz Kartlar
        border: "#E4E7EB",           // İnce Sınırlar
        text: "#1A1D20",
        muted: "#718096",
        success: "#16A34A",
        danger: "#DC2626"
    },

    styles: {
        body: "w-full max-w-full overflow-x-hidden relative min-h-screen bg-white text-[#1A1D20] font-sans flex flex-col antialiased selection:bg-[#FDBB30]/30 selection:text-[#111]",
        card: "relative overflow-hidden bg-white border border-[#E4E7EB] rounded-xl p-3 md:p-6 shadow-sm transition-all duration-300 hover:shadow-md",
        cardTitle: "text-[9px] md:text-xs font-bold tracking-wider md:tracking-widest text-[#718096] uppercase",
        cardValue: "text-lg md:text-3xl font-black text-[#111] mt-1 md:mt-2",
        filterSection: "bg-white border border-[#E4E7EB] rounded-xl p-3 md:p-5 shadow-sm",
        tabContainer: "grid grid-cols-3 gap-1 w-full border-b border-[#E4E7EB] pb-0 md:flex md:gap-5 md:w-auto select-none",
        tabActive: "px-1 md:px-2 py-2 md:py-3 text-[10px] md:text-sm font-bold text-[#111] border-b-4 border-[#FDBB30] transition-all whitespace-nowrap flex items-center justify-center text-center",
        tabInactive: "px-1 md:px-2 py-2 md:py-3 text-[10px] md:text-sm font-semibold text-[#718096] hover:text-[#111] border-b-4 border-transparent transition-all whitespace-nowrap flex items-center justify-center text-center",
        select: "bg-white border border-[#C5CBD3] rounded-lg px-4 py-2 text-[#1A1D20] focus:border-[#FDBB30] focus:ring-2 focus:ring-[#FDBB30]/20 transition-all text-sm outline-none",
        input: "bg-white border border-[#C5CBD3] rounded-lg px-4 py-2 text-[#1A1D20] placeholder-[#718096] focus:border-[#FDBB30] focus:ring-2 focus:ring-[#FDBB30]/20 transition-all text-sm outline-none w-64",
        outlineButton: "bg-white border border-[#FDBB30] text-[#111] rounded-lg px-5 py-2 font-semibold hover:bg-[#FDBB30]/10 transition-all text-sm",
        blackButton: "bg-[#111111] text-white rounded-lg px-5 py-2 font-semibold hover:bg-black transition-all text-sm",
        primaryButton: "bg-[#FDBB30] text-[#111] rounded-lg px-5 py-2 font-bold hover:bg-[#E5A520] transition-all text-sm"
    },

    // Risk kademelerinin görsel karşılığı. Kademeler RiskScore value object'inden gelir;
    // eşik değerleri orada tanımlıdır (backend RiskScoringConstants ile hizalı).
    riskTier: {
        NORMAL: {
            text: "text-emerald-600",
            bar: "bg-emerald-500",
            badge: "bg-emerald-500/10 text-emerald-700 border-emerald-500/20"
        },
        IZLE: {
            text: "text-amber-500",
            bar: "bg-amber-500",
            badge: "bg-amber-500/10 text-amber-600 border-amber-500/20"
        },
        EK_DOGRULAMA: {
            text: "text-orange-500",
            bar: "bg-orange-500",
            badge: "bg-orange-500/10 text-orange-600 border-orange-500/20"
        },
        RET_BLOKE: {
            text: "text-red-600",
            bar: "bg-red-600",
            badge: "bg-red-500/10 text-red-600 border-red-500/20"
        }
    }
};
