const API_BASE = localStorage.getItem("northwindApiBase") || "http://localhost:5055";
const PAGE_SIZE = 12;

const state = {
  token: localStorage.getItem("northwindToken"),
  page: 1,
  search: "",
  totalCount: 0,
  hasMore: false,
  editingId: null,
  suppliers: [],
  categories: []
};

const $ = (selector) => document.querySelector(selector);

function setStatus(message = "", type = "") {
  const element = $("#status");
  element.textContent = message;
  element.className = `status ${type}`.trim();
}

function setFormStatus(message = "") {
  $("#formStatus").textContent = message;
}

function setButtonProcessing(button, processing, text) {
  if (!button) return;
  if (processing) {
    button.dataset.originalText = button.textContent;
    button.textContent = text;
    button.disabled = true;
  } else {
    button.textContent = button.dataset.originalText || button.textContent;
    button.disabled = false;
  }
}

function authHeaders(json = false) {
  const headers = {};
  if (state.token) headers.Authorization = `Bearer ${state.token}`;
  if (json) headers["Content-Type"] = "application/json";
  return headers;
}

async function readResponse(response) {
  if (response.status === 204) return null;
  const text = await response.text();
  let data = null;
  try { data = text ? JSON.parse(text) : null; } catch { /* respuesta no JSON */ }
  if (!response.ok) {
    const error = new Error(data?.detail || data?.message || data?.title || `HTTP ${response.status}`);
    error.status = response.status;
    throw error;
  }
  return data;
}

function showAuthenticatedUser() {
  $("#loginPanel").classList.add("hidden");
  $("#appPanel").classList.remove("hidden");
  $("#logoutButton").classList.remove("hidden");
}

function showLogin() {
  $("#loginPanel").classList.remove("hidden");
  $("#appPanel").classList.add("hidden");
  $("#logoutButton").classList.add("hidden");
}

function fillSelect(select, items, valueKey, textKey, placeholder) {
  select.innerHTML = `<option value="">${placeholder}</option>`;
  items.forEach((item) => {
    const option = document.createElement("option");
    option.value = item[valueKey];
    option.textContent = item[textKey];
    select.append(option);
  });
}

// Carga los proveedores y ejecuta el callback cuando la petición termina.
function cargarProveedores(callback) {
  fetch(`${API_BASE}/api/proveedores?limit=100&offset=0`, { headers: authHeaders() })
    .then(readResponse)
    .then((data) => {
      state.suppliers = data || [];
      fillSelect($("#supplierId"), state.suppliers, "supplierId", "companyName", "Seleccione un proveedor...");
      callback(null, state.suppliers);
    })
    .catch((error) => callback(error));
}

// Carga las categorías y ejecuta el callback para encadenar la carga inicial.
function cargarCategorias(proveedorId, callback) {
  fetch(`${API_BASE}/api/categorias?limit=100&offset=0`, { headers: authHeaders() })
    .then(readResponse)
    .then((data) => {
      state.categories = data || [];
      fillSelect($("#categoryId"), state.categories, "categoryId", "categoryName", "Seleccione una categoría...");
      callback(null, state.categories, proveedorId);
    })
    .catch((error) => callback(error));
}

// Consulta un producto por ID para llenar el formulario de edición.
function consultarProductoPorId(id, onSuccess, onError, onComplete) {
  const xhr = new XMLHttpRequest();
  xhr.open("GET", `${API_BASE}/api/productos/${id}`);
  xhr.setRequestHeader("Authorization", `Bearer ${state.token}`);
  xhr.onload = () => {
    if (xhr.status >= 200 && xhr.status < 300) {
      onSuccess(JSON.parse(xhr.responseText));
    } else {
      onError(new Error(`No se pudo consultar el producto (${xhr.status}).`));
    }
    onComplete();
  };
  xhr.onerror = () => {
    onError(new Error("No fue posible conectarse con la API."));
    onComplete();
  };
  xhr.send();
}

// Envuelve POST y PUT en una promesa explícita para consumirla con then/catch/finally.
function guardarProducto(producto, id) {
  return new Promise((resolve, reject) => {
    const xhr = new XMLHttpRequest();
    const method = id ? "PUT" : "POST";
    const url = id ? `${API_BASE}/api/productos/${id}` : `${API_BASE}/api/productos`;
    xhr.open(method, url);
    xhr.setRequestHeader("Authorization", `Bearer ${state.token}`);
    xhr.setRequestHeader("Content-Type", "application/json");
    xhr.onload = () => {
      if (xhr.status >= 200 && xhr.status < 300) {
        resolve(xhr.status === 204 ? null : JSON.parse(xhr.responseText));
      } else {
        reject(new Error(`No se pudo guardar el producto (${xhr.status}).`));
      }
    };
    xhr.onerror = () => reject(new Error("No fue posible conectarse con la API."));
    xhr.send(JSON.stringify(producto));
  });
}


// Elimina un producto usando fetch, try/catch/finally y un botón bloqueado.
async function eliminarProducto(id, button) {
  setButtonProcessing(button, true, "Eliminando...");
  setStatus("Procesando eliminación...");
  try {
    const response = await fetch(`${API_BASE}/api/productos/${id}`, {
      method: "DELETE",
      headers: authHeaders()
    });
    await readResponse(response);
    setStatus("Producto eliminado correctamente.", "success");
    await cargarTabla();
  } catch (error) {
    setStatus(error.message, "error");
  } finally {
    setButtonProcessing(button, false);
  }
}


// Carga la tabla paginada con fetch y actualiza sus metadatos.
async function cargarTabla(button = null) {
  setButtonProcessing(button, true, "Cargando...");
  setStatus("Cargando productos...");
  try {
    const query = new URLSearchParams({ p: state.page, pageSize: PAGE_SIZE });
    if (state.search) query.set("search", state.search);
    const response = await fetch(`${API_BASE}/api/productos/paginados?${query}`, { headers: authHeaders() });
    const page = await readResponse(response);
    state.totalCount = page.totalCount;
    state.hasMore = page.hasMore;
    renderProducts(page.items || []);
    updatePagination();
    setStatus("");
  } catch (error) {
    setStatus(error.message, "error");
  } finally {
    setButtonProcessing(button, false);
  }
}

function renderProducts(products) {
  const body = $("#productsBody");
  body.innerHTML = "";
  if (!products.length) {
    body.innerHTML = '<tr><td class="empty" colspan="7">No se encontraron productos.</td></tr>';
    return;
  }
  products.forEach((product) => {
    const row = document.createElement("tr");
    row.innerHTML = `
      <td>${product.productId}</td>
      <td>${escapeHtml(product.productName)}</td>
      <td>${escapeHtml(product.quantityPerUnit || "—")}</td>
      <td>${product.unitPrice == null ? "—" : Number(product.unitPrice).toFixed(2)}</td>
      <td>${product.unitsInStock ?? "—"}</td>
      <td>${escapeHtml(product.categoryName || "Sin categoría")}</td>
      <td class="actions">
        <button class="button button-secondary edit-button" type="button" data-id="${product.productId}">Editar</button>
        <button class="button button-danger delete-button" type="button" data-id="${product.productId}">Eliminar</button>
      </td>`;
    body.append(row);
  });
}

function escapeHtml(value) {
  return String(value).replace(/[&<>'"]/g, (character) => ({
    "&": "&amp;", "<": "&lt;", ">": "&gt;", "'": "&#39;", '"': "&quot;"
  }[character]));
}

function updatePagination() {
  $("#pageInfo").textContent = `Página ${state.page} · ${state.totalCount} productos`;
  $("#previousButton").disabled = state.page <= 1;
  $("#nextButton").disabled = !state.hasMore;
}

function resetForm() {
  $("#productForm").reset();
  $("#productId").value = "";
  $("#formTitle").textContent = "Nuevo producto";
  state.editingId = null;
  setFormStatus("");
}

function openForm() {
  $("#formCard").classList.remove("hidden");
  $("#productName").focus();
  window.scrollTo({ top: document.body.scrollHeight, behavior: "smooth" });
}

function loadProductIntoForm(product) {
  $("#productId").value = product.productId;
  $("#productName").value = product.productName || "";
  $("#quantityPerUnit").value = product.quantityPerUnit || "";
  $("#unitPrice").value = product.unitPrice ?? "";
  $("#unitsInStock").value = product.unitsInStock ?? "";
  $("#supplierId").value = product.supplierId ?? "";
  $("#categoryId").value = product.categoryId ?? "";
  $("#formTitle").textContent = `Editar producto #${product.productId}`;
  state.editingId = product.productId;
  openForm();
}

function productFormData() {
  const valueOrNull = (selector) => $(selector).value === "" ? null : Number($(selector).value);
  return {
    productName: $("#productName").value.trim(),
    supplierId: valueOrNull("#supplierId"),
    categoryId: valueOrNull("#categoryId"),
    quantityPerUnit: $("#quantityPerUnit").value.trim() || null,
    unitPrice: valueOrNull("#unitPrice"),
    unitsInStock: valueOrNull("#unitsInStock"),
    unitsOnOrder: null,
    reorderLevel: null,
    discontinued: false
  };
}

// === ENFOQUE 4: FETCH API CON ASYNC/AWAIT ===
// Autentica al usuario y configura la sesión antes de cargar el CRUD.
async function iniciarSesion(event) {
  event.preventDefault();
  const button = $("#loginButton");
  setButtonProcessing(button, true, "Ingresando...");
  setStatus("");
  try {
    const response = await fetch(`${API_BASE}/api/auth/login`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ username: $("#username").value.trim(), password: $("#password").value })
    });
    const data = await readResponse(response);
    state.token = data.accessToken;
    localStorage.setItem("northwindToken", state.token);
    $("#sessionUser").textContent = `Sesión: ${data.user.username} · Rol: ${data.user.role}`;
    showAuthenticatedUser();
    cargarProveedores((supplierError) => {
      if (supplierError) return setStatus(supplierError.message, "error");
      cargarCategorias(null, (categoryError) => {
        if (categoryError) return setStatus(categoryError.message, "error");
        cargarTabla();
      });
    });
  } catch (error) {
    setStatus(error.message, "error");
  } finally {
    setButtonProcessing(button, false);
  }
}

// === ENFOQUE 3: PROMESAS (new Promise) ===
// Envía el formulario y consume la promesa de guardado con then/catch/finally.
function guardarDesdeFormulario(event) {
  event.preventDefault();
  const button = $("#saveButton");
  setButtonProcessing(button, true, "Guardando...");
  setFormStatus("Procesando...");
  guardarProducto(productFormData(), state.editingId)
    .then(() => {
      setStatus(state.editingId ? "Producto actualizado correctamente." : "Producto creado correctamente.", "success");
      $("#formCard").classList.add("hidden");
      resetForm();
      return cargarTabla();
    })
    .catch((error) => {
      setStatus(error.message, "error");
      setFormStatus("No se pudo guardar.");
    })
    .finally(() => {
      setButtonProcessing(button, false);
      if (!$("#formCard").classList.contains("hidden")) setFormStatus("");
    });
}

// === ENFOQUE 2: AJAX CON XMLHttpRequest ===
// Bloquea el botón mientras XMLHttpRequest carga los datos de edición.
function editarDesdeTabla(button) {
  setButtonProcessing(button, true, "Cargando...");
  setStatus("Consultando producto...");
  consultarProductoPorId(button.dataset.id, (product) => {
    loadProductIntoForm(product);
    setStatus("");
  }, (error) => setStatus(error.message, "error"), () => setButtonProcessing(button, false));
}

function bindEvents() {
  $("#loginForm").addEventListener("submit", iniciarSesion);
  $("#productForm").addEventListener("submit", guardarDesdeFormulario);
  $("#newButton").addEventListener("click", () => { resetForm(); openForm(); });
  $("#cancelButton").addEventListener("click", () => { resetForm(); $("#formCard").classList.add("hidden"); });
  $("#searchForm").addEventListener("submit", (event) => {
    event.preventDefault();
    state.search = $("#search").value.trim();
    state.page = 1;
    cargarTabla($("#searchButton"));
  });
  $("#previousButton").addEventListener("click", () => { state.page -= 1; cargarTabla($("#previousButton")); });
  $("#nextButton").addEventListener("click", () => { state.page += 1; cargarTabla($("#nextButton")); });
  $("#productsBody").addEventListener("click", (event) => {
    const button = event.target.closest("button");
    if (!button) return;
    if (button.classList.contains("edit-button")) editarDesdeTabla(button);
    if (button.classList.contains("delete-button") && confirm("¿Eliminar este producto?")) eliminarProducto(button.dataset.id, button);
  });
  $("#logoutButton").addEventListener("click", () => {
    state.token = null;
    localStorage.removeItem("northwindToken");
    showLogin();
    setStatus("");
  });
}

bindEvents();
if (state.token) {
  showAuthenticatedUser();
  cargarProveedores((supplierError) => {
    if (supplierError) return setStatus(supplierError.message, "error");
    cargarCategorias(null, (categoryError) => {
      if (categoryError) return setStatus(categoryError.message, "error");
      cargarTabla();
    });
  });
}
