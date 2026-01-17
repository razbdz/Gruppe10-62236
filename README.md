
PackBot er et automatiseret sorteringssystem udviklet som en del af *Three Week Project* i kurset **Industrial Programming (62236)**. Projektet har til formål at demonstrere, hvordan et cyber-fysisk system kan designes og implementeres ved at integrere industrirobotteknologi, sensorer, software og databaser i én samlet løsning.

Systemet er målrettet automatisering af sortering af pakkebokse i et e-commerce- eller produktionsmiljø.
Ved hjælp af fysiske sensorinput identificeres boksens størrelse,
hvorefter systemet automatisk træffer en sorteringsbeslutning og verificerer denne op imod relevante ordredata i en database.
Projektet er udviklet som et **Minimum Viable Product (MVP)** med fokus på funktionel korrekthed, demonstrerbarhed og en klar og forståelig systemarkitektur.

Motivationen for PackBot udspringer af, at sortering i mange mindre produktions- og pakkeprocesser fortsat udføres manuelt. 
Dette kan medføre gentagende og monotont arbejde, en øget fejlrate ved høje ordrevolumener samt begrænset sporbarhed og dataopsamling. Projektet demonstrerer derfor,
hvordan en automatiseret løsning kan reducere manuel håndtering, øge stabilitet og ensartethed i sorteringsprocessen og samtidig skabe et datagrundlag, der kan anvendes til kvalitetssikring og analyse.

PackBot er opbygget omkring en tydelig systemarkitektur, hvor både fysiske og digitale komponenter indgår.
En industrirobot fra Universal Robots varetager den fysiske håndtering og sortering af bokse, mens to fotoelektriske sensorer (DI3 og DI7) anvendes til at bestemme boksens størrelse ud fra dens fysiske udstrækning.
En C#-baseret backend indeholder systemets beslutningslogik, databaseadgang og kommunikation med robotten, mens en grafisk brugergrænseflade (GUI) udviklet i Avalonia giver operatøren overblik over systemets tilstand og mulighed for interaktion. Derudover anvendes en lokal SQLite-database med Entity Framework Core til lagring af ordredata og brugerkonti samt verifikation af sorteringsbeslutninger. Samlet set illustrerer systemet samspillet mellem **Operational Technology (OT)** og **IT-systemer**, hvilket er centralt i moderne industriel automatisering.

Sorteringslogikken er baseret på input fra de to fotoelektriske sensorer.
Hvis både DI3 og DI7 er aktive, klassificeres boksen som stor, mens en boks klassificeres som lille, hvis DI7 er aktiv og DI3 ikke er det.
Hvis ingen af sensorerne er aktive, befinder systemet sig i en ventetilstand. Sorteringsbeslutningen træffes i robotprogrammet og vises tydeligt i GUI’en, så operatøren kan følge processen i realtid.

Databasen anvendt i projektet er en lokal SQLite-database (`packbot.sqlite`), som er implementeret med Entity Framework Core.
Databasen indeholder tabeller til både ordrer og brugerkonti. Ordretabellen anvendes til at verificere sorteringsbeslutninger, 
mens brugertabellen understøtter login og rollebaseret adgang. Databasen initialiseres automatisk ved opstart og seedes med demo-data for at sikre, 
at systemet kan demonstreres uden ekstern opsætning.

GUI’en fungerer som operatørens primære interface og er udviklet med fokus på enkelhed og overskuelighed. 
Den giver blandt andet mulighed for visning af sensorstatus, aktuel sorteringsbeslutning, robotstatus og programflow samt opslag af ordrer via Order ID.
Derudover understøtter GUI’en verifikation mellem sensorbaserede beslutninger og databaseinformation, logning af systemhændelser samt adgang til administrative funktioner ved login som administrator.

Systemet anvender rollebaseret login med fokus på grundlæggende sikkerhedsprincipper.
Passwords lagres ikke i klartekst, men håndteres via password hashing med PBKDF2 og salt.
Administrative funktioner er udelukkende tilgængelige for autoriserede brugere, hvilket understøtter princippet om *least privilege*.

Projektet demonstreres gennem GUI’en med live status, sensor-simulering uden fysisk hardware, database-seedede demo-ordrer samt verifikation af sorteringsbeslutninger.
Der er udarbejdet både en skærmoptagelse af GUI’en under kørsel og en video, der viser den fysiske robot i drift. Demonstrationsvideoen er tilgængelig via et eksternt link.

PackBot er udviklet som et MVP inden for en tre ugers projektperiode. 
Avancerede funktioner såsom fuld industriel fejlhåndtering, belastnings- og stresstest, avanceret brugeradministration og sikkerhedscertificering er bevidst fravalgt eller kun delvist implementeret.
Fokus har været på funktionel korrekthed, tydelig systemarkitektur og klar formidling af projektets idé og opbygning.

Projektet anvender følgende teknologier: C#, Avalonia UI, SQLite, Entity Framework Core, URScript, TCP-sockets samt PBKDF2 til password hashing.
Projektet er gennemført som et gruppeprojekt i kurset **Industrial Programming (62236)** med det overordnede formål at demonstrere industriel automatisering, softwareintegration og systemdesign.
