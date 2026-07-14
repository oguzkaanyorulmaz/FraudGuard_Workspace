// src/styles/theme.ts

export const theme = {
    colors: {
        primary: "#FFC72C",          // VakıfBank Sarısı
        primaryHover: "#F4B400",
        black: "#1B1B1B",
        white: "#FFFFFF",

        bg: "#F8F8F8",
        card: "#FFFFFF",

        border: "#E7E7E7",

        text: "#222222",
        muted: "#707070",

        success: "#16A34A",
        danger: "#DC2626"
    },

    styles: {

        body:
            "min-h-screen bg-[#F8F8F8] text-[#222] font-sans flex overflow-hidden",

        card:
            "bg-white border border-[#E7E7E7] rounded-xl shadow-sm transition-all duration-300 hover:shadow-lg",

        cardTitle:
            "uppercase text-xs font-bold tracking-wider text-[#6F6F6F]",

        cardValue:
            "text-4xl font-black text-[#222]",

        filterSection:
            "bg-white border border-[#E7E7E7] rounded-xl p-5 shadow-sm",

        tabContainer:
            "flex gap-2",

        tabActive:
            "px-6 py-3 rounded-lg bg-[#FFC72C] text-[#111] font-bold border border-[#FFC72C]",

        tabInactive:
            "px-6 py-3 rounded-lg bg-white text-[#222] border border-[#FFC72C] hover:bg-[#FFF6D8] transition",

        select:
            "bg-white border border-[#D8D8D8] rounded-lg px-4 py-2 text-[#222] focus:border-[#FFC72C] focus:ring-2 focus:ring-[#FFC72C]/20",

        input:
            "bg-white border border-[#D8D8D8] rounded-lg px-4 py-2 text-[#222] focus:border-[#FFC72C] focus:ring-2 focus:ring-[#FFC72C]/20",

        /** Sarı kenarlıklı buton (Müşteri Ol tarzı) */

        outlineButton:
            "bg-white border-2 border-[#FFC72C] text-[#111] rounded-lg px-5 py-2 font-semibold hover:bg-[#FFF8DA] transition",

        /** Siyah buton (İnternet Bankacılığı tarzı) */

        blackButton:
            "bg-[#111111] text-white rounded-lg px-5 py-2 font-semibold hover:bg-black transition",

        /** Sarı buton */

        primaryButton:
            "bg-[#FFC72C] text-[#111] rounded-lg px-5 py-2 font-bold hover:bg-[#F4B400] transition"
    }
};