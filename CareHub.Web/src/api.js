export const API_BASE = import.meta.env.VITE_API_BASE || "http://localhost:5001/api";

async function request(path, options = {}) {
  const response = await fetch(`${API_BASE}${path}`, {
    headers: {
      "Content-Type": "application/json",
      ...(options.headers || {})
    },
    ...options
  });

  if (!response.ok) {
    throw new Error(`${response.status} ${response.statusText}`);
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

