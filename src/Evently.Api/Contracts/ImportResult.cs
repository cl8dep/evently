namespace Evently.Api;

public record ImportResult(int Imported, int Skipped, List<string> Errors);
