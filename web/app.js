// URL Base configurada en config.js
const API_URL = CONFIG.API_BASE_URL + '/api';

// Utilidad Debounce para evitar spam de peticiones
const _ = {
    debounce(func, delay) {
        let timeoutId;
        return function (...args) {
            clearTimeout(timeoutId);
            timeoutId = setTimeout(() => {
                func.apply(this, args);
            }, delay);
        };
    }
};

// Elementos del DOM
const loginScreen = document.getElementById('login-screen');
const dashboardScreen = document.getElementById('dashboard-screen');
const loginForm = document.getElementById('login-form');
const logoutBtn = document.getElementById('logout-btn');

// Estado de la app
let appState = {
    adminId: localStorage.getItem('meritumAdminId') || null,
    adminName: localStorage.getItem('meritumAdminName') || '',
    categories: [],
    projects: []
};

// ==========================================
// 1. INICIALIZACIÓN
// ==========================================
document.addEventListener('DOMContentLoaded', () => {
    checkAuth();
    setupNavigation();
});

function checkAuth() {
    if (appState.adminId) {
        showDashboard();
    } else {
        showLogin();
    }
}

function showLogin() {
    loginScreen.classList.add('active');
    dashboardScreen.classList.remove('active');
}

function showDashboard() {
    loginScreen.classList.remove('active');
    dashboardScreen.classList.add('active');
    document.getElementById('user-role-label').textContent = "Admin: " + appState.adminName;
    loadCategories(); // Cargar datos iniciales
    loadProjects();
}

// ==========================================
// 2. AUTENTICACIÓN (LOGIN/LOGOUT)
// ==========================================
loginForm.addEventListener('submit', async (e) => {
    e.preventDefault();
    const email = document.getElementById('email').value;
    const password = document.getElementById('password').value;
    const btn = loginForm.querySelector('button');

    try {
        btn.disabled = true;
        btn.innerHTML = "<i class='bx bx-loader-alt bx-spin'></i> Verificando...";

        const response = await fetch(`${API_URL}/Auth/admin-login`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ email, password })
        });

        const data = await response.json();

        if (response.ok) {
            // Guardar sesión
            appState.adminId = data.id || data.Id; // Fix para asegurar que saca la ID enviada
            appState.adminName = data.email ? data.email.split('@')[0] : 'Admin';
            localStorage.setItem('meritumAdminId', appState.adminId);
            localStorage.setItem('meritumAdminName', appState.adminName);

            showToast('¡Bienvenido de nuevo, Administrador!', 'success');
            showDashboard();
            loginForm.reset();
        } else {
            showToast(data.message || 'Credenciales incorrectas.', 'error');
        }
    } catch (error) {
        console.error('Error in login:', error);
        showToast('Error de conexión con el servidor.', 'error');
    } finally {
        btn.disabled = false;
        btn.innerHTML = "<span>Iniciar Sesión</span><i class='bx bx-right-arrow-alt'></i>";
    }
});

logoutBtn.addEventListener('click', () => {
    localStorage.removeItem('meritumAdminId');
    localStorage.removeItem('meritumAdminName');
    appState.adminId = null;
    showLogin();
});

// ==========================================
// 3. NAVEGACIÓN Y VISTAS
// ==========================================
function setupNavigation() {
    const navLinks = document.querySelectorAll('.nav-links li');
    const views = document.querySelectorAll('.view-section');
    const titleObj = document.getElementById('current-view-title');

    navLinks.forEach(link => {
        link.addEventListener('click', () => {
            // Activar link
            navLinks.forEach(l => l.classList.remove('active'));
            link.classList.add('active');

            // Mostrar vista
            const targetView = link.getAttribute('data-view');
            views.forEach(v => v.classList.remove('active'));
            document.getElementById(`view-${targetView}`).classList.add('active');

            // Cambiar título superior
            titleObj.textContent = targetView === 'projects' ? 'Gestión de Proyectos' : 'Gestión de Categorías';
        });
    });
}

// ==========================================
// 4. CATEGORÍAS (CRUD)
// ==========================================
async function loadCategories() {
    try {
        const res = await fetch(`${API_URL}/Categories`);
        const data = await res.json();
        appState.categories = data;
        renderCategoriesTable();
        updateCategorySelects();
    } catch (error) {
        showToast('Error cargando categorías.', 'error');
    }
}

function renderCategoriesTable() {
    const tbody = document.getElementById('categories-table-body');
    tbody.innerHTML = '';

    appState.categories.forEach(cat => {
        const tr = document.createElement('tr');
        // Renderizamos el ícono si existe, o un cubo por defecto
        const iconHtml = cat.iconUrl ? `<i class='bx ${cat.iconUrl}' style='font-size:1.2rem; color:var(--primary); margin-right:8px; vertical-align:middle;'></i>` : '';

        tr.innerHTML = `
            <td>${iconHtml}<strong>${cat.name}</strong></td>
            <td><span class="badge badge-info">${cat.id}</span></td>
            <td style="text-align: center;">
                <button class="btn-icon edit" onclick="editCategory('${cat.id}')" title="Editar">
                    <i class='bx bx-edit-alt'></i>
                </button>
                <button class="btn-icon delete" onclick="deleteCategory('${cat.id}')" title="Eliminar">
                    <i class='bx bx-trash'></i>
                </button>
            </td>
        `;
        tbody.appendChild(tr);
    });
}

function editCategory(id) {
    const cat = appState.categories.find(c => c.id === id);
    if (!cat) return;

    document.getElementById('category-id').value = cat.id;
    document.getElementById('category-name').value = cat.name;
    document.getElementById('category-icon').value = cat.iconUrl || '';

    document.getElementById('category-modal-title').textContent = "Editar Categoría";
    openModal('category-modal');
}

function updateCategorySelects() {
    const selects = [document.getElementById('filter-category'), document.getElementById('project-category')];
    selects.forEach(sel => {
        if (!sel) return;
        sel.innerHTML = sel.id === 'filter-category' ? '<option value="">Todas las categorías</option>' : '';
        appState.categories.forEach(cat => {
            const opt = document.createElement('option');
            opt.value = cat.id;
            opt.textContent = cat.name;
            sel.appendChild(opt);
        });
    });
}

// Búsqueda cruzada para filtros
document.getElementById('filter-category').addEventListener('change', loadProjects);
document.getElementById('search-project').addEventListener('input', _.debounce(loadProjects, 500));

document.getElementById('category-form').addEventListener('submit', async (e) => {
    e.preventDefault();
    const id = document.getElementById('category-id').value;
    const name = document.getElementById('category-name').value;
    const icon = document.getElementById('category-icon').value;
    const btn = document.getElementById('save-category-btn');

    try {
        btn.disabled = true;
        btn.innerHTML = "Guardando...";

        const isEditing = id !== "";
        const url = isEditing
            ? `${API_URL}/Categories/${id}?adminId=${appState.adminId}`
            : `${API_URL}/Categories?adminId=${appState.adminId}`;

        const method = isEditing ? 'PUT' : 'POST';

        const res = await fetch(url, {
            method: method,
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ name: name, iconUrl: icon })
        });

        const data = await res.json();
        if (res.ok) {
            showToast(`Categoría ${isEditing ? 'actualizada' : 'creada'} exitosamente`, 'success');
            closeModal('category-modal');
            document.getElementById('category-form').reset();
            loadCategories();
        } else {
            showToast(data.message || 'Error al guardar.', 'error');
        }
    } catch (err) {
        showToast('Error de conexión', 'error');
    } finally {
        btn.disabled = false;
        btn.innerHTML = "Guardar";
    }
});

async function deleteCategory(id) {
    if (!confirm('¿Estás seguro de eliminar esta categoría? Si tiene proyectos asociados, podrías tener problemas de integridad.')) return;

    try {
        const res = await fetch(`${API_URL}/Categories/${id}?adminId=${appState.adminId}`, { method: 'DELETE' });
        if (res.ok) {
            showToast('Categoría eliminada', 'success');
            loadCategories();
        } else {
            const d = await res.json();
            showToast(d.message || 'No se pudo eliminar', 'error');
        }
    } catch (e) {
        showToast('Error de conexión', 'error');
    }
}

// ==========================================
// 5. PROYECTOS (CRUD COMPLETO CON ARCHIVOS)
// ==========================================
async function loadProjects() {
    const categoryId = document.getElementById('filter-category').value;
    const searchTerm = document.getElementById('search-project').value;

    let url = `${API_URL}/Projects?`;
    if (categoryId) url += `categoryId=${categoryId}&`;
    if (searchTerm) url += `searchTerm=${searchTerm}`;

    try {
        const res = await fetch(url);
        const data = await res.json();
        appState.projects = data;
        renderProjectsTable();
    } catch (error) {
        showToast('Error cargando proyectos.', 'error');
    }
}

function renderProjectsTable() {
    const tbody = document.getElementById('projects-table-body');
    tbody.innerHTML = '';

    if (appState.projects.length === 0) {
        tbody.innerHTML = `<tr><td colspan="5" style="text-align: center; color: #6B7280; padding: 2rem;">No hay proyectos registrados aún.</td></tr>`;
        return;
    }

    appState.projects.forEach(p => {
        // Buscar el nombre de la categoría
        const categoryData = appState.categories.find(c => c.id === p.categoryId);
        const categoryName = categoryData ? categoryData.name : 'Sin Categoría';

        // Imagen placeholder si no hay
        const imgBlock = p.imageUrl
            ? `<img src="${p.imageUrl}" class="project-thumbnail" alt="Thumb">`
            : `<div class="empty-thumbnail"><i class='bx bx-image'></i></div>`;

        const tr = document.createElement('tr');
        tr.innerHTML = `
            <td>${imgBlock}</td>
            <td><strong>${p.title}</strong></td>
            <td><span class="badge">${categoryName}</span></td>
            <td style="font-size: 0.875rem;">${p.teamMembers || 'N/A'}</td>
            <td>
                <!-- Por ahora Edit lo dejamos comentado si no hemos implementado el modo UPDATE de archivos, pero puedes agregarlo facilmente -->
                <!-- <button class="btn-icon edit" title="Editar"><i class='bx bx-edit-alt'></i></button> -->
                <button class="btn-icon delete" onclick="deleteProject('${p.id}')" title="Eliminar">
                    <i class='bx bx-trash'></i>
                </button>
            </td>
        `;
        tbody.appendChild(tr);
    });
}

function editProject(id) {
    const p = appState.projects.find(x => x.id === id);
    if (!p) return;

    document.getElementById('project-id').value = p.id;
    document.getElementById('project-title').value = p.title;
    document.getElementById('project-category').value = p.categoryId;
    document.getElementById('project-members').value = p.teamMembers || '';
    document.getElementById('project-desc').value = p.description || '';

    // Reset previews
    document.getElementById('image-preview').style.display = 'none';
    document.getElementById('image-icon').style.display = 'block';
    document.getElementById('video-preview-name').textContent = '';
    document.getElementById('docs-preview-list').innerHTML = '';

    // Show existing image preview if available
    if (p.imageUrl) {
        document.getElementById('image-preview').src = p.imageUrl;
        document.getElementById('image-preview').style.display = 'block';
        document.getElementById('image-icon').style.display = 'none';
    }

    if (p.videoUrl) {
        document.getElementById('video-preview-name').textContent = "Video actual guardado";
    }

    if (p.documentUrls && p.documentUrls.length > 0) {
        const ul = document.getElementById('docs-preview-list');
        ul.innerHTML = `<li>${p.documentUrls.length} documento(s) guardado(s)</li>`;
    }

    document.getElementById('project-modal-title').textContent = "Editar Proyecto";
    openModal('project-modal');
}

document.getElementById('project-form').addEventListener('submit', async (e) => {
    e.preventDefault();
    const btn = document.getElementById('save-project-btn');
    const id = document.getElementById('project-id').value;

    try {
        const title = document.getElementById('project-title').value || '';
        const categoryId = document.getElementById('project-category').value || '';
        const teamMembers = document.getElementById('project-members').value || '';
        const description = document.getElementById('project-desc').value || '';

        // Archivos
        const imageInput = document.getElementById('project-image');
        const videoInput = document.getElementById('project-video');
        const docsInput = document.getElementById('project-docs');

        const formData = new FormData();
        formData.append('Title', title);
        formData.append('CategoryId', categoryId);

        // Solo enviar si no están vacíos (para que .NET los tome como null si están ausentes)
        if (teamMembers.trim() !== '') formData.append('TeamMembers', teamMembers);
        if (description.trim() !== '') formData.append('Description', description);

        // Si es edición, C# espera que la clase completa se empareje para evitar nulos sobreescribiendo
        if (id) formData.append('Id', id);

        if (imageInput && imageInput.files && imageInput.files.length > 0) {
            formData.append('ImageFile', imageInput.files[0]);
        }
        if (videoInput && videoInput.files && videoInput.files.length > 0) {
            formData.append('VideoFile', videoInput.files[0]);
        }
        if (docsInput && docsInput.files) {
            for (let i = 0; i < docsInput.files.length; i++) {
                formData.append('DocumentFiles', docsInput.files[i]);
            }
        }

        btn.disabled = true;
        btn.innerHTML = "Subiendo archivos...";

        const isEditing = id !== "";
        const url = isEditing
            ? `${API_URL}/Projects/${id}?adminId=${appState.adminId}`
            : `${API_URL}/Projects?adminId=${appState.adminId}`;

        const method = isEditing ? 'PUT' : 'POST';

        const res = await fetch(url, {
            method: method,
            body: formData
        });

        if (res.ok) {
            showToast(`Proyecto ${isEditing ? 'actualizado' : 'guardado'} con éxito`, 'success');
            closeModal('project-modal');
            document.getElementById('project-form').reset();

            // Limpiar previews manuales en cascarilla
            document.getElementById('image-preview').style.display = 'none';
            document.getElementById('image-icon').style.display = 'block';
            document.getElementById('video-preview-name').textContent = '';
            document.getElementById('docs-preview-list').innerHTML = '';

            loadProjects();
        } else {
            const data = await res.json();
            showToast(data.message || 'Error al guardar.', 'error');
        }
    } catch (err) {
        console.error("Submit Error:", err);
        showToast('Error de conexión o archivo demasiado grande.', 'error');
    } finally {
        btn.disabled = false;
        btn.innerHTML = "Guardar Proyecto";
    }
});

// ==========================================
// PREVISUALIZACIÓN DE ARCHIVOS
// ==========================================
document.getElementById('project-image').addEventListener('change', function (e) {
    const file = e.target.files[0];
    const preview = document.getElementById('image-preview');
    const icon = document.getElementById('image-icon');

    if (file) {
        const reader = new FileReader();
        reader.onload = function (e) {
            preview.src = e.target.result;
            preview.style.display = 'block';
            icon.style.display = 'none';
        }
        reader.readAsDataURL(file);
    } else {
        preview.style.display = 'none';
        icon.style.display = 'block';
    }
});

document.getElementById('project-video').addEventListener('change', function (e) {
    const file = e.target.files[0];
    const preview = document.getElementById('video-preview-name');
    if (file) {
        preview.textContent = "🎬 " + file.name;
    } else {
        preview.textContent = "";
    }
});

document.getElementById('project-docs').addEventListener('change', function (e) {
    const files = e.target.files;
    const list = document.getElementById('docs-preview-list');
    list.innerHTML = '';

    if (files.length > 0) {
        for (let i = 0; i < files.length; i++) {
            const li = document.createElement('li');
            li.textContent = "📄 " + files[i].name;
            list.appendChild(li);
        }
    }
});

async function deleteProject(id) {
    if (!confirm('¿Estás seguro de que deseas eliminar este proyecto completamente?')) return;

    try {
        const res = await fetch(`${API_URL}/Projects/${id}?adminId=${appState.adminId}`, { method: 'DELETE' });
        if (res.ok) {
            showToast('Proyecto eliminado', 'success');
            loadProjects();
        } else {
            const d = await res.json();
            showToast(d.message || 'No se pudo eliminar', 'error');
        }
    } catch (e) {
        showToast('Error de conexión', 'error');
    }
}

// ==========================================
// UTILS: Modales y Toasts y Lodash básico
// ==========================================
function openModal(id) {
    const modal = document.getElementById(id);
    if (modal) modal.classList.add('active');
}

function closeModal(id) {
    const modal = document.getElementById(id);
    if (modal) {
        modal.classList.remove('active');
        // si es un form, lo limpiamos al cerrar
        const form = modal.querySelector('form');
        if (form) form.reset();
    }
}

function showToast(message, type = 'success') {
    const container = document.getElementById('toast-container');
    const toast = document.createElement('div');
    toast.className = `toast ${type}`;

    const icon = type === 'success' ? 'bx-check-circle' : 'bx-x-circle';

    toast.innerHTML = `<i class='bx ${icon}'></i> <span>${message}</span>`;
    container.appendChild(toast);

    setTimeout(() => {
        toast.style.animation = 'fadeOut 0.3s forwards';
        setTimeout(() => toast.remove(), 300);
    }, 3000);
}

