#!/usr/bin/env bash

echo "Testing for $TEST_PLATFORM"

${UNITY_EXECUTABLE:-xvfb-run --auto-servernum --server-args='-screen 0 640x480x24' /opt/Unity/Editor/Unity} \
  -projectPath $PROJECT_PATH \
  -runTests \
  -testPlatform $TEST_PLATFORM \
  -testResults $(pwd)/$TEST_PLATFORM-results.xml \
  -logFile /dev/stdout \
  -batchmode

UNITY_EXIT_CODE=$?

if [ $UNITY_EXIT_CODE -eq 0 ]; then
  echo "Run succeeded, no failures occurred";
elif [ $UNITY_EXIT_CODE -eq 2 ]; then
  echo "Run succeeded, some tests failed";
elif [ $UNITY_EXIT_CODE -eq 3 ]; then
  echo "Run failure (other failure)";
else
  echo "Unexpected exit code $UNITY_EXIT_CODE";
fi

echo "cat $(pwd)/$TEST_PLATFORM-results.xml" && echo -en "travis_fold:start:test_results.1\\r"
cat $(pwd)/$TEST_PLATFORM-results.xml
echo -en "travis_fold:end:test_results.1\\r"

cat $(pwd)/$TEST_PLATFORM-results.xml | grep test-run | grep Passed
exit $UNITY_TEST_EXIT_CODE
