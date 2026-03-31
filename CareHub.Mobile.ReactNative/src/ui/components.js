import React from "react";
import {
  ActivityIndicator,
  SafeAreaView,
  ScrollView,
  Text,
  TextInput,
  TouchableOpacity,
  View
} from "react-native";
import { colors, radii, shadows, spacing, typography } from "./theme";

export function Screen({ children, scroll = true }) {
  const content = scroll ? (
    <ScrollView
      contentContainerStyle={{ padding: spacing.lg, paddingBottom: spacing.xl + 20 }}
      showsVerticalScrollIndicator={false}
    >
      {children}
    </ScrollView>
  ) : (
    <View style={{ flex: 1, padding: spacing.lg }}>{children}</View>
  );

  return <SafeAreaView style={{ flex: 1, backgroundColor: colors.background }}>{content}</SafeAreaView>;
}

export function Hero({ eyebrow, title, subtitle, badge }) {
  return (
    <View
      style={{
        backgroundColor: colors.panel,
        borderRadius: radii.lg,
        padding: spacing.lg,
        marginBottom: spacing.lg,
        ...shadows.card
      }}
    >
      {eyebrow ? (
        <Text style={{ color: "#d9d3c9", fontSize: typography.micro, fontWeight: "700", marginBottom: spacing.xs }}>
          {eyebrow}
        </Text>
      ) : null}
      <Text style={{ color: colors.textOnDark, fontSize: typography.title, fontWeight: "800", marginBottom: spacing.xs }}>
        {title}
      </Text>
      {subtitle ? <Text style={{ color: "#d9d3c9", lineHeight: 21 }}>{subtitle}</Text> : null}
      {badge ? (
        <View
          style={{
            alignSelf: "flex-start",
            marginTop: spacing.md,
            backgroundColor: colors.panelSoft,
            borderRadius: radii.pill,
            paddingHorizontal: spacing.md,
            paddingVertical: 8
          }}
        >
          <Text style={{ color: colors.textOnDark, fontWeight: "700" }}>{badge}</Text>
        </View>
      ) : null}
    </View>
  );
}

export function Card({ children, style }) {
  return (
    <View
      style={[
        {
          backgroundColor: colors.surface,
          borderRadius: radii.lg,
          padding: spacing.lg,
          marginBottom: spacing.lg,
          borderWidth: 1,
          borderColor: colors.border,
          ...shadows.card
        },
        style
      ]}
    >
      {children}
    </View>
  );
}

export function SectionTitle({ title, subtitle, actionLabel, onAction }) {
  return (
    <View style={{ marginBottom: spacing.md }}>
      <View style={{ flexDirection: "row", justifyContent: "space-between", alignItems: "center" }}>
        <Text style={{ fontSize: typography.section, fontWeight: "800", color: colors.text }}>{title}</Text>
        {actionLabel && onAction ? (
          <TouchableOpacity onPress={onAction}>
            <Text style={{ color: colors.accent, fontWeight: "700" }}>{actionLabel}</Text>
          </TouchableOpacity>
        ) : null}
      </View>
      {subtitle ? <Text style={{ color: colors.textMuted, marginTop: spacing.xs }}>{subtitle}</Text> : null}
    </View>
  );
}

export function FormLabel({ children }) {
  return <Text style={{ color: colors.text, fontWeight: "700", marginBottom: spacing.xs }}>{children}</Text>;
}

export function AppInput({ value, onChangeText, placeholder, keyboardType, secureTextEntry, autoCapitalize = "sentences", multiline = false, style }) {
  return (
    <TextInput
      value={value}
      onChangeText={onChangeText}
      placeholder={placeholder}
      placeholderTextColor="#8f8477"
      keyboardType={keyboardType}
      secureTextEntry={secureTextEntry}
      autoCapitalize={autoCapitalize}
      multiline={multiline}
      style={[
        {
          backgroundColor: colors.background,
          borderWidth: 1,
          borderColor: colors.border,
          borderRadius: radii.md,
          paddingHorizontal: spacing.md,
          paddingVertical: 14,
          color: colors.text,
          marginBottom: spacing.sm
        },
        multiline ? { minHeight: 96, textAlignVertical: "top" } : null,
        style
      ]}
    />
  );
}

export function PrimaryButton({ label, onPress, disabled, tone = "accent" }) {
  const backgroundColor =
    tone === "secondary" ? colors.surface :
      tone === "danger" ? colors.danger :
        colors.accent;

  return (
    <TouchableOpacity
      onPress={onPress}
      disabled={disabled}
      style={{
        backgroundColor: disabled ? "#d7cfc2" : backgroundColor,
        borderRadius: radii.md,
        borderWidth: tone === "secondary" ? 1 : 0,
        borderColor: colors.border,
        alignItems: "center",
        paddingVertical: 14,
        marginBottom: spacing.sm
      }}
    >
      <Text style={{ color: tone === "secondary" ? colors.text : colors.textOnDark, fontWeight: "800" }}>
        {label}
      </Text>
    </TouchableOpacity>
  );
}

export function Chip({ label, selected, onPress }) {
  return (
    <TouchableOpacity
      onPress={onPress}
      style={{
        marginRight: spacing.sm,
        marginBottom: spacing.sm,
        paddingHorizontal: spacing.md,
        paddingVertical: 10,
        borderRadius: radii.pill,
        borderWidth: 1,
        borderColor: selected ? colors.accent : colors.border,
        backgroundColor: selected ? colors.accentSoft : colors.surface
      }}
    >
      <Text style={{ color: colors.text, fontWeight: selected ? "700" : "600" }}>{label}</Text>
    </TouchableOpacity>
  );
}

export function InfoBanner({ text, tone = "neutral" }) {
  const palette =
    tone === "danger"
      ? { bg: "#fde7e2", border: "#efc2b8", text: colors.danger }
      : tone === "success"
        ? { bg: "#e5f5ed", border: "#badcc9", text: colors.success }
        : { bg: colors.accentSoft, border: "#efc4ad", text: colors.text };

  return (
    <View
      style={{
        backgroundColor: palette.bg,
        borderRadius: radii.md,
        padding: spacing.md,
        borderWidth: 1,
        borderColor: palette.border,
        marginBottom: spacing.md
      }}
    >
      <Text style={{ color: palette.text }}>{text}</Text>
    </View>
  );
}

export function LoadingBlock({ label }) {
  return (
    <View style={{ alignItems: "center", paddingVertical: spacing.md }}>
      <ActivityIndicator color={colors.accent} />
      {label ? <Text style={{ color: colors.textMuted, marginTop: spacing.sm }}>{label}</Text> : null}
    </View>
  );
}

export function ListRow({ title, subtitle, meta, children }) {
  return (
    <View
      style={{
        backgroundColor: colors.surface,
        borderRadius: radii.md,
        padding: spacing.md,
        borderWidth: 1,
        borderColor: colors.border,
        marginBottom: spacing.sm
      }}
    >
      <Text style={{ color: colors.text, fontWeight: "800", marginBottom: spacing.xs }}>{title}</Text>
      {subtitle ? <Text style={{ color: colors.textMuted, marginBottom: spacing.xs }}>{subtitle}</Text> : null}
      {meta ? <Text style={{ color: colors.textMuted, fontSize: typography.micro, marginBottom: spacing.sm }}>{meta}</Text> : null}
      {children}
    </View>
  );
}
