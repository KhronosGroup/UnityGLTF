#! /bin/bash

project_path=$(pwd)/UnityGLTF
log_file=$(pwd)/build/unity-mac.log

cached_folder=$(pwd)
upm_name=org.khronos.UnityGLTF
upm_src_folder_path=$(pwd)/UnityGLTF/Assets/UnityGLTF
upm_manifest_path=$(pwd)/scripts/package.json
upm_staging_path=$(pwd)/current-package/$upm_name
upm_staging_UWP_plugins_path=$upm_staging_path/Plugins/uap10.0.10586
upm_zip_export_path=$(pwd)/current-package/$upm_name.zip
upm_targz_export_path=$(pwd)/current-package/$upm_name.tar.gz

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
echo "Only keeping the following files:"
printf '%s\n' "${upm_UWP_Plugins[@]}"
find $upm_staging_UWP_plugins_path -maxdepth 1 -type f | grep -vE "$(IFS=\| && echo "${upm_UWP_Plugins[*]}")" | xargs -r rm

echo "Creating .zip of UPM package"
sudo zip -q -r $upm_zip_export_path ./

echo "Creating .tar.gz of UPM package"
tar -zcf $upm_targz_export_path ./

echo "Changing back to original folder $cached_folder"
cd $cached_folder

echo "Finishing with code $error_code"
exit $error_code

