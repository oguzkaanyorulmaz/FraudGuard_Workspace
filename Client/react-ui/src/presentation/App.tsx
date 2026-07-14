import React, { useState } from 'react';
import type { Transaction } from '../domain/entities/Transaction';
import { Header } from './components/layout/Header';
import { TransactionList } from './components/dashboard/TransactionList';
import { BulkActionBar } from './components/dashboard/BulkActionBar';
import { ActionModal } from './components/shared/ActionModal';
import { TransactionDetailsSidebar } from './components/dashboard/TransactionDetailsSidebar';
import { useTransactions } from './hooks/useTransactions';
import { useSelection } from './hooks/useSelection';
import { theme } from './styles/theme'; // Yeni eklenen tema importu

// Tailwind JIT'in sınıfları tanıması için (Bu sınıflar aslında theme.ts içindekiler)
/* bg-[#F4F5F7] text-[#1A1D20] selection:bg-[#FFCB05] bg-white border-[#E4E7EB] shadow-sm 
  text-xs text-[#718096] text-4xl text-[#111111] bg-[#E4E7EB] bg-[#111111] 
  text-white border-[#C5CBD3] focus:ring-[#FFCB05]/20 
*/

// Bu satır Tailwind'in tüm theme.ts sınıflarını build'e dahil etmesini zorunlu kılar
const _tailwindUsage = [
    theme.styles.body,
    theme.styles.card,
    theme.styles.filterSection,
    theme.styles.select,
    theme.styles.input
];

export default function App() {
    const { transactions, history, loading, handleApprove, handleBlock, handleBulkBlock } = useTransactions();
    const { selectedIds, toggleSelection, selectAll, clearSelection } = useSelection();

    // Arayüz Durumları (State)
    const [activeTab, setActiveTab] = useState<'PENDING' | 'HISTORY'>('PENDING');
    const [searchTerm, setSearchTerm] = useState('');
    const [riskFilter, setRiskFilter] = useState('ALL');

    // Modal ve Sidebar Durumları
    const [isModalOpen, setIsModalOpen] = useState(false);
    const [actionType, setActionType] = useState<'APPROVE' | 'BLOCK' | 'BULK_BLOCK' | null>(null);
    const [selectedTxnId, setSelectedTxnId] = useState<string | null>(null);
    const [sidebarTxn, setSidebarTxn] = useState<Transaction | null>(null);

    const openApproveModal = (id: string) => { setSelectedTxnId(id); setActionType('APPROVE'); setIsModalOpen(true); };
    const openBlockModal = (id: string) => { setSelectedTxnId(id); setActionType('BLOCK'); setIsModalOpen(true); };
    const openBulkBlockModal = () => { setActionType('BULK_BLOCK'); setIsModalOpen(true); };

    // DİKKAT: 3. parametre olan blockReasonId'yi buraya ekledik
    const handleModalConfirm = (id: string, reason: string, blockReasonId?: number) => {
        if (actionType === 'APPROVE') {
            handleApprove(id, reason);
        }
        else if (actionType === 'BLOCK') {
            // Modal'dan gelen o ID'yi nihayet handleBlock fonksiyonuna paslıyoruz!
            handleBlock(id, reason, blockReasonId); 
        }
        else if (actionType === 'BULK_BLOCK') { 
            // Eğer istersen ileride toplu bloklamada da aynı combobox'ı kullanabilirsin
            handleBulkBlock(selectedIds, reason); 
            clearSelection(); 
        }
        
        setIsModalOpen(false); 
        setSelectedTxnId(null); 
        setActionType(null);
    };

    // Filtreleme Mantığı
    const getFilteredData = () => {
        const sourceData = activeTab === 'PENDING'
            ? transactions.map(t => ({ transaction: t }))
            : history.map(h => ({ transaction: h.transaction, historyAction: h.action }));

        return sourceData.filter(item => {
            const txn = item.transaction;
            if (searchTerm && !txn.id.includes(searchTerm) && !txn.maskedCard.includes(searchTerm)) return false;
            if (riskFilter !== 'ALL') {
                const isHigh = txn.riskScore >= 70;
                if (riskFilter === 'HIGH' && !isHigh) return false;
                if (riskFilter === 'MEDIUM' && isHigh) return false;
            }
            return true;
        });
    };

    const filteredData = getFilteredData();

    return (
        <div className={theme.styles.body}>
            <div className="flex-1 transition-all duration-300">
                <Header />

                {/* Üst İstatistik Kartları */}
                <div className="grid grid-cols-1 md:grid-cols-3 gap-5 mb-6">
                    <div className={theme.styles.card}>
                        <div className="absolute top-0 left-0 w-full h-1 bg-red-500"></div>
                        <div className={theme.styles.cardTitle}>🟥 Bekleyen Şüpheli İşlem</div>
                        <div className="flex items-baseline gap-2 mt-2">
                            <span className={theme.styles.cardValue}>{transactions.length}</span>
                            <span className="text-xs text-red-500 font-bold animate-pulse">(Canlı)</span>
                        </div>
                    </div>
                    <div className={theme.styles.card}>
                        <div className="absolute top-0 left-0 w-full h-1 bg-[#111111]"></div>
                        <div className={theme.styles.cardTitle}>🚫 Blokelenen İşlemler</div>
                        <div className={theme.styles.cardValue}>{history.filter(h => h.action === 'BLOCKED').length}</div>
                    </div>
                    <div className={theme.styles.card}>
                        <div className="absolute top-0 left-0 w-full h-1 bg-[#FFCB05]"></div>
                        <div className={theme.styles.cardTitle}>✅ Onaylanan İşlemler</div>
                        <div className={theme.styles.cardValue}>{history.filter(h => h.action === 'APPROVED').length}</div>
                    </div>
                </div>

                {/* Filtreleme ve Tab Alanı */}
                <div className={theme.styles.filterSection}>
                    <div className="flex flex-wrap justify-between items-center gap-4">
                        <div className={theme.styles.tabContainer}>
                            <button
                                onClick={() => { setActiveTab('PENDING'); clearSelection(); }}
                                className={activeTab === 'PENDING' ? theme.styles.tabActive : theme.styles.tabInactive}
                            >
                                📂 Bekleyen ({transactions.length})
                            </button>
                            <button
                                onClick={() => { setActiveTab('HISTORY'); clearSelection(); }}
                                className={activeTab === 'HISTORY' ? theme.styles.tabActive : theme.styles.tabInactive}
                            >
                                🗄️ Geçmiş İşlemler ({history.length})
                            </button>
                        </div>

                        {/* Gelişmiş Filtreler */}
                        <div className="flex gap-3 text-sm">
                            <select
                                value={riskFilter}
                                onChange={(e) => setRiskFilter(e.target.value)}
                                className={theme.styles.select}
                            >
                                <option value="ALL">Tüm Risk Seviyeleri</option>
                                <option value="HIGH">Sadece Yüksek Risk (70+)</option>
                                <option value="MEDIUM">Orta Risk (&lt;70)</option>
                            </select>
                            <input
                                type="text"
                                placeholder="🔍 ID veya Kart Ara..."
                                value={searchTerm}
                                onChange={(e) => setSearchTerm(e.target.value)}
                                className={theme.styles.input}
                            />
                        </div>
                    </div>

                    <BulkActionBar selectedCount={selectedIds.length} onBulkBlock={openBulkBlockModal} onClear={clearSelection} />
                </div>

                <TransactionList
                    transactions={filteredData}
                    loading={loading}
                    isHistoryView={activeTab === 'HISTORY'}
                    selectedIds={selectedIds}
                    onToggleSelection={toggleSelection}
                    onSelectAll={selectAll}
                    onApprove={openApproveModal}
                    onBlock={openBlockModal}
                    onViewDetails={setSidebarTxn}
                />
            </div>

            <ActionModal isOpen={isModalOpen} actionType={actionType === 'BULK_BLOCK' ? 'BLOCK' : actionType} transactionId={actionType === 'BULK_BLOCK' ? `${selectedIds.length} Adet İşlem` : selectedTxnId} onConfirm={handleModalConfirm} onCancel={() => setIsModalOpen(false)} />

            {/* Yan Panel */}
            <TransactionDetailsSidebar transaction={sidebarTxn} isOpen={!!sidebarTxn} onClose={() => setSidebarTxn(null)} />
        </div>
    );
}