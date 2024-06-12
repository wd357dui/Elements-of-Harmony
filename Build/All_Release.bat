del "../publish" /Q
set Pausing=True
set BuildType=Release
call Standalone.bat
call MelonLoader.bat
call BepInEx.bat
pause