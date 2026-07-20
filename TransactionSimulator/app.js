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

// ─── Tab Switching ───
tabCard.addEventListener('click', () => switchTab('card'));
tabTransfer.addEventListener('click', () => switchTab('transfer'));

// ─── Transaction Type Change Handler (Show/Hide Original Transaction ID) ───
const transactionTypeSelect = document.getElementById('transactionType');
const originalTransactionGroup = document.getElementById('originalTransactionGroup');
const originalTransactionIdInput = document.getElementById('originalTransactionId');

transactionTypeSelect.addEventListener('change', () => {
    const val = parseInt(transactionTypeSelect.value, 10);
    if (val === 2 || val === 3) {
        originalTransactionGroup.style.display = 'block';
        originalTransactionIdInput.required = true;
    } else {
        originalTransactionGroup.style.display = 'none';
        originalTransactionIdInput.required = false;
        originalTransactionIdInput.value = '';
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
        originalTransactionId: originalTransactionIdInput.value ? parseInt(originalTransactionIdInput.value, 10) : null
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

    // Show raw JSON for extra info
    const knownKeys = ['status', 'transactionId', 'amount', 'currency', 'remainingBalance', 'declineReason', 'fraudReason', 'message'];
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
        type,
        status: normalizedStatus,
        statusText: displayStatus,
        amount: payload.amount,
        currency: payload.currency || 'TRY',
        meta,
        time: new Date()
    };

    history.unshift(entry);
    if (history.length > 20) history.pop();
    renderHistory();
}

function renderHistory() {
    if (history.length === 0) {
        historyList.innerHTML = '<div class="empty-history">Henüz işlem geçmişi yok</div>';
        return;
    }

    historyList.innerHTML = history.map((item) => {
        const statusClass = item.status === 'approved' ? 'approved'
            : item.status === 'suspicious' ? 'suspicious'
                : 'declined';
        const timeStr = item.time.toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit' });

        return `
            <div class="history-item">
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
                </div>
            </div>
        `;
    }).join('');
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
