
@echo off

rem **************************************************************************
rem *** ELGS AccessLog job
rem ***
rem *** CM_Build_Version=2017.03.6472.36412
rem **************************************************************************

REM **************************************************************************
REM *** Date Stamp Variable **************************************************
REM **************************************************************************
FOR /f "tokens=1,2" 	%%u in ('Date /t')  DO Set d=%%v
SET n=%time%
SET dtFULL=%d:~6,4%-%d:~0,2%-%d:~3,2%_%n:~0,2%-%n:~3,2%-%n:~6,2%
:: Check for leading space in the hour, if yes then build dtFULL again with 0+hour (0%n:~1,1%)
SET hour=%time:~0,2%
if "%hour:~0,1%" == " " SET dtFULL=%d:~6,4%-%d:~0,2%-%d:~3,2%_0%n:~1,1%-%n:~3,2%-%n:~6,2%

REM echo [%DATE% %TIME%] Running     :: %0

rem **************************************************************************
rem *** Prepare for the current run ******************************************
rem **************************************************************************
echo [%DATE% %TIME%] Preparation :: Preparing for the current run...
mkdir E:\ELGS\SYS\LOGS\Report\AccessLog >NUL 2>&1
mkdir E:\ELGS\SYS\Archive\LOGS\Report\AccessLog >NUL 2>&1
mkdir E:\ELGS\SYS\Configs\Report\AccessLog >NUL 2>&1

rem **************************************************************************
rem *** Clean up the old run *************************************************
rem **************************************************************************
echo [%DATE% %TIME%] Cleanup     :: Cleaning up the old run...
move /Y E:\ELGS\SYS\LOGS\Report\AccessLog\*.txt E:\ELGS\SYS\Archive\LOGS\Report\AccessLog\ >NUL 2>&1

rem **************************************************************************
rem *** Execute the current run **********************************************
rem **************************************************************************
echo [%DATE% %TIME%] Execution   :: Executing the current run...
E:\ELGS\SYS\Programs\AccessLog.exe E:\ELGS\SYS\Configs\Report\AccessLog\AccessLog.exe.config

rem **************************************************************************
rem *** Determine the exit code and exit appropriately ***********************
rem **************************************************************************
if [%ERRORLEVEL%] == [0] goto end
IF %ERRORLEVEL% LSS 10 goto warning_routine
goto error_routine

:end
echo [%DATE% %TIME%] Exit        :: Exiting normally (%ERRORLEVEL%)...
echo.
exit /B 0

:warning_routine
echo [%DATE% %TIME%] Exit        :: Exiting with warning status (%ERRORLEVEL%)...
echo.
exit /B %ERRORLEVEL%

:error_routine
rem *** Special case - force exit code 20 for WARN or FAIL (ELGS O&M) ********
echo [%DATE% %TIME%] Exit        :: Exiting with error status (20)...
echo.
exit /B 20
