const token = localStorage.getItem('token');
let currentUser = JSON.parse(localStorage.getItem('user') || '{}');

if (!token && window.location.pathname !== '/auth.html') {
    window.location.href = 'auth.html';
}

const API_URL = 'http://localhost:5203/api';

async function checkToken() {
    try {
        const response = await fetch(`${API_URL}/User/profile`, {
            headers: { 'Authorization': `Bearer ${token}` }
        });
        
        if (response.status === 401) {
            localStorage.removeItem('token');
            localStorage.removeItem('user');
            window.location.href = 'auth.html';
        }
    } catch (error) {
        console.error('Ошибка проверки токена');
    }
}

// Вызываем проверку токена
if (token) {
    checkToken();
}

// ========== ВСПОМОГАТЕЛЬНЫЕ ФУНКЦИИ ==========
function showError(message) {
    alert(message);
}

function showSuccess(message) {
    alert(message);
}

// ========== ПОЛЬЗОВАТЕЛИ (только для админа) ==========
async function loadUsers() {
    const container = document.getElementById('users-list');
    if (!container) return;
    
    if (currentUser.role !== 'Admin') {
        container.innerHTML = '<div class="error">Доступ запрещен. Только для администраторов.</div>';
        return;
    }
    
    container.innerHTML = '<div class="loading">Загрузка...</div>';
    
    try {
        const response = await fetch(`${API_URL}/User`, {
            headers: { 'Authorization': `Bearer ${token}` }
        });
        
        if (response.status === 403 || response.status === 401) {
            container.innerHTML = '<div class="error">Нет прав для просмотра пользователей</div>';
            return;
        }
        
        const users = await response.json();
        
        if (!users || !users.length) {
            container.innerHTML = '<div class="error">Нет пользователей</div>';
            return;
        }
        
        container.innerHTML = users.map(user => `
            <div class="card">
                <div class="card-header">
                    <div class="card-title">${user.user_name || 'Без имени'}</div>
                </div>
                <div class="card-content">
                    <div class="card-field"><strong>Email:</strong> ${user.email || '-'}</div>
                    <div class="card-field"><strong>Роль:</strong> ${user.role || 'Student'}</div>
                    <div class="card-field"><strong>Активен:</strong> ${user.isActive ? '✅ Да' : '❌ Нет'}</div>
                </div>
            </div>
        `).join('');
    } catch (error) {
        console.error('Ошибка:', error);
        container.innerHTML = '<div class="error">Ошибка загрузки пользователей</div>';
    }
}

async function loadRoles() {
    const container = document.getElementById('roles-list');
    if (!container) return;
    
    container.innerHTML = '<div class="loading">Загрузка...</div>';
    
    try {
        const response = await fetch(`${API_URL}/Roles`, {
            headers: { 'Authorization': `Bearer ${token}` }
        });
        const roles = await response.json();
        
        if (!roles || !roles.length) {
            container.innerHTML = '<div class="error">Нет ролей</div>';
            return;
        }
        
        container.innerHTML = roles.map(role => `
            <div class="card">
                <div class="card-header">
                    <div class="card-title">${role.role || 'Роль'}</div>
                </div>
                <div class="card-content">
                    <div class="card-field"><strong>ID:</strong> ${role.role_Id}</div>
                </div>
            </div>
        `).join('');
    } catch (error) {
        container.innerHTML = '<div class="error">Ошибка загрузки ролей</div>';
    }
}

async function loadAuthors() {
    const container = document.getElementById('authors-list');
    if (!container) return;
    
    container.innerHTML = '<div class="loading">Загрузка...</div>';
    
    try {
        const response = await fetch(`${API_URL}/Author`, {
            headers: { 'Authorization': `Bearer ${token}` }
        });
        const authors = await response.json();
        
        if (!authors || !authors.length) {
            container.innerHTML = '<div class="error">Нет авторов</div>';
            return;
        }
        
        container.innerHTML = authors.map(author => `
            <div class="card">
                <div class="card-header">
                    <div class="card-title">${author.userName || 'Автор'}</div>
                </div>
                <div class="card-content">
                    <div class="card-field"><strong>Подписчиков:</strong> ${author.subscribers || 0}</div>
                    <div class="card-field"><strong>Материалов:</strong> ${author.materialsCount || 0}</div>
                </div>
            </div>
        `).join('');
    } catch (error) {
        container.innerHTML = '<div class="error">Ошибка загрузки авторов</div>';
    }
}

async function becomeAuthor() {
    if (currentUser.role !== 'Student') {
        alert('Вы уже автор или администратор');
        return;
    }
    
    try {
        const response = await fetch(`${API_URL}/Author/become`, {
            method: 'POST',
            headers: {
                'Authorization': `Bearer ${token}`,
                'Content-Type': 'application/json'
            }
        });
        
        const data = await response.json();
        
        if (response.ok) {
            currentUser.role = 'Author';
            localStorage.setItem('user', JSON.stringify(currentUser));
            alert('Поздравляем! Теперь вы автор!');
            location.reload();
        } else {
            alert(data.message || 'Ошибка');
        }
    } catch (error) {
        alert('Ошибка при регистрации автором');
    }
}

// ========== МАТЕРИАЛЫ ==========
async function loadMaterials() {
    const container = document.getElementById('materials-list');
    if (!container) return;
    
    container.innerHTML = '<div class="loading">Загрузка...</div>';
    
    try {
        const response = await fetch(`${API_URL}/Material`, {
            headers: { 'Authorization': `Bearer ${token}` }
        });
        
        if (!response.ok) throw new Error('Ошибка загрузки');
        
        const materials = await response.json();
        
        if (!materials || !materials.length) {
            container.innerHTML = '<div class="error">Нет материалов</div>';
            return;
        }
        
        container.innerHTML = materials.map(material => `
            <div class="card">
                <div class="card-header">
                    <div class="card-title">${material.material_title}</div>
                    ${currentUser.role === 'Admin' ? `
                        <div class="card-actions">
                            <button class="btn-edit" onclick="editMaterial(${material.material_Id})">✏️</button>
                            <button class="btn-delete" onclick="deleteMaterial(${material.material_Id})">🗑️</button>
                        </div>
                    ` : ''}
                </div>
                <div class="card-content">
                    <div class="card-field"><strong>Дисциплина:</strong> ${material.discipline || '-'}</div>
                    <div class="card-field"><strong>Лайки:</strong> 👍 ${material.likes || 0}</div>
                    <div class="card-field"><strong>Ссылка:</strong> <a href="${material.link_file}" target="_blank">Открыть</a></div>
                    <button onclick="likeMaterial(${material.material_Id})">❤️ Лайк</button>
                    <button onclick="showComments(${material.material_Id})">💬 Комментарии</button>
                </div>
            </div>
        `).join('');
        
    } catch (error) {
        console.error('Ошибка:', error);
        container.innerHTML = '<div class="error">Ошибка загрузки материалов</div>';
    }
}

// Функция удаления материала
async function deleteMaterial(id) {
    if (!confirm('Вы уверены, что хотите удалить этот материал?')) return;
    
    try {
        const response = await fetch(`${API_URL}/Material/${id}`, {
            method: 'DELETE',
            headers: { 'Authorization': `Bearer ${token}` }
        });
        
        const data = await response.json();
        alert(data.message);
        if (response.ok) loadMaterials();
    } catch (error) {
        alert('Ошибка при удалении');
    }
}

// Функция редактирования материала
async function editMaterial(id) {
    // Сначала получим материал
    const response = await fetch(`${API_URL}/Material/${id}`, {
        headers: { 'Authorization': `Bearer ${token}` }
    });
    const material = await response.json();
    
    const modalBody = document.getElementById('modal-body');
    modalBody.innerHTML = `
        <h2>Редактировать материал</h2>
        <form id="editMaterialForm">
            <div class="form-group">
                <label>Название:</label>
                <input type="text" id="material_title" value="${material.material_title}" required>
            </div>
            <div class="form-group">
                <label>Дисциплина:</label>
                <input type="text" id="discipline" value="${material.discipline}" required>
            </div>
            <div class="form-group">
                <label>Ссылка на файл:</label>
                <input type="url" id="link_file" value="${material.link_file}" required>
            </div>
            <button type="submit" class="btn-submit">Сохранить</button>
        </form>
    `;
    
    document.getElementById('editMaterialForm').onsubmit = async (e) => {
        e.preventDefault();
        
        const updatedMaterial = {
            material_title: document.getElementById('material_title').value,
            discipline: document.getElementById('discipline').value,
            link_file: document.getElementById('link_file').value
        };
        
        const updateResponse = await fetch(`${API_URL}/Material/${id}`, {
            method: 'PUT',
            headers: {
                'Authorization': `Bearer ${token}`,
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(updatedMaterial)
        });
        
        const data = await updateResponse.json();
        alert(data.message);
        if (updateResponse.ok) {
            closeModal();
            loadMaterials();
        }
    };
    
    document.getElementById('modal').style.display = 'block';
}

function showTab(tabName) {
    // Скрываем все вкладки
    const tabs = document.querySelectorAll('.tab-content');
    tabs.forEach(tab => tab.classList.remove('active'));
    
    // Показываем выбранную вкладку
    const activeTab = document.getElementById(tabName);
    if (activeTab) activeTab.classList.add('active');
    
    // Обновляем активную кнопку
    const btns = document.querySelectorAll('.tab-btn');
    btns.forEach(btn => btn.classList.remove('active'));
    if (event && event.target) {
        event.target.classList.add('active');
    }
    
    // Загружаем данные для вкладки
    if (tabName === 'materials') {
        loadMaterials();
    } else if (tabName === 'authors') {
        loadAuthors();
    } else if (tabName === 'users') {
        loadUsers();
    } else if (tabName === 'roles') {
        loadRoles();
    } else if (tabName === 'comments') {
        loadComments();
    }
}

// Функция удаления материала
async function deleteMaterial(id) {
    if (!confirm('Вы уверены, что хотите удалить этот материал?')) return;
    
    try {
        const response = await fetch(`${API_URL}/Material/${id}`, {
            method: 'DELETE',
            headers: { 'Authorization': `Bearer ${token}` }
        });
        
        const data = await response.json();
        alert(data.message);
        if (response.ok) loadMaterials();
    } catch (error) {
        alert('Ошибка при удалении');
    }
}

// Функция редактирования материала
async function editMaterial(id) {
    // Сначала получим материал
    const response = await fetch(`${API_URL}/Material/${id}`, {
        headers: { 'Authorization': `Bearer ${token}` }
    });
    const material = await response.json();
    
    const modalBody = document.getElementById('modal-body');
    modalBody.innerHTML = `
        <h2>Редактировать материал</h2>
        <form id="editMaterialForm">
            <div class="form-group">
                <label>Название:</label>
                <input type="text" id="material_title" value="${material.material_title}" required>
            </div>
            <div class="form-group">
                <label>Дисциплина:</label>
                <input type="text" id="discipline" value="${material.discipline}" required>
            </div>
            <div class="form-group">
                <label>Ссылка на файл:</label>
                <input type="url" id="link_file" value="${material.link_file}" required>
            </div>
            <button type="submit" class="btn-submit">Сохранить</button>
        </form>
    `;
    
    document.getElementById('editMaterialForm').onsubmit = async (e) => {
        e.preventDefault();
        
        const updatedMaterial = {
            material_title: document.getElementById('material_title').value,
            discipline: document.getElementById('discipline').value,
            link_file: document.getElementById('link_file').value
        };
        
        const updateResponse = await fetch(`${API_URL}/Material/${id}`, {
            method: 'PUT',
            headers: {
                'Authorization': `Bearer ${token}`,
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(updatedMaterial)
        });
        
        const data = await updateResponse.json();
        alert(data.message);
        if (updateResponse.ok) {
            closeModal();
            loadMaterials();
        }
    };
    
    document.getElementById('modal').style.display = 'block';
}

function showAddMaterialForm() {
    if (currentUser.role !== 'Admin' && currentUser.role !== 'Author') {
        alert('Только авторы и администраторы могут создавать материалы. Станьте автором!');
        return;
    }
    
    const modalBody = document.getElementById('modal-body');
    if (!modalBody) return;
    
    modalBody.innerHTML = `
        <h2>Добавить материал</h2>
        <form id="materialForm">
            <div class="form-group">
                <label>Название:</label>
                <input type="text" id="material_title" required>
            </div>
            <div class="form-group">
                <label>Дисциплина:</label>
                <input type="text" id="discipline" required>
            </div>
            <div class="form-group">
                <label>Ссылка на файл:</label>
                <input type="url" id="link_file" required>
            </div>
            <button type="submit" class="btn-submit">Создать</button>
        </form>
    `;
    
    const form = document.getElementById('materialForm');
    if (form) {
        form.onsubmit = async (e) => {
            e.preventDefault();
            
            const material = {
                material_title: document.getElementById('material_title').value,
                discipline: document.getElementById('discipline').value,
                link_file: document.getElementById('link_file').value
            };
            
            try {
                const response = await fetch(`${API_URL}/Material`, {
                    method: 'POST',
                    headers: {
                        'Authorization': `Bearer ${token}`,
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify(material)
                });
                
                const data = await response.json();
                
                if (response.ok) {
                    closeModal();
                    loadMaterials();
                    
                    if (data.isNewAuthor && data.user) {
                        localStorage.setItem('user', JSON.stringify(data.user));
                        currentUser = data.user;
                        alert('🎉 Поздравляем! Вы стали автором!');
                        location.reload();
                    } else {
                        alert('Материал создан!');
                    }
                } else {
                    alert(data.message || 'Ошибка при создании');
                }
            } catch (error) {
                alert('Ошибка при создании');
            }
        };
    }
    
    const modal = document.getElementById('modal');
    if (modal) modal.style.display = 'block';
}

async function likeMaterial(id) {
    try {
        // Пробуем оба варианта
        let response = await fetch(`${API_URL}/Material/${id}/like`, {
            method: 'PATCH',
            headers: { 'Authorization': `Bearer ${token}` }
        });
        
        // Если не работает, попробуйте POST
        if (!response.ok) {
            response = await fetch(`${API_URL}/Material/${id}/like`, {
                method: 'POST',
                headers: { 'Authorization': `Bearer ${token}` }
            });
        }
        
        if (response.ok) {
            loadMaterials();
        } else {
            console.error('Ошибка лайка:', response.status);
        }
    } catch (error) {
        console.error('Ошибка лайка:', error);
    }
}
// Функция поиска по дисциплине
async function searchByDiscipline() {
    const searchText = document.getElementById('searchInput').value.toLowerCase().trim();
    
    if (!searchText) {
        loadMaterials();  // Если поиск пустой - показываем все материалы
        return;
    }
    
    const container = document.getElementById('materials-list');
    container.innerHTML = '<div class="loading">Поиск...</div>';
    
    try {
        const response = await fetch(`${API_URL}/Material`, {
            headers: { 'Authorization': `Bearer ${token}` }
        });
        
        const allMaterials = await response.json();
        
        // Фильтрация по дисциплине
        const filteredMaterials = allMaterials.filter(material => 
            material.discipline && material.discipline.toLowerCase().includes(searchText)
        );
        
        if (!filteredMaterials.length) {
            container.innerHTML = `<div class="error">По дисциплине "${searchText}" ничего не найдено</div>`;
            return;
        }
        
        // Отображаем отфильтрованные материалы
        container.innerHTML = filteredMaterials.map(material => `
            <div class="card">
                <div class="card-header">
                    <div class="card-title">${material.material_title}</div>
                    ${currentUser.role === 'Admin' ? `
                        <div class="card-actions">
                            <button class="btn-edit" onclick="editMaterial(${material.material_Id})">✏️</button>
                            <button class="btn-delete" onclick="deleteMaterial(${material.material_Id})">🗑️</button>
                        </div>
                    ` : ''}
                </div>
                <div class="card-content">
                    <div class="card-field"><strong>Дисциплина:</strong> ${material.discipline}</div>
                    <div class="card-field"><strong>Лайки:</strong> 👍 ${material.likes || 0}</div>
                    <div class="card-field"><strong>Ссылка:</strong> <a href="${material.link_file}" target="_blank">Открыть</a></div>
                    <button onclick="likeMaterial(${material.material_Id})">❤️ Лайк</button>
                    <button onclick="showComments(${material.material_Id})">💬 Комментарии</button>
                </div>
            </div>
        `).join('');
        
    } catch (error) {
        container.innerHTML = '<div class="error">Ошибка при поиске</div>';
    }
}

// Добавляем поиск по нажатию Enter
document.addEventListener('DOMContentLoaded', () => {
    // ... существующий код ...
    
    const searchInput = document.getElementById('searchInput');
    if (searchInput) {
        searchInput.addEventListener('keypress', (e) => {
            if (e.key === 'Enter') {
                searchByDiscipline();
            }
        });
    }
});
async function searchByDiscipline() {
    const searchText = document.getElementById('searchInput').value.toLowerCase().trim();
    
    if (!searchText) {
        loadMaterials();
        return;
    }
    
    const container = document.getElementById('materials-list');
    container.innerHTML = '<div class="loading">Поиск...</div>';
    
    try {
        // Используем API метод поиска
        const response = await fetch(`${API_URL}/Material/search/${encodeURIComponent(searchText)}`, {
            headers: { 'Authorization': `Bearer ${token}` }
        });
        
        if (response.status === 404) {
            container.innerHTML = `<div class="error">По дисциплине "${searchText}" ничего не найдено</div>`;
            return;
        }
        
        const materials = await response.json();
        
        container.innerHTML = materials.map(material => `
            <div class="card">
                <div class="card-header">
                    <div class="card-title">${material.material_title}</div>
                    ${currentUser.role === 'Admin' ? `
                        <div class="card-actions">
                            <button class="btn-edit" onclick="editMaterial(${material.material_Id})">✏️</button>
                            <button class="btn-delete" onclick="deleteMaterial(${material.material_Id})">🗑️</button>
                        </div>
                    ` : ''}
                </div>
                <div class="card-content">
                    <div class="card-field"><strong>Дисциплина:</strong> ${material.discipline}</div>
                    <div class="card-field"><strong>Лайки:</strong> 👍 ${material.likes || 0}</div>
                    <div class="card-field"><strong>Ссылка:</strong> <a href="${material.link_file}" target="_blank">Открыть</a></div>
                    <button onclick="likeMaterial(${material.material_Id})">❤️ Лайк</button>
                    <button onclick="showComments(${material.material_Id})">💬 Комментарии</button>
                </div>
            </div>
        `).join('');
        
    } catch (error) {
        container.innerHTML = '<div class="error">Ошибка при поиске</div>';
    }
}

async function showComments(materialId) {
    try {
        const response = await fetch(`${API_URL}/Comment/material/${materialId}`, {
            headers: { 'Authorization': `Bearer ${token}` }
        });
        const comments = await response.json();
        
        let commentsHtml = '<h3>Комментарии</h3>';
        
        if (!comments || comments.length === 0) {
            commentsHtml += '<p>Нет комментариев</p>';
        } else {
            comments.forEach(c => {
                commentsHtml += `
                    <div class="card">
                        <div><strong>${c.userName || 'Пользователь'}</strong>: ${c.text}</div>
                        <small>${new Date(c.date).toLocaleString()}</small>
                        ${currentUser.role === 'Admin' ? `
                            <button onclick="deleteComment(${c.comment_Id}, ${materialId})" style="margin-left: 10px; background: #f56565; color: white; border: none; padding: 5px 10px; border-radius: 5px;">🗑️</button>
                        ` : ''}
                    </div>
                `;
            });
        }
        
        commentsHtml += `
            <form id="commentForm">
                <textarea id="commentText" placeholder="Ваш комментарий..." required></textarea>
                <button type="submit">Отправить</button>
            </form>
        `;
        
        const modalBody = document.getElementById('modal-body');
        modalBody.innerHTML = commentsHtml;
        
        document.getElementById('commentForm').onsubmit = async (e) => {
            e.preventDefault();
            const text = document.getElementById('commentText').value;
            
            const commentResponse = await fetch(`${API_URL}/Comment`, {
                method: 'POST',
                headers: {
                    'Authorization': `Bearer ${token}`,
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({ material_id: materialId, text: text })
            });
            
            if (commentResponse.ok) {
                closeModal();
                showComments(materialId);
            } else {
                alert('Ошибка при отправке комментария');
            }
        };
        
        document.getElementById('modal').style.display = 'block';
    } catch (error) {
        alert('Ошибка загрузки комментариев');
    }
}

// Функция удаления комментария
async function deleteComment(commentId, materialId) {
    if (!confirm('Удалить комментарий?')) return;
    
    try {
        const response = await fetch(`${API_URL}/Comment/${commentId}`, {
            method: 'DELETE',
            headers: { 'Authorization': `Bearer ${token}` }
        });
        
        const data = await response.json();
        alert(data.message);
        if (response.ok) showComments(materialId);
    } catch (error) {
        alert('Ошибка при удалении');
    }
}

// ========== КОММЕНТАРИИ ==========
async function loadComments() {
    const container = document.getElementById('comments-list');
    if (container) {
        container.innerHTML = '<div class="loading">Выберите материал, чтобы увидеть комментарии</div>';
    }
}

// ========== ВКЛАДКИ ==========
let currentTab = 'materials';

function showTab(tabName) {
    currentTab = tabName;
    
    const btns = document.querySelectorAll('.tab-btn');
    const contents = document.querySelectorAll('.tab-content');
    
    btns.forEach(btn => btn.classList.remove('active'));
    if (event && event.target) {
        event.target.classList.add('active');
    }
    
    contents.forEach(content => content.classList.remove('active'));
    const activeContent = document.getElementById(tabName);
    if (activeContent) activeContent.classList.add('active');
    
    if (tabName === 'users') loadUsers();
    else if (tabName === 'roles') loadRoles();
    else if (tabName === 'authors') loadAuthors();
    else if (tabName === 'materials') loadMaterials();
    else if (tabName === 'comments') loadComments();
}

// ========== ЗАГРУЗКА ПРИ СТАРТЕ ==========
document.addEventListener('DOMContentLoaded', () => {
    // Обновляем currentUser
    currentUser = JSON.parse(localStorage.getItem('user') || '{}');
    
    // Отображение информации о пользователе
    const userNameEl = document.getElementById('userName');
    const userRoleEl = document.getElementById('userRole');
    
    if (userNameEl && currentUser.user_name) {
        userNameEl.textContent = currentUser.user_name;
    }
    if (userRoleEl && currentUser.role) {
        userRoleEl.textContent = currentUser.role;
    }
    
    // Показываем/скрываем кнопки
    const addMaterialBtn = document.querySelector('#materials .btn-add');
    if (addMaterialBtn) {
        if (currentUser.role === 'Author' || currentUser.role === 'Admin') {
            addMaterialBtn.style.display = 'block';
        } else {
            addMaterialBtn.style.display = 'none';
        }
    }
    
    const becomeAuthorBtn = document.getElementById('becomeAuthorBtn');
    if (becomeAuthorBtn && currentUser.role === 'Student') {
        becomeAuthorBtn.style.display = 'block';
    }
    
    // Скрываем вкладку пользователей для не-админов
    if (currentUser.role !== 'Admin') {
        const usersTabBtn = document.querySelector('.tab-btn');
        if (usersTabBtn && usersTabBtn.textContent.includes('Пользователи')) {
            usersTabBtn.style.display = 'none';
        }
    }
    
    // Загружаем начальные данные
    loadMaterials();
    loadAuthors();
});

// ========== ВЫХОД ==========
function logout() {
    localStorage.removeItem('token');
    localStorage.removeItem('user');
    window.location.href = 'auth.html';
}

// ========== МОДАЛЬНОЕ ОКНО ==========
function closeModal() {
    const modal = document.getElementById('modal');
    if (modal) modal.style.display = 'none';
}

window.onclick = function(event) {
    const modal = document.getElementById('modal');
    if (event.target == modal) {
        closeModal();
    }
}

// ========== ГЛОБАЛЬНЫЕ ФУНКЦИИ ==========
window.showTab = showTab;
window.searchByDiscipline = searchByDiscipline;
window.showAddMaterialForm = showAddMaterialForm;
window.likeMaterial = likeMaterial;
window.showComments = showComments;
window.becomeAuthor = becomeAuthor;
window.closeModal = closeModal;
window.logout = logout;