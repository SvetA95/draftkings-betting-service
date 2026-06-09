const API = '/api';
let currentUser = null;
let selectedSelection = null;

async function init() {
    const res = await fetch(`${API}/users`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ name: 'Demo User', balance: 100 })
    });
    currentUser = await res.json();
    updateBalance();
    loadMarkets();
    loadBets();

    document.getElementById('stake-input').addEventListener('input', updatePayout);
    document.getElementById('place-bet-btn').addEventListener('click', placeBet);
}

async function updateBalance() {
    const res = await fetch(`${API}/users/${currentUser.id}/balance`);
    const data = await res.json();
    document.querySelector('[data-testid="user-balance"]').textContent =
        `£${data.amount.toFixed(2)}`;
}

async function loadMarkets() {
    const container = document.getElementById('markets-list');
    container.innerHTML = '<p style="color:#666">Loading markets...</p>';

    // Create a demo market if none exist
    const marketRes = await fetch(`${API}/markets`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
            name: 'Match Winner',
            eventId: crypto.randomUUID(),
            eventName: 'Team A vs Team B',
            selections: [
                { name: 'Team A', odds: 2.50 },
                { name: 'Team B', odds: 1.80 },
                { name: 'Draw', odds: 3.20 }
            ]
        })
    });
    const market = await marketRes.json();
    renderMarket(container, market);
}

function renderMarket(container, market) {
    const stateClass = market.state.toLowerCase();
    const isSuspended = stateClass === 'suspended' || stateClass === 'closed';

    container.innerHTML = `
        <div class="market-card">
            <div class="market-header">
                <span class="market-name">${market.name}</span>
                <span class="market-state ${stateClass}">${market.state}</span>
            </div>
            <div class="selections">
                ${market.selections.map(s => `
                    <button class="selection-btn"
                            data-selection-id="${s.id}"
                            data-selection-name="${s.name}"
                            data-selection-odds="${s.odds}"
                            ${isSuspended ? 'disabled' : ''}>
                        <span class="selection-name">${s.name}</span>
                        <span class="selection-odds">${s.odds.toFixed(2)}</span>
                    </button>
                `).join('')}
            </div>
        </div>
    `;

    container.querySelectorAll('.selection-btn').forEach(btn => {
        btn.addEventListener('click', () => selectSelection(btn));
    });
}

function selectSelection(btn) {
    document.querySelectorAll('.selection-btn').forEach(b => b.classList.remove('selected'));
    btn.classList.add('selected');

    selectedSelection = {
        id: btn.dataset.selectionId,
        name: btn.dataset.selectionName,
        odds: parseFloat(btn.dataset.selectionOdds)
    };

    document.getElementById('betslip-empty').hidden = true;
    document.getElementById('betslip-content').hidden = false;
    document.getElementById('bet-confirmation').hidden = true;
    document.getElementById('betslip-selection-name').textContent = selectedSelection.name;
    document.getElementById('betslip-odds').textContent = selectedSelection.odds.toFixed(2);
    updatePayout();
}

function updatePayout() {
    const stake = parseFloat(document.getElementById('stake-input').value) || 0;
    const odds = selectedSelection ? selectedSelection.odds : 0;
    document.getElementById('potential-payout').textContent = `£${(stake * odds).toFixed(2)}`;
}

async function placeBet() {
    if (!selectedSelection) return;
    const stake = parseFloat(document.getElementById('stake-input').value);
    if (!stake || stake <= 0) return;

    const res = await fetch(`${API}/users/${currentUser.id}/bets`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ selectionId: selectedSelection.id, stake })
    });

    if (res.ok) {
        document.getElementById('betslip-content').hidden = true;
        document.getElementById('bet-confirmation').hidden = false;
        selectedSelection = null;
        document.querySelectorAll('.selection-btn').forEach(b => b.classList.remove('selected'));

        setTimeout(() => {
            updateBalance();
            loadBets();
        }, 2500);
    }
}

async function loadBets() {
    if (!currentUser) return;
    const res = await fetch(`${API}/users/${currentUser.id}/bets`);
    const bets = await res.json();
    const container = document.getElementById('bets-list');

    if (bets.length === 0) {
        container.innerHTML = '<p style="color:#666">No bets placed yet</p>';
        return;
    }

    container.innerHTML = bets.map(bet => `
        <div class="bet-item">
            <div class="bet-details">
                <div class="bet-selection">${bet.selectionName} @ ${bet.odds.toFixed(2)}</div>
                <div class="bet-info">Stake: £${bet.stake.toFixed(2)}</div>
            </div>
            <span class="bet-state ${bet.state.toLowerCase()}">${bet.state}</span>
            ${bet.payout && bet.payout > 0 ? `<span class="bet-payout">+£${bet.payout.toFixed(2)}</span>` : ''}
        </div>
    `).join('');
}

// Poll for bet state changes
setInterval(() => {
    if (currentUser) {
        updateBalance();
        loadBets();
    }
}, 3000);

init();
