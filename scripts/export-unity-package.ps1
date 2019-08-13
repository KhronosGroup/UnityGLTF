#! /bin/sh

$project_path="$(pwd)\UnityGLTF"
$log_file="$(pwd)\build\unity-windows.log"
$export_path="$(pwd)\current-package\UnityGLTF.unitypackage"

$error_code=0

echo "Creating package."
$UnityExe="C:\Program Files\Unity\Hub\Editor\2017.4.30f1\Editor\Unity.exe"
$Process = Start-Process -FilePath $UnityExe -PassThru -ArgumentList `
  "-batchmode", `
  "-nographics", `
  "-silent-crashes", `
  "-logFile", "$log_file", `
  "-projectPath", "$project_path", `
  "-exportPackage", "Assets/UnityGLTF", "$export_path", `
  "-quit"
Wait-Process -Id $Process.Id
if ( $Process.ExitCode -eq 0 ) {
  echo "Created package successfully."
  $error_code=0
} else {
  echo "Creating package failed. Exited with $($Process.ExitCode)"
  $error_code=1
}

echo 'Build logs:'
# cat $log_file

echo "Finishing with code $error_code"
exit $error_code