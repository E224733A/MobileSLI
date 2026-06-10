using System.Globalization;
using MobileSLI.Configuration;
using MobileSLI.Models;
using MobileSLI.Services.Api;

namespace MobileSLI.Services;

/// <summary>
/// Service d'orchestration de la synchronisation finale d'une tournée mobile.
/// Ce fichier est sensible : il valide les données locales, restaure le trajet camion,
/// construit le contrat JSON envoyé à l'API, interprète le résultat et met à jour l'état SQLite.
/// Ne pas modifier ce service sans vérifier le contrat API, les statuts locaux et les tests de reprise après erreur réseau.
/// </summary>
public sealed class SynchronisationService
{
    /// <summary>
    /// Code API indiquant que la date de tournée envoyée n'est pas acceptée par le serveur.
    /// </summary>
    private const string DateTourneeNonAutoriseeCode = "DATE_TOURNEE_NON_AUTORISEE";

    /// <summary>
    /// Code API indiquant que la tournée est expirée pour la synchronisation mobile.
    /// </summary>
    private const string DateTourneeExpireeCode = "DATE_TOURNEE_EXPIREE";

    private readonly DatabaseService _databaseService;
    private readonly SynchronisationsApiService _synchronisationsApiService;
    private readonly AppStateService _appStateService;

    public SynchronisationService(
        DatabaseService databaseService,
        SynchronisationsApiService synchronisationsApiService,
        AppStateService appStateService)
    {
        _databaseService = databaseService;
        _synchronisationsApiService = synchronisationsApiService;
        _appStateService = appStateService;
    }

    /// <summary>
    /// Synchronise une tournée locale vers l'API.
    /// Ordre volontaire du traitement : vérifier la tournée, normaliser les clients fermés,
    /// restaurer le trajet camion, valider les lignes, valider le trajet, construire le payload,
    /// envoyer à l'API puis verrouiller ou marquer l'erreur localement selon le résultat.
    /// </summary>
    /// <param name="idTourneeLocale">Identifiant SQLite local de la tournée à envoyer.</param>
    /// <returns>Résultat fonctionnel de la synchronisation, affichable par les écrans de résultat.</returns>
    public async Task<OperationResult> SynchroniserAsync(int idTourneeLocale)
    {
        if (idTourneeLocale <= 0)
        {
            return Failure("Aucune tournée locale n'est sélectionnée pour la synchronisation.", code: "VALIDATION_ERROR");
        }

        var tournee = await _databaseService.GetTourneeAsync(idTourneeLocale);
        if (tournee is null)
        {
            return Failure("Tournée locale introuvable.", code: "VALIDATION_ERROR");
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

        /*
         * Règle métier client fermé : avant l'envoi, les lignes concernées sont renormalisées
         * pour garantir NON_FAIT, quantités à 0, validation locale et commentaire standard.
         * Cette étape protège le payload même si la saisie locale a été interrompue ou reprise.
         */
        await _databaseService.NormalizeClosedLinesAsync(idTourneeLocale);

        /*
         * Le trajet camion est persisté en SQLite pendant la tournée.
         * On le recharge dans AppState avant validation pour éviter de perdre camion/kilométrages
         * après navigation, fermeture de page ou reprise de tournée.
         */
        await _databaseService.RestaurerTrajetDansAppStateAsync(idTourneeLocale, _appStateService);

        var lignes = await _databaseService.GetLignesAsync(idTourneeLocale);
        var validation = await ValidateBeforeSendAsync(lignes);

        if (!validation.Success)
        {
            return validation;
        }

        var trajetValidation = await ValidateTrajetBeforeSendAsync(idTourneeLocale);
        if (!trajetValidation.Success)
        {
            return trajetValidation;
        }

        var request = await _databaseService.BuildSynchronisationRequestAsync(idTourneeLocale);

        // La version du contrat est forcée ici pour garantir que le payload envoyé correspond au contrat mobile/API final.
        request.SchemaVersion = AppConfig.SchemaVersion;

        var trajet = BuildTrajetRequest();

        // Le contrat final ajoute la section Trajet obligatoire sans reconstruire toutes les lignes de tournée.
        var request13 = SynchronisationTourneeAvecTrajetRequest.From(request, trajet);

        OperationResult result;

        try
        {
            result = await _synchronisationsApiService.PostSynchronisationAsync(request13);
        }
        catch (HttpRequestException exception)
        {
            /*
             * Erreur réseau pendant l'envoi : la tournée ne doit pas être marquée synchronisée.
             * Les données restent en SQLite pour permettre une nouvelle tentative depuis le téléphone.
             */
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
            /*
             * Timeout ou annulation technique : résultat inconnu côté API.
             * Par sécurité, la tournée reste en erreur locale et n'est pas verrouillée comme synchronisée.
             */
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
            /*
             * Dernière barrière de sécurité : en cas d'erreur inattendue, on conserve les données locales
             * et on évite de faire croire à une synchronisation réussie.
             */
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
            /*
             * Succès confirmé par l'API : la tournée peut être verrouillée localement comme synchronisée.
             * Une purge limitée est ensuite lancée pour éviter l'accumulation d'anciennes tournées synchronisées.
             */
            await _databaseService.MarkSynchroniseeAsync(idTourneeLocale);
            await _databaseService.PurgeOldSynchronizedTourneesAsync(retentionDays: 7);
            return result;
        }

        if (IsDateTourneeNonAutorisee(result.Code, result.Message, result.TechnicalDetail))
        {
            /*
             * Refus de date métier : la tournée n'est pas considérée comme envoyée.
             * Le livreur doit recharger les tournées du jour plutôt que retenter le même payload expiré.
             */
            await _databaseService.MarkErreurSynchronisationAsync(idTourneeLocale);
            return result;
        }

        if (result.AlreadySynchronized || IsAlreadySentCode(result.Code) || ContainsAlreadySentMessage(result.Message))
        {
            /*
             * Cas idempotent : l'API indique que la tournée est déjà reçue.
             * On verrouille localement pour éviter que l'utilisateur renvoie plusieurs fois la même tournée.
             */
            await _databaseService.MarkDejaSynchroniseeAsync(idTourneeLocale);
            await _databaseService.PurgeOldSynchronizedTourneesAsync(retentionDays: 7);
            return result;
        }

        // Toute autre réponse négative garde la tournée en erreur locale afin de permettre diagnostic et nouvelle tentative.
        await _databaseService.MarkErreurSynchronisationAsync(idTourneeLocale);
        return result;
    }

    /// <summary>
    /// Valide les lignes de tournée avant construction définitive du payload.
    /// Cette validation bloque l'envoi si une ligne n'est pas terminée, non validée,
    /// sans identifiant source, sans quantité, avec doublon d'article ou avec quantité négative.
    /// </summary>
    private async Task<OperationResult> ValidateBeforeSendAsync(
        IReadOnlyCollection<LocalTourneeLigne> lignes)
    {
        if (lignes.Count == 0)
        {
            return Failure("La tournée ne contient aucune ligne à synchroniser.", code: "VALIDATION_ERROR");
        }

        // Contrôle d'unicité des lignes source : l'API doit recevoir chaque arrêt une seule fois.
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

            // Contrôle d'unicité des articles par point : un même code article ne doit pas être envoyé deux fois.
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

    /// <summary>
    /// Valide les informations de trajet camion avant l'envoi.
    /// La section trajet est obligatoire dans le contrat final : camion, kilométrages et dates mobiles
    /// doivent être présents et cohérents avant toute synchronisation.
    /// </summary>
    private async Task<OperationResult> ValidateTrajetBeforeSendAsync(int idTourneeLocale)
    {
        var persistedValidationMessage = await _databaseService.GetTrajetBlockingValidationMessageAsync(idTourneeLocale);
        if (!string.IsNullOrWhiteSpace(persistedValidationMessage))
        {
            return Failure(persistedValidationMessage, code: "VALIDATION_ERROR");
        }

        // Restauration défensive : le trajet peut être présent en SQLite même si AppState a été perdu en mémoire.
        await _databaseService.RestaurerTrajetDansAppStateAsync(idTourneeLocale, _appStateService);

        var camion = _appStateService.CurrentCamion;
        if (camion is null)
        {
            return Failure("Camion manquant. Revenez au choix camion avant d’envoyer la tournée.", code: "VALIDATION_ERROR");
        }

        if (string.IsNullOrWhiteSpace(camion.IdCamion))
        {
            return Failure("Identifiant camion manquant. Rechargez les camions puis recommencez.", code: "VALIDATION_ERROR");
        }

        if (string.IsNullOrWhiteSpace(camion.CodeCamion))
        {
            return Failure("Code camion manquant. Rechargez les camions puis recommencez.", code: "VALIDATION_ERROR");
        }

        if (!_appStateService.KilometrageDepart.HasValue)
        {
            return Failure("Kilométrage départ manquant. Revenez au choix camion avant d’envoyer la tournée.", code: "VALIDATION_ERROR");
        }

        if (!_appStateService.KilometrageArrivee.HasValue)
        {
            return Failure("Kilométrage arrivée manquant. Saisissez le kilométrage arrivée avant d’envoyer la tournée.", code: "VALIDATION_ERROR");
        }

        if (!_appStateService.DateDepartMobile.HasValue)
        {
            return Failure("Date départ mobile manquante. Revenez au choix camion avant d’envoyer la tournée.", code: "VALIDATION_ERROR");
        }

        if (!_appStateService.DateArriveeMobile.HasValue)
        {
            return Failure("Date arrivée mobile manquante. Saisissez le kilométrage arrivée avant d’envoyer la tournée.", code: "VALIDATION_ERROR");
        }

        if (_appStateService.KilometrageDepart.Value < 0)
        {
            return Failure("Le kilométrage départ ne peut pas être négatif.", code: "VALIDATION_ERROR");
        }

        if (_appStateService.KilometrageArrivee.Value < 0)
        {
            return Failure("Le kilométrage arrivée ne peut pas être négatif.", code: "VALIDATION_ERROR");
        }

        if (_appStateService.KilometrageArrivee.Value < _appStateService.KilometrageDepart.Value)
        {
            return Failure("Le kilométrage arrivée doit être supérieur ou égal au kilométrage départ.", code: "VALIDATION_ERROR");
        }

        return Success("Validation trajet réussie.");
    }

    /// <summary>
    /// Construit la section trajet du contrat JSON final.
    /// Cette méthode suppose que <see cref="ValidateTrajetBeforeSendAsync"/> a déjà validé la présence
    /// du camion, des kilométrages et des dates mobiles.
    /// </summary>
    private SynchronisationTrajetRequest BuildTrajetRequest()
    {
        var camion = _appStateService.CurrentCamion!;

        return new SynchronisationTrajetRequest
        {
            Camion = new SynchronisationCamionRequest
            {
                IdCamion = camion.IdCamion.Trim(),
                CodeCamion = camion.CodeCamion.Trim(),
                LibelleCamion = camion.LibelleCamion?.Trim() ?? string.Empty,
                Immatriculation = camion.Immatriculation?.Trim() ?? string.Empty
            },
            KilometrageDepart = _appStateService.KilometrageDepart!.Value,
            KilometrageArrivee = _appStateService.KilometrageArrivee!.Value,
            DateDepartMobile = FormatDateTime(_appStateService.DateDepartMobile!.Value),
            DateArriveeMobile = FormatDateTime(_appStateService.DateArriveeMobile!.Value)
        };
    }

    /// <summary>
    /// Détecte les codes API indiquant que la tournée a déjà été reçue.
    /// Ce cas permet de verrouiller localement sans renvoyer inutilement le même payload.
    /// </summary>
    private static bool IsAlreadySentCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        return string.Equals(code, "TOURNEE_ALREADY_SENT", StringComparison.OrdinalIgnoreCase)
               || string.Equals(code, "SYNCHRONISATION_ALREADY_EXISTS", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Détection de secours pour les réponses déjà synchronisées qui ne portent pas de code structuré fiable.
    /// Le message est inspecté pour rester robuste face à une réponse API partiellement différente.
    /// </summary>
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

    /// <summary>
    /// Détecte les refus liés à la date métier de tournée.
    /// On vérifie le code, le message et le détail technique car le format d'erreur peut varier selon le middleware API.
    /// </summary>
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

    /// <summary>
    /// Recherche les codes de refus de date dans une chaîne brute.
    /// </summary>
    private static bool ContainsDateTourneeNonAutorisee(string? value)
    {
        return !string.IsNullOrWhiteSpace(value)
               && (value.Contains(DateTourneeNonAutoriseeCode, StringComparison.OrdinalIgnoreCase)
                   || value.Contains(DateTourneeExpireeCode, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Formate une date mobile avec offset local pour le contrat trajet.
    /// Si la date est sans Kind, elle est considérée comme locale afin d'éviter un décalage UTC non voulu.
    /// </summary>
    private static string FormatDateTime(DateTime value)
    {
        var local = value.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(value, DateTimeKind.Local)
            : value.ToLocalTime();

        return new DateTimeOffset(local).ToString("yyyy-MM-ddTHH:mm:sszzz", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Construit un résultat de succès interne pour les validations locales.
    /// </summary>
    private static OperationResult Success(string message)
    {
        return new OperationResult
        {
            Success = true,
            Code = "SUCCESS",
            Message = message
        };
    }

    /// <summary>
    /// Construit un résultat d'échec interne sans lever d'exception.
    /// Ce choix permet aux écrans d'afficher un message clair et de conserver la tournée localement.
    /// </summary>
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
