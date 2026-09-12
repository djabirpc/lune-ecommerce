# Deploying Luna to Railway

Railway hosts this as **three services in one project**: a managed PostgreSQL plugin, the .NET
backend (Docker), and the React frontend (Docker, built to a static bundle and served by nginx).
Unlike the old Render Blueprint (`render.yaml`, removed), Railway is configured mostly through its
dashboard — `backend/railway.json` and `frontend/railway.json` each declare *how to build* that
service (Dockerfile + health check), but environment variables and volumes are set per-service in
the dashboard, not committed to the repo.

Railway's exact UI/field names can shift over time — if a step below doesn't match what you see,
cross-check against [Railway's current docs](https://docs.railway.com/) before assuming the app
itself is broken.

## 0. Prerequisites

- A Railway account, with this GitHub repo accessible to it (Railway → New Project → Deploy from
  GitHub repo — authorize access to `djabirpc/lune-ecommerce` if asked).
- Decide which branch Railway should deploy (`main` for anything you'd call "live"; `dev` if this
  is genuinely just for testing, per CLAUDE.md's dev/prod branch split).

## 1. Create the project and add PostgreSQL

1. Railway dashboard → **New Project** → **Provision PostgreSQL**. This gives you a `Postgres`
   service with its own persistent storage already handled by Railway — no manual volume needed
   for the database itself.
2. Note the service name (default `Postgres`) — you'll reference it as `${{Postgres.DATABASE_URL}}`
   from the backend.

## 2. Add the backend service

1. In the same project: **New** → **GitHub Repo** → select this repo.
2. **Settings → Source**: set **Root Directory** to `backend`. Railway will pick up
   `backend/railway.json` automatically (Dockerfile build, `/health` healthcheck path).
3. **Settings → Networking**: click **Generate Domain** to get a public
   `*.up.railway.app` URL. Copy it — you need it in step 4 and step 6.
4. **Variables**, add each of the following (matches `.env.example`'s backend section 1:1):

   | Variable | Value |
   |---|---|
   | `ASPNETCORE_ENVIRONMENT` | `Production` |
   | `ApplyMigrationsOnStartup` | `true` |
   | `ConnectionStrings__DefaultConnection` | `${{Postgres.DATABASE_URL}}` |
   | `Jwt__Issuer` | `Luna.Api` |
   | `Jwt__Audience` | `Luna.Client` |
   | `Jwt__Key` | a long random secret — generate one locally with `openssl rand -base64 48`, never reuse the local dev value |
   | `Jwt__AccessTokenExpiryMinutes` | `60` |
   | `Jwt__RefreshTokenExpiryDays` | `7` |
   | `Cors__AllowedOrigins__0` | leave blank for now — set in step 6, once the frontend's URL exists |
   | `InitialAdmin__Email` | `admin@luna.local` |
   | `InitialAdmin__Password` | `Papamama-123` (test credential — see the security note at the bottom) |
   | `InitialAdmin__FirstName` | `Luna` |
   | `InitialAdmin__LastName` | `Admin` |
   | `ShippingSync__Enabled` | `true` |
   | `ShippingSync__IntervalSeconds` | `300` |
   | `FileStorage__LocalPath` | `/app/uploads` |
   | `FileStorage__PublicBaseUrl` | the backend's own domain from step 3 (e.g. `https://luna-backend-production.up.railway.app`) — this gets baked into every uploaded image's URL |
   | `Yalidine__BaseUrl` / `Yalidine__ApiId` / `Yalidine__ApiToken` | leave blank — no real Yalidine credentials exist yet (CLAUDE.md section 16); the adapter stays structural-only regardless |
   | `ZRExpress__BaseUrl` / `ZRExpress__ApiKey` | leave blank, same reason |

   `ASPNETCORE_URLS=http://+:8080` is already baked into `backend/Dockerfile` — you don't need to
   set it again here.

5. Deploy. Watch the build logs; once healthy, hit `https://<your-backend-domain>/health` — it
   should return healthy JSON. Migrations and the SUPER_ADMIN seed both run automatically on first
   boot (`ApplyMigrationsOnStartup=true` + `InitialAdmin__*` above).

## 3. Add a volume for uploaded images

Product photos and homepage banners are written to local disk (`IFileStorageService`,
CLAUDE.md section 28) — without a volume, every redeploy wipes them.

1. Backend service → **Settings → Volumes** → **New Volume**.
2. **Mount path**: `/app/uploads` (must match `FileStorage__LocalPath` above exactly).
3. Redeploy the backend once the volume is attached.

This is the fix for the exact caveat the old Render free-tier setup had (no persistent disk on the
free plan) — uploaded images now survive redeploys and restarts.

## 4. Add the frontend service

1. Same project: **New** → **GitHub Repo** → this repo again.
2. **Settings → Source**: **Root Directory** = `frontend`. Railway should pick up
   `frontend/railway.json`, which builds `frontend/Dockerfile` — the production nginx build (this
   is deliberately the file literally named `Dockerfile`, since that's what Railway defaults to
   without any config at all; local dev's docker-compose is the one pointed at the differently-named
   `Dockerfile.dev` instead, see the comment at the top of `frontend/Dockerfile`).
3. **Verify the build settings actually took** — this is the step that broke the first time: open
   **Settings → Build**, and confirm **Dockerfile Path** reads `Dockerfile` (not `Dockerfile.dev`,
   not blank). If the service was created before `frontend/railway.json` existed, Railway may have
   already locked in a build method at creation time and not re-read the config file — set it
   explicitly here if so, then redeploy.
4. **Settings → Networking** → **Generate Domain**. Also check **Target Port** is `8080` (matches
   the nginx `EXPOSE 8080` in the Dockerfile) — if this field is blank or set to something else
   (5173 is the tell-tale sign of the dev image), fix it here.
5. **Variables** — these are Vite `VITE_*` build-time values, baked into the JS bundle when the
   image is built (not read at runtime). Railway passes service Variables as Docker build args
   automatically for Dockerfile builds; if a build doesn't seem to pick one up, check Railway's
   current docs on "Build Variables"/build-arg behavior — this has been known to change.

   | Variable | Value |
   |---|---|
   | `VITE_API_URL` | the backend's public domain from step 2.3 |
   | `VITE_META_PIXEL_ID` | blank unless you have a real Meta Pixel ID |
   | `VITE_TIKTOK_PIXEL_ID` | blank unless you have a real TikTok Pixel ID |
   | `VITE_INSTAGRAM_URL` / `VITE_TIKTOK_URL` / `VITE_FACEBOOK_URL` / `VITE_STORE_PHONE` | optional — blank hides those footer links entirely rather than pointing at a placeholder |

6. Deploy. Check the build logs for `nginx` starting up near the end — if you instead see `VITE
   ready in`/`Local: http://localhost:5173`, the service is still building the dev image; go back
   to step 3.

## 5. Close the loop (CORS + image URLs)

Same chicken-and-egg problem the old Render setup had — both services' URLs only exist *after*
they've been created once:

1. Copy the frontend's domain (step 4.4) into the backend's `Cors__AllowedOrigins__0` (step 2.4)
   and redeploy the backend.
2. If you changed `VITE_API_URL` after the frontend's first build, trigger a frontend redeploy too
   (Vite bakes it in at build time — a variable change alone doesn't retroactively update an
   already-built bundle).

## 6. Verify

- Storefront loads at the frontend domain, product images render.
- `/admin/login` → sign in with `admin@luna.local` / `Papamama-123`.
- Upload a product image or homepage banner — reload the page (or trigger a redeploy) and confirm
  it's still there, proving the volume from step 3 is actually mounted.
- Place a test COD order end to end.

## Troubleshooting: "Application failed to respond"

This is Railway's generic symptom for "the container started, but nothing answers on the port
Railway is routing to." Two causes, both covered above:

1. **Wrong Dockerfile built** (the dev image, which serves on port 5173 and isn't meant for
   production traffic at all — Vite's dev server also rejects requests from hosts it doesn't
   recognize by default). Fix: step 4.3 above.
2. **Wrong target port configured**, even with the right Dockerfile. Fix: step 4.4 above — Target
   Port must be `8080` for the frontend, matching its Dockerfile's `EXPOSE 8080`.

Check the deploy logs first (Railway dashboard → service → **Deployments** → latest → **View
Logs**) — they'll show exactly which process actually started, which tells you which of the two
this is before you go hunting through settings.

## Security note on the test admin password

`Papamama-123` is now in this chat log and this file's git history — treat it as a temporary,
throwaway credential, not something to keep if this deployment becomes anything more than a private
test. Rotate it from `/admin/users` (the existing reset-password admin feature) once you're done
testing, or before sharing the URL with anyone else.
