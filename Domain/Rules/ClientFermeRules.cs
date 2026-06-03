using System;

namespace MobileSLI.Domain.Rules;

public static class ClientFermeRules
{
    public const string StatutPassageClientFerme = "NON_FAIT";
    public const string CommentaireLivreurAutomatique = "Client fermé";

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

public readonly record struct ClientFermeLineState(
    bool EstFerme,
    string? StatutPassage,
    bool EstValidee,
    DateTime? HeureValidation,
    string? CommentaireLivreur);

public readonly record struct ClientFermeQuantiteState(
    int QuantiteLivree,
    int QuantiteRecuperee);
