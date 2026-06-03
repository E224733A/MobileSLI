using System;
using System.IO;
using System.Threading.Tasks;

#if ANDROID
using Android.Content;
using Android.Provider;
#endif

namespace MobileSLI.Services.Diagnostics;

/// <summary>
/// Service dédié à l'export de diagnostic de la base SQLite locale.
/// Il reçoit le chemin source de la base et se charge uniquement de créer
/// une copie exploitable dans le dossier Téléchargements de l'appareil.
/// </summary>
public sealed class DatabaseExportService
{
    public Task<string> ExportDatabaseToDownloadsAsync(string sourcePath)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            throw new ArgumentException("Le chemin de la base SQLite source est obligatoire.", nameof(sourcePath));
        }

        if (!File.Exists(sourcePath))
        {
            throw new FileNotFoundException("La base SQLite locale est introuvable.", sourcePath);
        }

        var exportFileName = $"mobile_sli_{DateTime.Now:yyyyMMdd_HHmmss}.db3";

#if ANDROID
        return ExportDatabaseToAndroidDownloadsAsync(sourcePath, exportFileName);
#else
        var downloadsDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Downloads");

        Directory.CreateDirectory(downloadsDirectory);

        var destinationPath = Path.Combine(downloadsDirectory, exportFileName);
        File.Copy(sourcePath, destinationPath, overwrite: true);

        return Task.FromResult(destinationPath);
#endif
    }

#if ANDROID
    private static Task<string> ExportDatabaseToAndroidDownloadsAsync(
        string sourcePath,
        string exportFileName)
    {
        var context = Android.App.Application.Context
            ?? throw new InvalidOperationException("Contexte Android indisponible.");

        if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Q)
        {
            var values = new ContentValues();
            values.Put(MediaStore.IMediaColumns.DisplayName, exportFileName);
            values.Put(MediaStore.IMediaColumns.MimeType, "application/x-sqlite3");
            values.Put(MediaStore.IMediaColumns.RelativePath, Android.OS.Environment.DirectoryDownloads);
            values.Put(MediaStore.IMediaColumns.IsPending, 1);

            var resolver = context.ContentResolver
                ?? throw new InvalidOperationException("ContentResolver Android indisponible.");

            var destinationUri = resolver.Insert(MediaStore.Downloads.ExternalContentUri, values)
                ?? throw new InvalidOperationException("Impossible de créer le fichier d'export dans Téléchargements.");

            try
            {
                using var input = File.OpenRead(sourcePath);
                using var output = resolver.OpenOutputStream(destinationUri)
                    ?? throw new InvalidOperationException("Impossible d'ouvrir le flux d'écriture Android.");

                input.CopyTo(output);
            }
            catch
            {
                resolver.Delete(destinationUri, null, null);
                throw;
            }
            finally
            {
                values.Clear();
                values.Put(MediaStore.IMediaColumns.IsPending, 0);
                resolver.Update(destinationUri, values, null, null);
            }

            return Task.FromResult($"Téléchargements/{exportFileName}");
        }

        var downloadsDirectory = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads)
            ?? throw new InvalidOperationException("Dossier Téléchargements Android indisponible.");

        Directory.CreateDirectory(downloadsDirectory.AbsolutePath);

        var destinationPath = Path.Combine(downloadsDirectory.AbsolutePath, exportFileName);
        File.Copy(sourcePath, destinationPath, overwrite: true);

        return Task.FromResult(destinationPath);
    }
#endif
}
