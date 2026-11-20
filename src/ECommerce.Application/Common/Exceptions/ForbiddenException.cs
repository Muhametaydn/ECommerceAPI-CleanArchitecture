namespace ECommerce.Application.Common.Exceptions;

/// <summary>An authenticated caller lacks permission for the requested resource.</summary>
public sealed class ForbiddenException(string message) : UnauthorizedAccessException(message);