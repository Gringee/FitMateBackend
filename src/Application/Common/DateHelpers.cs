namespace Application.Common;

public static class DateHelpers 
{
    public static (DateTime From, DateTime To) NormalizeRange(DateTime from, DateTime to)
    {
        var f = from.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(from, DateTimeKind.Utc) : from.ToUniversalTime();
        var t = to.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(to, DateTimeKind.Utc) : to.ToUniversalTime();
        return (f, t);
    }
}
