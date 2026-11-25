CREATE TABLE IF NOT EXISTS api_users (
    Id INT NOT NULL AUTO_INCREMENT,
    Username VARCHAR(50) NOT NULL,
    NormalizedUsername VARCHAR(50) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
    PasswordHash VARCHAR(512) NOT NULL,
    Role VARCHAR(16) NOT NULL,
    IsActive TINYINT(1) NOT NULL,
    SecurityStamp VARCHAR(32) NOT NULL,
    CreatedAtUtc DATETIME(6) NOT NULL,
    PRIMARY KEY (Id),
    UNIQUE KEY UX_api_users_NormalizedUsername (NormalizedUsername)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
