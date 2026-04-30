using TourneesMobile.Models;

namespace TourneesMobile.Services;

public sealed class DemoDataService
{
    public TourneeMobileDto CreateTourneeDemo()
    {
        return new TourneeMobileDto
        {
            SchemaVersion = "1.0",
            DateTournee = "2026-04-28",
            JourTournee = 2,
            JourLibelle = "Mardi",
            CodeTournee = "2001",
            LibelleTournee = "MDR VENDEE",
            StatutSynchronisation = StatutSynchronisation.NonEnvoyee,
            Livreur = new LivreurDto
            {
                CodeLivreur = "2",
                NomLivreur = "DAVID LEBAS"
            },
            Chargement = new ChargementDto
            {
                DateGenerationApi = DateTime.Now,
                NombrePointsEnvoyes = 4
            },
            Lignes =
            [
                CreateLigne("2026-04-28|2001|2|1058|1|1", 1, "1058", "EHPAD L EQUAIZIERE", "EHPAD EQUAIZIERE GARNACHE", "1", "EHPAD EQUAIZIERE GARNACHE", "Local arrière, sonner avant livraison", "MDR", "Rez-de-chaussée"),
                CreateLigne("2026-04-28|2001|2|1320|1|2", 2, "1320", "RESIDENCE BEL AIR", "RESIDENCE BEL AIR", "1", "Entrée principale", "Livraison avant 10h30", "MDR", "Quai 2"),
                CreateLigne("2026-04-28|2001|2|2042|2|3", 3, "2042", "CLINIQUE DU PARC", "CLINIQUE DU PARC - SERVICE A", "2", "Service A", "Accès par portail livraison", "VETEMENTS", "Sous-sol"),
                CreateLigne("2026-04-28|2001|2|3100|1|4", 4, "3100", "MAISON RETRAITE OCEANE", "MR OCEANE", "1", "Bâtiment B", "Prévenir accueil si chariots pleins", "MDR", "Hall")
            ]
        };
    }

    private static TourneeLigneMobileDto CreateLigne(
        string id,
        int ordre,
        string numClient,
        string nomClient,
        string nomAffiche,
        string codePdl,
        string descriptionPdl,
        string instructions,
        string typeLinge,
        string zoneDechargement)
    {
        return new TourneeLigneMobileDto
        {
            IdLigneSource = id,
            OrdreArret = ordre,
            Client = new ClientDto
            {
                NumClient = numClient,
                NomClient = nomClient,
                NomAffiche = nomAffiche
            },
            PointLivraison = new PointLivraisonDto
            {
                CodePDL = codePdl,
                DescriptionPDL = descriptionPdl,
                AdresseLigne1 = "Adresse démo",
                CodePostal = "85000",
                Ville = "Vendée"
            },
            InfosLivreur = new InfosLivreurDto
            {
                Instructions = instructions,
                TypeLinge = typeLinge,
                ZoneDechargement = zoneDechargement,
                Zone = "Zone Vendée"
            },
            Retour = new RetourInfoDto
            {
                CodeTourneeRetour = "2001",
                LibelleTourneeRetour = "Retour MDR VENDEE"
            },
            Saisie = new SaisieDto()
        };
    }
}
