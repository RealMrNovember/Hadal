#!/usr/bin/env bash
# Runs on CT 160 as user hadal (invoked by deploy.sh). Usage: remote-deploy.sh <version-label>
set -euo pipefail

VERSION="${1:-unknown}"
SERVER=/opt/hadal/server
IMAGE=hadal-hadal-gateway
COMPOSE=(docker compose -f "${SERVER}/docker-compose.hadal.yml" --env-file "${SERVER}/.env")
export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1

healthy() {
  for _ in $(seq 1 45); do
    if curl -fs http://127.0.0.1:19001/health >/dev/null && curl -fs http://127.0.0.1:19001/manifest/version.json >/dev/null; then
      return 0
    fi
    sleep 2
  done
  return 1
}

if [[ "${SKIP_TESTS:-0}" != "1" ]]; then
  echo "-- server tests"
  set -a; . "${SERVER}/.env"; set +a
  # The store tests write and delete rows (live-ops settings, catalog versions, coupons): they get a throwaway
  # database next to the live one, never "hadal" itself (the tests refuse that name).
  TEST_DB="hadal_test_deploy_$(date +%s)_$$"
  docker exec hadal-postgres psql -q -v ON_ERROR_STOP=1 -U hadal_app -d postgres -c "CREATE DATABASE ${TEST_DB};"
  trap 'docker exec hadal-postgres psql -q -U hadal_app -d postgres -c "DROP DATABASE IF EXISTS ${TEST_DB} WITH (FORCE);" >/dev/null 2>&1 || true' EXIT
  export HADAL_TEST_PG="Host=127.0.0.1;Port=19005;Database=${TEST_DB};Username=hadal_app;Password=${HADAL_DB_PASSWORD}"
  export HADAL_TEST_PG_REQUIRED=1
  if ! dotnet test "${SERVER}/Hadal.Game.Tests/Hadal.Game.Tests.csproj" --nologo > /tmp/hadal-deploy-tests.log 2>&1; then
    grep -E "\[FAIL\]| error |Failed!" /tmp/hadal-deploy-tests.log | head -20
    echo "!! tests failed — nothing deployed (full log: /tmp/hadal-deploy-tests.log)"
    exit 1
  fi
  grep -E "Passed!" /tmp/hadal-deploy-tests.log
fi

echo "-- build"
docker image tag "${IMAGE}:latest" "${IMAGE}:rollback" 2>/dev/null || echo "   (no previous image to keep)"
"${COMPOSE[@]}" build hadal-gateway >/dev/null

echo "-- rollout"
"${COMPOSE[@]}" up -d hadal-gateway >/dev/null

if healthy; then
  echo "${VERSION} $(date -u +%FT%TZ)" > "${SERVER}/DEPLOYED_VERSION"
  echo "== deployed ${VERSION}"
  exit 0
fi

echo "!! health check failed — rolling back"
docker logs --tail 30 hadal-gateway 2>&1 | sed 's/^/   | /'
if docker image inspect "${IMAGE}:rollback" >/dev/null 2>&1; then
  docker image tag "${IMAGE}:rollback" "${IMAGE}:latest"
  "${COMPOSE[@]}" up -d --no-build --force-recreate hadal-gateway >/dev/null
  healthy && echo "== rolled back to previous image" || echo "!! rollback also unhealthy — investigate now"
fi
exit 1
