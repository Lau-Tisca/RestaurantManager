USE RestaurantDB; -- ASIGURĂ-TE CĂ RULEZI ACEASTA ÎN CONTEXTUL BAZEI DE DATE CORECTE!
GO

-- =============================================
-- Tabel: Categorii
-- =============================================
IF OBJECT_ID('dbo.tblCategorii', 'U') IS NOT NULL
    DROP TABLE dbo.tblCategorii;
GO

CREATE TABLE dbo.tblCategorii (
    CategorieID INT IDENTITY(1,1) PRIMARY KEY,
    Nume NVARCHAR(100) NOT NULL UNIQUE,
    EsteActiv BIT NOT NULL DEFAULT 1
);
GO
PRINT 'Tabela tblCategorii creata.';
GO

-- =============================================
-- Tabel: Alergeni
-- =============================================
IF OBJECT_ID('dbo.tblAlergeni', 'U') IS NOT NULL
    DROP TABLE dbo.tblAlergeni;
GO

CREATE TABLE dbo.tblAlergeni (
    AlergenID INT IDENTITY(1,1) PRIMARY KEY,
    Nume NVARCHAR(100) NOT NULL UNIQUE,
    EsteActiv BIT NOT NULL DEFAULT 1
);
GO
PRINT 'Tabela tblAlergeni creata.';
GO

-- =============================================
-- Tabel: Preparate
-- =============================================
IF OBJECT_ID('dbo.tblPreparate', 'U') IS NOT NULL
    DROP TABLE dbo.tblPreparate; -- Va esua daca exista FK catre ea, sterge tabelele dependente intai
GO
-- Pentru a sterge in ordine corecta daca rulezi scriptul de mai multe ori
IF OBJECT_ID('dbo.tblPreparateAlergeni', 'U') IS NOT NULL DROP TABLE dbo.tblPreparateAlergeni;
IF OBJECT_ID('dbo.tblMeniuPreparate', 'U') IS NOT NULL DROP TABLE dbo.tblMeniuPreparate;
IF OBJECT_ID('dbo.tblElementeComanda', 'U') IS NOT NULL DROP TABLE dbo.tblElementeComanda;
IF OBJECT_ID('dbo.tblPreparate', 'U') IS NOT NULL DROP TABLE dbo.tblPreparate;
GO


CREATE TABLE dbo.tblPreparate (
    PreparatID INT IDENTITY(1,1) PRIMARY KEY,
    Denumire NVARCHAR(200) NOT NULL,
    Pret DECIMAL(10, 2) NOT NULL,
    CantitatePortie NVARCHAR(50) NOT NULL, -- ex: "300g", "500ml", "1 buc"
    CantitateTotalaStoc DECIMAL(10, 3) NOT NULL, -- ex: 3000.000 grame, 5000.000 ml, sau nr de portii
    UnitateMasuraStoc NVARCHAR(20) NOT NULL DEFAULT 'g', -- 'g', 'ml', 'buc' etc.
    CategorieID INT NOT NULL,
    Descriere NVARCHAR(MAX) NULL,
    CaleImagine NVARCHAR(255) NULL,
    EsteActiv BIT NOT NULL DEFAULT 1,

    CONSTRAINT FK_Preparate_Categorii FOREIGN KEY (CategorieID) REFERENCES tblCategorii(CategorieID),
    CONSTRAINT CK_Preparate_Pret CHECK (Pret >= 0),
    CONSTRAINT CK_Preparate_CantitateTotalaStoc CHECK (CantitateTotalaStoc >= 0)
);
GO
PRINT 'Tabela tblPreparate creata.';
GO

-- =============================================
-- Tabel: PreparateAlergeni (Legatura N-N)
-- =============================================
IF OBJECT_ID('dbo.tblPreparateAlergeni', 'U') IS NOT NULL
    DROP TABLE dbo.tblPreparateAlergeni;
GO

CREATE TABLE dbo.tblPreparateAlergeni (
    PreparatID INT NOT NULL,
    AlergenID INT NOT NULL,
    CONSTRAINT PK_PreparateAlergeni PRIMARY KEY (PreparatID, AlergenID),
    CONSTRAINT FK_PreparateAlergeni_Preparate FOREIGN KEY (PreparatID) REFERENCES tblPreparate(PreparatID) ON DELETE CASCADE,
    CONSTRAINT FK_PreparateAlergeni_Alergeni FOREIGN KEY (AlergenID) REFERENCES tblAlergeni(AlergenID) ON DELETE CASCADE
);
GO
PRINT 'Tabela tblPreparateAlergeni creata.';
GO

-- =============================================
-- Tabel: Utilizatori
-- =============================================
IF OBJECT_ID('dbo.tblUtilizatori', 'U') IS NOT NULL
    DROP TABLE dbo.tblUtilizatori; -- Va esua daca exista FK catre ea
GO
-- Pentru a sterge in ordine corecta daca rulezi scriptul de mai multe ori
IF OBJECT_ID('dbo.tblComenzi', 'U') IS NOT NULL DROP TABLE dbo.tblComenzi;
IF OBJECT_ID('dbo.tblUtilizatori', 'U') IS NOT NULL DROP TABLE dbo.tblUtilizatori;
GO


CREATE TABLE dbo.tblUtilizatori (
    UtilizatorID INT IDENTITY(1,1) PRIMARY KEY,
    Nume NVARCHAR(100) NOT NULL,
    Prenume NVARCHAR(100) NOT NULL,
    Email NVARCHAR(255) NOT NULL UNIQUE,
    NumarTelefon NVARCHAR(20) NULL,
    AdresaLivrare NVARCHAR(500) NULL,
    ParolaHash NVARCHAR(MAX) NOT NULL, -- Se va stoca hash-ul parolei
    TipUtilizator NVARCHAR(50) NOT NULL,
    DataInregistrare DATETIME NOT NULL DEFAULT GETDATE(),
    EsteActiv BIT NOT NULL DEFAULT 1,

    CONSTRAINT CK_Utilizatori_TipUtilizator CHECK (TipUtilizator IN ('Client', 'Angajat'))
);
GO
PRINT 'Tabela tblUtilizatori creata.';
GO

-- =============================================
-- Tabel: Meniuri
-- =============================================
IF OBJECT_ID('dbo.tblMeniuri', 'U') IS NOT NULL
    DROP TABLE dbo.tblMeniuri; -- Va esua daca exista FK catre ea
GO
-- Pentru a sterge in ordine corecta daca rulezi scriptul de mai multe ori
IF OBJECT_ID('dbo.tblMeniuPreparate', 'U') IS NOT NULL DROP TABLE dbo.tblMeniuPreparate;
IF OBJECT_ID('dbo.tblElementeComanda', 'U') IS NOT NULL DROP TABLE dbo.tblElementeComanda;
IF OBJECT_ID('dbo.tblMeniuri', 'U') IS NOT NULL DROP TABLE dbo.tblMeniuri;
GO

CREATE TABLE dbo.tblMeniuri (
    MeniuID INT IDENTITY(1,1) PRIMARY KEY,
    Nume NVARCHAR(200) NOT NULL,
    CategorieID INT NOT NULL,
    Descriere NVARCHAR(MAX) NULL,
    CaleImagine NVARCHAR(255) NULL,
    EsteActiv BIT NOT NULL DEFAULT 1,
    -- Pretul se calculeaza dinamic, nu se stocheaza aici, dar se poate adauga un camp pentru pretul de baza daca se doreste.
    -- Reducerea de X% se aplica la calculul final.

    CONSTRAINT FK_Meniuri_Categorii FOREIGN KEY (CategorieID) REFERENCES tblCategorii(CategorieID)
);
GO
PRINT 'Tabela tblMeniuri creata.';
GO

-- =============================================
-- Tabel: MeniuPreparate (Legatura N-N)
-- =============================================
IF OBJECT_ID('dbo.tblMeniuPreparate', 'U') IS NOT NULL
    DROP TABLE dbo.tblMeniuPreparate;
GO

CREATE TABLE dbo.tblMeniuPreparate (
    MeniuID INT NOT NULL,
    PreparatID INT NOT NULL,
    CantitateInMeniu NVARCHAR(50) NOT NULL, -- ex: "200g", "1 buc" (specifica pentru acest preparat in acest meniu)
    UnitateMasuraCantitateInMeniu NVARCHAR(20) NOT NULL DEFAULT 'g', -- 'g', 'ml', 'buc'
    CONSTRAINT PK_MeniuPreparate PRIMARY KEY (MeniuID, PreparatID),
    CONSTRAINT FK_MeniuPreparate_Meniuri FOREIGN KEY (MeniuID) REFERENCES tblMeniuri(MeniuID) ON DELETE CASCADE,
    CONSTRAINT FK_MeniuPreparate_Preparate FOREIGN KEY (PreparatID) REFERENCES tblPreparate(PreparatID) ON DELETE CASCADE -- Atentie la ON DELETE CASCADE aici
);
GO
PRINT 'Tabela tblMeniuPreparate creata.';
GO

-- =============================================
-- Tabel: Comenzi
-- =============================================
IF OBJECT_ID('dbo.tblComenzi', 'U') IS NOT NULL
    DROP TABLE dbo.tblComenzi; -- Va esua daca exista FK catre ea
GO
-- Pentru a sterge in ordine corecta daca rulezi scriptul de mai multe ori
IF OBJECT_ID('dbo.tblElementeComanda', 'U') IS NOT NULL DROP TABLE dbo.tblElementeComanda;
IF OBJECT_ID('dbo.tblComenzi', 'U') IS NOT NULL DROP TABLE dbo.tblComenzi;
GO

CREATE TABLE dbo.tblComenzi (
    ComandaID INT IDENTITY(1,1) PRIMARY KEY,
    UtilizatorID INT NOT NULL,
    DataComanda DATETIME NOT NULL DEFAULT GETDATE(),
    CodUnic NVARCHAR(20) NOT NULL UNIQUE,
    StareComanda NVARCHAR(50) NOT NULL DEFAULT 'Inregistrata',
    AdresaLivrareComanda NVARCHAR(500) NOT NULL,
    NumarTelefonComanda NVARCHAR(20) NULL,
    Subtotal DECIMAL(10, 2) NOT NULL,
    DiscountAplicat DECIMAL(10, 2) NOT NULL DEFAULT 0,
    CostTransport DECIMAL(10, 2) NOT NULL DEFAULT 0,
    TotalGeneral DECIMAL(10, 2) NOT NULL,
    OraEstimataLivrare DATETIME NULL,
    Observatii NVARCHAR(MAX) NULL,

    CONSTRAINT FK_Comenzi_Utilizatori FOREIGN KEY (UtilizatorID) REFERENCES tblUtilizatori(UtilizatorID),
    CONSTRAINT CK_Comenzi_StareComanda CHECK (StareComanda IN ('Inregistrata', 'Confirmata', 'In Pregatire', 'Plecata Spre Client', 'Livrata', 'Anulata', 'Refuzata')),
    CONSTRAINT CK_Comenzi_Subtotal CHECK (Subtotal >= 0),
    CONSTRAINT CK_Comenzi_DiscountAplicat CHECK (DiscountAplicat >= 0),
    CONSTRAINT CK_Comenzi_CostTransport CHECK (CostTransport >= 0),
    CONSTRAINT CK_Comenzi_TotalGeneral CHECK (TotalGeneral >= 0)
);
GO
PRINT 'Tabela tblComenzi creata.';
GO

-- =============================================
-- Tabel: ElementeComanda (Legatura Comanda cu Preparate/Meniuri)
-- =============================================
IF OBJECT_ID('dbo.tblElementeComanda', 'U') IS NOT NULL
    DROP TABLE dbo.tblElementeComanda;
GO

CREATE TABLE dbo.tblElementeComanda (
    ElementComandaID INT IDENTITY(1,1) PRIMARY KEY,
    ComandaID INT NOT NULL,
    PreparatID INT NULL, -- Poate fi un preparat individual
    MeniuID INT NULL,    -- Sau poate fi un meniu
    Cantitate INT NOT NULL,
    PretUnitarLaMomentulComenzii DECIMAL(10, 2) NOT NULL, -- Pretul efectiv la care s-a vandut (poate include reduceri specifice produsului/meniului)
    SubtotalElement DECIMAL(10, 2) NOT NULL, -- Cantitate * PretUnitarLaMomentulComenzii

    CONSTRAINT FK_ElementeComanda_Comenzi FOREIGN KEY (ComandaID) REFERENCES tblComenzi(ComandaID) ON DELETE CASCADE,
    CONSTRAINT FK_ElementeComanda_Preparate FOREIGN KEY (PreparatID) REFERENCES tblPreparate(PreparatID), -- Fara ON DELETE CASCADE aici, pentru istoric
    CONSTRAINT FK_ElementeComanda_Meniuri FOREIGN KEY (MeniuID) REFERENCES tblMeniuri(MeniuID),       -- Fara ON DELETE CASCADE aici, pentru istoric
    CONSTRAINT CK_ElementeComanda_Cantitate CHECK (Cantitate > 0),
    CONSTRAINT CK_ElementeComanda_PretUnitar CHECK (PretUnitarLaMomentulComenzii >= 0),
    CONSTRAINT CK_ElementeComanda_TipProdus CHECK ( (PreparatID IS NOT NULL AND MeniuID IS NULL) OR (PreparatID IS NULL AND MeniuID IS NOT NULL) )
);
GO
PRINT 'Tabela tblElementeComanda creata.';
GO

PRINT 'Toate tabelele au fost create cu succes!';
GO