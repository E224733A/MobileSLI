using System.Globalization;
using Microsoft.Maui.Storage;
using MobileSLI.Configuration;
using MobileSLI.Models;
using SQLite;

#if ANDROID
using Android.Content;
using Android.Provider;
#endif

namespace MobileSLI.Services;

public sealed class DatabaseService
{
    private const string DatabaseFileName = "mobile_sli.db3";

    private SQLiteAsyncConnection? _database;
    private readonly SettingsService _settings;

    public DatabaseService(SettingsService settings)
    {
        _settings = settings;
    }

    private static string DatabasePath => Path.Combine(FileSystem.AppDataDirectory, DatabaseFileName);

    private async Task<SQLiteAsyncConnection> GetDatabaseAsync()
    {
        if (_database is not null)
        {
            return _database;
        }

        SQLitePCL.Batteries_V2.Init();

        _database = new SQLiteAsyncConnection(DatabasePath);

        await _database.CreateTableAsync<LocalTournee>();
        await _database.CreateTableAsync<LocalTourneeLigne>();
        await _database.CreateTableAsync<LocalTourneeLigneQuantite>();

        await EnsureSchemaV12Async(_database);

        return _database;
    }

    private static async Task EnsureSchemaV12Async(SQLiteAsyncConnection db)
    {
        await TryAddColumnAsync(db, "LocalTournee", "SchemaVersion TEXT DEFAULT '1.2'");

        await TryAddColumnAsync(db, "LocalTourneeLigne", "Horaire TEXT");
        await TryAddColumnAsync(db, "LocalTourneeLigne", "NomAffiche TEXT");
        await TryAddColumnAsync(db, "LocalTourneeLigne", "AdresseLigne2 TEXT");
        await TryAddColumnAsync(db, "LocalTourneeLigne", "AdresseLigne3 TEXT");
        await TryAddColumnAsync(db, "LocalTourneeLigne", "CodeTournee TEXT");
        await TryAddColumnAsync(db, "LocalTourneeLigne", "LibelleTournee TEXT");
        await TryAddColumnAsync(db, "LocalTourneeLigne", "JourTournee INTEGER");
        await TryAddColumnAsync(db, "LocalTourneeLigne", "JourLibelle TEXT");
        await TryAddColumnAsync(db, "LocalTourneeLigne", "SchemaLivraison TEXT");
        await TryAddColumnAsync(db, "LocalTourneeLigne", "JourRetourLibelle TEXT");
        await TryAddColumnAsync(db, "LocalTourneeLigne", "CodeTourneeRetour TEXT");
        await TryAddColumnAsync(db, "LocalTourneeLigne", "LibelleTourneeRetour TEXT");
        await TryAddColumnAsync(db, "LocalTourneeLigne", "ZoneDechargementAfficheeValeur TEXT");
        await TryAddColumnAsync(db, "LocalTourneeLigne", "Precision TEXT");
        await TryAddColumnAsync(db, "LocalTourneeLigne", "Cle TEXT");
        await TryAddColumnAsync(db, "LocalTourneeLigne", "CommentaireExceptionnel TEXT");
        await TryAddColumnAsync(db, "LocalTourneeLigne", "PrecisionLivreur TEXT");

        await TryAddColumnAsync(db, "LocalTourneeLigneQuantite", "QuantiteLivreePrevue INTEGER");
    }

    private static async Task TryAddColumnAsync(
        SQLiteAsyncConnection db,
        string tableName,
        string columnDefinition)
    {
        try
        {
            await db.ExecuteAsync($"ALTER TABLE {tableName} ADD COLUMN {columnDefinition}");
        }
        catch (SQLiteException exception) when (
            exception.Message.Contains("duplicate column", StringComparison.OrdinalIgnoreCase)
            || exception.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase))
        {
            // Colonne déjà présente : migration locale déjà appliquée.
        }
        catch (Exception exception) when (
            exception.Message.Contains("duplicate column", StringComparison.OrdinalIgnoreCase)
            || exception.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase))
        {
            // Compatibilité avec les exceptions différentes selon Android/SQLite.
        }
    }

    public async Task<string> ExportDatabaseToDownloadsAsync()
    {
        var db = await GetDatabaseAsync();

        try
        {
            /*
             * Le checkpoint essaye de recopier les écritures WAL dans le fichier
             * principal avant export. Sur certains téléphones Android, SQLite peut
             * renvoyer une exception au libellé trompeur "not an error".
             * Pour un export de diagnostic, ce checkpoint ne doit jamais bloquer
             * l'utilisateur : on ferme ensuite la connexion pour stabiliser la base
             * avant la copie.
             */
            await db.ExecuteAsync("PRAGMA wal_checkpoint(TRUNCATE)");
        }
        catch
        {
            // Diagnostic uniquement : l'export doit rester possible même si le checkpoint échoue.
        }
        finally
        {
            await db.CloseAsync();
            _database = null;
        }

        if (!File.Exists(DatabasePath))
        {
            throw new FileNotFoundException("La base SQLite locale est introuvable.", DatabasePath);
        }

        var exportFileName = $"mobile_sli_{DateTime.Now:yyyyMMdd_HHmmss}.db3";

#if ANDROID
        return await ExportDatabaseToAndroidDownloadsAsync(DatabasePath, exportFileName);
#else
        var downloadsDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Downloads");

        Directory.CreateDirectory(downloadsDirectory);

        var destinationPath = Path.Combine(downloadsDirectory, exportFileName);
        File.Copy(DatabasePath, destinationPath, overwrite: true);

        return destinationPath;
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
            values.Put(Android.Provider.MediaStore.IMediaColumns.DisplayName, exportFileName);
            values.Put(Android.Provider.MediaStore.IMediaColumns.MimeType, "application/x-sqlite3");
            values.Put(Android.Provider.MediaStore.IMediaColumns.RelativePath, Android.OS.Environment.DirectoryDownloads);
            values.Put(Android.Provider.MediaStore.IMediaColumns.IsPending, 1);

            var resolver = context.ContentResolver
                ?? throw new InvalidOperationException("ContentResolver Android indisponible.");

            var destinationUri = resolver.Insert(Android.Provider.MediaStore.Downloads.ExternalContentUri, values)
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
                values.Put(Android.Provider.MediaStore.IMediaColumns.IsPending, 0);
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

    public async Task<int> SaveTourneeAsync(TourneeJourDto dto)
    {
        if (dto is null)
        {
            throw new ArgumentNullException(nameof(dto));
        }

        if (string.IsNullOrWhiteSpace(dto.CodeTournee))
        {
            throw new InvalidOperationException("Le code tournée est absent de la réponse API.");
        }

        if (dto.Livreur is null || string.IsNullOrWhiteSpace(dto.Livreur.CodeLivreur))
        {
            throw new InvalidOperationException("Le livreur est absent de la réponse API.");
        }

        await ExpireOldActiveTourneesAsync(dto.DateTournee);

        var activeTournee = await GetActiveTourneeAsync(dto.DateTournee);
        if (activeTournee is not null)
        {
            throw new InvalidOperationException(
                $"Une tournée non synchronisée est déjà présente sur ce téléphone : " +
                $"{activeTournee.CodeTournee} du {activeTournee.DateTournee:dd/MM/yyyy}. " +
                "Terminez ou envoyez cette tournée avant d'en charger une nouvelle.");
        }

        var db = await GetDatabaseAsync();
        var dateTournee = dto.DateTournee.Date;

        var existing = await db.Table<LocalTournee>()
            .Where(t => t.DateTournee == dateTournee
                        && t.CodeTournee == dto.CodeTournee
                        && t.CodeLivreur == dto.Livreur.CodeLivreur)
            .FirstOrDefaultAsync();

        if (existing is not null
            && string.Equals(existing.StatutLocal, TourneeLocalStatus.Expiree, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"La tournée {existing.CodeTournee} du {existing.DateTournee:dd/MM/yyyy} est expirée sur ce téléphone. " +
                "Rechargez les tournées du jour depuis l'API.");
        }

        if (existing is not null && existing.EstVerrouillee)
        {
            return existing.Id;
        }

        if (existing is not null)
        {
            throw new InvalidOperationException(
                $"Une version locale non verrouillée de la tournée {existing.CodeTournee} du " +
                $"{existing.DateTournee:dd/MM/yyyy} existe déjà. Elle ne peut pas être remplacée automatiquement.");
        }

        var tournee = new LocalTournee
        {
            SchemaVersion = string.IsNullOrWhiteSpace(dto.SchemaVersion)
                ? AppConfig.SchemaVersion
                : dto.SchemaVersion,
            DateTournee = dateTournee,
            CodeTournee = dto.CodeTournee,
            LibelleTournee = dto.LibelleTournee,
            CodeLivreur = dto.Livreur.CodeLivreur,
            NomLivreur = dto.Livreur.NomLivreur,
            StatutLocal = TourneeLocalStatus.Chargee,
            DateChargement = DateTime.Now,
            IdSynchronisation = Guid.NewGuid().ToString(),
            EstVerrouillee = false
        };

        await db.InsertAsync(tournee);

        foreach (var ligneDto in dto.Lignes.OrderBy(l => l.OrdreArret))
        {
            var client = ligneDto.Client ?? new ClientDto();
            var pointLivraison = ligneDto.PointLivraison ?? new PointLivraisonDto();
            var tourneeInfo = ligneDto.Tournee ?? new TourneeInfoDto();
            var retour = ligneDto.Retour ?? new RetourInfoDto();
            var infosLivreur = ligneDto.InfosLivreur ?? new InfosLivreurDto();

            var commentaireExceptionnel = string.IsNullOrWhiteSpace(infosLivreur.CommentaireExceptionnel)
                ? null
                : infosLivreur.CommentaireExceptionnel.Trim();

            var ligne = new LocalTourneeLigne
            {
                TourneeId = tournee.Id,
                IdLigneSource = ligneDto.IdLigneSource,
                OrdreArret = ligneDto.OrdreArret,
                Horaire = ligneDto.Horaire,

                NumClient = client.NumClient,
                NomClient = client.NomClient,
                NomAffiche = client.NomAffiche,

                CodePDL = pointLivraison.CodePDL,
                DescriptionPDL = pointLivraison.DescriptionPDL,
                AdresseLigne1 = pointLivraison.AdresseLigne1,
                AdresseLigne2 = pointLivraison.AdresseLigne2,
                AdresseLigne3 = pointLivraison.AdresseLigne3,
                Ville = pointLivraison.Ville,
                CodePostal = pointLivraison.CodePostal,

                CodeTournee = tourneeInfo.CodeTournee,
                LibelleTournee = tourneeInfo.LibelleTournee,
                JourTournee = tourneeInfo.JourTournee,
                JourLibelle = tourneeInfo.JourLibelle,
                SchemaLivraison = tourneeInfo.SchemaLivraison,

                JourTourneeRetour = retour.JourTourneeRetour,
                JourRetourLibelle = retour.JourRetourLibelle,
                CodeTourneeRetour = retour.CodeTourneeRetour,
                LibelleTourneeRetour = retour.LibelleTourneeRetour,

                Zone = infosLivreur.Zone,
                ZoneDechargement = infosLivreur.ZoneDechargement,
                ZoneDechargementAfficheeValeur = infosLivreur.ZoneDechargementAffichee,
                Precision = infosLivreur.Precision,
                Cle = infosLivreur.Cle,

                EstFerme = infosLivreur.EstFerme,
                DateFermeture = infosLivreur.DateFermeture,
                MotifFermeture = infosLivreur.MotifFermeture,

                Instructions = infosLivreur.Instructions,
                CommentaireFiche = infosLivreur.CommentaireFiche,
                CommentaireExceptionnel = commentaireExceptionnel,

                StatutPassage = StatutPassageConstants.AFaire,
                EstValidee = false,
                HeureValidation = null,
                CommentaireLivreur = null,
                PrecisionLivreur = null
            };

            await db.InsertAsync(ligne);

            foreach (var quantite in BuildQuantitesForLocalStorage(ligneDto, dto.ArticlesSaisissables))
            {
                await InsertQuantiteAsync(db, ligne.Id, quantite);
            }
        }

        return tournee.Id;
    }

    private static List<QuantiteSaisieMobileDto> BuildQuantitesForLocalStorage(
        TourneeLigneDto ligneDto,
        IEnumerable<ArticleSaisissableDto> articlesSaisissables)
    {
        var quantites = ligneDto.Saisie?.Quantites?.ToList() ?? new List<QuantiteSaisieMobileDto>();

        if (quantites.Count == 0)
        {
            quantites = articlesSaisissables
                .Select(article => new QuantiteSaisieMobileDto
                {
                    CodeArticle = article.CodeArticle,
                    Libelle = article.Libelle,
                    QuantiteLivreePrevue = null,
                    QuantiteLivree = 0,
                    QuantiteRecuperee = 0
                })
                .ToList();
        }

        if (!quantites.Any(q => IsRollsVides(q.CodeArticle)))
        {
            quantites.Add(new QuantiteSaisieMobileDto
            {
                CodeArticle = ArticleCodes.RollsVides,
                Libelle = "Rolls vides",
                QuantiteLivreePrevue = null,
                QuantiteLivree = 0,
                QuantiteRecuperee = 0
            });
        }

        return quantites;
    }

    private static async Task InsertQuantiteAsync(
        SQLiteAsyncConnection db,
        int ligneId,
        QuantiteSaisieMobileDto quantite)
    {
        var isRollsVides = IsRollsVides(quantite.CodeArticle);

        await db.InsertAsync(new LocalTourneeLigneQuantite
        {
            LigneId = ligneId,
            CodeArticle = quantite.CodeArticle,
            Libelle = quantite.Libelle ?? quantite.CodeArticle,
            QuantiteLivreePrevue = isRollsVides ? null : quantite.QuantiteLivreePrevue,
            QuantiteLivree = isRollsVides ? 0 : Math.Max(0, quantite.QuantiteLivree),
            QuantiteRecuperee = Math.Max(0, quantite.QuantiteRecuperee)
        });
    }

    private static bool IsRollsVides(string? codeArticle)
    {
        return string.Equals(codeArticle, ArticleCodes.RollsVides, StringComparison.OrdinalIgnoreCase);
    }

    public async Task<LocalTournee?> GetTourneeAsync(int tourneeId)
    {
        var db = await GetDatabaseAsync();
        return await db.Table<LocalTournee>().Where(t => t.Id == tourneeId).FirstOrDefaultAsync();
    }

    public async Task<LocalTournee?> GetLatestTourneeAsync()
    {
        var db = await GetDatabaseAsync();
        return await db.Table<LocalTournee>()
            .OrderByDescending(t => t.DateChargement)
            .FirstOrDefaultAsync();
    }

    public async Task<LocalTournee?> GetActiveTourneeAsync(DateTime? dateTourneeAutorisee = null)
    {
        var db = await GetDatabaseAsync();
        var referenceDate = (dateTourneeAutorisee ?? DateTime.Today).Date;

        var tournees = await db.Table<LocalTournee>()
            .OrderByDescending(t => t.DateChargement)
            .ToListAsync();

        return tournees.FirstOrDefault(tournee =>
            !tournee.EstVerrouillee
            && tournee.DateTournee.Date == referenceDate
            && !string.Equals(tournee.StatutLocal, TourneeLocalStatus.Synchronisee, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(tournee.StatutLocal, TourneeLocalStatus.DejaSynchronisee, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(tournee.StatutLocal, TourneeLocalStatus.Expiree, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<int> ExpireOldActiveTourneesAsync(DateTime? dateTourneeAutorisee = null)
    {
        var db = await GetDatabaseAsync();
        var referenceDate = (dateTourneeAutorisee ?? DateTime.Today).Date;

        var tournees = await db.Table<LocalTournee>()
            .Where(t =>
                !t.EstVerrouillee
                && t.DateTournee < referenceDate
                && t.StatutLocal != TourneeLocalStatus.Synchronisee
                && t.StatutLocal != TourneeLocalStatus.DejaSynchronisee
                && t.StatutLocal != TourneeLocalStatus.Expiree)
            .ToListAsync();

        foreach (var tournee in tournees)
        {
            tournee.StatutLocal = TourneeLocalStatus.Expiree;
            tournee.EstVerrouillee = true;
            await db.UpdateAsync(tournee);
        }

        return tournees.Count;
    }

    public async Task<int> PurgeOldSynchronizedTourneesAsync(int retentionDays = 7)
    {
        var db = await GetDatabaseAsync();

        if (retentionDays < 1)
        {
            retentionDays = 7;
        }

        var cutoff = DateTime.Now.AddDays(-retentionDays);

        var tournees = await db.Table<LocalTournee>()
            .Where(t => t.EstVerrouillee)
            .ToListAsync();

        var candidates = tournees
            .Where(t => IsPurgeableSynchronizedTournee(t, cutoff))
            .OrderBy(t => t.DateEnvoi ?? t.DateChargement)
            .ToList();

        foreach (var tournee in candidates)
        {
            await DeleteTourneeDataAsync(tournee.Id);
        }

        return candidates.Count;
    }

    private static bool IsPurgeableSynchronizedTournee(LocalTournee tournee, DateTime cutoff)
    {
        if (!tournee.EstVerrouillee)
        {
            return false;
        }

        var isSynchronizedStatus =
            string.Equals(tournee.StatutLocal, TourneeLocalStatus.Synchronisee, StringComparison.OrdinalIgnoreCase)
            || string.Equals(tournee.StatutLocal, TourneeLocalStatus.DejaSynchronisee, StringComparison.OrdinalIgnoreCase);

        if (!isSynchronizedStatus)
        {
            return false;
        }

        var referenceDate = tournee.DateEnvoi ?? tournee.DateChargement;

        return referenceDate < cutoff;
    }

    public async Task<List<LocalTourneeLigne>> GetLignesAsync(int tourneeId)
    {
        var db = await GetDatabaseAsync();
        return await db.Table<LocalTourneeLigne>()
            .Where(l => l.TourneeId == tourneeId)
            .OrderBy(l => l.OrdreArret)
            .ToListAsync();
    }

    public async Task<LocalTourneeLigne?> GetLigneAsync(int ligneId)
    {
        var db = await GetDatabaseAsync();
        return await db.Table<LocalTourneeLigne>().Where(l => l.Id == ligneId).FirstOrDefaultAsync();
    }

    public async Task<List<LocalTourneeLigneQuantite>> GetQuantitesAsync(int ligneId)
    {
        var db = await GetDatabaseAsync();
        return await db.Table<LocalTourneeLigneQuantite>()
            .Where(q => q.LigneId == ligneId)
            .OrderBy(q => q.CodeArticle)
            .ToListAsync();
    }

    public async Task SaveLigneAsync(
        LocalTourneeLigne ligne,
        IEnumerable<LocalTourneeLigneQuantite> quantites)
    {
        var db = await GetDatabaseAsync();

        var tournee = await GetTourneeAsync(ligne.TourneeId);
        if (tournee?.EstVerrouillee == true)
        {
            throw new InvalidOperationException("La tournée est verrouillée après synchronisation.");
        }

        await db.UpdateAsync(ligne);

        foreach (var quantite in quantites)
        {
            if (IsRollsVides(quantite.CodeArticle))
            {
                quantite.QuantiteLivreePrevue = null;
                quantite.QuantiteLivree = 0;
            }

            if (quantite.QuantiteLivree < 0 || quantite.QuantiteRecuperee < 0)
            {
                throw new InvalidOperationException("Les quantités négatives sont interdites.");
            }

            await db.UpdateAsync(quantite);
        }

        if (tournee is not null && tournee.StatutLocal == TourneeLocalStatus.Chargee)
        {
            tournee.StatutLocal = TourneeLocalStatus.EnCours;
            await db.UpdateAsync(tournee);
        }
    }

    public async Task<SynchronisationTourneeRequest> BuildSynchronisationRequestAsync(int tourneeId)
    {
        var tournee = await GetTourneeAsync(tourneeId)
            ?? throw new InvalidOperationException("Aucune tournée locale trouvée.");

        var lignes = await GetLignesAsync(tourneeId);

        var request = new SynchronisationTourneeRequest
        {
            SchemaVersion = string.IsNullOrWhiteSpace(tournee.SchemaVersion)
                ? AppConfig.SchemaVersion
                : tournee.SchemaVersion,
            IdSynchronisation = tournee.IdSynchronisation,
            DateTournee = tournee.DateTournee.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            CodeTournee = tournee.CodeTournee,
            LibelleTournee = tournee.LibelleTournee,
            StatutSynchronisation = "ENVOYEE",
            Livreur = new SynchronisationLivreurRequest
            {
                CodeLivreur = tournee.CodeLivreur,
                NomLivreur = tournee.NomLivreur
            },
            Mobile = new SynchronisationMobileRequest
            {
                NomAppareil = _settings.DeviceName,
                VersionApplication = _settings.ApplicationVersion,
                DateChargementMobile = FormatDateTime(tournee.DateChargement),
                DateEnvoiMobile = FormatDateTime(DateTime.Now)
            },
            CommentaireGlobal = string.IsNullOrWhiteSpace(tournee.CommentaireGlobal)
                ? null
                : tournee.CommentaireGlobal.Trim(),
            Lignes = new List<SynchronisationLigneRequest>()
        };

        foreach (var ligne in lignes)
        {
            var quantites = await GetQuantitesAsync(ligne.Id);

            request.Lignes.Add(new SynchronisationLigneRequest
            {
                IdLigneSource = ligne.IdLigneSource,
                OrdreArret = ligne.OrdreArret,
                Horaire = ligne.Horaire,
                Client = new SynchronisationClientRequest
                {
                    NumClient = ligne.NumClient,
                    NomClient = ligne.NomClient,
                    NomAffiche = string.IsNullOrWhiteSpace(ligne.NomAffiche)
                        ? string.IsNullOrWhiteSpace(ligne.NomClient)
                            ? ligne.NumClient
                            : $"{ligne.NumClient} - {ligne.NomClient}"
                        : ligne.NomAffiche
                },
                PointLivraison = new SynchronisationPointLivraisonRequest
                {
                    CodePDL = ligne.CodePDL ?? string.Empty,
                    DescriptionPDL = ligne.DescriptionPDL ?? string.Empty,
                    AdresseLigne1 = ligne.AdresseLigne1,
                    AdresseLigne2 = ligne.AdresseLigne2,
                    AdresseLigne3 = ligne.AdresseLigne3,
                    Ville = ligne.Ville,
                    CodePostal = ligne.CodePostal
                },
                Tournee = new SynchronisationTourneeInfoRequest
                {
                    CodeTournee = string.IsNullOrWhiteSpace(ligne.CodeTournee)
                        ? tournee.CodeTournee
                        : ligne.CodeTournee,
                    LibelleTournee = string.IsNullOrWhiteSpace(ligne.LibelleTournee)
                        ? tournee.LibelleTournee
                        : ligne.LibelleTournee,
                    JourTournee = ligne.JourTournee,
                    JourLibelle = ligne.JourLibelle,
                    SchemaLivraison = ligne.SchemaLivraison
                },
                Retour = new SynchronisationRetourInfoRequest
                {
                    JourTourneeRetour = ligne.JourTourneeRetour,
                    JourRetourLibelle = ligne.JourRetourLibelle,
                    CodeTourneeRetour = ligne.CodeTourneeRetour,
                    LibelleTourneeRetour = ligne.LibelleTourneeRetour
                },
                InfosLivreur = new SynchronisationInfosLivreurRequest
                {
                    Instructions = ligne.Instructions,
                    CommentaireExceptionnel = ligne.CommentaireExceptionnel,
                    ZoneDechargement = ligne.ZoneDechargement,
                    ZoneDechargementAffichee = ligne.ZoneDechargementAffichee,
                    Zone = ligne.Zone,
                    Precision = ligne.Precision,
                    Cle = ligne.Cle,
                    EstFerme = ligne.EstFerme,
                    DateFermeture = ligne.DateFermeture,
                    MotifFermeture = ligne.MotifFermeture
                },
                Saisie = new SynchronisationSaisieRequest
                {
                    PrecisionLivreur = string.IsNullOrWhiteSpace(ligne.PrecisionLivreur)
                        ? null
                        : ligne.PrecisionLivreur.Trim(),
                    StatutPassage = ligne.StatutPassage,
                    CommentaireLivreur = string.IsNullOrWhiteSpace(ligne.CommentaireLivreur)
                        ? null
                        : ligne.CommentaireLivreur.Trim(),
                    HeureValidation = ligne.HeureValidation.HasValue
                        ? FormatDateTime(ligne.HeureValidation.Value)
                        : null,
                    EstValidee = ligne.EstValidee,
                    Quantites = quantites.Select(q =>
                    {
                        var isRollsVides = IsRollsVides(q.CodeArticle);

                        return new SynchronisationQuantiteRequest
                        {
                            CodeArticle = q.CodeArticle,
                            Libelle = q.Libelle,
                            QuantiteLivreePrevue = isRollsVides ? null : q.QuantiteLivreePrevue,
                            QuantiteLivree = isRollsVides ? 0 : q.QuantiteLivree,
                            QuantiteRecuperee = q.QuantiteRecuperee
                        };
                    }).ToList()
                }
            });
        }

        return request;
    }

    public async Task UpdateCommentaireGlobalAsync(int tourneeId, string? commentaireGlobal)
    {
        var db = await GetDatabaseAsync();
        var tournee = await GetTourneeAsync(tourneeId);

        if (tournee is null || tournee.EstVerrouillee)
        {
            return;
        }

        tournee.CommentaireGlobal = string.IsNullOrWhiteSpace(commentaireGlobal)
            ? null
            : commentaireGlobal.Trim();

        await db.UpdateAsync(tournee);
    }

    public async Task MarkSynchroniseeAsync(int tourneeId)
    {
        var db = await GetDatabaseAsync();
        var tournee = await GetTourneeAsync(tourneeId);

        if (tournee is null)
        {
            return;
        }

        tournee.StatutLocal = TourneeLocalStatus.Synchronisee;
        tournee.DateEnvoi = DateTime.Now;
        tournee.EstVerrouillee = true;

        await db.UpdateAsync(tournee);
    }

    public async Task MarkErreurSynchronisationAsync(int tourneeId)
    {
        var db = await GetDatabaseAsync();
        var tournee = await GetTourneeAsync(tourneeId);

        if (tournee is null || tournee.EstVerrouillee)
        {
            return;
        }

        tournee.StatutLocal = TourneeLocalStatus.ErreurSynchronisation;

        await db.UpdateAsync(tournee);
    }

    public async Task MarkDejaSynchroniseeAsync(int tourneeId)
    {
        var db = await GetDatabaseAsync();
        var tournee = await GetTourneeAsync(tourneeId);

        if (tournee is null)
        {
            return;
        }

        tournee.StatutLocal = TourneeLocalStatus.DejaSynchronisee;
        tournee.DateEnvoi = DateTime.Now;
        tournee.EstVerrouillee = true;

        await db.UpdateAsync(tournee);
    }

    private async Task DeleteTourneeDataAsync(int tourneeId)
    {
        var db = await GetDatabaseAsync();

        var lignes = await db.Table<LocalTourneeLigne>()
            .Where(l => l.TourneeId == tourneeId)
            .ToListAsync();

        foreach (var ligne in lignes)
        {
            var quantites = await db.Table<LocalTourneeLigneQuantite>()
                .Where(q => q.LigneId == ligne.Id)
                .ToListAsync();

            foreach (var quantite in quantites)
            {
                await db.DeleteAsync(quantite);
            }

            await db.DeleteAsync(ligne);
        }

        var tournee = await db.Table<LocalTournee>()
            .Where(t => t.Id == tourneeId)
            .FirstOrDefaultAsync();

        if (tournee is not null)
        {
            await db.DeleteAsync(tournee);
        }
    }

    private static string FormatDateTime(DateTime value)
    {
        var local = value.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(value, DateTimeKind.Local)
            : value.ToLocalTime();

        return new DateTimeOffset(local).ToString("yyyy-MM-ddTHH:mm:sszzz", CultureInfo.InvariantCulture);
    }
}
