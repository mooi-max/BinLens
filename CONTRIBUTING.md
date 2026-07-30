# Contributing to BinLens

## Before opening a pull request

1. Keep command text, binary names, paths and parameters identical to upstream GTFOBins data.
2. Keep UI copy bilingual where applicable; do not translate command syntax.
3. Run the self-test:

   ```powershell
   dotnet run --project .\GtfobinsOffline.SelfTest\GtfobinsOffline.SelfTest.csproj -c Release
   ```

4. Keep pull requests focused and describe user-visible changes.

## Reporting bugs

Include the application version, Windows version, reproduction steps, expected behavior and actual behavior. Do not include target credentials, private host output or other sensitive data.
