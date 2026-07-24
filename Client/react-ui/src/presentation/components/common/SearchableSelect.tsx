import React, { useState, useRef, useEffect } from 'react';

interface Option {
    value: string;
    label: string;
}

interface Props {
    options: Option[];
    value: string;
    onChange: (value: string) => void;
    placeholder: string;
    className?: string;
    alignRight?: boolean;
}

export const SearchableSelect: React.FC<Props> = ({
    options,
    value,
    onChange,
    placeholder,
    className = "",
    alignRight = false
}) => {
    const [isOpen, setIsOpen] = useState(false);
    const containerRef = useRef<HTMLDivElement>(null);

    // Click outside to close
    useEffect(() => {
        const handleClickOutside = (event: MouseEvent) => {
            if (containerRef.current && !containerRef.current.contains(event.target as Node)) {
                setIsOpen(false);
            }
        };
        document.addEventListener("mousedown", handleClickOutside);
        return () => document.removeEventListener("mousedown", handleClickOutside);
    }, []);

    const selectedOption = options.find(opt => opt.value === value);

    return (
        <div ref={containerRef} className={`relative select-none ${className}`}>
            {/* Trigger Button */}
            <div
                onClick={() => setIsOpen(!isOpen)}
                className="flex items-center justify-between bg-white border border-[#C5CBD3] rounded-lg px-4 py-2 text-[#1A1D20] text-xs md:text-sm font-semibold cursor-pointer shadow-sm hover:border-gray-400 transition-all select-none h-[38px]"
            >
                <span className="truncate">
                    {selectedOption ? selectedOption.label : placeholder}
                </span>
                <svg
                    className={`w-4 h-4 text-slate-500 transition-transform duration-300 ${isOpen ? 'rotate-180' : ''}`}
                    fill="none"
                    stroke="currentColor"
                    viewBox="0 0 24 24"
                >
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" />
                </svg>
            </div>

            {/* Dropdown Panel */}
            <div
                className={`absolute ${alignRight ? 'right-0 left-auto' : 'left-0'} mt-1.5 w-[240px] md:w-[280px] bg-white border border-[#C5CBD3] rounded-xl shadow-xl z-50 transition-all duration-200 origin-top transform ${
                    isOpen 
                        ? 'opacity-100 scale-100 translate-y-0 pointer-events-auto' 
                        : 'opacity-0 scale-95 -translate-y-2 pointer-events-none'
                }`}
            >
                {/* Options List */}
                <div className="max-h-60 overflow-y-auto p-1.5 scrollbar-thin scrollbar-thumb-slate-200">
                    {options.map(option => {
                        const isSelected = option.value === value;
                        return (
                            <div
                                key={option.value}
                                onClick={() => {
                                    onChange(option.value);
                                    setIsOpen(false);
                                }}
                                className={`flex items-center justify-between px-3 py-2 rounded-lg text-xs md:text-sm cursor-pointer transition-colors ${
                                    isSelected
                                        ? 'bg-[#FDBB30]/10 text-[#111] font-bold border-l-4 border-[#FDBB30]'
                                        : 'text-[#1A1D20] hover:bg-slate-50 font-medium'
                                }`}
                            >
                                <span className="truncate pr-2">{option.label}</span>
                                {isSelected && (
                                    <svg className="w-4 h-4 text-[#FDBB30] flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={3} d="M5 13l4 4L19 7" />
                                    </svg>
                                )}
                            </div>
                        );
                    })}
                </div>
            </div>
        </div>
    );
};
