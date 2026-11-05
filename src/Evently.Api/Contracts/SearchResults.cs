namespace Evently.Api;

public record SearchResults(List<SearchEvent> Events, List<NameRef> Categories, List<NameRef> Topics, List<SearchUser> Users);
