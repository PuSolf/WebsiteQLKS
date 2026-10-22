(() => {
    const byId = id => document.getElementById(id);
    async function load() {
        try { const p = await window.HMS.api('/api/customer/profile'); byId('profileName').value = p.hoTen || ''; byId('profilePhone').value = p.soDienThoai || ''; byId('profileEmail').value = p.email || ''; byId('profileIdNumber').value = p.soGiayTo || ''; byId('profileUsername').value = p.username || ''; }
        catch (e) { window.HMS.toast(e.message, 'error'); }
    }
    byId('profileForm').addEventListener('submit', async e => {
        e.preventDefault();
        try { const result = await window.HMS.api('/api/customer/profile', { method: 'PUT', body: JSON.stringify({ hoTen: byId('profileName').value.trim(), soDienThoai: byId('profilePhone').value.trim(), email: byId('profileEmail').value.trim() || null, soGiayTo: byId('profileIdNumber').value.trim() || null }) }); window.HMS.toast(result.message); }
        catch (error) { window.HMS.toast(error.message, 'error'); }
    });
    load();
})();
