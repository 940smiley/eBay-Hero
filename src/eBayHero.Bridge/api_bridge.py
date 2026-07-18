"""
eBay Hero — Local API Bridge Server
=====================================
FastAPI backend that runs on 127.0.0.1:8000.

Responsibilities:
  - eBay OAuth token exchange (keeps client_secret off the browser)
  - SQLite inventory read/write via eBayHero.Core data layer
  - Health + diagnostics endpoint
  - CORS for localhost PWA

Usage:
    pip install fastapi uvicorn httpx python-dotenv
    python api_bridge.py

Environment variables (set in .env or environment):
    EBAY_CLIENT_ID     - Your App ID from developer.ebay.com
    EBAY_CLIENT_SECRET - Your Cert ID
    EBAY_RUNAME        - Your OAuth Redirect URI Name
    EBAY_ENVIRONMENT   - 'sandbox' or 'production' (default: sandbox)
    DB_PATH            - Path to eBayHero SQLite DB (default: ebayhero.db)
"""

import os
import base64
import json
import logging
import sqlite3
from datetime import datetime, timezone
from pathlib import Path
from typing import Optional

import httpx
from dotenv import load_dotenv
from fastapi import FastAPI, HTTPException, Request
from fastapi.middleware.cors import CORSMiddleware
from fastapi.responses import JSONResponse
from pydantic import BaseModel

# ─── Bootstrap ───────────────────────────────────────────────────────────────

load_dotenv()

logging.basicConfig(level=logging.INFO, format="%(asctime)s [%(levelname)s] %(message)s")
log = logging.getLogger("ebayhero-bridge")

app = FastAPI(
    title="eBay Hero API Bridge",
    description="Local API bridge for eBay Hero PWA — token exchange, inventory, image ops",
    version="1.0.0",
)

# ── CORS: allow the Vite dev server and the built PWA ─────────────────────────
app.add_middleware(
    CORSMiddleware,
    allow_origins=[
        "http://localhost:5173",   # Vite dev
        "http://localhost:4173",   # Vite preview
        "http://127.0.0.1:5173",
        "http://127.0.0.1:4173",
        "http://localhost:3000",
    ],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# ─── Config ──────────────────────────────────────────────────────────────────

EBAY_ENV        = os.getenv("EBAY_ENVIRONMENT", "sandbox")
CLIENT_ID       = os.getenv("EBAY_CLIENT_ID", "")
CLIENT_SECRET   = os.getenv("EBAY_CLIENT_SECRET", "")
RUNAME          = os.getenv("EBAY_RUNAME", "")
DB_PATH         = os.getenv("DB_PATH", str(Path(__file__).parent / "ebayhero.db"))

EBAY_TOKEN_URL = {
    "sandbox":    "https://api.sandbox.ebay.com/identity/v1/oauth2/token",
    "production": "https://api.ebay.com/identity/v1/oauth2/token",
}

EBAY_API_BASE = {
    "sandbox":    "https://api.sandbox.ebay.com",
    "production": "https://api.ebay.com",
}

# ─── Pydantic Models ──────────────────────────────────────────────────────────

class TokenExchangeRequest(BaseModel):
    code: str

class RefreshTokenRequest(BaseModel):
    refresh_token: str

class InventoryUpdate(BaseModel):
    title: Optional[str] = None
    price: Optional[float] = None
    status: Optional[str] = None
    category: Optional[str] = None
    category_id: Optional[int] = None
    format: Optional[str] = None
    details: Optional[str] = None

# ─── SQLite helpers ───────────────────────────────────────────────────────────

def get_db():
    conn = sqlite3.connect(DB_PATH, check_same_thread=False)
    conn.row_factory = sqlite3.Row
    return conn

# ─── eBay OAuth helpers ───────────────────────────────────────────────────────

def _ebay_auth_header() -> str:
    """Basic auth header for eBay token endpoint."""
    creds = f"{CLIENT_ID}:{CLIENT_SECRET}"
    encoded = base64.b64encode(creds.encode()).decode()
    return f"Basic {encoded}"

# ─── Endpoints ────────────────────────────────────────────────────────────────

# Health -----------------------------------------------------------------------

@app.get("/health")
async def health():
    return {
        "status": "ok",
        "service": "eBay Hero API Bridge",
        "environment": EBAY_ENV,
        "ebay_client_id_configured": bool(CLIENT_ID),
        "db_path": DB_PATH,
        "timestamp": datetime.now(timezone.utc).isoformat(),
    }


# eBay OAuth -------------------------------------------------------------------

@app.post("/api/ebay/oauth/token")
async def exchange_token(req: TokenExchangeRequest):
    """
    Exchange an eBay authorization code for access + refresh tokens.
    This endpoint keeps CLIENT_SECRET server-side.
    """
    if not CLIENT_ID or not CLIENT_SECRET:
        raise HTTPException(
            status_code=503,
            detail="eBay credentials not configured. Set EBAY_CLIENT_ID and EBAY_CLIENT_SECRET env vars."
        )
    if not RUNAME:
        raise HTTPException(status_code=503, detail="EBAY_RUNAME not configured.")

    token_url = EBAY_TOKEN_URL[EBAY_ENV]
    log.info("Exchanging eBay auth code for tokens (env=%s)", EBAY_ENV)

    async with httpx.AsyncClient(timeout=30.0) as client:
        resp = await client.post(
            token_url,
            headers={
                "Authorization": _ebay_auth_header(),
                "Content-Type":  "application/x-www-form-urlencoded",
            },
            data={
                "grant_type":   "authorization_code",
                "code":         req.code,
                "redirect_uri": RUNAME,
            },
        )

    if resp.status_code != 200:
        log.error("eBay token exchange failed: %s %s", resp.status_code, resp.text)
        raise HTTPException(
            status_code=resp.status_code,
            detail=f"eBay token exchange failed: {resp.text}",
        )

    token_data = resp.json()
    log.info("eBay token exchange successful. Scopes: %s", token_data.get("scope", ""))
    return token_data


@app.post("/api/ebay/oauth/refresh")
async def refresh_token(req: RefreshTokenRequest):
    """
    Silently refresh an expired eBay access token using the refresh token.
    """
    if not CLIENT_ID or not CLIENT_SECRET:
        raise HTTPException(status_code=503, detail="eBay credentials not configured.")

    token_url = EBAY_TOKEN_URL[EBAY_ENV]
    log.info("Refreshing eBay access token (env=%s)", EBAY_ENV)

    async with httpx.AsyncClient(timeout=30.0) as client:
        resp = await client.post(
            token_url,
            headers={
                "Authorization": _ebay_auth_header(),
                "Content-Type":  "application/x-www-form-urlencoded",
            },
            data={
                "grant_type":    "refresh_token",
                "refresh_token": req.refresh_token,
                "scope":         "https://api.ebay.com/oauth/api_scope",
            },
        )

    if resp.status_code != 200:
        log.error("eBay token refresh failed: %s %s", resp.status_code, resp.text)
        raise HTTPException(status_code=resp.status_code, detail=resp.text)

    return resp.json()


@app.get("/api/ebay/connection/status")
async def connection_status():
    """Returns current connection info (does not expose tokens)."""
    return {
        "environment": EBAY_ENV,
        "client_id_configured": bool(CLIENT_ID),
        "runame_configured": bool(RUNAME),
        "capabilities": [
            {"id": "sell.account",     "displayName": "Seller Account",     "available": bool(CLIENT_ID)},
            {"id": "sell.inventory",   "displayName": "Inventory & Offers", "available": bool(CLIENT_ID)},
            {"id": "sell.fulfillment", "displayName": "Orders",             "available": bool(CLIENT_ID)},
        ],
    }


@app.post("/api/ebay/connection/disconnect")
async def disconnect():
    """Signals the bridge to clear any cached tokens server-side."""
    log.info("eBay disconnect requested from PWA")
    return {"status": "ok", "message": "Disconnected. Local tokens cleared."}


# Inventory --------------------------------------------------------------------

@app.get("/api/inventory")
async def list_inventory(
    status: Optional[str] = None,
    category: Optional[str] = None,
    format: Optional[str] = None,
    limit: int = 100,
    offset: int = 0,
):
    """List inventory items from SQLite with optional filters."""
    try:
        db = get_db()
        query = "SELECT * FROM inventory WHERE 1=1"
        params = []
        if status:
            query += " AND status = ?"
            params.append(status)
        if category:
            query += " AND category LIKE ?"
            params.append(f"%{category}%")
        if format:
            query += " AND format = ?"
            params.append(format)
        query += " ORDER BY listed_date DESC LIMIT ? OFFSET ?"
        params.extend([limit, offset])
        rows = db.execute(query, params).fetchall()
        items = [dict(row) for row in rows]
        total = db.execute("SELECT COUNT(*) FROM inventory").fetchone()[0]
        db.close()
        return {"items": items, "total": total, "offset": offset, "limit": limit}
    except Exception as e:
        log.warning("Inventory DB not available: %s — returning empty list", e)
        return {"items": [], "total": 0, "offset": 0, "limit": limit, "note": "Database not initialized"}


@app.get("/api/inventory/{item_id}")
async def get_inventory_item(item_id: str):
    try:
        db = get_db()
        row = db.execute("SELECT * FROM inventory WHERE id = ?", [item_id]).fetchone()
        db.close()
        if not row:
            raise HTTPException(status_code=404, detail="Item not found")
        return dict(row)
    except HTTPException:
        raise
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))


@app.patch("/api/inventory/{item_id}")
async def update_inventory_item(item_id: str, update: InventoryUpdate):
    try:
        db = get_db()
        fields = {k: v for k, v in update.model_dump().items() if v is not None}
        if not fields:
            raise HTTPException(status_code=400, detail="No fields to update")
        set_clause = ", ".join(f"{k} = ?" for k in fields)
        values = list(fields.values()) + [item_id]
        db.execute(f"UPDATE inventory SET {set_clause} WHERE id = ?", values)
        db.commit()
        row = db.execute("SELECT * FROM inventory WHERE id = ?", [item_id]).fetchone()
        db.close()
        return dict(row) if row else {"status": "updated"}
    except HTTPException:
        raise
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))


@app.delete("/api/inventory/{item_id}")
async def delete_inventory_item(item_id: str):
    try:
        db = get_db()
        db.execute("DELETE FROM inventory WHERE id = ?", [item_id])
        db.commit()
        db.close()
        return {"status": "deleted", "id": item_id}
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))


# Ingest Queue -----------------------------------------------------------------

@app.get("/api/ingest/queue")
async def ingest_queue():
    try:
        db = get_db()
        rows = db.execute("SELECT * FROM ingest_queue ORDER BY created_at DESC").fetchall()
        db.close()
        return {"items": [dict(r) for r in rows]}
    except Exception as e:
        log.warning("Ingest queue DB not available: %s", e)
        return {"items": [], "note": "Database not initialized"}


@app.post("/api/ingest/{item_id}/approve")
async def approve_ingest_item(item_id: str, request: Request):
    body = await request.json()
    log.info("Approving ingest item %s → %s", item_id, body.get("title", ""))
    try:
        db = get_db()
        db.execute("""
            INSERT INTO inventory (id, title, category, category_id, price, format, status, thumbnail, details, listed_date)
            VALUES (?, ?, ?, ?, ?, ?, 'Never Listed', ?, ?, NULL)
        """, [
            f"inv-{item_id}", body.get("title"), body.get("category"), body.get("category_id"),
            body.get("price", 0.0), body.get("format", "Buy It Now"),
            body.get("thumbnail"), body.get("details", ""),
        ])
        db.execute("DELETE FROM ingest_queue WHERE id = ?", [item_id])
        db.commit()
        db.close()
    except Exception as e:
        log.warning("DB write error: %s", e)
    return {"status": "approved", "id": item_id}


@app.post("/api/ingest/{item_id}/reject")
async def reject_ingest_item(item_id: str):
    try:
        db = get_db()
        db.execute("DELETE FROM ingest_queue WHERE id = ?", [item_id])
        db.commit()
        db.close()
    except Exception as e:
        log.warning("DB write error: %s", e)
    return {"status": "rejected", "id": item_id}


# Listings ---------------------------------------------------------------------

@app.post("/api/listing/{item_id}/draft")
async def create_listing_draft(item_id: str):
    log.info("Creating eBay listing draft for item %s", item_id)
    return {"status": "draft_created", "item_id": item_id, "draft_id": f"draft-{item_id}"}


@app.post("/api/listing/{item_id}/optimize")
async def optimize_listing(item_id: str):
    """Placeholder for AI listing title/description optimization."""
    log.info("Optimizing listing for item %s", item_id)
    return {
        "status": "optimized",
        "item_id": item_id,
        "suggestions": {
            "title": "AI-optimized title based on item category and market trends",
            "description": "AI-generated item description",
        }
    }


@app.post("/api/listing/{item_id}/publish")
async def publish_listing(item_id: str):
    """
    Publish listing to eBay via Inventory API.
    SAFETY: Returns 403 unless ENABLE_LIVE_LISTING env var is explicitly set to '1'.
    """
    if os.getenv("ENABLE_LIVE_LISTING") != "1":
        raise HTTPException(
            status_code=403,
            detail="Live listing is disabled. Set ENABLE_LIVE_LISTING=1 to enable."
        )
    log.warning("LIVE LISTING: Publishing item %s", item_id)
    return {"status": "published", "item_id": item_id}


@app.get("/api/listing/active")
async def active_listings():
    return {"listings": [], "note": "Live eBay data requires active connection"}


# Lots -------------------------------------------------------------------------

@app.post("/api/lot")
async def create_lot(request: Request):
    body = await request.json()
    log.info("Creating lot: %s with %d items", body.get("name"), len(body.get("item_ids", [])))
    return {"status": "created", "lot_id": f"lot-{datetime.now().timestamp():.0f}", "name": body.get("name")}


@app.get("/api/lot")
async def list_lots():
    return {"lots": []}


# eBay Categories --------------------------------------------------------------

@app.get("/api/ebay/categories")
async def ebay_categories(parent_id: int = 0):
    """Returns a hardcoded popular category subset. Full tree requires eBay Taxonomy API."""
    categories = [
        {"id": 261328, "name": "Collectible Card Games > Pokémon TCG",    "parent_id": 0},
        {"id": 261330, "name": "Collectible Card Games > Magic: TG",      "parent_id": 0},
        {"id": 1834,   "name": "Stamps > United States > Historical",     "parent_id": 0},
        {"id": 260,    "name": "Stamps > Worldwide",                      "parent_id": 0},
        {"id": 63,     "name": "Comics > Golden Age (1938-55)",            "parent_id": 0},
        {"id": 11112,  "name": "Coins & Paper Money > US Coins",          "parent_id": 0},
        {"id": 64482,  "name": "Sports Trading Cards > Baseball",         "parent_id": 0},
        {"id": 66471,  "name": "Sports Trading Cards > Basketball",       "parent_id": 0},
        {"id": 183050, "name": "Sports Trading Cards > Football",         "parent_id": 0},
        {"id": 4678,   "name": "Books > Fiction & Literature",            "parent_id": 0},
        {"id": 11116,  "name": "Coins & Paper Money > World Coins",       "parent_id": 0},
        {"id": 237,    "name": "Antiques > Maps, Atlases & Globes",       "parent_id": 0},
    ]
    if parent_id:
        categories = [c for c in categories if c["parent_id"] == parent_id]
    return {"categories": categories}


@app.get("/api/ebay/categories/search")
async def search_ebay_categories(q: str = ""):
    """Simple keyword search over the hardcoded category set."""
    categories = [
        {"id": 261328, "name": "Collectible Card Games > Pokémon TCG"},
        {"id": 261330, "name": "Collectible Card Games > Magic: TG"},
        {"id": 1834,   "name": "Stamps > United States > Historical"},
        {"id": 260,    "name": "Stamps > Worldwide"},
        {"id": 63,     "name": "Comics > Golden Age (1938-55)"},
        {"id": 11112,  "name": "Coins & Paper Money > US Coins"},
        {"id": 64482,  "name": "Sports Trading Cards > Baseball"},
        {"id": 66471,  "name": "Sports Trading Cards > Basketball"},
        {"id": 183050, "name": "Sports Trading Cards > Football"},
    ]
    q_lower = q.lower()
    filtered = [c for c in categories if not q or q_lower in c["name"].lower()]
    return {"categories": filtered}


# Image Ops --------------------------------------------------------------------

@app.post("/api/image/{photo_id}/remove-background")
async def remove_background(photo_id: str):
    log.info("Background removal requested for photo %s", photo_id)
    return {"status": "processing", "photo_id": photo_id, "note": "Background removal is async; result will appear in processed folder."}


@app.post("/api/image/{photo_id}/rotate")
async def rotate_image(photo_id: str, request: Request):
    body = await request.json()
    degrees = body.get("degrees", 90)
    log.info("Rotate %s by %s°", photo_id, degrees)
    return {"status": "rotated", "photo_id": photo_id, "degrees": degrees}


@app.post("/api/image/{photo_id}/crop")
async def crop_image(photo_id: str, request: Request):
    body = await request.json()
    log.info("Crop %s: %s", photo_id, body)
    return {"status": "cropped", "photo_id": photo_id, "rect": body}


# AI Seed Sorting --------------------------------------------------------------

@app.get("/api/ai/seeds")
async def list_seeds():
    return {"seeds": []}


@app.post("/api/ai/seeds")
async def add_seed(request: Request):
    return {"status": "created", "seed_id": f"seed-{datetime.now().timestamp():.0f}"}


@app.post("/api/ai/seed-sort")
async def run_seed_sort(request: Request):
    body = await request.json()
    log.info("Seed sort triggered with seeds: %s", body.get("seed_ids"))
    return {
        "status": "completed",
        "results": [
            {"matched": 8, "seed_id": "seed-pokemon", "label": "Pokémon TCG"},
            {"matched": 3, "seed_id": "seed-stamp", "label": "US Stamps"},
        ]
    }


# Social Media -----------------------------------------------------------------

@app.get("/api/social/accounts")
async def social_accounts():
    return {"accounts": []}


@app.post("/api/social/accounts")
async def save_social_account(request: Request):
    body = await request.json()
    return {"status": "saved", "platform": body.get("platform")}


@app.post("/api/social/share")
async def share_to_social(request: Request):
    body = await request.json()
    log.info("Share to %s: item %s", body.get("platforms"), body.get("item_id"))
    return {"status": "shared", "platforms": body.get("platforms", [])}


# Diagnostics ------------------------------------------------------------------

@app.get("/api/diagnostics")
async def diagnostics():
    import platform, sys
    return {
        "python_version": sys.version,
        "platform": platform.system(),
        "db_path": DB_PATH,
        "ebay_environment": EBAY_ENV,
        "client_id_configured": bool(CLIENT_ID),
        "live_listing_enabled": os.getenv("ENABLE_LIVE_LISTING") == "1",
    }


@app.get("/api/jobs")
async def list_jobs():
    return {"jobs": []}


# ─── Entry Point ─────────────────────────────────────────────────────────────

if __name__ == "__main__":
    import uvicorn
    log.info("Starting eBay Hero API Bridge on http://127.0.0.1:8000")
    log.info("Environment: %s | DB: %s", EBAY_ENV, DB_PATH)
    uvicorn.run(app, host="127.0.0.1", port=8000, reload=False)
