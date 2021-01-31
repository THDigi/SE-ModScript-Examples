rem Edit these 3 lines only and do not add quotes!

set WORKSHOP_ID=YourWorkshopIdHere
set MOD_SCRIPT_PATH=YourScriptFolderNameHere
set STEAM_PATH=C:\Program Files (x86)\Steam

rem Do not edit below this.

set CLIENT_PATH="%STEAM_PATH%\steamapps\workshop\content\244850\%WORKSHOP_ID%\Data\Scripts\%MOD_SCRIPT_PATH%\"
set DS_PATH="%APPDATA%\SpaceEngineersDedicated\content\244850\%WORKSHOP_ID%\Data\Scripts\%MOD_SCRIPT_PATH%\"

rmdir %DS_PATH% /S /Q
rmdir %CLIENT_PATH% /S /Q

robocopy.exe .\ %DS_PATH% *.cs /xd "bin" "obj" ".vs" /S
robocopy.exe .\ %CLIENT_PATH% *.cs /xd "bin" "obj" ".vs" /S

pause