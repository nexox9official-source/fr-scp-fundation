# FR SCP Foundation — ressources Wikidot

<p align="center">
  <img src="theme/images/header-logo.svg" alt="Logo SCP" width="132">
</p>

<p align="center">
  <strong>Thème, composants et modèles de pages pour un wiki SCP francophone communautaire.</strong>
</p>

<p align="center">
  <img alt="Wikidot" src="https://img.shields.io/badge/plateforme-Wikidot-8b0015">
  <img alt="CSS" src="https://img.shields.io/badge/thème-Sigma--9-202020">
  <img alt="Licence" src="https://img.shields.io/badge/licence-CC%20BY--SA%203.0-b01">
</p>

> [!IMPORTANT]
> Ce projet est **communautaire et non officiel**. Il n'est ni affilié ni approuvé par la branche francophone officielle de la Fondation SCP ou par Wikidot.

## À quoi sert ce dépôt ?

Ce dépôt héberge les ressources utilisées par [fr-scp-fundation.wikidot.com](http://fr-scp-fundation.wikidot.com/) :

- le thème visuel Sigma-9 et sa personnalisation locale ;
- les images, polices et modules nécessaires au rendu ;
- des modèles prêts à coller pour les pages Wikidot ;
- un guide d'installation permettant de garder GitHub et Wikidot synchronisés.

## Installation rapide

1. Ouvrir le [guide d'installation Wikidot](docs/INSTALLATION-WIKIDOT.md).
2. Installer le thème globalement, ou utiliser `wikidot/component-theme.txt` pour un chargement page par page.
3. Copier les modèles `start`, `nav:top` et `nav:side` dans les pages correspondantes.
4. Remplacer ensuite les textes, liens et catégories par ceux du wiki.

Le fichier chargé par Wikidot est :

```css
@import url("https://nexox9official-source.github.io/fr-scp-fundation/theme/project.css");
```

## Organisation

| Dossier | Utilité |
|---|---|
| `theme/` | Thème principal, personnalisation, images et polices |
| `wikidot/` | Codes prêts à coller dans les pages du wiki |
| `docs/` | Procédure d'installation et documentation |
| `outils/` | Outils web complémentaires conservés depuis la base d'origine |

## Crédits et licence

Le thème repose sur **Sigma-9 CT Aleph** et sur des ressources créées par la communauté SCP. Les auteurs d'origine et les licences sont détaillés dans [CREDITS.md](CREDITS.md) et dans les en-têtes des fichiers concernés.

Sauf mention contraire, les adaptations de ce dépôt sont publiées sous licence [CC BY-SA 3.0](https://creativecommons.org/licenses/by-sa/3.0/deed.fr).
