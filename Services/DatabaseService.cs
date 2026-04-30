using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Storage;
using SQLite;
using TourneesMobile.Models;

namespace TourneesMobile.Services;

public sealed class DatabaseService
{
    private SQLiteAsyncConnection? _database;

    private async Task<SQLiteAsyncConnection> GetDatabaseAsync()
    {
        if (_database is not null)
            return _database;

        var path = Path.Combine(FileSystem.AppDataDirectory, "tournees_mobile.db3");
        _database = new SQLiteAsyncConnection(path, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);
        await _database.CreateTableAsync<TourneeEntity>();
        await _database.CreateTableAsync<ArretEntity>();
        return _database;
    }

    public async Task<TourneeEntity?> GetTourneeActiveAsync()
    {
        var db = await GetDatabaseAsync();

        return await db.Table<TourneeEntity>()
            .OrderByDescending(t => t.DateChargementMobile)
            .FirstOrDefaultAsync();
    }

    public async Task<List<ArretEntity>> GetArretsAsync(string idTourneeLocale)
    {
        var db = await GetDatabaseAsync();

        var arrets = await db.Table<ArretEntity>()
            .Where(a => a.IdTourneeLocale == idTourneeLocale)
            .ToListAsync();

        return arrets
            .OrderBy(a => a.OrdreArret ?? int.MaxValue)
            .ThenBy(a => a.NomClient)
            .ToList();
    }

    public async Task<ArretEntity?> GetArretAsync(string idLigneSource)
    {
        var db = await GetDatabaseAsync();
        return await db.FindAsync<ArretEntity>(idLigneSource);
    }

    public async Task SaveTourneeFromApiAsync(TourneeMobileDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.DateTournee))
            throw new InvalidOperationException("La date de tournée est absente.");
        if (string.IsNullOrWhiteSpace(dto.CodeTournee))
            throw new InvalidOperationException("Le code tournée est absent.");
        if (string.IsNullOrWhiteSpace(dto.Livreur.CodeLivreur))
            throw new InvalidOperationException("Le code livreur est absent.");

        var idTourneeLocale = BuildTourneeId(dto.DateTournee, dto.CodeTournee, dto.Livreur.CodeLivreur);

        var tournee = new TourneeEntity
        {
            IdTourneeLocale = idTourneeLocale,
            SchemaVersion = string.IsNullOrWhiteSpace(dto.SchemaVersion) ? "1.0" : dto.SchemaVersion,
            IdSynchronisation = Guid.NewGuid().ToString(),
            DateTournee = dto.DateTournee,
            CodeTournee = dto.CodeTournee,
            LibelleTournee = dto.LibelleTournee,
            CodeLivreur = dto.Livreur.CodeLivreur,
            NomLivreur = dto.Livreur.NomLivreur,
            DateChargementMobile = DateTime.Now,
            StatutSynchronisation = StatutSynchronisation.NonEnvoyee,
            EstVerrouillee = false
        };

        var arrets = dto.Lignes.Select(l => MapArret(idTourneeLocale, l)).ToList();

        var db = await GetDatabaseAsync();

        await db.ExecuteAsync("DELETE FROM arrets WHERE IdTourneeLocale = ?", idTourneeLocale);
        await db.ExecuteAsync("DELETE FROM tournees WHERE IdTourneeLocale = ?", idTourneeLocale);
        await db.InsertAsync(tournee);

        if (arrets.Count > 0)
            await db.InsertAllAsync(arrets);
    }

    public async Task SaveTourneeDemoAsync(TourneeMobileDto dto)
    {
        await SaveTourneeFromApiAsync(dto);
    }

    public async Task UpdateArretAsync(ArretEntity arret)
    {
        if (arret.NbExpes < 0 || arret.NbRolls < 0 || arret.NbVetements < 0 || arret.NbTapis < 0 || arret.NbSacs < 0 || arret.NbRecuperes < 0)
            throw new InvalidOperationException("Les quantités négatives sont interdites.");

        if (StatutPassage.DemandeCommentaire(arret.StatutPassage) && string.IsNullOrWhiteSpace(arret.CommentaireLivreur))
            throw new InvalidOperationException("Un commentaire est obligatoire pour un statut NON_FAIT ou ANOMALIE.");

        var db = await GetDatabaseAsync();
        await db.UpdateAsync(arret);
    }

    public async Task SetCommentaireGlobalAsync(string idTourneeLocale, string? commentaireGlobal)
    {
        var db = await GetDatabaseAsync();
        var tournee = await db.FindAsync<TourneeEntity>(idTourneeLocale);
        if (tournee is null)
            return;

        tournee.CommentaireGlobal = string.IsNullOrWhiteSpace(commentaireGlobal) ? null : commentaireGlobal.Trim();
        await db.UpdateAsync(tournee);
    }

    public async Task MarkSynchroniseeAsync(string idTourneeLocale, string? idSynchronisation = null)
    {
        var db = await GetDatabaseAsync();
        var tournee = await db.FindAsync<TourneeEntity>(idTourneeLocale);
        if (tournee is null)
            return;

        tournee.StatutSynchronisation = StatutSynchronisation.Envoyee;
        tournee.EstVerrouillee = true;
        tournee.DateEnvoiMobile = DateTime.Now;
        if (!string.IsNullOrWhiteSpace(idSynchronisation))
            tournee.IdSynchronisation = idSynchronisation;

        await db.UpdateAsync(tournee);
    }

    public async Task MarkErreurAsync(string idTourneeLocale)
    {
        var db = await GetDatabaseAsync();
        var tournee = await db.FindAsync<TourneeEntity>(idTourneeLocale);
        if (tournee is null)
            return;

        tournee.StatutSynchronisation = StatutSynchronisation.Erreur;
        await db.UpdateAsync(tournee);
    }

    public async Task<SynchronisationTourneeRequest> BuildSynchronisationRequestAsync(string idTourneeLocale, bool marquerDateEnvoi = true)
    {
        var db = await GetDatabaseAsync();
        var tournee = await db.FindAsync<TourneeEntity>(idTourneeLocale)
            ?? throw new InvalidOperationException("Aucune tournée locale trouvée.");

        var arrets = await GetArretsAsync(idTourneeLocale);

        if (arrets.Count == 0)
            throw new InvalidOperationException("La tournée ne contient aucun arrêt.");

        foreach (var arret in arrets)
        {
            if (!arret.EstValidee || arret.HeureValidation is null)
                throw new InvalidOperationException($"L'arrêt {arret.OrdreArret} - {arret.NomAfficheCourt} n'est pas validé.");

            if (StatutPassage.DemandeCommentaire(arret.StatutPassage) && string.IsNullOrWhiteSpace(arret.CommentaireLivreur))
                throw new InvalidOperationException($"Un commentaire est obligatoire pour l'arrêt {arret.OrdreArret}.");
        }

        var now = DateTime.Now;
        if (marquerDateEnvoi)
        {
            tournee.DateEnvoiMobile = now;
            await db.UpdateAsync(tournee);
        }

        return new SynchronisationTourneeRequest
        {
            SchemaVersion = "1.0",
            IdSynchronisation = tournee.IdSynchronisation,
            DateTournee = tournee.DateTournee,
            CodeTournee = tournee.CodeTournee,
            LibelleTournee = tournee.LibelleTournee,
            Livreur = new LivreurDto
            {
                CodeLivreur = tournee.CodeLivreur,
                NomLivreur = tournee.NomLivreur
            },
            Mobile = new MobileDto
            {
                NomAppareil = DeviceInfo.Current.Name,
                VersionApplication = AppInfo.Current.VersionString,
                DateChargementMobile = tournee.DateChargementMobile,
                DateEnvoiMobile = now
            },
            CommentaireGlobal = tournee.CommentaireGlobal,
            Lignes = arrets.Select(MapSynchronisationLigne).ToList()
        };
    }

    public async Task<string> BuildSynchronisationJsonPreviewAsync(string idTourneeLocale)
    {
        var request = await BuildSynchronisationRequestAsync(idTourneeLocale, marquerDateEnvoi: false);
        return System.Text.Json.JsonSerializer.Serialize(request, new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web)
        {
            WriteIndented = true
        });
    }

    public static string BuildTourneeId(string dateTournee, string codeTournee, string codeLivreur) =>
        $"{dateTournee}|{codeTournee}|{codeLivreur}";

    private static ArretEntity MapArret(string idTourneeLocale, TourneeLigneMobileDto ligne)
    {
        var saisie = ligne.Saisie ?? new SaisieDto();

        return new ArretEntity
        {
            IdLigneSource = string.IsNullOrWhiteSpace(ligne.IdLigneSource)
                ? $"{idTourneeLocale}|{ligne.Client.NumClient}|{ligne.PointLivraison.CodePDL}"
                : ligne.IdLigneSource,
            IdTourneeLocale = idTourneeLocale,
            OrdreArret = ligne.OrdreArret,
            Horaire = ligne.Horaire,
            NumClient = ligne.Client.NumClient,
            NomClient = ligne.Client.NomClient,
            NomAffiche = ligne.Client.NomAffiche,
            CodePDL = ligne.PointLivraison.CodePDL,
            DescriptionPDL = ligne.PointLivraison.DescriptionPDL,
            AdresseLigne1 = ligne.PointLivraison.AdresseLigne1,
            AdresseLigne2 = ligne.PointLivraison.AdresseLigne2,
            AdresseLigne3 = ligne.PointLivraison.AdresseLigne3,
            Ville = ligne.PointLivraison.Ville,
            CodePostal = ligne.PointLivraison.CodePostal,
            SchemaLivraison = ligne.InfosLivreur?.SchemaLivraison,
            Instructions = ligne.InfosLivreur?.Instructions,
            CommentaireFiche = ligne.InfosLivreur?.CommentaireFiche,
            ZoneDechargement = ligne.InfosLivreur?.ZoneDechargement,
            Zone = ligne.InfosLivreur?.Zone,
            Precision = ligne.InfosLivreur?.Precision,
            TypeLinge = ligne.InfosLivreur?.TypeLinge,
            CodeTourneeRetour = ligne.Retour?.CodeTourneeRetour,
            LibelleTourneeRetour = ligne.Retour?.LibelleTourneeRetour,
            NbExpes = saisie.NbExpes,
            NbRolls = saisie.NbRolls,
            NbVetements = saisie.NbVetements,
            NbTapis = saisie.NbTapis,
            NbSacs = saisie.NbSacs,
            NbRecuperes = saisie.NbRecuperes,
            PrecisionLivreur = saisie.PrecisionLivreur,
            StatutPassage = string.IsNullOrWhiteSpace(saisie.StatutPassage) ? StatutPassage.AFaire : saisie.StatutPassage,
            CommentaireLivreur = saisie.CommentaireLivreur,
            HeureValidation = saisie.HeureValidation,
            EstValidee = saisie.EstValidee
        };
    }

    private static SynchronisationLigneDto MapSynchronisationLigne(ArretEntity arret) => new()
    {
        IdLigneSource = arret.IdLigneSource,
        OrdreArret = arret.OrdreArret,
        Client = new ClientDto
        {
            NumClient = arret.NumClient,
            NomClient = arret.NomClient,
            NomAffiche = arret.NomAffiche
        },
        PointLivraison = new PointLivraisonDto
        {
            CodePDL = arret.CodePDL,
            DescriptionPDL = arret.DescriptionPDL
        },
        Saisie = new SaisieDto
        {
            NbExpes = arret.NbExpes,
            NbRolls = arret.NbRolls,
            NbVetements = arret.NbVetements,
            NbTapis = arret.NbTapis,
            NbSacs = arret.NbSacs,
            NbRecuperes = arret.NbRecuperes,
            PrecisionLivreur = string.IsNullOrWhiteSpace(arret.PrecisionLivreur) ? null : arret.PrecisionLivreur.Trim(),
            StatutPassage = arret.StatutPassage,
            CommentaireLivreur = string.IsNullOrWhiteSpace(arret.CommentaireLivreur) ? null : arret.CommentaireLivreur.Trim(),
            HeureValidation = arret.HeureValidation,
            EstValidee = arret.EstValidee
        }
    };
}
