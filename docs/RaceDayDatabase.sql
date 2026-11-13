--CREATIN A DATABASE FOR RACEDAY 
CREATE DATABASE RaceDayDB;
USE RaceDayDB;

--USER TABLE 
--Stores the login and persona; information for every user.
--The user can be either an organiser and participant

CREATE TABLE [User]
(
    UserID INT IDENTITY(1,1) PRIMARY KEY,
    FirstName NVARCHAR(50) NOT NULL,
    LastName NVARCHAR(50) NOT NULL,
    Email NVARCHAR(100) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(255) NOT NULL,
    Role NVARCHAR(20) NOT NULL,
    PhoneNumber NVARCHAR(20),
    CreatedAt DATETIME DEFAULT GETDATE()
);

-- ORGANISER TABLE
-- Stores the organiser's specific information 
CREATE TABLE Organiser
(
    OrganiserID INT PRIMARY KEY,
    OrganisationName NVARCHAR(100) NOT NULL,

    FOREIGN KEY (OrganiserID)
        REFERENCES [User](UserID)
);

--PARTICIPANT TABLE
--Stores participant-specific information.
CREATE TABLE Participant
(
    ParticipantID INT PRIMARY KEY,
    DateOfBirth DATE,
    EmergencyContactName NVARCHAR(100),
    EmergencyContactPhone NVARCHAR(20),
    ProfilePictureUrl NVARCHAR(255),

    FOREIGN KEY (ParticipantID)
        REFERENCES [User](UserID)
);

--EVEN TABLE 
--Stores race events created by organisers.
CREATE TABLE Event
(
    EventID INT IDENTITY(1,1) PRIMARY KEY,
    OrganiserID INT NOT NULL,
    Name NVARCHAR(100) NOT NULL,
    Description NVARCHAR(255),
    EventDate DATE NOT NULL,
    Location NVARCHAR(100) NOT NULL,
    DistanceKm DECIMAL(5,2) NOT NULL,
    EventType NVARCHAR(50) NOT NULL,
    BannerImageUrl NVARCHAR(255),
    CreatedAt DATETIME DEFAULT GETDATE(),

    FOREIGN KEY (OrganiserID)
        REFERENCES Organiser(OrganiserID)
);

--CATEGORY TABLE 
--Stores race categories for each event.
CREATE TABLE Category
(
    CategoryID INT IDENTITY(1,1) PRIMARY KEY,
    EventID INT NOT NULL,
    Name NVARCHAR(100) NOT NULL,
    MinAge INT NOT NULL,
    MaxAge INT NOT NULL,
    DistanceKm DECIMAL(5,2) NOT NULL,

    FOREIGN KEY (EventID)
        REFERENCES Event(EventID)
);

-- ENROLMENT TABLE 
-- Links Participants, Events, and Categories.
CREATE TABLE Enrolment
(
    EnrolmentID INT IDENTITY(1,1) PRIMARY KEY,
    ParticipantID INT NOT NULL,
    EventID INT NOT NULL,
    CategoryID INT NOT NULL,
    EnrolmentDate DATETIME DEFAULT GETDATE(),
    Status NVARCHAR(20) DEFAULT 'Pending',

    FOREIGN KEY (ParticipantID)
        REFERENCES Participant(ParticipantID),

    FOREIGN KEY (EventID)
        REFERENCES Event(EventID),

    FOREIGN KEY (CategoryID)
        REFERENCES Category(CategoryID)
);

--RESULT TABLE 
--Stores the official race result for each enrolment.
CREATE TABLE Result
(
    ResultID INT IDENTITY(1,1) PRIMARY KEY,
    EnrolmentID INT UNIQUE NOT NULL,
    FinishTime TIME,
    FinishPosition INT,
    RecordedAt DATETIME DEFAULT GETDATE(),

    FOREIGN KEY (EnrolmentID)
        REFERENCES Enrolment(EnrolmentID)
);

--DATA SAMPLE 
--Insert realistic records into every table.
--Insert four users (2 Organisers and 2 Participants)
INSERT INTO [User]
(FirstName,LastName,Email,PasswordHash,Role,PhoneNumber)
VALUES
('Mokgadi','Molapo','molapo@gmail.com','hash235','Organiser','0723456989'),
('Tendo','Manyama','tendo@raceday.com','hash453','Organiser','0712345678'),
('Mike','Mtheta','mike96@gamil.com','hash230','Participant','0824337675'),
('Thandi','Nkosi','thandi@icloud.com','hash413','Participant','0784596321');


-- Insert organisers linked to existing users
INSERT INTO Organiser
VALUES
(1,'RunFast Events'),
(2,'Elite Sports');

-- Insert participant profiles
INSERT INTO Participant
VALUES
(3,'2000-04-10','Mary Mtheta','0821111111',NULL),
(4,'1998-08-15','Peter Nkosi','0822222222',NULL);

-- Insert three race events
INSERT INTO Event
(OrganiserID,Name,Description,EventDate,Location,DistanceKm,EventType,BannerImageUrl)
VALUES
(1,'Polokwane Marathon','Annual city marathon','2026-10-15','Polokwane',42.20,'Road Race',NULL),
(1,'City Fun Run','Community race','2026-09-01','Polokwane',10.00,'Fun Run',NULL),
(2,'Trail Challenge','Mountain trail race','2026-11-20','Magoebaskloof',21.10,'Trail Run',NULL);

-- Insert race categories
INSERT INTO Category
(EventID,Name,MinAge,MaxAge,DistanceKm)
VALUES
(1,'Open',18,39,42.20),
(1,'Veterans',40,99,42.20),
(2,'Fun Run',12,99,10.00),
(3,'Open Trail',18,99,21.10);

-- Insert participant enrolments
INSERT INTO Enrolment
(ParticipantID, EventID, CategoryID, Status)
VALUES
(3,1,1,'Approved'),
(4,2,3,'Approved'),
(3,3,4,'Pending');

-- Insert race results
INSERT INTO Result
(EnrolmentID,FinishTime,FinishPosition)
VALUES
(1,'03:45:20',15),
(2,'00:52:40',8);

-- TESTING AND SHOWING RESULTS 
SELECT * FROM [User];
SELECT * FROM Organiser;
SELECT * FROM Participant;
SELECT * FROM Event;
SELECT * FROM Category;
SELECT * FROM Enrolment;
SELECT * FROM Result;