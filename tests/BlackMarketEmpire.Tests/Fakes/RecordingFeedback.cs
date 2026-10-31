namespace BlackMarketEmpire.Tests.Fakes;

public sealed class RecordingFeedback : IFeedback
{
    public List<SoundCue> Played { get; } = [];

    public void Play(SoundCue cue) => Played.Add(cue);
}
