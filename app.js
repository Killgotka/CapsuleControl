'use strict';

const SALT = 'spief-2026';
const SESSION_MINUTES = 25;

// ── Encoding (mirrors QREncoder.cs) ──────────────────────────────
function encode(capsuleId, isExtension, startTime) {
  const unixTs = Math.floor(startTime.getTime() / 1000);
  const payload = `${capsuleId}|${isExtension ? 1 : 0}|${unixTs}|${SALT}`;

  const raw = new TextEncoder().encode(payload);
  const xored = new Uint8Array(raw.length);
  for (let i = 0; i < raw.length; i++) {
    xored[i] = (raw[i] ^ (i * 7 + 13)) & 0xff;
  }

  // Base64 → URL-safe
  let binary = '';
  xored.forEach(b => { binary += String.fromCharCode(b); });
  return btoa(binary).replace(/\+/g, '-').replace(/\//g, '_').replace(/=/g, '');
}

// ── Decoding (mirrors QRValidator.cs) ────────────────────────────
function decode(qrText) {
  let text = qrText.trim().replace(/-/g, '+').replace(/_/g, '/');
  const pad = text.length % 4;
  if (pad > 0) text += '='.repeat(4 - pad);

  let xored;
  try {
    const binary = atob(text);
    xored = new Uint8Array(binary.length);
    for (let i = 0; i < binary.length; i++) xored[i] = binary.charCodeAt(i);
  } catch {
    throw new Error('Неверный формат строки');
  }

  const raw = new Uint8Array(xored.length);
  for (let i = 0; i < xored.length; i++) {
    raw[i] = (xored[i] ^ (i * 7 + 13)) & 0xff;
  }

  const parts = new TextDecoder().decode(raw).split('|');
  if (parts.length !== 4) throw new Error('Неверные данные');
  if (parts[3] !== SALT) throw new Error('Неверная подпись');

  const capsuleId = parseInt(parts[0]);
  const isExtension = parts[1] === '1';
  const startTime = new Date(parseInt(parts[2]) * 1000);

  if (isNaN(capsuleId) || isNaN(startTime.getTime())) throw new Error('Неверные данные');

  return { capsuleId, isExtension, startTime };
}

// ── Date/time helpers ─────────────────────────────────────────────
function toDateInputValue(d) {
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, '0');
  const day = String(d.getDate()).padStart(2, '0');
  return `${y}-${m}-${day}`;
}

function toTimeInputValue(d) {
  return `${String(d.getHours()).padStart(2, '0')}:${String(d.getMinutes()).padStart(2, '0')}`;
}

function formatDateTime(d) {
  return d.toLocaleString('ru-RU', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' });
}

function addMinutes(d, mins) {
  return new Date(d.getTime() + mins * 60000);
}

// ── Set date/time inputs ──────────────────────────────────────────
function setDateTime(d) {
  document.getElementById('start-date').value = toDateInputValue(d);
  document.getElementById('start-time').value = toTimeInputValue(d);
}

function getDateTime() {
  const date = document.getElementById('start-date').value;
  const time = document.getElementById('start-time').value || '00:00';
  if (!date) return null;
  return new Date(`${date}T${time}`);
}

// ── Page tabs ─────────────────────────────────────────────────────
document.querySelectorAll('.page-tab').forEach(btn => {
  btn.addEventListener('click', () => {
    document.querySelectorAll('.page-tab').forEach(t => t.classList.remove('active'));
    document.querySelectorAll('.page').forEach(p => p.classList.remove('active'));
    btn.classList.add('active');
    document.getElementById('page-' + btn.dataset.page).classList.add('active');
  });
});

// ── Stepper ───────────────────────────────────────────────────────
const capsuleInput = document.getElementById('capsule-id');

document.getElementById('step-down').addEventListener('click', () => {
  const v = parseInt(capsuleInput.value) || 1;
  capsuleInput.value = Math.max(1, v - 1);
});

document.getElementById('step-up').addEventListener('click', () => {
  const v = parseInt(capsuleInput.value) || 1;
  capsuleInput.value = Math.min(999, v + 1);
});

capsuleInput.addEventListener('change', () => {
  let v = parseInt(capsuleInput.value);
  if (isNaN(v) || v < 1) v = 1;
  if (v > 999) v = 999;
  capsuleInput.value = v;
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

// Set "now" on load
setDateTime(new Date());

// ── QR generation ─────────────────────────────────────────────────
let lastEncodedStr = '';

const ecLevels = { H: QRCode.CorrectLevel.H };

function isMobile() { return window.innerWidth <= 680; }

function generate() {
  if (document.activeElement) document.activeElement.blur();

  const capsuleId = parseInt(capsuleInput.value);
  if (isNaN(capsuleId) || capsuleId < 1 || capsuleId > 999) {
    showToast('Введите номер капсулы от 1 до 999');
    return;
  }

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

  // Render QR into temp div
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

      // Session info
      document.getElementById('info-capsule').textContent = `№ ${capsuleId}`;
      document.getElementById('info-time').textContent = formatDateTime(startTime);
      document.getElementById('info-end').textContent = formatDateTime(endTime);
      const extRow = document.getElementById('info-ext-row');
      extRow.style.display = isExtension ? 'flex' : 'none';
      document.getElementById('session-info').style.display = 'block';
      document.getElementById('preview-actions').style.display = 'flex';

      // Encoded string
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
  } catch (e) {
    showToast('Ошибка генерации QR-кода');
  } finally {
    document.body.removeChild(tmp);
  }
}

document.getElementById('generate-btn').addEventListener('click', generate);
capsuleInput.addEventListener('keydown', e => { if (e.key === 'Enter') generate(); });

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
  const capsuleId = parseInt(capsuleInput.value) || 1;
  link.download = `qr-capsule-${capsuleId}.png`;
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

// ── Camera scanner (html5-qrcode / ZXing) ────────────────────────
let scanner = null;
let scannerRunning = false;

function showDecodeResult(qrText) {
  const resultEl = document.getElementById('decode-result');
  const errorEl  = document.getElementById('decode-error');
  resultEl.style.display = 'none';
  errorEl.style.display  = 'none';

  try {
    const { capsuleId, isExtension, startTime } = decode(qrText);
    const endTime = addMinutes(startTime, SESSION_MINUTES);

    document.getElementById('dec-capsule').textContent = `№ ${capsuleId}`;
    document.getElementById('dec-time').textContent    = formatDateTime(startTime);
    document.getElementById('dec-end').textContent     = formatDateTime(endTime);
    document.getElementById('dec-type').textContent    = isExtension ? 'Продление' : 'Основная';

    resultEl.style.display = 'block';
    resultEl.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
  } catch {
    document.getElementById('decode-error-text').textContent = 'Неверная строка или подпись';
    errorEl.style.display = 'flex';
  }
}

async function stopCamera() {
  if (scanner && scannerRunning) {
    try { await scanner.stop(); } catch {}
    scannerRunning = false;
  }
  document.getElementById('scan-active').style.display = 'none';
  document.getElementById('scan-idle').style.display   = 'block';
}

async function startCamera() {
  document.getElementById('decode-result').style.display = 'none';
  document.getElementById('decode-error').style.display  = 'none';
  document.getElementById('scan-idle').style.display     = 'none';
  document.getElementById('scan-manual').style.display   = 'none';
  document.getElementById('scan-active').style.display   = 'block';

  if (!scanner) {
    scanner = new Html5Qrcode('qr-reader', { verbose: false });
  }

  try {
    await scanner.start(
      { facingMode: 'environment' },
      {
        fps: 15,
        // Фиксированный qrbox — функция ломается на iOS Safari
        qrbox: { width: 250, height: 250 },
        // Использует нативный BarcodeDetector на iOS 17+ и Chrome Android
        experimentalFeatures: { useBarCodeDetectorIfSupported: true },
        // НЕ передаём aspectRatio — ломает видеопоток на iOS
      },
      (decodedText) => {
        stopCamera();
        if (navigator.vibrate) navigator.vibrate(60);
        showDecodeResult(decodedText);
      },
      () => {} // per-frame miss — норма пока QR не в кадре
    );
    scannerRunning = true;
  } catch (err) {
    scannerRunning = false;
    document.getElementById('scan-active').style.display = 'none';
    // iOS может не поддерживать getUserMedia в Safari — показываем кнопку фото
    showIdle();
    showToast('Попробуйте кнопку «Сфотографировать»');
  }
}

function showManual() {
  stopCamera();
  document.getElementById('scan-idle').style.display   = 'none';
  document.getElementById('scan-manual').style.display = 'block';
}

function showIdle() {
  stopCamera();
  document.getElementById('scan-idle').style.display   = 'block';
  document.getElementById('scan-manual').style.display = 'none';
}

// «Сфотографировать» — нативная камера iOS, потом декодируем файл
document.getElementById('photo-btn').addEventListener('click', () => {
  document.getElementById('photo-input').click();
});

document.getElementById('photo-input').addEventListener('change', async (e) => {
  const file = e.target.files[0];
  if (!file) return;
  e.target.value = ''; // сброс чтобы можно было снова выбрать

  document.getElementById('decode-result').style.display = 'none';
  document.getElementById('decode-error').style.display  = 'none';

  if (!scanner) scanner = new Html5Qrcode('qr-reader', { verbose: false });

  try {
    const text = await scanner.scanFile(file, false);
    if (navigator.vibrate) navigator.vibrate(60);
    showDecodeResult(text);
  } catch {
    document.getElementById('decode-error-text').textContent = 'QR-код не найден на фото';
    document.getElementById('decode-error').style.display = 'flex';
  }
});

document.getElementById('start-scan-btn').addEventListener('click', startCamera);
document.getElementById('stop-scan-btn').addEventListener('click', stopCamera);
document.getElementById('manual-toggle-btn').addEventListener('click', showManual);
document.getElementById('camera-toggle-btn').addEventListener('click', showIdle);
document.getElementById('scan-again-btn').addEventListener('click', () => {
  document.getElementById('decode-result').style.display = 'none';
  startCamera();
});

// Останавливаем камеру при переходе на другую вкладку
document.querySelectorAll('.page-tab').forEach(btn => {
  btn.addEventListener('click', () => {
    if (btn.dataset.page !== 'decode') stopCamera();
  });
});

// ── Manual decode ─────────────────────────────────────────────────
document.getElementById('decode-btn').addEventListener('click', () => {
  if (document.activeElement) document.activeElement.blur();
  const input = document.getElementById('decode-input').value.trim();
  if (!input) { showToast('Вставьте строку QR-кода'); return; }
  showDecodeResult(input);
});

// ── Toast ─────────────────────────────────────────────────────────
function showToast(msg) {
  const toast = document.getElementById('toast');
  toast.textContent = msg;
  toast.classList.add('show');
  clearTimeout(toast._t);
  toast._t = setTimeout(() => toast.classList.remove('show'), 2800);
}
