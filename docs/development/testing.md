# Verification

Run these commands from the repository root:

```bash
dotnet test HelloWorldMvc.sln
cd ClientApp
npm run build
npm test
```

Restocking scenarios are covered by C# tests, including persistence checks with SQLite. The Gherkin files describe business behavior; they are not executed by a Gherkin runner.
