# Luma 0.3.4 — port Linux

Autore: KENNY. .NET 10 autonomo, Avalonia 12.1.4, Magick.NET 15.0.0, Tesseract C API 5 del sistema. Modelli OCR per le 14 lingue inclusi. Architettura Linux glibc x86_64/amd64. Il progetto Windows resta separato.

## Stato della consegna

Il port è compilato per Linux x64 e sono generati .deb, .rpm e .pkg.tar.zst usando nFPM 2.48.0 portabile su Windows. Non è stato installato WSL, una VM o un sistema Linux sul PC. Sono controllati i metadati dei pacchetti, l'eseguibile ELF, proprietario root e permessi 0755 dei launcher, presenza di modelli/configurazioni e dipendenze native. Non sono state eseguite la GUI, le installazioni/upgrade o l'OCR su Linux. La parità 1:1 di comportamento, qualità di rendering, velocità e RAM resta da verificare su desktop Linux; questi pacchetti non sono una release Linux certificata attraverso prove sul sistema.

## Funzioni portate

JPEG/PNG/GIF/HEIC/HEIF/WebP/AVIF/BMP/DIB/TIFF/ICO/SVG; riconoscimento firme dei file; orientamento EXIF; GIF animate con offset/trasparenza/disposal/delay e cicli; navigazione numerica naturale; zoom/100%/adatta/HiDPI; fullscreen; rotazione della vista; apertura tramite file picker, drag-and-drop e associazioni desktop; 14 lingue automatiche; temi chiaro/scuro/sistema persistenti; scroll adattato al tema; informazioni/versione/autore/icona; riconoscimento locale del testo, selezione per parole, Ctrl+C/menu/Copia tutto e Maiusc per pan sopra il testo.

Restano gli stessi limiti della 0.3.4 Windows: file 128 MiB, immagini 40 MP, GIF 1000 frame/24 milioni di pixel canvas, TIFF prima pagina, ICO variante maggiore, HEIF/AVIF/WebP immagine primaria/primo frame, SVG statico filtrato, OCR su copia fino a 2200 pixel con lingua sistema+inglese; niente OCR su GIF animate. Non sono aggiunte animazioni APNG/WebP, assenti su Windows.

Il backend X11 stabile di Avalonia conserva icona e integrazioni; su desktop Wayland usa XWayland. Non usa Wine. Il nuovo backend Wayland nativo non ha ancora tutte le integrazioni X11, quindi non viene selezionato. Installare/abilitare XWayland nelle sessioni Wayland. Le decorazioni della finestra e l'installer sono quelli del desktop e del gestore pacchetti Linux: non si possono riutilizzare DWM, WPF o il wizard MSI di Windows.

La lingua segue LANGUAGE/LC_ALL/LC_MESSAGES/LANG, con inglese di fallback; cambi della lingua di sessione che richiedono nuovo login si applicano al nuovo avvio. Preferenza tema: `${XDG_CONFIG_HOME:-~/.config}/luma/theme.txt`. Il tema del sistema viene dal provider piattaforma/portale del desktop Avalonia. La disponibilità di tema, dialoghi e clipboard dipende dai servizi del desktop, come avviene per le applicazioni native Linux.

## Installare

```sh
sudo apt install ./luma_0.3.4-1_amd64.deb
sudo dnf install ./luma-0.3.4-1.x86_64.rpm
sudo pacman -U ./luma-0.3.4-1-x86_64.pkg.tar.zst
```

Installazione /usr/lib/luma, launcher /usr/bin/luma, voce luma.desktop, icona e MIME inclusi. Selezionare Luma in «Apri con» per impostarla come predefinita: il pacchetto non sovrascrive le preferenze personali. Tema conservato alla disinstallazione.

Il runtime .NET, Magick/codec, Skia, modelli OCR e configurazioni SVG sono inclusi. glibc/libstdc++, fontconfig/X11/GTK, ICU/OpenSSL e libtesseract.so.5 sono dipendenze dichiarate e gestite da apt/dnf/pacman. Obiettivi iniziali: Debian/Ubuntu, Fedora e Arch x86_64 moderni; non musl/Alpine o ARM64. Il formato .rpm non rende automaticamente il pacchetto compatibile con ogni versione di RHEL/openSUSE; valgono dipendenze e ABI del sistema.

## Ricompilare

Con SDK .NET 10 e nFPM 2.48.0 su Windows:

```powershell
.\scripts\Build-Linux.ps1 -Nfpm C:\strumenti\nfpm.exe -Python python
```

Lo script pubblica Linux x64 e genera i tre pacchetti da una configurazione con permessi/owner Linux espliciti. I pacchetti sono in artifacts/linux/packages; SHA256SUMS ne contiene i checksum. Su Linux si può usare la stessa configurazione nFPM oppure linux/package.sh con dpkg-deb/rpmbuild. Per Arch è inclusa una ricetta PKGBUILD per makepkg eseguito su Arch come utente non root. I pacchetti nFPM hanno metadati pacman .PKGINFO/.INSTALL, non sono tar rinominati.

## Repository e release unificati

I sorgenti Windows e Linux sono ora file e cartelle del repository, non archivi ZIP usati come input della build. Consultare [docs/BUILDING.md](../docs/BUILDING.md) per la compilazione locale o tramite Actions. I due workflow restano separati e i pacchetti possono essere allegati alla stessa Release.

Il codice originale di Luma è GPL-3.0-only (LICENSE); le ricette includono il testo e dichiarano la licenza. I componenti di terze parti mantengono i propri termini. I pacchetti già generati prima della scelta della licenza non vengono modificati retroattivamente. Queste modifiche non costituiscono un audit completo delle dipendenze.

Pacchetti non firmati. GitHub Releases offre download manuali, non aggiornamenti automatici APT/DNF. La ricetta Arch inclusa è per la compilazione nel workflow, non è una ricetta AUR pubblicabile senza adattare URL e checksum.
