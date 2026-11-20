namespace ECommerce.Application.Common.Exceptions;

public sealed class ConflictException : Exception
{
    public ConflictException(Exception? innerException = null)
        : base("Kayıt başka bir işlem tarafından değiştirildi. Bilgileri yenileyip tekrar deneyin.", innerException) { }
    public ConflictException(string message) : base(message) { }
}