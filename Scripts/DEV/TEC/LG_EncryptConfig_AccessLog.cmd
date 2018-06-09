RENAME E:\ELGS\DEV\CONFIGS\Report\ACCESSLOG\AccessLog.exe.config web.config
CALL c:\Windows\Microsoft.NET\Framework\v4.0.30319\aspnet_regiis.exe -pef "connectionStrings" E:\ELGS\DEV\CONFIGS\Report\ACCESSLOG
RENAME E:\ELGS\DEV\CONFIGS\Report\ACCESSLOG\web.config AccessLog.exe.config

copy E:\ELGS\DEV\CONFIGS\Report\ACCESSLOG\AccessLog.exe.config E:\ELGS\DEV\Programs

pause