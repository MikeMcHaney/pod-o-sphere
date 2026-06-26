# Sales Site

The sales site is the public brochure and conversion surface for Pod-o-Sphere. It runs as a Next.js app on port `3000`.

## Run Locally

From the repository root:

```bash
npm install
npm run dev --workspace @pod-o-sphere/sales-site
```

Open `http://localhost:3000`.

For a production-style local build:

```bash
npm run build --workspace @pod-o-sphere/sales-site
npm run start --workspace @pod-o-sphere/sales-site
```

## Local Config

The current shell does not require app-specific environment variables. Future trial/signup flows will likely call the API through:

```bash
NEXT_PUBLIC_API_URL=http://localhost:5000
```

## Checks

```bash
npm run typecheck --workspace @pod-o-sphere/sales-site
npm run build --workspace @pod-o-sphere/sales-site
```

