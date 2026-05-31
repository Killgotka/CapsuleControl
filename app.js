'use strict';

const SALT = 'spief-2026';
const SESSION_MINUTES = 25;

const CAPSULE_NAMES = { 0: 'Конгресс', 1: 'F1', 2: 'F2', 3: 'Пресс центр' };

// ── Encoding (mirrors QREncoder.cs) ──────────────────────────────
function encode(capsuleId, isExtension, startTime) {
  const unixTs = Math.floor(startTime.getTime() / 1000);
  const payload = `${capsuleId}|${isExtension ? 1 : 0}|${unixTs}|${SALT}`;

  const raw = new TextEncoder().encode(payload);
  const xored = new Uint8Array(raw.length);
  for (let i = 0; i < raw.length; i++) {
    xored[i] = (raw[i] ^ (i * 7 + 13)) & 0xff;
  }

  let binary = '';
  xored.forEach(b => { binary += String.fromCharCode(b); });
  return btoa(binary).replace(/\+/g, '-').replace(/\//g, '_').replace(/=/g, '');
}

// ── Date/time helpers ─────────────────────────────────────────────
function toDateInputValue(d) {
  const tz = getTzOffset() ?? 0;
  const shifted = new Date(d.getTime() + tz * 3_600_000);
  const y   = shifted.getUTCFullYear();
  const m   = String(shifted.getUTCMonth() + 1).padStart(2, '0');
  const day = String(shifted.getUTCDate()).padStart(2, '0');
  return `${y}-${m}-${day}`;
}

function toTimeInputValue(d) {
  const tz = getTzOffset() ?? 0;
  const shifted = new Date(d.getTime() + tz * 3_600_000);
  return `${String(shifted.getUTCHours()).padStart(2, '0')}:${String(shifted.getUTCMinutes()).padStart(2, '0')}`;
}

function addMinutes(d, mins) {
  return new Date(d.getTime() + mins * 60000);
}

function setDateTime(d) {
  document.getElementById('start-date').value = toDateInputValue(d);
  document.getElementById('start-time').value = toTimeInputValue(d);
}

function getDateTime() {
  const date = document.getElementById('start-date').value;
  const time = document.getElementById('start-time').value || '00:00';
  if (!date) return null;
  const [y, m, d] = date.split('-').map(Number);
  const [h, min]  = time.split(':').map(Number);
  const tz = getTzOffset() ?? 0;
  return new Date(Date.UTC(y, m - 1, d, h, min, 0) - tz * 3_600_000);
}

// ── Capsule picker ────────────────────────────────────────────────
let selectedCapsule = 0;

document.querySelectorAll('.capsule-btn').forEach(btn => {
  btn.addEventListener('click', () => {
    document.querySelectorAll('.capsule-btn').forEach(b => b.classList.remove('active'));
    btn.classList.add('active');
    selectedCapsule = parseInt(btn.dataset.capsule);
  });
});

// ── Quick time buttons ────────────────────────────────────────────
document.getElementById('btn-now').addEventListener('click', () => setDateTime(new Date()));
document.getElementById('btn-30').addEventListener('click', () => {
  const base = getDateTime() || new Date();
  setDateTime(addMinutes(base, 30));
});
document.getElementById('btn-60').addEventListener('click', () => {
  const base = getDateTime() || new Date();
  setDateTime(addMinutes(base, 60));
});

// Init timezone, then seed inputs with current time in selected TZ
initTimezone();
setDateTime(new Date());
window.addEventListener('tz-changed', () => setDateTime(new Date()));

// ── QR generation ─────────────────────────────────────────────────
let lastEncodedStr = '';

function isMobile() { return window.innerWidth <= 680; }

function generate() {
  if (document.activeElement) document.activeElement.blur();

  const capsuleId = selectedCapsule;
  const startTime = getDateTime();
  if (!startTime || isNaN(startTime.getTime())) {
    showToast('Укажите дату и время начала');
    return;
  }

  const isExtension = document.getElementById('is-extension').checked;
  const encoded = encode(capsuleId, isExtension, startTime);
  lastEncodedStr = encoded;

  const endTime = addMinutes(startTime, SESSION_MINUTES);
  const canvas = document.getElementById('qr-canvas');
  const placeholder = document.getElementById('qr-placeholder');
  const area = document.getElementById('qr-area');

  const tmp = document.createElement('div');
  tmp.style.cssText = 'position:absolute;visibility:hidden;left:-9999px';
  document.body.appendChild(tmp);

  try {
    new QRCode(tmp, {
      text: encoded,
      width: 256,
      height: 256,
      colorDark: '#0f172a',
      colorLight: '#ffffff',
      correctLevel: QRCode.CorrectLevel.H,
    });

    const src = tmp.querySelector('canvas') || tmp.querySelector('img');

    const drawQR = () => {
      canvas.width = 256;
      canvas.height = 256;
      const ctx = canvas.getContext('2d');
      ctx.fillStyle = '#ffffff';
      ctx.fillRect(0, 0, 256, 256);
      ctx.drawImage(src, 0, 0, 256, 256);

      placeholder.style.display = 'none';
      canvas.style.display = 'block';
      area.classList.add('has-qr');

      const tz = getTzOffset() ?? 0;
      document.getElementById('info-capsule').innerHTML =
        `${CAPSULE_NAMES[capsuleId] ?? '—'}<span class="info-capsule-id">${capsuleId}</span>`;
      document.getElementById('info-time').textContent = fmt(startTime, tz);
      document.getElementById('info-end').textContent  = fmt(endTime,   tz);
      document.getElementById('info-ext-row').style.display = isExtension ? 'flex' : 'none';
      document.getElementById('session-info').style.display = 'block';
      document.getElementById('preview-actions').style.display = 'flex';

      document.getElementById('encoded-str').textContent = encoded;
      document.getElementById('encoded-wrap').style.display = 'block';

      if (isMobile()) {
        setTimeout(() => {
          document.getElementById('preview-card').scrollIntoView({ behavior: 'smooth', block: 'start' });
        }, 80);
      }
    };

    if (src.tagName === 'CANVAS') {
      drawQR();
    } else {
      if (src.complete) drawQR();
      else src.onload = drawQR;
    }
  } catch {
    showToast('Ошибка генерации QR-кода');
  } finally {
    document.body.removeChild(tmp);
  }
}

document.getElementById('generate-btn').addEventListener('click', generate);

// ── Download PNG ──────────────────────────────────────────────────
document.getElementById('download-png').addEventListener('click', () => {
  const canvas = document.getElementById('qr-canvas');
  if (!canvas || canvas.style.display === 'none') return;

  const pad = 28;
  const out = document.createElement('canvas');
  out.width = canvas.width + pad * 2;
  out.height = canvas.height + pad * 2;
  const ctx = out.getContext('2d');
  ctx.fillStyle = '#ffffff';
  ctx.fillRect(0, 0, out.width, out.height);
  ctx.drawImage(canvas, pad, pad);

  const link = document.createElement('a');
  link.download = `qr-capsule-${selectedCapsule}.png`;
  link.href = out.toDataURL('image/png');
  link.click();
  showToast('PNG сохранён');
});

// ── Copy encoded string ───────────────────────────────────────────
document.getElementById('copy-code').addEventListener('click', () => {
  if (!lastEncodedStr) return;
  navigator.clipboard.writeText(lastEncodedStr)
    .then(() => showToast('Код скопирован'))
    .catch(() => showToast('Не удалось скопировать'));
});

// ── Toast ─────────────────────────────────────────────────────────
function showToast(msg) {
  const toast = document.getElementById('toast');
  toast.textContent = msg;
  toast.classList.add('show');
  clearTimeout(toast._t);
  toast._t = setTimeout(() => toast.classList.remove('show'), 2800);
}
