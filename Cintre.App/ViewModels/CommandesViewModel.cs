using System;
using System.Collections.ObjectModel;
using System.Linq;
using Cintre.App.Data;
using Cintre.App.Helpers;
using Cintre.App.Models;
using Cintre.App.Services;

namespace Cintre.App.ViewModels;

// Propriétaire : Hugo
public class CommandesViewModel : ViewModelBase
{
    private string _texte = "";
    private bool _seulementRetard;
    private StatutCommande? _filtreStatut;
    private StatutCommande? _suivant;
    private Commande? _selection;
    private Commande? _detail;
    private bool _aUneSelection, _peutPasserSuivant, _peutAnnuler, _aucuneCommande;
    private string _texteBoutonSuivant = "";

    public ObservableCollection<Commande> Commandes { get; } = new();
    public ObservableCollection<HistoriqueStatut> Historique { get; } = new();

    public RelayCommand FiltrerStatutCommand { get; }
    public RelayCommand PasserSuivantCommand { get; }
    public RelayCommand AnnulerCommand { get; }

    public string Texte { get => _texte; set { SetProperty(ref _texte, value); Actualiser(); } }
    public bool SeulementRetard { get => _seulementRetard; set { SetProperty(ref _seulementRetard, value); Actualiser(); } }
    public Commande? Selection { get => _selection; set { SetProperty(ref _selection, value); ChargerDetail(); } }
    public Commande? Detail { get => _detail; private set => SetProperty(ref _detail, value); }
    public bool AUneSelection { get => _aUneSelection; private set => SetProperty(ref _aUneSelection, value); }
    public bool PeutPasserSuivant { get => _peutPasserSuivant; private set => SetProperty(ref _peutPasserSuivant, value); }
    public bool PeutAnnuler { get => _peutAnnuler; private set => SetProperty(ref _peutAnnuler, value); }
    public bool AucuneCommande { get => _aucuneCommande; private set => SetProperty(ref _aucuneCommande, value); }
    public string TexteBoutonSuivant { get => _texteBoutonSuivant; private set => SetProperty(ref _texteBoutonSuivant, value); }

    public CommandesViewModel()
    {
        // TEMPORAIRE : à supprimer quand la connexion de Belvian sera fusionnée
        if (Session.UtilisateurCourant == null)
            Session.UtilisateurCourant = new Utilisateur { Id = 1, Nom = "Test", Login = "admin", Role = RoleUtilisateur.Admin };

        FiltrerStatutCommand = new RelayCommand(p =>
        {
            if (Enum.TryParse<StatutCommande>(p as string, out var s)) _filtreStatut = s;
            else _filtreStatut = null; // "Tous"
            Actualiser();
        });
        PasserSuivantCommand = new RelayCommand(_ => PasserSuivant());
        AnnulerCommand = new RelayCommand(_ => Annuler());
        Actualiser();
    }

    private void Actualiser()
    {
        try
        {
            var idSelectionne = _selection?.Id;
            var liste = CommandeRepository.Rechercher(_texte, _filtreStatut);
            // on filtre les retards ici : la règle est portée par Commande.EnRetard
            if (_seulementRetard) liste = liste.Where(c => c.EnRetard).ToList();

            Commandes.Clear();
            foreach (var c in liste) Commandes.Add(c);
            AucuneCommande = Commandes.Count == 0;
            Selection = Commandes.FirstOrDefault(c => c.Id == idSelectionne);
        }
        catch (Exception ex)
        {
            MessageService.Erreur("Impossible de charger les commandes : " + ex.Message);
        }
    }

    private void ChargerDetail()
    {
        Historique.Clear();
        if (_selection == null)
        {
            Detail = null; AUneSelection = false;
            PeutPasserSuivant = false; PeutAnnuler = false; TexteBoutonSuivant = "";
            return;
        }
        try
        {
            Detail = CommandeRepository.ObtenirParId(_selection.Id);
            foreach (var h in CommandeRepository.ObtenirHistorique(_selection.Id)) Historique.Add(h);
        }
        catch (Exception ex)
        {
            MessageService.Erreur("Impossible de charger le détail : " + ex.Message);
        }
        AUneSelection = Detail != null;

        _suivant = _selection.Statut switch
        {
            StatutCommande.Recu => StatutCommande.EnTraitement,
            StatutCommande.EnTraitement => StatutCommande.Pret,
            _ => null
        };
        PeutPasserSuivant = _suivant != null;
        TexteBoutonSuivant = _suivant == StatutCommande.EnTraitement ? "Passer à En traitement"
                           : _suivant == StatutCommande.Pret ? "Passer à Prêt" : "";

        var estOperateur = Session.UtilisateurCourant?.Role == RoleUtilisateur.Operateur;
        var annulable = _selection.Statut is StatutCommande.Recu or StatutCommande.EnTraitement or StatutCommande.Pret;
        // payée : seul un Admin peut annuler (règle 8.4)
        PeutAnnuler = annulable && !estOperateur && (_selection.MontantPaye == 0 || Session.EstAdmin);
    }

    private void PasserSuivant()
    {
        var user = Session.UtilisateurCourant;
        if (_selection == null || _suivant == null || user == null) return;
        if (!CommandeService.TransitionAutorisee(_selection.Statut, _suivant.Value, Session.EstAdmin))
        {
            MessageService.Erreur("Ce changement de statut n'est pas autorisé.");
            return;
        }
        try
        {
            CommandeRepository.ChangerStatut(_selection.Id, _suivant.Value, user.Id);
            MessageService.Info("Statut mis à jour.");
            Actualiser();
        }
        catch (Exception ex) { MessageService.Erreur(ex.Message); }
    }

    private void Annuler()
    {
        var user = Session.UtilisateurCourant;
        if (_selection == null || user == null) return;
        if (!MessageService.Confirmer($"Annuler la commande {_selection.NumeroTicket} ?")) return;
        try
        {
            CommandeService.Annuler(_selection.Id, user.Id);
            MessageService.Info("Commande annulée.");
            Actualiser();
        }
        catch (Exception ex) { MessageService.Erreur(ex.Message); }
    }
}