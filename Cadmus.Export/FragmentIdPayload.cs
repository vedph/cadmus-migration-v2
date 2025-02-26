using System;

namespace Cadmus.Export;

/// <summary>
/// The payload for a fragment ID mapped in a <see cref="RendererContext"/>.
/// </summary>
public record FragmentIdPayload
{
    /// <summary>
    /// Gets the identifier (a GUID) of the layer part instance containing
    /// the targeted fragment.
    /// </summary>
    public string PartId { get; init; }

    /// <summary>
    /// Gets the fragment identifier, which is unique within the layer part
    /// and contains the layer type ID + <c>:</c> + role ID + <c>@</c> +
    /// fragment index.
    /// </summary>
    public string FragmentId { get; init; }

    /// <summary>
    /// Gets the numeric identifier assigned to the fragment.
    /// </summary>
    public int Id { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="FragmentIdPayload"/> class.
    /// </summary>
    /// <param name="PartId">The part identifier.</param>
    /// <param name="FragmentId">The fragment identifier.</param>
    /// <param name="Id">The identifier.</param>
    public FragmentIdPayload(string PartId, string FragmentId, int Id)
    {
        this.PartId = PartId
            ?? throw new ArgumentNullException(nameof(PartId));
        this.FragmentId = FragmentId
            ?? throw new ArgumentNullException(nameof(FragmentId));
        this.Id = Id;
    }

    /// <summary>
    /// Converts to string.
    /// </summary>
    /// <returns>
    /// A <see cref="string" /> that represents this instance.
    /// </returns>
    public override string ToString()
    {
        return $"#{Id} = {PartId}: {FragmentId}";
    }
}
