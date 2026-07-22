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
        body: "min-h-screen bg-white text-[#1A1D20] font-sans flex flex-col antialiased selection:bg-[#FDBB30]/30 selection:text-[#111]",
        card: "relative overflow-hidden bg-white border border-[#E4E7EB] rounded-xl p-6 shadow-sm transition-all duration-300 hover:shadow-md",
        cardTitle: "text-xs font-bold tracking-widest text-[#718096] uppercase",
        cardValue: "text-3xl font-black text-[#111] mt-2",
        filterSection: "bg-white border border-[#E4E7EB] rounded-xl p-5 shadow-sm",
        tabContainer: "flex gap-5 border-b border-[#E4E7EB] pb-0",
        tabActive: "px-2 py-3 text-sm font-bold text-[#111] border-b-4 border-[#FDBB30] transition-all relative top-[1px]",
        tabInactive: "px-2 py-3 text-sm font-semibold text-[#718096] hover:text-[#111] transition-all",
        select: "bg-white border border-[#C5CBD3] rounded-lg px-4 py-2 text-[#1A1D20] focus:border-[#FDBB30] focus:ring-2 focus:ring-[#FDBB30]/20 transition-all text-sm outline-none",
        input: "bg-white border border-[#C5CBD3] rounded-lg px-4 py-2 text-[#1A1D20] placeholder-[#718096] focus:border-[#FDBB30] focus:ring-2 focus:ring-[#FDBB30]/20 transition-all text-sm outline-none w-64",
        outlineButton: "bg-white border border-[#FDBB30] text-[#111] rounded-lg px-5 py-2 font-semibold hover:bg-[#FDBB30]/10 transition-all text-sm",
        blackButton: "bg-[#111111] text-white rounded-lg px-5 py-2 font-semibold hover:bg-black transition-all text-sm",
        primaryButton: "bg-[#FDBB30] text-[#111] rounded-lg px-5 py-2 font-bold hover:bg-[#E5A520] transition-all text-sm"
    }
};
