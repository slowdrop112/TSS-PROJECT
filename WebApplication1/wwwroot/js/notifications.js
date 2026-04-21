// Notifications dropdown (bell) - SCRUM-62/63/64
(() => {
  const bellBtn = document.getElementById('notificationBellButton');
  const menu = document.getElementById('notificationDropdownMenu');
  const itemsEl = document.getElementById('notificationItems');
  const badgeEl = document.getElementById('notificationUnreadBadge');
  const statusEl = document.getElementById('notificationStatusText');
  const tokenEl = document.getElementById('notificationAntiforgeryToken');
  const markAllBtn = document.getElementById('notificationMarkAllReadBtn');

  if (!bellBtn || !menu || !itemsEl || !badgeEl || !statusEl || !tokenEl) return;

  let isOpen = false;
  let loadedOnce = false;

  function setBadge(count) {
    if (count > 0) {
      badgeEl.textContent = String(count);
      badgeEl.classList.remove('d-none');
    } else {
      badgeEl.textContent = '0';
      badgeEl.classList.add('d-none');
    }
  }

  function escapeHtml(s) {
    return String(s)
      .replaceAll('&', '&amp;')
      .replaceAll('<', '&lt;')
      .replaceAll('>', '&gt;')
      .replaceAll('"', '&quot;')
      .replaceAll("'", '&#39;');
  }

  function formatDate(iso) {
    try {
      const d = new Date(iso);
      return d.toLocaleString('ro-RO');
    } catch {
      return '';
    }
  }

  async function fetchFeed(take = 5) {
    statusEl.textContent = 'Se încarcă…';

    const res = await fetch(`/Notifications/Feed?take=${take}`, { method: 'GET' });
    if (!res.ok) {
      statusEl.textContent = 'Eroare la încărcare.';
      itemsEl.innerHTML = '<div class="notification-empty">Nu am putut încărca notificările.</div>';
      return;
    }

    const data = await res.json();
    const unread = data.unreadCount ?? 0;
    const items = data.items ?? [];
    const currentTake = take;

    setBadge(unread);
    statusEl.textContent = unread > 0 ? `${unread} necitite` : 'Ești la zi.';

    if (items.length === 0) {
      itemsEl.innerHTML = '<div class="notification-empty">Nu ai notificări încă.</div>';
      return;
    }

    const notificationsHtml = items
      .map((n) => {
        const type = (n.type || 'info').toLowerCase();
        const isRead = !!n.isRead;
        const title = escapeHtml(n.title || '');
        const msg = escapeHtml(n.message || '');
        // Format date: simple "Acum X min" or just local string
        // Custom logic for nicer time if needed, but keeping existing
        const when = formatDate(n.createdDate);
        const link = n.linkUrl ? escapeHtml(n.linkUrl) : '';

        // Icon mapping
        let iconClass = 'hgi-information-circle text-info';
        if (type === 'success') iconClass = 'hgi-checkmark-circle-01 text-success';
        if (type === 'warning') iconClass = 'hgi-alert-triangle text-warning';
        if (type === 'error') iconClass = 'hgi-cancel-01 text-danger';

        // Styles
        const bgClass = isRead ? 'bg-transparent opacity-75' : 'notification-unread border-start border-4 border-primary';

        return `
          <div class="notification-item p-3 border-bottom border-light ${bgClass}" data-id="${n.id}">
             <div class="d-flex align-items-start gap-3">
                 <div class="neuro-btn-icon rounded-circle d-flex align-items-center justify-content-center flex-shrink-0 shadow-sm bg-white" style="width: 35px; height: 35px;">
                     <i class="hgi-stroke ${iconClass} hgi-md"></i>
                 </div>
                 <div class="flex-grow-1">
                     <div>
                         <h6 class="fw-bold text-dark mb-0">${title}</h6>
                         <small class="text-muted d-block mt-1" style="font-size: 0.7rem;">${when}</small>
                     </div>
                     <p class="text-muted small mb-2 text-break" style="line-height: 1.3; margin-top: 0.25rem;">${msg}</p>
                     
                     <div class="d-flex align-items-center gap-2">
                        ${link ? `<a class="btn btn-sm btn-link p-0 text-decoration-none small" href="${link}">Deschide</a>` : ''}
                        ${!isRead ? `<button class="btn btn-sm btn-link p-0 text-decoration-none small text-secondary js-mark-read" type="button">Marchează citit</button>` : ''}
                     </div>
                 </div>
             </div>
          </div>
        `;
      })
      .join('');

    // Add Load More / Show Less buttons if needed
    let loadMoreHtml = '';
    if (items.length >= currentTake) {
      loadMoreHtml = `
        <div class="notification-load-more mt-2 text-center">
          <button class="btn btn-sm btn-outline-primary js-load-more" data-current-take="${currentTake}" type="button">
            <i class="hgi-stroke hgi-arrow-down-01"></i> Încarcă mai multe
          </button>
        </div>
      `;
    }

    let showLessHtml = '';
    if (currentTake > 5) {
      showLessHtml = `
        <div class="notification-show-less mt-2 text-center">
          <button class="btn btn-sm btn-outline-secondary js-show-less" type="button">
            <i class="hgi-stroke hgi-arrow-up-01"></i> Arată mai puține
          </button>
        </div>
      `;
    }

    itemsEl.innerHTML = notificationsHtml + showLessHtml + loadMoreHtml;
  }

  async function postMarkRead(id) {
    const token = tokenEl.value || '';
    const body = new URLSearchParams({ id: String(id) });

    const res = await fetch('/Notifications/Feed?handler=MarkRead', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8',
        'RequestVerificationToken': token,
      },
      body,
    });
    return res.ok;
  }

  async function postMarkAllRead() {
    const token = tokenEl.value || '';
    const res = await fetch('/Notifications/Feed?handler=MarkAllRead', {
      method: 'POST',
      headers: { 'RequestVerificationToken': token },
    });
    return res.ok;
  }

  function openMenu() {
    isOpen = true;
    menu.classList.remove('d-none');
    bellBtn.setAttribute('aria-expanded', 'true');
    if (!loadedOnce) {
      loadedOnce = true;
      fetchFeed().catch(() => { });
    } else {
      // refresh lightweight
      fetchFeed().catch(() => { });
    }
  }

  function closeMenu() {
    isOpen = false;
    menu.classList.add('d-none');
    bellBtn.setAttribute('aria-expanded', 'false');
  }

  bellBtn.addEventListener('click', (e) => {
    e.preventDefault();
    e.stopPropagation();
    if (isOpen) closeMenu();
    else openMenu();
  });

  document.addEventListener('click', (e) => {
    if (!isOpen) return;
    const target = e.target;
    if (!(target instanceof Element)) return;
    if (!menu.contains(target) && target !== bellBtn) closeMenu();
  });

  document.addEventListener('keydown', (e) => {
    if (e.key === 'Escape' && isOpen) closeMenu();
  });

  itemsEl.addEventListener('click', async (e) => {
    const target = e.target;
    if (!(target instanceof Element)) return;

    // Handle mark as read
    if (target.classList.contains('js-mark-read')) {
      const container = target.closest('.notification-item');
      const id = container?.getAttribute('data-id');
      if (!id) return;

      target.setAttribute('disabled', 'disabled');
      const ok = await postMarkRead(id);
      if (ok) {
        await fetchFeed();
      } else {
        target.removeAttribute('disabled');
      }
      return;
    }

    // Handle load more button
    if (target.classList.contains('js-load-more') || target.closest('.js-load-more')) {
      const btn = target.classList.contains('js-load-more') ? target : target.closest('.js-load-more');
      const currentTake = parseInt(btn?.getAttribute('data-current-take') || '5');
      const newTake = currentTake + 5;
      await fetchFeed(newTake);
      return;
    }

    // Handle show less button
    if (target.classList.contains('js-show-less') || target.closest('.js-show-less')) {
      await fetchFeed(5);
      return;
    }
  });

  if (markAllBtn) {
    markAllBtn.addEventListener('click', async () => {
      markAllBtn.setAttribute('disabled', 'disabled');
      const ok = await postMarkAllRead();
      if (ok) await fetchFeed();
      markAllBtn.removeAttribute('disabled');
    });
  }

  // Initial badge update (optional)
  fetch('/Notifications/Feed?take=1', { method: 'GET' })
    .then((r) => (r.ok ? r.json() : null))
    .then((d) => {
      if (!d) return;
      setBadge(d.unreadCount ?? 0);
    })
    .catch(() => { });
})();

