namespace App.Domain.Enums
{
    public enum OrderStatus
    {
        Pending = 1,
        Confirmed = 2,
        Processing = 3,
        Shipped = 4,
        OutForDelivery = 5,
        Delivered = 6,
        Cancelled = 7,
        CancellationRequested = 8
    }
}
