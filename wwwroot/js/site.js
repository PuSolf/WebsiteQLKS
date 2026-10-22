(() => {
    const token = () => window.HMS?.requestVerificationToken || document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
    window.HMS = window.HMS || {};
    window.HMS.money = value => new Intl.NumberFormat('vi-VN', { maximumFractionDigits: 0 }).format(Number(value || 0)) + ' ₫';
    window.HMS.date = value => {
        if (!value) return '—';
        const date = typeof value === 'string' && /^\d{4}-\d{2}-\d{2}$/.test(value) ? new Date(value + 'T00:00:00') : new Date(value);
        return Number.isNaN(date.getTime()) ? String(value) : new Intl.DateTimeFormat('vi-VN', { day: '2-digit', month: '2-digit', year: 'numeric' }).format(date);
    };
    window.HMS.dateTime = value => {
        if (!value) return '—';
        const date = new Date(value);
        return Number.isNaN(date.getTime()) ? String(value) : new Intl.DateTimeFormat('vi-VN', { dateStyle: 'short', timeStyle: 'short' }).format(date);
    };
    window.HMS.escape = value => String(value ?? '').replace(/[&<>"']/g, ch => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[ch]));
    window.HMS.api = async (url, options = {}) => {
        const method = (options.method || 'GET').toUpperCase();
        const headers = new Headers(options.headers || {});
        headers.set('Accept', 'application/json');
        if (options.body && !(options.body instanceof FormData) && !headers.has('Content-Type')) headers.set('Content-Type', 'application/json');
        if (!['GET', 'HEAD', 'OPTIONS'].includes(method) && token()) headers.set('RequestVerificationToken', token());
        const response = await fetch(url, { ...options, method, headers, credentials: 'same-origin' });
        const raw = await response.text();
        let data = null;
        try { data = raw ? JSON.parse(raw) : null; } catch { data = { message: raw }; }
        if (!response.ok) {
            if (response.status === 401) throw new Error('Phiên đăng nhập chưa hợp lệ. Hãy đăng nhập để tiếp tục.');
            if (response.status === 403) throw new Error('Tài khoản của bạn không có quyền thực hiện thao tác này.');
            const validation = data?.errors ? Object.values(data.errors).flat().join(' ') : '';
            throw new Error(data?.message || validation || `Yêu cầu thất bại (HTTP ${response.status}).`);
        }
        return data;
    };
    window.HMS.toast = (message, kind = 'success') => {
        const region = document.getElementById('toastRegion');
        if (!region) { window.alert(message); return; }
        const item = document.createElement('div');
        item.className = `toast toast-${kind}`;
        item.textContent = message;
        region.appendChild(item);
        requestAnimationFrame(() => item.classList.add('is-visible'));
        window.setTimeout(() => { item.classList.remove('is-visible'); window.setTimeout(() => item.remove(), 250); }, 3800);
    };
    const toggle = document.getElementById('navToggle');
    const nav = document.getElementById('mainNav');
    toggle?.addEventListener('click', () => {
        const open = nav.classList.toggle('is-open');
        toggle.setAttribute('aria-expanded', String(open));
    });
    nav?.querySelectorAll('a').forEach(link => link.addEventListener('click', () => {
        nav.classList.remove('is-open'); toggle?.setAttribute('aria-expanded', 'false');
    }));
    document.addEventListener('click', event => {
        if (event.target.closest('[data-jump-rooms]')) document.getElementById('rooms')?.scrollIntoView({ behavior: 'smooth' });
    });
})();
