(() => {
    const $ = id => document.getElementById(id);
    const form = $('roomSearchForm');
    const grid = $('roomGrid');
    const modal = $('roomModal');
    let currentPage = 1;
    let currentRooms = [];
    const today = new Date();
    const localDate = date => `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`;
    const tomorrow = new Date(today); tomorrow.setDate(tomorrow.getDate() + 1);
    $('checkIn').min = localDate(today); $('checkIn').value = localDate(today);
    $('checkOut').min = localDate(tomorrow); $('checkOut').value = localDate(tomorrow);

    function buildQuery() {
        const params = new URLSearchParams({ checkIn: $('checkIn').value, checkOut: $('checkOut').value, page: currentPage, pageSize: 8 });
        if ($('roomTypeId').value) params.set('roomTypeId', $('roomTypeId').value);
        if ($('guests').value) params.set('guests', $('guests').value);
        if ($('keyword').value.trim()) params.set('keyword', $('keyword').value.trim());
        if (Number($('maxPrice').value) < 3000000) params.set('maxPrice', $('maxPrice').value);
        return params;
    }
    function roomImage(room) {
        if (room.duongDanHinhAnh && room.duongDanHinhAnh.startsWith('/img/') && !room.duongDanHinhAnh.includes('..')) return room.duongDanHinhAnh;
        const name = (room.loaiPhong || room.roomType || '').toLowerCase();
        if (name.includes('suite')) return '/img/room-suite.svg';
        if (name.includes('family')) return '/img/room-family.svg';
        if (name.includes('deluxe')) return '/img/room-deluxe.svg';
        return '/img/room-standard.svg';
    }
    function renderRooms(result) {
        currentRooms = result.items || [];
        $('resultCount').textContent = `${result.totalCount || 0} Phòng phù hợp`;
        if (!currentRooms.length) {
            grid.innerHTML = '<div class="empty-results"><span class="empty-symbol">⌕</span><h3>Chưa tìm thấy phòng phù hợp</h3><p>Thử thay đổi ngày lưu trú, số khách hoặc bộ lọc giá.</p><button class="button button-outline" id="resetFilters">Xóa bộ lọc</button></div>';
            $('roomPagination').innerHTML = '';
            $('resetFilters')?.addEventListener('click', () => { $('roomTypeId').value = ''; $('keyword').value = ''; $('guests').value = '2'; $('maxPrice').value = '3000000'; $('maxPriceLabel').textContent = '3.000.000 ₫'; currentPage = 1; loadRooms(); });
            return;
        }
        const sorted = [...currentRooms];
        if ($('sortBy').value === 'asc') sorted.sort((a, b) => a.gia - b.gia);
        if ($('sortBy').value === 'desc') sorted.sort((a, b) => b.gia - a.gia);
        grid.innerHTML = sorted.map((r, index) => `
          <article class="room-card" style="--card-delay:${index * 55}ms">
            <div class="room-image-wrap"><img class="room-image" src="${window.HMS.escape(roomImage(r))}" alt="Không gian ${window.HMS.escape(r.loaiPhong)}" loading="lazy"/><span class="room-type-tag">${window.HMS.escape(r.loaiPhong)}</span><button class="image-expand" type="button" data-detail="${r.roomId}" aria-label="Xem chi tiết phòng">↗</button></div>
            <div class="room-card-content"><div class="room-card-top"><h3>Phòng ${window.HMS.escape(r.soPhong)}</h3><span class="floor-tag">Tầng ${r.tang}</span></div><div class="room-card-meta"><span>♙ ${r.soKhachToiDa} khách</span><span>✦ ${window.HMS.escape((r.tienNghi || 'Tiện nghi chọn lọc').split(',').slice(0, 2).join(' · '))}</span></div><div class="room-card-bottom"><div><strong>${window.HMS.money(r.gia)}</strong><small>/ đêm</small></div><button class="text-button" type="button" data-detail="${r.roomId}">Chi tiết <span>↗</span></button></div><button class="button button-dark button-full room-book-button" type="button" data-book="${r.roomId}">Đặt phòng nhanh</button></div>
          </article>`).join('');
        renderPagination(result);
    }
    function renderPagination(result) {
        const pages = result.totalPages || 0;
        if (pages <= 1) { $('roomPagination').innerHTML = ''; return; }
        $('roomPagination').innerHTML = `<button class="page-button" ${currentPage <= 1 ? 'disabled' : ''} data-page="${currentPage - 1}">← Trước</button><span>Trang ${currentPage} / ${pages}</span><button class="page-button" ${currentPage >= pages ? 'disabled' : ''} data-page="${currentPage + 1}">Tiếp →</button>`;
        $('roomPagination').querySelectorAll('[data-page]').forEach(btn => btn.addEventListener('click', () => { currentPage = Number(btn.dataset.page); loadRooms(); }));
    }
    async function loadRooms() {
        if ($('checkOut').value <= $('checkIn').value) {
            window.HMS.toast('Ngày trả phòng phải sau ngày nhận phòng.', 'error'); return;
        }
        grid.innerHTML = '<div class="loading-card"><div class="loading-ring"></div><span>Đang cập nhật danh sách phòng...</span></div>';
        try { renderRooms(await window.HMS.api('/api/rooms?' + buildQuery().toString())); }
        catch (error) { grid.innerHTML = `<div class="empty-results"><h3>Không thể tải danh sách phòng</h3><p>${window.HMS.escape(error.message)}</p><button class="button button-outline" id="retryRooms">Thử lại</button><p class="muted">Hãy kiểm tra connection string và trạng thái SQL Server nếu lỗi kết nối.</p></div>`; $('retryRooms')?.addEventListener('click', loadRooms); }
    }
    async function showDetails(id) {
        $('roomModalContent').innerHTML = '<div class="loading-card"><div class="loading-ring"></div><span>Đang tải chi tiết...</span></div>';
        modal.hidden = false; document.body.classList.add('modal-open');
        try {
            const r = await window.HMS.api(`/api/rooms/${id}`);
            $('roomModalContent').innerHTML = `<img class="detail-image" src="${window.HMS.escape(roomImage({ loaiPhong: r.loaiPhong, duongDanHinhAnh: r.duongDanHinhAnh }))}" alt="Phòng ${window.HMS.escape(r.soPhong)}"/><div class="detail-content"><span class="eyebrow eyebrow-muted"><span class="eyebrow-line"></span> ${window.HMS.escape(r.loaiPhong)}</span><h2 id="roomModalTitle">Phòng ${window.HMS.escape(r.soPhong)}</h2><p class="muted">${window.HMS.escape(r.khachSan || '')} · Tầng ${r.tang}</p><p>${window.HMS.escape(r.tienNghi || 'Tiện nghi cao cấp, thiết kế tinh tế.')}</p><div class="detail-price"><strong>${window.HMS.money(r.gia)}</strong><span>/ đêm</span></div><div class="detail-facts"><span>♙ Tối đa ${r.soKhachToiDa} khách</span><span>⌂ ${window.HMS.escape(r.diaChi || 'Sunrise Hotel')}</span></div><button class="button button-dark button-full" data-book="${r.roomId}">Đặt phòng nhanh ↗</button></div>`;
        } catch (error) { $('roomModalContent').innerHTML = `<div class="detail-content"><h2>Không tải được thông tin</h2><p>${window.HMS.escape(error.message)}</p></div>`; }
    }
    function closeModal() { modal.hidden = true; document.body.classList.remove('modal-open'); }
    async function quickBook(id) {
        if (!window.HMS.isAuthenticated) {
            window.HMS.toast('Vui lòng đăng nhập hoặc tạo tài khoản để đặt phòng.', 'error');
            window.setTimeout(() => window.location.href = '/Account/Login?returnUrl=%2F%23rooms', 800); return;
        }
        if (window.HMS.role !== 'Customer') { window.HMS.toast('Tài khoản nhân viên nên tạo booking từ màn hình lễ tân.', 'error'); return; }
        if ($('checkOut').value <= $('checkIn').value) { window.HMS.toast('Vui lòng kiểm tra ngày nhận và trả phòng.', 'error'); return; }
        const room = currentRooms.find(x => x.roomId === Number(id));
        const name = room ? `phòng ${room.soPhong}` : 'phòng đã chọn';
        if (!window.confirm(`Xác nhận gửi yêu cầu đặt ${name} từ ${$('checkIn').value} đến ${$('checkOut').value}? Booking sẽ ở trạng thái chờ xác nhận.`)) return;
        const buttons = document.querySelectorAll(`[data-book="${id}"]`); buttons.forEach(b => { b.disabled = true; b.dataset.originalText = b.textContent; b.textContent = 'Đang xử lý...'; });
        try {
            const result = await window.HMS.api('/api/bookings', { method: 'POST', body: JSON.stringify({ roomId: Number(id), checkIn: $('checkIn').value, checkOut: $('checkOut').value, guests: Number($('guests').value) }) });
            closeModal(); window.HMS.toast(`${result.message} Mã booking #${result.bookingId}.`); currentPage = 1; await loadRooms();
        } catch (error) { window.HMS.toast(error.message, 'error'); }
        finally { buttons.forEach(b => { b.disabled = false; b.textContent = b.dataset.originalText || 'Đặt phòng nhanh'; }); }
    }
    form.addEventListener('submit', event => { event.preventDefault(); currentPage = 1; loadRooms(); document.getElementById('rooms').scrollIntoView({ behavior: 'smooth', block: 'start' }); });
    ['roomTypeId', 'guests', 'checkOut', 'sortBy'].forEach(id => $(id).addEventListener('change', () => { currentPage = 1; loadRooms(); }));
    let timer; $('keyword').addEventListener('input', () => { clearTimeout(timer); timer = setTimeout(() => { currentPage = 1; loadRooms(); }, 300); });
    $('maxPrice').addEventListener('input', () => { $('maxPriceLabel').textContent = window.HMS.money($('maxPrice').value); clearTimeout(timer); timer = setTimeout(() => { currentPage = 1; loadRooms(); }, 250); });
    $('checkIn').addEventListener('change', () => { if ($('checkOut').value <= $('checkIn').value) { const date = new Date($('checkIn').value + 'T00:00:00'); date.setDate(date.getDate() + 1); $('checkOut').value = localDate(date); $('checkOut').min = localDate(date); } currentPage = 1; loadRooms(); });
    grid.addEventListener('click', event => { const detail = event.target.closest('[data-detail]'); const book = event.target.closest('[data-book]'); if (detail) showDetails(detail.dataset.detail); else if (book) quickBook(book.dataset.book); });
    $('roomModalContent').addEventListener('click', event => { const book = event.target.closest('[data-book]'); if (book) quickBook(book.dataset.book); });
    $('closeRoomModal').addEventListener('click', closeModal); modal.addEventListener('click', event => { if (event.target === modal) closeModal(); });
    document.addEventListener('keydown', event => { if (event.key === 'Escape') closeModal(); });
    loadRooms();
})();
