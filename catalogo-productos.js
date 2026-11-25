const API_BASE = localStorage.getItem("northwindApiBase") || "http://localhost:5055";
// La API de este repositorio usa el segmento español definido en ProductsController.
const PRODUCTS_PAGED_ENDPOINT = "/api/productos/paginados";
const pageSize = 12;

let paginaActual = 1;
let filtroActual = "";
let cargando = false;
let hayMasProductos = true;
let productoSeleccionado = null;

const txtBuscar = document.querySelector("#txtBuscar");
const btnBuscar = document.querySelector("#btnBuscar");
const searchIcon = btnBuscar.querySelector("i");
const loading = document.querySelector("#loading");
const ready = document.querySelector("#ready");
const productos = document.querySelector("#productos");
const sentinel = document.querySelector("#sentinel");
const detailsDialog = document.querySelector("#detailsDialog");

function authHeaders() {
  const token = localStorage.getItem("northwindToken");
  return token ? { Authorization: `Bearer ${token}` } : {};
}

async function readResponse(response) {
  if (response.status === 204) return null;
  const text = await response.text();
  let data = null;
  try { data = text ? JSON.parse(text) : null; } catch { /* respuesta no JSON */ }
  if (!response.ok) {
    throw new Error(data?.detail || data?.message || data?.title || `Error HTTP ${response.status}`);
  }
  return data;
}

function cambiarEstado(mensaje, tipo = "ready") {
  ready.textContent = mensaje;
  ready.className = `status-message ready-message${tipo === "warning" ? " warning" : tipo === "error" ? " error" : ""}`;
}

function cambiarEstadoCarga(estaCargando) {
  loading.classList.toggle("hidden", !estaCargando);
  btnBuscar.disabled = estaCargando;
  searchIcon.classList.toggle("fa-magnifying-glass", !estaCargando);
  searchIcon.classList.toggle("fa-spinner", estaCargando);
  searchIcon.classList.toggle("fa-spin", estaCargando);
}

function escapeHtml(value) {
  return String(value ?? "").replace(/[&<>'"]/g, (character) => ({
    "&": "&amp;", "<": "&lt;", ">": "&gt;", "'": "&#39;", '"': "&quot;"
  }[character]));
}

function formatPrice(price) {
  if (price == null) return "Precio no disponible";
  return new Intl.NumberFormat("es-MX", { style: "currency", currency: "USD" }).format(Number(price));
}

function renderProducts(items) {
  items.forEach((product) => {
    const card = document.createElement("article");
    card.className = "product-card";
    card.innerHTML = `
      <p class="product-category">${escapeHtml(product.categoryName || "Sin categoría")}</p>
      <h2>${escapeHtml(product.productName)}</h2>
      <p class="product-quantity">${escapeHtml(product.quantityPerUnit || "Presentación no disponible")}</p>
      <div class="product-footer">
        <strong class="product-price">${formatPrice(product.unitPrice)}</strong>
        <button class="details-button" type="button" data-product-id="${product.productId}">
          <i class="fa-solid fa-circle-info" aria-hidden="true"></i> Ver detalles
        </button>
      </div>`;
    card.querySelector(".details-button").addEventListener("click", () => mostrarDetalles(product));
    productos.append(card);
  });
}

function mostrarDetalles(product) {
  productoSeleccionado = product;
  document.querySelector("#detailsTitle").textContent = product.productName;
  document.querySelector("#detailsContent").innerHTML = `
    <dl class="detail-list">
      <div><dt>ID</dt><dd>${escapeHtml(product.productId)}</dd></div>
      <div><dt>Categoría</dt><dd>${escapeHtml(product.categoryName || "Sin categoría")}</dd></div>
      <div><dt>Presentación</dt><dd>${escapeHtml(product.quantityPerUnit || "No disponible")}</dd></div>
      <div><dt>Precio</dt><dd>${formatPrice(product.unitPrice)}</dd></div>
      <div><dt>Unidades en stock</dt><dd>${escapeHtml(product.unitsInStock ?? "No disponible")}</dd></div>
    </dl>`;
  if (typeof detailsDialog.showModal === "function") detailsDialog.showModal();
  else detailsDialog.setAttribute("open", "");
}

async function cargarProductos() {
  if (cargando || !hayMasProductos) return;

  cargando = true;
  cambiarEstadoCarga(true);

  try {
    const query = new URLSearchParams({ p: paginaActual, pageSize });
    if (filtroActual) query.set("search", filtroActual);

    const response = await fetch(`${API_BASE}${PRODUCTS_PAGED_ENDPOINT}?${query}`, {
      headers: authHeaders()
    });
    const resultado = await readResponse(response);
    const items = resultado?.items || [];

    renderProducts(items);
    hayMasProductos = items.length === pageSize && resultado?.hasMore !== false;
    cambiarEstado(hayMasProductos ? "Listo" : "No hay más productos para mostrar", hayMasProductos ? "ready" : "warning");
  } catch (error) {
    paginaActual = Math.max(1, paginaActual - 1);
    cambiarEstado("Error al cargar los datos", "error");
    console.error("Error cargando productos:", error);
  } finally {
    cargando = false;
    cambiarEstadoCarga(false);
  }
}

function buscarProductos() {
  if (cargando) return;
  filtroActual = txtBuscar.value.trim();
  paginaActual = 1;
  hayMasProductos = true;
  productos.innerHTML = "";
  cargarProductos();
}

const observer = new IntersectionObserver((entries) => {
  if (!entries[0].isIntersecting || cargando || !hayMasProductos) return;
  paginaActual++;
  cargarProductos();
}, { rootMargin: "150px 0px" });

btnBuscar.addEventListener("click", buscarProductos);
txtBuscar.addEventListener("keydown", (event) => {
  if (event.key === "Enter") {
    event.preventDefault();
    buscarProductos();
  }
});
document.querySelector("#closeDetails").addEventListener("click", () => detailsDialog.close());
detailsDialog.addEventListener("click", (event) => {
  if (event.target === detailsDialog) detailsDialog.close();
});
observer.observe(sentinel);
cargarProductos();
