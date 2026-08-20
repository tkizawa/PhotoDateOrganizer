using System;
using System.Threading;
using System.Threading.Tasks;
using PhotoDateOrganizer.Models;

namespace PhotoDateOrganizer.Services;

public interface IPhotoOrganizerService
{
    Task<OrganizeResult> OrganizeAsync(
        string sourceDirectory,
        string destinationDirectory,
        IProgress<OrganizeProgress> progress,
        CancellationToken cancellationToken,
        CloudFileHandlingMode cloudFileMode);

    Task<OrganizeResult> OrganizeAsync(
        string sourceDirectory,
        string destinationDirectory,
        IProgress<OrganizeProgress> progress,
        CancellationToken cancellationToken,
        bool skipCloudOnlyFiles = true);
}

