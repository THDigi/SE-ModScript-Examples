@echo off
rem	Testing mods in DS by yourself can be done without the need to re-publish every time.
rem	You can simply update the files that are on your machine!
rem	This will only work for you, anyone else joining the server will of course download the mod from the workshop.

rem	To use:
rem	1. Copy this .bat file in the ROOT folder of your local mod (e.g. %appdata%/SpaceEngineers/Mods/YourLocalMod/<HERE>)

rem	2. Edit this variable if applicable (do not add quotes or end with backslash).
set STEAM_PATH=C:\Program Files (x86)\Steam

rem	3. Edit this with your mod's workshop id.
set WORKSHOP_ID=NumberHere

rem	Now you can run it every time you want to update the mod on DS and client.



rem 	Don't edit the below unless you really need different paths.
rem	NOTE: don't add quotes and don't end with a backslash!

set CLIENT_PATH=%STEAM_PATH%\steamapps\workshop\content\244850\%WORKSHOP_ID%
set DS_PATH=%APPDATA%\SpaceEngineersDedicated\content\244850\%WORKSHOP_ID%

rmdir "%CLIENT_PATH%" /S /Q
rmdir "%DS_PATH%" /S /Q

robocopy.exe .\ "%DS_PATH%"  *.* /S /xd .git bin obj .vs ignored /xf *.lnk *.git* *.bat *.zip *.7z *.blend* *.md *.log *.sln *.csproj *.csproj.user *.ruleset modinfo.sbmi
robocopy.exe "%DS_PATH%" "%CLIENT_PATH%" *.* /S

pause