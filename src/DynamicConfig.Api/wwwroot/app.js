const apiBase = '/api/configurations';
let allEntries = [];

const elements = {
    nameFilter: document.getElementById('nameFilter'),
    refreshBtn: document.getElementById('refreshBtn'),
    configForm: document.getElementById('configForm'),
    resetBtn: document.getElementById('resetBtn'),
    configTableBody: document.getElementById('configTableBody'),
    formTitle: document.getElementById('formTitle'),
    entryId: document.getElementById('entryId'),
    name: document.getElementById('name'),
    applicationName: document.getElementById('applicationName'),
    type: document.getElementById('type'),
    value: document.getElementById('value'),
    isActive: document.getElementById('isActive')
};

async function loadEntries() {
    const response = await fetch(apiBase);
    if (!response.ok) {
        throw new Error('Kayıtlar yüklenemedi.');
    }

    allEntries = await response.json();
    renderTable();
}

function renderTable() {
    const filter = elements.nameFilter.value.trim().toLowerCase();
    const filtered = allEntries.filter(entry =>
        !filter || entry.name.toLowerCase().includes(filter));

    elements.configTableBody.innerHTML = filtered.map(entry => `
        <tr>
            <td>${entry.id}</td>
            <td>${entry.name}</td>
            <td>${entry.type}</td>
            <td>${entry.value}</td>
            <td>${entry.isActive ? '1' : '0'}</td>
            <td>${entry.applicationName}</td>
            <td>${new Date(entry.updatedAt).toLocaleString()}</td>
            <td class="table-actions">
                <button type="button" data-action="edit" data-id="${entry.id}">Düzenle</button>
                <button type="button" class="danger" data-action="delete" data-id="${entry.id}">Sil</button>
            </td>
        </tr>
    `).join('');
}

function resetForm() {
    elements.formTitle.textContent = 'Yeni Kayıt';
    elements.entryId.value = '';
    elements.configForm.reset();
    elements.isActive.checked = true;
}

function fillForm(entry) {
    elements.formTitle.textContent = 'Kayıt Güncelle';
    elements.entryId.value = entry.id;
    elements.name.value = entry.name;
    elements.applicationName.value = entry.applicationName;
    elements.type.value = entry.type;
    elements.value.value = entry.value;
    elements.isActive.checked = entry.isActive;
}

async function saveEntry(event) {
    event.preventDefault();

    const payload = {
        name: elements.name.value.trim(),
        applicationName: elements.applicationName.value.trim(),
        type: elements.type.value,
        value: elements.value.value.trim(),
        isActive: elements.isActive.checked
    };

    const id = elements.entryId.value;
    const url = id ? `${apiBase}/${id}` : apiBase;
    const method = id ? 'PUT' : 'POST';

    const response = await fetch(url, {
        method,
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload)
    });

    if (!response.ok) {
        alert('Kayıt kaydedilemedi.');
        return;
    }

    resetForm();
    await loadEntries();
}

async function deleteEntry(id) {
    if (!confirm('Bu kaydı silmek istediğinize emin misiniz?')) {
        return;
    }

    const response = await fetch(`${apiBase}/${id}`, { method: 'DELETE' });
    if (!response.ok) {
        alert('Kayıt silinemedi.');
        return;
    }

    await loadEntries();
}

elements.nameFilter.addEventListener('input', renderTable);
elements.refreshBtn.addEventListener('click', () => loadEntries().catch(console.error));
elements.resetBtn.addEventListener('click', resetForm);
elements.configForm.addEventListener('submit', (event) => saveEntry(event).catch(console.error));

elements.configTableBody.addEventListener('click', (event) => {
    const button = event.target.closest('button');
    if (!button) {
        return;
    }

    const id = Number(button.dataset.id);
    const entry = allEntries.find(item => item.id === id);
    if (!entry) {
        return;
    }

    if (button.dataset.action === 'edit') {
        fillForm(entry);
        return;
    }

    if (button.dataset.action === 'delete') {
        deleteEntry(id).catch(console.error);
    }
});

loadEntries().catch(console.error);
