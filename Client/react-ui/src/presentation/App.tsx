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
import { AuthProvider, useAuth } from './contexts/AuthContext';
import { LoginPage } from './components/auth/LoginPage';


/* bg-[#F4F5F7] text-[#1A1D20] selection:bg-[#FFCB05] bg-white border-[#E4E7EB] shadow-sm 
  text-xs text-[#718096] text-4xl text-[#111111] bg-[#E4E7EB] bg-[#111111] 
  text-white border-[#C5CBD3] focus:ring-[#FFCB05]/20 
*/



function Dashboard() {
    const { transactions, history, loading, handleApprove, handleBlock, handleBulkBlock, handleBulkApprove } = useTransactions();
    const { selectedIds, toggleSelection, selectAll, clearSelection } = useSelection();

    // Arayüz Durumları (State)
    const [selectedTabs, setSelectedTabs] = useState<('PENDING' | 'BLOCKED' | 'APPROVED')[]>(['PENDING']);
    const [searchTerm, setSearchTerm] = useState('');
    const [selectedScenario, setSelectedScenario] = useState<string>('ALL');

    const [sortFields, setSortFields] = useState<{ field: string; direction: 'asc' | 'desc' }[]>([
        { field: 'date', direction: 'desc' }
    ]);

    const handleSort = (field: string) => {
        setSortFields(prev => {
            const isCompositeField = field === 'amount' || field === 'currency';

            if (!isCompositeField) {
                const existing = prev.find(s => s.field === field);
                if (existing) {
                    return [{ field, direction: existing.direction === 'asc' ? 'desc' as const : 'asc' as const }];
                }
                return [{ field, direction: 'desc' as const }];
            }

            let newSorts = prev.filter(s => s.field === 'amount' || s.field === 'currency');
            const existingIndex = newSorts.findIndex(s => s.field === field);
            if (existingIndex > -1) {
                const current = newSorts[existingIndex];
                const updated = {
                    field: current.field,
                    direction: (current.direction === 'asc' ? 'desc' : 'asc') as 'asc' | 'desc'
                };
                const nextSorts = [...newSorts];
                nextSorts[existingIndex] = updated;
                return nextSorts;
            } else {
                return [...newSorts, { field, direction: 'asc' as 'asc' | 'desc' }];
            }
        });
    };


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
            handleBulkBlock(selectedIds, reason, blockReasonId, analystName);
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

    const toggleTab = (tab: 'PENDING' | 'BLOCKED' | 'APPROVED') => {
        setSelectedTabs(prev => {
            if (prev.includes(tab)) {
                if (prev.length === 1) return prev; // En az bir tanesi seçili kalsın
                return prev.filter(t => t !== tab);
            } else {
                return [...prev, tab];
            }
        });
    };

    const handleTopCardClick = (tab: 'PENDING' | 'BLOCKED' | 'APPROVED') => {
        setSelectedTabs([tab]);
        setSelectedScenario('ALL');
        clearSelection();
    };

    const matchScenario = (txn: Transaction, scenario: string): boolean => {
        if (scenario === 'ALL') return true;
        return txn.ruleCode === scenario;
    };

    // Filtreleme ve Sıralama Mantığı
    const getFilteredData = () => {
        let sourceData: { transaction: Transaction; historyAction?: 'APPROVED' | 'BLOCKED' }[] = [];

        if (selectedTabs.includes('PENDING')) {
            sourceData = [...sourceData, ...transactions.map(t => ({ transaction: t }))];
        }
        if (selectedTabs.includes('BLOCKED')) {
            sourceData = [
                ...sourceData,
                ...history
                    .filter(h => h.action === 'BLOCKED')
                    .map(h => ({ transaction: h.transaction, historyAction: 'BLOCKED' as const }))
            ];
        }
        if (selectedTabs.includes('APPROVED')) {
            sourceData = [
                ...sourceData,
                ...history
                    .filter(h => h.action === 'APPROVED')
                    .map(h => ({ transaction: h.transaction, historyAction: 'APPROVED' as const }))
            ];
        }

        const filtered = sourceData.filter(item => {
            const txn = item.transaction;
            if (searchTerm && !txn.id.includes(searchTerm) && !txn.maskedCard.includes(searchTerm)) return false;
            if (!matchScenario(txn, selectedScenario)) return false;
            return true;
        });

        return [...filtered].sort((a, b) => {
            for (const sort of sortFields) {
                let valA: any;
                let valB: any;

                switch (sort.field) {
                    case 'riskScore':
                        valA = a.transaction.riskScore ?? 0;
                        valB = b.transaction.riskScore ?? 0;
                        break;
                    case 'transactionId':
                        valA = parseInt(a.transaction.transactionId || '0', 10);
                        valB = parseInt(b.transaction.transactionId || '0', 10);
                        break;
                    case 'amount':
                        valA = a.transaction.money?.getAmount() ?? 0;
                        valB = b.transaction.money?.getAmount() ?? 0;
                        break;
                    case 'currency':
                        valA = a.transaction.money?.getCurrency() || '';
                        valB = b.transaction.money?.getCurrency() || '';
                        break;
                    case 'date':
                        valA = new Date(a.transaction.date).getTime();
                        valB = new Date(b.transaction.date).getTime();
                        break;
                    case 'action':
                        valA = a.historyAction || 'PENDING';
                        valB = b.historyAction || 'PENDING';
                        break;
                    default:
                        continue;
                }

                if (valA < valB) return sort.direction === 'asc' ? -1 : 1;
                if (valA > valB) return sort.direction === 'asc' ? 1 : -1;
            }
            return 0;
        });
    };


    const filteredData = getFilteredData();

    const isPendingActive = selectedTabs.length === 1 && selectedTabs.includes('PENDING');
    const isBlockedActive = selectedTabs.length === 1 && selectedTabs.includes('BLOCKED');
    const isApprovedActive = selectedTabs.length === 1 && selectedTabs.includes('APPROVED');

    return (
        <div className={theme.styles.body}>
            {/* Maksimum genişlik, ortalama (mx-auto) ve konforlu dolgu (px ve py) eklenmiştir */}
            <div className="max-w-7xl mx-auto w-full px-4 py-6 md:px-8 flex-1 transition-all duration-300">
                <Header />

                {/* Üst İstatistik Kartları */}
                <div className="grid grid-cols-1 md:grid-cols-3 gap-5 mb-6">
                    {/* 1. Bekleyen İşlemler - Siyah Çizgili */}
                    <button
                        onClick={() => handleTopCardClick('PENDING')}
                        className={`${theme.styles.card} cursor-pointer text-left w-full hover:scale-[1.01] active:scale-[0.99] transition-all duration-200 focus:outline-none ${isPendingActive
                            ? 'bg-slate-100/80 font-semibold'
                            : 'opacity-70 hover:opacity-100'
                            }`}
                    >
                        <div className="absolute top-0 left-0 w-full h-1 bg-[#111111]"></div>
                        <div className={theme.styles.cardTitle}>⬛ Bekleyen Şüpheli İşlem</div>
                        <div className="flex items-baseline gap-2 mt-2">
                            <span className="text-3xl font-black text-black">{transactions.length}</span>
                            <span className="text-xs text-red-500 font-bold animate-pulse">(Canlı)</span>
                        </div>
                    </button>
                    {/* 2. Blokelenen İşlemler - Kırmızı Çizgili */}
                    <button
                        onClick={() => handleTopCardClick('BLOCKED')}
                        className={`${theme.styles.card} cursor-pointer text-left w-full hover:scale-[1.01] active:scale-[0.99] transition-all duration-200 focus:outline-none ${isBlockedActive
                            ? 'bg-red-50/50 font-semibold'
                            : 'opacity-70 hover:opacity-100'
                            }`}
                    >
                        <div className="absolute top-0 left-0 w-full h-1 bg-red-500"></div>
                        <div className={theme.styles.cardTitle}>🚫 Blokelenen İşlemler</div>
                        <div className="text-3xl font-black text-red-600 mt-2">{history.filter(h => h.action === 'BLOCKED').length}</div>
                    </button>
                    {/* 3. Onaylanan İşlemler - Yeşil Çizgili */}
                    <button
                        onClick={() => handleTopCardClick('APPROVED')}
                        className={`${theme.styles.card} cursor-pointer text-left w-full hover:scale-[1.01] active:scale-[0.99] transition-all duration-200 focus:outline-none ${isApprovedActive
                            ? 'bg-emerald-50/50 font-semibold'
                            : 'opacity-70 hover:opacity-100'
                            }`}
                    >
                        <div className="absolute top-0 left-0 w-full h-1 bg-emerald-500"></div>
                        <div className={theme.styles.cardTitle}>✅ Onaylanan İşlemler</div>
                        <div className="text-3xl font-black text-emerald-600 mt-2">{history.filter(h => h.action === 'APPROVED').length}</div>
                    </button>
                </div>


                {/* Filtreleme ve Tab Alanı */}
                <div className={theme.styles.filterSection}>
                    <div className="flex flex-wrap justify-between items-center gap-4">
                        <div className={theme.styles.tabContainer}>
                            <button
                                onClick={() => toggleTab('PENDING')}
                                className={selectedTabs.includes('PENDING') ? theme.styles.tabActive : theme.styles.tabInactive}
                            >
                                📂 Bekleyen ({transactions.length})
                            </button>
                            <button
                                onClick={() => toggleTab('BLOCKED')}
                                className={selectedTabs.includes('BLOCKED') ? theme.styles.tabActive : theme.styles.tabInactive}
                            >
                                🚫 Blokelenen ({history.filter(h => h.action === 'BLOCKED').length})
                            </button>
                            <button
                                onClick={() => toggleTab('APPROVED')}
                                className={selectedTabs.includes('APPROVED') ? theme.styles.tabActive : theme.styles.tabInactive}
                            >
                                ✅ Onaylanan ({history.filter(h => h.action === 'APPROVED').length})
                            </button>
                        </div>


                        {/* Gelişmiş Filtreler */}
                        <div className="flex gap-3 text-sm">
                            <select
                                value={selectedScenario}
                                onChange={(e) => setSelectedScenario(e.target.value)}
                                className={theme.styles.select}
                            >
                                <option value="ALL">Tüm Senaryolar</option>
                                <option value="BRUTE_FORCE">Ardışık Red (Brute Force)</option>
                                <option value="IMPOSSIBLE_TRAVEL">İmkansız Seyahat (Impossible Travel)</option>
                                <option value="CARD_TESTING">Yoklama Çekimi (Card Testing)</option>
                                <option value="MAX_OUT">Limit Boşaltma (Max-Out)</option>
                                <option value="ANOMALOUS_TIME">Gece Sıradışı Tutar (Anomalous Time)</option>
                                <option value="CROSS_BORDER">Sınır Ötesi İşlem (Cross Border)</option>
                                <option value="CURRENCY_MISMATCH">Para Birimi Sapması (Currency)</option>
                                <option value="HIGH_RISK_MCC">Yüksek Riskli MCC (İşyeri)</option>
                                <option value="VELOCITY">Hız/Sıklık Kuralı (Velocity)</option>
                                <option value="CONSECUTIVE_REFUNDS">Ardışık İade Kuralı (Consecutive Refunds)</option>
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
                    selectedIds={selectedIds}
                    onToggleSelection={toggleSelection}
                    onSelectAll={selectAll}
                    onApprove={openApproveModal}
                    onBlock={openBlockModal}
                    onViewDetails={setSidebarTxn}
                    sortFields={sortFields}
                    onSort={handleSort}
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

function AppRouter() {
    const { isLoggedIn } = useAuth();

    if (!isLoggedIn) {
        return <LoginPage />;
    }

    return <Dashboard />;
}

export default function App() {
    return (
        <AuthProvider>
            <AppRouter />
        </AuthProvider>
    );
}