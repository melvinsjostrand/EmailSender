I used phpmyadmin as an database, This is the tables you need and how you create them

-You create an PendingUser 

-You get an Confirm Email

-You confirm the Email and the user get created

CREATE TABLE PendingUser (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Role INT,
    Username VARCHAR(255),
    PasswordHash VARCHAR(255),
    Email VARCHAR(255),
    Address VARCHAR(255),
    Token VARCHAR(255),
    TokenExpiry DATETIME
);

CREATE TABLE `user` (
    `Id` INT AUTO_INCREMENT PRIMARY KEY,
    `Role` INT NOT NULL,
    `username` VARCHAR(50) NOT NULL UNIQUE,
    `password` VARCHAR(255) NOT NULL,
    `mail` VARCHAR(100) NOT NULL UNIQUE,
    `address` TEXT,
    `guid` VARCHAR(36) NOT NULL UNIQUE,
    `created_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
