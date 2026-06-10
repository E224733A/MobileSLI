using System;

namespace MobileSLI.Domain.Rules;

/// <summary>
/// Règles métier appliquées lorsqu'un client est signalé fermé.
/// Cette classe centralise la logique pour éviter que DatabaseService, les ViewModels
/// ou la synchronisation appliquent des variantes différentes de la même règle.
/// </summary>
public static class ClientFermeRules
{
    /// <summary>
    /// Statut obligatoire pour une ligne client fermé.
    /// </summary>
    public const string StatutPassageClientFerme = "NON_FAIT";

    /// <summary>
    /// Commentaire standard appliqué automatiquement sur une ligne client fermé.
    /// </summary>
    public const string CommentaireLivreurAutomatique = "Client fermé";

    /// <summary>
    /// Normalise l'état d'une ligne lorsque le client est fermé.
    /// Règle métier : statut NON_FAIT, ligne validée, heure de validation présente
    /// et commentaire livreur standard. Si le client n'est pas fermé, l'état est conservé.
    /// </summary>
    public static ClientFermeLineState NormalizeLine(
        ClientFermeLineState ligne,
        DateTime? now = null)
    {
        if (!ligne.EstFerme)
        {
            return ligne;
        }

        return ligne with
        {
            StatutPassage = StatutPassageClientFerme,
            EstValidee = true,
            HeureValidation = ligne.HeureValidation ?? now ?? DateTime.Now,
            CommentaireLivreur = CommentaireLivreurAutomatique
        };
    }

    /// <summary>
    /// Normalise les quantités d'une ligne client fermé.
    /// Règle métier : aucune quantité livrée ou récupérée ne doit être envoyée pour un client fermé.
    /// Si le client n'est pas fermé, les quantités sont conservées.
    /// </summary>
    public static ClientFermeQuantiteState NormalizeQuantite(
        ClientFermeQuantiteState quantite,
        bool estFerme)
    {
        if (!estFerme)
        {
            return quantite;
        }

        return quantite with
        {
            QuantiteLivree = 0,
            QuantiteRecuperee = 0
        };
    }
}

/// <summary>
/// État minimal d'une ligne nécessaire pour appliquer la règle client fermé.
/// </summary>
/// <param name="EstFerme">Indique si le client est fermé.</param>
/// <param name="StatutPassage">Statut de passage courant.</param>
/// <param name="EstValidee">Indique si la ligne est validée.</param>
/// <param name="HeureValidation">Heure de validation de la ligne.</param>
/// <param name="CommentaireLivreur">Commentaire saisi ou appliqué pour le livreur.</param>
public readonly record struct ClientFermeLineState(
    bool EstFerme,
    string? StatutPassage,
    bool EstValidee,
    DateTime? HeureValidation,
    string? CommentaireLivreur);

/// <summary>
/// État minimal des quantités nécessaire pour appliquer la règle client fermé.
/// </summary>
/// <param name="QuantiteLivree">Quantité livrée.</param>
/// <param name="QuantiteRecuperee">Quantité récupérée.</param>
public readonly record struct ClientFermeQuantiteState(
    int QuantiteLivree,
    int QuantiteRecuperee);
