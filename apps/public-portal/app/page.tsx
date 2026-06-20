const topics = ["Creative practice", "Independent media", "Audience growth"];

export default function PortalHome() {
  return (
    <main>
      <header><div className="mark">PS</div><div><p>Pod-o-Sphere presents</p><h1>The Demo Show Archive</h1></div></header>
      <section className="hero">
        <p className="eyebrow">Explore every conversation</p>
        <h2>What do you want to learn?</h2>
        <form><label className="sr-only" htmlFor="search">Search the archive</label><input id="search" placeholder="Search episodes, topics, guests, and moments" /><button type="submit">Search</button></form>
      </section>
      <section className="content"><div><h3>Featured topics</h3><div className="topics">{topics.map((topic) => <a href="#" key={topic}>{topic}</a>)}</div></div><aside><h3>Recent episodes</h3><p>Processed episodes will appear here when a show is connected and published.</p></aside></section>
    </main>
  );
}

