# Installer multilingue 0.3.4

Distribuire `Luma-Setup-0.3.4-x64.exe`. Il launcher seleziona la lingua di visualizzazione dell'utente Windows, passa una delle 13 trasformazioni incorporate a msiexec e avvia il wizard Windows Installer completo. Inglese come base/fallback. Non esiste un selettore manuale. La revisione r2 corregge il riepilogo MSI e aggiunge setup.log nella cache sorgente; il launcher mostra un messaggio se msiexec termina con un errore.

Lingue: en, it, cs, de, es, fr, ja, ko, pl, pt-BR, ru, tr, zh-Hans e zh-Hant. Portoghese di qualsiasi regione usa pt-BR; Taiwan/Hong Kong/Macao/zh-Hant usano zh-TW. Il rilevamento corrisponde a quello dell'app.

Le risorse `localization/WixUI_*.wxl` sono originali e immutate dal tag v5.0.2 del repository wixtoolset/wix. Ciascuna contiene gli stessi 561 identificatori, comprendenti wizard, errori, avanzamento e manutenzione. `languages.json` aggiunge i messaggi specifici di Luma, il LanguageId e il messaggio di errore del launcher. Le risorse generate sono in artifacts/installer-build/0.3.4.

Il Package ProductCode è condiviso tra le varianti della stessa build. Il cab viene riutilizzato tra le compilazioni. La trasformazione include la lingua del prodotto e la codepage; si usa il tipo language di WiX con soppressione della differenza di codepage. Le trasformazioni sono incorporate tramite la tabella _Storages e il riepilogo MSI dichiara soltanto la lingua base (x64;1033). Un database MSI sorgente può dichiarare una sola lingua: un elenco multiplo provocava errore 1624. L'UpgradeCode è invariato; IgnoreLanguage permette aggiornamenti da una versione installata in un'altra lingua.

`InstallerTools.cs` manipola i database solo durante la build. `Setup.cs` avvia msiexec /i e attende il risultato; non contiene un nuovo motore di installazione. Il launcher è compilato x64 con il compilatore .NET Framework incluso in Windows. L'MSI sorgente estratto è conservato sotto LocalAppData/Luma/Installer, in una sottocartella con SHA-256, insieme a setup.log. Le trasformazioni incorporate rimangono disponibili nella sorgente MSI per manutenzione e disinstallazione. Non sono scaricati payload durante l'installazione.

Aprendo direttamente l'MSI si usa la lingua inglese. Per distribuzione amministrativa con una lingua specifica: `msiexec /i Luma-Setup-0.3.4-x64.msi TRANSFORMS=":Luma_ru-RU"`. Non distribuire le MSI intermedie delle altre lingue. Chiudere Luma prima dell'aggiornamento.

Nessuna installazione/upgrade o verifica visiva del wizard eseguita durante questa sessione; build e integrità delle risorse verificabili separatamente. App, Setup e MSI non firmati.
