#!/usr/bin/env bash
# Runs on CT 160 as root (started by run-ct-tests.sh). Usage: ct-tests.sh <work-dir> [dotnet test --filter]
#
# Runs the whole server test suite in <work-dir> (its own copy of the sources, never /opt/hadal) with every
# Postgres-backed test switched on. Those tests get a disposable database inside the hadal-postgres container:
# hadal_test_<random>, created here and dropped on exit. The live "hadal" database is never connected to.
set -euo pipefail

WORK="${1:?work dir}"
FILTER="${2:-}"
SERVER_ENV=/opt/hadal/server/.env
PG_CONTAINER=hadal-postgres
DB="hadal_test_$(date +%s)_$RANDOM"

case "${WORK}" in
  /opt/hadal|/opt/hadal/*) echo "!! refusing to run in the live tree ${WORK}"; exit 2 ;;
esac

chown -R hadal:hadal "${WORK}"
find "${WORK}/server" -name "*.sh" -exec sed -i 's/\r$//' {} +

# The password is read here and handed to the test process only; it is never printed.
HADAL_DB_PASSWORD="$(grep -E '^HADAL_DB_PASSWORD=' "${SERVER_ENV}" | head -1 | cut -d= -f2-)"
[[ -n "${HADAL_DB_PASSWORD}" ]] || { echo "!! no HADAL_DB_PASSWORD in ${SERVER_ENV}"; exit 2; }

psql_admin() { docker exec -i "${PG_CONTAINER}" psql -v ON_ERROR_STOP=1 -q -U hadal_app -d postgres "$@"; }

drop_db() { psql_admin -c "DROP DATABASE IF EXISTS ${DB} WITH (FORCE);" >/dev/null 2>&1 || true; }
trap drop_db EXIT

psql_admin -c "CREATE DATABASE ${DB};" >/dev/null
echo "-- disposable test database ${DB} (dropped on exit)"

ENV_FILE="${WORK}/.test-env"
install -m 600 -o hadal -g hadal /dev/null "${ENV_FILE}"
cat > "${ENV_FILE}" <<EOF
export HADAL_TEST_PG='Host=127.0.0.1;Port=19005;Database=${DB};Username=hadal_app;Password=${HADAL_DB_PASSWORD}'
# Every test that needs Postgres must run: a missing variable is a failure, not a skip.
export HADAL_TEST_PG_REQUIRED=1
export PATH="\$PATH:/usr/share/dotnet:\$HOME/.dotnet" DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1
EOF

LOG="${WORK}/test.log"
FILTER_ARG=""
[[ -n "${FILTER}" ]] && FILTER_ARG="--filter $(printf '%q' "${FILTER}")"

set +e
su hadal -s /bin/bash -c ". '${ENV_FILE}'; dotnet test '${WORK}/server/Hadal.Game.Tests/Hadal.Game.Tests.csproj' --nologo ${FILTER_ARG}" > "${LOG}" 2>&1
STATUS=$?
set -e
rm -f "${ENV_FILE}"

grep -E "Passed!|Failed!|error CS|\[FAIL\]|Assert\.|Expected:|Actual:|at Hadal\.Game\.Tests" "${LOG}" | head -100
echo "-- full log: ${LOG}"
exit ${STATUS}
