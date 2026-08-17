@echo off
REM Add EF Core migration from batch file
dotnet ef migrations add 02_DB_Migration --project LearningEdge.Infrastructure.Migrations --startup-project LearningEdge.Api

pause


