@echo off
echo PRESS ANY KEY TO INSTALL TO LOCAL NUGET FEED
echo Remember to generate the up-to-date package.
c:\exe\nuget add .\Cadmus.Export\bin\Debug\Cadmus.Export.5.0.4.nupkg -source C:\Projects\_NuGet
c:\exe\nuget add .\Cadmus.Export.ML\bin\Debug\Cadmus.Export.ML.5.0.4.nupkg -source C:\Projects\_NuGet
c:\exe\nuget add .\Cadmus.Import\bin\Debug\Cadmus.Import.5.0.4.nupkg -source C:\Projects\_NuGet
c:\exe\nuget add .\Cadmus.Import.Proteus\bin\Debug\Cadmus.Import.Proteus.5.0.4.nupkg -source C:\Projects\_NuGet
c:\exe\nuget add .\Cadmus.Import.Excel\bin\Debug\Cadmus.Import.Excel.5.0.4.nupkg -source C:\Projects\_NuGet
pause
