"use client";

import { MsalProvider } from "@azure/msal-react";
import { msalInstance } from "./auth-config";

export function AuthProvider({ children }: Readonly<{ children: React.ReactNode }>) {
  return <MsalProvider instance={msalInstance}>{children}</MsalProvider>;
}
