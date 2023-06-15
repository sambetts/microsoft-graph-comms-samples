using Microsoft.Graph.Communications.Calls;
using Microsoft.Graph.Communications.Resources;
using Microsoft.Graph;
using System.Timers;
using Microsoft.Graph.Communications.Common.Telemetry;
using Microsoft.Graph.Communications.Common;
using ServiceHostedMediaBot.Common;

namespace Sample.IncidentBot.Bot;


/// <summary>
/// Base class for call handler for event handling, logging and cleanup.
/// </summary>
public class CallHandler : HeartbeatHandler
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CallHandler"/> class.
    /// </summary>
    /// <param name="bot">The bot.</param>
    /// <param name="call">The call.</param>
    public CallHandler(CallingBot bot, ICall call)
        : base(TimeSpan.FromMinutes(10), call?.GraphLogger)
    {
        this.Bot = bot;
        this.Call = call;

        this.Call.OnUpdated += this.OnCallUpdated;
        this.Call.Participants.OnUpdated += this.OnParticipantsUpdated;
    }

    /// <summary>
    /// Gets the call interface.
    /// </summary>
    public ICall Call { get; }

    /// <summary>
    /// Gets the bot.
    /// </summary>
    protected CallingBot Bot { get; }

    /// <inheritdoc/>
    protected override Task HeartbeatAsync(ElapsedEventArgs args)
    {
        return this.Call.KeepAliveAsync();
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        this.Call.OnUpdated -= this.OnCallUpdated;
        this.Call.Participants.OnUpdated -= this.OnParticipantsUpdated;

        foreach (var participant in this.Call.Participants)
        {
            participant.OnUpdated -= this.OnParticipantUpdated;
        }
    }

    /// <summary>
    /// The event handler when call is updated.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="args">The arguments.</param>
    protected virtual void CallOnUpdated(ICall sender, ResourceEventArgs<Call> args)
    {
        // do nothing in base class.
    }

    /// <summary>
    /// The event handler when participants are updated.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="args">The arguments.</param>
    protected virtual void ParticipantsOnUpdated(IParticipantCollection sender, CollectionEventArgs<IParticipant> args)
    {
        // do nothing in base class.
    }

    /// <summary>
    /// Event handler when participan is updated.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="args">The arguments.</param>
    protected virtual void ParticipantOnUpdated(IParticipant sender, ResourceEventArgs<Participant> args)
    {
        // do nothing in base class.
    }

    /// <summary>
    /// Event handler for call updated.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="args">The event arguments.</param>
    private void OnCallUpdated(ICall sender, ResourceEventArgs<Call> args)
    {
        this.CallOnUpdated(sender, args);
    }

    /// <summary>
    /// Event handler when participan is updated.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="args">The arguments.</param>
    private void OnParticipantUpdated(IParticipant sender, ResourceEventArgs<Participant> args)
    {
        this.ParticipantOnUpdated(sender, args);
    }

    /// <summary>
    /// The event handler when participants are updated.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="args">The arguments.</param>
    private void OnParticipantsUpdated(IParticipantCollection sender, CollectionEventArgs<IParticipant> args)
    {
        foreach (var participant in args.AddedResources)
        {
            participant.OnUpdated += this.OnParticipantUpdated;
        }

        foreach (var participant in args.RemovedResources)
        {
            participant.OnUpdated -= this.OnParticipantUpdated;
        }

        this.ParticipantsOnUpdated(sender, args);
    }
}

/// <summary>
/// The base class for handling heartbeats.
/// </summary>
public abstract class HeartbeatHandler : ObjectRootDisposable
{
    private System.Timers.Timer heartbeatTimer;

    /// <summary>
    /// Initializes a new instance of the <see cref="HeartbeatHandler"/> class.
    /// </summary>
    /// <param name="frequency">The frequency of the heartbeat.</param>
    /// <param name="logger">The graph logger.</param>
    public HeartbeatHandler(TimeSpan frequency, IGraphLogger logger)
        : base(logger)
    {
        // initialize the timer
        var timer = new System.Timers.Timer(frequency.TotalMilliseconds);
        timer.Enabled = true;
        timer.AutoReset = true;
        timer.Elapsed += this.HeartbeatDetected;
        this.heartbeatTimer = timer;
    }

    /// <summary>
    /// This function is called whenever the heartbeat frequency has ellapsed.
    /// </summary>
    /// <param name="args">The elapsed event args.</param>
    /// <returns>The <see cref="Task"/>.</returns>
    protected abstract Task HeartbeatAsync(ElapsedEventArgs args);

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        this.heartbeatTimer.Elapsed -= this.HeartbeatDetected;
        this.heartbeatTimer.Stop();
        this.heartbeatTimer.Dispose();
    }

    /// <summary>
    /// The heartbeat function.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="args">The elapsed event args.</param>
    private void HeartbeatDetected(object sender, ElapsedEventArgs args)
    {
        var task = $"{this.GetType().FullName}.{nameof(this.HeartbeatAsync)}(args)";
        this.GraphLogger.Verbose($"Starting running task: " + task);
        _ = Task.Run(() => this.HeartbeatAsync(args)).ForgetAndLogExceptionAsync(this.GraphLogger, task);
    }
}