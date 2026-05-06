using MobileSLI.Models;

namespace MobileSLI.Services;

public sealed class SynchronisationService
{
    private readonly DatabaseService _databaseService;
    private readonly ApiService _apiService;

    public SynchronisationService(DatabaseService databaseService, ApiService apiService)
    {
        _databaseService = databaseService;
        _apiService = apiService;
    }

    public async Task<OperationResult> SynchroniserAsync(int tourneeId)
    {
        var request = await _databaseService.BuildSynchronisationRequestAsync(tourneeId);
        var validation = ValidateBeforeSend(request);

        if (!validation.Success)
        {
            return validation;
        }

        var result = await _apiService.PostSynchronisationAsync(request);

        if (result.Success)
        {
            await _databaseService.MarkSynchroniseeAsync(tourneeId);
        }
        else if (result.AlreadySynchronized)
        {
            await _databaseService.MarkDejaSynchroniseeAsync(tourneeId);
        }
        else
        {
            await _databaseService.MarkErreurSynchronisationAsync(tourneeId);
        }

        return result;
    }

    public OperationResult ValidateBeforeSend(SynchronisationTourneeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.IdSynchronisation))
        {
            return OperationResult.Fail("Identifiant de synchronisation manquant.");
        }

        if (string.IsNullOrWhiteSpace(request.CodeTournee))
        {
            return OperationResult.Fail("Code tournée manquant.");
        }

        if (string.IsNullOrWhiteSpace(request.Livreur.CodeLivreur))
        {
            return OperationResult.Fail("Code livreur manquant.");
        }

        if (request.Lignes.Count == 0)
        {
            return OperationResult.Fail("Aucune ligne de tournée à envoyer.");
        }

        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var ligne in request.Lignes)
        {
            if (string.IsNullOrWhiteSpace(ligne.IdLigneSource))
            {
                return OperationResult.Fail($"Identifiant de ligne manquant pour le client {ligne.NumClient}.");
            }

            if (!ids.Add(ligne.IdLigneSource))
            {
                return OperationResult.Fail($"Identifiant de ligne en double : {ligne.IdLigneSource}.");
            }

            if (ligne.StatutPassage == StatutPassageConstants.AFaire)
            {
                return OperationResult.Fail($"Le client {ligne.NumClient} est encore à faire. Il doit être validé avant l'envoi.");
            }

            if ((ligne.StatutPassage == StatutPassageConstants.NonFait || ligne.StatutPassage == StatutPassageConstants.Anomalie)
                && string.IsNullOrWhiteSpace(ligne.CommentaireLivreur))
            {
                return OperationResult.Fail($"Un commentaire est obligatoire pour le client {ligne.NumClient}.");
            }

            if (ligne.EstValidee && ligne.HeureValidation is null)
            {
                return OperationResult.Fail($"Heure de validation manquante pour le client {ligne.NumClient}.");
            }

            foreach (var quantite in ligne.Quantites)
            {
                if (quantite.QuantiteLivree < 0 || quantite.QuantiteRecuperee < 0)
                {
                    return OperationResult.Fail($"Quantité négative interdite pour le client {ligne.NumClient}.");
                }
            }
        }

        return OperationResult.Ok("Validation locale réussie", request.Lignes.Count);
    }
}
