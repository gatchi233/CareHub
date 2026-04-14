import React, { useEffect, useMemo, useState } from "react";
import { FlatList, Text } from "react-native";
import { useAuth } from "../context/AuthContext";
import {
  aiCareQuery,
  aiDetectTrends,
  aiMedicationExplain,
  aiReportDraft,
  aiShiftHandoff,
  aiShiftSummary,
  aiTrendExplain,
  getResidents
} from "../services/apiClient";
import {
  AppInput,
  Card,
  Chip,
  Hero,
  InfoBanner,
  LoadingBlock,
  PrimaryButton,
  Screen,
  SectionTitle
} from "../ui/components";

export default function AiScreen() {
  const { token, user } = useAuth();
  const [residents, setResidents] = useState([]);
  const [selectedResidentId, setSelectedResidentId] = useState("");
  const [query, setQuery] = useState("");
  const [medicationName, setMedicationName] = useState("");
  const [dosage, setDosage] = useState("");
  const [trendDays, setTrendDays] = useState(7);
  const [responseTitle, setResponseTitle] = useState("AI Response");
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

  async function runRequest(title, task) {
    try {
      setLoading(true);
      setError("");
      setResponseTitle(title);
      const result = await task();
      setResponseText(result?.content || "No AI content returned.");
    } catch (err) {
      setError(err?.message || "AI request failed.");
    } finally {
      setLoading(false);
    }
  }

  if (!canUseAi) {
    return (
      <Screen>
        <Hero
          eyebrow="AI Assistant"
          title="AI tools unavailable"
          subtitle="The mobile AI workspace is currently limited to nurse accounts."
          badge="Restricted"
        />
      </Screen>
    );
  }

  return (
    <Screen>
      <Hero
        eyebrow="AI Assistant"
        title="Clinical AI workspace"
        subtitle="Run resident summaries, explain medications, draft reports, and generate a handoff without leaving the shift workflow."
        badge="Nurse AI tools"
      />

      {error ? <InfoBanner text={error} tone="danger" /> : null}

      <Card>
        <SectionTitle title="Resident Context" subtitle="Most AI actions use the selected resident." />
        <FlatList
          horizontal
          data={residents}
          keyExtractor={(item) => String(item.id || item.Id)}
          showsHorizontalScrollIndicator={false}
          renderItem={({ item }) => {
            const id = String(item.id || item.Id);
            const name = `${item.residentFName || item.ResidentFName || ""} ${item.residentLName || item.ResidentLName || ""}`.trim();
            return <Chip label={name || "Resident"} selected={id === selectedResidentId} onPress={() => setSelectedResidentId(id)} />;
          }}
        />
      </Card>

      <Card>
        <SectionTitle title="Resident Insight Actions" subtitle="Summaries, trends, and report drafts." />
        <PrimaryButton label="Shift Summary" onPress={() => selectedResidentId ? runRequest("Shift Summary", () => aiShiftSummary(selectedResidentId, token)) : setError("Choose a resident first.")} />
        <PrimaryButton label="Detect Trends" onPress={() => selectedResidentId ? runRequest("Detect Trends", () => aiDetectTrends(selectedResidentId, token)) : setError("Choose a resident first.")} tone="secondary" />
        <PrimaryButton label="Report Draft" onPress={() => selectedResidentId ? runRequest("Report Draft", () => aiReportDraft(selectedResidentId, token)) : setError("Choose a resident first.")} tone="secondary" />
        <FlatList
          horizontal
          data={[3, 7]}
          keyExtractor={(item) => String(item)}
          showsHorizontalScrollIndicator={false}
          renderItem={({ item }) => <Chip label={`${item}-Day`} selected={trendDays === item} onPress={() => setTrendDays(item)} />}
        />
        <PrimaryButton label="Trend Explain" onPress={() => selectedResidentId ? runRequest("Trend Explain", () => aiTrendExplain(selectedResidentId, trendDays, token)) : setError("Choose a resident first.")} tone="secondary" />
      </Card>

      <Card>
        <SectionTitle title="Care Query" subtitle="Ask a focused care question using the resident context." />
        <AppInput value={query} onChangeText={setQuery} placeholder="Ask a care question..." multiline />
        <PrimaryButton
          label="Run Care Query"
          onPress={() => query.trim() ? runRequest("Care Query", () => aiCareQuery(query.trim(), selectedResidentId || null, token)) : setError("Enter a care query.")}
        />
      </Card>

      <Card>
        <SectionTitle title="Medication Explain" subtitle="Get a quick plain-language medication explanation." />
        <AppInput value={medicationName} onChangeText={setMedicationName} placeholder="Medication name" />
        <AppInput value={dosage} onChangeText={setDosage} placeholder="Dosage (optional)" />
        <PrimaryButton
          label="Explain Medication"
          onPress={() => medicationName.trim() ? runRequest("Medication Explain", () => aiMedicationExplain(medicationName.trim(), dosage.trim() || null, token)) : setError("Enter a medication name.")}
        />
      </Card>

      <Card>
        <SectionTitle title="Shift Handoff" subtitle="Generate a facility-wide handoff note for the next team." />
        <PrimaryButton label="Generate Shift Handoff" onPress={() => runRequest("Shift Handoff", () => aiShiftHandoff(token))} tone="secondary" />
      </Card>

      <Card style={{ marginBottom: 0 }}>
        <SectionTitle title="AI Responses" subtitle={responseText ? responseTitle : "Run an AI action to review the generated response here."} />
        {loading ? <LoadingBlock label="Generating AI response" /> : null}
        <Text style={{ color: "#6c6257", marginBottom: 10 }}>AI responses are informational and should be reviewed by staff before use.</Text>
        <Text style={{ color: "#1f1911", lineHeight: 22 }}>{responseText || "No response yet."}</Text>
      </Card>
    </Screen>
  );
}
