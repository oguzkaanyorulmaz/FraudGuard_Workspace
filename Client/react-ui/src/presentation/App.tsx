import { useState, useEffect } from 'react';
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
import { SearchableSelect } from './components/common/SearchableSelect';


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

    // Ödeme Tipi State'i
    const [selectedPaymentType, setSelectedPaymentType] = useState<string>('ALL');

    // Ödeme Tipine Göre Gruplanmış Senaryolar
    const paymentTypeScenarios: Record<string, { value: string; label: string }[]> = {
        ALL: [
            { value: 'BRUTE_FORCE', label: 'Ardışık Red (Brute Force)' },
            { value: 'IMPOSSIBLE_TRAVEL', label: 'İmkansız Seyahat (Impossible Travel)' },
            { value: 'CARD_TESTING', label: 'Yoklama Çekimi (Card Testing)' },
            { value: 'MAX_OUT', label: 'Limit Boşaltma (Max-Out)' },
            { value: 'ANOMALOUS_TIME', label: 'Gece Sıradışı Tutar (Anomalous Time)' },
            { value: 'CROSS_BORDER', label: 'Sınır Ötesi İşlem (Cross Border)' },
            { value: 'CURRENCY_MISMATCH', label: 'Para Birimi Sapması (Currency)' },
            { value: 'HIGH_RISK_MCC', label: 'Yüksek Riskli MCC (İşyeri)' },
            { value: 'VELOCITY', label: 'Hız/Sıklık Kuralı (Velocity)' },
            { value: 'CONSECUTIVE_REFUNDS', label: 'Ardışık İade Kuralı (Consecutive Refunds)' },
            { value: 'HIGH_VALUE_REFUND_VOID', label: 'Yüksek Tutarlı İptal/İade (High Value Refund)' },
            { value: 'LATE_VOID', label: 'Gecikmiş İptal Kuralı (Late Void)' },
            { value: 'ACCOUNT_DRAIN', label: 'Hesap Boşaltma (Account Drain)' },
            { value: 'SMURFING', label: 'Dilimleme (Smurfing)' },
            { value: 'WALLET_CASHOUT', label: 'Wallet Cash-Out' },
            { value: 'MULTI_SOURCE_FUNDING', label: 'Çoklu Kaynakla Fonlama' },
            { value: 'CROSS_BORDER_TRANSFER', label: 'Sınır Ötesi Transfer' },
            { value: 'NEW_BENEFICIARY_TRANSFER', label: 'Yeni Alıcı Transferi' },
            { value: 'SUSPICIOUS_DESCRIPTION', label: 'Şüpheli Açıklama' },
            { value: 'HIGH_RISK_RECEIVER', label: 'Yüksek Riskli Alıcı' },
            { value: 'RECEIVER_BALANCE_ANOMALY', label: 'Katır Hesap Bakiye Sapması' },
            { value: 'MULTI_SENDER_TO_SINGLE_RECEIVER', label: 'Tek Alıcıya Çoklu Gönderim (Multi-Sender)' }
        ],
        CREDIT_CARD: [
            { value: 'BRUTE_FORCE', label: 'Ardışık Red (Brute Force)' },
            { value: 'IMPOSSIBLE_TRAVEL', label: 'İmkansız Seyahat (Impossible Travel)' },
            { value: 'CARD_TESTING', label: 'Yoklama Çekimi (Card Testing)' },
            { value: 'MAX_OUT', label: 'Limit Boşaltma (Max-Out)' },
            { value: 'ANOMALOUS_TIME', label: 'Gece Sıradışı Tutar (Anomalous Time)' },
            { value: 'CROSS_BORDER', label: 'Sınır Ötesi İşlem (Cross Border)' },
            { value: 'CURRENCY_MISMATCH', label: 'Para Birimi Sapması (Currency)' },
            { value: 'HIGH_RISK_MCC', label: 'Yüksek Riskli MCC (İşyeri)' },
            { value: 'VELOCITY', label: 'Hız/Sıklık Kuralı (Velocity)' },
            { value: 'CONSECUTIVE_REFUNDS', label: 'Ardışık İade Kuralı (Consecutive Refunds)' },
            { value: 'HIGH_VALUE_REFUND_VOID', label: 'Yüksek Tutarlı İptal/İade (High Value Refund)' },
            { value: 'LATE_VOID', label: 'Gecikmiş İptal Kuralı (Late Void)' }
        ],
        DEBIT_CARD: [
            { value: 'ACCOUNT_DRAIN', label: 'Hesap Boşaltma (Account Drain)' },
            { value: 'BRUTE_FORCE', label: 'Ardışık Red (Brute Force)' },
            { value: 'IMPOSSIBLE_TRAVEL', label: 'İmkansız Seyahat (Impossible Travel)' },
            { value: 'CARD_TESTING', label: 'Yoklama Çekimi (Card Testing)' },
            { value: 'ANOMALOUS_TIME', label: 'Gece Sıradışı Tutar (Anomalous Time)' },
            { value: 'CROSS_BORDER', label: 'Sınır Ötesi İşlem (Cross Border)' },
            { value: 'CURRENCY_MISMATCH', label: 'Para Birimi Sapması (Currency)' },
            { value: 'HIGH_RISK_MCC', label: 'Yüksek Riskli MCC (İşyeri)' },
            { value: 'VELOCITY', label: 'Hız/Sıklık Kuralı (Velocity)' },
            { value: 'CONSECUTIVE_REFUNDS', label: 'Ardışık İade Kuralı (Consecutive Refunds)' },
            { value: 'HIGH_VALUE_REFUND_VOID', label: 'Yüksek Tutarlı İptal/İade (High Value Refund)' },
            { value: 'LATE_VOID', label: 'Gecikmiş İptal Kuralı (Late Void)' }
        ],
        BANK_TRANSFER: [
            { value: 'SMURFING', label: 'Dilimleme (Smurfing)' },
            { value: 'WALLET_CASHOUT', label: 'Wallet Cash-Out' },
            { value: 'MULTI_SOURCE_FUNDING', label: 'Çoklu Kaynakla Fonlama' },
            { value: 'CROSS_BORDER_TRANSFER', label: 'Sınır Ötesi Transfer' },
            { value: 'NEW_BENEFICIARY_TRANSFER', label: 'Yeni Alıcı Transferi' },
            { value: 'SUSPICIOUS_DESCRIPTION', label: 'Şüpheli Açıklama' },
            { value: 'HIGH_RISK_RECEIVER', label: 'Yüksek Riskli Alıcı' },
            { value: 'RECEIVER_BALANCE_ANOMALY', label: 'Katır Hesap Bakiye Sapması' },
            { value: 'MULTI_SENDER_TO_SINGLE_RECEIVER', label: 'Tek Alıcıya Çoklu Gönderim (Multi-Sender)' }
        ]
    };


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
    const [exitingTxnIds, setExitingTxnIds] = useState<string[]>([]);

    const openApproveModal = (id: string) => { setSelectedTxnId(id); setActionType('APPROVE'); setIsModalOpen(true); };
    const openBlockModal = (id: string) => { setSelectedTxnId(id); setActionType('BLOCK'); setIsModalOpen(true); };
    const openBulkBlockModal = () => { setActionType('BULK_BLOCK'); setIsModalOpen(true); };
    const openBulkApproveModal = () => { setActionType('BULK_APPROVE'); setIsModalOpen(true); };

    const handleModalConfirm = (id: string, reason: string, blockReasonId?: number, analystName?: string) => {
        setIsModalOpen(false);
        const currentAction = actionType;
        setActionType(null);
        setSelectedTxnId(null);

        if (currentAction === 'APPROVE') {
            setExitingTxnIds(prev => [...prev, id]);
            setTimeout(() => {
                handleApprove(id, reason, analystName);
                setExitingTxnIds(prev => prev.filter(x => x !== id));
            }, 400);
        }
        else if (currentAction === 'BLOCK') {
            setExitingTxnIds(prev => [...prev, id]);
            setTimeout(() => {
                handleBlock(id, reason, blockReasonId, analystName);
                setExitingTxnIds(prev => prev.filter(x => x !== id));
            }, 400);
        }
        else if (currentAction === 'BULK_BLOCK') {
            const targets = [...selectedIds];
            setExitingTxnIds(prev => [...prev, ...targets]);
            setTimeout(() => {
                handleBulkBlock(targets, reason, blockReasonId, analystName);
                clearSelection();
                setExitingTxnIds(prev => prev.filter(x => !targets.includes(x)));
            }, 400);
        }
        else if (currentAction === 'BULK_APPROVE') {
            const targets = [...selectedIds];
            setExitingTxnIds(prev => [...prev, ...targets]);
            setTimeout(() => {
                handleBulkApprove(targets, reason, analystName);
                clearSelection();
                setExitingTxnIds(prev => prev.filter(x => !targets.includes(x)));
            }, 400);
        }
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
        setSelectedPaymentType('ALL');
        clearSelection();
    };

    const matchScenario = (txn: Transaction, scenario: string): boolean => {
        if (scenario === 'ALL') return true;
        return txn.ruleCode === scenario;
    };

    const getFilteredTransactions = () => {
        return transactions.filter(txn => {
            if (searchTerm && !txn.id.includes(searchTerm) && !txn.maskedCard.includes(searchTerm)) return false;
            if (selectedPaymentType !== 'ALL' && txn.paymentType !== selectedPaymentType) return false;
            if (!matchScenario(txn, selectedScenario)) return false;
            return true;
        });
    };

    const getFilteredHistory = (action: 'APPROVED' | 'BLOCKED') => {
        return history.filter(h => {
            if (h.action !== action) return false;
            const txn = h.transaction;
            if (searchTerm && !txn.id.includes(searchTerm) && !txn.maskedCard.includes(searchTerm)) return false;
            if (selectedPaymentType !== 'ALL' && txn.paymentType !== selectedPaymentType) return false;
            if (!matchScenario(txn, selectedScenario)) return false;
            return true;
        });
    };

    // Filtreleme ve Sıralama Mantığı
    const getFilteredData = () => {
        let sourceData: { transaction: Transaction; historyAction?: 'APPROVED' | 'BLOCKED' }[] = [];

        if (selectedTabs.includes('PENDING')) {
            sourceData = [...sourceData, ...getFilteredTransactions().map(t => ({ transaction: t }))];
        }
        if (selectedTabs.includes('BLOCKED')) {
            sourceData = [
                ...sourceData,
                ...getFilteredHistory('BLOCKED').map(h => ({ transaction: h.transaction, historyAction: 'BLOCKED' as const }))
            ];
        }
        if (selectedTabs.includes('APPROVED')) {
            sourceData = [
                ...sourceData,
                ...getFilteredHistory('APPROVED').map(h => ({ transaction: h.transaction, historyAction: 'APPROVED' as const }))
            ];
        }


        return [...sourceData].sort((a, b) => {
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


    const [visibleItems, setVisibleItems] = useState<{ transaction: Transaction; historyAction?: 'APPROVED' | 'BLOCKED' }[]>([]);

    useEffect(() => {
        const nextData = getFilteredData();
        const nextIds = new Set(nextData.map(item => item.transaction.id));
        const exiting = visibleItems.filter(item => !nextIds.has(item.transaction.id));

        if (exiting.length > 0 && visibleItems.length > 0) {
            const exitingIds = exiting.map(item => item.transaction.id);
            setExitingTxnIds(prev => [...new Set([...prev, ...exitingIds])]);
            const timer = setTimeout(() => {
                setVisibleItems(nextData);
                setExitingTxnIds(prev => prev.filter(id => !exitingIds.includes(id)));
            }, 400);
            return () => clearTimeout(timer);
        } else {
            setVisibleItems(nextData);
        }
    }, [transactions, history, selectedTabs, searchTerm, selectedScenario, selectedPaymentType, sortFields]);

    const isPendingActive = selectedTabs.length === 1 && selectedTabs.includes('PENDING');
    const isBlockedActive = selectedTabs.length === 1 && selectedTabs.includes('BLOCKED');
    const isApprovedActive = selectedTabs.length === 1 && selectedTabs.includes('APPROVED');

    const getEmptyMessage = () => {
        if (isPendingActive) return "Şüpheli İşlem Bulunmamaktadır.";
        if (isBlockedActive) return "Blokelenen İşlem Bulunmamaktadır.";
        if (isApprovedActive) return "Onaylanan İşlem Bulunmamaktadır.";
        return "Kayıt Bulunmamaktadır.";
    };

    return (
        <div className={theme.styles.body}>
            <Header />
            {/* Full-width container to occupy the entire screen */}
            <div className="w-full px-4 py-6 md:px-8 flex-1 transition-all duration-300">

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
                                📂 Bekleyen ({getFilteredTransactions().length})
                            </button>
                            <button
                                onClick={() => toggleTab('BLOCKED')}
                                className={selectedTabs.includes('BLOCKED') ? theme.styles.tabActive : theme.styles.tabInactive}
                            >
                                🚫 Blokelenen ({getFilteredHistory('BLOCKED').length})
                            </button>
                            <button
                                onClick={() => toggleTab('APPROVED')}
                                className={selectedTabs.includes('APPROVED') ? theme.styles.tabActive : theme.styles.tabInactive}
                            >
                                ✅ Onaylanan ({getFilteredHistory('APPROVED').length})
                            </button>
                        </div>


                        {/* Gelişmiş Filtreler */}
                        <div className="flex flex-wrap md:flex-nowrap items-center gap-2 text-sm overflow-visible pb-1 md:pb-0">
                            {/* Ödeme Tipi Seçimi */}
                            <SearchableSelect
                                options={[
                                    { value: 'ALL', label: '🔍 Tüm Tipler' },
                                    { value: 'CREDIT_CARD', label: '💳 Kredi Kartı' },
                                    { value: 'DEBIT_CARD', label: '🏦 Banka Kartı' },
                                    { value: 'BANK_TRANSFER', label: '💸 EFT / Havale' }
                                ]}
                                value={selectedPaymentType}
                                onChange={(val) => {
                                    setSelectedPaymentType(val);
                                    setSelectedScenario('ALL');
                                }}
                                placeholder="🔍 Tüm Tipler"
                                className="w-40 md:w-48"
                            />

                            {/* Senaryo Seçimi (Dinamik Süzülen) */}
                            <SearchableSelect
                                options={[
                                    { value: 'ALL', label: '📋 Tüm Senaryolar' },
                                    ...(paymentTypeScenarios[selectedPaymentType] || [])
                                ]}
                                value={selectedScenario}
                                onChange={setSelectedScenario}
                                placeholder="📋 Senaryo Seçiniz"
                                className="w-44 md:w-52"
                            />

                            <div className="relative flex items-center bg-white border border-[#C5CBD3] rounded-lg px-3 py-1.5 transition-all focus-within:border-[#FDBB30] focus-within:ring-2 focus-within:ring-[#FDBB30]/20 w-44 md:w-64">
                                <svg 
                                    className="w-4 h-4 text-slate-500 mr-2 flex-shrink-0" 
                                    fill="none" 
                                    stroke="currentColor" 
                                    viewBox="0 0 24 24"
                                >
                                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2.5} d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
                                </svg>
                                <input
                                    type="text"
                                    placeholder="Arama"
                                    value={searchTerm}
                                    onChange={(e) => setSearchTerm(e.target.value)}
                                    className="w-full bg-transparent border-none outline-none text-xs md:text-sm text-[#1A1D20] placeholder-slate-400 focus:ring-0 p-0 pr-5"
                                />
                                {searchTerm && (
                                    <button 
                                        type="button"
                                        onClick={() => setSearchTerm('')}
                                        className="absolute right-2 text-slate-400 hover:text-slate-700 bg-transparent border-none outline-none cursor-pointer flex items-center justify-center p-0"
                                    >
                                        <svg 
                                            className="w-4 h-4" 
                                            fill="none" 
                                            stroke="currentColor" 
                                            viewBox="0 0 24 24"
                                        >
                                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                                        </svg>
                                    </button>
                                )}
                            </div>
                        </div>

                    </div>

                    <BulkActionBar selectedCount={selectedIds.length} onBulkBlock={openBulkBlockModal} onClear={clearSelection} onBulkApprove={openBulkApproveModal} />
                </div>

                <TransactionList
                    transactions={visibleItems}
                    loading={loading}
                    selectedIds={selectedIds}
                    onToggleSelection={toggleSelection}
                    onSelectAll={selectAll}
                    onApprove={openApproveModal}
                    onBlock={openBlockModal}
                    onViewDetails={setSidebarTxn}
                    sortFields={sortFields}
                    onSort={handleSort}
                    exitingIds={exitingTxnIds}
                    emptyMessage={getEmptyMessage()}
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