using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using Cintre.App.Data;
using Cintre.App.Helpers;
using Cintre.App.Models;
using Cintre.App.Services;

namespace Cintre.App.ViewModels;

// Une ligne à afficher : un texte à gauche, une valeur à droite
public class LigneAffichage
{
    public string Texte { get; set; } = "";
    public string Valeur { get; set; } = "";
}

// Propriétaire : Valdez
public class RetraitViewModel : ViewModelBase
{
    private string _ticketSaisi = "";
    private string _montantSaisi = "";
    private string _messageErreur = "";
    private string _messageSucces = "";
    private Commande? _commande;
    private ModePaiement _mode = ModePaiement.Especes;

    // Déclenché quand le linge a été remis : la vue joue l'animation de succès
    public event Action? RetraitReussi;

    public ObservableCollection<LigneAffichage> Lignes { get; } = new();
    public ObservableCollection<LigneAffichage> Paiements { get; } = new();

    public RelayCommand RechercherCommand { get; }
    public RelayCommand EncaisserCommand { get; }
    public RelayCommand RemettreCommand { get; }

    public RetraitViewModel()
    {
        RechercherCommand = new RelayCommand(_ => Rechercher());
        EncaisserCommand = new RelayCommand(_ => Encaisser(), _ => PeutPayer);
        RemettreCommand = new RelayCommand(_ => Remettre(), _ => PeutRemettre);
    }

    // ===== Saisies =====

    public string TicketSaisi
    {
        get => _ticketSaisi;
        set => SetProperty(ref _ticketSaisi, value);
    }

    public string MontantSaisi
    {
        get => _montantSaisi;
        set => SetProperty(ref _montantSaisi, value);
    }

    // Les trois boutons radio du mode de paiement
    public bool ModeEspeces
    {
        get => _mode == ModePaiement.Especes;
        set { if (value) ChoisirMode(ModePaiement.Especes); }
    }

    public bool ModeMobileMoney
    {
        get => _mode == ModePaiement.MobileMoney;
        set { if (value) ChoisirMode(ModePaiement.MobileMoney); }
    }

    public bool ModeCarte
    {
        get => _mode == ModePaiement.Carte;
        set { if (value) ChoisirMode(ModePaiement.Carte); }
    }

    private void ChoisirMode(ModePaiement mode)
    {
        _mode = mode;
        OnPropertyChanged(nameof(ModeEspeces));
        OnPropertyChanged(nameof(ModeMobileMoney));
        OnPropertyChanged(nameof(ModeCarte));
    }

    // ===== Messages =====

    public string MessageErreur
    {
        get => _messageErreur;
        set
        {
            if (SetProperty(ref _messageErreur, value))
                OnPropertyChanged(nameof(AErreur));
        }
    }

    public bool AErreur => !string.IsNullOrEmpty(_messageErreur);

    public string MessageSucces
    {
        get => _messageSucces;
        set
        {
            if (SetProperty(ref _messageSucces, value))
                OnPropertyChanged(nameof(ASucces));
        }
    }

    public bool ASucces => !string.IsNullOrEmpty(_messageSucces);

    // ===== Affichage du résultat =====

    public bool AResultat => _commande != null;
    public bool SansResultat => _commande == null;

    public string ClientNom => _commande?.ClientNom ?? "";
    public string NumeroTicket => _commande?.NumeroTicket ?? "";
    public string TotalTexte => Fcfa(_commande?.MontantTotal ?? 0);
    public string PayeTexte => Fcfa(_commande?.MontantPaye ?? 0);
    public string ResteTexte => Fcfa(_commande?.Reste ?? 0);

    public string StatutTexte => _commande?.Statut switch
    {
        StatutCommande.Recu => "Reçu",
        StatutCommande.EnTraitement => "En traitement",
        StatutCommande.Pret => "Prêt",
        StatutCommande.Retire => "Retiré",
        StatutCommande.Annule => "Annulé",
        _ => ""
    };

    public Brush CouleurStatut => Couleur(_commande?.Statut switch
    {
        StatutCommande.Recu => "StatutRecuBrush",
        StatutCommande.EnTraitement => "StatutTraitementBrush",
        StatutCommande.Pret => "StatutPretBrush",
        StatutCommande.Retire => "StatutRetireBrush",
        StatutCommande.Annule => "StatutAnnuleBrush",
        _ => "TexteDiscretBrush"
    });

    // Rouge tant qu'il reste de l'argent à encaisser, vert quand c'est soldé
    public Brush CouleurReste => Couleur((_commande?.Reste ?? 0) > 0 ? "DangerBrush" : "SuccesBrush");

    public bool PeutPayer =>
        _commande != null && _commande.Statut != StatutCommande.Annule && _commande.Reste > 0;

    public bool PeutRemettre =>
        _commande != null && _commande.Statut == StatutCommande.Pret && _commande.Reste <= 0;

    // Explique à l'utilisateur pourquoi le bouton "Remettre le linge" est (in)actif
    public string AideRetrait
    {
        get
        {
            if (_commande == null) return "";
            if (_commande.Statut == StatutCommande.Annule) return "Cette commande est annulée.";
            if (_commande.Statut == StatutCommande.Retire)
                return $"Déjà retiré le {_commande.DateRetrait:dd/MM/yyyy 'à' HH:mm}.";
            if (_commande.Statut != StatutCommande.Pret) return "Linge pas encore prêt.";
            if (_commande.Reste > 0) return $"Il reste {_commande.Reste:N0} FCFA à payer.";
            return "Tout est payé : le linge peut être remis.";
        }
    }

    // ===== Actions =====

    private void Rechercher()
    {
        MessageErreur = "";
        MessageSucces = "";

        var ticket = (TicketSaisi ?? "").Trim().ToUpperInvariant();
        if (ticket.Length == 0)
        {
            Effacer();
            MessageErreur = "Saisis un numéro de ticket.";
            return;
        }

        try
        {
            var c = CommandeRepository.ObtenirParTicket(ticket);
            if (c == null)
            {
                Effacer();
                MessageErreur = $"Aucune commande trouvée pour le ticket {ticket}.";
                return;
            }
            Charger(c);
        }
        catch (Exception ex)
        {
            // Un problème de base ou de ticket ne doit jamais faire planter l'écran
            Effacer();
            MessageErreur = "Recherche impossible : " + ex.Message;
        }
    }

    private void Encaisser()
    {
        MessageErreur = "";
        MessageSucces = "";

        if (_commande == null) return;

        var utilisateur = Session.UtilisateurCourant;
        if (utilisateur == null)
        {
            MessageErreur = "Aucun utilisateur connecté.";
            return;
        }

        if (!LireMontant(out decimal montant))
        {
            MessageErreur = "Saisis un montant valide.";
            return;
        }

        var (ok, message) = PaiementService.Valider(_commande, montant);
        if (!ok)
        {
            MessageErreur = message;
            return;
        }

        try
        {
            var p = new Paiement
            {
                CommandeId = _commande.Id,
                Montant = montant,
                Mode = _mode,
                UtilisateurId = utilisateur.Id
            };
            p.Id = PaiementRepository.Enregistrer(p);
            p.DatePaiement = DateTime.Now;

            // On recharge la commande pour avoir le vrai montant payé et le vrai reste
            var maj = CommandeRepository.ObtenirParId(_commande.Id);
            if (maj == null)
            {
                _commande.MontantPaye += montant;
                maj = _commande;
            }
            Charger(maj);

            if (MessageService.Confirmer($"Paiement de {montant:N0} FCFA enregistré.\nImprimer le reçu ?"))
                RecuService.Imprimer(p, maj);
        }
        catch (Exception ex)
        {
            MessageErreur = "Paiement impossible : " + ex.Message;
        }
    }

    private void Remettre()
    {
        MessageErreur = "";
        MessageSucces = "";

        if (_commande == null) return;

        var utilisateur = Session.UtilisateurCourant;
        if (utilisateur == null)
        {
            MessageErreur = "Aucun utilisateur connecté.";
            return;
        }

        // Toutes les règles (statut Prêt, solde à zéro, points) sont dans RetraitService
        var (ok, message) = RetraitService.Retirer(_commande.NumeroTicket, utilisateur.Id);
        if (!ok)
        {
            MessageErreur = message;
            return;
        }

        try
        {
            var maj = CommandeRepository.ObtenirParTicket(_commande.NumeroTicket);
            if (maj != null) Charger(maj);
        }
        catch (Exception ex)
        {
            MessageErreur = "Le retrait est fait, mais l'affichage n'a pas pu être actualisé : " + ex.Message;
        }

        MessageSucces = message;
        RetraitReussi?.Invoke();
    }

    // ===== Outils =====

    private void Charger(Commande c)
    {
        _commande = c;

        Lignes.Clear();
        foreach (var l in c.Lignes)
        {
            Lignes.Add(new LigneAffichage
            {
                Texte = $"{l.Quantite} × {l.TypeVetementNom} ({l.ServiceNom})",
                Valeur = Fcfa(l.SousTotal)
            });
        }

        Paiements.Clear();
        foreach (var p in PaiementRepository.ObtenirParCommande(c.Id))
        {
            Paiements.Add(new LigneAffichage
            {
                Texte = $"{p.DatePaiement:dd/MM/yyyy HH:mm} · {RecuService.LibelleMode(p.Mode)}",
                Valeur = Fcfa(p.Montant)
            });
        }

        // Le champ montant est prérempli avec le reste à payer
        MontantSaisi = c.Reste > 0 ? c.Reste.ToString("0") : "";
        RafraichirResultat();
    }

    private void Effacer()
    {
        _commande = null;
        Lignes.Clear();
        Paiements.Clear();
        MontantSaisi = "";
        RafraichirResultat();
    }

    private void RafraichirResultat()
    {
        foreach (var nom in new[]
        {
            nameof(AResultat), nameof(SansResultat), nameof(ClientNom), nameof(NumeroTicket),
            nameof(TotalTexte), nameof(PayeTexte), nameof(ResteTexte), nameof(StatutTexte),
            nameof(CouleurStatut), nameof(CouleurReste), nameof(PeutPayer), nameof(PeutRemettre),
            nameof(AideRetrait)
        })
        {
            OnPropertyChanged(nom);
        }
    }

    // Accepte "2500", "2 500" ou "2500,0" ; les espaces (même insécables) sont ignorés
    private bool LireMontant(out decimal montant)
    {
        var texte = (MontantSaisi ?? "")
            .Replace(" ", "").Replace("\u00A0", "").Replace("\u202F", "");

        return decimal.TryParse(texte, NumberStyles.Number, CultureInfo.CurrentCulture, out montant)
            || decimal.TryParse(texte, NumberStyles.Number, CultureInfo.InvariantCulture, out montant);
    }

    private static string Fcfa(decimal montant) => montant.ToString("N0") + " FCFA";

    private static Brush Couleur(string cle)
        => Application.Current?.TryFindResource(cle) as Brush ?? Brushes.Gray;
}