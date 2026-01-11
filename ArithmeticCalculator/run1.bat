dotnet --version

if exist ".\CalculatorApp\bin" rmdir /s /q ".\CalculatorApp\bin"
if exist ".\CalculatorApp\obj" rmdir /s /q ".\CalculatorApp\obj"
if exist ".\ExpressionCalculator\bin" rmdir /s /q ".\ExpressionCalculator\bin"
if exist ".\ExpressionCalculator\obj" rmdir /s /q ".\ExpressionCalculator\obj"

dotnet restore

@REM dotnet build --configuration Debug --no-restore
@REM dotnet run --project CalculatorApp --configuration Debug --no-build

dotnet build --configuration Release --no-restore
dotnet run --project CalculatorApp --configuration Release --no-build --console

pause