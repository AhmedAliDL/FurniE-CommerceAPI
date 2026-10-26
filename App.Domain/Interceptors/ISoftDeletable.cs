namespace App.Domain.Interceptors
{
    public interface ISoftDeletable
    {
        bool IsDeleted { get; set; }
        DateTime DeletedAt { get; set; }
    }
}
