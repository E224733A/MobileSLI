using MobileSLI.Models;
using MobileSLI.Services.Api;

namespace MobileSLI.Services;

public sealed class SynchronisationService
{
    private const string DateTourneeNonAutoriseeCode = "DATE_TOURNEE_NON_AUTORISEE";
    private const string DateTourneeExpireeCode = "DATE_TOURNEE_EXPIREE";

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

        if (string.Equals(tournee.StatutLocal, TourneeLocalStatus.Expiree, StringComparison.OrdinalIgnoreCase))
        {
            return Failure(
                "Cette tournée est expirée sur le téléphone. Rechargez les tournées du jour depuis le dépôt.",
                alreadySynchronized: false,
                code: "TOURNEE_LOCALE_EXPIREE");
        }

        if (tournee.EstVerrouillee)
        {
            var alreadySynchronized =
                string.Equals(tournee.StatutLocal, TourneeLocalStatus.Synchronisee, StringComparison.OrdinalIgnoreCase)
                || string.Equals(tournee.StatutLocal, TourneeLocalStatus.DejaSynchronisee, StringComparison.OrdinalIgnoreCase);

            return Failure(
                "Cette tournée est déjà verrouillée sur le téléphone.",
                alreadySynchronized: alreadySynchronized,
                code: tournee.StatutLocal);
        }

        var lignes = await _databaseService.GetLignesAsync(idTourneeLocale);
        var validation = await ValidateBeforeSendAsync(lignes);

        if (!validation.Success)
        {
            await _databaseService.MarkErreurSynchronisationAsync(idTourneeLocale);
            return validation;
        }

        var request = await _databaseService.BuildSynchronisationRequestAsync(idTourneeLocale);

        OperationResult result;

        try
        {
            result = await _synchronisationsApiService.PostSynchronisationAsync(request);
        }
        catch (HttpRequestException exception)
        {
            await _databaseService.MarkErreurSynchronisationAsync(idTourneeLocale);

            return new OperationResult
            {
                Success = false,
                Code = "NETWORK_ERROR",
                Message = "Connexion perdue pendant l’envoi. Les données restent enregistrées sur le téléphone.",
                TechnicalDetail = exception.Message
            };
        }
        catch (TaskCanceledException exception)
        {
            await _databaseService.MarkErreurSynchronisationAsync(idTourneeLocale);

            return new OperationResult
            {
                Success = false,
                Code = "TIMEOUT",
                Message = "L’envoi a pris trop de temps ou la connexion a été interrompue. Les données restent enregistrées sur le téléphone.",
                TechnicalDetail = exception.Message
            };
        }
        catch (Exception exception)
        {
            await _databaseService.MarkErreurSynchronisationAsync(idTourneeLocale);

            return new OperationResult
            {
                Success = false,
                Code = "SYNC_UNKNOWN_ERROR",
                Message = "Impossible de confirmer l’envoi de la tournée. Les données restent enregistrées sur le téléphone.",
                TechnicalDetail = exception.Message
            };
        }

        if (result.Success)
        {
            await _databaseService.MarkSynchroniseeAsync(idTourneeLocale);
            await _databaseService.PurgeOldSynchronizedTourneesAsync(retentionDays: 7);
            return result;
        }

        if (IsDateTourneeNonAutorisee(result.Code, result.Message, result.TechnicalDetail))
        {
            await _databaseService.MarkErreurSynchronisationAsync(idTourneeLocale);
            return result;
        }

        if (result.AlreadySynchronized || IsAlreadySentCode(result.Code) || ContainsAlreadySentMessage(result.Message))
        {
            await _databaseService.MarkDejaSynchroniseeAsync(idTourneeLocale);
            await _databaseService.PurgeOldSynchronizedTourneesAsync(retentionDays: 7);
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
            return Failure("La tournée ne contient aucune ligne à synchroniser.", code: "VALIDATION_ERROR");
        }

        var idLignes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var ligne in lignes)
        {
            if (string.IsNullOrWhiteSpace(ligne.IdLigneSource))
            {
                return Failure(
                    "Une ligne ne possède pas d'identifiant source. Synchronisation impossible.",
                    code: "VALIDATION_ERROR");
            }

            if (!idLignes.Add(ligne.IdLigneSource))
            {
                return Failure(
                    $"L'identifiant de ligne source est présent plusieurs fois : {ligne.IdLigneSource}.",
                    code: "VALIDATION_ERROR");
            }

            if (string.Equals(ligne.StatutPassage, StatutPassageConstants.AFaire, StringComparison.OrdinalIgnoreCase))
            {
                return Failure(
                    $"Le point {ligne.NumClient} - {ligne.NomClient} est encore à faire.",
                    code: "VALIDATION_ERROR");
            }

            if ((string.Equals(ligne.StatutPassage, StatutPassageConstants.NonFait, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(ligne.StatutPassage, StatutPassageConstants.Anomalie, StringComparison.OrdinalIgnoreCase))
                && string.IsNullOrWhiteSpace(ligne.CommentaireLivreur))
            {
                return Failure(
                    $"Un commentaire est obligatoire pour {ligne.NumClient} - {ligne.NomClient}.",
                    code: "VALIDATION_ERROR");
            }

            if (!ligne.EstValidee)
            {
                return Failure(
                    $"Le point {ligne.NumClient} - {ligne.NomClient} n'est pas validé.",
                    code: "VALIDATION_ERROR");
            }

            if (ligne.HeureValidation is null)
            {
                return Failure(
                    $"Le point {ligne.NumClient} - {ligne.NomClient} n'a pas d'heure de validation.",
                    code: "VALIDATION_ERROR");
            }

            var quantites = await _databaseService.GetQuantitesAsync(ligne.Id);
            if (quantites.Count == 0)
            {
                return Failure(
                    $"Le point {ligne.NumClient} - {ligne.NomClient} ne contient aucune quantité.",
                    code: "VALIDATION_ERROR");
            }

            var articles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var quantite in quantites)
            {
                if (string.IsNullOrWhiteSpace(quantite.CodeArticle))
                {
                    return Failure(
                        $"Un article du point {ligne.NumClient} - {ligne.NomClient} n'a pas de code article.",
                        code: "VALIDATION_ERROR");
                }

                if (!articles.Add(quantite.CodeArticle))
                {
                    return Failure(
                        $"L'article {quantite.CodeArticle} est présent plusieurs fois sur le point {ligne.NumClient} - {ligne.NomClient}.",
                        code: "VALIDATION_ERROR");
                }

                if (quantite.QuantiteLivree < 0 || quantite.QuantiteRecuperee < 0)
                {
                    return Failure(
                        $"Quantité négative interdite pour {ligne.NumClient} - {ligne.NomClient}.",
                        code: "VALIDATION_ERROR");
                }
            }
        }

        return Success("Validation locale réussie.");
    }

    private static bool IsAlreadySentCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        return string.Equals(code, "TOURNEE_ALREADY_SENT", StringComparison.OrdinalIgnoreCase)
               || string.Equals(code, "SYNCHRONISATION_ALREADY_EXISTS", StringComparison.OrdinalIgnoreCase);
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

    private static bool IsDateTourneeNonAutorisee(
        string? code,
        string? message,
        string? technicalDetail)
    {
        return string.Equals(code, DateTourneeNonAutoriseeCode, StringComparison.OrdinalIgnoreCase)
               || string.Equals(code, DateTourneeExpireeCode, StringComparison.OrdinalIgnoreCase)
               || ContainsDateTourneeNonAutorisee(technicalDetail)
               || ContainsDateTourneeNonAutorisee(message);
    }

    private static bool ContainsDateTourneeNonAutorisee(string? value)
    {
        return !string.IsNullOrWhiteSpace(value)
               && (value.Contains(DateTourneeNonAutoriseeCode, StringComparison.OrdinalIgnoreCase)
                   || value.Contains(DateTourneeExpireeCode, StringComparison.OrdinalIgnoreCase));
    }

    private static OperationResult Success(string message)
    {
        return new OperationResult
        {
            Success = true,
            Code = "SUCCESS",
            Message = message
        };
    }

    private static OperationResult Failure(
        string message,
        bool alreadySynchronized = false,
        string code = "")
    {
        return new OperationResult
        {
            Success = false,
            AlreadySynchronized = alreadySynchronized,
            Code = code,
            Message = message
        };
    }
}
