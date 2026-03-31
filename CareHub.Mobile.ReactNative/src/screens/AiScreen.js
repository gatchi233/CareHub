import React, { useEffect, useMemo, useState } from "react";
import {
  ActivityIndicator,
  FlatList,
  SafeAreaView,
  Text,
  TextInput,
  TouchableOpacity,
  View
} from "react-native";
import { useAuth } from "../context/AuthContext";
import {
  aiCareQuery,
  aiDetectTrends,
  aiMedicationExplain,
  aiReportDraft,
  aiShiftSummary,
  aiShiftHandoff,
  aiTrendExplain,
  getResidents
} from "../services/apiClient";

export default function AiScreen() {
  const { token, user } = useAuth();
  const [residents, setResidents] = useState([]);
  const [selectedResidentId, setSelectedResidentId] = useState("");
  const [query, setQuery] = useState("");
  const [medicationName, setMedicationName] = useState("");
  const [dosage, setDosage] = useState("");
  const [trendDays, setTrendDays] = useState(7);
  const [responseText, setResponseText] = useState("");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");

  const canUseAi = useMemo(() => user?.role === "Nurse", [user]);

  useEffect(() => {
    async function loadResidents() {
      if (!canUseAi) return;
      try {
        const data = await getResidents(token);
        const list = Array.isArray(data) ? data : [];
        setResidents(list);
        if (list.length > 0) {
          setSelectedResidentId(String(list[0].id || list[0].Id));
        }
      } catch (err) {
        setError(err?.message || "Failed to load residents.");
      }
    }
    loadResidents();
  }, [canUseAi, token]);

  async function runShiftSummary() {
    if (!selectedResidentId) {
      setError("Choose a resident first.");
      return;
    }
    setLoading(true);
    setError("");
    try {
      const result = await aiShiftSummary(selectedResidentId, token);
      setResponseText(result?.content || "No AI content returned.");
    } catch (err) {
      setError(err?.message || "AI shift summary failed.");
    } finally {
      setLoading(false);
    }
  }

  async function runTrendDetect() {
    if (!selectedResidentId) {
      setError("Choose a resident first.");
      return;
    }
    setLoading(true);
    setError("");
    try {
      const result = await aiDetectTrends(selectedResidentId, token);
      setResponseText(result?.content || "No AI content returned.");
    } catch (err) {
      setError(err?.message || "AI trend detection failed.");
    } finally {
      setLoading(false);
    }
  }

  async function runCareQuery() {
    if (!query.trim()) {
      setError("Enter a care query.");
      return;
    }
    setLoading(true);
    setError("");
    try {
      const result = await aiCareQuery(query.trim(), selectedResidentId || null, token);
      setResponseText(result?.content || "No AI content returned.");
    } catch (err) {
      setError(err?.message || "AI care query failed.");
    } finally {
      setLoading(false);
    }
  }

  async function runTrendExplain() {
    if (!selectedResidentId) {
      setError("Choose a resident first.");
      return;
    }
    setLoading(true);
    setError("");
    try {
      const result = await aiTrendExplain(selectedResidentId, trendDays, token);
      setResponseText(result?.content || "No AI content returned.");
    } catch (err) {
      setError(err?.message || "AI trend explain failed.");
    } finally {
      setLoading(false);
    }
  }

  async function runMedicationExplain() {
    if (!medicationName.trim()) {
      setError("Enter a medication name.");
      return;
    }
    setLoading(true);
    setError("");
    try {
      const result = await aiMedicationExplain(medicationName.trim(), dosage.trim() || null, token);
      setResponseText(result?.content || "No AI content returned.");
    } catch (err) {
      setError(err?.message || "AI medication explain failed.");
    } finally {
      setLoading(false);
    }
  }

  async function runReportDraft() {
    if (!selectedResidentId) {
      setError("Choose a resident first.");
      return;
    }
    setLoading(true);
    setError("");
    try {
      const result = await aiReportDraft(selectedResidentId, token);
      setResponseText(result?.content || "No AI content returned.");
    } catch (err) {
      setError(err?.message || "AI report draft failed.");
    } finally {
      setLoading(false);
    }
  }

  async function runShiftHandoff() {
    setLoading(true);
    setError("");
    try {
      const result = await aiShiftHandoff(token);
      setResponseText(result?.content || "No AI content returned.");
    } catch (err) {
      setError(err?.message || "AI shift handoff failed.");
    } finally {
      setLoading(false);
    }
  }

  if (!canUseAi) {
    return (
      <SafeAreaView style={{ flex: 1, padding: 16 }}>
        <Text style={{ fontSize: 20, marginBottom: 8 }}>AI Assistant</Text>
        <Text>AI tools are currently available on mobile for Nurse role only.</Text>
      </SafeAreaView>
    );
  }

  return (
    <SafeAreaView style={{ flex: 1, padding: 16 }}>
      <Text style={{ fontSize: 20, marginBottom: 8 }}>AI Assistant</Text>
      <Text style={{ marginBottom: 8 }}>Run quick resident summaries and care queries.</Text>

      <Text style={{ marginBottom: 4 }}>Resident Context</Text>
      <FlatList
        horizontal
        data={residents}
        keyExtractor={(item) => String(item.id || item.Id)}
        renderItem={({ item }) => {
          const id = String(item.id || item.Id);
          const selected = id === selectedResidentId;
          const name = `${item.residentFName || item.ResidentFName || ""} ${item.residentLName || item.ResidentLName || ""}`.trim();
          return (
            <TouchableOpacity
              onPress={() => setSelectedResidentId(id)}
              style={{
                marginRight: 8,
                marginBottom: 8,
                paddingHorizontal: 10,
                paddingVertical: 8,
                borderWidth: 1,
                borderColor: selected ? "#2a7" : "#ccc",
                backgroundColor: selected ? "#e7fff4" : "#fff",
                borderRadius: 8
              }}
            >
              <Text>{name || "Resident"}</Text>
            </TouchableOpacity>
          );
        }}
      />

      <View style={{ flexDirection: "row", marginBottom: 8 }}>
        <TouchableOpacity onPress={runShiftSummary} style={{ marginRight: 12 }}>
          <Text style={{ color: "#2a7" }}>Shift Summary</Text>
        </TouchableOpacity>
        <TouchableOpacity onPress={runTrendDetect}>
          <Text style={{ color: "#2a7" }}>Detect Trends</Text>
        </TouchableOpacity>
      </View>
      <View style={{ flexDirection: "row", marginBottom: 8 }}>
        <TouchableOpacity onPress={runReportDraft} style={{ marginRight: 12 }}>
          <Text style={{ color: "#2a7" }}>Report Draft</Text>
        </TouchableOpacity>
        <TouchableOpacity onPress={runShiftHandoff}>
          <Text style={{ color: "#2a7" }}>Shift Handoff</Text>
        </TouchableOpacity>
      </View>
      <View style={{ flexDirection: "row", marginBottom: 8 }}>
        <TouchableOpacity onPress={() => setTrendDays(3)} style={{ marginRight: 12 }}>
          <Text style={{ color: trendDays === 3 ? "#2a7" : "#666" }}>3-Day</Text>
        </TouchableOpacity>
        <TouchableOpacity onPress={() => setTrendDays(7)} style={{ marginRight: 12 }}>
          <Text style={{ color: trendDays === 7 ? "#2a7" : "#666" }}>7-Day</Text>
        </TouchableOpacity>
        <TouchableOpacity onPress={runTrendExplain}>
          <Text style={{ color: "#2a7" }}>Trend Explain</Text>
        </TouchableOpacity>
      </View>

      <TextInput
        value={query}
        onChangeText={setQuery}
        placeholder="Ask a care question..."
        style={{ borderWidth: 1, borderColor: "#ccc", marginBottom: 8, padding: 10, borderRadius: 6 }}
      />
      <TouchableOpacity
        onPress={runCareQuery}
        style={{ backgroundColor: "#2a7", paddingVertical: 10, borderRadius: 6, alignItems: "center", marginBottom: 10 }}
      >
        <Text style={{ color: "white", fontWeight: "600" }}>Run Care Query</Text>
      </TouchableOpacity>

      <TextInput
        value={medicationName}
        onChangeText={setMedicationName}
        placeholder="Medication name for explain..."
        style={{ borderWidth: 1, borderColor: "#ccc", marginBottom: 8, padding: 10, borderRadius: 6 }}
      />
      <TextInput
        value={dosage}
        onChangeText={setDosage}
        placeholder="Dosage (optional)"
        style={{ borderWidth: 1, borderColor: "#ccc", marginBottom: 8, padding: 10, borderRadius: 6 }}
      />
      <TouchableOpacity
        onPress={runMedicationExplain}
        style={{ backgroundColor: "#2a7", paddingVertical: 10, borderRadius: 6, alignItems: "center", marginBottom: 10 }}
      >
        <Text style={{ color: "white", fontWeight: "600" }}>Explain Medication</Text>
      </TouchableOpacity>

      {loading ? <ActivityIndicator style={{ marginBottom: 8 }} /> : null}
      {error ? <Text style={{ color: "red", marginBottom: 8 }}>{error}</Text> : null}

      <View style={{ borderWidth: 1, borderColor: "#ddd", borderRadius: 8, padding: 10 }}>
        <Text style={{ fontWeight: "600", marginBottom: 6 }}>AI Response</Text>
        <Text>{responseText || "No response yet."}</Text>
      </View>
    </SafeAreaView>
  );
}
