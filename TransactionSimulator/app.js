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

// ─── Tab Switching ───
tabCard.addEventListener('click', () => switchTab('card'));
tabTransfer.addEventListener('click', () => switchTab('transfer'));

// --- Transaction Type Change Handler (Show/Hide RRN for Refund) ---
const transactionTypeSelect = document.getElementById('transactionType');
const rrnGroup = document.getElementById('rrnGroup');
const rrnInput = document.getElementById('rrnInput');

transactionTypeSelect.addEventListener('change', () => {
    const val = parseInt(transactionTypeSelect.value, 10);
    if (val === 2) {
        rrnGroup.style.display = 'block';
        rrnInput.required = true;
    } else {
        rrnGroup.style.display = 'none';
        rrnInput.required = false;
        rrnInput.value = '';
    }
});

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
const cardNumberInput = document.getElementById('cardNumber');
const paymentTypeSelect = document.getElementById('paymentType');

cardNumberInput.addEventListener('input', (e) => {
    let val = e.target.value.replace(/\D/g, '');

    // Kart numarasına göre otomatik algılama (4 ile başlıyorsa Banka, 5 ile başlıyorsa Kredi Kartı)
    if (val.startsWith('4')) {
        paymentTypeSelect.value = "2"; // Banka Kartı
    } else if (val.startsWith('5')) {
        paymentTypeSelect.value = "1"; // Kredi Kartı
    }

    val = val.substring(0, 16);
    let formatted = val.replace(/(.{4})/g, '$1 ').trim();
    e.target.value = formatted;
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

    const payload = {
        cardNumber: cardNumberInput.value.replace(/\s/g, ''),
        expiryDate: expiryInput.value,
        cvv: document.getElementById('cvv').value,
        amount: parseFloat(document.getElementById('cardAmount').value),
        currency: document.getElementById('cardCurrency').value,
        transactionType: parseInt(document.getElementById('transactionType').value),
        paymentType: parseInt(document.getElementById('paymentType').value),
        channelTypeId: parseInt(document.getElementById('channelTypeId').value),
        location: document.getElementById('cardLocation').value || 'Sanal POS',
        country: document.getElementById('cardCountry').value || 'Türkiye',
        merchantCategory: document.getElementById('merchantCategory').value,
        rrn: rrnInput.value || null
    };

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
    hideResponse();

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
        showResponse(merged, res.ok, type);
        addHistory(merged, type, payload);
    } catch (err) {
        showErrorResponse(err.message);
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
            if (p.transactionType) detailRows += historyDetailRow('İşlem Tipi', p.transactionType === 1 ? 'Satış' : 'İade');
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
        if (r.declineReason) detailRows += historyDetailRow('Red Sebebi', r.declineReason);
        if (r.fraudReason) detailRows += historyDetailRow('Fraud Nedeni', r.fraudReason);
        if (r.message) detailRows += historyDetailRow('Mesaj', r.message);

        return `
            <div class="history-item-wrapper" data-history-id="${item.id}">
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

function historyDetailRow(label, value) {
    return `
        <div class="history-detail-item">
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
    const wrapper = document.querySelector(`[data-history-id="${id}"]`);
    if (wrapper) {
        wrapper.classList.toggle('expanded');
    }
}

clearHistory.addEventListener('click', () => {
    history = [];
    renderHistory();
});

// ─── Backend Health Check ───
async function checkBackend() {
    try {
        const res = await fetch(`${API_BASE}/process`, {
            method: 'OPTIONS'
        });
        statusIndicator.className = 'status-indicator online';
        statusText.textContent = 'Backend Aktif';
    } catch {
        // Try another way — just attempt a HEAD or small request
        try {
            const res = await fetch('/api/transactions/process', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: '{}'
            });
            // Even if it returns 400, backend is alive
            statusIndicator.className = 'status-indicator online';
            statusText.textContent = 'Backend Aktif';
        } catch {
            statusIndicator.className = 'status-indicator offline';
            statusText.textContent = 'Backend Kapalı';
        }
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
