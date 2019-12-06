#! /bin/sh

project_path=$(pwd)/UnityGLTF
log_file=$(pwd)/build/unity-mac.log

error_code=0

echo "Building project and running tests."
/Applications/Unity/Unity.app/Contents/MacOS/Unity \
  -batchmode \
  -nographics \
  -silent-crashes \
  -logFile "$log_file" \
  -projectPath "$project_path" \
  -runEditorTests
if [ $? = 0 ] ; then
  echo "Project built and tests run successfully."
  error_code=0
else
  echo "Project build/test run failed. Exited with $?."
  error_code=1
fi

echo 'Build logs:'
cat $log_file

echo "Finishing with code $error_code"
exit $error_code