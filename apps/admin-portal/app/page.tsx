"use client";

import { InteractionRequiredAuthError } from "@azure/msal-browser";
import { useIsAuthenticated, useMsal } from "@azure/msal-react";
import { FormEvent, useCallback, useEffect, useMemo, useState } from "react";
import { apiScope, apiUrl, authConfigurationError } from "./auth-config";

const navItems = ["Overview", "Tenants", "Invitations", "Show claims"];

type CurrentUser = {
  appUserId: string;
  subject: string;
  issuer: string;
  name: string | null;
  contactEmail: string | null;
  preferredUsername: string | null;
  isSuperAdmin: boolean;
  roles: string[];
  platformRoles: string[];
  tenantMemberships: TenantMembership[];
};

type TenantMembership = {
  tenantId: string;
  tenantName: string;
  tenantSlug: string;
  roleName: string;
  isActive: boolean;
};

type AdminTenant = {
  tenantId: string;
  tenantName: string;
  slug: string;
  status: string;
  activeUserCount: number;
};

type ShowClaim = {
  showClaimId: string;
  showId: string | null;
  requestingAppUserId: string;
  reviewedByAppUserId: string | null;
  claimType: string;
  sourceUrl: string;
  status: string;
  notes: string | null;
  createdAtUtc: string;
  reviewedAtUtc: string | null;
};

type InvitationResponse = {
  invitationId: string;
  email: string;
  roleName: string;
  expiresAtUtc: string;
  token: string;
};

type InviteFormState = {
  tenantId: string;
  email: string;
  roleName: string;
  showId: string;
  expiresInDays: string;
};

const defaultInviteForm: InviteFormState = {
  tenantId: "",
  email: "",
  roleName: "TenantAdmin",
  showId: "",
  expiresInDays: "14",
};

export default function AdminHome() {
  const { instance, accounts, inProgress } = useMsal();
  const isAuthenticated = useIsAuthenticated();
  const [currentUser, setCurrentUser] = useState<CurrentUser | null>(null);
  const [tenants, setTenants] = useState<AdminTenant[]>([]);
  const [showClaims, setShowClaims] = useState<ShowClaim[]>([]);
  const [inviteForm, setInviteForm] = useState<InviteFormState>(defaultInviteForm);
  const [latestInvitation, setLatestInvitation] = useState<InvitationResponse | null>(null);
  const [error, setError] = useState("");
  const [adminError, setAdminError] = useState("");
  const [loadingUser, setLoadingUser] = useState(false);
  const [loadingAdmin, setLoadingAdmin] = useState(false);
  const [adminLoaded, setAdminLoaded] = useState(false);
  const [submittingInvite, setSubmittingInvite] = useState(false);
  const [reviewingClaimId, setReviewingClaimId] = useState<string | null>(null);
  const [hasMounted, setHasMounted] = useState(false);

  useEffect(() => {
    setHasMounted(true);
  }, []);

  const signInDisabled = !hasMounted || Boolean(authConfigurationError) || inProgress !== "none";
  const account = accounts[0];

  const acquireAccessToken = useCallback(async () => {
    if (!account || !apiScope) return null;

    const token = await instance.acquireTokenSilent({ account, scopes: [apiScope] });
    return token.accessToken;
  }, [account, instance]);

  const apiRequest = useCallback(async <T,>(path: string, init?: RequestInit): Promise<T> => {
    const accessToken = await acquireAccessToken();
    if (!accessToken) throw new Error("No signed-in account is available.");

    const response = await fetch(`${apiUrl}${path}`, {
      ...init,
      headers: {
        Authorization: `Bearer ${accessToken}`,
        ...(init?.body ? { "Content-Type": "application/json" } : {}),
        ...init?.headers,
      },
    });

    if (!response.ok) {
      const message = await response.text();
      throw new Error(message || `API returned ${response.status} ${response.statusText}`);
    }

    return await response.json() as T;
  }, [acquireAccessToken]);

  const loadCurrentUser = useCallback(async () => {
    if (!account || !apiScope) return;

    setLoadingUser(true);
    setError("");
    try {
      setCurrentUser(await apiRequest<CurrentUser>("/api/me"));
    } catch (requestError) {
      if (requestError instanceof InteractionRequiredAuthError) {
        await instance.acquireTokenRedirect({ account, scopes: [apiScope] });
        return;
      }
      setError(requestError instanceof Error ? requestError.message : "Authentication failed.");
    } finally {
      setLoadingUser(false);
    }
  }, [account, apiRequest, instance]);

  const loadAdminData = useCallback(async () => {
    setLoadingAdmin(true);
    setAdminError("");
    try {
      const [tenantResult, claimResult] = await Promise.all([
        apiRequest<AdminTenant[]>("/api/admin/tenants"),
        apiRequest<ShowClaim[]>("/api/admin/show-claims/pending"),
      ]);
      setTenants(tenantResult);
      setShowClaims(claimResult);
      setInviteForm((previous) => ({
        ...previous,
        tenantId: previous.tenantId || tenantResult[0]?.tenantId || "",
      }));
    } catch (requestError) {
      if (requestError instanceof InteractionRequiredAuthError && account) {
        await instance.acquireTokenRedirect({ account, scopes: [apiScope] });
        return;
      }
      setAdminError(requestError instanceof Error ? requestError.message : "Admin data could not be loaded.");
    } finally {
      setAdminLoaded(true);
      setLoadingAdmin(false);
    }
  }, [account, apiRequest, instance]);

  useEffect(() => {
    if (isAuthenticated && inProgress === "none" && !currentUser && !loadingUser && !error) void loadCurrentUser();
  }, [currentUser, error, inProgress, isAuthenticated, loadCurrentUser, loadingUser]);

  useEffect(() => {
    if (currentUser?.isSuperAdmin && !adminLoaded && !loadingAdmin) {
      void loadAdminData();
    }
  }, [adminLoaded, currentUser, loadAdminData, loadingAdmin]);

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
    setTenants([]);
    setShowClaims([]);
    setAdminLoaded(false);
    await instance.logoutRedirect({ account });
  }

  async function createInvitation(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setSubmittingInvite(true);
    setAdminError("");
    setLatestInvitation(null);

    const expiresInDays = Number.parseInt(inviteForm.expiresInDays, 10);
    const request = {
      email: inviteForm.email,
      roleName: inviteForm.roleName,
      showId: inviteForm.showId.trim() || null,
      expiresInDays: Number.isFinite(expiresInDays) ? expiresInDays : null,
    };

    try {
      const invitation = await apiRequest<InvitationResponse>(
        `/api/admin/tenants/${inviteForm.tenantId}/invitations`,
        { method: "POST", body: JSON.stringify(request) },
      );
      setLatestInvitation(invitation);
      setInviteForm((previous) => ({ ...previous, email: "", showId: "" }));
    } catch (requestError) {
      setAdminError(requestError instanceof Error ? requestError.message : "Invitation could not be created.");
    } finally {
      setSubmittingInvite(false);
    }
  }

  async function reviewShowClaim(showClaimId: string, action: "approve" | "reject") {
    setReviewingClaimId(showClaimId);
    setAdminError("");
    try {
      await apiRequest<ShowClaim>(
        `/api/admin/show-claims/${showClaimId}/${action}`,
        { method: "POST", body: JSON.stringify({}) },
      );
      setShowClaims((previous) => previous.filter((showClaim) => showClaim.showClaimId !== showClaimId));
    } catch (requestError) {
      setAdminError(requestError instanceof Error ? requestError.message : `Show claim could not be ${action}d.`);
    } finally {
      setReviewingClaimId(null);
    }
  }

  const activeTenant = useMemo(
    () => tenants.find((tenant) => tenant.tenantId === inviteForm.tenantId),
    [inviteForm.tenantId, tenants],
  );

  if (!hasMounted || !isAuthenticated) {
    return (
      <main className="auth-shell">
        <section className="auth-panel" aria-labelledby="signin-heading">
          <div className="brand">Pod-o-Sphere</div>
          <p className="eyebrow">Admin portal</p>
          <h1 id="signin-heading">Sign in to continue</h1>
          <p className="auth-copy">Use your authorized Microsoft External ID account to access tenant and onboarding operations.</p>
          {authConfigurationError && <div className="notice error">Missing admin portal configuration: {authConfigurationError}</div>}
          {error && <div className="notice error">{error}</div>}
          <button type="button" onClick={signIn} disabled={signInDisabled}>Sign in with Microsoft</button>
        </section>
      </main>
    );
  }

  return (
    <main className="admin-shell">
      <aside>
        <div className="brand">Pod-o-Sphere</div>
        <p>Admin</p>
        {navItems.map((item) => <a href="#" key={item}>{item}</a>)}
      </aside>
      <section className="workspace">
        <header>
          <div>
            <p className="eyebrow">Workspace</p>
            <h1>{currentUser?.name ? `Welcome, ${currentUser.name}` : "Welcome"}</h1>
          </div>
          <button type="button" className="secondary" onClick={signOut}>Sign out</button>
        </header>

        {error && <div className="notice error">{error} <button type="button" className="inline" onClick={loadCurrentUser}>Retry API</button></div>}
        {!currentUser && <div className="notice">{loadingUser ? "Validating the API access token..." : "Signed in. Waiting for the API identity response."}</div>}

        {currentUser && (
          <>
            <div className="notice success">
              <strong>{currentUser.isSuperAdmin ? "SuperAdmin" : "Authenticated"}</strong>
              <span>Contact: {currentUser.contactEmail ?? "not set"}</span>
            </div>

            <div className="identity-grid">
              <article>
                <span>App user</span>
                <strong>{currentUser.name ?? currentUser.contactEmail ?? "Registered"}</strong>
                <p>{currentUser.appUserId}</p>
              </article>
              <article>
                <span>Roles</span>
                <strong>{currentUser.roles.length ? currentUser.roles.join(", ") : "None"}</strong>
                <p>{currentUser.isSuperAdmin ? "Platform administration is enabled." : "No platform role is active."}</p>
              </article>
              <article>
                <span>Identity hint</span>
                <strong>{currentUser.preferredUsername ?? "Not provided"}</strong>
                <p>Token username hints stay separate from contact email.</p>
              </article>
            </div>

            {currentUser.tenantMemberships.length > 0 && (
              <div className="membership-list">
                {currentUser.tenantMemberships.map((membership) => (
                  <article key={`${membership.tenantId}-${membership.roleName}`}>
                    <span>{membership.roleName}</span>
                    <strong>{membership.tenantName}</strong>
                    <p>{membership.tenantSlug}</p>
                  </article>
                ))}
              </div>
            )}

            {currentUser.isSuperAdmin ? (
              <div className="admin-panels">
                <div className="section-header">
                  <div>
                    <p className="eyebrow">SuperAdmin</p>
                    <h2>Onboarding operations</h2>
                  </div>
                  <button type="button" className="secondary" onClick={loadAdminData} disabled={loadingAdmin}>
                    {loadingAdmin ? "Refreshing..." : "Refresh"}
                  </button>
                </div>

                {adminError && <div className="notice error">{adminError}</div>}

                <div className="cards">
                  <article>
                    <span>Tenants</span>
                    <strong>{tenants.length}</strong>
                    <p>Known metadata tenants available for invite workflows.</p>
                  </article>
                  <article>
                    <span>Pending claims</span>
                    <strong>{showClaims.length}</strong>
                    <p>Show ownership requests awaiting manual review.</p>
                  </article>
                  <article>
                    <span>Invite target</span>
                    <strong>{activeTenant?.tenantName ?? "None"}</strong>
                    <p>{activeTenant?.slug ?? "Create or select a tenant before inviting users."}</p>
                  </article>
                </div>

                <div className="ops-grid">
                  <article className="tool-panel">
                    <div className="panel-heading">
                      <span>Invitations</span>
                      <strong>Create tenant invite</strong>
                    </div>
                    <form onSubmit={createInvitation}>
                      <label>
                        Tenant
                        <select
                          value={inviteForm.tenantId}
                          onChange={(event) => setInviteForm((previous) => ({ ...previous, tenantId: event.target.value }))}
                          required
                        >
                          {tenants.map((tenant) => (
                            <option value={tenant.tenantId} key={tenant.tenantId}>{tenant.tenantName}</option>
                          ))}
                        </select>
                      </label>
                      <label>
                        Email
                        <input
                          value={inviteForm.email}
                          onChange={(event) => setInviteForm((previous) => ({ ...previous, email: event.target.value }))}
                          type="email"
                          required
                        />
                      </label>
                      <label>
                        Role
                        <select
                          value={inviteForm.roleName}
                          onChange={(event) => setInviteForm((previous) => ({ ...previous, roleName: event.target.value }))}
                        >
                          <option value="TenantOwner">TenantOwner</option>
                          <option value="TenantAdmin">TenantAdmin</option>
                        </select>
                      </label>
                      <div className="form-row">
                        <label>
                          Show ID
                          <input
                            value={inviteForm.showId}
                            onChange={(event) => setInviteForm((previous) => ({ ...previous, showId: event.target.value }))}
                            placeholder="Optional"
                          />
                        </label>
                        <label>
                          Days
                          <input
                            value={inviteForm.expiresInDays}
                            onChange={(event) => setInviteForm((previous) => ({ ...previous, expiresInDays: event.target.value }))}
                            inputMode="numeric"
                          />
                        </label>
                      </div>
                      <button type="submit" disabled={submittingInvite || tenants.length === 0}>
                        {submittingInvite ? "Creating..." : "Create invite"}
                      </button>
                    </form>
                    {latestInvitation && (
                      <div className="token-box">
                        <span>One-time invite token</span>
                        <code>{latestInvitation.token}</code>
                        <p>Expires {new Date(latestInvitation.expiresAtUtc).toLocaleString()}</p>
                      </div>
                    )}
                  </article>

                  <article className="tool-panel">
                    <div className="panel-heading">
                      <span>Show claims</span>
                      <strong>Pending review</strong>
                    </div>
                    {showClaims.length === 0 ? (
                      <p className="empty-state">No pending show claims.</p>
                    ) : (
                      <div className="claim-list">
                        {showClaims.map((showClaim) => (
                          <div className="claim-item" key={showClaim.showClaimId}>
                            <div>
                              <span>{showClaim.claimType}</span>
                              <strong>{showClaim.sourceUrl}</strong>
                              <p>{showClaim.notes ?? "No notes provided."}</p>
                              <small>{new Date(showClaim.createdAtUtc).toLocaleString()}</small>
                            </div>
                            <div className="claim-actions">
                              <button
                                type="button"
                                className="secondary"
                                onClick={() => reviewShowClaim(showClaim.showClaimId, "reject")}
                                disabled={reviewingClaimId === showClaim.showClaimId}
                              >
                                Reject
                              </button>
                              <button
                                type="button"
                                onClick={() => reviewShowClaim(showClaim.showClaimId, "approve")}
                                disabled={reviewingClaimId === showClaim.showClaimId}
                              >
                                Approve
                              </button>
                            </div>
                          </div>
                        ))}
                      </div>
                    )}
                  </article>
                </div>
              </div>
            ) : (
              <div className="notice">No SuperAdmin platform role is active for this account.</div>
            )}
          </>
        )}
      </section>
    </main>
  );
}
