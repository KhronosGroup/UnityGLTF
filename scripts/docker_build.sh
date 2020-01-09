#!/usr/bin/env bash

set -e

docker run \
  -e BUILD_NAME \
  -e PROJECT_PATH \
  -e UNITY_LICENSE_CONTENT \
  -e BUILD_TARGET \
  -e UNITY_USERNAME \
  -e UNITY_PASSWORD \
  -w /project/ \
  -v $(pwd):/project/ \
  $IMAGE_NAME \
  /bin/bash -c "/project/scripts/before_script.sh && /project/scripts/build.sh"
