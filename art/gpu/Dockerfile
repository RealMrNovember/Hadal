# GPU image for HADAL art jobs (doku / doku2, RTX 5060 = sm_120 → needs CUDA 12.8 builds).
FROM pytorch/pytorch:2.7.1-cuda12.8-cudnn9-runtime

RUN pip install --no-cache-dir \
      diffusers==0.33.1 transformers==4.51.3 accelerate==1.6.0 safetensors==0.5.3 \
      huggingface_hub==0.30.2 pillow

ENV HF_HOME=/work/models
WORKDIR /work
