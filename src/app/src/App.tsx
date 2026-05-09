import { useEffect, useState } from "react";
import { Navigate, Route, Routes } from "react-router-dom";
import { AppLayout } from "./layouts/AppLayout";
import { ThemeLayout } from "./layouts/ThemeLayout";
import { Dashboard } from "./pages/Dashboard";
import { Login } from "./pages/Login";
import { Profile } from "./pages/Profile";
import { Reports } from "./pages/Reports";
import { Settings } from "./pages/Settings";
import { Users } from "./pages/Users";
import { ThemeComponents } from "./theme/pages/ThemeComponents";
import { ThemeDashboard } from "./theme/pages/ThemeDashboard";
import { ThemeLayouts } from "./theme/pages/ThemeLayouts";
import { ThemeLogin } from "./theme/pages/ThemeLogin";
import { ThemeProfile } from "./theme/pages/ThemeProfile";
import { ThemeReports } from "./theme/pages/ThemeReports";
import { ThemeSettings } from "./theme/pages/ThemeSettings";
import { ThemeUsers } from "./theme/pages/ThemeUsers";
import { AppThemeProvider } from "./context/AppThemeContext";

type Theme = "light" | "dark";

function getInitialTheme(): Theme {
  const savedTheme = localStorage.getItem("obp-theme");

  if (savedTheme === "light" || savedTheme === "dark") {
    return savedTheme;
  }

  return window.matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light";
}

function App() {
  const [theme, setTheme] = useState<Theme>(getInitialTheme);

  useEffect(() => {
    document.documentElement.classList.toggle("dark", theme === "dark");
    localStorage.setItem("obp-theme", theme);
  }, [theme]);

  function toggleTheme() {
    setTheme((currentTheme) => (currentTheme === "dark" ? "light" : "dark"));
  }

  return (
    <AppThemeProvider>
      <Routes>
        <Route path="/login" element={<Login />} />
        <Route element={<AppLayout theme={theme} onThemeToggle={toggleTheme} />}>
          <Route index element={<Dashboard />} />
          <Route path="/dashboard" element={<Dashboard />} />
          <Route path="/users" element={<Users />} />
          <Route path="/reports" element={<Reports />} />
          <Route path="/settings" element={<Settings />} />
          <Route path="/profile" element={<Profile />} />
        </Route>
        <Route path="/theme" element={<ThemeLayout theme={theme} />}>
          <Route index element={<ThemeDashboard />} />
          <Route path="users" element={<ThemeUsers />} />
          <Route path="reports" element={<ThemeReports />} />
          <Route path="settings" element={<ThemeSettings />} />
          <Route path="profile" element={<ThemeProfile />} />
          <Route path="login" element={<ThemeLogin />} />
          <Route path="layouts" element={<ThemeLayouts />} />
          <Route path="components" element={<ThemeComponents />} />
        </Route>
        <Route path="*" element={<Navigate replace to="/" />} />
      </Routes>
    </AppThemeProvider>
  );
}

export default App;
