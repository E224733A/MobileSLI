using MobileSLI.Models;
using MobileSLI.Services.Api;

namespace MobileSLI.Services;

public sealed class SynchronisationService
{
    private readonly DatabaseService _databaseService;
    private readonly SynchronisationsApiService _synchronisationsApiService;

    public SynchronisationService(
        DatabaseService databaseService,
        SynchronisationsApiService synchronisationsApiService)
    {
        _databaseService = databaseService;
        _synchronisationsApiService = synchronisationsApiService;
    }

    public async Task<OperationResult> SynchroniserAsync(int idTourneeLocale)
    {
        if (idTourneeLocale <= 0)
        {
            return Failure("Aucune tournée locale n'est sélectionnée pour la synchronisation.");
        }

        var tournee = await _databaseService.GetTourneeAsync(idTourneeLocale);
        if (tournee is null)
        {
            return Failure("Tournée locale introuvable.");
        }

        var lignes = await _databaseService.GetLignesAsync(idTourneeLocale);
        var validation = await ValidateBeforeSendAsync(lignes);

        if (!validation.Success)
        {
            await _databaseService.MarkErreurSynchronisationAsync(idTourneeLocale);
            return validation;
        }

        var request = await _databaseService.BuildSynchronisationRequestAsync(idTourneeLocale);
        var result = await _synchronisationsApiService.PostSynchronisationAsync(request);

        if (result.Success)
        {
            await _databaseService.MarkSynchroniseeAsync(idTourneeLocale);
            return result;
        }

        if (ContainsAlreadySentMessage(result.Message))
        {
            await _databaseService.MarkDejaSynchroniseeAsync(idTourneeLocale);
            return result;
        }

        await _databaseService.MarkErreurSynchronisationAsync(idTourneeLocale);
        return result;
    }

    private async Task<OperationResult> ValidateBeforeSendAsync(
        IReadOnlyCollection<LocalTourneeLigne> lignes)
    {
        if (lignes.Count == 0)
        {
            return Failure("La tournée ne contient aucune ligne à synchroniser.");
        }

        var idLignes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var ligne in lignes)
        {
            if (string.IsNullOrWhiteSpace(ligne.IdLigneSource))
            {
                return Failure("Une ligne ne possède pas d'identifiant source. Synchronisation impossible.");
            }

            if (!idLignes.Add(ligne.IdLigneSource))
            {
                return Failure($"L'identifiant de ligne source est présent plusieurs fois : {ligne.IdLigneSource}.");
            }

            if (string.Equals(ligne.StatutPassage, StatutPassageConstants.AFaire, StringComparison.OrdinalIgnoreCase))
            {
                return Failure($"Le point {ligne.NumClient} - {ligne.NomClient} est encore à faire.");
            }

            if ((string.Equals(ligne.StatutPassage, StatutPassageConstants.NonFait, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(ligne.StatutPassage, StatutPassageConstants.Anomalie, StringComparison.OrdinalIgnoreCase))
                && string.IsNullOrWhiteSpace(ligne.CommentaireLivreur))
            {
                return Failure($"Un commentaire est obligatoire pour {ligne.NumClient} - {ligne.NomClient}.");
            }

            if (!ligne.EstValidee)
            {
                return Failure($"Le point {ligne.NumClient} - {ligne.NomClient} n'est pas validé.");
            }

            if (ligne.HeureValidation is null)
            {
                return Failure($"Le point {ligne.NumClient} - {ligne.NomClient} n'a pas d'heure de validation.");
            }

            var quantites = await _databaseService.GetQuantitesAsync(ligne.Id);
            if (quantites.Count == 0)
            {
                return Failure($"Le point {ligne.NumClient} - {ligne.NomClient} ne contient aucune quantité.");
            }

            var articles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var quantite in quantites)
            {
                if (string.IsNullOrWhiteSpace(quantite.CodeArticle))
                {
                    return Failure($"Un article du point {ligne.NumClient} - {ligne.NomClient} n'a pas de code article.");
                }

                if (!articles.Add(quantite.CodeArticle))
                {
                    return Failure($"L'article {quantite.CodeArticle} est présent plusieurs fois sur le point {ligne.NumClient} - {ligne.NomClient}.");
                }

                if (quantite.QuantiteLivree < 0 || quantite.QuantiteRecuperee < 0)
                {
                    return Failure($"Quantité négative interdite pour {ligne.NumClient} - {ligne.NomClient}.");
                }
            }
        }

        return Success("Validation locale réussie.");
    }

    private static bool ContainsAlreadySentMessage(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return false;
        }

        return message.Contains("déjà", StringComparison.OrdinalIgnoreCase)
               || message.Contains("deja", StringComparison.OrdinalIgnoreCase)
               || message.Contains("already", StringComparison.OrdinalIgnoreCase)
               || message.Contains("TOURNEE_ALREADY_SENT", StringComparison.OrdinalIgnoreCase)
               || message.Contains("SYNCHRONISATION_ALREADY_EXISTS", StringComparison.OrdinalIgnoreCase);
    }

    private static OperationResult Success(string message)
    {
        return new OperationResult
        {
            Success = true,
            Message = message
        };
    }

    private static OperationResult Failure(string message)
    {
        return new OperationResult
        {
            Success = false,
            Message = message
        };
    }
}
