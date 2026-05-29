import { lazy, useEffect, useState, type ReactNode } from "react";
import { Navigate, Route, Routes, useLocation } from "react-router-dom";
import { AppLayout } from "./layouts/AppLayout";
import { ThemeLayout } from "./layouts/ThemeLayout";
import { ForgotPassword } from "./pages/ForgotPassword";
import { Login } from "./pages/Login";
import { ResetPassword } from "./pages/ResetPassword";
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
        <Route path="/forgot-password" element={<ForgotPassword />} />
        <Route path="/reset-password" element={<ResetPassword />} />
        <Route
          element={
            <RequireAuth>
              <AppLayout theme={theme} onThemeToggle={toggleTheme} />
            </RequireAuth>
          }
        >
          {appRoutes.map((route) => {
            const element = route.permission ? <RequirePermission permission={route.permission}>{route.element}</RequirePermission> : route.element;

            return route.index ? (
              <Route index element={element} key="index" />
            ) : (
              <Route path={route.path} element={element} key={route.path} />
            );
          })}
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

function RequirePermission({ children, permission }: { children: ReactNode; permission: string }) {
  const { user } = useAuth();

  if (!user?.permissions.includes(permission)) {
    return (
      <main className="grid min-h-[50vh] place-items-center px-4">
        <div className="max-w-md rounded-xl border border-border bg-card p-6 text-center shadow-soft">
          <h1 className="text-xl font-bold text-foreground">Access denied</h1>
          <p className="mt-2 text-sm leading-6 text-muted-foreground">Your role does not have permission to view this page.</p>
        </div>
      </main>
    );
  }

  return children;
}

export default App;
