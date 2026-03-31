import React, { useCallback, useEffect, useMemo, useState } from "react";
import { Alert, FlatList, RefreshControl, Text, TouchableOpacity, View } from "react-native";
import { useAuth } from "../context/AuthContext";
import {
  createObservation,
  deleteObservation,
  getObservations,
  getObservationsByResident,
  getResidents,
  updateObservation
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

function residentName(item) {
  const first = item.residentFName || item.ResidentFName || "";
  const last = item.residentLName || item.ResidentLName || "";
  return `${first} ${last}`.trim();
}

export default function ObservationsScreen() {
  const { token, user } = useAuth();
  const [items, setItems] = useState([]);
  const [residents, setResidents] = useState([]);
  const [selectedResidentId, setSelectedResidentId] = useState("");
  const [selectedResidentName, setSelectedResidentName] = useState("");
  const [type, setType] = useState("");
  const [value, setValue] = useState("");
  const [editingId, setEditingId] = useState("");
  const [saving, setSaving] = useState(false);
  const [loadingList, setLoadingList] = useState(false);
  const [refreshing, setRefreshing] = useState(false);
  const [query, setQuery] = useState("");
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");

  const canRecord = user?.role === "Nurse" || user?.role === "General CareStaff";
  const isObserver = user?.role === "Observer";

  const loadObservations = useCallback(
    async (residentIdOverride = selectedResidentId) => {
      try {
        setLoadingList(true);
        setError("");
        const data =
          residentIdOverride && canRecord
            ? await getObservationsByResident(residentIdOverride, token)
            : await getObservations(token);
        setItems(Array.isArray(data) ? data : []);
      } catch (err) {
        setError(err?.message || "Failed to load observations.");
      } finally {
        setLoadingList(false);
      }
    },
    [canRecord, selectedResidentId, token]
  );

  const loadResidents = useCallback(async () => {
    if (!canRecord) {
      setResidents([]);
      return;
    }

    try {
      const data = await getResidents(token);
      const list = Array.isArray(data) ? data : [];
      setResidents(list);
      if (list.length > 0) {
        const first = list[0];
        const firstId = String(first.id || first.Id || "");
        setSelectedResidentId((current) => current || firstId);
        setSelectedResidentName((current) => current || residentName(first));
      }
    } catch (err) {
      setError(err?.message || "Failed to load residents for observation entry.");
    }
  }, [canRecord, token]);

  useEffect(() => {
    loadResidents();
  }, [loadResidents]);

  useEffect(() => {
    loadObservations();
  }, [loadObservations]);

  const onRefresh = useCallback(async () => {
    try {
      setRefreshing(true);
      await loadObservations();
    } finally {
      setRefreshing(false);
    }
  }, [loadObservations]);

  function pickResident(resident) {
    const id = String(resident.id || resident.Id || "");
    setSelectedResidentId(id);
    setSelectedResidentName(residentName(resident));
    loadObservations(id);
  }

  function resetForm() {
    setEditingId("");
    setType("");
    setValue("");
  }

  async function onSaveObservation() {
    setSuccess("");
    setError("");
    if (!selectedResidentId) {
      setError("Choose a resident.");
      return;
    }
    if (!type.trim() || !value.trim()) {
      setError("Type and value are required.");
      return;
    }

    try {
      setSaving(true);
      const payload = {
        id: editingId || undefined,
        residentId: selectedResidentId,
        residentName: selectedResidentName,
        type: type.trim(),
        value: value.trim(),
        recordedBy: user?.displayName || user?.username || "mobile-user"
      };

      if (editingId) {
        await updateObservation(editingId, { ...payload, id: editingId }, token);
        setSuccess("Observation updated.");
      } else {
        await createObservation(payload, token);
        setSuccess("Observation saved.");
      }

      resetForm();
      await loadObservations(selectedResidentId);
    } catch (err) {
      setError(err?.message || "Failed to save observation.");
    } finally {
      setSaving(false);
    }
  }

  function startEdit(item) {
    setEditingId(String(item.id || item.Id || ""));
    setType((item.type || item.Type || "").toString());
    setValue((item.value || item.Value || "").toString());
    setSelectedResidentId(String(item.residentId || item.ResidentId || ""));
    setSelectedResidentName((item.residentName || item.ResidentName || "").toString());
    setError("");
    setSuccess("");
  }

  function confirmDelete(item) {
    const observationId = String(item.id || item.Id || "");
    Alert.alert("Delete observation", "This permanently removes the observation.", [
      { text: "Cancel", style: "cancel" },
      {
        text: "Delete",
        style: "destructive",
        onPress: async () => {
          try {
            setError("");
            setSuccess("");
            await deleteObservation(observationId, token);
            if (editingId === observationId) resetForm();
            setSuccess("Observation deleted.");
            await loadObservations(selectedResidentId);
          } catch (err) {
            setError(err?.message || "Failed to delete observation.");
          }
        }
      }
    ]);
  }

  const modeLabel = useMemo(() => {
    if (isObserver) return "Observer history";
    if (canRecord) return "Observation recording";
    return "Unavailable";
  }, [canRecord, isObserver]);

  const filteredItems = useMemo(() => {
    const term = query.trim().toLowerCase();
    if (!term) return items;
    return items.filter((item) => {
      const typeText = String(item.type || item.Type || "").toLowerCase();
      const valueText = String(item.value || item.Value || "").toLowerCase();
      const nameText = String(item.residentName || item.ResidentName || "").toLowerCase();
      const recorder = String(item.recordedBy || item.RecordedBy || "").toLowerCase();
      return typeText.includes(term) || valueText.includes(term) || nameText.includes(term) || recorder.includes(term);
    });
  }, [items, query]);

  return (
    <Screen>
      <Hero
        eyebrow="Observation Feed"
        title="Track resident observations"
        subtitle={canRecord ? "Capture notes and vitals quickly, then review the shift feed in one place." : "Read-only observation history for the signed-in resident account."}
        badge={modeLabel}
      />

      {error ? <InfoBanner text={error} tone="danger" /> : null}
      {success ? <InfoBanner text={success} tone="success" /> : null}
      {isObserver ? <InfoBanner text="Observer accounts can only review their own observation records." /> : null}

      <Card>
        <SectionTitle title="Search Feed" subtitle="Filter by observation type, resident, value, or recorder." />
        <AppInput value={query} onChangeText={setQuery} placeholder="Filter observations" autoCapitalize="none" />
      </Card>

      {canRecord ? (
        <Card>
          <SectionTitle
            title={editingId ? "Edit Observation" : "Record Observation"}
            subtitle="Select a resident, record the type, and capture the value."
          />
          <FlatList
            horizontal
            data={residents}
            keyExtractor={(item) => String(item.id || item.Id)}
            showsHorizontalScrollIndicator={false}
            renderItem={({ item }) => (
              <Chip
                label={residentName(item) || "Resident"}
                selected={String(item.id || item.Id) === selectedResidentId}
                onPress={() => pickResident(item)}
              />
            )}
            style={{ marginBottom: spacing.sm }}
          />

          <FormLabel>Observation Type</FormLabel>
          <AppInput value={type} onChangeText={setType} placeholder="BP, Temp, Mood, Note" />
          <FormLabel>Observation Value</FormLabel>
          <AppInput value={value} onChangeText={setValue} placeholder="120/80, 37.1, Resident resting comfortably" multiline />

          <PrimaryButton
            label={saving ? "Saving..." : editingId ? "Save Changes" : "Save Observation"}
            onPress={onSaveObservation}
            disabled={saving}
          />
          {editingId ? <PrimaryButton label="Cancel Edit" onPress={resetForm} tone="secondary" /> : null}
        </Card>
      ) : null}

      <Card style={{ marginBottom: 0 }}>
        <SectionTitle title="Observation Feed" subtitle={`${filteredItems.length} items in the current view`} />
        {loadingList ? <LoadingBlock label="Loading observations" /> : null}
        <FlatList
          data={filteredItems}
          scrollEnabled={false}
          refreshControl={<RefreshControl refreshing={refreshing} onRefresh={onRefresh} tintColor={colors.accent} />}
          keyExtractor={(item) => String(item.id || item.Id)}
          renderItem={({ item }) => (
            <ListRow
              title={`${(item.type || item.Type || "").toString()}: ${(item.value || item.Value || "").toString()}`}
              subtitle={`${(item.residentName || item.ResidentName || "").toString()} | ${(item.recordedBy || item.RecordedBy || "").toString()}`}
              meta={(item.recordedAt || item.RecordedAt || "").toString()}
            >
              {canRecord ? (
                <View style={{ flexDirection: "row", marginTop: spacing.xs }}>
                  <TouchableOpacity onPress={() => startEdit(item)} style={{ marginRight: spacing.md }}>
                    <Text style={{ color: colors.accent, fontWeight: "700" }}>Edit</Text>
                  </TouchableOpacity>
                  <TouchableOpacity onPress={() => confirmDelete(item)}>
                    <Text style={{ color: colors.danger, fontWeight: "700" }}>Delete</Text>
                  </TouchableOpacity>
                </View>
              ) : null}
            </ListRow>
          )}
          ListEmptyComponent={!loadingList ? <Text style={{ color: colors.textMuted }}>No observations match the current filter.</Text> : null}
        />
      </Card>
    </Screen>
  );
}
