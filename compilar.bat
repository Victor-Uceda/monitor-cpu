@echo off
REM Compila sin instalar SDK: usa el compilador que ya trae Windows 10.
REM 65001 = UTF-8 (el valor "utf8" no lo acepta csc y falla con CS2016).
"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /codepage:65001 /target:winexe /out:MonitorCpu.exe /reference:System.Management.dll App\ConstantesApp.cs Sensores\InfoBateria.cs Sensores\InfoRam.cs Sensores\ILectorFrecuenciaCpu.cs Sensores\ILectorBateria.cs Sensores\ILectorUsoCpu.cs Sensores\ILectorRam.cs Sensores\LectorFrecuenciaCpu.cs Sensores\LectorBateria.cs Sensores\LectorUsoCpu.cs Sensores\LectorRam.cs UI\GraficaHistorial.cs Sistema\GestorArranque.cs UI\VentanaPrincipal.cs App\Program.cs
if %errorlevel%==0 echo OK: MonitorCpu.exe generado
pause
