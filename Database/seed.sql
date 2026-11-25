-- Cintré : données de départ (lancées une seule fois, à la création de la base).

INSERT INTO parametre (cle, valeur) VALUES
  ('nom_pressing','Cintré'), ('adresse','Yaoundé, Cameroun'),
  ('telephone','+237 600 000 000'), ('devise','FCFA'),
  ('delai_standard_jours','3'), ('supplement_urgence_pourcent','30'),
  ('seuil_points_remise','50'), ('remise_fidelite_pourcent','10'),
  ('jours_abandon','30')
ON CONFLICT (cle) DO NOTHING;

INSERT INTO service (nom, description) VALUES
  ('Lavage','Lavage et séchage'), ('Repassage','Repassage seul'),
  ('Nettoyage à sec','Pour les tissus délicats'), ('Retouche','Petites réparations')
ON CONFLICT (nom) DO NOTHING;

INSERT INTO type_vetement (nom) VALUES
  ('Chemise'),('Pantalon'),('Robe'),('Costume'),('Veste'),('Couette'),('Drap'),('Rideau')
ON CONFLICT (nom) DO NOTHING;

INSERT INTO tarif (service_id, type_vetement_id, prix)
SELECT s.id, t.id, x.prix
FROM (VALUES
  ('Lavage','Chemise',500), ('Lavage','Pantalon',600), ('Lavage','Robe',1000),
  ('Lavage','Drap',1000), ('Lavage','Couette',3000),
  ('Repassage','Chemise',300), ('Repassage','Pantalon',300), ('Repassage','Robe',500),
  ('Nettoyage à sec','Chemise',1000), ('Nettoyage à sec','Pantalon',1200),
  ('Nettoyage à sec','Robe',2000), ('Nettoyage à sec','Costume',4000),
  ('Nettoyage à sec','Veste',2500), ('Nettoyage à sec','Rideau',3500),
  ('Retouche','Pantalon',1000), ('Retouche','Robe',1500)
) AS x(nom_service, nom_type, prix)
JOIN service s ON s.nom = x.nom_service
JOIN type_vetement t ON t.nom = x.nom_type
ON CONFLICT (service_id, type_vetement_id) DO NOTHING;
