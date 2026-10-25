#!/usr/bin/env bash
# One-time public exposure of the Hadal gateway through Cloudflare Tunnel. Run on the DL380 HOST:
#
#   ssh cicibyte@dl380 'bash -s' < server/deploy/setup-tunnel.sh
#
# What it does (idempotent):
#   1. pushes the Cloudflare account cert into CT 160 *temporarily*
#   2. creates tunnel "hadal" and routes DNS hadal-api.cicibyte.com → this tunnel
#      (hadal.cicibyte.com — the public teaser site — is NOT touched)
#   3. deletes the account cert from the CT again; only the tunnel's own credentials stay
#   4. installs cloudflared as a systemd service → http://127.0.0.1:19001 (gateway)
# No ports are opened; cloudflared dials out to Cloudflare.
set -euo pipefail
export PATH="$PATH:/usr/sbin"

CT=160
HOSTNAME_PUBLIC=hadal-api.cicibyte.com
CERT=/root/cloudflare/cicibyte-cert.pem

sudo test -f "$CERT" || { echo "account cert missing at $CERT"; exit 1; }

sudo pct exec "$CT" -- mkdir -p /root/.cloudflared /etc/cloudflared
sudo pct push "$CT" "$CERT" /root/.cloudflared/cert.pem --perms 600
trap 'sudo pct exec "$CT" -- rm -f /root/.cloudflared/cert.pem' EXIT

sudo pct exec "$CT" -- bash -s <<EOF
set -euo pipefail
if ! cloudflared tunnel list -o json | python3 -c "import json,sys; sys.exit(0 if any(t['name']=='hadal' for t in json.load(sys.stdin)) else 1)"; then
  cloudflared tunnel create hadal
fi
ID=\$(cloudflared tunnel list -o json | python3 -c "import json,sys; print([t['id'] for t in json.load(sys.stdin) if t['name']=='hadal'][0])")

cloudflared tunnel route dns hadal $HOSTNAME_PUBLIC

if [ -f /root/.cloudflared/\$ID.json ]; then
  mv /root/.cloudflared/\$ID.json /etc/cloudflared/\$ID.json
fi
chmod 600 /etc/cloudflared/\$ID.json

cat > /etc/cloudflared/config.yml <<CFG
tunnel: \$ID
credentials-file: /etc/cloudflared/\$ID.json
ingress:
  - hostname: $HOSTNAME_PUBLIC
    service: http://127.0.0.1:19001
  - service: http_status:404
CFG

if ! systemctl is-enabled cloudflared >/dev/null 2>&1; then
  cloudflared service install
fi
systemctl restart cloudflared
sleep 3
systemctl is-active cloudflared
EOF

echo "== checking https://$HOSTNAME_PUBLIC/health (DNS can take a minute)"
for _ in $(seq 1 20); do
  if curl -fs "https://$HOSTNAME_PUBLIC/health"; then echo; echo "== tunnel is live"; exit 0; fi
  sleep 5
done
echo "!! not reachable yet — check: sudo pct exec $CT -- journalctl -u cloudflared -n 50"
