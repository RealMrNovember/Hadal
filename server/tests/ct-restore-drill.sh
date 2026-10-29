#!/usr/bin/env bash
# Runs on CT 160 as root (started by run-restore-drill.sh). Usage: ct-restore-drill.sh <work-dir> [dump-file]
#
# The backup restore drill (docs/HADAL_SERVER_INFRASTRUCTURE.md, "Restore drill"): restores a pg_dump from
# /opt/hadal/backups (the newest one by default) into a throwaway database hadal_restore_<time> inside hadal-postgres,
# compares its row counts with the live database (read-only), then boots the real gateway code on it and loads every
# player through the repository (RestoreDrillTests), including a balance rollback. The throwaway database is dropped
# at the end unless KEEP_DB=1. The live "hadal" database is only ever read, inside a READ ONLY transaction.
set -euo pipefail
export LC_ALL=C

WORK="${1:?work dir}"
DUMP="${2:-}"
SERVER_ENV=/opt/hadal/server/.env
PG_CONTAINER=hadal-postgres
DB="hadal_restore_$(date +%Y%m%d%H%M%S)"

case "${WORK}" in
  /opt/hadal|/opt/hadal/*) echo "!! refusing to run in the live tree ${WORK}"; exit 2 ;;
esac
[[ -n "${DUMP}" ]] || DUMP="$(ls -t /opt/hadal/backups/*.sql | head -1)"
[[ -f "${DUMP}" ]] || { echo "!! no dump ${DUMP}"; exit 2; }
# A dump made with --create or by pg_dumpall would switch to (or recreate) the live database: refuse it.
if grep -qE '^(\\connect|CREATE DATABASE|DROP DATABASE)' "${DUMP}"; then
  echo "!! ${DUMP} contains \\connect / CREATE DATABASE / DROP DATABASE; restore it by hand into a scratch database"
  exit 2
fi

chown -R hadal:hadal "${WORK}"
HADAL_DB_PASSWORD="$(grep -E '^HADAL_DB_PASSWORD=' "${SERVER_ENV}" | head -1 | cut -d= -f2-)"

psql_db() { local db="$1"; shift; docker exec -i "${PG_CONTAINER}" psql -v ON_ERROR_STOP=1 -q -U hadal_app -d "${db}" "$@"; }
drop_db() {
  if [[ "${KEEP_DB:-0}" == "1" ]]; then
    echo "-- KEEP_DB=1: ${DB} is kept; drop it with: docker exec ${PG_CONTAINER} psql -U hadal_app -d postgres -c 'DROP DATABASE ${DB} WITH (FORCE);'"
  else
    psql_db postgres -c "DROP DATABASE IF EXISTS ${DB} WITH (FORCE);" >/dev/null 2>&1 || true
    echo "-- dropped ${DB}"
  fi
  rm -f "${WORK}/.drill-env"
}
trap drop_db EXIT

echo "== restore drill: $(basename "${DUMP}") ($(du -h "${DUMP}" | cut -f1)) -> ${DB}"
psql_db postgres -c "CREATE DATABASE ${DB};"
START=$(date +%s)
psql_db "${DB}" < "${DUMP}" >/dev/null
echo "-- restored in $(( $(date +%s) - START ))s"

# Row counts: restored vs live (live read-only). The live side has moved on since the dump; the point is that
# every table came back with data of the right size.
COUNTS_SQL="$(psql_db "${DB}" -At -c "SELECT string_agg(format('SELECT %L, count(*) FROM hadal.%I', table_name, table_name), ' UNION ALL ' ORDER BY table_name) FROM information_schema.tables WHERE table_schema = 'hadal'")"
printf '%-24s %10s %10s\n' table restored live
join -t '|' -a 1 \
  <(psql_db "${DB}" -At -c "${COUNTS_SQL}" | sort) \
  <(printf 'BEGIN READ ONLY;\n%s;\nROLLBACK;\n' "${COUNTS_SQL}" | psql_db hadal -At 2>/dev/null | grep '|' | sort) \
  | awk -F'|' '{ printf "%-24s %10s %10s\n", $1, $2, ($3 == "" ? "-" : $3) }'

ENV_FILE="${WORK}/.drill-env"
install -m 600 -o hadal -g hadal /dev/null "${ENV_FILE}"
cat > "${ENV_FILE}" <<EOF
export HADAL_RESTORE_DRILL_PG='Host=127.0.0.1;Port=19005;Database=${DB};Username=hadal_app;Password=${HADAL_DB_PASSWORD}'
export PATH="\$PATH:/usr/share/dotnet:\$HOME/.dotnet" DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1
EOF

echo "-- gateway code on the restored data (RestoreDrillTests)"
set +e
su hadal -s /bin/bash -c ". '${ENV_FILE}'; dotnet test '${WORK}/server/Hadal.Game.Tests/Hadal.Game.Tests.csproj' --nologo \
  --filter 'FullyQualifiedName~RestoreDrillTests' --logger 'console;verbosity=detailed'" > "${WORK}/drill.log" 2>&1
STATUS=$?
set -e
grep -E "^\s+(players|logged in|running|the release file|after a restart)|^\s+(Passed|Failed) Hadal|Test Run|error CS|\[FAIL\]|Assert\.|Expected:|Actual:" "${WORK}/drill.log" | head -40
echo "-- full log: ${WORK}/drill.log"
exit ${STATUS}
