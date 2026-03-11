using AutoNumber.Model;
using CommunityToolkit.Mvvm.Messaging.Messages;
using System.Drawing;

namespace AutoNumber.Infrastructure;

/// <summary>
/// Sent by FileManager after a new (non-metadata) image is loaded and initialized.
/// LabelManager subscribes to run face-label placement.
/// </summary>
public sealed class NewImageOpenedMessage(List<Rectangle> faces)
{
    public List<Rectangle> Faces { get; } = faces;
}

/// <summary>
/// Sent by ImageModel after its own state is restored from metadata.
/// Each manager subscribes to apply its portion of the styling.
/// </summary>
public sealed class MetadataLoadedMessage(AutoNumMetaData_V1 metadata)
{
    public AutoNumMetaData_V1 Metadata { get; } = metadata;
}

/// <summary>
/// Sent by LabelManager after labels are renumbered.
/// NameManager subscribes to refresh the sorted view and reposition names.
/// </summary>
public sealed class LabelsChangedMessage;
