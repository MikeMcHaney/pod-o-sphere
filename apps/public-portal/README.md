# Public Portal

The public portal is the audience-facing branded show experience. It runs as a Next.js app on port `3002`.

## Run Locally

From the repository root:

```bash
npm install
npm run dev --workspace @pod-o-sphere/public-portal
```

Open `http://localhost:3002`.

For a production-style local build:

```bash
npm run build --workspace @pod-o-sphere/public-portal
npm run start --workspace @pod-o-sphere/public-portal
```

## Local Config

The current shell does not require app-specific environment variables. As search, published show lookup, and branding APIs come online, this app should use the API base URL from the shared environment shape:

```bash
NEXT_PUBLIC_API_URL=http://localhost:5000
```

## Checks

```bash
npm run typecheck --workspace @pod-o-sphere/public-portal
npm run build --workspace @pod-o-sphere/public-portal
```

