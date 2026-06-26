"use client";

import { InteractionRequiredAuthError } from "@azure/msal-browser";
import { useIsAuthenticated, useMsal } from "@azure/msal-react";
import { useCallback, useEffect, useState } from "react";
import { apiScope, apiUrl, authConfigurationError } from "./auth-config";

const navItems = ["Overview", "Shows", "Sources", "Branding", "Jobs"];

type CurrentUser = {
  subject: string;
  issuer: string;
  name: string | null;
  email: string | null;
};

export default function AdminHome() {
  const { instance, accounts, inProgress } = useMsal();
  const isAuthenticated = useIsAuthenticated();
  const [currentUser, setCurrentUser] = useState<CurrentUser | null>(null);
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);
  const [hasMounted, setHasMounted] = useState(false);

  useEffect(() => {
    setHasMounted(true);
  }, []);

  const callApi = useCallback(async () => {
    const account = accounts[0];
    if (!account || !apiScope) return;

    setLoading(true);
    setError("");
    try {
      const token = await instance.acquireTokenSilent({ account, scopes: [apiScope] });
      const response = await fetch(`${apiUrl}/api/me`, {
        headers: { Authorization: `Bearer ${token.accessToken}` },
      });
      if (!response.ok) throw new Error(`API returned ${response.status} ${response.statusText}`);
      setCurrentUser(await response.json() as CurrentUser);
    } catch (requestError) {
      if (requestError instanceof InteractionRequiredAuthError) {
        await instance.acquireTokenRedirect({ account, scopes: [apiScope] });
        return;
      }
      setError(requestError instanceof Error ? requestError.message : "Authentication failed.");
    } finally {
      setLoading(false);
    }
  }, [accounts, instance]);

  useEffect(() => {
    if (isAuthenticated && inProgress === "none" && !currentUser && !loading && !error) void callApi();
  }, [callApi, currentUser, error, inProgress, isAuthenticated, loading]);

  async function signIn() {
    if (authConfigurationError) return;
    setError("");
    try {
      await instance.loginRedirect({ scopes: ["openid", "profile", "email", apiScope] });
    } catch (signInError) {
      setError(signInError instanceof Error ? signInError.message : "Microsoft sign-in could not be started.");
    }
  }

  async function signOut() {
    setCurrentUser(null);
    await instance.logoutRedirect({ account: accounts[0] });
  }

  const signInDisabled = !hasMounted || Boolean(authConfigurationError) || inProgress !== "none";

  return (
    <main>
      <aside><div className="brand">Pod-o-Sphere</div><p>Client Admin</p>{navItems.map((item) => <a href="#" key={item}>{item}</a>)}</aside>
      <section>
        <header>
          <div><p className="eyebrow">Workspace</p><h1>{currentUser?.name ? `Welcome, ${currentUser.name}` : "Welcome"}</h1></div>
          {isAuthenticated
            ? <button type="button" className="secondary" onClick={signOut}>Sign out</button>
            : <button type="button" onClick={signIn} disabled={signInDisabled}>Sign in with Microsoft</button>}
        </header>
        {authConfigurationError && <div className="notice error">Missing admin portal configuration: {authConfigurationError}</div>}
        {error && <div className="notice error">{error} {isAuthenticated && <button type="button" className="inline" onClick={callApi}>Retry API</button>}</div>}
        {!authConfigurationError && !isAuthenticated && <div className="notice">Sign in to establish your External ID session and test the protected API.</div>}
        {isAuthenticated && !currentUser && <div className="notice">{loading ? "Validating the API access token..." : "Signed in. Waiting for the API identity response."}</div>}
        {currentUser && <div className="notice success">Authenticated API identity: {currentUser.email ?? currentUser.subject}</div>}
        <div className="cards">
          <article><span>Shows</span><strong>0</strong><p>Create your first show after authentication is connected.</p></article>
          <article><span>Processing jobs</span><strong>0</strong><p>Onboarding and enrichment activity will appear here.</p></article>
          <article><span>Portal status</span><strong>Draft</strong><p>Your branded public portal is not published yet.</p></article>
        </div>
      </section>
    </main>
  );
}
