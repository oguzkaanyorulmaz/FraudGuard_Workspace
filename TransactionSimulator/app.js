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
    if (channelVal === 3) { // ATM Cihazı
        merchantCategoryGroup.style.display = 'none';
    } else {
        merchantCategoryGroup.style.display = 'block';
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
    // Update active button
    document.querySelectorAll('.history-filter-btn').forEach(btn => {
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
        if (r.declineReason && item.status !== 'Approved') detailRows += historyDetailRow('Red Sebebi', r.declineReason);
        if (r.fraudReason) detailRows += historyDetailRow('Fraud Nedeni', r.fraudReason);
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
