#!/usr/bin/env bash
# Runs the full server test suite on CT 160, Postgres tests included, from the dev machine:
#
#   bash server/tests/run-ct-tests.sh                      tests committed HEAD
#   bash server/tests/run-ct-tests.sh --worktree           HEAD plus the uncommitted changes under shared/ and server/
#   bash server/tests/run-ct-tests.sh --worktree <path>... HEAD plus only these paths as they are on disk
#   bash server/tests/run-ct-tests.sh <tree-ish>           any commit or tree (git write-tree output works too)
#   TEST_FILTER="FullyQualifiedName~Economy" bash server/tests/run-ct-tests.sh
#
# The sources go to their own folder on the CT (HADAL_TEST_DIR, default /opt/hadal-safetytest), never to the live
# /opt/hadal. Postgres tests get a throwaway database inside hadal-postgres that is dropped afterwards
# (server/tests/ct-tests.sh). Nothing is deployed and the live gateway is not touched.
set -euo pipefail

HOST="${HADAL_DEPLOY_HOST:-cicibyte@doku}"
CT="${HADAL_CT:-}"
# Root on the Hadal host: Doku directly (since 2026-11-01), or inside a Proxmox CT when HADAL_CT is set
# (the old DL380 one: HADAL_DEPLOY_HOST=cicibyte@dl380 HADAL_CT=160).
if [[ -n "${CT}" ]]; then ON="export PATH=\$PATH:/usr/sbin; sudo pct exec ${CT} --"; else ON="sudo"; fi
DIR="${HADAL_TEST_DIR:-/opt/hadal-safetytest}"
REPO="$(git -C "$(dirname "${BASH_SOURCE[0]}")" rev-parse --show-toplevel)"
PATHS=(shared server/Hadal.Game server/Hadal.Gateway server/Hadal.Game.Tests server/tests)

case "${DIR}" in
  /opt/hadal|/opt/hadal/*) echo "!! HADAL_TEST_DIR must not be the live tree"; exit 2 ;;
esac

TREE="${1:-HEAD}"
if [[ "${TREE}" == "--worktree" ]]; then
  # A private index: HEAD plus the working-tree state of the given paths (default: all test paths), so other
  # people's uncommitted files can be left out. The real index is left alone.
  shift
  ADD=("$@")
  [[ ${#ADD[@]} -eq 0 ]] && ADD=("${PATHS[@]}")
  TMP_INDEX="$(mktemp)"
  trap 'rm -f "${TMP_INDEX}"' EXIT
  GIT_INDEX_FILE="${TMP_INDEX}" git -C "${REPO}" read-tree HEAD
  GIT_INDEX_FILE="${TMP_INDEX}" git -C "${REPO}" add -A -- "${ADD[@]}"
  TREE="$(GIT_INDEX_FILE="${TMP_INDEX}" git -C "${REPO}" write-tree)"
fi

echo "== testing $(git -C "${REPO}" rev-parse --short "${TREE}") on ${HOST}${CT:+ CT ${CT}} in ${DIR}"
git -C "${REPO}" archive --format=tar.gz "${TREE}" -- "${PATHS[@]}" \
  | ssh -o BatchMode=yes "${HOST}" "${ON} bash -c \
      'rm -rf ${DIR} && mkdir -p ${DIR} && tar --no-same-owner -m -xzf - -C ${DIR} \
       && sed -i \"s/\r\$//\" ${DIR}/server/tests/ct-tests.sh && bash ${DIR}/server/tests/ct-tests.sh ${DIR} \"${TEST_FILTER:-}\"'"
