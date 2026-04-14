import React, { useCallback, useEffect, useMemo, useState } from "react";
import { Alert, FlatList, RefreshControl, Text } from "react-native";
import { useAuth } from "../context/AuthContext";
import {
  adjustMedicationStock,
  createMedication,
  deleteMedication,
  getLowStockMedications,
  getMedications,
  getResidents,
  updateMedication
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

const EMPTY_FORM = {
  id: "",
  medName: "",
  dosage: "",
  usage: "",
  quantity: "1",
  quantityUnit: "tablet",
  stockQuantity: "0",
  reorderLevel: "10",
  expiryDate: "",
  residentId: "",
  residentName: "",
  timesPerDay: "3"
};

function medicationName(item) {
  return item.medName || item.MedName || "Medication";
}

function toForm(item) {
  return {
    id: String(item.id || item.Id || ""),
    medName: item.medName || item.MedName || "",
    dosage: item.dosage || item.Dosage || "",
    usage: item.usage || item.Usage || "",
    quantity: String(item.quantity || item.Quantity || 1),
    quantityUnit: item.quantityUnit || item.QuantityUnit || "tablet",
    stockQuantity: String(item.stockQuantity ?? item.StockQuantity ?? 0),
    reorderLevel: String(item.reorderLevel ?? item.ReorderLevel ?? 10),
    expiryDate: (item.expiryDate || item.ExpiryDate || "").toString().slice(0, 10),
    residentId: String(item.residentId || item.ResidentId || ""),
    residentName: item.residentName || item.ResidentName || "",
    timesPerDay: String(item.timesPerDay || item.TimesPerDay || 3)
  };
}

function parseIntField(value, fallback) {
  const parsed = Number(value);
  if (!Number.isFinite(parsed)) return fallback;
  return Math.max(0, Math.round(parsed));
}

function toPayload(form) {
  const residentId = form.residentId.trim();
  return {
    id: form.id || undefined,
    medName: form.medName.trim(),
    dosage: form.dosage.trim(),
    usage: form.usage.trim() || null,
    quantity: parseIntField(form.quantity, 1),
    quantityUnit: form.quantityUnit.trim() || "tablet",
    stockQuantity: parseIntField(form.stockQuantity, 0),
    reorderLevel: parseIntField(form.reorderLevel, 10),
    expiryDate: form.expiryDate ? new Date(`${form.expiryDate}T00:00:00Z`).toISOString() : new Date().toISOString(),
    residentId: residentId || null,
    residentName: form.residentName.trim() || null,
    timesPerDay: parseIntField(form.timesPerDay, 3)
  };
}

export default function MedicationsScreen() {
  const { token, user } = useAuth();
  const [items, setItems] = useState([]);
  const [lowStockItems, setLowStockItems] = useState([]);
  const [residents, setResidents] = useState([]);
  const [loading, setLoading] = useState(false);
  const [refreshing, setRefreshing] = useState(false);
  const [saving, setSaving] = useState(false);
  const [deletingId, setDeletingId] = useState("");
  const [selectedMedicationId, setSelectedMedicationId] = useState("");
  const [formMode, setFormMode] = useState("");
  const [query, setQuery] = useState("");
  const [stockDelta, setStockDelta] = useState("1");
  const [showLowStock, setShowLowStock] = useState(false);
  const [form, setForm] = useState(EMPTY_FORM);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");

  const canManage = user?.role === "Nurse";
  const isObserver = user?.role === "Observer";

  const residentOptions = useMemo(
    () =>
      residents.map((resident) => ({
        id: String(resident.id || resident.Id || ""),
        name: `${resident.residentFName || resident.ResidentFName || ""} ${resident.residentLName || resident.ResidentLName || ""}`.trim()
      })),
    [residents]
  );

  const load = useCallback(
    async (isRefresh = false) => {
      try {
        if (isRefresh) setRefreshing(true);
        else setLoading(true);
        setError("");

        const requests = [getMedications(token)];
        if (canManage) {
          requests.push(getResidents(token));
          requests.push(getLowStockMedications(token));
        }

        const results = await Promise.all(requests);
        const medicationList = Array.isArray(results[0]) ? results[0] : [];
        setItems(medicationList);

        if (canManage) {
          const residentList = Array.isArray(results[1]) ? results[1] : [];
          const lowList = Array.isArray(results[2]) ? results[2] : [];
          setResidents(residentList);
          setLowStockItems(lowList);

          if (selectedMedicationId) {
            const selected = medicationList.find((item) => String(item.id || item.Id) === selectedMedicationId);
            if (selected) {
              if (formMode === "edit") {
                setForm(toForm(selected));
              }
              return;
            }

            setSelectedMedicationId("");
            setFormMode("");
          }

          if (medicationList.length === 0) {
            setSelectedMedicationId("");
            setFormMode("");
            setForm(EMPTY_FORM);
          }
        }
      } catch (err) {
        setError(err?.message || "Failed to load medications.");
      } finally {
        if (isRefresh) setRefreshing(false);
        else setLoading(false);
      }
    },
    [canManage, formMode, selectedMedicationId, token]
  );

  useEffect(() => {
    load();
  }, [load]);

  const filteredItems = useMemo(() => {
    const term = query.trim().toLowerCase();
    const source = showLowStock && canManage ? lowStockItems : items;
    if (!term) return source;
    return source.filter((item) => {
      const medName = String(item.medName || item.MedName || "").toLowerCase();
      const dosage = String(item.dosage || item.Dosage || "").toLowerCase();
      const usage = String(item.usage || item.Usage || "").toLowerCase();
      const residentName = String(item.residentName || item.ResidentName || "").toLowerCase();
      return medName.includes(term) || dosage.includes(term) || usage.includes(term) || residentName.includes(term);
    });
  }, [canManage, items, lowStockItems, query, showLowStock]);

  const selectedMedication = useMemo(
    () => items.find((item) => String(item.id || item.Id || "") === selectedMedicationId),
    [items, selectedMedicationId]
  );

  function updateForm(key, value) {
    setForm((current) => ({ ...current, [key]: value }));
  }

  function startCreate() {
    setSelectedMedicationId("");
    setFormMode("create");
    setForm(EMPTY_FORM);
    setError("");
    setSuccess("");
  }

  function selectMedication(item) {
    setSelectedMedicationId(String(item.id || item.Id || ""));
    setFormMode("");
    setError("");
    setSuccess("");
  }

  function startEditMedication() {
    if (!selectedMedication) return;
    setSelectedMedicationId(String(selectedMedication.id || selectedMedication.Id || ""));
    setForm(toForm(selectedMedication));
    setFormMode("edit");
    setError("");
    setSuccess("");
  }

  async function onSave() {
    setError("");
    setSuccess("");
    if (!form.medName.trim()) {
      setError("Medication name is required.");
      return;
    }
    if (!form.dosage.trim()) {
      setError("Dosage is required.");
      return;
    }

    try {
      setSaving(true);
      const payload = toPayload(form);
      if (formMode === "edit" && selectedMedicationId) {
        await updateMedication(selectedMedicationId, { ...payload, id: selectedMedicationId }, token);
        setSuccess("Medication updated.");
      } else {
        const created = await createMedication(payload, token);
        const createdId = String(created?.id || created?.Id || "");
        if (createdId) setSelectedMedicationId(createdId);
        setSuccess("Medication created.");
      }
      setFormMode("");
      await load();
    } catch (err) {
      setError(err?.message || "Failed to save medication.");
    } finally {
      setSaving(false);
    }
  }

  async function onAdjustStock() {
    const delta = Number(stockDelta);
    if (!selectedMedicationId) {
      setError("Choose a medication first.");
      return;
    }
    if (!Number.isFinite(delta) || delta === 0) {
      setError("Stock delta must be a non-zero number.");
      return;
    }

    try {
      setError("");
      setSuccess("");
      await adjustMedicationStock(selectedMedicationId, Math.round(delta), token);
      setSuccess(`Stock adjusted by ${Math.round(delta)}.`);
      await load();
    } catch (err) {
      setError(err?.message || "Failed to adjust stock.");
    }
  }

  function confirmDelete() {
    if (!selectedMedicationId) return;
    Alert.alert("Delete medication", "This permanently removes the medication record.", [
      { text: "Cancel", style: "cancel" },
      {
        text: "Delete",
        style: "destructive",
        onPress: async () => {
          try {
            setDeletingId(selectedMedicationId);
            setError("");
            setSuccess("");
            await deleteMedication(selectedMedicationId, token);
            setSelectedMedicationId("");
            setFormMode("");
            setForm(EMPTY_FORM);
            setSuccess("Medication deleted.");
            await load();
          } catch (err) {
            setError(err?.message || "Failed to delete medication.");
          } finally {
            setDeletingId("");
          }
        }
      }
    ]);
  }

  return (
    <Screen>
      <Hero
        eyebrow="Medication Inventory"
        title="Medication management"
        subtitle={canManage ? "Manage resident medications, inventory-only stock, and quick inventory adjustments from mobile." : "Review the medication list and stock state for the signed-in resident context."}
        badge={canManage ? "Nurse inventory controls" : "Observer view"}
      />

      {error ? <InfoBanner text={error} tone="danger" /> : null}
      {success ? <InfoBanner text={success} tone="success" /> : null}

      <Card>
        <SectionTitle
          title="Search and Filters"
          subtitle="Search by medication, dosage, usage, or assigned resident."
          actionLabel={canManage ? "Toggle low stock" : undefined}
          onAction={canManage ? () => setShowLowStock((current) => !current) : undefined}
        />
        <AppInput value={query} onChangeText={setQuery} placeholder="Medication, dosage, usage, resident" autoCapitalize="none" />
        {canManage ? (
          <InfoBanner text={showLowStock ? "Showing only low-stock medications." : "Showing all medications."} />
        ) : null}
        {canManage ? (
          <PrimaryButton label="New Medication" onPress={startCreate} tone="secondary" />
        ) : null}
      </Card>

      {selectedMedication ? (
        <Card>
          <SectionTitle
            title="Medication Detail"
            subtitle="Review medication details before choosing an action."
          />
          <Text style={{ color: colors.text, fontWeight: "900", fontSize: 18, marginBottom: spacing.sm }}>
            {medicationName(selectedMedication)}
          </Text>
          <Text style={{ color: colors.textMuted, marginBottom: spacing.xs }}>
            Dosage: {selectedMedication.dosage || selectedMedication.Dosage || "N/A"}
          </Text>
          <Text style={{ color: colors.textMuted, marginBottom: spacing.xs }}>
            Assigned to: {selectedMedication.residentName || selectedMedication.ResidentName || "Inventory"}
          </Text>
          <Text style={{ color: colors.textMuted, marginBottom: spacing.xs }}>
            Stock: {selectedMedication.stockQuantity ?? selectedMedication.StockQuantity ?? 0}
          </Text>
          <Text style={{ color: colors.textMuted, marginBottom: spacing.md }}>
            Reorder at: {selectedMedication.reorderLevel ?? selectedMedication.ReorderLevel ?? 0}
          </Text>
          {canManage ? (
            <PrimaryButton label="Edit Medication" onPress={startEditMedication} />
          ) : null}
        </Card>
      ) : null}

      {canManage && formMode ? (
        <Card>
          <SectionTitle title={formMode === "edit" ? "Edit Medication" : "Create Medication"} subtitle="Maintain dosage, inventory levels, and resident assignment." />

          <FormLabel>Medication Name</FormLabel>
          <AppInput value={form.medName} onChangeText={(value) => updateForm("medName", value)} placeholder="Medication name" />
          <FormLabel>Dosage</FormLabel>
          <AppInput value={form.dosage} onChangeText={(value) => updateForm("dosage", value)} placeholder="Dosage" />
          <FormLabel>Usage</FormLabel>
          <AppInput value={form.usage} onChangeText={(value) => updateForm("usage", value)} placeholder="Usage or indication" multiline />
          <FormLabel>Quantity Per Dose</FormLabel>
          <AppInput value={form.quantity} onChangeText={(value) => updateForm("quantity", value)} placeholder="1" keyboardType="number-pad" />
          <FormLabel>Quantity Unit</FormLabel>
          <AppInput value={form.quantityUnit} onChangeText={(value) => updateForm("quantityUnit", value)} placeholder="tablet" />
          <FormLabel>Stock Quantity</FormLabel>
          <AppInput value={form.stockQuantity} onChangeText={(value) => updateForm("stockQuantity", value)} placeholder="0" keyboardType="number-pad" />
          <FormLabel>Reorder Level</FormLabel>
          <AppInput value={form.reorderLevel} onChangeText={(value) => updateForm("reorderLevel", value)} placeholder="10" keyboardType="number-pad" />
          <FormLabel>Expiry Date</FormLabel>
          <AppInput value={form.expiryDate} onChangeText={(value) => updateForm("expiryDate", value)} placeholder="YYYY-MM-DD" autoCapitalize="none" />
          <FormLabel>Times Per Day</FormLabel>
          <AppInput value={form.timesPerDay} onChangeText={(value) => updateForm("timesPerDay", value)} placeholder="3" keyboardType="number-pad" />

          <FormLabel>Resident Assignment</FormLabel>
          <FlatList
            horizontal
            data={residentOptions}
            keyExtractor={(item) => item.id}
            showsHorizontalScrollIndicator={false}
            ListHeaderComponent={<Chip label="Inventory Only" selected={!form.residentId} onPress={() => { updateForm("residentId", ""); updateForm("residentName", ""); }} />}
            renderItem={({ item }) => (
              <Chip
                label={item.name || "Resident"}
                selected={item.id === form.residentId}
                onPress={() => {
                  updateForm("residentId", item.id);
                  updateForm("residentName", item.name);
                }}
              />
            )}
            style={{ marginBottom: spacing.sm }}
          />

          <PrimaryButton label={saving ? "Saving..." : formMode === "edit" ? "Save Medication" : "Create Medication"} onPress={onSave} disabled={saving} />

          {formMode === "edit" && selectedMedicationId ? (
            <>
              <FormLabel>Adjust Stock</FormLabel>
              <AppInput
                value={stockDelta}
                onChangeText={setStockDelta}
                placeholder="Use positive or negative value"
                keyboardType="numbers-and-punctuation"
                autoCapitalize="none"
              />
              <PrimaryButton label="Apply Stock Delta" onPress={onAdjustStock} tone="secondary" />
              <PrimaryButton
                label={deletingId === selectedMedicationId ? "Deleting..." : "Delete Medication"}
                onPress={confirmDelete}
                disabled={deletingId === selectedMedicationId}
                tone="secondary"
              />
            </>
          ) : null}
          <PrimaryButton
            label="Cancel"
            onPress={() => {
              setFormMode("");
              setForm(EMPTY_FORM);
            }}
            disabled={saving}
            tone="secondary"
          />
        </Card>
      ) : null}

      <Card style={{ marginBottom: 0 }}>
        <SectionTitle title="Medication List" subtitle={`${filteredItems.length} medication records`} />
        {loading ? <LoadingBlock label="Loading medications" /> : null}
        <FlatList
          data={filteredItems}
          scrollEnabled={false}
          refreshControl={<RefreshControl refreshing={refreshing} onRefresh={() => load(true)} tintColor={colors.accent} />}
          keyExtractor={(item) => String(item.id || item.Id)}
          renderItem={({ item }) => {
            const stock = item.stockQuantity ?? item.StockQuantity ?? 0;
            const reorderLevel = item.reorderLevel ?? item.ReorderLevel ?? 0;
            const assignedResident = item.residentName || item.ResidentName || "Inventory";
            return (
              <ListRow
                title={medicationName(item)}
                subtitle={`${item.dosage || item.Dosage || "No dosage"} | ${assignedResident}`}
                meta={`Stock ${stock} | Reorder at ${reorderLevel}`}
              >
                <Text style={{ color: stock <= reorderLevel ? colors.danger : colors.textMuted }}>
                  {stock <= reorderLevel ? "Low stock attention needed." : "Inventory level is currently above reorder threshold."}
                </Text>
                <PrimaryButton
                  label={String(item.id || item.Id || "") === selectedMedicationId ? "Viewing Details" : "View Details"}
                  onPress={() => selectMedication(item)}
                  tone="secondary"
                />
              </ListRow>
            );
          }}
          ListEmptyComponent={!loading ? <Text style={{ color: colors.textMuted }}>No medications match the current filter.</Text> : null}
        />
      </Card>
    </Screen>
  );
}
