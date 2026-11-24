namespace Cintre.App.Models;

// Rôle d'un employé : il décide des écrans visibles (règle 8.7)
public enum RoleUtilisateur { Admin, Caissier, Operateur }

// Cycle de vie d'une commande (règle 8.4), dans l'ordre normal
public enum StatutCommande { Recu, EnTraitement, Pret, Retire, Annule }

public enum ModePaiement { Especes, MobileMoney, Carte }

public enum TypeIncident { Perte, Dommage, Autre }