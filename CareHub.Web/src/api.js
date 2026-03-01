export const API_BASE = import.meta.env.VITE_API_BASE || "http://localhost:5001/api";
const TOKEN_KEY = "carehub_token";

let authToken = "";
if (typeof window !== "undefined") {
  authToken = window.localStorage.getItem(TOKEN_KEY) || "";
}

export function setAuthToken(token) {
  authToken = token || "";
  if (typeof window !== "undefined") {
    if (authToken) {
      window.localStorage.setItem(TOKEN_KEY, authToken);
    } else {
      window.localStorage.removeItem(TOKEN_KEY);
    }
  }
}

export function getAuthToken() {
  return authToken;
}

async function request(path, options = {}) {
  const response = await fetch(`${API_BASE}${path}`, {
    headers: {
      "Content-Type": "application/json",
      ...(authToken ? { Authorization: `Bearer ${authToken}` } : {}),
      ...(options.headers || {})
    },
    ...options
  });

  if (!response.ok) {
    const message = `${response.status} ${response.statusText}`;
    const error = new Error(message);
    error.status = response.status;
    throw error;
  }

  if (response.status === 204) {
    return null;
  }

  return response.json();
}

export const api = {
  get(path) {
    return request(path);
  },
  post(path, body) {
    return request(path, { method: "POST", body: JSON.stringify(body) });
  },
  put(path, body) {
    return request(path, { method: "PUT", body: JSON.stringify(body) });
  },
  del(path) {
    return request(path, { method: "DELETE" });
  },
  postNoBody(path) {
    return request(path, { method: "POST" });
  }
};
