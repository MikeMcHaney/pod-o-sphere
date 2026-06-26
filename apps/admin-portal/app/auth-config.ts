import { Configuration, LogLevel, PublicClientApplication } from "@azure/msal-browser";

const clientId = process.env.NEXT_PUBLIC_ENTRA_CLIENT_ID ?? "configuration-required";
const tenantSubdomain = process.env.NEXT_PUBLIC_ENTRA_TENANT_SUBDOMAIN ?? "";
const tenantId = process.env.NEXT_PUBLIC_ENTRA_TENANT_ID ?? "";

export const apiScope = process.env.NEXT_PUBLIC_ENTRA_API_SCOPE ?? "";
export const apiUrl = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5000";

export const authConfigurationError = [
  !process.env.NEXT_PUBLIC_ENTRA_CLIENT_ID && "NEXT_PUBLIC_ENTRA_CLIENT_ID",
  !tenantSubdomain && "NEXT_PUBLIC_ENTRA_TENANT_SUBDOMAIN",
  !tenantId && "NEXT_PUBLIC_ENTRA_TENANT_ID",
  !apiScope && "NEXT_PUBLIC_ENTRA_API_SCOPE",
].filter(Boolean).join(", ");

const tenantHost = `${tenantSubdomain || "configuration-required"}.ciamlogin.com`;
const issuerHost = tenantId ? `${tenantId}.ciamlogin.com` : "configuration-required.ciamlogin.com";
const tenantDomain = `${tenantSubdomain || "configuration-required"}.onmicrosoft.com`;
const configuration: Configuration = {
  auth: {
    clientId,
    authority: `https://${tenantHost}/${tenantDomain}/v2.0`,
    knownAuthorities: [tenantHost, issuerHost],
    redirectUri: typeof window === "undefined" ? "http://localhost:3001" : window.location.origin,
    postLogoutRedirectUri: typeof window === "undefined" ? "http://localhost:3001" : window.location.origin,
  },
  cache: {
    cacheLocation: "sessionStorage",
  },
  system: {
    loggerOptions: {
      logLevel: LogLevel.Warning,
      piiLoggingEnabled: false,
    },
  },
};

export const msalInstance = new PublicClientApplication(configuration);
