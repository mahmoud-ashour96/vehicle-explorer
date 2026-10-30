#!/bin/bash
# Run on the EC2 instance (via SSM Session Manager) to deploy the latest image published by CI.
set -euo pipefail

IMAGE="${1:-ghcr.io/OWNER/vehicle-explorer:latest}"

docker pull "$IMAGE"
docker rm -f vehicle-explorer 2>/dev/null || true
docker run -d \
  --name vehicle-explorer \
  --restart unless-stopped \
  --memory 512m \
  -p 80:8080 \
  "$IMAGE"
docker image prune -f
