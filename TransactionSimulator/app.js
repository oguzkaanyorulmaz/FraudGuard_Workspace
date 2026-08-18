/* ============================================
   FraudGuard İşlem Simülatörü — App Logic
   ============================================ */

const API_BASE = '/api/transactions';

// ─── DOM Elements ───
const tabCard = document.getElementById('tabCard');
const tabTransfer = document.getElementById('tabTransfer');
const tabSlider = document.getElementById('tabSlider');
const cardForm = document.getElementById('cardForm');
const transferForm = document.getElementById('transferForm');
const responseArea = document.getElementById('responseArea');
const responseCard = document.getElementById('responseCard');
const responseIcon = document.getElementById('responseIcon');
const responseTitle = document.getElementById('responseTitle');
const responseBody = document.getElementById('responseBody');
const closeResponse = document.getElementById('closeResponse');
const historyList = document.getElementById('historyList');
const clearHistory = document.getElementById('clearHistory');
const statusIndicator = document.getElementById('statusIndicator');
const statusText = statusIndicator.querySelector('.status-text');
const cardSubmitBtn = document.getElementById('cardSubmitBtn');
const transferSubmitBtn = document.getElementById('transferSubmitBtn');

// ─── State ───
let history = [];
let historyFilter = 'all';
let expandedHistoryId = null;

// ─── Tab Switching ───
tabCard.addEventListener('click', () => switchTab('card'));
tabTransfer.addEventListener('click', () => switchTab('transfer'));

// --- Transaction and Channel Type Constraints Handler ---
const transactionTypeSelect = document.getElementById('transactionType');
const rrnGroup = document.getElementById('rrnGroup');
const rrnInput = document.getElementById('rrnInput');
const channelTypeSelect = document.getElementById('channelTypeId');
const merchantCategorySelect = document.getElementById('merchantCategory');
const merchantCategoryGroup = merchantCategorySelect.closest('.form-group');

const allChannels = [
    { value: '1', label: 'POS (Fiziksel POS)' },
    { value: '2', label: 'VirtualPOS (Sanal POS)' },
    { value: '3', label: 'ATM (ATM Cihazı)' },
    { value: '4', label: 'Mobile (Mobil Şube)' },
    { value: '5', label: 'Web (İnternet Şubesi)' }
];

function populateChannelOptions(allowedIds, defaultValue) {
    const currentVal = channelTypeSelect.value;
    channelTypeSelect.innerHTML = '';
    
    allChannels.forEach(ch => {
        const id = parseInt(ch.value, 10);
        if (allowedIds.includes(id)) {
            const opt = document.createElement('option');
            opt.value = ch.value;
            opt.textContent = ch.label;
            channelTypeSelect.appendChild(opt);
        }
    });

    const isCurrentAllowed = allowedIds.includes(parseInt(currentVal, 10));
    channelTypeSelect.value = isCurrentAllowed ? currentVal : defaultValue.toString();
}

function handleTransactionTypeChange() {
    const txType = parseInt(transactionTypeSelect.value, 10);
    
    // RRN (Referans No) Alanı Kontrolü (Sadece İade İşleminde zorunlu)
    if (txType === 2) {
        rrnGroup.style.display = 'block';
        rrnInput.required = true;
    } else {
        rrnGroup.style.display = 'none';
        rrnInput.required = false;
        rrnInput.value = '';
    }

    channelTypeSelect.disabled = false;

    if (txType === 3) { // ATM Para Yatırma
        populateChannelOptions([3], 3);
    } 
    else if (txType === 4) { // Kredi Kartı Borç Ödeme
        populateChannelOptions([3, 4, 5], 4);
    }
    else if (txType === 1 || txType === 2) { // Satış veya İade
        populateChannelOptions([1, 2, 4, 5], 2);
    }

    // Kanal değiştiği için kategori görünürlüğünü de güncelle
    handleChannelTypeChange();
}

function handleChannelTypeChange() {
    const channelVal = parseInt(channelTypeSelect.value, 10);
    const isAtm = channelVal === 3;

    merchantCategoryGroup.style.display = isAtm ? 'none' : 'block';

    // ATM işleminin üye işyeri yoktur; seçici gizlenir ve seçim temizlenir.
    const merchantGroupEl = document.getElementById('merchantGroup');
    if (merchantGroupEl) {
        merchantGroupEl.style.display = isAtm ? 'none' : 'block';
        if (isAtm) {
            const select = document.getElementById('merchantId');
            select.value = '';
            select.dispatchEvent(new Event('change'));
        }
    }
}

// Event Listeners
transactionTypeSelect.addEventListener('change', handleTransactionTypeChange);
channelTypeSelect.addEventListener('change', handleChannelTypeChange);

// Sayfa ilk yüklendiğinde kısıtlamaları hemen uygulamak için fonksiyonu bir kez çalıştıralım:
handleTransactionTypeChange();

function switchTab(tab) {
    if (tab === 'card') {
        tabCard.classList.add('active');
        tabTransfer.classList.remove('active');
        tabSlider.classList.remove('right');
        cardForm.classList.remove('hidden');
        transferForm.classList.add('hidden');
    } else {
        tabTransfer.classList.add('active');
        tabCard.classList.remove('active');
        tabSlider.classList.add('right');
        transferForm.classList.remove('hidden');
        cardForm.classList.add('hidden');
    }
    hideResponse();
}

// ─── Card Number Formatting ───
// ─── Kart Numarası Biçimlendirme, Logo ve Geçerlilik Kontrolü ───
const cardNumberInput = document.getElementById('cardNumber');
const paymentTypeSelect = document.getElementById('paymentType');
const cardLogoContainer = document.getElementById('cardLogoContainer');
const cardNumberError = document.getElementById('cardNumberError');
const cardInputWrapper = cardNumberInput.closest('.input-wrapper');

// Dinamik SVG Logoları
const CARD_LOGOS = {
    visa: `
        <svg class="card-brand-logo visible" viewBox="0 0 36 12" width="36" height="12">
            <text x="0" y="11" font-family="'Inter', -apple-system, sans-serif" font-weight="900" font-size="12" fill="#0E4595" font-style="italic">VISA</text>
        </svg>
    `,
    mastercard: `
        <svg class="card-brand-logo visible" viewBox="0 0 24 16" width="24" height="16">
            <circle cx="8" cy="8" r="8" fill="#EB001B" />
            <circle cx="16" cy="8" r="8" fill="#F79E1B" fill-opacity="0.85" />
        </svg>
    `,
    troy: `
        <svg class="card-brand-logo visible" viewBox="0 0 36 14" width="36" height="14">
            <text x="0" y="11" font-family="-apple-system, BlinkMacSystemFont, sans-serif" font-weight="900" font-size="12" fill="#00A59B" font-style="italic">troy</text>
        </svg>
    `
};

// Luhn (Mod 10) Doğrulama Algoritması
function validateLuhn(cardNumber) {
    if (!cardNumber || cardNumber.length < 13 || cardNumber.length > 19) return false;
    let sum = 0;
    let shouldDouble = false;
    for (let i = cardNumber.length - 1; i >= 0; i--) {
        let digit = parseInt(cardNumber.charAt(i), 10);
        if (isNaN(digit)) return false;
        if (shouldDouble) {
            digit *= 2;
            if (digit > 9) digit -= 9;
        }
        sum += digit;
        shouldDouble = !shouldDouble;
    }
    return (sum % 10 === 0);
}

cardNumberInput.addEventListener('input', (e) => {
    let val = e.target.value.replace(/\D/g, '');

    // 1. Dinamik Kart Tipi Algılama ve Logo Değişimi
    let brand = '';
    if (val.startsWith('4')) {
        brand = 'visa';
        paymentTypeSelect.value = "2"; // Banka Kartı (Debit)
    } else if (val.startsWith('5')) {
        brand = 'mastercard';
        paymentTypeSelect.value = "1"; // Kredi Kartı
    } else if (val.startsWith('6')) {
        brand = 'troy';
        paymentTypeSelect.value = "2"; // Troy genellikle Banka Kartı (Debit) olarak kabul edilsin
    }

    if (brand) {
        cardLogoContainer.innerHTML = CARD_LOGOS[brand];
    } else {
        cardLogoContainer.innerHTML = '';
    }

    // 2. Formatlama (4'er haneli boşluk bırakarak)
    val = val.substring(0, 16);
    let formatted = val.replace(/(.{4})/g, '$1 ').trim();
    e.target.value = formatted;

    // 3. Anlık Doğruluk Kontrolü (16 hane tamamlandığında kontrol et)
    if (val.length === 16) {
        if (!validateLuhn(val)) {
            cardNumberError.classList.remove('hidden');
            cardInputWrapper.classList.add('has-error');
        } else {
            cardNumberError.classList.add('hidden');
            cardInputWrapper.classList.remove('has-error');
        }
    } else {
        // Kullanıcı yazmaya devam ettiği sürece hata gösterme
        cardNumberError.classList.add('hidden');
        cardInputWrapper.classList.remove('has-error');
    }
});



// ─── Expiry Date Formatting ───
const expiryInput = document.getElementById('expiryDate');
expiryInput.addEventListener('input', (e) => {
    let val = e.target.value.replace(/\D/g, '');
    if (val.length >= 2) {
        val = val.substring(0, 2) + '/' + val.substring(2, 4);
    }
    e.target.value = val;
});

// ─── Card Form Submit ───
cardForm.addEventListener('submit', async (e) => {
    e.preventDefault();

    const cardNumberRaw = cardNumberInput.value.replace(/\s/g, '');

    const payload = {
        cardNumber: cardNumberRaw,
        expiryDate: expiryInput.value,
        cvv: document.getElementById('cvv').value,
        amount: parseFloat(document.getElementById('cardAmount').value),
        transactionType: parseInt(document.getElementById('transactionType').value),
        paymentType: parseInt(document.getElementById('paymentType').value),
        channelTypeId: parseInt(document.getElementById('channelTypeId').value),
        location: document.getElementById('cardLocation').value || 'Ankara',
        country: document.getElementById('cardCountry').value || 'Türkiye',
        merchantCategory: parseInt(document.getElementById('channelTypeId').value, 10) === 3
            ? 'ATM / Nakit'
            : document.getElementById('merchantCategory').value,
        // Boş bırakılırsa işyeri bazlı sayaçlar hesaplanmaz; backend null bekliyor.
        merchantId: parseInt(document.getElementById('channelTypeId').value, 10) === 3
            ? null
            : (document.getElementById('merchantId').value || null),
        rrn: rrnInput.value || null
    };

    // 🟢 Gönderim öncesi son doğruluk kontrolü
    if (!validateLuhn(cardNumberRaw)) {
        cardNumberError.classList.remove('hidden');
        cardInputWrapper.classList.add('has-error');

        // Simülatör geçmişinde hata kaydı oluştur ve göster
        addHistory({
            isSuccess: false,
            message: "Geçersiz Kart",
            status: "Declined",
            declineReason: "Girdiğiniz kart numarası Luhn (Mod 10) algoritma doğrulamasından geçemedi."
        }, 'card', payload);
        return;
    }

    await sendRequest(`${API_BASE}/process`, payload, 'card', cardSubmitBtn);
});


// ─── Transfer Form Submit ───
transferForm.addEventListener('submit', async (e) => {
    e.preventDefault();

    const payload = {
        senderIBAN: document.getElementById('senderIBAN').value,
        receiverIBAN: document.getElementById('receiverIBAN').value,
        receiverName: document.getElementById('receiverName').value,
        description: document.getElementById('description').value || 'Para transferi',
        amount: parseFloat(document.getElementById('transferAmount').value),
        currency: document.getElementById('transferCurrency').value,
        location: document.getElementById('transferLocation').value || 'İnternet Bankacılığı',
        country: document.getElementById('transferCountry').value || 'Türkiye'
    };

    await sendRequest(`${API_BASE}/transfer`, payload, 'transfer', transferSubmitBtn);
});

// ─── Send API Request ───
async function sendRequest(url, payload, type, btn) {
    btn.classList.add('loading');

    try {
        const res = await fetch(url, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });

        const raw = await res.json();
        // ResponseDTO<T> wraps actual data inside "data" property
        const inner = raw.data || raw;
        const merged = { ...inner, message: raw.message, isSuccess: raw.isSuccess };
        addHistory(merged, type, payload);
    } catch (err) {
        addHistory({ status: 'Error', message: err.message }, type, payload);
    } finally {
        btn.classList.remove('loading');
    }
}

// ─── Show Response ───
function showResponse(data, isOk, type) {
    responseArea.classList.remove('hidden');

    // Determine status
    const status = (data.status || '').toLowerCase();
    let statusClass = 'status-approved';
    let icon = '✅';
    let title = 'İşlem Onaylandı';

    if (status === 'suspicious' || status.includes('fraud') || status.includes('şüpheli')) {
        statusClass = 'status-suspicious';
        icon = '⚠️';
        title = 'Şüpheli İşlem Tespit Edildi';
    } else if (status === 'declined' || status === 'rejected' || status.includes('red')) {
        statusClass = 'status-declined';
        icon = '❌';
        title = 'İşlem Reddedildi';
    } else if (!isOk) {
        statusClass = 'status-error';
        icon = '🚨';
        title = 'Hata Oluştu';
    }

    responseCard.className = `response-card glass-card ${statusClass}`;
    responseIcon.textContent = icon;
    responseTitle.textContent = title;

    // Build response body
    let html = '<div class="response-grid">';

    // Status
    const statusDisplayClass = status === 'approved' ? 'approved' :
        status === 'suspicious' ? 'suspicious' : 'declined';
    html += renderItem('Durum', `<span class="status-text ${statusDisplayClass}">${data.status || 'Bilinmiyor'}</span>`);

    if (data.transactionId) {
        html += renderItem('İşlem ID', data.transactionId);
    }

    if (data.amount !== undefined) {
        html += renderItem('Tutar', `${formatAmount(data.amount)} ${data.currency || ''}`);
    }

    if (data.remainingBalance !== undefined) {
        html += renderItem('Kalan Bakiye', formatAmount(data.remainingBalance));
    }

    if (data.declineReason) {
        html += renderItem('Red Sebebi', data.declineReason, true);
    }

    if (data.fraudReason) {
        html += renderItem('Fraud Nedeni', data.fraudReason, true);
    }

    if (data.message) {
        html += renderItem('Mesaj', data.message, true);
    }

    if (data.rrn) {
        html += renderItem('RRN (Referans No)', data.rrn);
    }

    // Show raw JSON for extra info
    const knownKeys = ['status', 'transactionId', 'amount', 'currency', 'remainingBalance', 'declineReason', 'fraudReason', 'message', 'isSuccess', 'rrn'];
    const extra = Object.entries(data).filter(([k]) => !knownKeys.includes(k));
    if (extra.length > 0) {
        extra.forEach(([key, value]) => {
            html += renderItem(formatKey(key), typeof value === 'object' ? JSON.stringify(value) : value);
        });
    }

    html += '</div>';
    responseBody.innerHTML = html;

    // Scroll into view
    responseArea.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
}

function showErrorResponse(message) {
    responseArea.classList.remove('hidden');
    responseCard.className = 'response-card glass-card status-error';
    responseIcon.textContent = '🚨';
    responseTitle.textContent = 'Bağlantı Hatası';
    responseBody.innerHTML = `
        <div class="response-grid">
            ${renderItem('Hata', message, true)}
            ${renderItem('Öneri', 'Backend\'in çalıştığından emin olun (docker compose up --build)', true)}
        </div>
    `;
}

function hideResponse() {
    responseArea.classList.add('hidden');
}

function renderItem(label, value, fullWidth = false) {
    return `
        <div class="response-item ${fullWidth ? 'full-width' : ''}">
            <div class="response-label">${label}</div>
            <div class="response-value">${value}</div>
        </div>
    `;
}

function formatAmount(amount) {
    return new Intl.NumberFormat('tr-TR', {
        minimumFractionDigits: 2,
        maximumFractionDigits: 2
    }).format(amount);
}

function formatKey(key) {
    return key
        .replace(/([A-Z])/g, ' $1')
        .replace(/^./, s => s.toUpperCase())
        .trim();
}

// ─── Response Close ───
closeResponse.addEventListener('click', hideResponse);

// ─── History ───
function addHistory(data, type, payload) {
    // Normalize status from API
    const rawStatus = (data.status || '').toString();
    let normalizedStatus = 'error';
    let displayStatus = 'Error';

    if (rawStatus.toLowerCase() === 'approved') {
        normalizedStatus = 'approved';
        displayStatus = 'Approved';
    } else if (rawStatus.toLowerCase() === 'suspicious') {
        normalizedStatus = 'suspicious';
        displayStatus = 'Suspicious';
    } else if (rawStatus.toLowerCase() === 'declined' || rawStatus.toLowerCase() === 'rejected') {
        normalizedStatus = 'declined';
        displayStatus = 'Declined';
    }

    // Build meta description
    let meta;
    if (type === 'card') {
        meta = `•••• ${(payload.cardNumber || '').slice(-4)}`;
    } else {
        // Transfer: Gönderen IBAN sonu → Alıcı adı
        const senderShort = payload.senderIBAN ? `****${payload.senderIBAN.slice(-4)}` : 'Gönderici';
        const receiverShort = payload.receiverName || `****${(payload.receiverIBAN || '').slice(-4)}`;
        meta = `${senderShort} → ${receiverShort}`;
    }

    const entry = {
        id: Date.now(),
        type,
        status: normalizedStatus,
        statusText: displayStatus,
        amount: payload.amount,
        currency: payload.currency || 'TRY',
        meta,
        time: new Date(),
        payload,
        responseData: data
    };

    history.unshift(entry);
    if (history.length > 50) history.pop();

    // En son gönderilen işlemi otomatik açalım
    expandedHistoryId = entry.id;

    renderHistory();
}

function setHistoryFilter(filter) {
    historyFilter = filter;
    // Kural yönetimi sayfası da aynı sınıfı kullanıyor; seçim yalnızca bu sayfada güncellenir.
    document.querySelectorAll('#pageTransactions .history-filter-btn').forEach(btn => {
        btn.classList.toggle('active', btn.dataset.filter === filter);
    });
    renderHistory();
}

function renderHistory() {
    let filtered = history;
    if (historyFilter === 'suspicious') {
        filtered = history.filter(item => item.status === 'suspicious');
    }
    const display = filtered.slice(0, 10);

    if (display.length === 0) {
        const emptyMsg = historyFilter === 'suspicious'
            ? 'Henüz şüpheli işlem yok'
            : 'Henüz işlem geçmişi yok';
        historyList.innerHTML = `<div class="empty-history">${emptyMsg}</div>`;
        return;
    }

    historyList.innerHTML = display.map((item) => {
        const statusClass = item.status === 'approved' ? 'approved'
            : item.status === 'suspicious' ? 'suspicious'
                : 'declined';
        const timeStr = item.time.toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit' });

        // Build detail rows
        let detailRows = '';
        const p = item.payload || {};
        const r = item.responseData || {};

        if (item.type === 'card') {
            if (p.cardNumber) detailRows += historyDetailRow('Kart No', maskCard(p.cardNumber));
            if (p.expiryDate) detailRows += historyDetailRow('Son Kullanma', p.expiryDate);
            if (p.transactionType) {
                const typeMap = { 1: 'Satış', 2: 'İade', 3: 'Para Yatırma', 4: 'Borç Ödeme' };
                detailRows += historyDetailRow('İşlem Tipi', typeMap[p.transactionType] || p.transactionType);
            }
            if (p.paymentType) detailRows += historyDetailRow('Ödeme Tipi', p.paymentType === 1 ? 'Kredi Kartı' : 'Banka Kartı');
            if (p.channelTypeId) detailRows += historyDetailRow('Kanal', getChannelName(p.channelTypeId));
            if (p.merchantCategory) detailRows += historyDetailRow('Kategori', p.merchantCategory);
            if (p.location) detailRows += historyDetailRow('Lokasyon', p.location);
            if (p.rrn) detailRows += historyDetailRow('RRN (Gönderilen)', p.rrn);
        } else {
            if (p.senderIBAN) detailRows += historyDetailRow('Gönderici IBAN', maskIban(p.senderIBAN));
            if (p.receiverIBAN) detailRows += historyDetailRow('Alıcı IBAN', maskIban(p.receiverIBAN));
            if (p.receiverName) detailRows += historyDetailRow('Alıcı Adı', p.receiverName);
            if (p.description) detailRows += historyDetailRow('Açıklama', p.description);
            if (p.location) detailRows += historyDetailRow('Lokasyon', p.location);
        }

        // Response fields
        if (r.transactionId) detailRows += historyDetailRow('İşlem ID', r.transactionId);
        if (r.rrn) detailRows += historyDetailRow('RRN', r.rrn);
        if (p.merchantId) detailRows += historyDetailRow('Üye İşyeri', p.merchantId);
        if (r.declineReason && item.status !== 'Approved') detailRows += historyDetailRow('Red Sebebi', r.declineReason);
        if (r.fraudReason) detailRows += historyDetailRow('Fraud Nedeni', r.fraudReason);

        // Fraud değerlendirmesi. Onaylanan işlemlerde de gösterilir: bir kural tetiklenip
        // puanı eşiğin altında kaldığında karar NORMAL olur, ama kural çalışmıştır.
        detailRows += renderFraudBreakdown(r);

        if (r.message) detailRows += historyDetailRow('Mesaj', r.message, true); // 🟢 fullWidth = true

        const isExpanded = item.id === expandedHistoryId;
        return `
            <div class="history-item-wrapper ${isExpanded ? 'expanded' : ''}" data-history-id="${item.id}">
                <div class="history-item" onclick="toggleHistoryDetail(${item.id})">
                    <div class="history-left">
                        <span class="history-type-badge ${item.type}">${item.type === 'card' ? 'KART' : 'EFT'}</span>
                        <div class="history-info">
                            <span class="history-amount">${formatAmount(item.amount)} ${item.currency}</span>
                            <span class="history-meta">${item.meta}</span>
                        </div>
                    </div>
                    <div class="history-right">
                        <span class="history-status ${statusClass}">${item.statusText}</span>
                        <span class="history-time">${timeStr}</span>
                        <span class="history-chevron">‹</span>
                    </div>
                </div>
                <div class="history-detail">
                    <div class="history-detail-grid">
                        ${detailRows}
                    </div>
                </div>
            </div>
        `;
    }).join('');
}

function historyDetailRow(label, value, fullWidth = false) {
    return `
        <div class="history-detail-item ${fullWidth ? 'full-width' : ''}">
            <span class="history-detail-label">${label}</span>
            <span class="history-detail-value">${value}</span>
        </div>
    `;
}

function maskCard(num) {
    const clean = (num || '').replace(/\s/g, '');
    if (clean.length < 8) return clean;
    return clean.slice(0, 4) + ' •••• •••• ' + clean.slice(-4);
}

function maskIban(iban) {
    if (!iban || iban.length < 10) return iban;
    return iban.slice(0, 6) + '••••••••••' + iban.slice(-4);
}

function getChannelName(id) {
    const map = { 1: 'Fiziksel POS', 2: 'Sanal POS', 3: 'ATM', 4: 'Mobil Şube', 5: 'İnternet Şubesi' };
    return map[id] || id;
}

function toggleHistoryDetail(id) {
    if (expandedHistoryId === id) {
        expandedHistoryId = null;
    } else {
        expandedHistoryId = id;
    }
    renderHistory();
}

clearHistory.addEventListener('click', () => {
    history = [];
    renderHistory();
});

// ─── Backend Health Check ───
async function checkBackend() {
    try {
        const res = await fetch(`${API_BASE}/ping`, {
            method: 'GET'
        });
        if (res.ok) {
            statusIndicator.className = 'status-indicator online';
            statusText.textContent = 'Backend Aktif';
        } else {
            statusIndicator.className = 'status-indicator offline';
            statusText.textContent = 'Backend Kapalı';
        }
    } catch (err) {
        statusIndicator.className = 'status-indicator offline';
        statusText.textContent = 'Backend Bağlantı Hatası';
    }
}

// ─── Dinamik Para Birimi Logosu ───
const cardCurrencySelect = document.getElementById('cardCurrency');
const cardAmountSign = document.getElementById('cardAmountSign');
const transferCurrencySelect = document.getElementById('transferCurrency');
const transferAmountSign = document.getElementById('transferAmountSign');

const currencySymbols = {
    'TRY': '₺',
    'USD': '$',
    'EUR': '€',
    'GBP': '£'
};

function updateCurrencySymbol(selectEl, signEl) {
    const currency = selectEl.value;
    signEl.textContent = currencySymbols[currency] || '₺';
}

cardCurrencySelect.addEventListener('change', () => updateCurrencySymbol(cardCurrencySelect, cardAmountSign));
transferCurrencySelect.addEventListener('change', () => updateCurrencySymbol(transferCurrencySelect, transferAmountSign));


// ─── Init ───
renderHistory();
checkBackend();
setInterval(checkBackend, 30000);

/* ============================================
   Kimlik Doğrulama
   ============================================ */

const AUTH_STORAGE_KEY = 'fg_sim_auth';
const RULES_API = '/api/RuleManagement';

// { token, username, role, roleName }
let auth = null;

const loginScreen = document.getElementById('loginScreen');
const appShell = document.getElementById('appShell');
const loginError = document.getElementById('loginError');
const loginSuccess = document.getElementById('loginSuccess');
const signInForm = document.getElementById('signInForm');
const signUpForm = document.getElementById('signUpForm');
const signInBtn = document.getElementById('signInBtn');
const signUpBtn = document.getElementById('signUpBtn');
const loginTabSignIn = document.getElementById('loginTabSignIn');
const loginTabSignUp = document.getElementById('loginTabSignUp');

function readStoredAuth() {
    try {
        const raw = localStorage.getItem(AUTH_STORAGE_KEY);
        return raw ? JSON.parse(raw) : null;
    } catch {
        return null;
    }
}

function persistAuth(value) {
    auth = value;
    if (value) {
        localStorage.setItem(AUTH_STORAGE_KEY, JSON.stringify(value));
    } else {
        localStorage.removeItem(AUTH_STORAGE_KEY);
    }
}

function showLoginMessage(el, text) {
    [loginError, loginSuccess].forEach(node => {
        node.classList.add('hidden');
        node.textContent = '';
    });
    if (el && text) {
        el.textContent = text;
        el.classList.remove('hidden');
    }
}

function setLoginMode(mode) {
    const isSignIn = mode === 'signin';
    loginTabSignIn.classList.toggle('active', isSignIn);
    loginTabSignUp.classList.toggle('active', !isSignIn);
    signInForm.classList.toggle('hidden', !isSignIn);
    signUpForm.classList.toggle('hidden', isSignIn);
    showLoginMessage(null);
}

loginTabSignIn.addEventListener('click', () => setLoginMode('signin'));
loginTabSignUp.addEventListener('click', () => setLoginMode('signup'));

function enterApp() {
    loginScreen.classList.add('hidden');
    appShell.classList.remove('hidden');

    document.getElementById('userName').textContent = auth.username;
    document.getElementById('userRole').textContent = auth.roleName || '—';
    document.getElementById('userAvatar').textContent = (auth.username || '?').charAt(0);

    checkBackend();
    loadRules();
    loadFields();
    loadMerchants();
}

function exitApp(message) {
    persistAuth(null);
    appShell.classList.add('hidden');
    loginScreen.classList.remove('hidden');
    setLoginMode('signin');
    document.getElementById('loginPassword').value = '';
    if (message) showLoginMessage(loginError, message);
}

// ─── Giriş ───
signInForm.addEventListener('submit', async (e) => {
    e.preventDefault();
    showLoginMessage(null);
    signInBtn.disabled = true;
    signInBtn.textContent = 'GİRİŞ YAPILIYOR...';

    try {
        const res = await fetch('/api/Auth/login', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                username: document.getElementById('loginUsername').value.trim(),
                password: document.getElementById('loginPassword').value
            })
        });

        const body = await res.json();

        if (!res.ok || !body.isSuccess) {
            showLoginMessage(loginError, firstError(body) || 'Kullanıcı adı veya şifre hatalı.');
            return;
        }

        persistAuth({
            token: body.data.token,
            username: body.data.username,
            role: body.data.role,
            roleName: body.data.roleName
        });
        enterApp();
    } catch (err) {
        showLoginMessage(loginError, `Backend'e ulaşılamadı: ${err.message}`);
    } finally {
        signInBtn.disabled = false;
        signInBtn.textContent = 'GİRİŞ YAP';
    }
});

// ─── Kayıt ───
signUpForm.addEventListener('submit', async (e) => {
    e.preventDefault();
    showLoginMessage(null);
    signUpBtn.disabled = true;
    signUpBtn.textContent = 'KAYIT YAPILIYOR...';

    try {
        const res = await fetch('/api/Auth/register', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                username: document.getElementById('regUsername').value.trim(),
                mail: document.getElementById('regMail').value.trim(),
                password: document.getElementById('regPassword').value,
                role: parseInt(document.getElementById('regRole').value, 10)
            })
        });

        const body = await res.json();

        if (!res.ok || !body.isSuccess) {
            showLoginMessage(loginError, firstError(body) || 'Kayıt başarısız. Kullanıcı adı alınmış olabilir.');
            return;
        }

        setLoginMode('signin');
        showLoginMessage(loginSuccess, 'Kayıt başarılı! Şimdi giriş yapabilirsiniz.');
        document.getElementById('loginUsername').value = document.getElementById('regUsername').value.trim();
        document.getElementById('regPassword').value = '';
    } catch (err) {
        showLoginMessage(loginError, `Backend'e ulaşılamadı: ${err.message}`);
    } finally {
        signUpBtn.disabled = false;
        signUpBtn.textContent = 'KAYIT OL';
    }
});

document.getElementById('logoutBtn').addEventListener('click', () => exitApp());

/**
 * ResponseDTO<T> hata mesajını çıkarır. Middleware düz metin döndürdüğünde
 * gövde JSON olmayabilir; o durumda çağıran taraf kendi mesajını verir.
 */
function firstError(body) {
    if (!body) return null;
    if (Array.isArray(body.errors) && body.errors.length > 0) return body.errors[0];
    return body.message || null;
}

/**
 * Token'lı istek. 401 dönerse oturum düşmüştür; kullanıcı giriş ekranına alınır.
 */
async function authFetch(url, options = {}) {
    const res = await fetch(url, {
        ...options,
        headers: {
            ...(options.headers || {}),
            'Authorization': `Bearer ${auth?.token || ''}`
        }
    });

    if (res.status === 401) {
        exitApp('Oturumunuz sona erdi. Lütfen tekrar giriş yapın.');
        throw new Error('unauthorized');
    }

    return res;
}

/* ============================================
   Sayfa Geçişi
   ============================================ */

const pages = {
    transactions: document.getElementById('pageTransactions'),
    rules: document.getElementById('pageRules')
};

document.querySelectorAll('.page-nav-btn').forEach(btn => {
    btn.addEventListener('click', () => switchPage(btn.dataset.page));
});

function switchPage(name) {
    document.querySelectorAll('.page-nav-btn').forEach(btn => {
        btn.classList.toggle('active', btn.dataset.page === name);
    });
    Object.entries(pages).forEach(([key, el]) => {
        el.classList.toggle('hidden', key !== name);
    });

    // Kural listesi başka bir sekmeden değişmiş olabilir; sayfaya her girişte tazelenir.
    if (name === 'rules') loadRules();
}

/* ============================================
   Bildirim
   ============================================ */

const toastStack = document.getElementById('toastStack');

function toast(message, kind = 'ok', duration = 5000) {
    const el = document.createElement('div');
    el.className = `toast ${kind}`;
    el.innerHTML = `
        <span class="toast-icon">${kind === 'ok' ? '✅' : '⚠️'}</span>
        <span>${escapeHtml(message)}</span>
    `;
    toastStack.appendChild(el);
    setTimeout(() => el.remove(), duration);
}

function escapeHtml(value) {
    return String(value ?? '')
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#39;');
}

/* ============================================
   Kural Yönetimi
   ============================================ */

const ruleForm = document.getElementById('ruleForm');
const ruleSubmitBtn = document.getElementById('ruleSubmitBtn');
const rulesList = document.getElementById('rulesList');
const ruleCountBadge = document.getElementById('ruleCount');
const ruleSearchInput = document.getElementById('ruleSearch');
const expressionInput = document.getElementById('ruleExpression');
const expressionResult = document.getElementById('expressionResult');
const validateExprBtn = document.getElementById('validateExprBtn');

let rules = [];
let ruleFilter = 'all';
let ruleSearch = '';

// ─── Katalog ───
async function loadRules() {
    if (!auth) return;

    try {
        const res = await authFetch(`${RULES_API}/all-rules`);
        const body = await res.json();

        if (!body.isSuccess) {
            toast(firstError(body) || 'Kural listesi alınamadı.', 'bad');
            return;
        }

        rules = body.data || [];
        renderRules();
    } catch (err) {
        if (err.message !== 'unauthorized') {
            toast(`Kural listesi alınamadı: ${err.message}`, 'bad');
        }
    }
}

function renderRules() {
    const term = ruleSearch.trim().toLowerCase();

    const filtered = rules.filter(rule => {
        if (ruleFilter === 'active' && !rule.isActive) return false;
        if (ruleFilter === 'passive' && rule.isActive) return false;
        if (!term) return true;
        return [rule.ruleCode, rule.ruleName, rule.expression, rule.description]
            .some(field => (field || '').toLowerCase().includes(term));
    });

    ruleCountBadge.textContent = filtered.length;

    if (filtered.length === 0) {
        rulesList.innerHTML = `<div class="empty-history">${term ? 'Aramayla eşleşen kural yok' : 'Kural bulunamadı'}</div>`;
        return;
    }

    rulesList.innerHTML = filtered.map(rule => `
        <div class="rule-item ${rule.isActive ? '' : 'passive'}">
            <div class="rule-top">
                <span class="rule-code">${escapeHtml(rule.ruleCode)}</span>
                <span class="rule-score">${rule.score}P</span>
                <span class="rule-tag">${escapeHtml(rule.target)}</span>
                <span class="rule-tag">${escapeHtml(rule.category)}</span>
                ${rule.isCritical
            ? '<span class="rule-tag critical" title="Puanı güven indiriminden muaf">KESİN</span>'
            : ''}
                <div class="rule-actions">
                    <label class="switch-row" title="${rule.isActive ? 'Pasife al' : 'Aktifleştir'}">
                        <input type="checkbox" data-action="toggle" data-id="${rule.ruleId}"
                            ${rule.isActive ? 'checked' : ''}>
                        <span class="switch-track"><span class="switch-thumb"></span></span>
                    </label>
                    <button class="icon-btn danger" data-action="delete" data-id="${rule.ruleId}"
                        data-code="${escapeHtml(rule.ruleCode)}" title="Kuralı sil">
                        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"
                            stroke-linecap="round" stroke-linejoin="round">
                            <polyline points="3 6 5 6 21 6" />
                            <path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2" />
                        </svg>
                    </button>
                </div>
            </div>
            <div class="rule-name">${escapeHtml(rule.ruleName)}</div>
            <div class="rule-expression">${escapeHtml(rule.expression || '— ifade yok —')}</div>
        </div>
    `).join('');
}

// Liste yeniden çizildiği için olaylar delegasyonla bağlanır.
rulesList.addEventListener('change', (e) => {
    const input = e.target.closest('[data-action="toggle"]');
    if (input) setRuleStatus(parseInt(input.dataset.id, 10), input.checked);
});

rulesList.addEventListener('click', (e) => {
    const btn = e.target.closest('[data-action="delete"]');
    if (btn) deleteRule(parseInt(btn.dataset.id, 10), btn.dataset.code);
});

document.querySelectorAll('[data-rule-filter]').forEach(btn => {
    btn.addEventListener('click', () => {
        ruleFilter = btn.dataset.ruleFilter;
        document.querySelectorAll('[data-rule-filter]').forEach(b => {
            b.classList.toggle('active', b === btn);
        });
        renderRules();
    });
});

ruleSearchInput.addEventListener('input', () => {
    ruleSearch = ruleSearchInput.value;
    renderRules();
});

document.getElementById('refreshRules').addEventListener('click', loadRules);

// ─── Aktif / Pasif ───
async function setRuleStatus(ruleId, isActive) {
    try {
        const res = await authFetch(`${RULES_API}/rules/${ruleId}/status`, {
            method: 'PATCH',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ isActive })
        });

        const body = await res.json();

        if (!body.isSuccess) {
            toast(firstError(body) || 'Durum değiştirilemedi.', 'bad');
            loadRules(); // anahtar sunucudaki gerçek duruma geri döner
            return;
        }

        const rule = rules.find(r => r.ruleId === ruleId);
        if (rule) rule.isActive = body.data.isActive;

        renderRules();
        toast(body.message, 'ok');
    } catch (err) {
        if (err.message !== 'unauthorized') {
            toast(`Durum değiştirilemedi: ${err.message}`, 'bad');
            loadRules();
        }
    }
}

// ─── Silme ───
async function deleteRule(ruleId, ruleCode) {
    if (!confirm(`'${ruleCode}' kuralı kalıcı olarak silinecek. Onaylıyor musunuz?`)) return;

    try {
        const res = await authFetch(`${RULES_API}/rules/${ruleId}`, { method: 'DELETE' });
        const body = await res.json();

        if (!body.isSuccess) {
            // En sık sebep: kurala bağlı fraud logu var, FK silmeyi engelliyor.
            toast(firstError(body) || 'Kural silinemedi.', 'bad', 8000);
            return;
        }

        rules = rules.filter(r => r.ruleId !== ruleId);
        renderRules();
        toast(body.message, 'ok');
    } catch (err) {
        if (err.message !== 'unauthorized') {
            toast(`Kural silinemedi: ${err.message}`, 'bad');
        }
    }
}

// ─── İfade doğrulama ───
validateExprBtn.addEventListener('click', () => validateExpression(true));

async function validateExpression(showToast) {
    const expression = expressionInput.value.trim();

    if (!expression) {
        setExpressionResult(false, 'İfade boş olamaz.');
        return false;
    }

    validateExprBtn.disabled = true;

    try {
        const res = await authFetch(`${RULES_API}/validate-expression`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ expression })
        });

        const body = await res.json();
        const isValid = body.isSuccess && body.data?.isValid;

        setExpressionResult(isValid, isValid ? 'İfade geçerli.' : (body.data?.error || firstError(body)));

        if (showToast && isValid) toast('İfade derlendi, kullanılabilir.', 'ok', 3000);
        return isValid;
    } catch (err) {
        if (err.message !== 'unauthorized') {
            setExpressionResult(false, err.message);
        }
        return false;
    } finally {
        validateExprBtn.disabled = false;
    }
}

function setExpressionResult(isValid, text) {
    expressionResult.className = `expression-result ${isValid ? 'ok' : 'bad'}`;
    expressionResult.textContent = text || '';
    expressionResult.classList.toggle('hidden', !text);
}

expressionInput.addEventListener('input', () => {
    expressionResult.classList.add('hidden');
});

// ─── Kural ekleme ───
ruleForm.addEventListener('submit', async (e) => {
    e.preventDefault();

    // Backend zaten derlemeden kaydetmiyor; burada önden doğrulayıp
    // kullanıcıya hatayı ifade alanının yanında gösteriyoruz.
    const isValid = await validateExpression(false);
    if (!isValid) {
        toast('İfade derlenemedi, kural kaydedilmedi.', 'bad');
        return;
    }

    const payload = {
        ruleCode: document.getElementById('ruleCode').value.trim(),
        ruleName: document.getElementById('ruleName').value.trim(),
        description: document.getElementById('ruleDescription').value.trim() || null,
        expression: expressionInput.value.trim(),
        score: parseInt(document.getElementById('ruleScore').value, 10),
        target: document.getElementById('ruleTarget').value,
        category: document.getElementById('ruleCategory').value,
        isCritical: document.getElementById('ruleIsCritical').checked,
        isActive: document.getElementById('ruleIsActive').checked
    };

    ruleSubmitBtn.classList.add('loading');

    try {
        const res = await authFetch(`${RULES_API}/rules`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });

        const body = await res.json();

        if (!body.isSuccess) {
            toast(firstError(body) || 'Kural eklenemedi.', 'bad', 8000);
            return;
        }

        toast(body.data?.message || body.message, 'ok');
        ruleForm.reset();
        document.getElementById('ruleScore').value = '35';
        document.getElementById('ruleIsActive').checked = true;
        setExpressionResult(true, '');
        await loadRules();
    } catch (err) {
        if (err.message !== 'unauthorized') {
            toast(`Kural eklenemedi: ${err.message}`, 'bad');
        }
    } finally {
        ruleSubmitBtn.classList.remove('loading');
    }
});

/* ============================================
   Kullanılabilir Alanlar
   ============================================ */

const fieldsBody = document.getElementById('fieldsBody');
const fieldsList = document.getElementById('fieldsList');
const fieldSearchInput = document.getElementById('fieldSearch');
const toggleFieldsBtn = document.getElementById('toggleFields');

let fields = [];
let fieldSearch = '';

toggleFieldsBtn.addEventListener('click', () => {
    const willShow = fieldsBody.classList.contains('hidden');
    fieldsBody.classList.toggle('hidden', !willShow);
    toggleFieldsBtn.textContent = willShow ? 'Gizle' : 'Göster';
});

fieldSearchInput.addEventListener('input', () => {
    fieldSearch = fieldSearchInput.value;
    renderFields();
});

async function loadFields() {
    if (!auth) return;

    try {
        const res = await authFetch(`${RULES_API}/available-fields`);
        const body = await res.json();
        if (!body.isSuccess) return;

        fields = body.data || [];
        renderFields();
    } catch {
        // Alan listesi yardımcı bilgidir; alınamaması kural yazmayı engellemez.
    }
}

function renderFields() {
    const term = fieldSearch.trim().toLowerCase();
    const filtered = term
        ? fields.filter(f => f.name.toLowerCase().includes(term))
        : fields;

    if (filtered.length === 0) {
        fieldsList.innerHTML = '<div class="empty-history">Eşleşen alan yok</div>';
        return;
    }

    fieldsList.innerHTML = filtered.map(field => `
        <div class="field-row ${field.isPopulated ? '' : 'unpopulated'}"
             data-field="${escapeHtml(field.name)}"
             title="${field.isPopulated ? 'İfadeye eklemek için tıklayın' : escapeHtml(field.note || 'Çalışma anında dolmuyor')}">
            ${field.isPopulated ? '' : '<span class="field-warn">⚠️</span>'}
            <span class="field-name">input.${escapeHtml(field.name)}</span>
            <span class="field-type">${escapeHtml(field.type)}</span>
        </div>
    `).join('');
}

// Alana tıklayınca ifadenin imleç konumuna eklenir.
fieldsList.addEventListener('click', (e) => {
    const row = e.target.closest('[data-field]');
    if (!row) return;

    const snippet = `input.${row.dataset.field}`;
    const start = expressionInput.selectionStart ?? expressionInput.value.length;
    const end = expressionInput.selectionEnd ?? expressionInput.value.length;

    expressionInput.value =
        expressionInput.value.slice(0, start) + snippet + expressionInput.value.slice(end);

    expressionInput.focus();
    expressionInput.selectionStart = expressionInput.selectionEnd = start + snippet.length;
    expressionResult.classList.add('hidden');
});

/* ============================================
   Açılış
   ============================================ */

(function bootstrap() {
    const stored = readStoredAuth();

    if (!stored?.token) {
        setLoginMode('signin');
        return;
    }

    // Saklanan token'ın süresi dolmuş olabilir; authFetch 401'de giriş ekranına döner.
    auth = stored;
    enterApp();
})();

/* ============================================
   Üye İşyeri Seçimi
   ============================================ */

const merchantSelect = document.getElementById('merchantId');
const merchantMeta = document.getElementById('merchantMeta');
const merchantGroup = document.getElementById('merchantGroup');

let merchants = [];

async function loadMerchants() {
    if (!auth) return;

    try {
        const res = await authFetch('/api/Merchant');
        const body = await res.json();
        if (!body.isSuccess) return;

        merchants = body.data || [];

        merchantSelect.innerHTML =
            '<option value="">— İşyeri seçilmedi —</option>' +
            merchants.map(m =>
                `<option value="${escapeHtml(m.merchantId)}">${escapeHtml(m.merchantName)} · ${escapeHtml(m.merchantCategory)}</option>`
            ).join('');
    } catch {
        // İşyeri listesi alınamazsa seçici boş kalır; işlem yine de gönderilebilir.
    }
}

/**
 * İşyeri seçilince kategoriyi işyerinin kendi kategorisine sabitler.
 * İkisinin çelişmesi, kategori bazlı kuralların işyeri verisiyle tutarsız
 * çalışmasına yol açardı.
 */
merchantSelect.addEventListener('change', () => {
    const merchant = merchants.find(m => m.merchantId === merchantSelect.value);

    if (!merchant) {
        merchantCategorySelect.disabled = false;
        merchantMeta.classList.add('hidden');
        return;
    }

    const option = [...merchantCategorySelect.options]
        .find(o => o.value === merchant.merchantCategory);

    if (option) {
        merchantCategorySelect.value = merchant.merchantCategory;
    } else {
        // Seed'de olup kategori listesinde bulunmayan işyerleri (Kripto, Bahis) için
        // seçenek anında eklenir; aksi halde kategori eski değerinde kalırdı.
        const created = document.createElement('option');
        created.value = merchant.merchantCategory;
        created.textContent = merchant.merchantCategory;
        merchantCategorySelect.appendChild(created);
        merchantCategorySelect.value = merchant.merchantCategory;
    }

    merchantCategorySelect.disabled = true;

    // Sunucudan geliyor: kural motorundaki IsyeriYasiGun ile aynı hesap,
    // istemcide tekrar hesaplanırsa saat dilimi farkıyla sapabilir.
    const posAge = merchant.posAgeDays;

    merchantMeta.innerHTML = `
        <span>MCC <strong>${escapeHtml(merchant.mccCode)}</strong></span>
        <span>${escapeHtml(merchant.city)}</span>
        <span>POS yaşı <strong>${posAge} gün</strong>${posAge <= 30 ? ' ⚠️ yeni işyeri' : ''}</span>
    `;
    merchantMeta.classList.remove('hidden');
});

/* ============================================
   Fraud Skor Kırılımı
   ============================================ */

const DECISION_LABELS = {
    NORMAL: { text: 'NORMAL', cls: 'approved' },
    IZLE: { text: 'İZLE', cls: 'suspicious' },
    EK_DOGRULAMA: { text: 'EK DOĞRULAMA', cls: 'suspicious' },
    RET_BLOKE: { text: 'RET / BLOKE', cls: 'declined' }
};

/**
 * Kararın nasıl oluştuğunu gösterir: hangi kurallar tetiklendi, kaç puan yazdılar,
 * güven indirimi ne kadarını götürdü. Kural yazarken "kuralım çalıştı mı" sorusunun
 * cevabı burasıdır — tetiklenen bir kural, karar NORMAL kalsa bile listede görünür.
 */
function renderFraudBreakdown(r) {
    if (!r || r.decision === undefined || r.decision === null) return '';

    const triggered = r.triggeredRules || [];
    const failures = r.ruleFailures || [];
    const combos = r.appliedCombinations || [];
    const trust = r.trustFactors || [];

    const decision = DECISION_LABELS[r.decision] || { text: r.decision, cls: 'declined' };

    let html = `
        <div class="history-detail-item full-width fraud-block">
            <span class="history-detail-label">Fraud Değerlendirmesi</span>
            <div class="fraud-summary">
                <span class="fraud-decision ${decision.cls}">${decision.text}</span>
                <span class="fraud-score">Skor <strong>${r.riskScore ?? 0}</strong></span>
            </div>
            <div class="fraud-math">
                ham <strong>${r.rawRuleScore ?? 0}</strong>
                ${combos.length ? ` + bonus <strong>${r.totalBonusScore ?? 0}</strong>` : ''}
                − güven indirimi <strong>${r.totalTrustDiscount ?? 0}</strong>
                = <strong>${r.riskScore ?? 0}</strong>
            </div>
    `;

    if (triggered.length > 0) {
        html += `
            <div class="fraud-rules">
                ${triggered.map(rule => `
                    <span class="fraud-rule-badge ${rule.isCritical ? 'critical' : ''}"
                          title="${escapeHtml(rule.reason || rule.ruleName || '')}${rule.isCritical ? ' — kesin kural, puanı indirimden muaf' : ''}">
                        ${escapeHtml(rule.ruleCode)}
                        <em>${rule.score}P</em>
                        <i>${escapeHtml(rule.target || '')}</i>
                        ${rule.isCritical ? '<b>KESİN</b>' : ''}
                    </span>
                `).join('')}
            </div>
        `;
    } else {
        html += '<div class="fraud-empty">Hiçbir kural tetiklenmedi</div>';
    }

    if (combos.length > 0) {
        html += `<div class="fraud-note">Kombinasyon: ${combos
            .map(c => `${escapeHtml(c.combinationName)} (+${c.bonusScore}P)`).join(', ')}</div>`;
    }

    if (trust.length > 0) {
        html += `<div class="fraud-note">${trust.map(t => escapeHtml(t)).join(' · ')}</div>`;
    }

    // İfadesi çalışma anında patlayan kurallar. Sessizce atlanırlar, burada görünür olmaları şart.
    if (failures.length > 0) {
        html += `
            <div class="fraud-failures">
                ⚠️ Değerlendirilemeyen kural:
                ${failures.map(f => `<code>${escapeHtml(f.ruleCode)}</code> — ${escapeHtml(f.error)}`).join('<br>')}
            </div>
        `;
    }

    html += '</div>';
    return html;
}
