import React, { useState } from "react";
import {
  ActivityIndicator,
  SafeAreaView,
  Text,
  TextInput,
  TouchableOpacity,
  View
} from "react-native";
import { useAuth } from "../context/AuthContext";
import { colors, radii, shadows, spacing, typography } from "../ui/theme";

export default function LoginScreen() {
  const { login, error, loading } = useAuth();
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");

  async function onLogin() {
    await login(username.trim(), password);
  }

  return (
    <SafeAreaView style={{ flex: 1, backgroundColor: colors.background }}>
      <View
        style={{
          flex: 1,
          paddingHorizontal: spacing.lg,
          paddingVertical: spacing.xl,
          justifyContent: "space-between"
        }}
      >
        <View>
          <View
            style={{
              backgroundColor: colors.panel,
              borderRadius: radii.lg,
              padding: spacing.lg,
              marginBottom: spacing.lg,
              ...shadows.card
            }}
          >
            <Text
              style={{
                color: colors.textOnDark,
                fontSize: typography.hero,
                fontWeight: "800",
                marginBottom: spacing.sm
              }}
            >
              CareHub
            </Text>
            <Text
              style={{
                color: "#d9d3c9",
                fontSize: typography.body,
                lineHeight: 21
              }}
            >
              Mobile clinical workspace for shift updates, medication checks, observations, and resident workflows.
            </Text>
          </View>

          <View
            style={{
              backgroundColor: colors.surface,
              borderRadius: radii.lg,
              padding: spacing.lg,
              borderWidth: 1,
              borderColor: colors.border,
              ...shadows.card
            }}
          >
            <Text
              style={{
                fontSize: typography.title,
                fontWeight: "800",
                color: colors.text,
                marginBottom: spacing.xs
              }}
            >
              Sign In
            </Text>
            <Text style={{ color: colors.textMuted, marginBottom: spacing.md }}>
              Roles enabled on mobile: Nurse, General CareStaff, Observer.
            </Text>

            <TextInput
              placeholder="Username"
              placeholderTextColor="#8f8477"
              value={username}
              onChangeText={setUsername}
              autoCapitalize="none"
              style={{
                backgroundColor: colors.background,
                borderWidth: 1,
                borderColor: colors.border,
                borderRadius: radii.md,
                marginBottom: spacing.sm,
                paddingHorizontal: spacing.md,
                paddingVertical: 14,
                color: colors.text
              }}
            />
            <TextInput
              placeholder="Password"
              placeholderTextColor="#8f8477"
              value={password}
              onChangeText={setPassword}
              secureTextEntry
              style={{
                backgroundColor: colors.background,
                borderWidth: 1,
                borderColor: colors.border,
                borderRadius: radii.md,
                marginBottom: spacing.md,
                paddingHorizontal: spacing.md,
                paddingVertical: 14,
                color: colors.text
              }}
            />

            <TouchableOpacity
              onPress={onLogin}
              disabled={loading}
              style={{
                backgroundColor: loading ? "#d79877" : colors.accent,
                paddingVertical: 15,
                borderRadius: radii.md,
                alignItems: "center"
              }}
            >
              <Text style={{ color: colors.textOnDark, fontWeight: "800", fontSize: typography.body }}>
                {loading ? "Signing In..." : "Enter Mobile Workspace"}
              </Text>
            </TouchableOpacity>

            {loading ? <ActivityIndicator style={{ marginTop: spacing.md }} color={colors.accent} /> : null}
            {error ? (
              <Text style={{ marginTop: spacing.md, color: colors.danger, lineHeight: 20 }}>
                {error}
              </Text>
            ) : null}
          </View>
        </View>

        <View
          style={{
            backgroundColor: colors.accentSoft,
            borderRadius: radii.md,
            padding: spacing.md,
            borderWidth: 1,
            borderColor: "#efc4ad"
          }}
        >
          <Text style={{ color: colors.text, fontWeight: "700", marginBottom: spacing.xs }}>
            Quick access accounts
          </Text>
          <Text style={{ color: colors.textMuted }}>
            `nurse1`, `carestaff1`, and `resident1` use the shared demo passwords documented in the mobile README.
          </Text>
        </View>
      </View>
    </SafeAreaView>
  );
}
