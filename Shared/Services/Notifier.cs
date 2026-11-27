namespace ProfAlan.Shared.Services;

public enum NoticeTone
{
    Success,
    Error,
    Info
}

public sealed record Notice(Guid Id, string Message, NoticeTone Tone);

/// <summary>Short-lived toasts for confirmations and failures.</summary>
public sealed class Notifier
{
    private readonly List<Notice> notices = [];

    public IReadOnlyList<Notice> Notices => notices;

    public event Action? Changed;

    public void Success(string message) => Show(message, NoticeTone.Success);

    public void Error(string message) => Show(message, NoticeTone.Error);

    public void Info(string message) => Show(message, NoticeTone.Info);

    public void Dismiss(Guid id)
    {
        notices.RemoveAll(n => n.Id == id);
        Changed?.Invoke();
    }

    private void Show(string message, NoticeTone tone)
    {
        var notice = new Notice(Guid.NewGuid(), message, tone);
        notices.Add(notice);
        Changed?.Invoke();
        _ = DismissLaterAsync(notice.Id, tone == NoticeTone.Error ? 6000 : 3200);
    }

    private async Task DismissLaterAsync(Guid id, int delay)
    {
        await Task.Delay(delay);
        Dismiss(id);
    }
}
