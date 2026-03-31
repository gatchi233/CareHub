import { Platform } from "react-native";

const API_BASE_BY_PLATFORM = {
  android: "http://10.0.2.2:5007/api",
  ios: "http://localhost:5007/api",
  default: "http://localhost:5007/api"
};

let apiBaseUrl =
  (typeof process !== "undefined" && process?.env?.CAREHUB_API_BASE_URL) ||
  API_BASE_BY_PLATFORM[Platform.OS] ||
  API_BASE_BY_PLATFORM.default;

export function setApiBaseUrl(url) {
  const trimmed = (url || "").trim();
  if (trimmed) {
    apiBaseUrl = trimmed.replace(/\/+$/, "");
  }
}

export function getApiBaseUrl() {
  return apiBaseUrl;
}

export async function apiRequest(path, options = {}, token = "") {
  const response = await fetch(`${apiBaseUrl}${path}`, {
    headers: {
      "Content-Type": "application/json",
      ...(token ? { Authorization: `Bearer ${token}` } : {})
    },
    ...options
  });

  if (!response.ok) {
    const text = await response.text();
    try {
      const parsed = JSON.parse(text);
      const message = parsed?.message || parsed?.error || parsed?.detail || text;
      throw new Error(message || `${response.status} ${response.statusText}`);
    } catch {
      throw new Error(text || `${response.status} ${response.statusText}`);
    }
  }

  if (response.status === 204) {
    return null;
  }

  return response.json();
}

export async function login(username, password) {
  return apiRequest("/auth/login", {
    method: "POST",
    body: JSON.stringify({ username, password })
  });
}

export async function me(token) {
  return apiRequest("/auth/me", { method: "GET" }, token);
}

export async function getResidents(token) {
  return apiRequest("/residents", { method: "GET" }, token);
}

export async function createResident(resident, token) {
  return apiRequest(
    "/residents",
    {
      method: "POST",
      body: JSON.stringify(resident)
    },
    token
  );
}

export async function updateResident(residentId, resident, token) {
  return apiRequest(
    `/residents/${residentId}`,
    {
      method: "PUT",
      body: JSON.stringify(resident)
    },
    token
  );
}

export async function deleteResident(residentId, token) {
  return apiRequest(
    `/residents/${residentId}`,
    {
      method: "DELETE"
    },
    token
  );
}

export async function getObservations(token) {
  return apiRequest("/observations", { method: "GET" }, token);
}

export async function getObservationsByResident(residentId, token) {
  return apiRequest(`/observations/by-resident/${residentId}`, { method: "GET" }, token);
}

export async function createObservation(observation, token) {
  return apiRequest(
    "/observations",
    {
      method: "POST",
      body: JSON.stringify(observation)
    },
    token
  );
}

export async function updateObservation(observationId, observation, token) {
  return apiRequest(
    `/observations/${observationId}`,
    {
      method: "PUT",
      body: JSON.stringify(observation)
    },
    token
  );
}

export async function deleteObservation(observationId, token) {
  return apiRequest(
    `/observations/${observationId}`,
    {
      method: "DELETE"
    },
    token
  );
}

export async function getMedications(token) {
  return apiRequest("/medications", { method: "GET" }, token);
}

export async function getLowStockMedications(token) {
  return apiRequest("/medications/lowstock", { method: "GET" }, token);
}

export async function createMedication(medication, token) {
  return apiRequest(
    "/medications",
    {
      method: "POST",
      body: JSON.stringify(medication)
    },
    token
  );
}

export async function updateMedication(medicationId, medication, token) {
  return apiRequest(
    `/medications/${medicationId}`,
    {
      method: "PUT",
      body: JSON.stringify(medication)
    },
    token
  );
}

export async function deleteMedication(medicationId, token) {
  return apiRequest(
    `/medications/${medicationId}`,
    {
      method: "DELETE"
    },
    token
  );
}

export async function adjustMedicationStock(medicationId, delta, token) {
  return apiRequest(
    `/medications/${medicationId}/adjustStock?delta=${delta}`,
    {
      method: "POST"
    },
    token
  );
}

export async function getMarEntries(token, query = {}) {
  const params = new URLSearchParams();
  if (query.residentId) params.set("residentId", query.residentId);
  if (query.fromUtc) params.set("fromUtc", query.fromUtc);
  if (query.toUtc) params.set("toUtc", query.toUtc);
  if (query.includeVoided) params.set("includeVoided", "true");
  const qs = params.toString();
  return apiRequest(`/mar${qs ? `?${qs}` : ""}`, { method: "GET" }, token);
}

export async function getMarReport(token, query) {
  const params = new URLSearchParams();
  params.set("fromUtc", query.fromUtc);
  params.set("toUtc", query.toUtc);
  if (query.residentId) params.set("residentId", query.residentId);
  return apiRequest(`/mar/report?${params.toString()}`, { method: "GET" }, token);
}

export async function createMarEntry(entry, token) {
  return apiRequest(
    "/mar",
    {
      method: "POST",
      body: JSON.stringify(entry)
    },
    token
  );
}

export async function voidMarEntry(marEntryId, reason, token) {
  return apiRequest(
    `/mar/${marEntryId}/void`,
    {
      method: "POST",
      body: JSON.stringify({ reason: reason || "Voided on mobile app" })
    },
    token
  );
}

export async function getMedicationOrders(token) {
  return apiRequest("/medicationorders", { method: "GET" }, token);
}

export async function createMedicationOrder(order, token) {
  return apiRequest(
    "/medicationorders",
    {
      method: "POST",
      body: JSON.stringify(order)
    },
    token
  );
}

export async function updateMedicationOrderStatus(orderId, statusPayload, token) {
  return apiRequest(
    `/medicationorders/${orderId}/status`,
    {
      method: "PUT",
      body: JSON.stringify(statusPayload)
    },
    token
  );
}

export async function aiShiftSummary(residentId, token) {
  return apiRequest(
    "/ai/shift-summary",
    {
      method: "POST",
      body: JSON.stringify({ residentId })
    },
    token
  );
}

export async function aiMedicationExplain(medicationName, dosage, token) {
  return apiRequest(
    "/ai/medication-explain",
    {
      method: "POST",
      body: JSON.stringify({ medicationName, dosage })
    },
    token
  );
}

export async function aiDetectTrends(residentId, token) {
  return apiRequest(
    "/ai/detect-trends",
    {
      method: "POST",
      body: JSON.stringify({ residentId })
    },
    token
  );
}

export async function aiReportDraft(residentId, token) {
  return apiRequest(
    "/ai/report-draft",
    {
      method: "POST",
      body: JSON.stringify({ residentId })
    },
    token
  );
}

export async function aiShiftHandoff(token) {
  return apiRequest(
    "/ai/shift-handoff",
    {
      method: "POST",
      body: JSON.stringify({})
    },
    token
  );
}

export async function aiCareQuery(query, residentId, token) {
  return apiRequest(
    "/ai/care-query",
    {
      method: "POST",
      body: JSON.stringify({ query, residentId })
    },
    token
  );
}

export async function aiTrendExplain(residentId, days, token) {
  return apiRequest(
    "/ai/trend-explain",
    {
      method: "POST",
      body: JSON.stringify({ residentId, days })
    },
    token
  );
}
