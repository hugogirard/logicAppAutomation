namespace Contoso.Application;

public record EmailRequest(string message, string from, string to);