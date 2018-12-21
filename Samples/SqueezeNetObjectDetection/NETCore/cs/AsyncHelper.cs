using System.Threading.Tasks;
using Windows.Foundation;
using System.Threading;

namespace Helpers
{
    static class AsyncHelper
    {
        // Work around this problem:
        // https://github.com/Microsoft/dotnet/issues/590
        public static async Task<T> AsAsync<T>(IAsyncOperation<T> op)
        {
            T result = default;
            using (var AsyncMeSemaphore = new SemaphoreSlim(0, 1))
            {
                op.Completed += (o, s) =>
                {
                    AsyncMeSemaphore.Release();
                };
                // in case the op completes before the handler got connected we must check
                // status and complete things before waiting
                if (op.Status == AsyncStatus.Completed)
                {
                    AsyncMeSemaphore.Release();
                }
                await AsyncMeSemaphore.WaitAsync();
                result = op.GetResults();
            }

            return result;
        }

        public static async Task AsAsync(IAsyncAction op)
        {
            using (var AsyncMeSemaphore = new SemaphoreSlim(0, 1))
            {
                op.Completed += (o, s) =>
                {
                    AsyncMeSemaphore.Release();
                };
                // in case the op completes before the handler got connected we must check
                // status and complete things before waiting
                if (op.Status == AsyncStatus.Completed)
                {
                    AsyncMeSemaphore.Release();
                }
                await AsyncMeSemaphore.WaitAsync();
            }
        }

        public static async Task<TResult> AsAsync<TResult, TProgress>(IAsyncOperationWithProgress<TResult, TProgress> op)
        {
            TResult result = default;
            using (var AsyncMeSemaphore = new SemaphoreSlim(0, 1))
            {
                op.Completed += (o, s) =>
                {
                    result = o.GetResults();
                    AsyncMeSemaphore.Release();
                };
                await AsyncMeSemaphore.WaitAsync();
            }

            return result;
        }    
    }
}