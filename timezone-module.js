'use strict';

const TIMEZONE_KEY = 'app_tz_offset';

const TIMEZONES = [
  { label: 'UTC+0 — Лондон',        offset: 0  },
  { label: 'UTC+1 — Берлин',        offset: 1  },
  { label: 'UTC+2 — Калининград',   offset: 2  },
  { label: 'UTC+3 — Москва',        offset: 3  },
  { label: 'UTC+4 — Самара',        offset: 4  },
  { label: 'UTC+5 — Екатеринбург',  offset: 5  },
  { label: 'UTC+6 — Омск',          offset: 6  },
  { label: 'UTC+7 — Красноярск',    offset: 7  },
  { label: 'UTC+8 — Иркутск',       offset: 8  },
  { label: 'UTC+9 — Якутск / Чита', offset: 9  },
  { label: 'UTC+10 — Владивосток',  offset: 10 },
  { label: 'UTC+11 — Магадан',      offset: 11 },
  { label: 'UTC+12 — Камчатка',     offset: 12 },
];

function getTzOffset() {
  const v = localStorage.getItem(TIMEZONE_KEY);
  return v !== null ? parseInt(v, 10) : null;
}

function saveTzOffset(offset) {
  localStorage.setItem(TIMEZONE_KEY, String(offset));
}

/**
 * Форматирует Date в DD.MM.YYYY, HH:MM в заданном UTC-offset.
 * Использует UTC-поля сдвинутой даты — результат одинаков в любом браузере.
 */
function fmt(date, tzOffset = 0) {
  const p = n => String(n).padStart(2, '0');
  const shifted = new Date(date.getTime() + tzOffset * 3_600_000);
  return (
    `${p(shifted.getUTCDate())}.${p(shifted.getUTCMonth() + 1)}.${shifted.getUTCFullYear()}, ` +
    `${p(shifted.getUTCHours())}:${p(shifted.getUTCMinutes())}`
  );
}

function updateTzIndicator() {
  const el = document.getElementById('tz-indicator');
  if (!el) return;
  const tz = getTzOffset();
  el.textContent = tz !== null ? (tz >= 0 ? `UTC+${tz}` : `UTC${tz}`) : 'UTC?';
}

function buildTzList() {
  const list    = document.getElementById('tz-list');
  const current = getTzOffset();

  list.innerHTML = TIMEZONES.map(tz => `
    <li class="tz-item${tz.offset === current ? ' active' : ''}" data-offset="${tz.offset}">
      <span>${tz.label}</span>
      ${tz.offset === current
        ? '<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><polyline points="20 6 9 17 4 12"/></svg>'
        : ''}
    </li>
  `).join('');

  list.querySelectorAll('.tz-item').forEach(item => {
    item.addEventListener('click', () => {
      const offset = parseInt(item.dataset.offset, 10);
      saveTzOffset(offset);
      updateTzIndicator();
      window.dispatchEvent(new CustomEvent('tz-changed', { detail: { offset } }));
      closeSettings();
    });
  });
}

function openSettings() {
  buildTzList();
  document.getElementById('settings-modal').style.display = 'flex';
}

function closeSettings() {
  document.getElementById('settings-modal').style.display = 'none';
}

function initTimezone() {
  document.getElementById('btn-settings')
    ?.addEventListener('click', openSettings);

  document.getElementById('btn-settings-close')
    ?.addEventListener('click', closeSettings);

  document.getElementById('settings-modal')
    ?.addEventListener('click', e => {
      if (e.target === document.getElementById('settings-modal')) closeSettings();
    });

  updateTzIndicator();

  if (getTzOffset() === null) openSettings();
}
