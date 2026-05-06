using Microsoft.Maui.Storage;
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

        return _database;
    }

    public async Task<int> SaveTourneeAsync(TourneeJourDto dto)
    {
        var db = await GetDatabaseAsync();

        var existing = await db.Table<LocalTournee>()
            .Where(t => t.DateTournee == dto.DateTournee.Date
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
            DateTournee = dto.DateTournee.Date,
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
            var ligne = new LocalTourneeLigne
            {
                TourneeId = tournee.Id,
                IdLigneSource = ligneDto.IdLigneSource,
                OrdreArret = ligneDto.OrdreArret,
                NumClient = ligneDto.NumClient,
                NomClient = ligneDto.NomClient,
                CodePDL = ligneDto.CodePDL,
                DescriptionPDL = ligneDto.DescriptionPDL,
                AdresseLigne1 = ligneDto.AdresseLigne1,
                Ville = ligneDto.Ville,
                CodePostal = ligneDto.CodePostal,
                Zone = ligneDto.Zone,
                ZoneDechargement = ligneDto.ZoneDechargement,
                Instructions = ligneDto.Instructions,
                CommentaireFiche = ligneDto.CommentaireFiche,
                StatutPassage = StatutPassageConstants.AFaire,
                EstValidee = false,
                HeureValidation = null,
                CommentaireLivreur = null
            };

            await db.InsertAsync(ligne);

            foreach (var article in dto.ArticlesSaisissables)
            {
                await db.InsertAsync(new LocalTourneeLigneQuantite
                {
                    LigneId = ligne.Id,
                    CodeArticle = article.CodeArticle,
                    Libelle = article.Libelle,
                    QuantiteLivree = 0,
                    QuantiteRecuperee = 0
                });
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

    public async Task SaveLigneAsync(LocalTourneeLigne ligne, IEnumerable<LocalTourneeLigneQuantite> quantites)
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
            SchemaVersion = "1.1",
            IdSynchronisation = tournee.IdSynchronisation,
            DateTournee = tournee.DateTournee,
            CodeTournee = tournee.CodeTournee,
            LibelleTournee = tournee.LibelleTournee,
            Livreur = new LivreurDto
            {
                CodeLivreur = tournee.CodeLivreur,
                NomLivreur = tournee.NomLivreur
            },
            Mobile = new MobileInfoDto
            {
                NomAppareil = _settings.DeviceName,
                VersionApplication = _settings.ApplicationVersion,
                DateChargement = tournee.DateChargement,
                DateEnvoi = DateTime.Now
            },
            CommentaireGlobal = tournee.CommentaireGlobal,
            Lignes = new List<SynchronisationLigneRequest>()
        };

        foreach (var ligne in lignes)
        {
            var quantites = await GetQuantitesAsync(ligne.Id);
            request.Lignes.Add(new SynchronisationLigneRequest
            {
                IdLigneSource = ligne.IdLigneSource,
                OrdreArret = ligne.OrdreArret,
                NumClient = ligne.NumClient,
                NomClient = ligne.NomClient,
                CodePDL = ligne.CodePDL,
                DescriptionPDL = ligne.DescriptionPDL,
                StatutPassage = ligne.StatutPassage,
                EstValidee = ligne.EstValidee,
                HeureValidation = ligne.HeureValidation,
                CommentaireLivreur = string.IsNullOrWhiteSpace(ligne.CommentaireLivreur) ? null : ligne.CommentaireLivreur,
                Quantites = quantites.Select(q => new QuantiteArticleRequest
                {
                    CodeArticle = q.CodeArticle,
                    Libelle = q.Libelle,
                    QuantiteLivree = q.QuantiteLivree,
                    QuantiteRecuperee = q.QuantiteRecuperee
                }).ToList()
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

        tournee.CommentaireGlobal = string.IsNullOrWhiteSpace(commentaireGlobal) ? null : commentaireGlobal.Trim();
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
        var lignes = await db.Table<LocalTourneeLigne>().Where(l => l.TourneeId == tourneeId).ToListAsync();

        foreach (var ligne in lignes)
        {
            var quantites = await db.Table<LocalTourneeLigneQuantite>().Where(q => q.LigneId == ligne.Id).ToListAsync();
            foreach (var quantite in quantites)
            {
                await db.DeleteAsync(quantite);
            }

            await db.DeleteAsync(ligne);
        }

        var tournee = await db.Table<LocalTournee>().Where(t => t.Id == tourneeId).FirstOrDefaultAsync();
        if (tournee is not null)
        {
            await db.DeleteAsync(tournee);
        }
    }
}
