const navItems = ["Overview", "Shows", "Sources", "Branding", "Jobs"];

export default function AdminHome() {
  return (
    <main>
      <aside><div className="brand">Pod-o-Sphere</div><p>Client Admin</p>{navItems.map((item) => <a href="#" key={item}>{item}</a>)}</aside>
      <section>
        <header><div><p className="eyebrow">Workspace</p><h1>Welcome back</h1></div><button type="button">Sign in with Entra ID</button></header>
        <div className="notice">Phase 0 shell: identity, tenant context, and live data arrive in Phase 1.</div>
        <div className="cards">
          <article><span>Shows</span><strong>0</strong><p>Create your first show after authentication is connected.</p></article>
          <article><span>Processing jobs</span><strong>0</strong><p>Onboarding and enrichment activity will appear here.</p></article>
          <article><span>Portal status</span><strong>Draft</strong><p>Your branded public portal is not published yet.</p></article>
        </div>
      </section>
    </main>
  );
}

