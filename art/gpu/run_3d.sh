#!/usr/bin/env bash
# Run one image_to_3d.py job in a container and record the whole-GPU peak (nvidia-smi, 1 s samples),
# which also counts CUDA context + allocator cache that torch.cuda.max_memory_allocated() does not.
#
#   ~/hadal-gpu/repo/art/gpu/run_3d.sh hadal-3d-hunyuan --model hunyuan-mini-turbo \
#       --image /work/out/3d/input/diver_robot_rgba.png --out /work/out/3d/hunyuan-mini-turbo
#
# The container is capped at MEM (default 10g, no extra swap) so an OOM kills the job, not the host/sshd
# (doku2 has 15 GB RAM; on doku use MEM=12g). Run one heavy job at a time. Log: <out>/run.log
set -euo pipefail
IMAGE=$1; shift
OUT_HOST=""
args=("$@")
for ((i = 0; i < ${#args[@]}; i++)); do
  [[ ${args[$i]} == --out ]] && OUT_HOST=${args[$((i + 1))]/#\/work/$HOME/hadal-gpu}
done
mkdir -p "$OUT_HOST"
base=$(nvidia-smi --query-gpu=memory.used --format=csv,noheader,nounits | head -1)
nvidia-smi --query-gpu=memory.used --format=csv,noheader,nounits -l 1 > "$OUT_HOST/smi.log" &
SMI=$!
start=$(date +%s)
set +e
docker run --rm --gpus all --shm-size 2g --memory ${MEM:-10g} --memory-swap ${MEM:-10g} -v "$HOME/hadal-gpu:/work" "$IMAGE" \
  python -X faulthandler /work/repo/art/gpu/image_to_3d.py "$@" 2>&1 | tee "$OUT_HOST/run.log" \
  | grep -E "^\[image_to_3d\]|Error|error|Fatal"
code=${PIPESTATUS[0]}
set -e
kill $SMI
peak=$(sort -n "$OUT_HOST/smi.log" | tail -1)
echo "[run_3d] exit ${code}, wall $(( $(date +%s) - start ))s, nvidia-smi peak ${peak} MiB (idle before: ${base} MiB)" \
  | tee -a "$OUT_HOST/run.log"
