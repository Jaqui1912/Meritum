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

let removeExistingImage = false;
let removeExistingPreviewVideo = false;
let keptVideoUrls = [];
let keptDocumentUrls = [];
let externalVideoUrls = [];
let currentVideosTransfer = new DataTransfer();
let projectTechnologies = []; // NUEVO: Estado para las tecnologías del form

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

async function showDashboard() {
    loginScreen.classList.remove('active');
    dashboardScreen.classList.add('active');
    document.getElementById('user-role-label').textContent = "Admin: " + appState.adminName;

    // Cargar categorías primero para que cuando carguen los proyectos, puedan mapear los nombres correctamente
    await loadCategories();
    await loadProjects();
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
        const reqOpts = { method: method };

        if (isEditing) {
            reqOpts.headers = { 'Content-Type': 'application/json' };
            reqOpts.body = JSON.stringify({ name: name, iconUrl: icon });
        } else {
            const fd = new FormData();
            fd.append('Name', name);
            if (icon) fd.append('IconUrl', icon);
            reqOpts.body = fd;
        }

        const res = await fetch(url, reqOpts);

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
        tbody.innerHTML = `<tr><td colspan="7" style="text-align: center; color: #6B7280; padding: 2rem;">No hay proyectos registrados aún.</td></tr>`;
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

        // Extraer nombre de video(s)
        let videoStr = '<span style="color:#9CA3AF; font-size:0.8rem;">N/A</span>';
        if (p.videoUrls && p.videoUrls.length > 0) {
            videoStr = `<span class="badge" style="background:var(--secondary); color:#A16207;">🎬 ${p.videoUrls.length}</span>`;
        }

        // Documentos count
        let docsStr = '<span style="color:#9CA3AF; font-size:0.8rem;">N/A</span>';
        if (p.documentUrls && p.documentUrls.length > 0) {
            docsStr = `<span class="badge" style="background:var(--secondary); color:#A16207;">📄 ${p.documentUrls.length}</span>`;
        }

        // Tecnologías tags
        let techStr = '<span style="color:#9CA3AF; font-size:0.8rem;">Ninguna</span>';
        if (p.technologies && p.technologies.length > 0) {
            techStr = p.technologies.map(t => `<span class="badge" style="background:#5B21B6; color:white; font-size: 0.75rem;">${t}</span>`).join(' ');
        }

        const tr = document.createElement('tr');
        tr.innerHTML = `
            <td>${imgBlock}</td>
            <td><strong>${p.title}</strong></td>
            <td><span class="badge">${categoryName}</span></td>
            <td>${techStr}</td>
            <td>${videoStr}</td>
            <td>${docsStr}</td>
            <td style="font-size: 0.875rem;">${p.teamMembers || 'N/A'}</td>
            <td>
                <button class="btn-icon edit" onclick="editProject('${p.id}')" title="Editar">
                    <i class='bx bx-edit-alt'></i>
                </button>
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
    document.getElementById('image-preview-container').style.display = 'none';
    document.getElementById('image-icon').style.display = 'block';

    document.getElementById('videos-preview-list').innerHTML = '';
    document.getElementById('docs-preview-list').innerHTML = '';

    removeExistingImage = false;
    removeExistingPreviewVideo = false;
    keptVideoUrls = p.videoUrls ? [...p.videoUrls] : [];
    keptDocumentUrls = p.documentUrls ? [...p.documentUrls] : [];
    externalVideoUrls = [];
    currentDocsTransfer = new DataTransfer();
    currentVideosTransfer = new DataTransfer();
    document.getElementById('project-docs').value = "";
    document.getElementById('project-video').value = "";
    document.getElementById('external-video-url').value = "";
    document.getElementById('project-preview-video').value = "";
    document.getElementById('remove-existing-preview-video').value = 'false';

    // Tecnologías
    projectTechnologies = p.technologies ? [...p.technologies] : [];
    renderTechTags();

    // Show existing image preview if available
    if (p.imageUrl) {
        document.getElementById('image-preview').src = p.imageUrl;
        document.getElementById('image-preview-container').style.display = 'block';
        document.getElementById('image-icon').style.display = 'none';

        const btn = document.getElementById('remove-image-btn');
        if (btn) btn.style.display = 'flex';
    }

    // Show existing preview video if available
    const pvContainer = document.getElementById('preview-video-preview-container');
    const pvPlayer = document.getElementById('preview-video-player');
    const pvIcon = document.getElementById('preview-video-icon');
    const pvRemoveBtn = document.getElementById('remove-preview-video-btn');
    pvContainer.style.display = 'none';
    pvPlayer.src = '';
    pvIcon.style.display = 'block';
    if (pvRemoveBtn) pvRemoveBtn.style.display = 'none';

    if (p.previewVideoUrl) {
        pvPlayer.src = p.previewVideoUrl;
        pvContainer.style.display = 'block';
        pvIcon.style.display = 'none';
        if (pvRemoveBtn) pvRemoveBtn.style.display = 'flex';
    }

    renderVideosList();
    renderDocsList();

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
        if (projectTechnologies.length > 0) formData.append('Technologies', projectTechnologies.join(','));

        // Si es edición, C# espera que la clase completa se empareje para evitar nulos sobreescribiendo
        if (id) formData.append('Id', id);

        if (imageInput && imageInput.files && imageInput.files.length > 0) {
            formData.append('ImageFile', imageInput.files[0]);
        }
        if (videoInput && videoInput.files && videoInput.files.length > 0) {
            for (let i = 0; i < videoInput.files.length; i++) {
                formData.append('VideoFiles', videoInput.files[i]);
            }
        }
        if (docsInput && docsInput.files) {
            for (let i = 0; i < docsInput.files.length; i++) {
                formData.append('DocumentFiles', docsInput.files[i]);
            }
        }

        formData.append('RemoveExistingImage', removeExistingImage);

        // Preview video
        const previewVideoInput = document.getElementById('project-preview-video');
        if (previewVideoInput && previewVideoInput.files && previewVideoInput.files.length > 0) {
            formData.append('PreviewVideoFile', previewVideoInput.files[0]);
        }
        formData.append('RemoveExistingPreviewVideo', removeExistingPreviewVideo);

        if (keptVideoUrls.length > 0) {
            formData.append('KeptVideoUrls', keptVideoUrls.join(','));
        }
        if (keptDocumentUrls.length > 0) {
            formData.append('KeptDocumentUrls', keptDocumentUrls.join(','));
        }
        
        externalVideoUrls.forEach(url => formData.append('ExternalVideoUrls', url));

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
            document.getElementById('videos-preview-list').innerHTML = '';
            document.getElementById('docs-preview-list').innerHTML = '';
            currentVideosTransfer = new DataTransfer();
            currentDocsTransfer = new DataTransfer();
            keptVideoUrls = [];
            keptDocumentUrls = [];
            externalVideoUrls = [];

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

// Wrapper para Nuevo Proyecto
function openNewProjectModal() {
    projectTechnologies = [];
    renderTechTags();
    document.getElementById('project-form').reset();
    document.getElementById('project-id').value = "";
    document.getElementById('project-modal-title').textContent = "Nuevo Proyecto";
    
    document.getElementById('image-preview-container').style.display = 'none';
    document.getElementById('image-icon').style.display = 'block';
    document.getElementById('videos-preview-list').innerHTML = '';
    document.getElementById('docs-preview-list').innerHTML = '';
    document.getElementById('preview-video-preview-container').style.display = 'none';
    document.getElementById('preview-video-icon').style.display = 'block';
    
    currentVideosTransfer = new DataTransfer();
    currentDocsTransfer = new DataTransfer();
    keptVideoUrls = [];
    keptDocumentUrls = [];
    externalVideoUrls = [];
    removeExistingImage = false;
    removeExistingPreviewVideo = false;
    
    openModal('project-modal');
}

// ==========================================
// PREVISUALIZACIÓN DE ARCHIVOS YAÑADIR TECNOLOGÍAS
// ==========================================

// Logica de tecnologías
document.getElementById('add-tech-btn').addEventListener('click', () => {
    const input = document.getElementById('tech-input');
    const val = input.value.trim();
    if(val) {
        if(!projectTechnologies.includes(val)) {
            projectTechnologies.push(val);
            renderTechTags();
        }
        input.value = '';
    }
});

function renderTechTags() {
    const container = document.getElementById('tech-tags-container');
    container.innerHTML = '';
    projectTechnologies.forEach((tech, index) => {
        const badge = document.createElement('div');
        badge.style.cssText = "background: #5B21B6; color: white; padding: 4px 10px; border-radius: 999px; font-size: 0.8rem; display: flex; align-items: center; gap: 5px;";
        badge.innerHTML = `
            ${tech}
            <i class='bx bx-x' style='cursor:pointer; font-size: 1.1rem; opacity: 0.7;' onmouseover="this.style.opacity='1'" onmouseout="this.style.opacity='0.7'" onclick="removeTech(${index})"></i>
        `;
        container.appendChild(badge);
    });
}

window.removeTech = function(index) {
    projectTechnologies.splice(index, 1);
    renderTechTags();
};

document.getElementById('project-image').addEventListener('change', function (e) {
    const file = e.target.files[0];
    const previewContainer = document.getElementById('image-preview-container');
    const preview = document.getElementById('image-preview');
    const icon = document.getElementById('image-icon');
    const btn = document.getElementById('remove-image-btn');

    if (file) {
        const reader = new FileReader();
        reader.onload = function (e) {
            preview.src = e.target.result;
            previewContainer.style.display = 'block';
            icon.style.display = 'none';
            if (btn) btn.style.display = 'flex';
        }
        reader.readAsDataURL(file);
    } else {
        previewContainer.style.display = 'none';
        icon.style.display = 'block';
    }
});

document.getElementById('remove-image-btn').addEventListener('click', function (e) {
    e.preventDefault();
    e.stopPropagation();
    document.getElementById('project-image').value = ""; // clear file
    removeExistingImage = true;
    document.getElementById('image-preview-container').style.display = 'none';
    document.getElementById('image-icon').style.display = 'block';
});

// Preview Video (presentación ~10s)
document.getElementById('project-preview-video').addEventListener('change', function (e) {
    const file = e.target.files[0];
    const container = document.getElementById('preview-video-preview-container');
    const player = document.getElementById('preview-video-player');
    const icon = document.getElementById('preview-video-icon');
    const btn = document.getElementById('remove-preview-video-btn');

    if (file) {
        const fileUrl = URL.createObjectURL(file);
        player.src = fileUrl;
        player.style.display = 'block';
        container.style.display = 'block';
        icon.style.display = 'none';
        if (btn) btn.style.display = 'flex';
    } else {
        player.src = "";
        container.style.display = 'none';
        icon.style.display = 'block';
    }
});

document.getElementById('remove-preview-video-btn').addEventListener('click', function (e) {
    e.preventDefault();
    e.stopPropagation();
    document.getElementById('project-preview-video').value = "";
    removeExistingPreviewVideo = true;
    document.getElementById('remove-existing-preview-video').value = 'true';

    const player = document.getElementById('preview-video-player');
    if (player) { player.pause(); player.src = ""; }

    document.getElementById('preview-video-preview-container').style.display = 'none';
    document.getElementById('preview-video-icon').style.display = 'block';
});

document.getElementById('project-video').addEventListener('change', function (e) {
    const input = e.target;
    if (input.files && input.files.length > 0) {
        for (let i = 0; i < input.files.length; i++) {
            currentVideosTransfer.items.add(input.files[i]);
        }
    }
    input.files = currentVideosTransfer.files;
    renderVideosList();
});

let currentDocsTransfer = new DataTransfer();

document.getElementById('project-docs').addEventListener('change', function (e) {
    const input = e.target;
    if (input.files && input.files.length > 0) {
        for (let i = 0; i < input.files.length; i++) {
            currentDocsTransfer.items.add(input.files[i]);
        }
    }
    input.files = currentDocsTransfer.files;
    renderDocsList();
});

function renderDocsList() {
    const list = document.getElementById('docs-preview-list');
    list.innerHTML = '';

    // Renderizar documentos que ya estaban guardados en la nube
    keptDocumentUrls.forEach((docUrl) => {
        const parts = docUrl.split('/');
        const name = parts[parts.length - 1].split('_').pop();

        const li = document.createElement('li');
        li.style.position = 'relative';
        li.style.padding = '8px 30px 8px 8px';
        li.style.background = 'rgba(79, 70, 229, 0.05)';
        li.style.marginBottom = '6px';
        li.style.borderRadius = '6px';
        li.textContent = "☁️ " + name;

        const btn = document.createElement('button');
        btn.type = 'button';
        btn.className = 'remove-file-btn';
        btn.innerHTML = `<svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" fill="#484848" viewBox="2 2 20 20"><path d="M14.83 7.76 12 10.59 9.17 7.76 7.76 9.17 10.59 12l-2.83 2.83 1.41 1.41L12 13.41l2.83 2.83 1.41-1.41L13.41 12l2.83-2.83z"></path><path d="M12 2C9.33 2 6.82 3.04 4.93 4.93S2 9.33 2 12s1.04 5.18 2.93 7.07c1.95 1.95 4.51 2.92 7.07 2.92s5.12-.97 7.07-2.92S22 14.67 22 12s-1.04-5.18-2.93-7.07A9.93 9.93 0 0 0 12 2m5.66 15.66c-3.12 3.12-8.19 3.12-11.31 0-1.51-1.51-2.34-3.52-2.34-5.66s.83-4.15 2.34-5.66S9.87 4 12.01 4s4.15.83 5.66 2.34 2.34 3.52 2.34 5.66-.83 4.15-2.34 5.66Z"></path></svg>`;
        btn.style.transform = 'none';
        btn.style.top = '50%';
        btn.style.transform = 'translateY(-50%)';
        btn.style.right = '4px';

        btn.onclick = function (e) {
            e.preventDefault();
            e.stopPropagation();
            keptDocumentUrls = keptDocumentUrls.filter(u => u !== docUrl);
            renderDocsList();
        };

        li.appendChild(btn);
        list.appendChild(li);
    });

    const files = currentDocsTransfer.files;
    if (files.length > 0) {
        for (let i = 0; i < files.length; i++) {
            const li = document.createElement('li');
            li.style.position = 'relative';
            li.style.padding = '8px 30px 8px 8px';
            li.style.background = 'rgba(250, 116, 43, 0.05)';
            li.style.marginBottom = '6px';
            li.style.borderRadius = '6px';
            li.textContent = "📄 " + files[i].name;

            const btn = document.createElement('button');
            btn.type = 'button';
            btn.className = 'remove-file-btn';
            btn.innerHTML = `<svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" fill="#484848" viewBox="2 2 20 20"><path d="M14.83 7.76 12 10.59 9.17 7.76 7.76 9.17 10.59 12l-2.83 2.83 1.41 1.41L12 13.41l2.83 2.83 1.41-1.41L13.41 12l2.83-2.83z"></path><path d="M12 2C9.33 2 6.82 3.04 4.93 4.93S2 9.33 2 12s1.04 5.18 2.93 7.07c1.95 1.95 4.51 2.92 7.07 2.92s5.12-.97 7.07-2.92S22 14.67 22 12s-1.04-5.18-2.93-7.07A9.93 9.93 0 0 0 12 2m5.66 15.66c-3.12 3.12-8.19 3.12-11.31 0-1.51-1.51-2.34-3.52-2.34-5.66s.83-4.15 2.34-5.66S9.87 4 12.01 4s4.15.83 5.66 2.34 2.34 3.52 2.34 5.66-.83 4.15-2.34 5.66Z"></path></svg>`;
            btn.style.transform = 'none';
            btn.style.top = '50%';
            btn.style.transform = 'translateY(-50%)';
            btn.style.right = '4px';

            btn.onclick = function (e) {
                e.preventDefault();
                e.stopPropagation();
                removeDocFile(i);
            };

            li.appendChild(btn);
            list.appendChild(li);
        }
    }
}

function removeDocFile(index) {
    const input = document.getElementById('project-docs');
    const newDt = new DataTransfer();

    for (let i = 0; i < currentDocsTransfer.files.length; i++) {
        if (i !== index) {
            newDt.items.add(currentDocsTransfer.files[i]);
        }
    }

    currentDocsTransfer = newDt;
    input.files = currentDocsTransfer.files;
    renderDocsList(); // re-render list without the removed item
}

document.getElementById('add-external-video-btn').addEventListener('click', function(e) {
    e.preventDefault();
    const input = document.getElementById('external-video-url');
    let url = input.value.trim();
    if (url) {
        // Sistema inteligente para transformar URLs de Google Drive a enlaces directos de descarga/streaming
        if (url.includes('drive.google.com')) {
            let fileId = null;
            if (url.includes('/file/d/')) {
                const match = url.match(/\/d\/([a-zA-Z0-9_-]+)/);
                if (match && match[1]) fileId = match[1];
            } else if (url.includes('id=')) {
                const match = url.match(/[?&]id=([a-zA-Z0-9_-]+)/);
                if (match && match[1]) fileId = match[1];
            }
            
            if (fileId) {
                // Convertir a formato de descarga directa para visualización sin login
                url = `https://drive.google.com/uc?export=download&id=${fileId}`;
            }
        }

        externalVideoUrls.push(url);
        input.value = "";
        renderVideosList();
    }
});

function renderVideosList() {
    const list = document.getElementById('videos-preview-list');
    list.innerHTML = '';

    // Renderizar videos que ya estaban guardados en la nube
    keptVideoUrls.forEach((vidUrl) => {
        let displayName = vidUrl;
        let prefix = "🔗 ";
        if (vidUrl.includes('/uploads/')) {
            const parts = vidUrl.split('/');
            displayName = parts[parts.length - 1].split('_').pop();
            prefix = "☁️ ";
        } else if (vidUrl.length > 40) {
            displayName = vidUrl.substring(0, 40) + '...';
        }

        const li = document.createElement('li');
        li.style.position = 'relative';
        li.style.padding = '8px 30px 8px 8px';
        li.style.background = 'rgba(79, 70, 229, 0.05)';
        li.style.marginBottom = '6px';
        li.style.borderRadius = '6px';
        li.textContent = prefix + displayName;

        const btn = document.createElement('button');
        btn.type = 'button';
        btn.className = 'remove-file-btn';
        btn.innerHTML = `<svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" fill="#484848" viewBox="2 2 20 20"><path d="M14.83 7.76 12 10.59 9.17 7.76 7.76 9.17 10.59 12l-2.83 2.83 1.41 1.41L12 13.41l2.83 2.83 1.41-1.41L13.41 12l2.83-2.83z"></path><path d="M12 2C9.33 2 6.82 3.04 4.93 4.93S2 9.33 2 12s1.04 5.18 2.93 7.07c1.95 1.95 4.51 2.92 7.07 2.92s5.12-.97 7.07-2.92S22 14.67 22 12s-1.04-5.18-2.93-7.07A9.93 9.93 0 0 0 12 2m5.66 15.66c-3.12 3.12-8.19 3.12-11.31 0-1.51-1.51-2.34-3.52-2.34-5.66s.83-4.15 2.34-5.66S9.87 4 12.01 4s4.15.83 5.66 2.34 2.34 3.52 2.34 5.66-.83 4.15-2.34 5.66Z"></path></svg>`;
        btn.style.transform = 'none';
        btn.style.top = '50%';
        btn.style.transform = 'translateY(-50%)';
        btn.style.right = '4px';

        btn.onclick = function (e) {
            e.preventDefault();
            e.stopPropagation();
            keptVideoUrls = keptVideoUrls.filter(u => u !== vidUrl);
            renderVideosList();
        };

        li.appendChild(btn);
        list.appendChild(li);
    });

    // Renderizar videos externos nuevos
    externalVideoUrls.forEach((extUrl, index) => {
        const li = document.createElement('li');
        li.style.position = 'relative';
        li.style.padding = '8px 30px 8px 8px';
        li.style.background = 'rgba(16, 185, 129, 0.05)';
        li.style.marginBottom = '6px';
        li.style.borderRadius = '6px';
        li.textContent = "🔗 " + (extUrl.length > 40 ? extUrl.substring(0, 40) + '...' : extUrl);

        const btn = document.createElement('button');
        btn.type = 'button';
        btn.className = 'remove-file-btn';
        btn.innerHTML = `<svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" fill="#484848" viewBox="2 2 20 20"><path d="M14.83 7.76 12 10.59 9.17 7.76 7.76 9.17 10.59 12l-2.83 2.83 1.41 1.41L12 13.41l2.83 2.83 1.41-1.41L13.41 12l2.83-2.83z"></path></svg>`;
        btn.style.transform = 'none';
        btn.style.top = '50%';
        btn.style.transform = 'translateY(-50%)';
        btn.style.right = '4px';

        btn.onclick = function (e) {
            e.preventDefault();
            e.stopPropagation();
            externalVideoUrls.splice(index, 1);
            renderVideosList();
        };

        li.appendChild(btn);
        list.appendChild(li);
    });

    // Renderizar videos nuevos (seleccionados localmente)
    const files = currentVideosTransfer.files;
    if (files.length > 0) {
        for (let i = 0; i < files.length; i++) {
            const li = document.createElement('li');
            li.style.position = 'relative';
            li.style.padding = '8px 30px 8px 8px';
            li.style.background = 'rgba(250, 116, 43, 0.05)';
            li.style.marginBottom = '6px';
            li.style.borderRadius = '6px';
            li.textContent = "🎬 " + files[i].name;

            const btn = document.createElement('button');
            btn.type = 'button';
            btn.className = 'remove-file-btn';
            btn.innerHTML = `<svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" fill="#484848" viewBox="2 2 20 20"><path d="M14.83 7.76 12 10.59 9.17 7.76 7.76 9.17 10.59 12l-2.83 2.83 1.41 1.41L12 13.41l2.83 2.83 1.41-1.41L13.41 12l2.83-2.83z"></path><path d="M12 2C9.33 2 6.82 3.04 4.93 4.93S2 9.33 2 12s1.04 5.18 2.93 7.07c1.95 1.95 4.51 2.92 7.07 2.92s5.12-.97 7.07-2.92S22 14.67 22 12s-1.04-5.18-2.93-7.07A9.93 9.93 0 0 0 12 2m5.66 15.66c-3.12 3.12-8.19 3.12-11.31 0-1.51-1.51-2.34-3.52-2.34-5.66s.83-4.15 2.34-5.66S9.87 4 12.01 4s4.15.83 5.66 2.34 2.34 3.52 2.34 5.66-.83 4.15-2.34 5.66Z"></path></svg>`;
            btn.style.transform = 'none';
            btn.style.top = '50%';
            btn.style.transform = 'translateY(-50%)';
            btn.style.right = '4px';

            btn.onclick = function (e) {
                e.preventDefault();
                e.stopPropagation();
                removeVideoFile(i);
            };

            li.appendChild(btn);
            list.appendChild(li);
        }
    }
}

function removeVideoFile(index) {
    const input = document.getElementById('project-video');
    const newDt = new DataTransfer();

    for (let i = 0; i < currentVideosTransfer.files.length; i++) {
        if (i !== index) {
            newDt.items.add(currentVideosTransfer.files[i]);
        }
    }

    currentVideosTransfer = newDt;
    input.files = currentVideosTransfer.files;
    renderVideosList();
}

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
        if (form) {
            form.reset();
            // Los campos hidden no se limpian con form.reset(), hay que forzarlos
            const hiddenId = form.querySelector('input[type="hidden"]');
            if (hiddenId) hiddenId.value = '';
        }

        // Limpieza visual extra si es proyecto
        if (id === 'project-modal') {
            const imgContainer = document.getElementById('image-preview-container');
            if (imgContainer) imgContainer.style.display = 'none';
            const imgIcon = document.getElementById('image-icon');
            if (imgIcon) imgIcon.style.display = 'block';

            // Preview video cleanup
            const pvContainer = document.getElementById('preview-video-preview-container');
            if (pvContainer) pvContainer.style.display = 'none';
            const pvPlayer = document.getElementById('preview-video-player');
            if (pvPlayer) { pvPlayer.pause(); pvPlayer.src = ''; }
            const pvIcon = document.getElementById('preview-video-icon');
            if (pvIcon) pvIcon.style.display = 'block';
            const pvRemoveBtn = document.getElementById('remove-preview-video-btn');
            if (pvRemoveBtn) pvRemoveBtn.style.display = 'none';
            removeExistingPreviewVideo = false;
            const pvHidden = document.getElementById('remove-existing-preview-video');
            if (pvHidden) pvHidden.value = 'false';

            document.getElementById('videos-preview-list').innerHTML = '';
            document.getElementById('docs-preview-list').innerHTML = '';
            currentVideosTransfer = new DataTransfer();
            currentDocsTransfer = new DataTransfer();
            keptVideoUrls = [];
            keptDocumentUrls = [];
            removeExistingImage = false;

            const btn1 = document.getElementById('remove-image-btn');
            if (btn1) btn1.style.display = 'flex';
        }
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

