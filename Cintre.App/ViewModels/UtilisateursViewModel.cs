using System.Collections.ObjectModel;
using Cintre.App.Data;
using Cintre.App.Helpers;
using Cintre.App.Models;
using Cintre.App.Services;
using Npgsql;

namespace Cintre.App.ViewModels;

// Propriétaire : Belvian
public class UtilisateursViewModel : ViewModelBase
{
    private const int LongueurMinMotDePasse = 6;

    private Utilisateur? _selection;
    private int _idEnEdition;                 // 0 = nouveau compte
    private bool _nouveauCompte = true;
    private string _nom = "", _login = "";
    private RoleUtilisateur _role = RoleUtilisateur.Operateur;
    private string _texteBasculer = "Désactiver";
    private string _messageErreur = "", _messageSucces = "";

    public ObservableCollection<Utilisateur> Utilisateurs { get; } = new();
    public Array Roles { get; } = Enum.GetValues(typeof(RoleUtilisateur));

    // Les mots de passe sont recopiés depuis les PasswordBox par la vue (comme ConnexionBaseWindow)
    public string MotDePasseForm { get; set; } = "";
    public string AncienMotDePasse { get; set; } = "";
    public string NouveauMotDePasse { get; set; } = "";
    public string ConfirmationMotDePasse { get; set; } = "";

    // La vue vide les PasswordBox quand ce signal arrive
    public event Action? EffacerMotsDePasse;

    public Utilisateur? Selection
    {
        get => _selection;
        set
        {
            SetProperty(ref _selection, value);
            ChargerFormulaire(value);
        }
    }

    public bool NouveauCompte { get => _nouveauCompte; set => SetProperty(ref _nouveauCompte, value); }
    public string Nom { get => _nom; set => SetProperty(ref _nom, value); }
    public string Login { get => _login; set => SetProperty(ref _login, value); }
    public RoleUtilisateur Role { get => _role; set => SetProperty(ref _role, value); }
    public string TexteBasculer { get => _texteBasculer; set => SetProperty(ref _texteBasculer, value); }
    public string MessageErreur { get => _messageErreur; set => SetProperty(ref _messageErreur, value); }
    public string MessageSucces { get => _messageSucces; set => SetProperty(ref _messageSucces, value); }

    public RelayCommand NouveauCommand { get; }
    public RelayCommand EnregistrerCommand { get; }
    public RelayCommand BasculerEtatCommand { get; }
    public RelayCommand ReinitialiserCommand { get; }
    public RelayCommand ChangerMonMotDePasseCommand { get; }

    private readonly bool _autorise;

    public UtilisateursViewModel()
    {
        NouveauCommand = new RelayCommand(_ => Nouveau());
        EnregistrerCommand = new RelayCommand(_ => Enregistrer());
        BasculerEtatCommand = new RelayCommand(_ => BasculerEtat());
        ReinitialiserCommand = new RelayCommand(_ => Reinitialiser());
        ChangerMonMotDePasseCommand = new RelayCommand(_ => ChangerMonMotDePasse());

        // Écran sensible : on revérifie le droit d'accès (règle 8.7)
        _autorise = AuthService.PeutAcceder("Utilisateurs");
        if (!_autorise)
        {
            MessageErreur = "Accès réservé à l'administrateur.";
            return;
        }

        ChargerListe(null);
    }

    // ---------- Messages d'erreur ----------

    // Traduit une exception technique en message clair pour l'utilisateur
    private static string MessageTechnique(Exception ex) => ex switch
    {
        PostgresException => "La base de données a refusé l'opération.",
        NpgsqlException or TimeoutException or System.Net.Sockets.SocketException
            => "Impossible de joindre la base de données. Vérifie la connexion au serveur.",
        _ => "Une erreur inattendue est survenue."
    };

    // ---------- Liste et formulaire ----------

    private void ChargerListe(int? idASelectionner)
    {
        try
        {
            var tous = UtilisateurRepository.ObtenirTous();
            Utilisateurs.Clear();
            foreach (var u in tous)
                Utilisateurs.Add(u);

            Selection = idASelectionner == null
                ? null
                : Utilisateurs.FirstOrDefault(u => u.Id == idASelectionner);
        }
        catch (Exception ex)
        {
            MessageErreur = MessageTechnique(ex);
        }
    }

    private void ChargerFormulaire(Utilisateur? u)
    {
        MessageErreur = "";
        MessageSucces = "";

        if (u == null)
        {
            _idEnEdition = 0;
            NouveauCompte = true;
            Nom = "";
            Login = "";
            Role = RoleUtilisateur.Operateur;
            TexteBasculer = "Désactiver";
        }
        else
        {
            _idEnEdition = u.Id;
            NouveauCompte = false;       // le login ne change plus après la création
            Nom = u.Nom;
            Login = u.Login;
            Role = u.Role;
            TexteBasculer = u.Actif ? "Désactiver" : "Activer";
        }

        EffacerMotsDePasse?.Invoke();
    }

    private void Nouveau()
    {
        if (!_autorise) return;
        Selection = null;
    }

    // ---------- Actions ----------

    private void Enregistrer()
    {
        if (!_autorise) return;
        MessageErreur = "";
        MessageSucces = "";

        if (string.IsNullOrWhiteSpace(Nom))
        {
            MessageErreur = "Le nom est obligatoire.";
            return;
        }

        if (_idEnEdition == 0)
            Creer();
        else
            Modifier();
    }

    private void Creer()
    {
        var login = Login.Trim();
        if (login.Length < 3 || login.Any(char.IsWhiteSpace))
        {
            MessageErreur = "Le login doit faire au moins 3 caractères, sans espace.";
            return;
        }
        if (MotDePasseForm.Length < LongueurMinMotDePasse)
        {
            MessageErreur = $"Le mot de passe doit faire au moins {LongueurMinMotDePasse} caractères.";
            return;
        }

        var u = new Utilisateur
        {
            Nom = Nom.Trim(),
            Login = login,
            MotDePasseHash = AuthService.Hacher(MotDePasseForm),   // jamais le mot de passe en clair
            Role = Role,
            Actif = true
        };

        try
        {
            var id = UtilisateurRepository.Ajouter(u);
            ChargerListe(id);
            MessageSucces = $"Compte « {login} » créé.";
        }
        catch (PostgresException ex) when (ex.SqlState == "23505")   // violation d'unicité
        {
            MessageErreur = "Ce login est déjà utilisé.";
        }
        catch (Exception ex)
        {
            MessageErreur = MessageTechnique(ex);
        }
    }

    private void Modifier()
    {
        var courant = Session.UtilisateurCourant;

        // Interdit de rétrograder son propre compte
        if (courant != null && courant.Id == _idEnEdition && Role != courant.Role)
        {
            MessageErreur = "Tu ne peux pas changer ton propre rôle.";
            Role = courant.Role;
            return;
        }

        try
        {
            UtilisateurRepository.Modifier(new Utilisateur
            {
                Id = _idEnEdition,
                Nom = Nom.Trim(),
                Role = Role
            });
            ChargerListe(_idEnEdition);
            MessageSucces = "Modifications enregistrées.";
        }
        catch (Exception ex)
        {
            MessageErreur = MessageTechnique(ex);
        }
    }

    private void BasculerEtat()
    {
        if (!_autorise) return;
        MessageErreur = "";
        MessageSucces = "";

        var u = Selection;
        if (u == null)
        {
            MessageErreur = "Sélectionne d'abord un utilisateur dans la liste.";
            return;
        }

        // Interdit de désactiver son propre compte
        if (u.Id == Session.UtilisateurCourant?.Id)
        {
            MessageErreur = "Tu ne peux pas désactiver ton propre compte.";
            return;
        }

        try
        {
            var nouvelEtat = !u.Actif;
            UtilisateurRepository.ChangerEtat(u.Id, nouvelEtat);
            ChargerListe(u.Id);
            MessageSucces = nouvelEtat ? "Compte activé." : "Compte désactivé.";
        }
        catch (Exception ex)
        {
            MessageErreur = MessageTechnique(ex);
        }
    }

    // L'admin choisit un nouveau mot de passe pour l'utilisateur sélectionné
    private void Reinitialiser()
    {
        if (!_autorise) return;
        MessageErreur = "";
        MessageSucces = "";

        var u = Selection;
        if (u == null)
        {
            MessageErreur = "Sélectionne d'abord un utilisateur dans la liste.";
            return;
        }
        if (MotDePasseForm.Length < LongueurMinMotDePasse)
        {
            MessageErreur = $"Saisis un nouveau mot de passe d'au moins {LongueurMinMotDePasse} caractères.";
            return;
        }

        try
        {
            UtilisateurRepository.ChangerMotDePasse(u.Id, AuthService.Hacher(MotDePasseForm));
            var login = u.Login;
            EffacerMotsDePasse?.Invoke();
            MessageSucces = $"Mot de passe de « {login} » réinitialisé.";
        }
        catch (Exception ex)
        {
            MessageErreur = MessageTechnique(ex);
        }
    }

    // L'utilisateur connecté change son propre mot de passe (l'ancien est demandé)
    private void ChangerMonMotDePasse()
    {
        if (!_autorise) return;
        MessageErreur = "";
        MessageSucces = "";

        var courant = Session.UtilisateurCourant;
        if (courant == null)
        {
            MessageErreur = "Aucun utilisateur connecté.";
            return;
        }
        if (NouveauMotDePasse.Length < LongueurMinMotDePasse)
        {
            MessageErreur = $"Le nouveau mot de passe doit faire au moins {LongueurMinMotDePasse} caractères.";
            return;
        }
        if (NouveauMotDePasse != ConfirmationMotDePasse)
        {
            MessageErreur = "La confirmation ne correspond pas au nouveau mot de passe.";
            return;
        }

        try
        {
            // On relit le hash en base : celui de la session peut être périmé
            var enBase = UtilisateurRepository.ObtenirParLogin(courant.Login);
            if (enBase == null || !AuthService.Verifier(AncienMotDePasse, enBase.MotDePasseHash))
            {
                MessageErreur = "L'ancien mot de passe est incorrect.";
                return;
            }

            var hash = AuthService.Hacher(NouveauMotDePasse);
            UtilisateurRepository.ChangerMotDePasse(courant.Id, hash);
            courant.MotDePasseHash = hash;

            EffacerMotsDePasse?.Invoke();
            MessageSucces = "Ton mot de passe a été changé.";
        }
        catch (Exception ex)
        {
            MessageErreur = MessageTechnique(ex);
        }
    }
}