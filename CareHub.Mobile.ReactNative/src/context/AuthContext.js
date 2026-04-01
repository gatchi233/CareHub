import AsyncStorage from "@react-native-async-storage/async-storage";
import React, { createContext, useContext, useEffect, useMemo, useState } from "react";
import {
  API_BASE_URL_STORAGE_KEY,
  login as loginApi,
  me as meApi,
  setApiBaseUrl
} from "../services/apiClient";

const AuthContext = createContext(null);
const AUTH_STORAGE_KEY = "carehub_mobile_auth";

export function AuthProvider({ children }) {
  const [token, setToken] = useState("");
  const [user, setUser] = useState(null);
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);
  const [initializing, setInitializing] = useState(true);

  const isAuthenticated = Boolean(token && user);

  useEffect(() => {
    let mounted = true;

    async function restoreSession() {
      try {
        const savedApiBaseUrl = await AsyncStorage.getItem(API_BASE_URL_STORAGE_KEY);
        if (savedApiBaseUrl) {
          setApiBaseUrl(savedApiBaseUrl);
        }

        const raw = await AsyncStorage.getItem(AUTH_STORAGE_KEY);
        if (!raw) {
          return;
        }

        const saved = JSON.parse(raw);
        const savedToken = saved?.token || "";
        const savedUser = saved?.user || null;

        if (mounted && savedToken && savedUser) {
          setToken(savedToken);
          setUser(savedUser);
        }
      } catch {
        if (mounted) {
          setToken("");
          setUser(null);
        }
      } finally {
        if (mounted) {
          setInitializing(false);
        }
      }
    }

    restoreSession();

    return () => {
      mounted = false;
    };
  }, []);

  async function login(username, password) {
    setLoading(true);
    setError("");
    try {
      const login = await loginApi(username, password);
      const accessToken = login?.accessToken || "";
      if (!accessToken) {
        throw new Error("No access token returned.");
      }

      const profile = await meApi(accessToken);
      const role = profile?.role || "";

      // Role matrix: Admin cannot use mobile platform.
      if (role === "Admin") {
        throw new Error("Admin role is not allowed on mobile.");
      }

      setToken(accessToken);
      setUser(profile);
      await AsyncStorage.setItem(
        AUTH_STORAGE_KEY,
        JSON.stringify({ token: accessToken, user: profile })
      );
      return true;
    } catch (err) {
      setToken("");
      setUser(null);
      await AsyncStorage.removeItem(AUTH_STORAGE_KEY);
      setError(err?.message || "Login failed.");
      return false;
    } finally {
      setLoading(false);
    }
  }

  async function logout() {
    setToken("");
    setUser(null);
    setError("");
    await AsyncStorage.removeItem(AUTH_STORAGE_KEY);
  }

  const value = useMemo(
    () => ({
      token,
      user,
      error,
      loading,
      initializing,
      isAuthenticated,
      login,
      logout
    }),
    [token, user, error, loading, initializing, isAuthenticated]
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  return useContext(AuthContext);
}
