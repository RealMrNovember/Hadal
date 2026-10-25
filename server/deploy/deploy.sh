#!/usr/bin/env bash
# One-command backend deploy to CT 160 (run from the repo on the dev machine):
#
#   server/deploy/deploy.sh            sync → server tests → build → rollout → health check
#   SKIP_TESTS=1 server/deploy/deploy.sh   (emergency only)
#
# On a failed health check the previous gateway image is restored automatically.
set -euo pipefail

HOST="${HADAL_DEPLOY_HOST:-cicibyte@dl380}"
CT="${HADAL_CT:-160}"
REPO="$(git -C "$(dirname "${BASH_SOURCE[0]}")" rev-parse --show-toplevel)"
SHA="$(git -C "${REPO}" rev-parse --short HEAD)"

if [[ -n "$(git -C "${REPO}" status --porcelain -- shared server)" ]]; then
  echo "!! shared/ or server/ has uncommitted changes — deploying them as ${SHA}+dirty"
  SHA="${SHA}+dirty"
fi

echo "== sync ${SHA} → CT ${CT}"
tar -C "${REPO}" --exclude=bin --exclude=obj --exclude='bin~' --exclude='obj~' --exclude='.env' \
    -czf - shared server \
  | ssh -o BatchMode=yes "${HOST}" "export PATH=\$PATH:/usr/sbin; sudo pct exec ${CT} -- bash -c \
      'tar --no-same-owner -m -xzf - -C /opt/hadal && find /opt/hadal/server -name \"*.sh\" -exec sed -i \"s/\r\$//\" {} + && chown -R hadal:hadal /opt/hadal/shared /opt/hadal/server'"

echo "== remote rollout"
ssh -o BatchMode=yes "${HOST}" "export PATH=\$PATH:/usr/sbin; sudo pct exec ${CT} -- su - hadal -c \
  'SKIP_TESTS=${SKIP_TESTS:-0} bash /opt/hadal/server/deploy/remote-deploy.sh ${SHA}'"
