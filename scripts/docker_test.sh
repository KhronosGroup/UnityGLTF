#!/usr/bin/env bash

set -e

docker run \
  -e PROJECT_PATH \
  -e UNITY_LICENSE_CONTENT \
  -e TEST_PLATFORM \
  -e UNITY_USERNAME \
  -e UNITY_PASSWORD \
  -w /project/ \
  -v $(pwd):/project/ \
  $IMAGE_NAME \
  /bin/bash -c "/project/scripts/before_script.sh && /project/scripts/test.sh"
