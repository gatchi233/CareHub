import React, { useCallback, useEffect, useMemo, useState } from "react";
import { Alert, FlatList, RefreshControl, Text, View } from "react-native";
import { useAuth } from "../context/AuthContext";
import { createResident, deleteResident, getResidents, updateResident } from "../services/apiClient";
import {
  AppInput,
  Card,
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
  residentFName: "",
  residentLName: "",
  roomNumber: "",
  roomType: "Single",
  bedLabel: "",
  gender: "",
  dateOfBirth: "",
  doctorName: "",
  doctorContact: "",
  emergencyContactName1: "",
  emergencyContactPhone1: "",
  emergencyRelationship1: "",
  remarks: ""
};

function residentName(item) {
  const first = item.residentFName || item.ResidentFName || "";
  const last = item.residentLName || item.ResidentLName || "";
  return `${first} ${last}`.trim();
}

function toForm(item) {
  return {
    id: String(item.id || item.Id || ""),
    residentFName: item.residentFName || item.ResidentFName || "",
    residentLName: item.residentLName || item.ResidentLName || "",
    roomNumber: item.roomNumber || item.RoomNumber || "",
    roomType: item.roomType || item.RoomType || "Single",
    bedLabel: item.bedLabel || item.BedLabel || "",
    gender: item.gender || item.Gender || "",
    dateOfBirth: item.dateOfBirth || item.DateOfBirth || "",
    doctorName: item.doctorName || item.DoctorName || "",
    doctorContact: item.doctorContact || item.DoctorContact || "",
    emergencyContactName1: item.emergencyContactName1 || item.EmergencyContactName1 || "",
    emergencyContactPhone1: item.emergencyContactPhone1 || item.EmergencyContactPhone1 || "",
    emergencyRelationship1: item.emergencyRelationship1 || item.EmergencyRelationship1 || "",
    remarks: item.remarks || item.Remarks || ""
  };
}

function toPayload(form) {
  return {
    id: form.id || undefined,
    ResidentFName: form.residentFName.trim(),
    ResidentLName: form.residentLName.trim(),
    RoomNumber: form.roomNumber.trim(),
    RoomType: form.roomType.trim() || "Single",
    BedLabel: form.bedLabel.trim() || null,
    Gender: form.gender.trim() || null,
    DateOfBirth: form.dateOfBirth.trim(),
    DoctorName: form.doctorName.trim(),
    DoctorContact: form.doctorContact.trim(),
    EmergencyContactName1: form.emergencyContactName1.trim(),
    EmergencyContactPhone1: form.emergencyContactPhone1.trim(),
    EmergencyRelationship1: form.emergencyRelationship1.trim(),
    Remarks: form.remarks.trim() || null
  };
}

export default function ResidentsScreen() {
  const { token, user } = useAuth();
  const [items, setItems] = useState([]);
  const [loading, setLoading] = useState(false);
  const [refreshing, setRefreshing] = useState(false);
  const [saving, setSaving] = useState(false);
  const [deletingId, setDeletingId] = useState("");
  const [query, setQuery] = useState("");
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");
  const [selectedResidentId, setSelectedResidentId] = useState("");
  const [formMode, setFormMode] = useState("");
  const [form, setForm] = useState(EMPTY_FORM);

  const canManage = user?.role === "Nurse";

  const load = useCallback(
    async (isRefresh = false) => {
      try {
        if (isRefresh) setRefreshing(true);
        else setLoading(true);
        setError("");
        const data = await getResidents(token);
        const list = Array.isArray(data) ? data : [];
        setItems(list);

        if (!canManage) return;

        if (selectedResidentId) {
          const selected = list.find((item) => String(item.id || item.Id) === selectedResidentId);
          if (selected) {
            if (formMode === "edit") {
              setForm(toForm(selected));
            }
            return;
          }

          setSelectedResidentId("");
          setFormMode("");
        }

        if (list.length === 0) {
          setSelectedResidentId("");
          setFormMode("");
          setForm(EMPTY_FORM);
        }
      } catch (err) {
        setError(err?.message || "Failed to load residents.");
      } finally {
        if (isRefresh) setRefreshing(false);
        else setLoading(false);
      }
    },
    [canManage, formMode, selectedResidentId, token]
  );

  useEffect(() => {
    load();
  }, [load]);

  const filteredItems = useMemo(() => {
    const term = query.trim().toLowerCase();
    if (!term) return items;
    return items.filter((item) => {
      const full = residentName(item).toLowerCase();
      const room = String(item.roomNumber || item.RoomNumber || "").toLowerCase();
      const doctor = String(item.doctorName || item.DoctorName || "").toLowerCase();
      return full.includes(term) || room.includes(term) || doctor.includes(term);
    });
  }, [items, query]);

  const selectedResident = useMemo(
    () => items.find((item) => String(item.id || item.Id || "") === selectedResidentId),
    [items, selectedResidentId]
  );

  function updateForm(key, value) {
    setForm((current) => ({ ...current, [key]: value }));
  }

  function startCreate() {
    setSelectedResidentId("");
    setFormMode("create");
    setForm(EMPTY_FORM);
    setError("");
    setSuccess("");
  }

  function selectResident(item) {
    setSelectedResidentId(String(item.id || item.Id || ""));
    setFormMode("");
    setError("");
    setSuccess("");
  }

  function startEditResident() {
    if (!selectedResident) return;
    setSelectedResidentId(String(selectedResident.id || selectedResident.Id || ""));
    setForm(toForm(selectedResident));
    setFormMode("edit");
    setError("");
    setSuccess("");
  }

  async function onSave() {
    setError("");
    setSuccess("");
    if (!form.residentFName.trim() || !form.residentLName.trim()) {
      setError("Resident first and last name are required.");
      return;
    }
    if (!form.roomNumber.trim()) {
      setError("Room number is required.");
      return;
    }
    if (!form.dateOfBirth.trim()) {
      setError("Date of birth is required.");
      return;
    }

    try {
      setSaving(true);
      const payload = toPayload(form);
      if (formMode === "edit" && selectedResidentId) {
        await updateResident(selectedResidentId, { ...payload, id: selectedResidentId }, token);
        setSuccess("Resident updated.");
      } else {
        const created = await createResident(payload, token);
        const createdId = String(created?.id || created?.Id || "");
        if (createdId) setSelectedResidentId(createdId);
        setSuccess("Resident created.");
      }
      setFormMode("");
      await load();
    } catch (err) {
      setError(err?.message || "Failed to save resident.");
    } finally {
      setSaving(false);
    }
  }

  function confirmDelete() {
    if (!selectedResidentId) return;
    Alert.alert("Delete resident", "This permanently removes the resident record.", [
      { text: "Cancel", style: "cancel" },
      {
        text: "Delete",
        style: "destructive",
        onPress: async () => {
          try {
            setDeletingId(selectedResidentId);
            setError("");
            setSuccess("");
            await deleteResident(selectedResidentId, token);
            setSelectedResidentId("");
            setFormMode("");
            setForm(EMPTY_FORM);
            setSuccess("Resident deleted.");
            await load();
          } catch (err) {
            setError(err?.message || "Failed to delete resident.");
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
        eyebrow="Resident Directory"
        title="Resident management"
        subtitle={canManage ? "Review, update, and add residents without leaving the mobile workflow." : "Browse resident records with the details that matter most on shift."}
        badge={canManage ? "Nurse editing enabled" : "Read only"}
      />

      {error ? <InfoBanner text={error} tone="danger" /> : null}
      {success ? <InfoBanner text={success} tone="success" /> : null}

      <Card>
        <SectionTitle
          title="Resident Search"
          subtitle="Filter by resident name, room, or doctor."
          actionLabel={canManage ? "New resident" : undefined}
          onAction={canManage ? startCreate : undefined}
        />
        <AppInput
          value={query}
          onChangeText={setQuery}
          placeholder="Search by name, room, or doctor"
          autoCapitalize="none"
        />
      </Card>

      {selectedResident ? (
        <Card>
          <SectionTitle
            title="Resident Detail"
            subtitle="Review the resident profile before choosing an action."
          />
          <Text style={{ color: colors.text, fontWeight: "900", fontSize: 18, marginBottom: spacing.sm }}>
            {residentName(selectedResident) || "Unknown resident"}
          </Text>
          <Text style={{ color: colors.textMuted, marginBottom: spacing.xs }}>
            Room {selectedResident.roomNumber || selectedResident.RoomNumber || "N/A"}
          </Text>
          <Text style={{ color: colors.textMuted, marginBottom: spacing.xs }}>
            DOB {selectedResident.dateOfBirth || selectedResident.DateOfBirth || "N/A"}
          </Text>
          <Text style={{ color: colors.textMuted, marginBottom: spacing.xs }}>
            Doctor: {selectedResident.doctorName || selectedResident.DoctorName || "Not set"}
          </Text>
          <Text style={{ color: colors.textMuted, marginBottom: spacing.md }}>
            Contact: {selectedResident.emergencyContactName1 || selectedResident.EmergencyContactName1 || "N/A"}
            {selectedResident.emergencyContactPhone1 || selectedResident.EmergencyContactPhone1
              ? ` | ${selectedResident.emergencyContactPhone1 || selectedResident.EmergencyContactPhone1}`
              : ""}
          </Text>
          {canManage ? (
            <PrimaryButton label="Edit Resident" onPress={startEditResident} />
          ) : null}
        </Card>
      ) : null}

      {canManage && formMode ? (
        <Card>
          <SectionTitle
            title={formMode === "edit" ? "Edit Resident" : "Create Resident"}
            subtitle="Core resident profile details for rooming and contact records."
          />

          <FormLabel>First Name</FormLabel>
          <AppInput value={form.residentFName} onChangeText={(value) => updateForm("residentFName", value)} placeholder="First name" />
          <FormLabel>Last Name</FormLabel>
          <AppInput value={form.residentLName} onChangeText={(value) => updateForm("residentLName", value)} placeholder="Last name" />
          <FormLabel>Room Number</FormLabel>
          <AppInput value={form.roomNumber} onChangeText={(value) => updateForm("roomNumber", value)} placeholder="Room number" />
          <FormLabel>Room Type</FormLabel>
          <AppInput value={form.roomType} onChangeText={(value) => updateForm("roomType", value)} placeholder="Single or Double" />
          <FormLabel>Bed Label</FormLabel>
          <AppInput value={form.bedLabel} onChangeText={(value) => updateForm("bedLabel", value)} placeholder="Bed label" />
          <FormLabel>Gender</FormLabel>
          <AppInput value={form.gender} onChangeText={(value) => updateForm("gender", value)} placeholder="Gender" />
          <FormLabel>Date of Birth</FormLabel>
          <AppInput value={form.dateOfBirth} onChangeText={(value) => updateForm("dateOfBirth", value)} placeholder="YYYY-MM-DD" autoCapitalize="none" />
          <FormLabel>Doctor Name</FormLabel>
          <AppInput value={form.doctorName} onChangeText={(value) => updateForm("doctorName", value)} placeholder="Doctor name" />
          <FormLabel>Doctor Contact</FormLabel>
          <AppInput value={form.doctorContact} onChangeText={(value) => updateForm("doctorContact", value)} placeholder="Doctor contact" />
          <FormLabel>Emergency Contact</FormLabel>
          <AppInput value={form.emergencyContactName1} onChangeText={(value) => updateForm("emergencyContactName1", value)} placeholder="Primary contact name" />
          <FormLabel>Emergency Phone</FormLabel>
          <AppInput value={form.emergencyContactPhone1} onChangeText={(value) => updateForm("emergencyContactPhone1", value)} placeholder="Primary contact phone" autoCapitalize="none" />
          <FormLabel>Emergency Relationship</FormLabel>
          <AppInput value={form.emergencyRelationship1} onChangeText={(value) => updateForm("emergencyRelationship1", value)} placeholder="Relationship" />
          <FormLabel>Remarks</FormLabel>
          <AppInput value={form.remarks} onChangeText={(value) => updateForm("remarks", value)} placeholder="Remarks" multiline />

          <PrimaryButton
            label={saving ? "Saving..." : formMode === "edit" ? "Save Resident" : "Create Resident"}
            onPress={onSave}
            disabled={saving}
          />
          {formMode === "edit" && selectedResidentId ? (
            <PrimaryButton
              label={deletingId === selectedResidentId ? "Deleting..." : "Delete Resident"}
              onPress={confirmDelete}
              disabled={deletingId === selectedResidentId}
              tone="secondary"
            />
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
        <SectionTitle title="Residents" subtitle={`${filteredItems.length} resident records`} />
        {loading ? <LoadingBlock label="Loading residents" /> : null}
        <FlatList
          data={filteredItems}
          scrollEnabled={false}
          refreshControl={<RefreshControl refreshing={refreshing} onRefresh={() => load(true)} tintColor={colors.accent} />}
          keyExtractor={(item) => String(item.id || item.Id)}
          renderItem={({ item }) => (
            <ListRow
              title={residentName(item) || "Unknown resident"}
              subtitle={`Room ${item.roomNumber || item.RoomNumber || "N/A"} | DOB ${item.dateOfBirth || item.DateOfBirth || "N/A"}`}
              meta={`Doctor: ${item.doctorName || item.DoctorName || "Not set"}`}
            >
              <PrimaryButton
                label={String(item.id || item.Id || "") === selectedResidentId ? "Viewing Details" : "View Details"}
                onPress={() => selectResident(item)}
                tone="secondary"
              />
            </ListRow>
          )}
          ListEmptyComponent={!loading ? <Text style={{ color: colors.textMuted }}>No residents match the current filter.</Text> : null}
        />
      </Card>
    </Screen>
  );
}
