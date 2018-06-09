RENAME E:\ELGS\SYS\CONFIGS\Report\ACCESSLOG\AccessLog.exe.config web.config
CALL c:\Windows\Microsoft.NET\Framework\v4.0.30319\aspnet_regiis.exe -pef "connectionStrings" E:\ELGS\SYS\CONFIGS\Report\ACCESSLOG
RENAME E:\ELGS\SYS\CONFIGS\Report\ACCESSLOG\web.config AccessLog.exe.config

copy E:\ELGS\SYS\CONFIGS\Report\ACCESSLOG\AccessLog.exe.config E:\ELGS\SYS\Programs

pause