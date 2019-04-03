#! /bin/sh

project_path=$(pwd)/UnityGLTF
log_file=$(pwd)/build/unity-mac.log

cached_folder=$(pwd)
upm_name=org.khronos.UnityGLTF
upm_src_folder_path=$(pwd)/UnityGLTF/Assets/UnityGLTF
upm_manifest_path=$(pwd)/scripts/package.json
upm_staging_path=$(pwd)/current-package/$upm_name
upm_zip_export_path=$(pwd)/current-package/$upm_name.zip
upm_targz_export_path=$(pwd)/current-package/$upm_name.tar.gz

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

echo "Creating .zip of UPM package"
sudo zip -r $upm_zip_export_path ./

echo "Creating .tar.gz of UPM package"
tar -zcvf $upm_targz_export_path ./

echo "Changing back to original folder $cached_folder"
cd $cached_folder

echo "Finishing with code $error_code"
exit $error_code