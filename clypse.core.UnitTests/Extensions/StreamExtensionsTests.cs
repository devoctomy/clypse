namespace clypse.core.UnitTests.Extensions;

public static class StreamExtensionsTests
{
    public static async Task<int> ReadAllAsync(
        this Stream stream,
        Memory<byte> buffer,
        CancellationToken cancellationToken = default)
    {
        int totalRead = 0;
        while (totalRead < buffer.Length)
        {
            int bytesRead = await stream.ReadAsync(
                buffer[totalRead..],
                cancellationToken)
                .ConfigureAwait(false);
            if (bytesRead == 0)
            {
                break;
            }

            totalRead += bytesRead;
        }
        return totalRead;
    }
}