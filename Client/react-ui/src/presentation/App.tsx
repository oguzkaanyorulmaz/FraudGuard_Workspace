import { useState } from 'react';
import type { Transaction } from '../domain/entities/Transaction';
import { Header } from './components/layout/Header';
import { TransactionList } from './components/dashboard/TransactionList';
import { BulkActionBar } from './components/dashboard/BulkActionBar';
import { ActionModal } from './components/shared/ActionModal';
import { TransactionDetailsSidebar } from './components/dashboard/TransactionDetailsSidebar';
import { useTransactions } from './hooks/useTransactions';
import { useSelection } from './hooks/useSelection';
import { theme } from './styles/theme';

/* bg-[#F4F5F7] text-[#1A1D20] selection:bg-[#FFCB05] bg-white border-[#E4E7EB] shadow-sm 
  text-xs text-[#718096] text-4xl text-[#111111] bg-[#E4E7EB] bg-[#111111] 
  text-white border-[#C5CBD3] focus:ring-[#FFCB05]/20 
*/



export default function App() {
    const { transactions, history, loading, handleApprove, handleBlock, handleBulkBlock, handleBulkApprove } = useTransactions();
    const { selectedIds, toggleSelection, selectAll, clearSelection } = useSelection();

    // Arayüz Durumları (State)
    const [activeTab, setActiveTab] = useState<'PENDING' | 'HISTORY'>('PENDING');
    const [searchTerm, setSearchTerm] = useState('');
    const [riskFilter, setRiskFilter] = useState('ALL');


    // Modal ve Sidebar Durumları
    const [isModalOpen, setIsModalOpen] = useState(false);
    const [actionType, setActionType] = useState<'APPROVE' | 'BLOCK' | 'BULK_BLOCK' | 'BULK_APPROVE' | null>(null);
    const [selectedTxnId, setSelectedTxnId] = useState<string | null>(null);
    const [sidebarTxn, setSidebarTxn] = useState<Transaction | null>(null);

    const openApproveModal = (id: string) => { setSelectedTxnId(id); setActionType('APPROVE'); setIsModalOpen(true); };
    const openBlockModal = (id: string) => { setSelectedTxnId(id); setActionType('BLOCK'); setIsModalOpen(true); };
    const openBulkBlockModal = () => { setActionType('BULK_BLOCK'); setIsModalOpen(true); };
    const openBulkApproveModal = () => { setActionType('BULK_APPROVE'); setIsModalOpen(true); };

    const handleModalConfirm = (id: string, reason: string, blockReasonId?: number, analystName?: string) => {
        if (actionType === 'APPROVE') {
            handleApprove(id, reason, analystName);
        }
        else if (actionType === 'BLOCK') {
            handleBlock(id, reason, blockReasonId, analystName);
        }
        else if (actionType === 'BULK_BLOCK') {
            handleBulkBlock(selectedIds, reason);
            clearSelection();
        }
        else if (actionType === 'BULK_APPROVE') {
            handleBulkApprove(selectedIds, reason, analystName);
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
            {/* Maksimum genişlik, ortalama (mx-auto) ve konforlu dolgu (px ve py) eklenmiştir */}
            <div className="max-w-7xl mx-auto w-full px-4 py-6 md:px-8 flex-1 transition-all duration-300">
                <Header />

                {/* Üst İstatistik Kartları */}
                <div className="grid grid-cols-1 md:grid-cols-3 gap-5 mb-6">
                    {/* 1. Bekleyen İşlemler - Siyah Çizgili & Siyah Yazılı */}
                    <div className={theme.styles.card}>
                        <div className="absolute top-0 left-0 w-full h-1 bg-[#111111]"></div>
                        <div className={theme.styles.cardTitle}>⬛ Bekleyen Şüpheli İşlem</div>
                        <div className="flex items-baseline gap-2 mt-2">
                            <span className="text-3xl font-black text-black">{transactions.length}</span>
                            <span className="text-xs text-red-500 font-bold animate-pulse">(Canlı)</span>
                        </div>
                    </div>
                    {/* 2. Blokelenen İşlemler - Kırmızı Çizgili & Kırmızı Yazılı */}
                    <div className={theme.styles.card}>
                        <div className="absolute top-0 left-0 w-full h-1 bg-red-500"></div>
                        <div className={theme.styles.cardTitle}>🚫 Blokelenen İşlemler</div>
                        <div className="text-3xl font-black text-red-600 mt-2">{history.filter(h => h.action === 'BLOCKED').length}</div>
                    </div>
                    {/* 3. Onaylanan İşlemler - Yeşil Çizgili & Yeşil Yazılı */}
                    <div className={theme.styles.card}>
                        <div className="absolute top-0 left-0 w-full h-1 bg-emerald-500"></div>
                        <div className={theme.styles.cardTitle}>✅ Onaylanan İşlemler</div>
                        <div className="text-3xl font-black text-emerald-600 mt-2">{history.filter(h => h.action === 'APPROVED').length}</div>
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

                    <BulkActionBar selectedCount={selectedIds.length} onBulkBlock={openBulkBlockModal} onClear={clearSelection} onBulkApprove={openBulkApproveModal} />
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

            <ActionModal
                isOpen={isModalOpen}
                actionType={actionType === 'BULK_APPROVE' ? 'APPROVE' : (actionType === 'BULK_BLOCK' ? 'BLOCK' : actionType)}
                transactionId={actionType === 'BULK_APPROVE' || actionType === 'BULK_BLOCK' ? `${selectedIds.length} Adet İşlem` : selectedTxnId}
                onConfirm={handleModalConfirm}
                onCancel={() => setIsModalOpen(false)}
            />
            {/* Yan Panel */}
            <TransactionDetailsSidebar transaction={sidebarTxn} isOpen={!!sidebarTxn} onClose={() => setSidebarTxn(null)} />
        </div>
    );
}