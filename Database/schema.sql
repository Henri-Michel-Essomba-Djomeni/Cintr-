-- Cintré : schéma de la base. Peut être relancé sans danger (IF NOT EXISTS).

CREATE TABLE IF NOT EXISTS utilisateur (
  id SERIAL PRIMARY KEY,
  nom VARCHAR(100) NOT NULL,
  login VARCHAR(50) NOT NULL UNIQUE,
  mot_de_passe_hash VARCHAR(100) NOT NULL,
  role VARCHAR(20) NOT NULL CHECK (role IN ('Admin','Caissier','Operateur')),
  actif BOOLEAN NOT NULL DEFAULT TRUE,
  cree_le TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS client (
  id SERIAL PRIMARY KEY,
  nom VARCHAR(120) NOT NULL,
  telephone VARCHAR(30) NOT NULL UNIQUE,
  email VARCHAR(120),
  adresse VARCHAR(200),
  points_fidelite INT NOT NULL DEFAULT 0,
  cree_le TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS service (
  id SERIAL PRIMARY KEY,
  nom VARCHAR(80) NOT NULL UNIQUE,
  description VARCHAR(200),
  actif BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE TABLE IF NOT EXISTS type_vetement (
  id SERIAL PRIMARY KEY,
  nom VARCHAR(80) NOT NULL UNIQUE
);

CREATE TABLE IF NOT EXISTS tarif (
  id SERIAL PRIMARY KEY,
  service_id INT NOT NULL REFERENCES service(id),
  type_vetement_id INT NOT NULL REFERENCES type_vetement(id),
  prix NUMERIC(10,0) NOT NULL CHECK (prix >= 0),
  UNIQUE (service_id, type_vetement_id)
);

CREATE TABLE IF NOT EXISTS commande (
  id SERIAL PRIMARY KEY,
  numero_ticket VARCHAR(30) NOT NULL UNIQUE,
  client_id INT NOT NULL REFERENCES client(id),
  utilisateur_id INT NOT NULL REFERENCES utilisateur(id),
  date_depot TIMESTAMP NOT NULL DEFAULT NOW(),
  date_prevue TIMESTAMP NOT NULL,
  date_retrait TIMESTAMP NULL,
  statut VARCHAR(20) NOT NULL DEFAULT 'Recu'
    CHECK (statut IN ('Recu','EnTraitement','Pret','Retire','Annule')),
  urgent BOOLEAN NOT NULL DEFAULT FALSE,
  remise_pourcent NUMERIC(5,2) NOT NULL DEFAULT 0,
  montant_total NUMERIC(12,0) NOT NULL DEFAULT 0,
  montant_paye NUMERIC(12,0) NOT NULL DEFAULT 0,
  remarque TEXT
);

CREATE TABLE IF NOT EXISTS ligne_commande (
  id SERIAL PRIMARY KEY,
  commande_id INT NOT NULL REFERENCES commande(id) ON DELETE CASCADE,
  type_vetement_id INT NOT NULL REFERENCES type_vetement(id),
  service_id INT NOT NULL REFERENCES service(id),
  quantite INT NOT NULL CHECK (quantite > 0),
  prix_unitaire NUMERIC(10,0) NOT NULL,
  couleur VARCHAR(40),
  remarque VARCHAR(200),
  photo BYTEA
);

CREATE TABLE IF NOT EXISTS paiement (
  id SERIAL PRIMARY KEY,
  commande_id INT NOT NULL REFERENCES commande(id),
  montant NUMERIC(12,0) NOT NULL CHECK (montant > 0),
  mode VARCHAR(20) NOT NULL CHECK (mode IN ('Especes','MobileMoney','Carte')),
  date_paiement TIMESTAMP NOT NULL DEFAULT NOW(),
  utilisateur_id INT NOT NULL REFERENCES utilisateur(id)
);

CREATE TABLE IF NOT EXISTS incident (
  id SERIAL PRIMARY KEY,
  commande_id INT NOT NULL REFERENCES commande(id),
  type VARCHAR(20) NOT NULL CHECK (type IN ('Perte','Dommage','Autre')),
  description TEXT NOT NULL,
  date_incident TIMESTAMP NOT NULL DEFAULT NOW(),
  resolu BOOLEAN NOT NULL DEFAULT FALSE,
  resolution TEXT
);

CREATE TABLE IF NOT EXISTS historique_statut (
  id SERIAL PRIMARY KEY,
  commande_id INT NOT NULL REFERENCES commande(id) ON DELETE CASCADE,
  ancien_statut VARCHAR(20),
  nouveau_statut VARCHAR(20) NOT NULL,
  date_changement TIMESTAMP NOT NULL DEFAULT NOW(),
  utilisateur_id INT REFERENCES utilisateur(id)
);

CREATE TABLE IF NOT EXISTS parametre (
  cle VARCHAR(50) PRIMARY KEY,
  valeur VARCHAR(200) NOT NULL
);
