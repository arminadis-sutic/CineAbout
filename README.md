# CineAbout

## Brzi početak

1. Instaliraj i pokreni Docker Desktop.
2. Raspakiraj `.env-tajne.zip` u korijen repozitorija.
3. Pokreni:

   ```bash
   docker compose up --build
   ```

4. Sačekaj da API bude spreman.
   Provjeri: http://localhost:5218/api/Health
   Odgovor treba biti `healthy`.

5. Pokreni desktop aplikaciju:

   ```bash
   cd CineAboutApp.UI/CineAbout_ui
   flutter run -d windows
   ```

6. Pokreni mobilnu aplikaciju u Android emulatoru:

   ```bash
   cd CineAboutApp.UI/CineAbout_mobile
   flutter run
   ```

## O projektu

Mobilna i desktop aplikacija za ljubitelje filma: recenzije, forum, kvizovi, liste za gledanje i personalizovane preporuke.
Seminarski rad iz predmeta *Razvoj softvera II* (IB220011).

## Arhitektura

| Servis | Projekat | Namjena |
|---|---|---|
| `cineabout-api` | `CineAboutApp` | REST API (JWT autentifikacija, EF Core, SignalR), objavljuje poruke u RabbitMQ |
| `cineabout-subscriber` | `CineAboutApp.Subscriber` | Zaseban worker: prima RabbitMQ poruke i šalje e-mail (SMTP) i push (Firebase, vidi *Push notifikacije (dan izlaska) i e-mail*) |
| `cineabout-db` | SQL Server 2022 | Baza `220011`, kreiraju je EF migracije, a popunjava seeder |
| `cineabout-rabbitmq` | RabbitMQ 3.13 | Message broker |

Flutter klijenti (zasebne aplikacije):

- `CineAboutApp.UI/CineAbout_ui` - desktop administracija (Windows)
- `CineAboutApp.UI/CineAbout_mobile` - mobilna aplikacija za korisnike (Android)

## Preduvjeti

| Za | Potrebno je |
|---|---|
| Backend (API, worker, SQL Server, RabbitMQ) | Docker Desktop s Compose v2. Ništa drugo: slike se grade iz izvornog koda (.NET 8 SDK treba samo za build ili testiranje izvan Dockera). Slobodni portovi: 5218 (API), 1434 (SQL Server), 5672/15672 (RabbitMQ). |
| Desktop administracija (Windows) | Flutter (stable, građeno s 3.47.9) s podrškom za Windows desktop: Visual Studio s paketom *Desktop development with C++*, uključen Windows *Developer Mode*. |
| Mobilna aplikacija (Android) | Flutter, Android SDK i Android emulator (AVD) ili telefon s uključenim USB debugging. |

### Okruženje testiranja Android aplikacije

Funkcionalno testiranje mobilne aplikacije obavljeno je ručno
na fizičkom Android telefonu pomoću zasebnog LAN builda.
Provjereni su odjava i prijava, učitavanje početnog ekrana,
Discover praznog stanja, Watchlist, Profile i Forum.
Sama swipe radnja u Discoveru nije testirana.

Predajni release APK izgrađen je iz commita `9803bba` i koristi
zadanu API adresu `http://10.0.2.2:5218` za Android emulator (AVD).
LAN build korišten u telefonskom testiranju izrađen je ranije,
prije završnog čišćenja izvornog koda.

Predajni APK uspješno je instaliran i pokrenut na fizičkom telefonu,
ali na njemu ne može doseći backend preko adrese `10.0.2.2`,
što je očekivano jer je ta adresa namijenjena Android emulatoru.

Funkcionalni tokovi predajnog APK-a u AVD-u nisu testirani
zbog ograničenja lokalnog računara. Test LAN builda na telefonu
ne predstavlja provjeru tačnog predajnog APK-a u emulatoru.

Za korištenje na fizičkom telefonu potrebno je napraviti zaseban
build s `API_BASE_URL` postavljenim na LAN adresu računara,
prema uputama u nastavku.

## Pokretanje backenda

1. Za predaju, raspakiraj `.env-tajne.zip` u korijen repozitorija.
   Arhiva sadrži `.env` i zamjenjuje postojeći `.env`.
   Ne kreiraj `.env` ručno.

   Arhiva je šifrovana (AES-256); njena šifra daje se samo na DL sistemu, nikada u repozitoriju.
   Raspakiraj je u korijen, pored `docker-compose.yml`. Git ignoriše raspakirani `.env`.

   Za lokalni razvoj bez arhive, kopiraj `.env.example` u `.env` i popuni obavezne vrijednosti.
   Primjer sadrži samo placeholdere (nema tajni); promijeni barem `DB_PASSWORD`/`SQL_SA_PASSWORD` (moraju biti jednake) i `JWT_SECRET`.
2. Pokreni sve:

   ```bash
   docker compose up --build
   ```

3. Migracije se primjenjuju, a baza se popunjava automatski pri startu API-ja (prvi start: oko minut, dok log servisa `cineabout-api` ne ispiše `Application started`).
   Provjera: http://localhost:5218/api/Health odgovara s `healthy`. Nije potreban nikakav ručni SQL korak; vidi *Baza podataka i seed podaci* ispod.
4. API: http://localhost:5218 (Swagger je dostupan samo kada je `ASPNETCORE_ENVIRONMENT=Development`). RabbitMQ UI: http://localhost:15672.

**Stari Docker volumeni.** Ako je RabbitMQ volumen već postojao iz ranijeg pokretanja, prvo pokreni:

```bash
docker compose down -v
```

RabbitMQ prima novu lozinku samo pri prvom kreiranju volumena.
Ova komanda briše lokalne podatke baze i RabbitMQ-a.

Sva konfiguracija je u `.env`; ništa nije upisano tvrdo u izvorni kod ni u `appsettings.json` (koji sadrži samo prazne placeholdere i nivoe logiranja).
Pored postavki konekcije, `.env` sadrži trajanja tokena `JWT_EXPIRATION_HOURS`, `JWT_REFRESH_EXPIRATION_HOURS` i `JWT_RESET_EXPIRATION_MINUTES`;
API se ne pokreće i navodi koja vrijednost nedostaje ako je neka od njih odsutna. Stariji `.env` mora dobiti ove tri linije (vidi `.env.example`).

### Reset lozinke

`Forgot password?` šalje e-mailom link (`BACKEND_URL/reset-password?token=...`) preko RabbitMQ-a i subscribera (SMTP postavke su u `.env`).
Otvaranje linka na telefonu pokreće aplikaciju na ekranu za novu lozinku (`cineabout://app/reset`); token se može i ručno zalijepiti.

- Token čini 32 nasumična bajta, vrijedi `JWT_RESET_EXPIRATION_MINUTES` minuta (15 u `.env.example`) i može se iskoristiti samo jednom. Pohranjuje se samo hash s ključem, vezan za korisnika i svrhu.
- Novi zahtjev poništava starije linkove; najviše jedan link po računu u minuti; 8 zahtjeva u 15 minuta po adresi klijenta (`RateLimits__PasswordReset` za promjenu).
- Odgovor je isti za poznate i nepoznate e-mail adrese.
- Reset, promjena lozinke, promjena uloge ili deaktivacija završavaju sve sesije tog korisnika: refresh tokeni se brišu, a već izdati access tokeni prestaju važiti (security stamp se provjerava pri svakom zahtjevu).
- `BACKEND_URL` mora biti dostupan s uređaja koji otvara e-mail.

### Push notifikacije (dan izlaska) i e-mail - podešavanje vjerodajnica

Za predaju, sve potrebne Firebase vrijednosti već su isporučene: serverski ključ je u `.env-tajne.zip`, a Android konfiguracija `google-services.json` u repozitoriju. Reviewer ne kreira ni ne skida Firebase fajlove.

Push radi samo kada su ispunjeni svi uvjeti:

- stack radi s `DELIVERY_MODE=live`;
- `FIREBASE_CREDENTIALS_JSON` je prisutan u `.env`;
- Cloud Messaging API (V1) je omogućen za Firebase projekat.

Tok ostaje **API -> RabbitMQ -> worker**: API (reminder dispatcher, svakih 10 minuta) kreira obavijest "izlazi danas", upisuje je korisniku
(unutar aplikacije, SignalR) i objavljuje push poruku s tokenima uređaja korisnika koji imaju uključen push; `cineabout-subscriber` je šalje preko Firebasea.
Telefon registruje svoj token s `User/register-device` nakon prijave i uklanja ga kada se korisnik odjavi.

**Firebase (push).** Ništa tajno nije u repozitoriju niti u slici. Koraci ispod su potrebni samo kada se Firebase konfiguracija pravi ispočetka (ne i za predajnu arhivu).

1. Firebase konzola -> tvoj projekat -> Project settings -> Service accounts -> *Generate new private key*. Taj JSON fajl čuvaj na sigurnom; nije u Gitu.
2. Stavi **cijeli JSON u jednu liniju, u jednostrukim navodnicima**, u `.env`: `FIREBASE_CREDENTIALS_JSON='{"type":"service_account",...}'` i postavi `FIREBASE_PROJECT_ID=<your project id>`.
   Nizovi backslash-n unutar privatnog ključa ostaju tačno kakvi su u fajlu; jednostruki navodnici sprječavaju Docker Compose da bilo šta promijeni. Na mašini koja pokreće projekat nije potreban nikakav dodatni fajl:
   `.env` (isporučen u šifrovanoj konfiguracijskoj arhivi) nosi vjerodajnice, a worker ih čita pri startu. Nikada nisu dio slike, APK-a ni EXE-a.
   *Opcionalna alternativa:* ostavi `FIREBASE_CREDENTIALS_JSON` prazan i postavi `FIREBASE_CREDENTIALS_PATH=C:/path/to/the-key.json`; `docker-compose.yml` tada montira taj fajl samo za čitanje u worker.
   **Prioritet:** ako su popunjena oba, `FIREBASE_CREDENTIALS_JSON` pobjeđuje, a fajl se ignoriše. Ako nijedan nema privatni ključ, push je isključen (worker log to kaže).
3. `docker compose up -d --build cineabout-subscriber`; uz `DELIVERY_MODE=live` log workera mora ispisati `Firebase initialized from FIREBASE_CREDENTIALS_JSON` (ili `... from the mounted credentials file`). Neispravna JSON vrijednost prijavljuje se bez ispisivanja same vrijednosti.
   Uz `DELIVERY_MODE=test` Firebase aplikacija se ne kreira bez obzira na prisutne vjerodajnice.
4. Mobilna aplikacija: `CineAboutApp.UI/CineAbout_mobile/android/app/google-services.json` (Android klijentska konfiguracija paketa `com.cineabout.cineabout_mobile`, bez privatnog ključa) dio je repozitorija,
   pa je build APK-a preuzima bez ikakvog dodatnog koraka. Push na telefonu radi samo kada stack radi s `DELIVERY_MODE=live`, a service-account ključ iz koraka 2 je u `.env`.
   Ponovo izgradi APK (`flutter build apk --release --dart-define=API_BASE_URL=http://<PC LAN address>:5218`).
5. Cloud Messaging API (V1) mora biti omogućen za projekat. Android 13+ traži od korisnika dozvolu za obavijesti pri prvoj prijavi.

**Test režim.** `DELIVERY_MODE=test` čini da worker obrađuje poruke, ali ne šalje stvarne e-mailove ni push notifikacije
(SMTP se nikada ne poziva, a Firebase aplikacija se čak ni ne kreira; log workera počinje s `DELIVERY MODE = TEST`). Koristi ga za svako automatsko testiranje.
`DELIVERY_MODE=live` šalje stvarne e-mailove i push notifikacije. Predajna konfiguracija koristi `live`.

**E-mail (SMTP).** `.env`: `MAIL_HOST`, `MAIL_PORT` (587), `MAIL_ENABLE_SSL=true`, `MAIL_USERNAME` (prikazano ime), `MAIL_EMAIL` (adresa pošiljatelja) i `MAIL_PASSWORD`.
Ako su `MAIL_EMAIL`/`MAIL_PASSWORD` prazni, worker preskače e-mailove i to ispisuje u logu.
Za Gmail isporuku, `MAIL_PASSWORD` mora biti Gmail App Password (*lozinka za aplikaciju* naloga s dvostepenom verifikacijom), a ne obična lozinka Google naloga.
Gmail može odbiti prijavu ako App Password ili sigurnosne postavke naloga nisu ispravno konfigurisani.

### Novi nalozi

Je li nova registracija odmah aktivna određuje administratorska postavka `AutoActivateNewUsers` (desktop aplikacija, Settings; nova baza se seeduje s uključenom). Kada je isključena, nalog se kreira neaktivan: mobilna aplikacija tada prikazuje "Account created. An administrator has to activate it before you can sign in." i vraća se na ekran za prijavu, a administrator aktivira korisnika na desktop ekranu Users.

### Baza podataka i seed podaci

Baza se zove `220011` (broj indeksa). EF Core migracije kreiraju šemu (24 domenske tabele, strani ključevi definisani u EF konfiguraciji), a seeder je popunjava
jednom: koraci seedera bilježe se u `SeedHistories`, pa ponovno pokretanje nikada ne duplira redove i nikada ne prepisuje ono što je administrator uredio.
Start iz prazne baze daje (izmjereno na probnoj bazi s istom slikom):

| Oblast | Redova |
|---|---|
| Korisnici (10, uloge `Admin` / `User`), profili, nizovi (jednaki danima dnevnog kviza u demo sesijama) | 10 / 10 / 10 |
| Naslovi (svi s posterom), osobe (sve s fotografijom), studiji (7 s logom) | 29 / 121 / 13 |
| Žanrovi, jezici, države, gradovi, vrste nagrada, nagrade | 13 / 9 / 14 / 9 / 6 / 8 |
| Recenzije, glasovi za recenzije | 45 / 57 |
| Forum teme, komentari, glasovi za komentare, glasovi za teme | 12 / 32 / 56 / 41 |
| Redovi Watchlist / Interested, swipe-ovi (ulaz za recommender), praćenja | 32 / 60 / 13 |
| Pitanja kviza, kategorije, sesije | 33 / 5 / 22 |
| Prijave (moderatorski red), podsjetnici, obavijesti | 3 / 8 / 5 |

Četiri demo naloga posjeduju i nekoliko lista s naslovima (`UserList`), a demo podsjetnika nema (podsjetnik traži stvaran nadolazeći naslov s njegovim TMDB id-jem). Kasnije korekcije demo podataka su mali
verzionisani koraci (ključevi u `SeedHistories` poput `DemoLists-v1`): svaki se izvrši jednom po bazi i dira samo redove koje navodi.
Demo katalog (naslovi, žanrovi, režiseri, glumci s likovima, sinopsisi) uređen je u `CatalogSeeder`. Posteri, fotografije glumaca i logotipi studija su **linkovi** na
`image.tmdb.org` (`CatalogImages`), pa TMDB token nije potreban, ali mašina treba pristup internetu da bi se slike prikazale. Seedovani korisnici nemaju profilnu fotografiju (aplikacija prikazuje njihove inicijale).

### Tehničke tabele i njihove veze

Svaka tabela je stranim ključem povezana s podacima koje opisuje, osim tri koje nemaju na šta da pokazuju:

| Tabela | Čemu služi | Zašto nema strani ključ |
|---|---|---|
| `AppSettings` | Jedan red s postavkama administratora (danas: je li nova registracija odmah aktivna, `AutoActivateNewUsers`; desktop ekran *Settings*). | To je jedan zapis postavki; nijedan drugi red mu ne pripada. |
| `SeedHistories` | Jedan red po koraku seeda koji je već izvršen (`Seed.InitialCatalog.v1`, `DemoLists-v1`, ...). Pri svakom startu API ovdje provjerava da svaki korak izvrši samo jednom, pa ponovno pokretanje nikada ne duplira podatke niti prepisuje ono što je administrator uredio. | Marker je naziv i datum; ne opisuje nijedan entitet. |
| `PendingFileDeletions` | Red čekanja putanja fajlova (stare profilne slike) koje treba obrisati s diska. Red se upisuje u istom spremanju u bazu kao i promjena zbog koje je fajl postao suvišan; pozadinski servis briše fajl i uklanja red, a ponavlja pokušaj ako je fajl zaključan. | Putanja fajla nije entitet baze; red je radna stavka. |

`AuditLogs.PerformedByUserId` i `BlacklistedTokens.UserId` (ko se odjavio, postavlja se pri odjavi; stariji redovi nemaju zabilježenog vlasnika) su pravi strani ključevi prema korisnicima.
Nalozi se anonimizuju, nikada ne brišu, pa ove veze ostaju važeće kada se nalog obriše.

### Napomena o dizajnu: obavijesti

`IUserNotifier` (koristi se nakon uklanjanja recenzije, komentara, praćenja, odluke o prijavi, dana izlaska) poziva `NotificationService.CreateAsync`, koji sprema obavijest, a zatim šalje poruke unutar aplikacije / push
preko RabbitMQ-a i SignalR-a. Ovo se izvršava **nakon** što je glavna operacija potvrđena (commit), a njen neuspjeh se hvata, pa obavijest nikada ne može poništiti ono što je korisnik upravo uradio. Druga strana toga: **glavna operacija može uspjeti, a obavijest izostati** ako naknadni korak ne uspije (greška baze pri spremanju, RabbitMQ ili SignalR nedostupni). Neuspjeh se samo logira; ništa se ne ponavlja niti bilježi za kasnije. Ovo je običan poziv nakon commita, **ne** transactional outbox (outbox bi poruku pohranio u istoj transakciji kao i promjenu, a kasnije je pouzdano isporučio). To je dokumentirano odstupanje koje je izabrao autor; nije poznato prihvata li ga kurs. Premještanje obavijesti u jedno spremanje glavne
operacije zahtijevalo bi da push / SignalR poruke čekaju commit (post-commit hook, tj. outbox) i da neuspjela obavijest poništi korisnikovu radnju; zato je to dokumentirani izuzetak.

### TMDB (nadolazeća izdanja i podsjetnici)

U `.env` postavi `TMDB_ACCESS_TOKEN` na TMDB *API Read Access Token* (https://www.themoviedb.org/settings/api).
Bez njega `GET /api/Reminder/upcoming` odgovara jasnom porukom "not configured", a sve ostalo nastavlja raditi.
Pozadinski servis u API-ju kreira obavijest "izlazi danas" (unutar aplikacije i SignalR; objavljuje se i push poruka, vidi *Push notifikacije (dan izlaska) i e-mail*).

## Pregled API-ja

Sve rute su pod `/api` i traže JWT, osim: `User/login`, `register`, `refresh`, `reset-password`, `reset-password/confirm`, liste država `Country` (potrebna formi za registraciju), `Health` i stranice linka za reset `/reset-password`. Trenutni korisnik uvijek dolazi iz tokena.

| Oblast | Endpointi |
|---|---|
| Autentifikacija / korisnici | `User/login`, `register`, `refresh`, `logout`, `reset-password` (šalje link e-mailom), `reset-password/confirm`, `User/me`, `UserProfile/me` (upload fotografije: JPG/PNG/WEBP, najviše 2 MB, provjeravaju se deklarisani tip i stvarni sadržaj, stari fajl se uklanja; grad se bira preko `CityId` iz `City`), `UserProfile/{id}/image/{file}` (preuzimanje profilne slike: samo prijavljeni korisnici; vlasnik i administratori uvijek, ostali dok je profil javan; slike nisu javni statički fajlovi), `UserProfile/me/genres` |
| Filmovi | `Movie` (čitanje kada je korisnik prijavljen; pisanje samo admin), `Genre`, `Language`, `Country`, `City`, `Person` i `Studio` (+ `{id}/movies` za stranice osobe / studija), `AwardType` |
| Recenzije | `Review` (čitanje kada je korisnik prijavljen, korisnik kreira/uređuje/briše svoje), `Review/{id}/vote`, `Review/summary/{movieId}` (prosjek + histogram) |
| Forum | `Thread` (tagovi, oznaka spoilera, sortiranje), `Thread/tags`, `Thread/{id}/vote`, `Comment`, `Comment/{id}/vote` |
| Moderacija | `Report` (korisnici prijavljuju sadržaj; admin odlučuje preko `Report/{id}/decision`), `Review/{id}/status`, `Thread/{id}/status` |
| Liste / društveno | `Watchlist` (Watchlist + Interested), `UserList` (liste koje korisnik kreira: kreiranje/preimenovanje/brisanje, `UserList/{id}/items/{movieId}`; čitljive svakom prijavljenom korisniku, `?userId=` za tuđi profil), `Follow/{userId}`, `Follow/feed` (prikazuje i "created a list") |
| Kviz | `Quiz/daily/start`, `Quiz/free/start`, `Quiz/submit`, `Quiz/streak`, `Quiz/leaderboard`, `Quiz/categories` |
| Discover | `Discover` (preporuke s objašnjenjem), `Discover/swipe` |
| Podsjetnici | `Reminder/upcoming`, `Reminder` (straničen kao svaka lista, `PageSize` najviše 100) |
| Admin | `Dashboard/*`, `Export/community-activity`, `Export/content-popularity` (PDF), `AuditLog`, `QuizQuestion` (kategorija preko `categoryId`), `QuizCategory`, `Role` (kreiranje / preimenovanje / brisanje; `Admin` i `User` su fiksni), `AppSettings` |

Zahtjevi za kreiranje/izmjenu validiraju se na serveru (obavezna polja, dužine, raspon godina/datuma, postojeće reference, vrijednosti enuma) i odgovaraju s `400` i `{"errors": {...}}` te čitljivom porukom. Recenzija koju je administrator uklonio skrivena je od svih ostalih, i kada se čita po id-ju (`404`).

Enumi se prihvataju i vraćaju kao stringovi (npr. `"Interested"`); prihvataju se i numeričke vrijednosti.

## Pokretanje Flutter aplikacija

Adresa API-ja nikada nije upisana tvrdo: obje aplikacije je čitaju preko `String.fromEnvironment('API_BASE_URL')`.

| Klijent | Zadano (koriste ga predajni buildovi) | Ostali ciljevi |
|---|---|---|
| Desktop (Windows) | `http://localhost:5218` | bilo koji host: `--dart-define=API_BASE_URL=http://<host>:5218` |
| Android emulator (AVD) | `http://10.0.2.2:5218` (naziv koji emulator koristi za host PC) | - |
| Fizički telefon | ne može se koristiti sa zadanom vrijednošću | `--dart-define=API_BASE_URL=http://<PC-LAN-address>:5218` (vidi ispod) |

Desktop administracija (Windows; Windows *Developer Mode* mora biti uključen za Flutter plugine):

```bash
cd CineAboutApp.UI/CineAbout_ui
flutter run -d windows --dart-define=API_BASE_URL=http://localhost:5218
```

Mobilna aplikacija (Android; emulator host mašinu dosegne na `10.0.2.2`, što je zadana vrijednost):

```bash
cd CineAboutApp.UI/CineAbout_mobile
flutter run --dart-define=API_BASE_URL=http://10.0.2.2:5218
# fizički telefon u istoj mreži: koristi LAN adresu PC-ja (ipconfig), a ne 10.0.2.2
```

Zadana adresa `10.0.2.2` postoji samo unutar Android emulatora. Release APK izgrađen bez `--dart-define=API_BASE_URL=...` na pravom telefonu prikazuje
"Cannot reach the server". Za telefon: telefon i PC u istoj Wi-Fi/LAN mreži, build s LAN adresom PC-ja
(`ipconfig`), i dozvoli dolazni TCP 5218 za lokalnu mrežu u Windows Firewallu (API sluša na svim interfejsima: `API_HOST_BIND=0.0.0.0`).
Brza provjera iz browsera na telefonu: `http://<PC LAN address>:5218/api/Health` mora prikazati `{"status":"healthy",...}`.

Testovi: `flutter test` u `CineAbout_ui` pokreće widget testove. Svaka aplikacija ima i integracijske testove koji se prijavljuju seedovanim nalogom na pokrenuti API:
desktop `flutter test integration_test -d windows --dart-define=API_BASE_URL=http://localhost:5218`; mobilna (treba Android emulator ili telefon, nema Windows cilja)
`flutter test integration_test -d <device-id> --dart-define=API_BASE_URL=http://10.0.2.2:5218` (na telefonu koristi LAN adresu).
Poznato ograničenje: `flutter test` **deinstalira aplikaciju s uređaja kada završi**: koristi emulator koji možeš resetovati, a ne telefon s podacima do kojih ti je stalo. Mobilni test zatvara
softversku tastaturu i prije dodira pomjera svaku kontrolu u vidno polje (tastatura je na pravom telefonu prekrivala dugmad); ta ispravljena verzija još nije pokrenuta na uređaju.

## Release buildovi za predaju

Build fajlovi nisu u Gitu; prilažu se GitHub Releaseu kao jedan ZIP (`fit-build-YYYY-MM-DD.zip`). Gradi sa **zadanim** adresama (bez `--dart-define`):

```bash
cd CineAboutApp.UI/CineAbout_mobile
flutter clean
flutter build apk --release          # build/app/outputs/flutter-apk/app-release.apk   (API 10.0.2.2:5218, Android emulator)

cd ../CineAbout_ui
flutter clean
flutter build windows --release      # build/windows/x64/runner/Release/                (API localhost:5218)
```

ZIP sadrži APK i folder `Release` desktop builda, nikada `.env`, service-account ključ ni `google-services.json` kao zaseban fajl.
Build napravljen s LAN adresom (za fizički telefon) je lokalni testni build i ne smije biti onaj koji se predaje.

## Test nalozi

| Kontekst | Korisničko ime | Lozinka |
|---|---|---|
| Desktop (Admin) | `desktop` | `test` |
| Mobilna (User) | `mobile` | `test` |

Uloge: `Admin`, `User`.

## Recommender

Vidi [recommender-dokumentacija.md](recommender-dokumentacija.md).

## EF migracije

```bash
dotnet tool restore
dotnet dotnet-ef migrations add <Name> --project CineAboutApp.Services --startup-project CineAboutApp --output-dir Migrations
```
