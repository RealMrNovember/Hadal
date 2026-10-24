#!/usr/bin/env bash
# Headless Unity Editor on the Hadal CT (GameCI image, version pinned to ProjectVersion.txt).
#
#   unity.sh activate      one-time license activation (prompts for Unity ID; nothing is stored in shell history)
#   unity.sh import-license  read a UnityEntitlementLicense.xml from stdin (only works if not machine-bound)
#   unity.sh hub / hub-stop  Unity Hub GUI over noVNC for Personal-license activation on this machine
#   unity.sh compile       open the project in batchmode, import + compile scripts, exit
#   unity.sh exec <method> run a static editor method in batchmode (e.g. project/scene setup)
#   unity.sh test <mode>   run EditMode or PlayMode tests, results in artifacts/
#   unity.sh shell         bash inside the editor container
#
# The license and the Library cache live in Docker volumes so they survive container restarts.
set -euo pipefail

REPO="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
# ProjectVersion.txt is CRLF on Windows checkouts — strip the \r or the image tag is invalid.
VERSION="$(sed -n 's/^m_EditorVersion: //p' "${REPO}/ProjectSettings/ProjectVersion.txt" | tr -d '\r')"
IMAGE="unityci/editor:ubuntu-${VERSION}-base-3.2.2"
ARTIFACTS="${REPO}/artifacts"
mkdir -p "${ARTIFACTS}"

# Unity binds licenses to the machine identity; every editor/Hub container must present the same one.
IDENTITY=(--hostname hadal-unity --mac-address 02:42:ac:48:44:01)
HUB_IMAGE="hadal/unity-hub:${VERSION}"

# Fixed name: Docker refuses a second editor on the same project instead of Unity aborting mid-import.
run() {
  docker run --rm --name hadal-unity-editor ${TTY:-} "${IDENTITY[@]}" \
    -v "${REPO}:/project" \
    -v hadal_unity_license:/root/.local/share/unity3d \
    -v hadal_unity_config:/root/.config/unity3d \
    -v hadal_unity_library:/project/Library \
    -v hadal_unity_cache:/root/.cache/unity3d \
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
    run unity-editor -batchmode -nographics -projectPath /project -runTests -testPlatform "${MODE}" \
      -testResults "/project/artifacts/tests-${MODE}.xml" -logFile "/project/artifacts/tests-${MODE}.log"
    ;;
  shell)
    TTY=-it run bash
    ;;
  *)
    sed -n '2,10p' "$0"; exit 1
    ;;
esac
