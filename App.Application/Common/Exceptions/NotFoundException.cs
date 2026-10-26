namespace App.Application.Common.Exceptions
{
    public class NotFoundException(string message) : Exception(message)
    {
    }
}
