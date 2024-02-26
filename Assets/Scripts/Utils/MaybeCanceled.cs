namespace Utils
{
    public record MaybeCanceled;
    public record MaybeCanceled<T>;

    public record Canceled : MaybeCanceled;
    public record Canceled<T> : MaybeCanceled<T>;

    public record Completed : MaybeCanceled;
    public record Completed<T>(T Data) : MaybeCanceled<T>;
}
