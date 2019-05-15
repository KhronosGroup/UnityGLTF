#! /bin/bash

project_path=$(pwd)/UnityGLTF
log_file=$(pwd)/build/unity-mac.log

cached_folder=$(pwd)
upm_name=org.khronos.UnityGLTF
echo "##vso[task.setvariable variable=UPM_NAME]$upm_name"
upm_src_folder_path=$(pwd)/UnityGLTF/Assets/UnityGLTF
upm_manifest_path=$(pwd)/scripts/package.json
upm_staging_path=$(pwd)/current-package/$upm_name
upm_staging_UWP_plugins_path=$upm_staging_path/UnityGLTF/Plugins/uap10.0.10586
upm_zip_export_path=$(pwd)/current-package/$upm_name.zip
upm_targz_export_path=$(pwd)/current-package/$upm_name.tar.gz

if [[ $BUILD_SOURCEBRANCH == *"refs/tags"* ]]; then
  echo "Detected refs/tags in $BUILD_SOURCEBRANCH so this must be a tagged release build."
  # Splits the string with "refs/tags", takes the second value and then 
  # swaps out any slashes for underscores
  GIT_TAG=$(echo $BUILD_SOURCEBRANCH | awk -F'refs/tags/' '{print $2}' | tr '/' '_')
  echo "Setting GIT_TAG variable to: $GIT_TAG"
  echo "##vso[task.setvariable variable=GIT_TAG]$GIT_TAG"
else
  echo "Did not detect refs/tags in $BUILD_SOURCEBRANCH so skipping GIT_TAG variable set"
fi

# msbuild spits out every single dependency dll for UWP
# These are the only files that are needed by the UPM package for Unity 2018.3+
# Including all the files in the UWP plugin directory causes name collision errors when 
# building for UWP in Unity
upm_UWP_Plugins=(
	"GLTFSerialization.dll"
	"GLTFSerialization.dll.meta"
	"GLTFSerialization.pdb"
	"Newtonsoft.Json.dll"
	"Newtonsoft.Json.dll.meta"
)

error_code=0
echo $upm_name
echo $upm_src_folder_path
echo $upm_manifest_path
echo $upm_staging_path

echo "Creating package folder"
mkdir $upm_staging_path
echo "Copying package.json"
cp $upm_manifest_path $upm_staging_path

echo "Copying package contents from $upm_src_folder_path"
cp -r $upm_src_folder_path $upm_staging_path

echo "Changing to $upm_staging_path folder"
cd $upm_staging_path

echo "Cleaning out UWP plugin DLLs that are not needed for Unity2018.3+"
find $upm_staging_UWP_plugins_path -maxdepth 1 -type f | grep -vE "$(IFS=\| && echo "${upm_UWP_Plugins[*]}")" | xargs rm

echo "Files left in $upm_staging_UWP_plugins_path"
for entry in "$upm_staging_UWP_plugins_path"/*
do
  echo "$entry"
done