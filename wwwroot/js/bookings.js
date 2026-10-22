(() => {
    const host = document.getElementById('myBookings');
    const labels = { Pending: 'Chờ xác nhận', Confirmed: 'Đã xác nhận', CheckedIn: 'Đang lưu trú', CheckedOut: 'Đã trả phòng', Cancelled: 'Đã hủy' };
    async function load() {
        try {
            const bookings = await window.HMS.api('/api/bookings/mine');
            if (!bookings.length) { host.innerHTML = '<div class="empty-results"><span class="empty-symbol">✧</span><h3>Hành trình của bạn bắt đầu từ đây</h3><p>Bạn chưa có booking nào. Hãy khám phá các không gian của Sunrise.</p><a class="button button-dark" href="/#rooms">Tìm phòng ↗</a></div>'; return; }
            host.innerHTML = bookings.map(b => `<article class="booking-card"><div class="booking-card-head"><div><span class="booking-id">BOOKING #${b.bookingId}</span><h3>${(b.rooms || []).map(r => `Phòng ${window.HMS.escape(r.soPhong)} · ${window.HMS.escape(r.loaiPhong)}`).join(', ')}</h3></div><span class="status-pill status-${b.trangThai.toLowerCase()}">${labels[b.trangThai] || window.HMS.escape(b.trangThai)}</span></div><div class="booking-facts"><div><span>NHẬN PHÒNG</span><b>${window.HMS.date(b.ngayNhanPhong)}</b></div><div><span>TRẢ PHÒNG</span><b>${window.HMS.date(b.ngayTraPhong)}</b></div><div><span>TỔNG TIỀN</span><b>${window.HMS.money(b.tongTien)}</b></div><div><span>ĐÃ ĐẶT CỌC</span><b>${window.HMS.money(b.tienCoc)}</b></div></div>${b.payments?.length ? `<div class="booking-payments"><strong>Giao dịch:</strong> ${b.payments.map(p => `${window.HMS.money(p.soTien)} (${window.HMS.escape(p.phuongThuc)} · ${window.HMS.escape(p.trangThai)})`).join(' · ')}</div>` : ''}<div class="booking-card-actions">${['Pending', 'Confirmed'].includes(b.trangThai) ? `<button class="button button-outline button-small" data-cancel="${b.bookingId}">Hủy booking</button>` : ''}<span class="muted">${b.thoiGianHuy ? 'Đã hủy lúc ' + window.HMS.dateTime(b.thoiGianHuy) : ''}</span></div></article>`).join('');
        } catch (error) { host.innerHTML = `<div class="empty-results"><h3>Không tải được booking</h3><p>${window.HMS.escape(error.message)}</p></div>`; }
    }
    host.addEventListener('click', async event => {
        const button = event.target.closest('[data-cancel]'); if (!button) return;
        if (!window.confirm('Bạn chắc chắn muốn hủy booking này? Thời gian hủy sẽ được lưu trong hệ thống.')) return;
        try { const result = await window.HMS.api(`/api/bookings/${button.dataset.cancel}/cancel`, { method: 'POST' }); window.HMS.toast(result.message); await load(); }
        catch (error) { window.HMS.toast(error.message, 'error'); }
    });
    load();
})();
