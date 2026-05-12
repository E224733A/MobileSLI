using System.Globalization;
using Microsoft.Maui.Storage;
using MobileSLI.Configuration;
using MobileSLI.Models;
using SQLite;

namespace MobileSLI.Services;

public sealed class DatabaseService
{
    private SQLiteAsyncConnection? _database;
    private readonly SettingsService _settings;

    public DatabaseService(SettingsService settings)
    {
        _settings = settings;
    }

    private async Task<SQLiteAsyncConnection> GetDatabaseAsync()
    {
        if (_database is not null)
        {
            return _database;
        }

        SQLitePCL.Batteries_V2.Init();

        var databasePath = Path.Combine(FileSystem.AppDataDirectory, "mobile_sli.db3");
        _database = new SQLiteAsyncConnection(databasePath);

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

        var db = await GetDatabaseAsync();
        var dateTournee = dto.DateTournee.Date;

        var existing = await db.Table<LocalTournee>()
            .Where(t => t.DateTournee == dateTournee
                        && t.CodeTournee == dto.CodeTournee
                        && t.CodeLivreur == dto.Livreur.CodeLivreur)
            .FirstOrDefaultAsync();

        if (existing is not null && existing.EstVerrouillee)
        {
            return existing.Id;
        }

        if (existing is not null)
        {
            await DeleteTourneeDataAsync(existing.Id);
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

            var quantitesInitiales = ligneDto.Saisie?.Quantites ?? [];

            if (quantitesInitiales.Count > 0)
            {
                foreach (var quantite in quantitesInitiales)
                {
                    await db.InsertAsync(new LocalTourneeLigneQuantite
                    {
                        LigneId = ligne.Id,
                        CodeArticle = quantite.CodeArticle,
                        Libelle = quantite.Libelle ?? quantite.CodeArticle,
                        QuantiteLivreePrevue = quantite.QuantiteLivreePrevue,
                        QuantiteLivree = Math.Max(0, quantite.QuantiteLivree),
                        QuantiteRecuperee = Math.Max(0, quantite.QuantiteRecuperee)
                    });
                }
            }
            else
            {
                foreach (var article in dto.ArticlesSaisissables)
                {
                    await db.InsertAsync(new LocalTourneeLigneQuantite
                    {
                        LigneId = ligne.Id,
                        CodeArticle = article.CodeArticle,
                        Libelle = article.Libelle,
                        QuantiteLivreePrevue = null,
                        QuantiteLivree = 0,
                        QuantiteRecuperee = 0
                    });
                }
            }
        }

        return tournee.Id;
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

    public async Task<LocalTournee?> GetActiveTourneeAsync()
    {
        var db = await GetDatabaseAsync();

        var tournees = await db.Table<LocalTournee>()
            .OrderByDescending(t => t.DateChargement)
            .ToListAsync();

        return tournees.FirstOrDefault(tournee =>
            !tournee.EstVerrouillee
            && !string.Equals(tournee.StatutLocal, TourneeLocalStatus.Synchronisee, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(tournee.StatutLocal, TourneeLocalStatus.DejaSynchronisee, StringComparison.OrdinalIgnoreCase));
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
                    Quantites = quantites.Select(q => new SynchronisationQuantiteRequest
                    {
                        CodeArticle = q.CodeArticle,
                        Libelle = q.Libelle,
                        QuantiteLivreePrevue = q.QuantiteLivreePrevue,
                        QuantiteLivree = q.QuantiteLivree,
                        QuantiteRecuperee = q.QuantiteRecuperee
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
