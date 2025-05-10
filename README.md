# RestaurantManagerApp

Aplicație desktop dezvoltată în C# și WPF pentru gestionarea comenzilor online și a operațiunilor unui restaurant. Proiectul utilizează arhitectura MVVM pentru o structură curată și testabilă.

## Scopul Proiectului

Dezvoltarea unei soluții software complete care să permită:
*   Clienților: vizualizarea meniului, plasarea comenzilor online.
*   Angajaților restaurantului: gestionarea meniului, a stocurilor, procesarea comenzilor și generarea de rapoarte.

## Stadiul Actual al Proiectului (La data de [10.05.2025])

Proiectul se află în fazele inițiale de dezvoltare. Până în prezent, au fost realizate următoarele:

1.  **Configurarea Fundațiilor Proiectului:**
    *   Crearea proiectului WPF (.NET 8.0).
    *   Integrarea pachetului `CommunityToolkit.Mvvm` pentru implementarea MVVM.
    *   Configurarea unui fișier `appsettings.json` pentru setările aplicației (inclusiv șirul de conexiune).
    *   Stabilirea structurii de directoare pentru o bună organizare a codului.
2.  **Proiectarea și Implementarea Bazei de Date:**
    *   Definirea schemei bazei de date relaționale în SQL Server (`RestaurantDB`).
    *   Crearea tabelelor pentru: Categorii, Alergeni, Preparate, Utilizatori, Meniuri, Comenzi și tabelele de legătură asociate.
    *   Utilizarea principiilor de normalizare (3NF).
3.  **Definirea Modelelor de Date (POCOs):**
    *   Crearea claselor C# corespunzătoare tabelelor din baza de date în directorul `Models`.
4.  **Configurarea Accesului la Date cu Entity Framework Core:**
    *   Instalarea pachetelor NuGet necesare pentru EF Core (`Microsoft.EntityFrameworkCore.SqlServer`, `Tools`, `Design`).
    *   (Urmează) Crearea clasei `DbContext` (`RestaurantContext`) pentru interacțiunea cu baza de date.

## Funcționalități Planificate

*   **Autentificare Utilizatori:** Sistem de login/register pentru clienți și angajați.
*   **Vizualizare Meniu:** Afișarea detaliată a produselor și meniurilor, grupate pe categorii, cu imagini, prețuri, alergeni.
*   **Căutare și Filtrare:** Căutare avansată în meniu (după nume, ingrediente, alergeni "conține"/"nu conține").
*   **Plasare Comenzi:** Coș de cumpărături, proces de checkout, calcul preț final (cu reduceri și cost de transport).
*   **Management Comenzi (Client):** Vizualizare istoric comenzi, urmărire status comandă activă, anulare comandă.
*   **Management Produse și Meniuri (Angajat):** CRUD pentru categorii, preparate (cu alergeni, imagini, stoc), meniuri (cu preparate componente).
*   **Management Comenzi (Angajat):** Vizualizare toate comenzile, actualizare status, vizualizare detalii client.
*   **Rapoarte (Angajat):**
    *   Preparate cu stoc redus.
    *   (Alte rapoarte relevante pentru vânzări/operațiuni).

## Tehnologii Utilizate

*   **Limbaj de programare:** C#
*   **Framework UI:** WPF (Windows Presentation Foundation)
*   **Arhitectură:** MVVM (Model-View-ViewModel)
*   **Bază de Date:** SQL Server (Versiunea 2022 recomandată)
*   **ORM (Object-Relational Mapper):** Entity Framework Core
*   **Management Configurație:** `appsettings.json`
*   **Framework .NET:** .NET 8.0

## Cerințe de Sistem (pentru Dezvoltare)

*   Visual Studio 2022 (sau o versiune compatibilă cu .NET 8.0)
*   .NET 8.0 SDK
*   SQL Server (2019, 2022 Developer Edition sau Express Edition)
*   SQL Server Management Studio (SSMS)

## Setup și Rulare Proiect (Pași Generali)

1.  **Clonați repository-ul** (dacă proiectul este pe Git) sau descărcați sursele.
2.  **Configurați Baza de Date:**
    *   Asigurați-vă că aveți o instanță SQL Server funcțională.
    *   Rulați scriptul de creare a bazei de date (localizat în `DatabaseScripts/CreateRestaurantDB_Schema.sql`) pentru a crea baza de date `RestaurantDB` și tabelele aferente.
3.  **Actualizați Șirul de Conexiune:**
    *   Deschideți fișierul `appsettings.json` din rădăcina proiectului.
    *   Modificați valoarea `DefaultConnection` din secțiunea `ConnectionStrings` pentru a corespunde configurației dvs. SQL Server (nume server, tip autentificare).
4.  **Deschideți Soluția în Visual Studio:**
    *   Deschideți fișierul `.sln` (de ex., `RestaurantManagerApp.sln`).
5.  **Restaurați Pachetele NuGet:**
    *   Visual Studio ar trebui să facă acest lucru automat la deschiderea soluției. Dacă nu, dați click dreapta pe soluție în Solution Explorer și selectați "Restore NuGet Packages".
6.  **Build Proiect:**
    *   Din meniul "Build", selectați "Build Solution" (sau `Ctrl+Shift+B`).
7.  **Rulare Proiect:**
    *   Apăsați `F5` sau butonul "Start" din Visual Studio.

## Contribuții

Acesta este un proiect în curs de dezvoltare. Feedback-ul și sugestiile sunt binevenite.

---
