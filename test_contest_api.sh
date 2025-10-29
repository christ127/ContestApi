#!/usr/bin/env bash
set -euo pipefail

# ==== CONFIG ====
# Local:
BASE="${BASE:-http://localhost:5111}"
# Cloud example:
# BASE="https://contest-api.azurewebsites.net"

SLUG="${SLUG:-photo-contest-2026}"     # must exist (or create with create_contest)
PHOTO="${PHOTO:-./photo.jpg}"          # local image file to upload
EMAIL_UNIQ="$(date +%s)@example.com"   # unique email to avoid "already submitted"
FIRST="Ana"
LAST="Martínez"
PHONE="(787) 555-1234"

echo "Using BASE=$BASE"
echo "Using SLUG=$SLUG"
echo "Using PHOTO=$PHOTO"
echo "Unique email: $EMAIL_UNIQ"
echo

# small helper for pretty print if jq is available
pp() { if command -v jq >/dev/null 2>&1; then jq; else cat; fi; }

# 0) Health & DB
echo "==> /health"
curl -sS "$BASE/health" | pp
echo

echo "==> /dbcheck"
curl -sS "$BASE/dbcheck" | pp
echo

# 1) (Optional) Create a contest if you don’t have one
create_contest() {
  echo "==> POST /api/contests (create)"
  curl -sS -i -X POST "$BASE/api/contests" \
    -H "Content-Type: application/json" \
    -d "{
      \"name\": \"Photo Contest 2026\",
      \"slug\": \"photo-contest-2026\",
      \"startsAtUtc\": \"2025-11-01T00:00:00Z\",
      \"endsAtUtc\": \"2025-12-01T00:00:00Z\",
      \"isActive\": true
    }"
  echo
}
# Uncomment next line to create:
# create_contest

# 2) Get contest by slug
echo "==> GET /api/contests/{slug}"
curl -sS "$BASE/api/contests/$SLUG" | pp
echo

# 3) PRESIGN upload
echo "==> POST /api/uploads/presign"
# get file info
if [[ ! -f "$PHOTO" ]]; then
  echo "ERROR: PHOTO not found at $PHOTO"
  exit 1
fi
MIME="$(file --mime-type -b "$PHOTO" || echo image/jpeg)"
BYTES="$(wc -c < "$PHOTO" | tr -d ' ')"

PRESIGN_JSON="$(curl -sS -X POST "$BASE/api/uploads/presign" \
  -H "Content-Type: application/json" \
  -d "{\"FileName\":\"$(basename "$PHOTO")\",\"ContentType\":\"$MIME\",\"Bytes\":$BYTES}" )"

echo "$PRESIGN_JSON" | pp
UPLOAD_URL="$(echo "$PRESIGN_JSON" | (jq -r .uploadUrl 2>/dev/null || python - <<'PY'
import sys, json
d=json.load(sys.stdin); print(d.get("uploadUrl",""))
PY
))"
BLOB_NAME="$(echo "$PRESIGN_JSON" | (jq -r .blobName 2>/dev/null || python - <<'PY'
import sys, json
d=json.load(sys.stdin); print(d.get("blobName",""))
PY
))"

if [[ -z "$UPLOAD_URL" || -z "$BLOB_NAME" ]]; then
  echo "ERROR: presign did not return uploadUrl/blobName"
  exit 1
fi
echo "uploadUrl: $UPLOAD_URL"
echo "blobName : $BLOB_NAME"
echo

# 4) Upload to Blob with SAS
echo "==> PUT file to Blob (SAS)"
curl -sS -X PUT --upload-file "$PHOTO" \
  -H "x-ms-blob-type: BlockBlob" \
  -H "Content-Type: $MIME" \
  "$UPLOAD_URL" -i
echo

# 5) Create submission (includes phone + blob info)
echo "==> POST /api/submissions"
curl -sS -i -X POST "$BASE/api/submissions" \
  -H "Content-Type: application/json" \
  -d "{
    \"contestSlug\": \"$SLUG\",
    \"firstName\": \"$FIRST\",
    \"lastName\": \"$LAST\",
    \"email\": \"$EMAIL_UNIQ\",
    \"phone\": \"$PHONE\",
    \"consentGiven\": true,
    \"consentVersion\": \"v1\",
    \"blobName\": \"$BLOB_NAME\",
    \"contentType\": \"$MIME\",
    \"sizeBytes\": $BYTES
  }"
echo

# 6) List submissions (paged)
echo "==> GET /api/submissions?contestSlug=$SLUG&page=1&pageSize=10"
curl -sS "$BASE/api/submissions?contestSlug=$SLUG&page=1&pageSize=10" | pp
echo

# 7) Export CSV (will save to file)
OUT="export_${SLUG}_$(date +%Y%m%d%H%M%S).csv"
echo "==> GET /api/submissions/export?contestSlug=$SLUG  -> $OUT"
curl -sS -L "$BASE/api/submissions/export?contestSlug=$SLUG" -o "$OUT"
echo "Saved CSV -> $OUT"
echo

echo "All tests done ✅"

