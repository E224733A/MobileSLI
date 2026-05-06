using System.Collections;
using System.Reflection;
using MobileSLI.Models;
using MobileSLI.Services.Api;

namespace MobileSLI.Services;

public sealed class SynchronisationService
{
    private readonly DatabaseService _databaseService;
    private readonly SettingsService _settingsService;
    private readonly SynchronisationsApiService _synchronisationsApiService;

    public SynchronisationService(
        DatabaseService databaseService,
        SettingsService settingsService,
        SynchronisationsApiService synchronisationsApiService)
    {
        _databaseService = databaseService;
        _settingsService = settingsService;
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
        if (lignes.Count == 0)
        {
            return Failure("La tournée ne contient aucune ligne à synchroniser.");
        }

        var validation = await ValidateBeforeSendAsync(lignes);
        if (!validation.Success)
        {
            return validation;
        }

        var request = await BuildSynchronisationRequestAsync(tournee, lignes);

        var result = await _synchronisationsApiService.PostSynchronisationAsync(request);

        if (result.Success)
        {
            await TryMarkTourneeAsSynchroniseeAsync(idTourneeLocale);
        }

        return result;
    }

    private async Task<OperationResult> ValidateBeforeSendAsync(
        IReadOnlyCollection<LocalTourneeLigne> lignes)
    {
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var ligne in lignes)
        {
            if (string.IsNullOrWhiteSpace(ligne.IdLigneSource))
            {
                return Failure("Une ligne ne possède pas d'identifiant source. Synchronisation impossible.");
            }

            if (!ids.Add(ligne.IdLigneSource))
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

    private async Task<SynchronisationTourneeRequest> BuildSynchronisationRequestAsync(
        LocalTournee tournee,
        IReadOnlyCollection<LocalTourneeLigne> lignes)
    {
        var request = Activator.CreateInstance<SynchronisationTourneeRequest>()
            ?? throw new InvalidOperationException("Impossible de créer la requête de synchronisation.");

        SetValue(request, "SchemaVersion", "1.1");
        SetValue(request, "IdSynchronisation", Guid.NewGuid().ToString());
        SetValue(request, "DateTournee", tournee.DateTournee);
        SetValue(request, "CodeTournee", tournee.CodeTournee);
        SetValue(request, "LibelleTournee", tournee.LibelleTournee);
        SetValue(request, "CommentaireGlobal", string.IsNullOrWhiteSpace(tournee.CommentaireGlobal) ? null : tournee.CommentaireGlobal.Trim());

        var livreur = CreateAndAssignNestedObject(request, "Livreur");
        if (livreur is not null)
        {
            SetValue(livreur, "CodeLivreur", tournee.CodeLivreur);
            SetValue(livreur, "NomLivreur", tournee.NomLivreur);
        }

        var mobile = CreateAndAssignNestedObject(request, "Mobile");
        if (mobile is not null)
        {
            SetValue(mobile, "NomAppareil", _settingsService.DeviceName);
            SetValue(mobile, "VersionApplication", _settingsService.ApplicationVersion);
            SetValue(mobile, "DateChargement", TryReadValue<DateTime?>(tournee, "DateChargement") ?? TryReadValue<DateTime?>(tournee, "DateChargementMobile"));
            SetValue(mobile, "DateEnvoi", DateTime.Now);
        }

        var requestLines = CreateAndAssignList(request, "Lignes");
        if (requestLines is null)
        {
            throw new InvalidOperationException("Impossible de créer la liste des lignes de synchronisation.");
        }

        var lineElementType = GetListElementType(requestLines.GetType());

        foreach (var ligne in lignes)
        {
            var requestLine = Activator.CreateInstance(lineElementType)
                ?? throw new InvalidOperationException("Impossible de créer une ligne de synchronisation.");

            SetValue(requestLine, "IdLigneSource", ligne.IdLigneSource);
            SetValue(requestLine, "OrdreArret", ligne.OrdreArret);

            var client = CreateAndAssignNestedObject(requestLine, "Client");
            if (client is not null)
            {
                SetValue(client, "NumClient", ligne.NumClient);
                SetValue(client, "NomClient", ligne.NomClient);
                SetValue(client, "NomAffiche", TryReadValue<string>(ligne, "NomAffiche") ?? $"{ligne.NumClient} - {ligne.NomClient}");
            }

            var pointLivraison = CreateAndAssignNestedObject(requestLine, "PointLivraison");
            if (pointLivraison is not null)
            {
                SetValue(pointLivraison, "CodePDL", ligne.CodePDL);
                SetValue(pointLivraison, "DescriptionPDL", ligne.DescriptionPDL);
                SetValue(pointLivraison, "AdresseLigne1", ligne.AdresseLigne1);
                SetValue(pointLivraison, "AdresseLigne2", TryReadValue<string>(ligne, "AdresseLigne2"));
                SetValue(pointLivraison, "AdresseLigne3", TryReadValue<string>(ligne, "AdresseLigne3"));
                SetValue(pointLivraison, "Ville", ligne.Ville);
                SetValue(pointLivraison, "CodePostal", ligne.CodePostal);
            }

            var saisie = CreateAndAssignNestedObject(requestLine, "Saisie");
            if (saisie is not null)
            {
                SetValue(saisie, "PrecisionLivreur", TryReadValue<string>(ligne, "PrecisionLivreur"));
                SetValue(saisie, "StatutPassage", ligne.StatutPassage);
                SetValue(saisie, "CommentaireLivreur", ligne.CommentaireLivreur);
                SetValue(saisie, "HeureValidation", ligne.HeureValidation);
                SetValue(saisie, "EstValidee", ligne.EstValidee);

                var requestQuantites = CreateAndAssignList(saisie, "Quantites");
                if (requestQuantites is not null)
                {
                    var quantiteElementType = GetListElementType(requestQuantites.GetType());
                    var quantites = await _databaseService.GetQuantitesAsync(ligne.Id);

                    foreach (var quantite in quantites)
                    {
                        var requestQuantite = Activator.CreateInstance(quantiteElementType)
                            ?? throw new InvalidOperationException("Impossible de créer une quantité de synchronisation.");

                        SetValue(requestQuantite, "CodeArticle", quantite.CodeArticle);
                        SetValue(requestQuantite, "Libelle", quantite.Libelle);
                        SetValue(requestQuantite, "QuantiteLivree", quantite.QuantiteLivree);
                        SetValue(requestQuantite, "QuantiteRecuperee", quantite.QuantiteRecuperee);

                        requestQuantites.Add(requestQuantite);
                    }
                }
            }

            requestLines.Add(requestLine);
        }

        return request;
    }

    private async Task TryMarkTourneeAsSynchroniseeAsync(int idTourneeLocale)
    {
        var methodNames = new[]
        {
            "MarkTourneeAsSynchroniseeAsync",
            "MarquerTourneeSynchroniseeAsync",
            "VerrouillerTourneeAsync",
            "LockTourneeAsync"
        };

        foreach (var methodName in methodNames)
        {
            var method = _databaseService
                .GetType()
                .GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);

            if (method is null)
            {
                continue;
            }

            var parameters = method.GetParameters();
            object? result;

            if (parameters.Length == 1)
            {
                result = method.Invoke(_databaseService, new object[] { idTourneeLocale });
            }
            else if (parameters.Length == 2)
            {
                result = method.Invoke(_databaseService, new object[] { idTourneeLocale, DateTime.Now });
            }
            else
            {
                continue;
            }

            if (result is Task task)
            {
                await task;
            }

            return;
        }
    }

    private static object? CreateAndAssignNestedObject(
        object target,
        string propertyName)
    {
        var property = GetWritableProperty(target, propertyName);
        if (property is null)
        {
            return null;
        }

        var instance = Activator.CreateInstance(property.PropertyType);
        if (instance is null)
        {
            return null;
        }

        property.SetValue(target, instance);
        return instance;
    }

    private static IList? CreateAndAssignList(
        object target,
        string propertyName)
    {
        var property = GetWritableProperty(target, propertyName);
        if (property is null)
        {
            return null;
        }

        var elementType = property.PropertyType.IsGenericType
            ? property.PropertyType.GetGenericArguments()[0]
            : typeof(object);

        var listType = typeof(List<>).MakeGenericType(elementType);
        var list = Activator.CreateInstance(listType) as IList;

        if (list is null)
        {
            return null;
        }

        property.SetValue(target, list);
        return list;
    }

    private static Type GetListElementType(Type listType)
    {
        if (listType.IsGenericType)
        {
            return listType.GetGenericArguments()[0];
        }

        var interfaceType = listType
            .GetInterfaces()
            .FirstOrDefault(type => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>));

        return interfaceType?.GetGenericArguments()[0] ?? typeof(object);
    }

    private static PropertyInfo? GetWritableProperty(
        object target,
        string propertyName)
    {
        return target
            .GetType()
            .GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
    }

    private static void SetValue(
        object target,
        string propertyName,
        object? value)
    {
        var property = GetWritableProperty(target, propertyName);
        if (property is null || !property.CanWrite)
        {
            return;
        }

        if (value is null)
        {
            if (!property.PropertyType.IsValueType || Nullable.GetUnderlyingType(property.PropertyType) is not null)
            {
                property.SetValue(target, null);
            }

            return;
        }

        var targetType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

        try
        {
            if (targetType.IsEnum)
            {
                property.SetValue(target, Enum.Parse(targetType, value.ToString()!, ignoreCase: true));
                return;
            }

            if (targetType == typeof(Guid))
            {
                property.SetValue(target, Guid.Parse(value.ToString()!));
                return;
            }

            if (targetType == typeof(DateTimeOffset) && value is DateTime dateTime)
            {
                property.SetValue(target, new DateTimeOffset(dateTime));
                return;
            }

            if (targetType == typeof(DateTime) && value is DateTimeOffset dateTimeOffset)
            {
                property.SetValue(target, dateTimeOffset.DateTime);
                return;
            }

            if (targetType.IsAssignableFrom(value.GetType()))
            {
                property.SetValue(target, value);
                return;
            }

            property.SetValue(target, Convert.ChangeType(value, targetType));
        }
        catch
        {
            // Une propriété optionnelle non compatible ne doit pas bloquer
            // la création de la requête si elle n'existe pas dans ce modèle.
        }
    }

    private static TValue? TryReadValue<TValue>(
        object source,
        string propertyName)
    {
        var property = source
            .GetType()
            .GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);

        if (property is null || !property.CanRead)
        {
            return default;
        }

        var value = property.GetValue(source);

        if (value is TValue typedValue)
        {
            return typedValue;
        }

        return default;
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
