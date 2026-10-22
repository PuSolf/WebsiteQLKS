(() => {
    const $ = id => document.getElementById(id);
    const money = window.HMS.money, esc = window.HMS.escape;
    const labels = { Pending: 'Chờ xác nhận', Confirmed: 'Đã xác nhận', CheckedIn: 'Đang lưu trú', CheckedOut: 'Đã trả phòng', Cancelled: 'Đã hủy', Available: 'Sẵn sàng', Reserved: 'Đã giữ', Occupied: 'Đang sử dụng', Maintenance: 'Bảo trì', NeedsCleaning: 'Chờ dọn' };
    const statusClass = value => 'status-' + String(value || '').toLowerCase();
    const isManager = window.HMS.role === 'Manager';
    async function api(path, options) { return window.HMS.api(path, options); }
    function actionButton(action, id, text, kind = 'outline') { return `<button type="button" class="button button-${kind} button-small" data-action="${action}" data-id="${id}">${text}</button>`; }
    function bookingActions(b) {
        if (b.trangThai === 'Pending') return actionButton('edit-booking', b.bookingId, 'Sửa') + actionButton('confirm', b.bookingId, 'Xác nhận', 'gold') + actionButton('cancel', b.bookingId, 'Hủy');
        if (b.trangThai === 'Confirmed') return actionButton('edit-booking', b.bookingId, 'Sửa') + actionButton('check-in', b.bookingId, 'Check-in', 'dark') + actionButton('cancel', b.bookingId, 'Hủy');
        if (b.trangThai === 'CheckedIn') return actionButton('check-out', b.bookingId, 'Check-out', 'dark');
        return '<span class="muted">—</span>';
    }
    async function loadBookings() {
        const host = $('managerBookings'); if (!host) return;
        host.innerHTML = '<tr><td colspan="7">Đang tải...</td></tr>';
        const status = $('bookingStatus')?.value || 'All', q = $('bookingQuery')?.value.trim() || '';
        try {
            const data = await api('/api/management/bookings?' + new URLSearchParams({ status, q, page: 1, pageSize: 50 }));
            window.__hmsBookings = data.items;
            host.innerHTML = data.items.length ? data.items.map(b => `<tr><td><strong>#${b.bookingId}</strong><small>${window.HMS.date(b.ngayNhanPhong)} · ${window.HMS.date(b.ngayTraPhong)}</small></td><td><b>${esc(b.customer)}</b><small>${esc(b.soDienThoai || '')}</small></td><td>${(b.rooms || []).map(r => `<span class="room-chip">${esc(r.soPhong)}</span>`).join(' ')}<small>${(b.rooms || []).map(r => esc(r.roomType)).join(', ')}</small></td><td>${window.HMS.date(b.ngayNhanPhong)}<small>đến ${window.HMS.date(b.ngayTraPhong)}</small></td><td><b>${money(b.tongTien)}</b><small>Cọc ${money(b.tienCoc)}</small></td><td><span class="status-pill ${statusClass(b.trangThai)}">${labels[b.trangThai] || esc(b.trangThai)}</span></td><td><div class="table-actions">${bookingActions(b)}</div></td></tr>`).join('') : '<tr><td colspan="7" class="table-empty">Không có booking phù hợp.</td></tr>';
        } catch (e) { host.innerHTML = `<tr><td colspan="7" class="table-empty">${esc(e.message)}</td></tr>`; }
    }
    async function handleBookingAction(action, id) {
        if (action === 'edit-booking') {
            const booking = (window.__hmsBookings || []).find(b => b.bookingId === Number(id));
            if (!booking) return;
            const currentRoomId = booking.rooms?.[0]?.roomId;
            const checkIn = window.prompt('Ngày nhận phòng (YYYY-MM-DD)', booking.ngayNhanPhong);
            if (!checkIn) return;
            const checkOut = window.prompt('Ngày trả phòng (YYYY-MM-DD)', booking.ngayTraPhong);
            if (!checkOut) return;
            const roomId = Number(window.prompt('RoomId mới', String(currentRoomId || '')));
            if (!Number.isInteger(roomId) || roomId < 1) { window.HMS.toast('RoomId không hợp lệ.', 'error'); return; }
            try { const result = await api(`/api/management/bookings/${id}`, { method: 'PUT', body: JSON.stringify({ checkIn, checkOut, roomId }) }); window.HMS.toast(result.message); await loadBookings(); if (isManager) loadSummary(); }
            catch (e) { window.HMS.toast(e.message, 'error'); }
            return;
        }
        let path = `/api/management/bookings/${id}/${action}`; let body;
        if (action === 'check-out') {
            const discount = Number(window.prompt('Giảm giá (VND)', '0'));
            if (!Number.isFinite(discount) || discount < 0) { window.HMS.toast('Mức giảm giá không hợp lệ.', 'error'); return; }
            const surcharge = Number(window.prompt('Phụ thu (VND)', '0'));
            if (!Number.isFinite(surcharge) || surcharge < 0) { window.HMS.toast('Mức phụ thu không hợp lệ.', 'error'); return; }
            path = `/api/management/bookings/${id}/check-out`; body = JSON.stringify({ giamGia: discount, phuThu: surcharge });
        }
        if (action === 'cancel' && !window.confirm('Xác nhận hủy booking #' + id + '?')) return;
        if (action === 'confirm' && !window.confirm('Xác nhận booking #' + id + '?')) return;
        try { const result = await api(path, { method: 'POST', ...(body ? { body } : {}) }); window.HMS.toast(result.message || 'Đã cập nhật booking.'); await loadBookings(); if (isManager) { await loadSummary(); } }
        catch (e) { window.HMS.toast(e.message, 'error'); }
    }
    async function loadSummary() {
        if (!$('managerStats')) return;
        try {
            const s = await api('/api/management/summary');
            const values = [
                ['Tổng số phòng', s.totalRooms, 'Phòng trong hệ thống', '⌂'],
                ['Sẵn sàng', s.availableRooms, 'Có thể khai thác', '✦'],
                ['Booking hoạt động', s.activeBookings, 'Chờ / xác nhận / lưu trú', '↗'],
                ['Nhận phòng hôm nay', s.todayArrivals, 'Theo ngày nhận dự kiến', '↓'],
                ['Doanh thu tháng', money(s.monthlyRevenue), 'Tính cả các khoản hoàn tiền', '₫']
            ];
            $('managerStats').innerHTML = values.map(([title, value, sub, icon]) => `<article class="stat-card"><div class="stat-top"><span>${title}</span><i>${icon}</i></div><strong>${value}</strong><small>${sub}</small></article>`).join('');
        } catch (e) { $('managerStats').innerHTML = `<p class="inline-error">${esc(e.message)}</p>`; }
    }
    async function loadRoomTypes() {
        const data = await api('/api/management/room-types');
        window.__hmsRoomTypes = data;
        if ($('editRoomType')) $('editRoomType').innerHTML = data.map(t => `<option value="${t.roomTypeId}">${esc(t.ten)} · ${money(t.gia)}</option>`).join('');
        const host = $('managerRoomTypes');
        if (host) host.innerHTML = data.length ? data.map(t => `<tr><td><b>${esc(t.ten)}</b></td><td>${money(t.gia)}</td><td>${t.soKhachToiDa} khách</td><td>${esc(t.tienNghi || '—')}</td><td><div class="table-actions">${actionButton('edit-room-type', t.roomTypeId, 'Sửa')}${actionButton('delete-room-type', t.roomTypeId, 'Xóa')}</div></td></tr>`).join('') : '<tr><td colspan="5" class="table-empty">Chưa có hạng phòng.</td></tr>';
        return data;
    }
    async function loadRooms() {
        const host = $('managerRooms'); if (!host) return;
        host.innerHTML = '<tr><td colspan="6">Đang tải...</td></tr>';
        try {
            await loadRoomTypes(); const rooms = await api('/api/management/rooms');
            host.innerHTML = rooms.length ? rooms.map(r => `<tr><td><b>${esc(r.soPhong)}</b></td><td>${r.tang}</td><td>${esc(r.roomType)}</td><td>${money(r.gia)}</td><td><span class="status-pill ${statusClass(r.trangThai)}">${labels[r.trangThai] || esc(r.trangThai)}</span></td><td><div class="table-actions">${actionButton('edit-room', r.roomId, 'Sửa')}${actionButton('delete-room', r.roomId, 'Xóa')}</div></td></tr>`).join('') : '<tr><td colspan="6" class="table-empty">Chưa có phòng.</td></tr>';
            window.__hmsRooms = rooms;
        } catch (e) { host.innerHTML = `<tr><td colspan="6" class="table-empty">${esc(e.message)}</td></tr>`; }
    }
    function openRoomEditor(room) {
        $('roomEditor').hidden = false;
        $('editRoomId').value = room?.roomId || '';
        $('editRoomNumber').value = room?.soPhong || '';
        $('editRoomFloor').value = room?.tang ?? 1;
        $('editRoomStatus').value = room?.trangThai || 'Available';
        $('editRoomImage').value = room?.duongDanHinhAnh || '';
        if ($('editRoomImageFile')) $('editRoomImageFile').value = '';
        if (room) $('editRoomType').value = room.roomTypeId;
        $('editRoomNumber').focus();
        $('roomEditor').scrollIntoView({ behavior: 'smooth', block: 'center' });
    }
    async function saveRoom(event) {
        event.preventDefault();
        const id = $('editRoomId').value;
        try {
            const file = $('editRoomImageFile')?.files?.[0];
            if (file) {
                const upload = new FormData(); upload.append('file', file);
                const uploaded = await api('/api/management/rooms/image', { method: 'POST', body: upload });
                $('editRoomImage').value = uploaded.path;
            }
            const body = { soPhong: $('editRoomNumber').value.trim(), tang: Number($('editRoomFloor').value), roomTypeId: Number($('editRoomType').value), trangThai: $('editRoomStatus').value, duongDanHinhAnh: $('editRoomImage').value.trim() || null, hotelId: 1 };
            const result = await api(id ? `/api/management/rooms/${id}` : '/api/management/rooms', { method: id ? 'PUT' : 'POST', body: JSON.stringify(body) });
            window.HMS.toast(result.message || 'Đã lưu phòng.'); $('roomEditor').hidden = true; await loadRooms(); await loadSummary();
        } catch (e) { window.HMS.toast(e.message, 'error'); }
    }
    function openRoomTypeEditor(type) {
        $('roomTypeEditor').hidden = false;
        $('editRoomTypeId').value = type?.roomTypeId || '';
        $('editRoomTypeName').value = type?.ten || '';
        $('editRoomTypePrice').value = type?.gia ?? 500000;
        $('editRoomTypeGuests').value = type?.soKhachToiDa ?? 2;
        $('editRoomTypeAmenities').value = type?.tienNghi || '';
        $('editRoomTypeName').focus();
        $('roomTypeEditor').scrollIntoView({ behavior: 'smooth', block: 'center' });
    }
    async function saveRoomType(event) {
        event.preventDefault();
        const id = $('editRoomTypeId').value;
        const body = { ten: $('editRoomTypeName').value.trim(), gia: Number($('editRoomTypePrice').value), soKhachToiDa: Number($('editRoomTypeGuests').value), tienNghi: $('editRoomTypeAmenities').value.trim() || null };
        try { const result = await api(id ? `/api/management/room-types/${id}` : '/api/management/room-types', { method: id ? 'PUT' : 'POST', body: JSON.stringify(body) }); window.HMS.toast(result.message || 'Đã lưu hạng phòng.'); $('roomTypeEditor').hidden = true; await loadRoomTypes(); await loadRooms(); }
        catch (e) { window.HMS.toast(e.message, 'error'); }
    }
    async function loadPayments() {
        const host = $('managerPayments'); if (!host) return;
        host.innerHTML = '<tr><td colspan="8">Đang tải...</td></tr>';
        try { const rows = await api('/api/management/payments'); host.innerHTML = rows.length ? rows.map(p => `<tr><td>#${p.paymentId}</td><td>#${p.bookingId}</td><td>${esc(p.customer)}</td><td><b>${money(p.soTien)}</b></td><td>${esc(p.phuongThuc)}</td><td>${window.HMS.dateTime(p.thoiGianThanhToan)}</td><td><span class="status-pill ${statusClass(p.trangThai)}">${esc(p.trangThai)}</span></td><td>${p.trangThai === 'Completed' ? actionButton('refund', p.paymentId, 'Hoàn tiền') : '—'}</td></tr>`).join('') : '<tr><td colspan="8" class="table-empty">Chưa có giao dịch.</td></tr>'; }
        catch (e) { host.innerHTML = `<tr><td colspan="8" class="table-empty">${esc(e.message)}</td></tr>`; }
    }
    async function loadReports() {
        try {
            const data = await api('/api/management/reports');
            $('revenueReport').innerHTML = data.revenue.length ? `<div class="mini-report-list">${data.revenue.map(r => `<div><span>${window.HMS.date(r.date)}</span><b>${money(r.revenue)}</b><small>${r.paymentCount || 0} giao dịch</small></div>`).join('')}</div>` : '<p class="muted">Chưa có dữ liệu doanh thu.</p>';
            $('bookingReport').innerHTML = data.bookings.length ? `<div class="mini-report-list">${data.bookings.map(b => `<div><span>#${b.bookingId} · ${esc(b.customer)}</span><b>${money(b.tongTien)}</b><small>${labels[b.trangThai] || esc(b.trangThai)} · ${b.roomCount || 0} phòng</small></div>`).join('')}</div>` : '<p class="muted">Chưa có booking.</p>';
        } catch (e) { $('revenueReport').innerHTML = `<p class="inline-error">${esc(e.message)}</p>`; }
    }
    async function loadCustomers() {
        const host = $('managerCustomers') || $('receptionCustomers'); if (!host) return;
        const q = $('customerQuery')?.value.trim() || '';
        try {
            const rows = await api('/api/management/customers?' + new URLSearchParams({ q }));
            if ($('receptionCustomers')) host.innerHTML = rows.length ? rows.map(c => `<tr><td><b>${esc(c.hoTen)}</b></td><td>${esc(c.soDienThoai)}</td><td>${esc(c.email || '—')}</td></tr>`).join('') : '<tr><td colspan="3">Không tìm thấy khách hàng.</td></tr>';
            else host.innerHTML = rows.length ? rows.map(c => `<tr><td>${c.customerId}</td><td>${esc(c.hoTen)}</td><td>${esc(c.soDienThoai)}</td><td>${esc(c.email || '—')}</td><td>${esc(c.soGiayTo || '—')}</td><td><div class="table-actions">${actionButton('edit-customer', c.customerId, 'Sửa')}${isManager && !c.userId ? actionButton('delete-customer', c.customerId, 'Xóa') : ''}</div></td></tr>`).join('') : '<tr><td colspan="6">Không tìm thấy khách hàng.</td></tr>'; 
        } catch (e) { host.innerHTML = `<tr><td colspan="5">${esc(e.message)}</td></tr>`; }
    }
    async function createCustomer(event) {
        event.preventDefault();
        try { const result = await api('/api/management/customers', { method: 'POST', body: JSON.stringify({ hoTen: $('newCustomerName').value.trim(), soDienThoai: $('newCustomerPhone').value.trim(), email: $('newCustomerEmail').value.trim() || null, soGiayTo: $('newCustomerIdNumber').value.trim() || null }) }); window.HMS.toast(result.message); $('newCustomerForm').reset(); await loadCustomers(); }
        catch (e) { window.HMS.toast(e.message, 'error'); }
    }
    async function loadEmployees() {
        if (!$('managerEmployees')) return;
        try { const rows = await api('/api/management/employees'); $('managerEmployees').innerHTML = rows.length ? rows.map(e => `<tr><td>${e.employeeId}</td><td>${esc(e.hoTen)}</td><td>${esc(e.chucVu)}</td><td>${esc(e.username)}</td><td>${esc(e.email)}</td><td><span class="status-pill ${e.isActive ? 'status-available' : 'status-cancelled'}">${e.isActive ? 'Đang hoạt động' : 'Đã khóa'}</span></td><td>${actionButton('toggle-employee', e.employeeId, e.isActive ? 'Khóa' : 'Mở khóa')}</td></tr>`).join('') : '<tr><td colspan="6">Chưa có nhân viên.</td></tr>'; }
        catch (e) { $('managerEmployees').innerHTML = `<tr><td colspan="6">${esc(e.message)}</td></tr>`; }
    }
    async function createEmployee(event) {
        event.preventDefault();
        try { const result = await api('/api/management/employees', { method: 'POST', body: JSON.stringify({ hoTen: $('newEmployeeName').value.trim(), chucVu: 'Receptionist', soDienThoai: $('newEmployeePhone').value.trim() || null, username: $('newEmployeeUsername').value.trim(), email: $('newEmployeeEmail').value.trim(), password: $('newEmployeePassword').value }) }); window.HMS.toast(result.message); $('newEmployeeForm').reset(); await loadEmployees(); }
        catch (e) { window.HMS.toast(e.message, 'error'); }
    }
    async function loadHotel() {
        try { const h = await api('/api/management/hotel-info'); $('hotelId').value = h.hotelId; $('hotelName').value = h.ten || ''; $('hotelPhone').value = h.soDienThoai || ''; $('hotelAddress').value = h.diaChi || ''; $('hotelEmail').value = h.email || ''; $('hotelCheckin').value = String(h.gioNhanPhong).slice(0, 5); $('hotelCheckout').value = String(h.gioTraPhong).slice(0, 5); }
        catch (e) { window.HMS.toast(e.message, 'error'); }
    }
    async function saveHotel(event) {
        event.preventDefault(); const id = $('hotelId').value;
        try { const result = await api(`/api/management/hotel-info/${id}`, { method: 'PUT', body: JSON.stringify({ ten: $('hotelName').value.trim(), diaChi: $('hotelAddress').value.trim(), soDienThoai: $('hotelPhone').value.trim(), email: $('hotelEmail').value.trim() || null, gioNhanPhong: $('hotelCheckin').value + ':00', gioTraPhong: $('hotelCheckout').value + ':00' }) }); window.HMS.toast(result.message); }
        catch (e) { window.HMS.toast(e.message, 'error'); }
    }
    async function loadStaffCustomers() {
        if (!$('staffCustomerId')) return;
        const query = $('staffCustomerQuery')?.value.trim() || '';
        const keep = $('staffCustomerId').value;
        try {
            const rows = await api('/api/management/customers?' + new URLSearchParams({ q: query }));
            $('staffCustomerId').innerHTML = '<option value="">Chọn khách hàng</option>' + rows.map(c => `<option value="${c.customerId}">${esc(c.hoTen)} · ${esc(c.soDienThoai)}</option>`).join('');
            if (keep) $('staffCustomerId').value = keep;
        } catch (e) { window.HMS.toast(e.message, 'error'); }
    }
    async function loadStaffRooms() {
        if (!$('staffRoomId')) return;
        const checkIn = $('staffCheckIn').value, checkOut = $('staffCheckOut').value;
        if (!checkIn || !checkOut || checkOut <= checkIn) { $('staffRoomId').innerHTML = '<option value="">Kiểm tra ngày nhận/trả</option>'; return; }
        $('staffRoomId').innerHTML = '<option value="">Đang tìm phòng...</option>';
        try {
            const result = await api('/api/rooms?' + new URLSearchParams({ checkIn, checkOut, guests: $('staffGuests').value, page: 1, pageSize: 48 }));
            $('staffRoomId').innerHTML = '<option value="">Chọn phòng trống</option>' + result.items.map(r => `<option value="${r.roomId}">Phòng ${esc(r.soPhong)} · ${esc(r.loaiPhong)} · ${money(r.gia)}/đêm</option>`).join('');
            if (!result.items.length) $('staffRoomId').innerHTML = '<option value="">Không có phòng phù hợp</option>';
        } catch (e) { $('staffRoomId').innerHTML = '<option value="">Không thể tải phòng</option>'; window.HMS.toast(e.message, 'error'); }
    }
    async function createFrontDeskBooking(event) {
        event.preventDefault();
        const body = { customerId: Number($('staffCustomerId').value), roomId: Number($('staffRoomId').value), checkIn: $('staffCheckIn').value, checkOut: $('staffCheckOut').value, guests: Number($('staffGuests').value) };
        if (!body.customerId || !body.roomId) { window.HMS.toast('Vui lòng chọn khách hàng và phòng.', 'error'); return; }
        try { const result = await api('/api/bookings', { method: 'POST', body: JSON.stringify(body) }); window.HMS.toast(result.message + ' Mã booking #' + result.bookingId); $('receptionBookingForm').reset(); initStaffDates(); await loadStaffCustomers(); await loadStaffRooms(); await loadBookings(); }
        catch (e) { window.HMS.toast(e.message, 'error'); }
    }
    function initStaffDates() {
        if (!$('staffCheckIn')) return;
        const now = new Date(); const fmt = d => `${d.getFullYear()}-${String(d.getMonth()+1).padStart(2,'0')}-${String(d.getDate()).padStart(2,'0')}`;
        const next = new Date(now); next.setDate(next.getDate()+1);
        $('staffCheckIn').min = fmt(now); $('staffCheckIn').value = fmt(now);
        $('staffCheckOut').min = fmt(next); $('staffCheckOut').value = fmt(next);
    }
    async function processPayment(event) {
        event.preventDefault();
        try { const result = await api('/api/management/payments', { method: 'POST', body: JSON.stringify({ bookingId: Number($('paymentBookingId').value), soTien: Number($('paymentAmount').value), phuongThuc: $('paymentMethod').value, invoiceId: Number($('paymentInvoiceId')?.value) || null }) }); window.HMS.toast(result.message); $('paymentForm').reset(); if ($('managerPayments')) loadPayments(); }
        catch (e) { window.HMS.toast(e.message, 'error'); }
    }

    document.addEventListener('click', async event => {
        const tab = event.target.closest('[data-tab]');
        if (tab) {
            document.querySelectorAll('.tab').forEach(x => x.classList.toggle('active', x === tab));
            document.querySelectorAll('.manager-panel').forEach(x => x.classList.toggle('active', x.id === 'panel-' + tab.dataset.tab));
            const panel = tab.dataset.tab;
            if (panel === 'rooms') loadRooms(); if (panel === 'room-types') loadRoomTypes(); if (panel === 'payments') loadPayments(); if (panel === 'reports') loadReports();
            if (panel === 'customers') loadCustomers(); if (panel === 'employees') loadEmployees(); if (panel === 'hotel') loadHotel();
            return;
        }
        const button = event.target.closest('[data-action]'); if (!button) return;
        const { action, id } = button.dataset;
        if (['confirm', 'cancel', 'check-in', 'check-out', 'edit-booking'].includes(action)) await handleBookingAction(action, id);
        else if (action === 'edit-room') openRoomEditor((window.__hmsRooms || []).find(r => r.roomId === Number(id)));
        else if (action === 'edit-room-type') openRoomTypeEditor((window.__hmsRoomTypes || []).find(r => r.roomTypeId === Number(id)));
        else if (action === 'delete-room-type') {
            if (!window.confirm('Xóa hạng phòng #' + id + '? Nếu đã có phòng sử dụng, hệ thống sẽ từ chối xóa.')) return;
            try { const result = await api(`/api/management/room-types/${id}`, { method: 'DELETE' }); window.HMS.toast(result.message); await loadRoomTypes(); await loadRooms(); }
            catch (e) { window.HMS.toast(e.message, 'error'); }
        } else if (action === 'edit-customer') {
            const row = button.closest('tr'); const cells = row?.querySelectorAll('td');
            const currentName = cells?.[1]?.textContent?.trim() || ''; const currentPhone = cells?.[2]?.textContent?.trim() || ''; const currentEmail = cells?.[3]?.textContent?.trim() || ''; const currentId = cells?.[4]?.textContent?.trim() || '';
            const hoTen = window.prompt('Họ tên khách hàng', currentName); if (hoTen === null) return;
            const soDienThoai = window.prompt('Số điện thoại', currentPhone); if (soDienThoai === null) return;
            const email = window.prompt('Email (bỏ trống nếu không có)', currentEmail === '—' ? '' : currentEmail); if (email === null) return;
            const soGiayTo = window.prompt('Số giấy tờ (bỏ trống nếu không có)', currentId === '—' ? '' : currentId); if (soGiayTo === null) return;
            try { const result = await api(`/api/management/customers/${id}`, { method: 'PUT', body: JSON.stringify({ hoTen, soDienThoai, email: email || null, soGiayTo: soGiayTo || null }) }); window.HMS.toast(result.message); await loadCustomers(); }
            catch (e) { window.HMS.toast(e.message, 'error'); }
        } else if (action === 'delete-customer') {
            if (!window.confirm('Xóa hồ sơ khách hàng #' + id + '?')) return;
            try { const result = await api(`/api/management/customers/${id}`, { method: 'DELETE' }); window.HMS.toast(result.message); await loadCustomers(); }
            catch (e) { window.HMS.toast(e.message, 'error'); }
        } else if (action === 'toggle-employee') {
            if (!window.confirm('Đổi trạng thái tài khoản nhân viên #' + id + '?')) return;
            try { const result = await api(`/api/management/employees/${id}/toggle-status`, { method: 'POST' }); window.HMS.toast(result.message); await loadEmployees(); }
            catch (e) { window.HMS.toast(e.message, 'error'); }
        } else if (action === 'delete-room') {
            if (!window.confirm('Xóa phòng #' + id + '? Nếu phòng có lịch sử booking, hệ thống sẽ từ chối xóa.')) return;
            try { const result = await api(`/api/management/rooms/${id}`, { method: 'DELETE' }); window.HMS.toast(result.message); await loadRooms(); await loadSummary(); }
            catch (e) { window.HMS.toast(e.message, 'error'); }
        } else if (action === 'refund') {
            if (!window.confirm('Ghi nhận hoàn tiền cho giao dịch #' + id + '?')) return;
            try { const result = await api(`/api/management/payments/${id}/refund`, { method: 'POST' }); window.HMS.toast(result.message); await loadPayments(); }
            catch (e) { window.HMS.toast(e.message, 'error'); }
        }
    });
    $('refreshBookings')?.addEventListener('click', loadBookings);
    $('newRoomButton')?.addEventListener('click', async () => { try { await loadRoomTypes(); openRoomEditor(null); } catch (e) { window.HMS.toast(e.message, 'error'); } });
    $('cancelRoomEdit')?.addEventListener('click', () => { $('roomEditor').hidden = true; });
    $('roomEditor')?.addEventListener('submit', saveRoom);
    $('newRoomTypeButton')?.addEventListener('click', () => openRoomTypeEditor(null));
    $('cancelRoomTypeEdit')?.addEventListener('click', () => { $('roomTypeEditor').hidden = true; });
    $('roomTypeEditor')?.addEventListener('submit', saveRoomType);
    $('refreshPayments')?.addEventListener('click', loadPayments); $('refreshReports')?.addEventListener('click', loadReports);
    $('searchCustomers')?.addEventListener('click', loadCustomers); $('customerSearchForm')?.addEventListener('submit', e => { e.preventDefault(); loadCustomers(); });
    $('newCustomerForm')?.addEventListener('submit', createCustomer); $('refreshEmployees')?.addEventListener('click', loadEmployees); $('newEmployeeForm')?.addEventListener('submit', createEmployee);
    $('hotelInfoForm')?.addEventListener('submit', saveHotel); $('paymentForm')?.addEventListener('submit', processPayment);
    $('receptionBookingForm')?.addEventListener('submit', createFrontDeskBooking);
    $('staffCheckIn')?.addEventListener('change', () => { const from = new Date($('staffCheckIn').value + 'T00:00:00'); from.setDate(from.getDate()+1); const fmt = d => `${d.getFullYear()}-${String(d.getMonth()+1).padStart(2,'0')}-${String(d.getDate()).padStart(2,'0')}`; $('staffCheckOut').min = fmt(from); if ($('staffCheckOut').value <= $('staffCheckIn').value) $('staffCheckOut').value = fmt(from); loadStaffRooms(); });
    $('staffCheckOut')?.addEventListener('change', loadStaffRooms); $('staffGuests')?.addEventListener('change', loadStaffRooms);
    let staffCustomerTimer; $('staffCustomerQuery')?.addEventListener('input', () => { clearTimeout(staffCustomerTimer); staffCustomerTimer = setTimeout(loadStaffCustomers, 250); });
    loadBookings(); if (isManager) loadSummary(); if ($('receptionBookingForm')) { initStaffDates(); loadStaffCustomers(); loadStaffRooms(); }
})();
