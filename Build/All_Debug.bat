del "../publish" /Q
set Pausing=True
set BuildType=Debug
call Standalone.bat
call MelonLoader.bat
call BepInEx.bat
pause