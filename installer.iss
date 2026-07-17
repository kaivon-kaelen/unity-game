; ╔══════════════════════════════════════════════════════════════╗
; ║          Inno Setup Script — MonJeu                         ║
; ║                                                              ║
; ║  ⚠️  ADAPTE les valeurs marquées ← en commentaire          ║
; ║  ⚠️  Les variables ci-dessous sont passées via GitHub        ║
; ║      Actions (env:) — on les récupère ici via #define       ║
; ║                                                              ║
; ║  CHEMIN DU JEU : Build\{%GAME_NAME}.exe                     ║
; ║  OUTPUT      : output\{%GAME_NAME}-Setup.exe                ║
; ╚══════════════════════════════════════════════════════════════╝

#define GAME_NAME    "{%GAME_NAME%}"
#define GAME_VERSION "{%GAME_VERSION%}"
#define GAME_EXE     "{%GAME_NAME%}.exe"

; ─── Fichiers à inclure dans l'installeur ──────────────────────────
;    Ajoute ici tous les fichiers/dossiers produits par le build
;    Unity (Data/, MonoBleedingEdge/, etc.)

#define SOURCE_DIR "build"

[Setup]
; ▼ Remplace par le nom officiel de ton jeu
AppName={#GAME_NAME}
; ▼ Remplace par la version actuelle
AppVersion={#GAME_VERSION}
; ▼ Identifiant unique (utilise ton Unity Account ID ou un domaine)
AppPublisher=Kaivon Kaelen
AppPublisherURL=https://www.youtube.com/@kaivon_kaelen
; ▼ URL de support
AppSupportURL=https://www.youtube.com/@kaivon_kaelen

; ▼ Dossier d'installation par défaut (Program Files)
DefaultDirName={autopf}\{#GAME_NAME}
; ▼ Répertoire du menu Démarrer
DefaultGroupName={#GAME_NAME}
; ▼ Manifeste pour Windows 10/11 compatibilité
MinVersion=10.0

; ▼ Nom du fichier installeur généré (sans .exe ajouté automatiquement)
OutputBaseFilename={#GAME_NAME}-Setup-{#GAME_VERSION}
OutputDir=output

; ▼ Place l'installeur dans .\output\ à côté du script
;    (sera créé automatiquement s'il n'existe pas)

; ─── Zones d'installation autorisées ──────────────────────────
AllowNoIcons=yes
; Allow change of install dir
AllowUserPage=yes

[Languages]
Name: "french"; MessagesFile: "compiler:French.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; ── Fichiers principaux du build Unity ──────────────────────────
;    Remplace MonJeu.exe par le nom exact de ton exécutable
;    wildcard * = tous les fichiers à la racine du dossier build
;    flag deleteafterinstall = supprime les fichiers non listés
Source: "{#SOURCE_DIR}\{#GAME_EXE}"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SOURCE_DIR}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

; ── Exemple : ajouter un fichier additionnel ──────────────────
; Source: "README.txt"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#GAME_NAME}"; Filename: "{app}\{#GAME_EXE}"
Name: "{group}\Désinstaller {#GAME_NAME}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#GAME_NAME}"; Filename: "{app}\{#GAME_EXE}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#GAME_EXE}"; Description: "Lancer {#GAME_NAME}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{app}"