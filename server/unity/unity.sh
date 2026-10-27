#!/usr/bin/env bash
# Headless Unity Editor on the Hadal CT (GameCI image, version pinned to ProjectVersion.txt).
#
#   unity.sh activate      one-time license activation (prompts for Unity ID; nothing is stored in shell history)
#   unity.sh import-license  read a UnityEntitlementLicense.xml from stdin (only works if not machine-bound)
#   unity.sh hub / hub-stop  Unity Hub GUI over noVNC for Personal-license activation on this machine
#   unity.sh compile       open the project in batchmode, import + compile scripts, exit
#   unity.sh exec <method> run a static editor method in batchmode (e.g. project/scene setup)
#   unity.sh test <mode> [category]  run EditMode or PlayMode tests, results in artifacts/
#   unity.sh acceptance    live PlayMode run vs the real gateway, gateway restart, persistence check
#   unity.sh latency ["100 200"]  live PlayMode run with simulated server latency
#   unity.sh account-e2e   account UI flow (protect, sign in elsewhere) vs a throwaway in-memory gateway
#   unity.sh android       development APK in artifacts/build/ (needs the android editor image)
#   unity.sh shell         bash inside the editor container
#
# The license, the Library cache and the Gradle cache live in Docker volumes so they survive container
# restarts. Without the Gradle volume every APK build redid all dexing and transforms (~11 of 12 minutes).
set -euo pipefail

REPO="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
# ProjectVersion.txt is CRLF on Windows checkouts — strip the \r or the image tag is invalid.
VERSION="$(sed -n 's/^m_EditorVersion: //p' "${REPO}/ProjectSettings/ProjectVersion.txt" | tr -d '\r')"
# UNITY_MODULE=android selects the image with the Android SDK/NDK; each module keeps its own
# Library volume so switching platforms never forces a full reimport of the other.
UNITY_MODULE="${UNITY_MODULE:-base}"
IMAGE="unityci/editor:ubuntu-${VERSION}-${UNITY_MODULE}-3.2.2"
LIBRARY_VOLUME="hadal_unity_library"
[[ "${UNITY_MODULE}" != base ]] && LIBRARY_VOLUME+="_${UNITY_MODULE}"
ARTIFACTS="${REPO}/artifacts"
mkdir -p "${ARTIFACTS}"

# Unity binds licenses to the machine identity; every editor/Hub container must present the same one.
IDENTITY=(--hostname hadal-unity --mac-address 02:42:ac:48:44:01)
HUB_IMAGE="hadal/unity-hub:${VERSION}"

# Fixed name: Docker refuses a second editor on the same project instead of Unity aborting mid-import.
# UNITY_NETWORK=hadal_isolated_net lets PlayMode tests reach the gateway as hadal-gateway:8080.
# HADAL_* variables pass through for GameConfigSO's editor/dev overrides.
run() {
  # Editor containers run as root; clear a stale Temp/ (lock files) the host user cannot delete.
  docker run --rm -v "${REPO}:/project" "${IMAGE}" rm -rf /project/Temp >/dev/null 2>&1 || true
  docker run --rm --name hadal-unity-editor ${TTY:-} "${IDENTITY[@]}" \
    ${UNITY_NETWORK:+--network "${UNITY_NETWORK}"} \
    -e HADAL_GATEWAY_URL -e HADAL_AUTH_PROFILE -e HADAL_FRESH_ACCOUNT -e HADAL_LOCAL_SIM -e HADAL_VERSION \
    -v "${REPO}:/project" \
    -v hadal_unity_license:/root/.local/share/unity3d \
    -v hadal_unity_config:/root/.config/unity3d \
    -v "${LIBRARY_VOLUME}:/project/Library" \
    -v hadal_unity_cache:/root/.cache/unity3d \
    -v hadal_unity_android:/root/.android \
    -v hadal_unity_gradle:/root/.gradle \
    -v "${REPO}/server/unity/gitconfig:/root/.gitconfig:ro" \
    -w /project \
    "${IMAGE}" "$@"
}

if [[ "${1:-}" != "" ]] && ! docker image inspect "${IMAGE}" >/dev/null 2>&1; then
  echo "Editor image ${IMAGE} not present yet (still downloading?). Try again later." >&2
  exit 1
fi

case "${1:-}" in
  activate)
    read -rp "Unity ID e-mail: " UNITY_EMAIL
    read -rsp "Unity ID password: " UNITY_PASSWORD; echo
    read -rp "Serial (empty for Personal): " UNITY_SERIAL
    SERIAL_ARGS=()
    [[ -n "${UNITY_SERIAL}" ]] && SERIAL_ARGS=(-serial "${UNITY_SERIAL}")
    run unity-editor -batchmode -nographics -quit -logFile - \
      -username "${UNITY_EMAIL}" -password "${UNITY_PASSWORD}" "${SERIAL_ARGS[@]}"
    ;;
  import-license)
    # Personal licenses cannot be activated from batchmode on a new machine. Pipe in the
    # UnityEntitlementLicense.xml from a Hub-activated machine:
    #   type "%LOCALAPPDATA%\Unity\licenses\UnityEntitlementLicense.xml" | ssh ... unity.sh import-license
    TTY=-i run bash -c 'mkdir -p /root/.local/share/unity3d/Unity/licenses \
      && cat > /root/.local/share/unity3d/Unity/licenses/UnityEntitlementLicense.xml \
      && chmod 600 /root/.local/share/unity3d/Unity/licenses/UnityEntitlementLicense.xml \
      && echo "License stored ($(wc -c < /root/.local/share/unity3d/Unity/licenses/UnityEntitlementLicense.xml) bytes)."'
    ;;
  hub)
    # Personal-license activation through the Hub GUI (noVNC on 127.0.0.1:6080 — reach it over an SSH tunnel).
    docker build -q -t "${HUB_IMAGE}" --build-arg "EDITOR_IMAGE=${IMAGE}" "${REPO}/server/unity/hub"
    docker rm -f hadal-unity-hub >/dev/null 2>&1 || true
    docker run -d --name hadal-unity-hub "${IDENTITY[@]}" -p 127.0.0.1:6080:6080 \
      -v hadal_unity_license:/root/.local/share/unity3d \
      -v hadal_unity_config:/root/.config/unity3d \
      "${HUB_IMAGE}"
    echo "Hub running. Tunnel: ssh -J cicibyte@dl380 -L 6080:127.0.0.1:6080 root@<ct-ip>, then open http://localhost:6080/vnc.html"
    ;;
  hub-stop)
    docker rm -f hadal-unity-hub
    ;;
  compile)
    run unity-editor -batchmode -nographics -quit -projectPath /project \
      -logFile /project/artifacts/compile.log || true
    # "will not be loaded" means the DLL compiled but Unity rejected it (e.g. a missing plugin
    # dependency) — the editor then runs with none of our code, so it is a failure too.
    grep -E "error CS|Scripts have compiler errors|will not be loaded|Reference has errors|Unable to resolve reference|Exiting batchmode" \
      "${ARTIFACTS}/compile.log" | head -50
    ! grep -qE "error CS|will not be loaded due to errors" "${ARTIFACTS}/compile.log"
    ;;
  exec)
    # Run a static editor method, e.g. Hadal.EditorTools.HadalProjectSetup.CreateBootstrapScene
    METHOD="${2:?usage: unity.sh exec <Namespace.Class.Method>}"
    run unity-editor -batchmode -nographics -quit -projectPath /project -executeMethod "${METHOD}" \
      -logFile /project/artifacts/exec.log || { grep -E "error CS|Exception|executeMethod" "${ARTIFACTS}/exec.log" | head -30; exit 1; }
    grep -E "^\[Hadal\]" "${ARTIFACTS}/exec.log" || true
    ;;
  test)
    MODE="${2:-EditMode}"
    CATEGORY="${3:-}"
    run unity-editor -batchmode -nographics -projectPath /project -runTests -testPlatform "${MODE}" \
      ${CATEGORY:+-testCategory "${CATEGORY}"} \
      -testResults "/project/artifacts/tests-${MODE}.xml" -logFile "/project/artifacts/tests-${MODE}.log"
    ;;
  acceptance)
    # Phase 1 exit criterion: real client vs real server, including a gateway restart.
    export UNITY_NETWORK=hadal_isolated_net
    export HADAL_GATEWAY_URL=ws://hadal-gateway:8080/gateway
    export HADAL_AUTH_PROFILE="acceptance-$(date +%s)"
    echo "== live loop (auth profile ${HADAL_AUTH_PROFILE})"
    "$0" test PlayMode LiveServer; rc=$?
    cp "${ARTIFACTS}/tests-PlayMode.xml" "${ARTIFACTS}/tests-LiveServer.xml" 2>/dev/null
    [ $rc = 0 ] || exit $rc
    echo "== restarting gateway"
    docker restart hadal-gateway >/dev/null
    for _ in $(seq 1 30); do curl -fs http://127.0.0.1:19001/health >/dev/null && break; sleep 1; done
    echo "== persistence after restart"
    "$0" test PlayMode Persistence
    ;;
  account-e2e)
    # Account flow through the real UI against a THROWAWAY gateway: same image as production but
    # in-memory stores (no database), its own random JWT secret, an isolated network, removed
    # afterwards. Never touches the live gateway or its accounts.
    NET=hadal_account_e2e
    docker network create "${NET}" >/dev/null 2>&1 || true
    docker rm -f hadal-gateway-e2e >/dev/null 2>&1 || true
    docker run -d --name hadal-gateway-e2e --network "${NET}" \
      -e ConnectionStrings__Hadal= \
      -e Auth__JwtSecret="e2e-$(head -c 32 /dev/urandom | base64 | tr -d '\n')" \
      -e Auth__GuestLoginsPerHour=1000 -e Auth__TransferLoginsPerHour=1000 \
      hadal-hadal-gateway >/dev/null
    trap 'docker rm -f hadal-gateway-e2e >/dev/null 2>&1; docker network rm "${NET}" >/dev/null 2>&1' EXIT
    for _ in $(seq 1 30); do docker logs hadal-gateway-e2e 2>&1 | grep -q "Now listening" && break; sleep 1; done
    export UNITY_NETWORK="${NET}"
    export HADAL_GATEWAY_URL=ws://hadal-gateway-e2e:8080/gateway
    "$0" test PlayMode AccountFlow; rc=$?
    cp "${ARTIFACTS}/tests-PlayMode.xml" "${ARTIFACTS}/tests-AccountFlow.xml" 2>/dev/null
    exit $rc
    ;;
  latency)
    # Live loop with every server frame delayed (Phase 0-R exit criterion: 100 and 200 ms).
    COMPOSE=(docker compose -f "${REPO}/server/docker-compose.hadal.yml" --env-file "${REPO}/server/.env")
    set_latency() {
      HADAL_SIM_LATENCY_MS="$1" "${COMPOSE[@]}" up -d --no-deps --force-recreate hadal-gateway >/dev/null 2>&1
      for _ in $(seq 1 30); do curl -fs http://127.0.0.1:19001/health >/dev/null && break; sleep 1; done
    }
    export UNITY_NETWORK=hadal_isolated_net
    export HADAL_GATEWAY_URL=ws://hadal-gateway:8080/gateway
    rc=0
    for ms in ${2:-100 200}; do
      echo "== live loop at ${ms} ms"
      set_latency "${ms}"
      HADAL_AUTH_PROFILE="latency-${ms}-$(date +%s)" "$0" test PlayMode LiveServer || rc=1
      cp "${ARTIFACTS}/tests-PlayMode.xml" "${ARTIFACTS}/tests-Latency${ms}.xml" 2>/dev/null
    done
    echo "== latency off"
    set_latency 0
    exit $rc
    ;;
  android)
    # Development APK → artifacts/build/hadal-dev.apk (debug-signed, sideload only).
    UNITY_MODULE=android "$0" exec Hadal.EditorTools.HadalBuild.BuildAndroidDev
    ls -la "${ARTIFACTS}/build/" 2>/dev/null
    ;;
  shell)
    TTY=-it run bash
    ;;
  *)
    sed -n '2,10p' "$0"; exit 1
    ;;
esac
