import { createContext, useContext, useEffect, useState } from "react";
import * as api from "./api";

const AuthContext = createContext(null);

export function AuthProvider({ children }) {
  const [user, setUser] = useState(() => {
    try { return JSON.parse(localStorage.getItem("user")) || null; } catch { return null; }
  });

  useEffect(() => {
    if (user) localStorage.setItem("user", JSON.stringify(user));
    else localStorage.removeItem("user");
  }, [user]);

  function loginUser(data) {
    localStorage.setItem("token", data.token);
    localStorage.setItem("refreshToken", data.refreshToken);
    setUser(data.user);
  }

  function updateUser(data) { setUser(data); }

  async function logout() {
    const refreshToken = localStorage.getItem("refreshToken");
    try { if (refreshToken) await api.logout(refreshToken); } catch { /* local logout still succeeds */ }
    localStorage.removeItem("token");
    localStorage.removeItem("refreshToken");
    setUser(null);
  }

  return <AuthContext.Provider value={{ user, loginUser, updateUser, logout }}>{children}</AuthContext.Provider>;
}

export const useAuth = () => useContext(AuthContext);
