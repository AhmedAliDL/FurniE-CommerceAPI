namespace App.Application.Common.Exceptions
{
    public class InvalidRoleException : Exception
    {
        public InvalidRoleException(string message) : base(message)
        {

        }
        public InvalidRoleException(string message, Exception innerException) : base(message, innerException)
        {

        }
    }
}
