# SiteRP SCP Systems — inspirations publiques de Faster Community

Objectif: reproduire proprement des mécaniques RP comparables, sans copier de code propriétaire. Cible: SCP:SL 14.2.7 + LabAPI + SiteRP.Core + UCR/UCI/ProjectMER.

## 1. SCP-079 / C.A.S.S.I.E. — priorité absolue

Etat RP persistant:
- C.A.S.S.I.E. = IA Fondation normale, alliée au Site.
- SCP-079 = état compromis/deconfiné de l'IA.
- L'état ne dépend pas du simple démarrage d'un round: il dépend d'un piratage, d'un événement RP ou d'une décision staff.

Capacités prévues:
- contrôle des portes et verrouillages;
- contrôle des lumières / blackouts;
- interaction avec caméras;
- haut-parleurs / messages IA;
- annonces ciblées;
- Tesla et systèmes de sécurité avec limites RP;
- gaz/flash par salle uniquement dans zones autorisées, avec cooldown et protections anti-abus;
- verrouillage temporaire d'un secteur;
- possibilité d'assister la Fondation tant que l'IA est saine;
- possibilité d'aider les SCP / Chaos si SCP-079 est réellement compromis.

Contre-jeu technicien:
- salle serveurs = point de diagnostic;
- commande / interaction de type check pour connaître état C.A.S.S.I.E./079;
- piratage par rôle autorisé (ex. Technicien Chaos / traître selon règles du serveur);
- reconnexion C.A.S.S.I.E. / neutralisation de 079 par Technicien FIM autorisé;
- journalisation de chaque hack, tentative, reconnexion et action critique.

## 2. SCP vanilla améliorés

### SCP-049
- peut réanimer des cadavres valides même s'il ne les a pas tués;
- relation RP possible avec Fondation en état CONTAINED/COOPERATIVE;
- devient HOSTILE sur agression/incident;
- 049-2 reçoit un lien maître vers 049;
- commandes RP locales pour ordres simples aux instances.

### SCP-939
- gaz amnésique;
- effet mémoire/vision temporaire côté victime;
- immunités prévues pour rôles robotiques;
- système de sons/mimétisme conservé;
- cooldowns et zones interdites pour éviter abus.

### SCP-106
- faiblesse à la lumière / lampe;
- Femur Breaker réellement utilisable pour reconfinement;
- état CONTAINED/BREACHED;
- Pocket Dimension conservée;
- téléportations et traversées contrôlées selon état RP.

### SCP-096
- système de capture;
- immobilisation par plusieurs opérateurs autorisés;
- sac sur la tête via rôle Spécialiste Confinement/FIM;
- une fois bagué: état escortable/reconfinable;
- retrait du sac uniquement par règles/interaction autorisée.

### SCP-173
- comportement de confinement RP;
- pas d'ouverture volontaire de dispositifs interdits;
- mécanisme de recontainment à prévoir sans simplement tuer le SCP.

### SCP-3114
- priorité interactions/infiltration;
- système RP de couverture / identité du corps utilisé;
- restrictions d'armes respectées.

## 3. SCP custom — première vague

### SCP-008-X
- infection transmissible;
- humain infecté -> transformation en rôle SCP-008-X après délai;
- plusieurs 008-X forment un seul outbreak logique;
- état OUTBREAK dans la machine d'état du Site;
- antidote/quarantaine possible plus tard;
- skins dédiés via UCR + SLWardrobe/ProjectMER si disponible.

### SCP-1048
- petit SCP conscient, non hostile par défaut;
- peut devenir hostile selon actions RP;
- capture/escorte possible;
- variantes:
  - SCP-1048-B: variante organique hostile;
  - SCP-1048-C: variante métallique protectrice;
- apparences custom communautaires à privilégier, pas de modèle improvisé.

### SCP-999
- SCP non hostile / social utile au DarkRP;
- interaction médicale/morale légère;
- aucune mécanique pay-to-win.

### SCP-131-A / SCP-131-B
- petits SCP non hostiles;
- compagnons de laboratoire / événements;
- utiles pour donner de la vie au Site en état NORMAL.

### SCP-2295
- SCP médical événementiel/non hostile;
- peut intervenir dans événements médicaux sous contrôle staff.

## 4. SCP event / deuxième vague

A étudier après validation de la première vague:
- SCP-1507 Alpha + instances;
- SCP-457;
- SCP-076-2;
- autres SCP compatibles avec modèles communautaires propres et gameplay reconfinable.

Principe: aucun SCP event ne spawn automatiquement dans le serveur persistant. Activation uniquement par staff/event/state machine.

## 5. Machine d'état commune

Chaque SCP custom/vanilla géré par SiteRP doit avoir un état explicite:
- CONTAINED
- TESTING
- COOPERATIVE
- HOSTILE
- BREACHED
- RECONTAINING
- RECONTAINED
- DISABLED (pour 079)

Le Site possède aussi:
- NORMAL
- INCIDENT
- BREACH
- MAJOR_BREACH
- EVACUATION

Ces deux niveaux doivent interagir (ex. SCP-008 outbreak -> INCIDENT/BREACH selon propagation).

## 6. Règles techniques

- Pas de respawn automatique MTF/Chaos/SCP custom.
- Pas de changement de map/destruction irréversible pour un SCP.
- Toutes les actions critiques ont logs.
- Cooldowns anti-abus pour 079, gaz, flash, portes, annonces, hacks.
- Les interactions sont limitées aux rôles autorisés.
- Les skins custom sont des assets communautaires vérifiés et compatibles ProjectMER/SLWardrobe; ne pas inventer de faux packs.
- Garder les IDs UCR stables.

## 7. Référence Faster Community

Fonctions publiques observées et utilisées uniquement comme inspiration de gameplay:
- SCP-079/C.A.S.S.I.E. avec portes, lumières, caméras, haut-parleurs;
- piratage/diagnostic/reconnexion dans la salle serveurs;
- gaz/flash de salle pour IA;
- SCP-049 réanimation élargie;
- SCP-939 gaz amnésique;
- SCP-106 faiblesse à la lumière + Femur Breaker;
- SCP-096 capture + sac;
- SCP-008-X;
- SCP-1048 et variantes;
- systèmes de techniciens/réparation.

Ne pas copier leur code, noms internes, assets privés ou logique propriétaire non publiée.
