@echo off
echo Cleaning all bin and obj folders recursively...

REM Delete all 'bin' folders
for /d /r %%i in (bin) do (
    if exist "%%i" (
        echo Deleting: %%i
        rmdir /s /q "%%i"
    )
)

REM Delete all 'obj' folders
for /d /r %%i in (obj) do (
    if exist "%%i" (
        echo Deleting: %%i
        rmdir /s /q "%%i"
    )
)

echo Done.
pause
