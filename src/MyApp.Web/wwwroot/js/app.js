const messagesEl = document.getElementById('messages');
const formEl = document.getElementById('chatForm');
const inputEl = document.getElementById('promptInput');
const sendBtn = document.getElementById('sendBtn');
const ordersListEl = document.getElementById('ordersList');
const sessionLabel = document.getElementById('sessionLabel');

let currentSessionId = null;

function appendMessage(html) {
  messagesEl.insertAdjacentHTML('beforeend', html);
  messagesEl.scrollTop = messagesEl.scrollHeight;
  return messagesEl.lastElementChild;
}

async function loadOrders() {
  const res = await fetch('/orders/partial');
  if (res.ok) ordersListEl.innerHTML = await res.text();
}

async function sendPrompt(text, endpoint = '/chat/turn') {
  const prompt = text.trim();
  if (!prompt) return;

  const userEl = appendMessage('<div class="msg user"><span class="role">Você</span></div>');
  userEl.appendChild(document.createTextNode(prompt));

  const pendingEl = appendMessage(
    '<div class="msg bot"><span class="role">OrderAssistant</span><div class="activity-bar"><span class="spinner"></span><span>Processando...</span></div></div>'
  );

  inputEl.value = '';
  sendBtn.disabled = true;

  try {
    const res = await fetch(endpoint, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ message: prompt, sessionId: currentSessionId })
    });

    if (!res.ok) throw new Error(`Erro HTTP ${res.status}`);

    pendingEl.outerHTML = await res.text();
    const botEl = messagesEl.lastElementChild;
    if (botEl?.dataset.sessionId) {
      currentSessionId = botEl.dataset.sessionId;
      sessionLabel.textContent = `Sessão: ${currentSessionId}`;
    }

    const ordersOob = botEl?.querySelector('template[data-orders-oob]');
    if (ordersOob) {
      ordersListEl.innerHTML = ordersOob.innerHTML;
      ordersOob.remove();
    }

    messagesEl.scrollTop = messagesEl.scrollHeight;
  } catch (err) {
    pendingEl.className = 'msg error';
    pendingEl.textContent = err.message;
  } finally {
    sendBtn.disabled = false;
    inputEl.focus();
  }
}

// Event delegation: target endpoint comes explicitly from the HTML (action / data-chat-action)
messagesEl.addEventListener('submit', (e) => {
  const form = e.target.closest('form[data-chat-form]');
  if (!form) return;

  e.preventDefault();
  const endpoint = form.getAttribute('action') || '/chat/turn';
  const parts = [...new FormData(form).entries()].map(([k, v]) => `${k}: ${String(v).trim()}`);
  form.querySelectorAll('input, button').forEach((el) => (el.disabled = true));
  sendPrompt(parts.join(', '), endpoint);
});

messagesEl.addEventListener('click', (e) => {
  const btn = e.target.closest('button[data-chat-message]');
  if (!btn) return;

  const endpoint = btn.dataset.chatAction || '/chat/turn';
  btn.closest('[data-ui-choices]')?.querySelectorAll('button').forEach((b) => (b.disabled = true));
  sendPrompt(btn.dataset.chatMessage, endpoint);
});

formEl.addEventListener('submit', (e) => {
  e.preventDefault();
  sendPrompt(inputEl.value, formEl.getAttribute('action') || '/chat/turn');
});

document.querySelectorAll('.chip').forEach((btn) =>
  btn.addEventListener('click', () => sendPrompt(btn.dataset.prompt))
);

document.getElementById('newSessionBtn').addEventListener('click', () => {
  currentSessionId = null;
  sessionLabel.textContent = 'Sessão: nova';
  messagesEl.innerHTML = '<div class="msg bot"><span class="role">OrderAssistant</span>Nova conversa iniciada. Como posso ajudar?</div>';
});

document.getElementById('refreshOrders').addEventListener('click', loadOrders);
