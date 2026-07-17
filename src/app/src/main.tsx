import React from "react";
import ReactDOM from "react-dom/client";
import { BrowserRouter } from "react-router-dom";
import App from "./App";
import { AuthProvider } from "./context/AuthContext";
import { WorkspaceBrandingProvider } from "./context/WorkspaceBrandingContext";
import "./styles.css";

ReactDOM.createRoot(document.getElementById("root")!).render(
  <React.StrictMode>
    <BrowserRouter>
      <AuthProvider>
        <WorkspaceBrandingProvider>
          <App />
        </WorkspaceBrandingProvider>
      </AuthProvider>
    </BrowserRouter>
  </React.StrictMode>
);
