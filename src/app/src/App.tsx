import { useEffect, useState, type ReactNode } from "react";
import { Navigate, Route, Routes, useLocation } from "react-router-dom";
import { AppLayout } from "./layouts/AppLayout";
import { ThemeLayout } from "./layouts/ThemeLayout";
import { Login } from "./pages/Login";
import { AppThemeProvider } from "./context/AppThemeContext";
import { useAuth } from "./context/AuthContext";
import { platformModules } from "./modules";
import { getModuleRoutes } from "./platform/moduleRegistry";
import { themePages } from "./theme/config/themePages";

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
  const appRoutes = getModuleRoutes(platformModules);

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
        <Route
          element={
            <RequireAuth>
              <AppLayout theme={theme} onThemeToggle={toggleTheme} />
            </RequireAuth>
          }
        >
          {appRoutes.map((route) =>
            route.index ? (
              <Route index element={route.element} key="index" />
            ) : (
              <Route path={route.path} element={route.element} key={route.path} />
            )
          )}
        </Route>
        <Route path="/theme" element={<ThemeLayout theme={theme} />}>
          {themePages.map((page) =>
            page.index ? (
              <Route index element={page.element} key={page.path} />
            ) : (
              <Route path={page.routePath} element={page.element} key={page.path} />
            )
          )}
        </Route>
        <Route path="*" element={<Navigate replace to="/" />} />
      </Routes>
    </AppThemeProvider>
  );
}

function RequireAuth({ children }: { children: ReactNode }) {
  const { status } = useAuth();
  const location = useLocation();

  if (status === "loading") {
    return (
      <main className="grid min-h-screen place-items-center bg-background px-4 text-sm font-semibold text-muted-foreground">
        Loading...
      </main>
    );
  }

  if (status === "anonymous") {
    return <Navigate replace to="/login" state={{ from: location }} />;
  }

  return children;
}

export default App;
