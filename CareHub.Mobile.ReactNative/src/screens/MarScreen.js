import React, { useCallback, useEffect, useMemo, useState } from "react";
import { FlatList, RefreshControl, Text, TouchableOpacity, View } from "react-native";
import { useAuth } from "../context/AuthContext";
import {
  createMarEntry,
  getMarEntries,
  getMarReport,
  getMedications,
  getResidents,
  voidMarEntry
} from "../services/apiClient";
import {
  AppInput,
  Card,
  Chip,
  FormLabel,
  Hero,
  InfoBanner,
  ListRow,
  LoadingBlock,
  PrimaryButton,
  Screen,
  SectionTitle
} from "../ui/components";
import { colors, spacing } from "../ui/theme";

const MAR_STATUSES = ["Given", "Refused", "Held", "Missed", "NotAvailable"];

function toResidentName(resident) {
  const first = resident.residentFName || resident.ResidentFName || "";
  const last = resident.residentLName || resident.ResidentLName || "";
  return `${first} ${last}`.trim() || "Unknown resident";
}

function generateGuid() {
  const block = () => Math.floor((1 + Math.random()) * 0x10000).toString(16).slice(1);
  return `${block()}${block()}-${block()}-4${block().slice(1)}-${((8 + Math.random() * 4) | 0).toString(16)}${block().slice(1)}-${block()}${block()}${block()}`;
}

export default function MarScreen() {
  const { token, user } = useAuth();
  const [entries, setEntries] = useState([]);
  const [residents, setResidents] = useState([]);
  const [medications, setMedications] = useState([]);
  const [selectedResidentId, setSelectedResidentId] = useState("");
  const [selectedMedicationId, setSelectedMedicationId] = useState("");
  const [selectedStatus, setSelectedStatus] = useState("Given");
  const [doseQuantity, setDoseQuantity] = useState("1");
  const [doseUnit, setDoseUnit] = useState("tablet");
  const [notes, setNotes] = useState("");
  const [loading, setLoading] = useState(false);
  const [refreshing, setRefreshing] = useState(false);
  const [saving, setSaving] = useState(false);
  const [voidingId, setVoidingId] = useState("");
  const [query, setQuery] = useState("");
  const [includeVoided, setIncludeVoided] = useState(false);
  const [reportLoading, setReportLoading] = useState(false);
  const [report, setReport] = useState(null);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");

  const residentById = useMemo(() => {
    const map = new Map();
    residents.forEach((resident) => map.set(String(resident.id || resident.Id), toResidentName(resident)));
    return map;
  }, [residents]);

  const filteredMeds = useMemo(() => {
    if (!selectedResidentId) return [];
    return medications.filter((med) => String(med.residentId || med.ResidentId || "") === selectedResidentId);
  }, [medications, selectedResidentId]);

  const loadData = useCallback(async () => {
    try {
      setLoading(true);
      setError("");
      const fromUtc = new Date(Date.now() - 24 * 60 * 60 * 1000).toISOString();
      const toUtc = new Date().toISOString();
      const [marData, residentData, medicationData] = await Promise.all([
        getMarEntries(token, { fromUtc, toUtc, includeVoided }),
        getResidents(token),
        getMedications(token)
      ]);

      const residentList = Array.isArray(residentData) ? residentData : [];
      const medList = Array.isArray(medicationData) ? medicationData : [];
      setEntries(Array.isArray(marData) ? marData : []);
      setResidents(residentList);
      setMedications(medList);
      if (!selectedResidentId && residentList.length > 0) {
        setSelectedResidentId(String(residentList[0].id || residentList[0].Id || ""));
      }
    } catch (err) {
      setError(err?.message || "Failed to load MAR data.");
    } finally {
      setLoading(false);
    }
  }, [includeVoided, selectedResidentId, token]);

  useEffect(() => {
    loadData();
  }, [loadData]);

  useEffect(() => {
    if (!selectedResidentId || filteredMeds.length === 0) {
      setSelectedMedicationId("");
      return;
    }
    const stillSelected = filteredMeds.some((med) => String(med.id || med.Id) === selectedMedicationId);
    if (!stillSelected) {
      setSelectedMedicationId(String(filteredMeds[0].id || filteredMeds[0].Id || ""));
    }
  }, [filteredMeds, selectedMedicationId, selectedResidentId]);

  const filteredEntries = useMemo(() => {
    const term = query.trim().toLowerCase();
    if (!term) return entries;
    return entries.filter((item) => {
      const residentId = String(item.residentId || item.ResidentId || "");
      const residentName = String(residentById.get(residentId) || "").toLowerCase();
      const status = String(item.status || item.Status || "").toLowerCase();
      const medName = String(
        medications.find((med) => String(med.id || med.Id) === String(item.medicationId || item.MedicationId))?.medName || ""
      ).toLowerCase();
      return residentName.includes(term) || status.includes(term) || medName.includes(term);
    });
  }, [entries, medications, query, residentById]);

  async function onCreate() {
    setError("");
    setSuccess("");
    if (!selectedResidentId) {
      setError("Choose a resident.");
      return;
    }
    if (!selectedMedicationId) {
      setError("Choose a medication.");
      return;
    }
    const parsedDose = Number(doseQuantity);
    if (!Number.isFinite(parsedDose) || parsedDose <= 0) {
      setError("Dose quantity must be a positive number.");
      return;
    }

    try {
      setSaving(true);
      const nowIso = new Date().toISOString();
      await createMarEntry(
        {
          clientRequestId: generateGuid(),
          residentId: selectedResidentId,
          medicationId: selectedMedicationId,
          status: selectedStatus,
          doseQuantity: parsedDose,
          doseUnit: doseUnit.trim() || "tablet",
          administeredAtUtc: nowIso,
          scheduledForUtc: nowIso,
          notes: notes.trim(),
          recordedBy: user?.displayName || user?.username || "mobile-nurse"
        },
        token
      );
      setNotes("");
      setSuccess("MAR entry saved.");
      await loadData();
    } catch (err) {
      setError(err?.message || "Failed to create MAR entry.");
    } finally {
      setSaving(false);
    }
  }

  async function onVoid(entryId) {
    setError("");
    setSuccess("");
    try {
      setVoidingId(entryId);
      await voidMarEntry(entryId, "Voided from mobile", token);
      setSuccess("MAR entry voided.");
      await loadData();
    } catch (err) {
      setError(err?.message || "Failed to void MAR entry.");
    } finally {
      setVoidingId("");
    }
  }

  async function onLoadReport() {
    try {
      setReportLoading(true);
      setError("");
      const fromUtc = new Date(Date.now() - 24 * 60 * 60 * 1000).toISOString();
      const toUtc = new Date().toISOString();
      const residentId = selectedResidentId || undefined;
      const data = await getMarReport(token, { fromUtc, toUtc, residentId });
      setReport(data);
    } catch (err) {
      setError(err?.message || "Failed to load MAR report.");
    } finally {
      setReportLoading(false);
    }
  }

  return (
    <Screen>
      <Hero
        eyebrow="Medication Administration Record"
        title="MAR review and entry"
        subtitle="Record administrations quickly, filter the active feed, and pull a 24-hour summary without leaving mobile."
        badge="Nurse MAR tools"
      />

      {error ? <InfoBanner text={error} tone="danger" /> : null}
      {success ? <InfoBanner text={success} tone="success" /> : null}

      <Card>
        <SectionTitle
          title="Feed Filters"
          subtitle="Review recent entries by resident, medication, or status."
          actionLabel={includeVoided ? "Hide voided" : "Show voided"}
          onAction={() => setIncludeVoided((current) => !current)}
        />
        <AppInput value={query} onChangeText={setQuery} placeholder="Resident, medication, or status" autoCapitalize="none" />
        <PrimaryButton label={reportLoading ? "Loading Report..." : "Load 24h MAR Report"} onPress={onLoadReport} tone="secondary" />
      </Card>

      {report ? (
        <Card>
          <SectionTitle title="24 Hour MAR Report" subtitle="Quick compliance snapshot for the current resident scope." />
          <View style={{ flexDirection: "row", flexWrap: "wrap" }}>
            {[
              ["Total", report.summary?.totalEntries ?? report.Summary?.TotalEntries ?? 0],
              ["Given", report.summary?.givenCount ?? report.Summary?.GivenCount ?? 0],
              ["Refused", report.summary?.refusedCount ?? report.Summary?.RefusedCount ?? 0],
              ["Missed", report.summary?.missedCount ?? report.Summary?.MissedCount ?? 0],
              ["Held", report.summary?.heldCount ?? report.Summary?.HeldCount ?? 0],
              ["Not Avail.", report.summary?.notAvailableCount ?? report.Summary?.NotAvailableCount ?? 0]
            ].map(([label, value]) => (
              <View
                key={label}
                style={{
                  width: "48%",
                  marginRight: "2%",
                  marginBottom: spacing.sm,
                  backgroundColor: colors.background,
                  borderRadius: 16,
                  padding: spacing.md
                }}
              >
                <Text style={{ color: colors.textMuted, fontSize: 12, fontWeight: "700" }}>{label}</Text>
                <Text style={{ color: colors.text, fontSize: 24, fontWeight: "800" }}>{value}</Text>
              </View>
            ))}
          </View>
        </Card>
      ) : null}

      <Card>
        <SectionTitle title="New MAR Entry" subtitle="Pick resident, medication, status, and dose details." />
        <FormLabel>Resident</FormLabel>
        <FlatList
          horizontal
          data={residents}
          keyExtractor={(item) => String(item.id || item.Id)}
          showsHorizontalScrollIndicator={false}
          renderItem={({ item }) => (
            <Chip
              label={toResidentName(item)}
              selected={String(item.id || item.Id) === selectedResidentId}
              onPress={() => setSelectedResidentId(String(item.id || item.Id))}
            />
          )}
          style={{ marginBottom: spacing.sm }}
        />

        <FormLabel>Medication</FormLabel>
        <FlatList
          horizontal
          data={filteredMeds}
          keyExtractor={(item) => String(item.id || item.Id)}
          showsHorizontalScrollIndicator={false}
          renderItem={({ item }) => (
            <Chip
              label={item.medName || item.MedName || "Medication"}
              selected={String(item.id || item.Id) === selectedMedicationId}
              onPress={() => setSelectedMedicationId(String(item.id || item.Id))}
            />
          )}
          ListEmptyComponent={<Text style={{ color: colors.textMuted, marginBottom: spacing.sm }}>No resident medications found.</Text>}
          style={{ marginBottom: spacing.sm }}
        />

        <FormLabel>Status</FormLabel>
        <FlatList
          horizontal
          data={MAR_STATUSES}
          keyExtractor={(item) => item}
          showsHorizontalScrollIndicator={false}
          renderItem={({ item }) => (
            <Chip label={item} selected={item === selectedStatus} onPress={() => setSelectedStatus(item)} />
          )}
          style={{ marginBottom: spacing.sm }}
        />

        <FormLabel>Dose Quantity</FormLabel>
        <AppInput value={doseQuantity} onChangeText={setDoseQuantity} placeholder="1" keyboardType="decimal-pad" autoCapitalize="none" />
        <FormLabel>Dose Unit</FormLabel>
        <AppInput value={doseUnit} onChangeText={setDoseUnit} placeholder="tablet" />
        <FormLabel>Notes</FormLabel>
        <AppInput value={notes} onChangeText={setNotes} placeholder="Optional notes" multiline />
        <PrimaryButton label={saving ? "Saving..." : "Save MAR Entry"} onPress={onCreate} disabled={saving} />
      </Card>

      <Card style={{ marginBottom: 0 }}>
        <SectionTitle title="Recent MAR Entries" subtitle={`${filteredEntries.length} entries in the current feed`} />
        {loading ? <LoadingBlock label="Loading MAR feed" /> : null}
        <FlatList
          data={filteredEntries}
          scrollEnabled={false}
          refreshControl={<RefreshControl refreshing={refreshing} onRefresh={async () => { setRefreshing(true); try { await loadData(); } finally { setRefreshing(false); } }} tintColor={colors.accent} />}
          keyExtractor={(item) => String(item.id || item.Id)}
          renderItem={({ item }) => {
            const entryId = String(item.id || item.Id || "");
            const residentId = String(item.residentId || item.ResidentId || "");
            const medName = medications.find((med) => String(med.id || med.Id) === String(item.medicationId || item.MedicationId))?.medName || "Medication";
            const isVoided = item.isVoided || item.IsVoided;
            return (
              <ListRow
                title={`${residentById.get(residentId) || "Resident"} - ${medName}`}
                subtitle={`${(item.status || item.Status || "").toString()} | ${(item.doseQuantity || item.DoseQuantity || "").toString()} ${(item.doseUnit || item.DoseUnit || "").toString()}`}
                meta={(item.administeredAtUtc || item.AdministeredAtUtc || "").toString()}
              >
                {!isVoided ? (
                  <TouchableOpacity onPress={() => onVoid(entryId)} disabled={voidingId === entryId}>
                    <Text style={{ color: voidingId === entryId ? colors.textMuted : colors.danger, fontWeight: "700" }}>
                      {voidingId === entryId ? "Voiding..." : "Void Entry"}
                    </Text>
                  </TouchableOpacity>
                ) : (
                  <Text style={{ color: colors.danger, fontWeight: "700" }}>Voided</Text>
                )}
              </ListRow>
            );
          }}
          ListEmptyComponent={!loading ? <Text style={{ color: colors.textMuted }}>No MAR entries match the current filter.</Text> : null}
        />
      </Card>
    </Screen>
  );
}
