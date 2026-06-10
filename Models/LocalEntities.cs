using MobileSLI.Configuration;
using SQLite;

namespace MobileSLI.Models;

/// <summary>
/// En-tête de tournée stocké localement dans SQLite sur le téléphone.
/// Il conserve l'identité de la tournée, le livreur, l'état local de synchronisation
/// et les informations de trajet camion nécessaires au contrat mobile/API 1.3.
/// </summary>
public sealed class LocalTournee
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    /// <summary>
    /// Version du contrat API associée à la tournée locale.
    /// </summary>
    public string SchemaVersion { get; set; } = AppConfig.SchemaVersion;

    [Indexed]
    public DateTime DateTournee { get; set; }

    [Indexed]
    public string CodeTournee { get; set; } = string.Empty;

    public string LibelleTournee { get; set; } = string.Empty;
    public string CodeLivreur { get; set; } = string.Empty;
    public string NomLivreur { get; set; } = string.Empty;

    /// <summary>
    /// État local de la tournée : chargée, en cours, synchronisée, expirée ou abandonnée.
    /// </summary>
    public string StatutLocal { get; set; } = TourneeLocalStatus.Chargee;

    public DateTime DateChargement { get; set; }
    public DateTime? DateEnvoi { get; set; }
    public string IdSynchronisation { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Empêche toute modification locale après synchronisation, expiration ou abandon local.
    /// </summary>
    public bool EstVerrouillee { get; set; }

    public string? CommentaireGlobal { get; set; }

    /*
     * Trajet camion persisté localement.
     * Ces champs peuvent rester null pour les anciennes tournées déjà présentes dans SQLite
     * avant l'ajout du contrat mobile/API 1.3.
     */
    public string? IdCamion { get; set; }
    public string? CodeCamion { get; set; }
    public string? LibelleCamion { get; set; }
    public string? Immatriculation { get; set; }
    public int? KilometrageDepart { get; set; }
    public int? KilometrageArrivee { get; set; }
    public DateTime? DateDepartMobile { get; set; }
    public DateTime? DateArriveeMobile { get; set; }
}

/// <summary>
/// Ligne de tournée stockée localement dans SQLite.
/// Elle contient les données de contexte reçues de l'API, les informations client/PDL,
/// les consignes livreur, l'état de fermeture client et la saisie effectuée sur le téléphone.
/// </summary>
public sealed class LocalTourneeLigne
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public int TourneeId { get; set; }

    [Indexed]
    public string IdLigneSource { get; set; } = string.Empty;

    public int OrdreArret { get; set; }
    public string? Horaire { get; set; }

    public string NumClient { get; set; } = string.Empty;
    public string NomClient { get; set; } = string.Empty;
    public string? NomAffiche { get; set; }

    public string? CodePDL { get; set; }
    public string? DescriptionPDL { get; set; }
    public string? AdresseLigne1 { get; set; }
    public string? AdresseLigne2 { get; set; }
    public string? AdresseLigne3 { get; set; }
    public string? Ville { get; set; }
    public string? CodePostal { get; set; }

    /*
     * Lien optionnel vers l'adresse de livraison.
     * L'API construit directement ce lien Google Maps à partir de la vue PDL/adresse livraison.
     */
    public string? LienAdresseLivraison { get; set; }

    public string? CodeTournee { get; set; }
    public string? LibelleTournee { get; set; }
    public int? JourTournee { get; set; }
    public string? JourLibelle { get; set; }
    public string? SchemaLivraison { get; set; }

    public int? JourTourneeRetour { get; set; }
    public string? JourRetourLibelle { get; set; }
    public string? CodeTourneeRetour { get; set; }
    public string? LibelleTourneeRetour { get; set; }

    public string? Zone { get; set; }
    public string? ZoneDechargement { get; set; }

    /*
     * Valeur finale renvoyée par l'API lorsqu'elle applique déjà la règle métier.
     * La propriété calculée ZoneDechargementAffichee l'utilise en priorité.
     */
    public string? ZoneDechargementAfficheeValeur { get; set; }

    public string? Precision { get; set; }
    public string? Cle { get; set; }

    public bool EstFerme { get; set; }
    public DateTime? DateFermeture { get; set; }
    public string? MotifFermeture { get; set; }

    public string? Instructions { get; set; }

    /*
     * Ancien champ conservé pour compatibilité avec des réponses API plus anciennes.
     */
    public string? CommentaireFiche { get; set; }

    /*
     * Commentaire ponctuel venant de l'administration ou de l'expédition.
     * Ce n'est pas le commentaire saisi par le livreur pendant la tournée.
     */
    public string? CommentaireExceptionnel { get; set; }

    public string StatutPassage { get; set; } = StatutPassageConstants.AFaire;
    public bool EstValidee { get; set; }
    public DateTime? HeureValidation { get; set; }
    public string? CommentaireLivreur { get; set; }
    public string? PrecisionLivreur { get; set; }

    [Ignore]
    public string? ZoneDechargementAffichee
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(ZoneDechargementAfficheeValeur))
            {
                return ZoneDechargementAfficheeValeur.Trim();
            }

            if (string.IsNullOrWhiteSpace(ZoneDechargement))
            {
                return JourTourneeRetour?.ToString();
            }

            var zone = ZoneDechargement.Trim();

            if (zone.StartsWith("+", StringComparison.Ordinal))
            {
                return JourTourneeRetour.HasValue
                    ? $"{JourTourneeRetour.Value} {zone}"
                    : zone;
            }

            return zone;
        }
    }

    [Ignore]
    public bool IsFermetureVisible => EstFerme;

    [Ignore]
    public string FermetureText
    {
        get
        {
            if (!EstFerme)
            {
                return string.Empty;
            }

            if (!string.IsNullOrWhiteSpace(MotifFermeture))
            {
                return $"Client fermé : {MotifFermeture}";
            }

            if (DateFermeture.HasValue)
            {
                return $"Client fermé le {DateFermeture.Value:dd/MM/yyyy}";
            }

            return "Client fermé.";
        }
    }
}

/// <summary>
/// Quantité locale associée à une ligne de tournée.
/// Elle conserve la quantité prévue par l'expédition et les quantités réellement saisies
/// par le livreur pour un article donné.
/// </summary>
public sealed class LocalTourneeLigneQuantite
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public int LigneId { get; set; }

    public string CodeArticle { get; set; } = string.Empty;
    public string Libelle { get; set; } = string.Empty;

    /*
     * Pré-remplissage expédition pour la colonne Livré.
     * null = non renseigné ; 0 = zéro volontaire ; > 0 = quantité prévue.
     */
    public int? QuantiteLivreePrevue { get; set; }

    public int QuantiteLivree { get; set; }
    public int QuantiteRecuperee { get; set; }
}
