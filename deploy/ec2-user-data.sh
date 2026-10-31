#!/bin/bash
# EC2 user data for Amazon Linux 2023: installs Docker and runs the image that CI publishes to GHCR.
# Paste this into "Advanced details -> User data" when launching the instance.
set -euxo pipefail

IMAGE="ghcr.io/mahmoud-ashour96/vehicle-explorer:latest"

dnf install -y docker
systemctl enable --now docker

docker run -d \
  --name vehicle-explorer \
  --restart unless-stopped \
  --memory 512m \
  -p 80:8080 \
  "$IMAGE"
