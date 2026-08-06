# Installer le thème et les pages sur Wikidot

Ce guide relie le dépôt GitHub au wiki `fr-scp-fundation.wikidot.com`.

## 1. Vérifier GitHub Pages

Dans le dépôt GitHub, ouvrir **Settings → Pages** puis vérifier que la publication utilise la branche `main` et le dossier `/ (root)`.

Le fichier suivant doit s'ouvrir publiquement :

`https://nexox9official-source.github.io/fr-scp-fundation/theme/project.css`

## 2. Installer le thème global

Dans Wikidot, ouvrir le gestionnaire du site, puis **Apparence et comportement → Thèmes → Thème personnalisé**.

Coller uniquement :

```css
@import url("https://nexox9official-source.github.io/fr-scp-fundation/theme/project.css");
```

Enregistrer puis actualiser le wiki avec `Ctrl + F5`.

### Variante par page

Si le thème ne doit pas être global, créer la page `component:theme` et y coller le contenu de [`wikidot/component-theme.txt`](../wikidot/component-theme.txt). Ajouter ensuite ceci en première ligne des pages concernées :

```text
[[include component:theme]]
```

Ne pas installer les deux méthodes en même temps : cela chargerait deux fois la feuille de style.

## 3. Construire la navigation

Créer ou modifier les pages suivantes :

| Adresse Wikidot | Fichier à copier |
|---|---|
| `/nav:top` | [`wikidot/nav-top.txt`](../wikidot/nav-top.txt) |
| `/nav:side` | [`wikidot/nav-side.txt`](../wikidot/nav-side.txt) |
| `/start` | [`wikidot/start.txt`](../wikidot/start.txt) |

Les liens rouges indiquent simplement qu'une page cible n'existe pas encore. Il suffit de créer ces pages ou de remplacer leur adresse dans le code.

## 4. Pages à créer ensuite

Ordre conseillé :

1. `guide-du-nouveau` — fonctionnement du wiki et règles essentielles ;
2. `scp-series` — index des dossiers SCP ;
3. `bibliotheque` — histoires, rapports et autres formats ;
4. `contribuer` — procédure pour proposer un article ;
5. `a-propos` — présentation du projet et mention « non officiel » ;
6. `credits` — auteurs, sources et licence CC BY-SA 3.0.

## 5. Modifier le design plus tard

Les réglages propres au projet se trouvent dans `theme/project.css`. Le fichier `theme/sigma9.css` sert de base et doit rester aussi proche que possible de la source originale afin de faciliter les mises à jour.

Après chaque modification CSS publiée sur GitHub, Wikidot peut conserver l'ancienne version quelques minutes. Tester avec une actualisation forcée (`Ctrl + F5`) ou dans une fenêtre privée.

