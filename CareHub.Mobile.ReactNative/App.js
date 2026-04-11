import React from "react";
import { ActivityIndicator, Image, View } from "react-native";
import { NavigationContainer } from "@react-navigation/native";
import { createNativeStackNavigator } from "@react-navigation/native-stack";
import { createBottomTabNavigator } from "@react-navigation/bottom-tabs";
import { AuthProvider, useAuth } from "./src/context/AuthContext";
import LoginScreen from "./src/screens/LoginScreen";
import DashboardScreen from "./src/screens/DashboardScreen";
import ResidentsScreen from "./src/screens/ResidentsScreen";
import ObservationsScreen from "./src/screens/ObservationsScreen";
import MedicationsScreen from "./src/screens/MedicationsScreen";
import MarScreen from "./src/screens/MarScreen";
import OrdersScreen from "./src/screens/OrdersScreen";
import AiScreen from "./src/screens/AiScreen";
import { colors, radii } from "./src/ui/theme";

const Stack = createNativeStackNavigator();
const Tabs = createBottomTabNavigator();

const TAB_ICONS = {
  Dashboard: require("./src/assets/icons/home.png"),
  Residents: require("./src/assets/icons/residence.png"),
  Observations: require("./src/assets/icons/observation.png"),
  Medications: require("./src/assets/icons/medication.png"),
  MAR: require("./src/assets/icons/medical-record.png"),
  Orders: require("./src/assets/icons/orders.png"),
  AI: require("./src/assets/icons/ai-assistant.png")
};

function AppTabs() {
  const { user } = useAuth();
  const role = user?.role || "";

  const canSeeResidents = role === "Nurse" || role === "General CareStaff";
  const canSeeObservations =
    role === "Nurse" || role === "General CareStaff" || role === "Observer";
  const canSeeMedications = role === "Nurse" || role === "Observer";
  const canSeeMar = role === "Nurse";
  const canSeeOrders = role === "Nurse";
  const canSeeAi = role === "Nurse";

  return (
    <Tabs.Navigator
      screenOptions={({ route }) => ({
        headerShown: false,
        tabBarShowLabel: false,
        tabBarActiveTintColor: colors.accent,
        tabBarInactiveTintColor: "#8a8176",
        tabBarStyle: {
          backgroundColor: colors.surface,
          borderTopColor: colors.border,
          paddingTop: 8,
          paddingBottom: 8,
          paddingHorizontal: 6,
          height: 68,
          borderTopLeftRadius: radii.lg,
          borderTopRightRadius: radii.lg
        },
        tabBarItemStyle: {
          marginHorizontal: 2,
          paddingVertical: 6,
          borderRadius: radii.md
        },
        tabBarIcon: ({ route, focused }) => {
          const icon = TAB_ICONS[route.name] || TAB_ICONS.Dashboard;
          return (
            <View
              style={{
                width: 46,
                height: 42,
                borderRadius: radii.md,
                alignItems: "center",
                justifyContent: "center",
                backgroundColor: focused ? colors.accentSoft : "transparent"
              }}
            >
              <Image
                source={icon}
                resizeMode="contain"
                style={{
                  width: focused ? 30 : 27,
                  height: focused ? 30 : 27,
                  opacity: focused ? 1 : 0.62
                }}
              />
            </View>
          );
        },
        sceneStyle: {
          backgroundColor: colors.background
        }
      })}
    >
      <Tabs.Screen name="Dashboard" component={DashboardScreen} />
      {canSeeResidents ? (
        <Tabs.Screen name="Residents" component={ResidentsScreen} />
      ) : null}
      {canSeeObservations ? (
        <Tabs.Screen name="Observations" component={ObservationsScreen} />
      ) : null}
      {canSeeMedications ? (
        <Tabs.Screen name="Medications" component={MedicationsScreen} />
      ) : null}
      {canSeeMar ? <Tabs.Screen name="MAR" component={MarScreen} /> : null}
      {canSeeOrders ? <Tabs.Screen name="Orders" component={OrdersScreen} /> : null}
      {canSeeAi ? <Tabs.Screen name="AI" component={AiScreen} /> : null}
    </Tabs.Navigator>
  );
}

function RootNavigator() {
  const { isAuthenticated, initializing } = useAuth();

  if (initializing) {
    return (
      <View
        style={{
          flex: 1,
          alignItems: "center",
          justifyContent: "center",
          backgroundColor: colors.background
        }}
      >
        <ActivityIndicator size="large" color={colors.accent} />
      </View>
    );
  }

  return (
    <NavigationContainer>
      <Stack.Navigator screenOptions={{ headerShown: false }}>
        {isAuthenticated ? (
          <Stack.Screen name="AppTabs" component={AppTabs} />
        ) : (
          <Stack.Screen name="Login" component={LoginScreen} />
        )}
      </Stack.Navigator>
    </NavigationContainer>
  );
}

export default function App() {
  return (
    <AuthProvider>
      <RootNavigator />
    </AuthProvider>
  );
}
