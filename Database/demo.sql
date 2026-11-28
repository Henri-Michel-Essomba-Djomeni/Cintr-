-- Données de démonstration Cintré. À lancer à la main (pgAdmin ou psql), jamais embarqué dans l'application.
-- Prérequis : avoir lancé l'application une fois (elle crée les tables et l'utilisateur admin).
-- Relançable sans danger : si les données de démo existent déjà, il ne fait rien.
--
-- Pour tout supprimer ensuite :
--   DELETE FROM incident WHERE commande_id IN (SELECT id FROM commande WHERE remarque = '[DEMO]');
--   DELETE FROM paiement WHERE commande_id IN (SELECT id FROM commande WHERE remarque = '[DEMO]');
--   DELETE FROM commande WHERE remarque = '[DEMO]';
--   DELETE FROM client WHERE email LIKE '%@example.com' AND id NOT IN (SELECT client_id FROM commande);

DO $$
DECLARE
  v_user INT;
BEGIN
  SELECT id INTO v_user FROM utilisateur ORDER BY id LIMIT 1;
  IF v_user IS NULL THEN
    RAISE EXCEPTION 'Aucun utilisateur trouvé : lance l''application une fois avant ce script.';
  END IF;

  IF EXISTS (SELECT 1 FROM commande WHERE remarque = '[DEMO]') THEN
    RAISE NOTICE 'Données de démo déjà présentes : rien à faire.';
    RETURN;
  END IF;

  -- ===== Clients =====
  INSERT INTO client (nom, telephone, email, adresse, points_fidelite, cree_le) VALUES
    ('Amina Nkoulou',      '+237 677 11 22 33', 'amina.nkoulou@example.com', 'Bastos, Yaoundé',        64, NOW() - INTERVAL '60 days'),
    ('Jean-Paul Mbarga',   '+237 699 44 55 66', 'jp.mbarga@example.com',     'Mvan, Yaoundé',           32, NOW() - INTERVAL '45 days'),
    ('Sandrine Fotso',     '+237 655 77 88 99', NULL,                         'Essos, Yaoundé',          18, NOW() - INTERVAL '30 days'),
    ('Paul Etoundi',       '+237 677 20 30 40', 'paul.etoundi@example.com',  'Biyem-Assi, Yaoundé',     51, NOW() - INTERVAL '50 days'),
    ('Carine Tchoumi',     '+237 690 12 34 56', NULL,                         'Mimboman, Yaoundé',       12, NOW() - INTERVAL '20 days'),
    ('Boris Kamga',        '+237 655 98 76 54', 'boris.kamga@example.com',   'Nlongkak, Yaoundé',        7, NOW() - INTERVAL '15 days'),
    ('Marie Ngo Bayiha',   '+237 670 65 43 21', NULL,                         'Omnisports, Yaoundé',     26, NOW() - INTERVAL '35 days'),
    ('Hervé Ndongo',       '+237 699 01 02 03', 'herve.ndongo@example.com',  'Mendong, Yaoundé',         4, NOW() - INTERVAL '10 days')
  ON CONFLICT (telephone) DO NOTHING;

  -- ===== Commandes (jours_depot = il y a combien de jours ; jours_retrait = -1 si pas retirée ;
  --                  pct = part déjà payée en % ; mode = mode du paiement) =====
  CREATE TEMP TABLE demo_cmd (
    n INT, tel TEXT, jours_depot INT, delai INT, statut TEXT, urgent BOOLEAN,
    remise NUMERIC, jours_retrait INT, pct INT, mode TEXT
  ) ON COMMIT DROP;

  INSERT INTO demo_cmd VALUES
    (801, '+237 677 11 22 33',  0, 3, 'Recu',         FALSE,  0, -1,   0, 'Especes'),
    (802, '+237 699 44 55 66',  0, 2, 'EnTraitement', TRUE,   0, -1,  50, 'Especes'),
    (803, '+237 655 77 88 99',  1, 3, 'EnTraitement', FALSE,  0, -1,   0, 'Especes'),
    (804, '+237 677 20 30 40',  2, 3, 'Pret',         FALSE,  0, -1,  50, 'MobileMoney'),
    (805, '+237 690 12 34 56',  3, 3, 'Pret',         FALSE, 10, -1,   0, 'Especes'),
    (806, '+237 655 98 76 54',  5, 3, 'EnTraitement', FALSE,  0, -1,   0, 'Especes'),
    (807, '+237 677 11 22 33',  6, 3, 'Pret',         FALSE,  0, -1, 100, 'Carte'),
    (808, '+237 670 65 43 21',  4, 2, 'Retire',       FALSE,  0,  0, 100, 'Especes'),
    (809, '+237 699 01 02 03',  5, 3, 'Retire',       FALSE,  0,  1, 100, 'MobileMoney'),
    (810, '+237 699 44 55 66',  7, 3, 'Retire',       FALSE,  0,  3, 100, 'Especes'),
    (811, '+237 655 77 88 99',  8, 3, 'Retire',       TRUE,  10,  4, 100, 'Carte'),
    (812, '+237 690 12 34 56',  9, 3, 'Retire',       FALSE,  0,  6, 100, 'MobileMoney'),
    (813, '+237 677 20 30 40', 10, 3, 'Retire',       FALSE,  0,  7, 100, 'Especes'),
    (814, '+237 655 98 76 54',  2, 3, 'Annule',       FALSE,  0, -1,   0, 'Especes'),
    (815, '+237 670 65 43 21', 45, 3, 'Pret',         FALSE,  0, -1,   0, 'Especes'),
    (816, '+237 699 01 02 03', 12, 3, 'Retire',       FALSE,  0,  9, 100, 'Especes');

  INSERT INTO commande (numero_ticket, client_id, utilisateur_id, date_depot, date_prevue, date_retrait,
                        statut, urgent, remise_pourcent, remarque)
  SELECT 'CT-' || to_char(NOW() - make_interval(days => d.jours_depot), 'YYYYMMDD') || '-' || lpad(d.n::text, 4, '0'),
         c.id,
         v_user,
         NOW() - make_interval(days => d.jours_depot),
         NOW() - make_interval(days => d.jours_depot) + make_interval(days => d.delai),
         CASE WHEN d.jours_retrait >= 0 THEN NOW() - make_interval(days => d.jours_retrait) END,
         d.statut, d.urgent, d.remise, '[DEMO]'
  FROM demo_cmd d
  JOIN client c ON c.telephone = d.tel;

  -- ===== Lignes de commande (prix pris dans la grille des tarifs) =====
  CREATE TEMP TABLE demo_ligne (n INT, service TEXT, vetement TEXT, qte INT, couleur TEXT) ON COMMIT DROP;

  INSERT INTO demo_ligne VALUES
    (801, 'Lavage',          'Chemise',  4, 'Blanc'),
    (801, 'Repassage',       'Pantalon', 2, 'Noir'),
    (802, 'Nettoyage à sec', 'Costume',  1, 'Bleu nuit'),
    (802, 'Nettoyage à sec', 'Chemise',  2, 'Blanc'),
    (803, 'Lavage',          'Drap',     3, 'Blanc'),
    (803, 'Lavage',          'Couette',  1, 'Gris'),
    (804, 'Nettoyage à sec', 'Robe',     2, 'Rouge'),
    (804, 'Retouche',        'Robe',     1, 'Rouge'),
    (805, 'Lavage',          'Pantalon', 3, 'Beige'),
    (805, 'Repassage',       'Chemise',  5, 'Bleu'),
    (806, 'Lavage',          'Chemise',  6, 'Blanc'),
    (806, 'Repassage',       'Chemise',  6, 'Blanc'),
    (807, 'Nettoyage à sec', 'Veste',    2, 'Noir'),
    (808, 'Lavage',          'Robe',     2, 'Vert'),
    (808, 'Repassage',       'Robe',     2, 'Vert'),
    (809, 'Lavage',          'Chemise',  5, 'Bleu'),
    (810, 'Nettoyage à sec', 'Costume',  2, 'Gris'),
    (810, 'Nettoyage à sec', 'Chemise',  2, 'Blanc'),
    (811, 'Nettoyage à sec', 'Rideau',   2, 'Crème'),
    (811, 'Lavage',          'Drap',     2, 'Blanc'),
    (812, 'Lavage',          'Pantalon', 4, 'Noir'),
    (813, 'Repassage',       'Chemise',  8, 'Blanc'),
    (813, 'Repassage',       'Pantalon', 4, 'Noir'),
    (814, 'Lavage',          'Couette',  2, 'Bleu'),
    (815, 'Lavage',          'Drap',     4, 'Blanc'),
    (815, 'Lavage',          'Chemise',  3, 'Blanc'),
    (816, 'Nettoyage à sec', 'Veste',    1, 'Marron'),
    (816, 'Lavage',          'Pantalon', 2, 'Gris');

  INSERT INTO ligne_commande (commande_id, type_vetement_id, service_id, quantite, prix_unitaire, couleur)
  SELECT cm.id, tv.id, s.id, l.qte, t.prix, l.couleur
  FROM demo_ligne l
  JOIN commande cm ON cm.remarque = '[DEMO]'
                  AND cm.numero_ticket LIKE ('CT-%-' || lpad(l.n::text, 4, '0'))
  JOIN service s ON s.nom = l.service
  JOIN type_vetement tv ON tv.nom = l.vetement
  JOIN tarif t ON t.service_id = s.id AND t.type_vetement_id = tv.id;

  -- Total = somme des lignes, +30 % si urgent, moins la remise
  UPDATE commande cm
  SET montant_total = ROUND(
        COALESCE((SELECT SUM(lc.quantite * lc.prix_unitaire) FROM ligne_commande lc WHERE lc.commande_id = cm.id), 0)
        * CASE WHEN cm.urgent THEN 1.30 ELSE 1 END
        * (1 - cm.remise_pourcent / 100))
  WHERE cm.remarque = '[DEMO]';

  -- ===== Paiements =====
  INSERT INTO paiement (commande_id, montant, mode, date_paiement, utilisateur_id)
  SELECT cm.id,
         ROUND(cm.montant_total * d.pct / 100.0),
         d.mode,
         CASE WHEN d.pct = 100 AND d.jours_retrait >= 0 THEN cm.date_retrait ELSE cm.date_depot END,
         v_user
  FROM demo_cmd d
  JOIN commande cm ON cm.remarque = '[DEMO]'
                  AND cm.numero_ticket LIKE ('CT-%-' || lpad(d.n::text, 4, '0'))
  WHERE d.pct > 0 AND cm.montant_total > 0;

  UPDATE commande cm
  SET montant_paye = COALESCE((SELECT SUM(p.montant) FROM paiement p WHERE p.commande_id = cm.id), 0)
  WHERE cm.remarque = '[DEMO]';

  -- ===== Historique des statuts =====
  INSERT INTO historique_statut (commande_id, ancien_statut, nouveau_statut, date_changement, utilisateur_id)
  SELECT id, NULL, 'Recu', date_depot, v_user FROM commande WHERE remarque = '[DEMO]';

  INSERT INTO historique_statut (commande_id, ancien_statut, nouveau_statut, date_changement, utilisateur_id)
  SELECT id, 'Recu', 'EnTraitement', LEAST(date_depot + INTERVAL '1 hour', NOW()), v_user
  FROM commande WHERE remarque = '[DEMO]' AND statut IN ('EnTraitement', 'Pret', 'Retire');

  INSERT INTO historique_statut (commande_id, ancien_statut, nouveau_statut, date_changement, utilisateur_id)
  SELECT id, 'EnTraitement', 'Pret', LEAST(date_prevue - INTERVAL '12 hours', NOW()), v_user
  FROM commande WHERE remarque = '[DEMO]' AND statut IN ('Pret', 'Retire');

  INSERT INTO historique_statut (commande_id, ancien_statut, nouveau_statut, date_changement, utilisateur_id)
  SELECT id, 'Pret', 'Retire', date_retrait, v_user
  FROM commande WHERE remarque = '[DEMO]' AND statut = 'Retire';

  INSERT INTO historique_statut (commande_id, ancien_statut, nouveau_statut, date_changement, utilisateur_id)
  SELECT id, 'Recu', 'Annule', LEAST(date_depot + INTERVAL '1 hour', NOW()), v_user
  FROM commande WHERE remarque = '[DEMO]' AND statut = 'Annule';

  -- ===== Incidents =====
  INSERT INTO incident (commande_id, type, description, date_incident, resolu, resolution)
  SELECT cm.id, x.genre, x.texte, NOW() - make_interval(days => x.jours), x.resolu, x.solution
  FROM (VALUES
    (806, 'Dommage', 'Tache d''huile restée sur une chemise blanche après lavage.', 1, FALSE, NULL),
    (811, 'Dommage', 'Bouton de veste arraché au repassage.', 3, TRUE, 'Bouton remplacé gratuitement, client informé.'),
    (803, 'Perte',   'Un drap introuvable au comptage à la réception.', 0, FALSE, NULL)
  ) AS x(n, genre, texte, jours, resolu, solution)
  JOIN commande cm ON cm.remarque = '[DEMO]'
                  AND cm.numero_ticket LIKE ('CT-%-' || lpad(x.n::text, 4, '0'));

  RAISE NOTICE 'Données de démo créées : 8 clients, 16 commandes, paiements, historique et 3 incidents.';
END $$;