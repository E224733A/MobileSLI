using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;
using MobileSLI.Configuration;
using MobileSLI.Domain.Rules;
using MobileSLI.Models;
using MobileSLI.Services.Diagnostics;
using SQLite;

namespace MobileSLI.Services;

/// <summary>
/// Service central d'accès à la base SQLite locale du téléphone.
/// Ce fichier est sensible : il gère les migrations locales, le stockage des tournées,
/// la reprise après interruption, les règles client fermé, le trajet camion, la purge,
/// l'export diagnostic et la construction du payload de synchronisation envoyé à l'API.
/// À terme, ce service devrait être découpé en services spécialisés pour réduire sa responsabilité.
/// </summary>

public sealed class DatabaseService
{
    /// <summary>
    /// Nom fixe de la base SQLite locale dans l'espace applicatif MAUI.
    /// Ne pas modifier sans prévoir une migration ou une récupération des données existantes.
    /// </summary>
    private const string DatabaseFileName = "mobile_sli.db3";

    private SQLiteAsyncConnection? _database;
    private readonly SettingsService _settings;
    private readonly DatabaseExportService _databaseExportService;

    public DatabaseService(SettingsService settings, DatabaseExportService databaseExportService)
    {
        _settings = settings;
        _databaseExportService = databaseExportService;
    }

    public DatabaseService(SettingsService settings)
        : this(settings, new DatabaseExportService())
    {
    }

    /// <summary>
    /// Emplacement physique de la base SQLite dans le dossier de données de l'application.
    /// </summary>
    private static string DatabasePath => Path.Combine(FileSystem.AppDataDirectory, DatabaseFileName);

    /// <summary>
    /// Ouvre ou réutilise la connexion SQLite locale, crée les tables si besoin,
    /// puis applique les migrations incrémentales utilisées par les versions 1.2 et 1.3 du contrat mobile.
    /// </summary>
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
        await EnsureSchemaV13TrajetAsync(_database);

        return _database;
    }

    /// <summary>
    /// Migration locale historique du contrat 1.2.
    /// Les colonnes sont ajoutées de manière tolérante pour ne pas casser les téléphones
    /// qui possèdent déjà une base créée par une version précédente de l'application.
    /// </summary>
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
        await TryAddColumnAsync(db, "LocalTourneeLigne", "LienAdresseLivraison TEXT");

        await TryAddColumnAsync(db, "LocalTourneeLigneQuantite", "QuantiteLivreePrevue INTEGER");
    }

    /// <summary>
    /// Migration locale du contrat 1.3 ajoutant le trajet camion.
    /// Ces champs sont nécessaires pour envoyer camion, kilométrage départ/arrivée
    /// et dates mobiles dans le payload final de synchronisation.
    /// </summary>
    private static async Task EnsureSchemaV13TrajetAsync(SQLiteAsyncConnection db)
    {
        await TryAddColumnAsync(db, "LocalTournee", "IdCamion TEXT");
        await TryAddColumnAsync(db, "LocalTournee", "CodeCamion TEXT");
        await TryAddColumnAsync(db, "LocalTournee", "LibelleCamion TEXT");
        await TryAddColumnAsync(db, "LocalTournee", "Immatriculation TEXT");
        await TryAddColumnAsync(db, "LocalTournee", "KilometrageDepart INTEGER");
        await TryAddColumnAsync(db, "LocalTournee", "KilometrageArrivee INTEGER");
        await TryAddColumnAsync(db, "LocalTournee", "DateDepartMobile DATETIME");
        await TryAddColumnAsync(db, "LocalTournee", "DateArriveeMobile DATETIME");
    }

    /// <summary>
    /// Ajoute une colonne SQLite si elle n'existe pas encore.
    /// Les erreurs de colonne déjà présente sont ignorées pour rendre les migrations idempotentes.
    /// </summary>
    private static async Task TryAddColumnAsync(
        SQLiteAsyncConnection db,
        string tableName,
        string columnDefinition)
    {
        try
        {
        /*
            * Les noms de table et définitions de colonne doivent rester des constantes internes de migration.
            * Ne jamais appeler cette méthode avec une valeur utilisateur ou une valeur venant de l'API.
        */
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

    /// <summary>
    /// Exporte la base SQLite locale dans le dossier de téléchargement accessible pour diagnostic.
    /// Le checkpoint WAL est tenté avant export pour intégrer les écritures récentes dans le fichier principal.
    /// </summary>
    public async Task<string> ExportDatabaseToDownloadsAsync()
    {
        var db = await GetDatabaseAsync();

        try
        {
            await db.ExecuteAsync("PRAGMA wal_checkpoint(TRUNCATE)");
        }
        catch
        {
            // Diagnostic uniquement : l'export doit rester possible même si le checkpoint échoue.
        }
        finally
        {
    /*
        * L'export ferme volontairement la connexion après le checkpoint WAL.
        * Cela force SQLite à libérer le fichier principal avant la copie de diagnostic.
        * Ne pas utiliser cet export comme opération métier pendant une synchronisation.
    */
            await db.CloseAsync();
            _database = null;
        }

        return await _databaseExportService.ExportDatabaseToDownloadsAsync(DatabasePath);
    }

    /// <summary>
    /// Enregistre une tournée complète reçue de l'API dans SQLite.
    /// Cette méthode refuse d'écraser une tournée active non synchronisée et applique les règles locales
    /// nécessaires à la reprise, à la fermeture client et à la future synchronisation.
    /// </summary>
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

        // Avant de charger une nouvelle tournée, les anciennes tournées actives hors date autorisée sont expirées.
        await ExpireOldActiveTourneesAsync(dto.DateTournee);
    /*
         * Stratégie anti-écrasement :
         * - une tournée active non synchronisée bloque le chargement d'une nouvelle tournée ;
         * - une tournée expirée doit être rechargée depuis l'API ;
         * - une tournée déjà verrouillée peut être réutilisée sans recréer de doublon local.
         * Cette règle protège les saisies terrain non envoyées.
    */
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

        var existingTournees = await db.Table<LocalTournee>()
            .Where(t => t.DateTournee == dateTournee
                        && t.CodeTournee == dto.CodeTournee
                        && t.CodeLivreur == dto.Livreur.CodeLivreur)
            .ToListAsync();

        var existing = existingTournees.FirstOrDefault(tournee =>
            !string.Equals(tournee.StatutLocal, TourneeLocalStatus.AbandonneeLocale, StringComparison.OrdinalIgnoreCase));

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

            /*
             * Chaque ligne API est copiée dans une ligne locale SQLite.
             * Les données de contexte sont conservées pour pouvoir reconstruire le payload complet
             * même si le téléphone est redémarré avant la synchronisation.
             */
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
                LienAdresseLivraison = NormalizeOptionalText(pointLivraison.LienAdresseLivraison),

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

            ApplyClosedLineDefaults(ligne);

            await db.InsertAsync(ligne);

            foreach (var quantite in BuildQuantitesForLocalStorage(ligneDto, dto.ArticlesSaisissables))
            {
                ApplyClosedQuantiteDefaults(ligne.EstFerme, quantite);
                await InsertQuantiteAsync(db, ligne.Id, quantite);
            }
        }

        // Normalisation finale défensive après insertion de toutes les lignes et quantités.
        await NormalizeClosedLinesAsync(tournee.Id);

        return tournee.Id;
    }

    /// <summary>
    /// Prépare les quantités locales d'une ligne.
    /// Si l'API ne fournit pas de quantités déjà initialisées, on crée une quantité par article saisissable.
    /// La ligne ROLLS_VIDES est ajoutée si elle manque, car elle doit rester disponible côté mobile.
    /// </summary>
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

    /// <summary>
    /// Insère une quantité locale en empêchant l'enregistrement de valeurs négatives.
    /// </summary>
    private static async Task InsertQuantiteAsync(
        SQLiteAsyncConnection db,
        int ligneId,
        QuantiteSaisieMobileDto quantite)
    {
        await db.InsertAsync(new LocalTourneeLigneQuantite
        {
            LigneId = ligneId,
            CodeArticle = quantite.CodeArticle,
            Libelle = quantite.Libelle ?? quantite.CodeArticle,
            QuantiteLivreePrevue = quantite.QuantiteLivreePrevue,
            QuantiteLivree = Math.Max(0, quantite.QuantiteLivree),
            QuantiteRecuperee = Math.Max(0, quantite.QuantiteRecuperee)
        });
    }

    /// <summary>
    /// Identifie le code article ROLLS_VIDES sans dépendre de la casse.
    /// </summary>
    private static bool IsRollsVides(string? codeArticle)
    {
        return string.Equals(codeArticle, ArticleCodes.RollsVides, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Charge une tournée locale par son identifiant SQLite.
    /// </summary>
    public async Task<LocalTournee?> GetTourneeAsync(int tourneeId)
    {
        var db = await GetDatabaseAsync();
        return await db.Table<LocalTournee>().Where(t => t.Id == tourneeId).FirstOrDefaultAsync();
    }

    /// <summary>
    /// Retourne la dernière tournée locale non abandonnée.
    /// Sert notamment aux écrans de reprise après fermeture de l'application.
    /// </summary>
    public async Task<LocalTournee?> GetLatestTourneeAsync()
    {
        var db = await GetDatabaseAsync();
        var tournees = await db.Table<LocalTournee>()
            .OrderByDescending(t => t.DateChargement)
            .ToListAsync();

        return tournees.FirstOrDefault(tournee =>
            !string.Equals(tournee.StatutLocal, TourneeLocalStatus.AbandonneeLocale, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Recherche la tournée active de la date métier autorisée.
    /// Une tournée active est non verrouillée, non synchronisée, non expirée et non abandonnée.
    /// </summary>
    public async Task<LocalTournee?> GetActiveTourneeAsync(DateTime? dateTourneeAutorisee = null)
    {
    /*
        * La date API doit être passée dès qu'elle est connue.
        * DateTime.Today est seulement un repli local pour les appels où la date métier serveur
        * n'a pas encore été récupérée.
    */
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
            && !string.Equals(tournee.StatutLocal, TourneeLocalStatus.Expiree, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(tournee.StatutLocal, TourneeLocalStatus.AbandonneeLocale, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Recherche une tournée active précise par code et date.
    /// Les tournées déjà envoyées, expirées, abandonnées ou verrouillées sont exclues.
    /// </summary>
    public async Task<LocalTournee?> GetActiveTourneeByCodeAndDateAsync(string codeTournee, DateTime dateTournee)
    {
        var db = await GetDatabaseAsync();
        var date = dateTournee.Date;
        var tournees = await db.Table<LocalTournee>()
            .Where(t => t.CodeTournee == codeTournee && t.DateTournee == date)
            .ToListAsync();

        return tournees
            .Where(t =>
                !string.Equals(t.StatutLocal, TourneeLocalStatus.Synchronisee, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(t.StatutLocal, TourneeLocalStatus.DejaSynchronisee, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(t.StatutLocal, TourneeLocalStatus.Expiree, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(t.StatutLocal, TourneeLocalStatus.AbandonneeLocale, StringComparison.OrdinalIgnoreCase)
                && !t.EstVerrouillee)
            .OrderByDescending(t => t.DateChargement)
            .FirstOrDefault();
    }

    /// <summary>
    /// Expire les tournées locales actives plus anciennes que la date métier autorisée.
    /// Cela évite qu'un livreur envoie une tournée d'une ancienne date après changement de journée côté API.
    /// </summary>
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
                && t.StatutLocal != TourneeLocalStatus.Expiree
                && t.StatutLocal != TourneeLocalStatus.AbandonneeLocale)
            .ToListAsync();

        foreach (var tournee in tournees)
        {
            tournee.StatutLocal = TourneeLocalStatus.Expiree;
            tournee.EstVerrouillee = true;
            await db.UpdateAsync(tournee);
        }

        return tournees.Count;
    }

    /// <summary>
    /// Supprime les tournées synchronisées anciennes après une durée de rétention courte.
    /// La purge porte uniquement sur les tournées verrouillées et confirmées comme reçues ou déjà reçues par l'API.
    /// </summary>
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

    /// <summary>
    /// Supprime les tournées abandonnées localement après la durée de rétention prévue.
    /// Les tournées abandonnées sont conservées temporairement pour garder une trace pendant les tests et diagnostics.
    /// </summary>
    public async Task<int> PurgeOldAbandonedTourneesAsync(int retentionDays = 30)
    {
        var db = await GetDatabaseAsync();

        if (retentionDays < 1)
        {
            retentionDays = 30;
        }

        var cutoff = DateTime.Now.AddDays(-retentionDays);

        var tournees = await db.Table<LocalTournee>()
            .Where(t => t.EstVerrouillee)
            .ToListAsync();

        var candidates = tournees
            .Where(t => IsPurgeableAbandonedTournee(t, cutoff))
            .OrderBy(t => t.DateChargement)
            .ToList();

        foreach (var tournee in candidates)
        {
            await DeleteTourneeDataAsync(tournee.Id);
        }

        return candidates.Count;
    }

    /// <summary>
    /// Vérifie si une tournée synchronisée peut être purgée selon le cutoff fourni.
    /// </summary>
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

    /// <summary>
    /// Vérifie si une tournée abandonnée localement peut être purgée selon le cutoff fourni.
    /// </summary>
    private static bool IsPurgeableAbandonedTournee(LocalTournee tournee, DateTime cutoff)
    {
        if (!tournee.EstVerrouillee)
        {
            return false;
        }

        if (!string.Equals(tournee.StatutLocal, TourneeLocalStatus.AbandonneeLocale, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return tournee.DateChargement < cutoff;
    }

    /// <summary>
    /// Renormalise toutes les lignes client fermé d'une tournée non verrouillée.
    /// Règle métier : un client fermé doit rester NON_FAIT, validé, avec commentaire standard
    /// et quantités livrées/récupérées à zéro.
    /// </summary>
    public async Task<int> NormalizeClosedLinesAsync(int tourneeId)
    {
        if (tourneeId <= 0)
        {
            return 0;
        }

        var db = await GetDatabaseAsync();
        var tournee = await db.Table<LocalTournee>()
            .Where(t => t.Id == tourneeId)
            .FirstOrDefaultAsync();

        if (tournee is null || tournee.EstVerrouillee)
        {
            return 0;
        }

        var lignesFermees = await db.Table<LocalTourneeLigne>()
            .Where(l => l.TourneeId == tourneeId && l.EstFerme)
            .ToListAsync();

        var lignesCorrigees = 0;

        foreach (var ligne in lignesFermees)
        {
            if (ApplyClosedLineDefaults(ligne))
            {
                await db.UpdateAsync(ligne);
                lignesCorrigees++;
            }

            var quantites = await db.Table<LocalTourneeLigneQuantite>()
                .Where(q => q.LigneId == ligne.Id)
                .ToListAsync();

            foreach (var quantite in quantites)
            {
                if (ApplyClosedQuantiteDefaults(ligne.EstFerme, quantite))
                {
                    await db.UpdateAsync(quantite);
                }
            }
        }

        return lignesCorrigees;
    }

    /// <summary>
    /// Applique la règle métier client fermé sur une ligne locale.
    /// La logique métier est déléguée à ClientFermeRules pour garder une source unique de vérité.
    /// </summary>
    private static bool ApplyClosedLineDefaults(LocalTourneeLigne ligne)
    {
        var normalized = ClientFermeRules.NormalizeLine(
            new ClientFermeLineState(
                ligne.EstFerme,
                ligne.StatutPassage,
                ligne.EstValidee,
                ligne.HeureValidation,
                ligne.CommentaireLivreur),
            DateTime.Now);

        var hasChanged = !string.Equals(ligne.StatutPassage, normalized.StatutPassage, StringComparison.OrdinalIgnoreCase)
            || ligne.EstValidee != normalized.EstValidee
            || ligne.HeureValidation != normalized.HeureValidation
            || !string.Equals(ligne.CommentaireLivreur, normalized.CommentaireLivreur, StringComparison.Ordinal);

        if (ligne.EstFerme)
        {
            ligne.StatutPassage = normalized.StatutPassage ?? string.Empty;
            ligne.EstValidee = normalized.EstValidee;
            ligne.HeureValidation = normalized.HeureValidation;
            ligne.CommentaireLivreur = normalized.CommentaireLivreur;
        }

        return hasChanged;
    }

    /// <summary>
    /// Applique la règle client fermé sur une quantité avant insertion locale.
    /// </summary>
    private static bool ApplyClosedQuantiteDefaults(bool estFerme, QuantiteSaisieMobileDto quantite)
    {
        var normalized = ClientFermeRules.NormalizeQuantite(
            new ClientFermeQuantiteState(
                quantite.QuantiteLivree,
                quantite.QuantiteRecuperee),
            estFerme);

        var hasChanged = quantite.QuantiteLivree != normalized.QuantiteLivree
            || quantite.QuantiteRecuperee != normalized.QuantiteRecuperee;

        if (!hasChanged)
        {
            return false;
        }

        quantite.QuantiteLivree = normalized.QuantiteLivree;
        quantite.QuantiteRecuperee = normalized.QuantiteRecuperee;

        return true;
    }

    /// <summary>
    /// Applique la règle client fermé sur une quantité déjà stockée en SQLite.
    /// </summary>
    private static bool ApplyClosedQuantiteDefaults(bool estFerme, LocalTourneeLigneQuantite quantite)
    {
        var normalized = ClientFermeRules.NormalizeQuantite(
            new ClientFermeQuantiteState(
                quantite.QuantiteLivree,
                quantite.QuantiteRecuperee),
            estFerme);

        var hasChanged = quantite.QuantiteLivree != normalized.QuantiteLivree
            || quantite.QuantiteRecuperee != normalized.QuantiteRecuperee;

        if (!hasChanged)
        {
            return false;
        }

        quantite.QuantiteLivree = normalized.QuantiteLivree;
        quantite.QuantiteRecuperee = normalized.QuantiteRecuperee;

        return true;
    }

    /// <summary>
    /// Charge les lignes d'une tournée dans l'ordre de passage.
    /// </summary>
    public async Task<List<LocalTourneeLigne>> GetLignesAsync(int tourneeId)
    {
        var db = await GetDatabaseAsync();
        return await db.Table<LocalTourneeLigne>()
            .Where(l => l.TourneeId == tourneeId)
            .OrderBy(l => l.OrdreArret)
            .ToListAsync();
    }

    /// <summary>
    /// Charge une ligne locale par son identifiant SQLite.
    /// </summary>
    public async Task<LocalTourneeLigne?> GetLigneAsync(int ligneId)
    {
        var db = await GetDatabaseAsync();
        return await db.Table<LocalTourneeLigne>().Where(l => l.Id == ligneId).FirstOrDefaultAsync();
    }

    /// <summary>
    /// Charge les quantités d'une ligne dans l'ordre des codes articles.
    /// </summary>
    public async Task<List<LocalTourneeLigneQuantite>> GetQuantitesAsync(int ligneId)
    {
        var db = await GetDatabaseAsync();
        return await db.Table<LocalTourneeLigneQuantite>()
            .Where(q => q.LigneId == ligneId)
            .OrderBy(q => q.CodeArticle)
            .ToListAsync();
    }

    /// <summary>
    /// Enregistre la saisie d'une ligne et de ses quantités.
    /// La méthode refuse les modifications si la tournée est verrouillée et remet automatiquement
    /// les valeurs client fermé dans leur état métier attendu.
    /// </summary>
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

        ApplyClosedLineDefaults(ligne);

        await db.UpdateAsync(ligne);

        foreach (var quantite in quantites)
        {
            ApplyClosedQuantiteDefaults(ligne.EstFerme, quantite);

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

    /// <summary>
    /// Persiste le camion et le kilométrage de départ sur la tournée locale.
    /// Cette persistance est indispensable pour pouvoir restaurer le trajet après navigation ou reprise de l'application.
    /// </summary>
    public async Task PersistTrajetDepartAsync(
        int tourneeId,
        CamionDto camion,
        int kilometrageDepart,
        DateTime dateDepartMobile)
    {
        if (tourneeId <= 0)
        {
            throw new InvalidOperationException("Aucune tournée locale n'est sélectionnée pour enregistrer le trajet départ.");
        }

        if (camion is null)
        {
            throw new ArgumentNullException(nameof(camion));
        }

        if (string.IsNullOrWhiteSpace(camion.IdCamion) || string.IsNullOrWhiteSpace(camion.CodeCamion))
        {
            throw new InvalidOperationException("Camion incomplet : idCamion et codeCamion sont obligatoires.");
        }

        if (kilometrageDepart < 0)
        {
            throw new InvalidOperationException("Le kilométrage départ ne peut pas être négatif.");
        }

        var db = await GetDatabaseAsync();
        var tournee = await GetTourneeAsync(tourneeId)
            ?? throw new InvalidOperationException("Tournée locale introuvable pour enregistrer le trajet départ.");

        if (tournee.EstVerrouillee)
        {
            return;
        }

        tournee.IdCamion = camion.IdCamion.Trim();
        tournee.CodeCamion = camion.CodeCamion.Trim();
        tournee.LibelleCamion = string.IsNullOrWhiteSpace(camion.LibelleCamion) ? null : camion.LibelleCamion.Trim();
        tournee.Immatriculation = string.IsNullOrWhiteSpace(camion.Immatriculation) ? null : camion.Immatriculation.Trim();
        tournee.KilometrageDepart = kilometrageDepart;
        tournee.DateDepartMobile = dateDepartMobile;

        await db.UpdateAsync(tournee);
    }

    /// <summary>
    /// Persiste le kilométrage d'arrivée sur la tournée locale.
    /// Le kilométrage arrivée ne peut pas être inférieur au kilométrage départ déjà stocké.
    /// </summary>
    public async Task PersistTrajetArriveeAsync(
        int tourneeId,
        int kilometrageArrivee,
        DateTime dateArriveeMobile)
    {
        if (tourneeId <= 0)
        {
            throw new InvalidOperationException("Aucune tournée locale n'est sélectionnée pour enregistrer le trajet arrivée.");
        }

        if (kilometrageArrivee < 0)
        {
            throw new InvalidOperationException("Le kilométrage arrivée ne peut pas être négatif.");
        }

        var db = await GetDatabaseAsync();
        var tournee = await GetTourneeAsync(tourneeId)
            ?? throw new InvalidOperationException("Tournée locale introuvable pour enregistrer le trajet arrivée.");

        if (tournee.KilometrageDepart.HasValue && kilometrageArrivee < tournee.KilometrageDepart.Value)
        {
            throw new InvalidOperationException("Le kilométrage arrivée doit être supérieur ou égal au kilométrage départ.");
        }

        if (tournee.EstVerrouillee)
        {
            return;
        }

        tournee.KilometrageArrivee = kilometrageArrivee;
        tournee.DateArriveeMobile = dateArriveeMobile;

        await db.UpdateAsync(tournee);
    }

    /// <summary>
    /// Restaure dans AppState les informations de trajet persistées en SQLite.
    /// Cette méthode permet de reprendre une tournée sans perdre camion, kilométrages et dates mobiles.
    /// </summary>
    public async Task RestaurerTrajetDansAppStateAsync(
        int tourneeId,
        AppStateService appStateService)
    {
        ArgumentNullException.ThrowIfNull(appStateService);

        var tournee = await GetTourneeAsync(tourneeId);
        if (tournee is null)
        {
            appStateService.ClearTrajet();
            return;
        }

        appStateService.ApplyTrajetFromTournee(tournee);
    }

    /// <summary>
    /// Retourne un message bloquant si le trajet camion de la tournée locale est incomplet ou incohérent.
    /// Cette validation est utilisée avant synchronisation pour éviter un payload 1.3 incomplet.
    /// </summary>
    public async Task<string?> GetTrajetBlockingValidationMessageAsync(int tourneeId)
    {
        var tournee = await GetTourneeAsync(tourneeId);
        if (tournee is null)
        {
            return "Tournée locale introuvable.";
        }

        if (string.IsNullOrWhiteSpace(tournee.IdCamion))
        {
            return "Camion manquant sur la tournée locale. Revenez au choix camion.";
        }

        if (string.IsNullOrWhiteSpace(tournee.CodeCamion))
        {
            return "Code camion manquant sur la tournée locale. Rechargez les camions puis recommencez.";
        }

        if (!tournee.KilometrageDepart.HasValue)
        {
            return "Kilométrage départ manquant sur la tournée locale. Revenez au choix camion.";
        }

        if (!tournee.DateDepartMobile.HasValue)
        {
            return "Date départ mobile manquante sur la tournée locale. Revenez au choix camion.";
        }

        if (!tournee.KilometrageArrivee.HasValue)
        {
            return "Kilométrage arrivée manquant. Saisissez le kilométrage arrivée avant l'envoi.";
        }

        if (!tournee.DateArriveeMobile.HasValue)
        {
            return "Date arrivée mobile manquante. Saisissez le kilométrage arrivée avant l'envoi.";
        }

        if (tournee.KilometrageDepart.Value < 0 || tournee.KilometrageArrivee.Value < 0)
        {
            return "Les kilométrages trajet ne peuvent pas être négatifs.";
        }

        if (tournee.KilometrageArrivee.Value < tournee.KilometrageDepart.Value)
        {
            return "Le kilométrage arrivée doit être supérieur ou égal au kilométrage départ.";
        }

        return null;
    }

    /// <summary>
    /// Reconstruit le contrat de synchronisation à partir des données SQLite locales.
    /// Cette méthode est critique : elle garantit que les informations chargées depuis l'API et les saisies livreur
    /// sont renvoyées ensemble dans le format attendu par l'API.
    /// </summary>
    public async Task<SynchronisationTourneeRequest> BuildSynchronisationRequestAsync(int tourneeId)
    {
        // Sécurité avant mapping : les lignes client fermé sont remises dans leur état métier attendu.
        await NormalizeClosedLinesAsync(tourneeId);

        var tournee = await GetTourneeAsync(tourneeId)
            ?? throw new InvalidOperationException("Aucune tournée locale trouvée.");

        var lignes = await GetLignesAsync(tourneeId);
    /*
        * Mapping critique SQLite -> contrat JSON API.
        * Les champs de contexte reçus au chargement sont renvoyés avec la saisie livreur.
        * Ne pas renommer, supprimer ou déplacer un champ sans vérifier le contrat API correspondant.
    */
        var request = new SynchronisationTourneeRequest
        {
            SchemaVersion = AppConfig.SchemaVersion,
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
                    DateFermeture = ligne.DateFermeture.HasValue
                        ? ligne.DateFermeture.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                        : null,
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

    /// <summary>
    /// Met à jour le commentaire global d'une tournée tant qu'elle n'est pas verrouillée.
    /// </summary>
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

    /// <summary>
    /// Marque une tournée comme synchronisée après confirmation de succès par l'API.
    /// La tournée devient verrouillée pour empêcher toute modification ou renvoi involontaire.
    /// </summary>
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

    /// <summary>
    /// Marque une tournée comme étant en erreur de synchronisation.
    /// Elle reste modifiable/rejouable sauf si elle est déjà verrouillée.
    /// </summary>
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

    /// <summary>
    /// Abandonne une tournée uniquement sur le téléphone.
    /// Elle est verrouillée localement pour ne plus bloquer le chargement d'une nouvelle tournée,
    /// mais elle n'est pas envoyée à l'API comme synchronisée.
    /// </summary>
    public async Task<bool> AbandonnerTourneeLocaleAsync(int tourneeId, string? raison = null)
    {
        var db = await GetDatabaseAsync();
        var tournee = await GetTourneeAsync(tourneeId);

        if (tournee is null)
        {
            return false;
        }

        if (string.Equals(tournee.StatutLocal, TourneeLocalStatus.Synchronisee, StringComparison.OrdinalIgnoreCase)
            || string.Equals(tournee.StatutLocal, TourneeLocalStatus.DejaSynchronisee, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Une tournée déjà synchronisée ne peut pas être abandonnée localement.");
        }

        var trace = string.IsNullOrWhiteSpace(raison)
            ? $"Tournée abandonnée localement le {DateTime.Now:dd/MM/yyyy HH:mm}."
            : $"Tournée abandonnée localement le {DateTime.Now:dd/MM/yyyy HH:mm}. Raison : {raison.Trim()}";

        tournee.StatutLocal = TourneeLocalStatus.AbandonneeLocale;
        tournee.EstVerrouillee = true;
        tournee.CommentaireGlobal = string.IsNullOrWhiteSpace(tournee.CommentaireGlobal)
            ? trace
            : $"{tournee.CommentaireGlobal.Trim()}{Environment.NewLine}{trace}";

        await db.UpdateAsync(tournee);
        return true;
    }

    /// <summary>
    /// Marque localement une tournée comme déjà synchronisée lorsque l'API indique qu'elle existe déjà.
    /// Ce statut évite les renvois répétés tout en conservant une trace claire du cas idempotent.
    /// </summary>
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

    /// <summary>
    /// Supprime toutes les données locales liées à une tournée : quantités, lignes, puis en-tête de tournée.
    /// L'ordre est important pour éviter de laisser des données orphelines.
    /// </summary>
/*
     * Suppression manuelle dans l'ordre enfant -> parent.
     * Ne pas inverser l'ordre sauf si des contraintes SQLite avec cascade sont ajoutées explicitement.
*/
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

    /// <summary>
    /// Normalise un texte optionnel en supprimant les espaces inutiles et en convertissant les valeurs vides en null.
    /// </summary>
    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    /// <summary>
    /// Formate une date locale avec offset pour le contrat JSON de synchronisation.
    /// Les dates sans Kind sont considérées comme locales pour éviter un décalage UTC involontaire.
    /// </summary>
    private static string FormatDateTime(DateTime value)
    {
        var local = value.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(value, DateTimeKind.Local)
            : value.ToLocalTime();

        return new DateTimeOffset(local).ToString("yyyy-MM-ddTHH:mm:sszzz", CultureInfo.InvariantCulture);
    }
}
