const configured = (import.meta.env.VITE_API_URL || "").trim().replace(/\/$/, "");
const host = window.location.hostname || "localhost";
const protocol = window.location.protocol === "https:" ? "https:" : "http:";
const bases = [
  configured,
  `${protocol}//${host}:5000/api`,
  "http://localhost:5000/api",
  "http://127.0.0.1:5000/api"
].filter(Boolean).filter((x, i, a) => a.indexOf(x) === i);

let activeBase = bases[0];

async function request(base, path, options = {}) {
  const token = localStorage.getItem("token");
  const headers = {
    Accept: "application/json",
    "Content-Type": "application/json",
    ...(options.headers || {})
  };
  if (token) headers.Authorization = `Bearer ${token}`;

  const response = await fetch(`${base}${path}`, { ...options, headers });
  const text = await response.text();
  let data = {};
  try { data = text ? JSON.parse(text) : {}; }
  catch { data = { message: text }; }

  if (!response.ok) {
    const message = data?.detail ? `${data?.message || 'Server error'}: ${data.detail}` : (data?.message || data?.title || `Request failed (${response.status})`);
    const error = new Error(message);
    error.status = response.status;
    throw error;
  }
  return data;
}

async function rawReq(path, options = {}, allowRefresh = true) {
  const order = [activeBase, ...bases.filter(x => x !== activeBase)];
  let lastError;
  for (const base of order) {
    try {
      const result = await request(base, path, options);
      activeBase = base;
      return result;
    } catch (error) {
      lastError = error;
      if (error?.status === 401 && allowRefresh && path !== "/auth/refresh" && path !== "/auth/login") {
        const refreshToken = localStorage.getItem("refreshToken");
        if (refreshToken) {
          try {
            const refreshed = await request(base, "/auth/refresh", { method: "POST", body: JSON.stringify({ refreshToken }) });
            localStorage.setItem("token", refreshed.token);
            localStorage.setItem("refreshToken", refreshed.refreshToken);
            localStorage.setItem("user", JSON.stringify(refreshed.user));
            return await request(base, path, options);
          } catch { /* fall through to the original auth error */ }
        }
      }
      if (!(error instanceof TypeError)) throw error;
    }
  }
  throw new Error(`Cannot connect to the backend. Make sure ASP.NET Core is running on http://localhost:5000. ${lastError?.message || ""}`.trim());
}

async function req(path, options = {}) {
  try { return await rawReq(path, options, true); }
  catch (error) { throw error; }
}

export const getApiBase = () => activeBase;
export const health = () => req("/health");
export const login = (email, password) => req("/auth/login", { method: "POST", body: JSON.stringify({ email, password }) });
export const register = (name, email, password) => req("/auth/register", { method: "POST", body: JSON.stringify({ name, email, password }) });
export const logout = refreshToken => req("/auth/logout", { method: "POST", body: JSON.stringify({ refreshToken }) });
export const refresh = refreshToken => req("/auth/refresh", { method: "POST", body: JSON.stringify({ refreshToken }) });
export const getProducts = (search = "", category = "all") => req(`/products?search=${encodeURIComponent(search)}&category=${encodeURIComponent(category)}`);
export const getProduct = id => req(`/products/${id}`);
export const getCategories = () => req("/products/categories");
export const createOrder = payload => req("/orders", { method: "POST", body: JSON.stringify(payload) });
export const getMyOrders = () => req("/orders/mine");
export const getAllOrders = () => req("/orders");
export const updateOrderStatus = (id, status) => req(`/orders/${id}/status`, { method: "PATCH", body: JSON.stringify(status) });
export const createProduct = payload => req("/products", { method: "POST", body: JSON.stringify(payload) });
export const deleteProduct = id => req(`/products/${id}`, { method: "DELETE" });

export const updateProduct = (id, payload) => req(`/products/${id}`, { method: "PUT", body: JSON.stringify(payload) });
export const getAdminStats = () => req("/admin/stats");
export const getAdminUsers = () => req("/admin/users");
export const getProfile = () => req("/profile");
export const updateProfile = name => req("/profile", { method: "PUT", body: JSON.stringify({ name }) });
export const getWishlist = () => req("/wishlist");
export const addWishlist = id => req(`/wishlist/${id}`, { method: "POST" });
export const removeWishlist = id => req(`/wishlist/${id}`, { method: "DELETE" });
export const getReviews = id => req(`/reviews/product/${id}`);
export const saveReview = (id, rating, comment) => req(`/reviews/product/${id}`, { method: "POST", body: JSON.stringify({ rating, comment }) });
export const forgotPassword = email => req("/auth/forgot-password", { method: "POST", body: JSON.stringify({ email }) });
