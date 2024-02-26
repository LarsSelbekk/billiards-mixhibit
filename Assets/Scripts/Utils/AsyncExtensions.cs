// Adapted from https://stackoverflow.com/a/24148785/15455509

using System.Threading;
using System.Threading.Tasks;

namespace Utils
{
    public static class AsyncExtensions
    {
        public static async Task<MaybeCanceled<T>> WithCancellation<T>(
            this Task<T> task,
            CancellationToken cancellationToken
        )
        {
            var tcs = new TaskCompletionSource<bool>();
            await using (cancellationToken.Register(s => ((TaskCompletionSource<bool>)s).TrySetResult(true), tcs))
            {
                if (task != await Task.WhenAny(task, tcs.Task))
                {
                    return new Canceled<T>();
                }
            }

            return new Completed<T>(task.Result);
        }

        public static async Task<MaybeCanceled> WithCancellation(
            this Task task,
            CancellationToken cancellationToken
        )
        {
            var tcs = new TaskCompletionSource<bool>();
            await using (cancellationToken.Register(s => ((TaskCompletionSource<bool>)s).TrySetResult(true), tcs))
            {
                if (task != await Task.WhenAny(task, tcs.Task))
                {
                    return new Canceled();
                }
            }

            return new Completed();
        }
    }
}
