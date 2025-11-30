using System;
using System.Text;
using System.Threading;

namespace Octopus.Tools;

public class ConsoleProgressBar : IDisposable, IProgress<Double>
{
    private const Int32 blockCount = 10;
    private readonly TimeSpan animationInterval = TimeSpan.FromSeconds(1.0 / 8);
    private const String animation = @"|/-\";

    private readonly Timer timer;

    private Double currentProgress = 0;
    private String currentText = String.Empty;
    private Boolean disposed = false;
    private Int32 animationIndex = 0;

    public ConsoleProgressBar()
    {
        timer = new Timer(TimerHandler);

        // A progress bar is only for temporary display in a console window.
        // If the console output is redirected to a file, draw nothing.
        // Otherwise, we'll end up with a lot of garbage in the target file.
        if (!Console.IsOutputRedirected)
        {
            ResetTimer();
        }
    }

    public void Report(Double value)
    {
        // Make sure value is in [0..1] range
        value = Math.Max(0, Math.Min(1, value));
        Interlocked.Exchange(ref currentProgress, value);
    }

    private void TimerHandler(Object state)
    {
        lock (timer)
        {
            if (disposed) return;

            var progressBlockCount = (Int32)(currentProgress * blockCount);
            var percent = (Int32)(currentProgress * 100);
            var text = String.Format("[{0}{1}] {2,3}% {3}",
                new String('#', progressBlockCount), new String('-', blockCount - progressBlockCount),
                percent,
                animation[animationIndex++ % animation.Length]);
            UpdateText(text);

            ResetTimer();
        }
    }

    private void UpdateText(String text)
    {
        // Get length of common portion
        var commonPrefixLength = 0;
        var commonLength = Math.Min(currentText.Length, text.Length);
        while (commonPrefixLength < commonLength && text[commonPrefixLength] == currentText[commonPrefixLength])
        {
            commonPrefixLength++;
        }

        // Backtrack to the first differing character
        var outputBuilder = new StringBuilder();
        outputBuilder.Append('\b', currentText.Length - commonPrefixLength);

        // Output new suffix
        outputBuilder.Append(text.Substring(commonPrefixLength));

        // If the new text is shorter than the old one: delete overlapping characters
        var overlapCount = currentText.Length - text.Length;
        if (overlapCount > 0)
        {
            outputBuilder.Append(' ', overlapCount);
            outputBuilder.Append('\b', overlapCount);
        }

        Console.Write(outputBuilder);
        currentText = text;
    }

    private void ResetTimer()
    {
        timer.Change(animationInterval, TimeSpan.FromMilliseconds(-1));
    }

    public void Dispose()
    {
        lock (timer)
        {
            disposed = true;
            UpdateText(String.Empty);
        }
    }
}