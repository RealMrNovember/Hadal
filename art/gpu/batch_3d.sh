#!/usr/bin/env bash
# Whole asset set on one GPU box: SDXL concepts (+ cut-outs) for every asset in the roster, then
# TRELLIS textured 3D per asset, strictly one heavy job at a time under run_3d.sh's memory cap.
#
#   nohup ~/hadal-gpu/repo/art/gpu/batch_3d.sh set1 [id,id,...] > ~/hadal-gpu/out/set1.log 2>&1 &
#   ROSTER=creatures.json nohup ~/hadal-gpu/repo/art/gpu/batch_3d.sh creatures > ~/hadal-gpu/out/creatures.log 2>&1 &
#
# Output: ~/hadal-gpu/out/<set>/concept/<id>{,_rgba}.png and ~/hadal-gpu/out/<set>/3d/<id>/<id>_textured.glb
# Finished assets are skipped, so a stopped batch resumes where it left off.
set -uo pipefail
SET=${1:?usage: batch_3d.sh <set-name> [ids]}
ONLY=${2:-}
ROSTER_FILE=${ROSTER:-assets.json}
ROSTER=/work/repo/art/gpu/$ROSTER_FILE
HOST_OUT=$HOME/hadal-gpu/out/$SET
mkdir -p "$HOST_OUT/concept" "$HOST_OUT/3d"

ids=$(python3 -c "import json; r=json.load(open('$HOME/hadal-gpu/repo/art/gpu/$ROSTER_FILE')); o=set(filter(None,'$ONLY'.split(','))); print(' '.join(a['id'] for a in r['assets'] if not o or a['id'] in o))")

# One container per image for both stages: SDXL with CPU offload (~10 GB RAM) and onnxruntime's
# BiRefNet session both grow across images and a multi-image run is OOM-killed under the cap.
# doku2 idles at ~1.8 GB of 15.5 GB, so a 12 GB cap leaves the host room. BiRefNet weights (~1 GB)
# live in /work/models/u2net (Dockerfile.cutout) so they are downloaded once.
docker image inspect hadal-gpu-cutout >/dev/null 2>&1 || \
  docker build -q -t hadal-gpu-cutout -f "$HOME/hadal-gpu/repo/art/gpu/Dockerfile.cutout" "$HOME/hadal-gpu/repo/art/gpu" >/dev/null
stage() {
  docker run --rm --gpus all --memory 12g --memory-swap 12g -v "$HOME/hadal-gpu:/work" hadal-gpu-cutout \
    python /work/repo/art/gpu/concept.py --batch $ROSTER --out /work/out/$SET/concept --only "$1" "$2" \
    2>&1 | grep -E '^\[concept\]|Error|error|Killed'
}

for id in $ids; do
  [ -f "$HOST_OUT/concept/$id.png" ] || { echo "[batch] $(date +%T) concept $id"; stage "$id" --no-cutout; }
done
for id in $ids; do
  [ -f "$HOST_OUT/concept/${id}_rgba.png" ] || { echo "[batch] $(date +%T) cut-out $id"; stage "$id" --cutout-only; }
done

missing=0
for id in $ids; do
  [ -f "$HOST_OUT/concept/${id}_rgba.png" ] || { echo "[batch] missing concept cut-out for $id"; missing=1; }
done
[ $missing = 0 ] || { echo "[batch] stopping: fix the concept stage first"; exit 1; }

for id in $ids; do
  if [ -f "$HOST_OUT/3d/$id/${id}_textured.glb" ]; then
    echo "[batch] $id already done"
    continue
  fi
  echo "[batch] $(date +%T) trellis $id"
  "$HOME/hadal-gpu/repo/art/gpu/run_3d.sh" hadal-3d-trellis --model trellis --texture \
    --image "/work/out/$SET/concept/${id}_rgba.png" --out "/work/out/$SET/3d/$id" --name "$id" | tail -2
  # A dense concept can run the 8 GB card out of memory in the mesh decoder: once more, coarser.
  if [ ! -f "$HOST_OUT/3d/$id/${id}_textured.glb" ]; then
    echo "[batch] $(date +%T) trellis $id again with --max-voxels 9000"
    "$HOME/hadal-gpu/repo/art/gpu/run_3d.sh" hadal-3d-trellis --model trellis --texture --max-voxels 9000 \
      --image "/work/out/$SET/concept/${id}_rgba.png" --out "/work/out/$SET/3d/$id" --name "$id" | tail -2
  fi
done
echo "[batch] $(date +%T) done"
