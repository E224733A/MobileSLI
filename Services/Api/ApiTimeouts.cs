using System;

namespace MobileSLI.Services.Api;

/// <summary>
/// Regroupe les constantes de timeout pour les appels API. Les valeurs ont été
/// ajustées pour rendre l'application plus tolérante aux réseaux lents. Les
/// temps d'attente sont exprimés en secondes et doivent rester cohérents avec
/// les spécifications du lot de stabilisation terrain. Les appels de
/// récupération des listes (tournées disponibles, livreurs, etc.) disposent
/// d'un délai suffisamment long pour éviter des coupures intempestives, sans
/// toutefois dépasser les recommandations métier.
/// </summary>
public static class ApiTimeouts
{
    /// <summary>
    /// Timeout pour la vérification de santé de l'API. Doit rester court afin
    /// d'offrir un retour rapide à l'utilisateur en cas de service indisponible.
    /// </summary>
    public static readonly TimeSpan HealthCheck = TimeSpan.FromSeconds(12);

    /// <summary>
    /// Timeout pour la récupération de la liste des livreurs. Spécifié à 60
    /// secondes selon le cahier des charges.
    /// </summary>
    public static readonly TimeSpan Livreurs = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Timeout pour la récupération de la liste des tournées disponibles. Ce
    /// délai a été augmenté à 120 secondes pour supporter des temps de
    /// réponse API plus longs sur des réseaux lents.
    /// </summary>
    public static readonly TimeSpan TourneesDisponibles = TimeSpan.FromSeconds(120);

    /// <summary>
    /// Timeout pour le chargement complet d'une tournée (appel GetTourneeJour).
    /// Porté à 180 secondes pour permettre un téléchargement complet même sur
    /// des connexions dégradées.
    /// </summary>
    public static readonly TimeSpan ChargementTournee = TimeSpan.FromSeconds(180);

    /// <summary>
    /// Timeout pour les appels de synchronisation (POST). Aligné sur
    /// ChargementTournee pour permettre l'envoi des données dans des
    /// conditions réseau similaires.
    /// </summary>
    public static readonly TimeSpan Synchronisation = TimeSpan.FromSeconds(180);

    /// <summary>
    /// Timeout par défaut pour les requêtes GET. Utilisé par défaut dans
    /// ApiClient lorsque aucun délai spécifique n'est fourni. Égal à
    /// TourneesDisponibles afin de couvrir la majorité des appels GET.
    /// </summary>
    public static readonly TimeSpan DefaultGet = TourneesDisponibles;

    /// <summary>
    /// Timeout par défaut pour les requêtes POST. Égal à Synchronisation.
    /// </summary>
    public static readonly TimeSpan DefaultPost = Synchronisation;

    /// <summary>
    /// Délai entre deux tentatives lors du chargement d'une tournée. Inchangé
    /// par rapport à la version précédente (1,5 seconde).
    /// </summary>
    public static readonly TimeSpan ChargementTourneeRetryDelay = TimeSpan.FromMilliseconds(1500);

    /// <summary>
    /// Nombre de tentatives supplémentaires autorisées lors du chargement
    /// d'une tournée complète. Conserve la valeur originale de 1.
    /// </summary>
    public const int ChargementTourneeRetryCount = 1;
}